using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerStatsMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text m_NameText;

    [SerializeField]
    private TMP_Text m_LaunchCountText;

    [SerializeField]
    private TMP_Text m_WoodCount;

    [SerializeField]
    private TMP_Text m_PingText;

    [Header("Props")]
    private float m_PollPingSec = 1.0f;
    private float m_LastPollPing = 0;

    public void OnEnable()
    {
#if UNITY_EDITOR
        if (!CloudApi.IsConnected)
            return;
#endif

        using (var snapHandle = new SnapshotHandle(AcquireType.ReadOnly))
        {
            m_NameText.text = $"Name: {snapHandle.Value.PlayerName}";
            m_LaunchCountText.text = $"Launch Count: {snapHandle.Value.Stats.LaunchCount}";
			m_WoodCount.text = $"Wood Count: {snapHandle.Value.Items.FirstOrDefault(item => item.Name == "wood")?.Count ?? 0}x";
        }
    }

    private void Update()
    {
        if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsConnected)
        {
            if (Time.time - m_LastPollPing > m_PollPingSec)
            {
                int ping = MultiplayerManager.Instance.Ping;
                string color = GetPingColor(ping);
                m_PingText.text = $"Ping: <color={color}>{ping}ms</color>";

                m_LastPollPing = Time.time;
            }
        }
    }

    private string GetPingColor(int ping)
    {
        if (ping <= 50)
        {
            return "green";
        }
        else if (ping <= 120)
        {
            return "yellow";
        }
        else
        {
            return "red";
        }
    }

    public void OnClickFind() { }
}
