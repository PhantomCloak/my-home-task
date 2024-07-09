using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Photon.Pun;
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
    Matched,
    Launching
}

struct MatchmakeInfo
{
    public string PhotonId;
    public string PlayFabId;
    public Character SelectedCharacter;
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

    [SerializeField]
    private Color m_MatchFoundColor;

    [Header("Props")]
    [SerializeField]
    private string s_QueueName = "DemoQueue";

    [SerializeField]
    private int m_PollFrequencyMs = 6000;

    private static Coroutine s_LoadMatchSequenceCoroutine;
    private static Task s_PollThread;
    private static CancellationTokenSource s_PollThreadCTS = new();
    private static CancellationToken s_PollThreadCT = s_PollThreadCTS.Token;

    public static MatchMakingMenu Instance;

    private MatchmakingState m_MatchmakingState;
    private TimeSpan m_SearchStartTime;

    public bool IsMatchmakingInProgress
    {
        get { return m_MatchmakingState == MatchmakingState.Searching; }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable() { }

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
                m_MatchmakingState = MatchmakingState.Launching;

                m_CancelButton.gameObject.SetActive(false);

                var colors = m_FindButton.colors;
                colors.disabledColor = m_MatchFoundColor;
                m_FindButton.colors = colors;

                if (s_LoadMatchSequenceCoroutine != null)
                {
                    StopCoroutine(s_LoadMatchSequenceCoroutine);
                }

                s_LoadMatchSequenceCoroutine = StartCoroutine(LoadMatchSequence());
                break;
            case MatchmakingState.Launching:
                m_StatusText.text =
                    $"Match Launching! {MultiplayerManager.Instance.IsMasterClient}";
                break;
            case MatchmakingState.Idle:
                m_StatusText.text = "Find Match!";
                m_FindButton.interactable = SelectionAreaMenu.Instance.HasCharacterSelected;
                m_CancelButton.gameObject.SetActive(false);
                break;
        }
    }

    IEnumerator LoadMatchSequence()
    {
        int totalPlayersInRoom = SharedVariables.CurrentMultiplayerRoom.PlayersInRoom.Count;

        while (!(PhotonNetwork.InRoom && PhotonNetwork.PlayerList.Length == totalPlayersInRoom))
        {
            yield return new WaitForSeconds(0.5f);
        }

        // To handle cases where Master client disconnected / changed
        // below statement can be wrapped into loop then everything should work but ATM there is no disconnect callback
        if (MultiplayerManager.Instance.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SampleScene");
            yield break;
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

    // Additionally we should check game version etc.
    private void CreateTicket()
    {
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer
                {
                    Entity = GetCurrentPlayerEntityKey(),
                    Attributes = new MatchmakingPlayerAttributes
                    {
                        DataObject = new MatchmakeInfo
                        {
                            PhotonId = MultiplayerManager.Instance.NetcodeUserId,
                            PlayFabId = CloudApi.PlayFabId,
                            SelectedCharacter = SelectionAreaMenu.Instance.SelectedCharacter
                        },
                    },
                },
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
                Cleanup();
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
        m_MatchmakingState = MatchmakingState.Matched;

        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest
            {
                MatchId = result.MatchId,
                QueueName = s_QueueName,
                ReturnMemberAttributes = true
            },
            (matchResult) =>
            {
                Debug.Log($"Match Id: {matchResult.MatchId}");
                Debug.Log($"Match ArrangementString: {matchResult.ArrangementString}");

                var fetchTasks = new List<Task>();
                var slots = new HashSet<string>();
                foreach (var player in matchResult.Members)
                {
                    Debug.Log(
                        $"Match Opponent ID: {player.Entity.Id} Attributes: {player?.Attributes?.ToJson()}"
                    );

                    if (player.Attributes != null)
                    {
                        var info = JsonConvert.DeserializeObject<MatchmakeInfo>(
                            player.Attributes.DataObject.ToString()
                        );

                        if (player.Entity.Id == CloudApi.EntityId)
                        {
                            SharedVariables.CurrentMultiplayerRoom.MyCharacter =
                                info.SelectedCharacter;
                            SharedVariables.CurrentMultiplayerRoom.PlayersInRoom.TryAdd(
                                info.SelectedCharacter,
                                Snapshot.CurrentSnapshoot
                            );
                        }
                        else
                        {
                            fetchTasks.Add(
                                Task.Run(async () =>
                                {
                                    var snap = await Snapshot.GetSnapshotOtherAsync(info.PlayFabId);
                                    Debug.Log("Got other player snap");
                                    SharedVariables.CurrentMultiplayerRoom.PlayersInRoom.TryAdd(
                                        info.SelectedCharacter,
                                        snap
                                    );
                                })
                            );
                        }

                        slots.Add(info.PhotonId);
                    }
                    else
                    {
                        Debug.LogError(
                            $"{player.Entity.Id} has missing Attribute data ${player.Attributes?.ToJson()}."
                        );
                        // Handle scenario here.
                        // ...
                        // ...
                    }
                }

                // Run deferred tasks after validation, optional: add timeout
                Task.WhenAll(fetchTasks)
                    .ContinueWith(_ =>
                    {
                        MultiplayerManager.Instance.JoinOrCreateRoom(
                            matchResult.MatchId,
                            OnNetcodeJoinRoom,
                            slots
                        );
                        m_MatchmakingState = MatchmakingState.Matched;
                    });
            },
            this.OnMatchmakingError
        );
    }

    private void TryLaunchGame() { }

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

    private void OnNetcodeJoinRoom(NetcodeCallbackResult result)
    {
        Debug.Log("Room Joined");
        PhotonNetwork.NickName = Snapshot.CurrentSnapshoot.PlayerName;
    }

    private void Cleanup()
    {
        SharedVariables.CurrentMultiplayerRoom = new Room();

        if (s_PollThread != null)
        {
            s_PollThreadCTS.Cancel();
            s_PollThread.Dispose();
            s_PollThread = null;
        }

        if (s_LoadMatchSequenceCoroutine != null)
        {
            StopCoroutine(s_LoadMatchSequenceCoroutine);
        }
    }
}
