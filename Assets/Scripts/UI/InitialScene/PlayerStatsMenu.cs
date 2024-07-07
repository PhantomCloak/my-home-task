using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        using (var snapHandle = new SnapshotHandle(AcquireType.ReadOnly)) {
			m_NameText.text = $"Name: {snapHandle.Value.PlayerName}";
			m_LaunchCountText.text = $"Launch Count: {snapHandle.Value.Stats.LaunchCount}";
			m_PingText.text = $"Ping : 10ms";
		}
    }

    public void OnClickFind() { }
}
