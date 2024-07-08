using System;
using System.Threading;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MatchmakingState
{
	Idle,
    Cancelled,
    Searching,
    Matched
}

public class MatchMakingMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text m_StatusText;

    [SerializeField]
    private Button m_FindButton;

    [SerializeField]
    private Button m_CancelButton;

	private Color m_MatchFoundColor;

    [Header("Props")]
    [SerializeField]
    private string s_QueueName = "DemoQueue";

    [SerializeField]
    private int m_PollFrequencyMs = 6000;

    private static Task s_PollThread;
    private static CancellationTokenSource s_PollThreadCTS = new();
    private static CancellationToken s_PollThreadCT = s_PollThreadCTS.Token;

    private TimeSpan m_SearchStartTime;

    public static MatchMakingMenu Instance;

    private MatchmakingState m_MatchmakingState;

    public bool IsMatchmakingInProgress
    {
        get { return m_MatchmakingState == MatchmakingState.Searching; }
    }

    private void Awake()
    {
        Instance = this;
    }

    private EntityKey GetCurrentPlayerEntityKey()
    {
        return new EntityKey { Id = CloudApi.EntityId, Type = "title_player_account", };
    }

    private void Update()
    {
        switch (m_MatchmakingState)
        {
			case MatchmakingState.Cancelled:
				m_MatchmakingState = MatchmakingState.Idle;
				break;
            case MatchmakingState.Searching:
                m_FindButton.interactable = false;
                m_CancelButton.gameObject.SetActive(true);

                m_StatusText.text =
                    $"Searching ({Math.Floor((DateTime.Now.TimeOfDay - m_SearchStartTime).TotalSeconds)})";
                break;
            case MatchmakingState.Matched:
                m_StatusText.text = "Match Found!";
                m_CancelButton.gameObject.SetActive(false);

				var colors = m_FindButton.colors;
				colors.disabledColor = m_MatchFoundColor;
				m_FindButton.colors = colors;
                break;
			case MatchmakingState.Idle:
                m_StatusText.text = "Find Match!";
				m_FindButton.interactable = SelectionAreaMenu.Instance.HasCharacterSelected;
                m_CancelButton.gameObject.SetActive(false);
				break;
        }
    }

    public void OnClickFind()
    {
        if (IsMatchmakingInProgress)
        {
            CancelTicket();
            return;
        }

        CancelTicket(() =>
        {
            CreateTicket();
        });
    }

    public void OnClickCancel()
    {
        CancelTicket();
    }

    private void CreateTicket()
    {
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer { Entity = GetCurrentPlayerEntityKey(), },
                GiveUpAfterSeconds = 120,
                QueueName = s_QueueName,
            },
            OnMatchmakingTicketCreated,
            OnMatchmakingError
        );
    }

    private void CancelTicket(Action onComplete = null)
    {
        Debug.Log("All tickets are cancelled");

        m_SearchStartTime = TimeSpan.Zero;
        if (s_PollThread != null)
        {
            s_PollThreadCTS.Cancel();
        }

        PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(
            new CancelAllMatchmakingTicketsForPlayerRequest()
            {
                QueueName = s_QueueName,
                Entity = GetCurrentPlayerEntityKey()
            },
            (result) =>
            {
                m_MatchmakingState = MatchmakingState.Cancelled;
                onComplete?.Invoke();
            },
            OnMatchmakingError
        );
    }

    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult result)
    {
        Debug.Log($"Matchmaking ticket created: {result.TicketId}");

        m_MatchmakingState = MatchmakingState.Searching;
        m_SearchStartTime = DateTime.Now.TimeOfDay;

        s_PollThread = Task.Run(async () =>
        {
            while (m_MatchmakingState == MatchmakingState.Searching)
            {
                var ticketResult = await GetMatchmakingTicketAsync(result.TicketId);

                Debug.Log(ticketResult.Status);

                if (ticketResult.Status == "Canceled")
                {
                    CancelTicket();
                }
                else if (ticketResult.Status == "Matched")
                {
                    StartMatch(ticketResult);
                }

                await Task.Delay(m_PollFrequencyMs, s_PollThreadCT);
            }
        });
    }

    private void StartMatch(GetMatchmakingTicketResult result)
    {
        Debug.Log("Match Opponents");

        foreach (var player in result.Members)
        {
            Debug.Log($"Opponent ID: {player.Entity.Id} Attributes: {player.Attributes.ToJson()}");
        }

        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest { MatchId = result.MatchId, QueueName = s_QueueName, },
            (matchResult) =>
            {
                Debug.Log($"Match Id: {matchResult.MatchId}");
                Debug.Log($"Match ArrangementString: {matchResult.ArrangementString}");

                foreach (var player in matchResult.Members)
                {
                    Debug.Log(
                        $"Match Opponent ID: {player.Entity.Id} Attributes: {player?.Attributes?.ToJson()}"
                    );
                }

                m_MatchmakingState = MatchmakingState.Matched;
            },
            this.OnMatchmakingError
        );
    }

    private Task<GetMatchmakingTicketResult> GetMatchmakingTicketAsync(string ticketId)
    {
        var tcs = new TaskCompletionSource<GetMatchmakingTicketResult>();

        PlayFabMultiplayerAPI.GetMatchmakingTicket(
            new GetMatchmakingTicketRequest { TicketId = ticketId, QueueName = s_QueueName, },
            result => tcs.SetResult(result),
            error => tcs.SetException(new Exception("Matchmaking error"))
        );

        return tcs.Task;
    }

    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult result)
    {
        Debug.Log($"Ticket Status: {result.Status}");

        foreach (var player in result.Members)
        {
            Debug.Log($"Player: {player.Entity.Id} - {player.Entity.Type}");
        }
    }

    private void OnMatchmakingError(PlayFabError error)
    {
        Debug.LogError($"An error occured at matchmaking. Error: {error.GenerateErrorReport()}");
    }
}
