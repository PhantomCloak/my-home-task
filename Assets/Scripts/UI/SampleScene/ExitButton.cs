using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
	public void OnClick() {
		
		MultiplayerManager.Instance?.Disconnect();
		CloudApi.Clear();
		SceneManager.LoadScene(0);
	}
}
