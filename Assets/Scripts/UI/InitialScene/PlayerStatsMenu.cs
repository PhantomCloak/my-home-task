using TMPro;
using UnityEngine;

public class PlayerStatsMenu : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_NameText;

    [SerializeField]
    private TMP_Text m_LaunchCountText;

    [SerializeField]
    private TMP_Text m_PingText;

    public void OnEnable()
    {
#if UNITY_EDITOR
		if(!CloudApi.IsConnected)
			return;
#endif

        using (var snapHandle = new SnapshotHandle(AcquireType.ReadOnly)) {
			m_NameText.text = $"Name: {snapHandle.Value.PlayerName}";
			m_LaunchCountText.text = $"Launch Count: {snapHandle.Value.Stats.LaunchCount}";
			m_PingText.text = $"Ping : 10ms";
		}
    }

    public void OnClickFind() { }
}
