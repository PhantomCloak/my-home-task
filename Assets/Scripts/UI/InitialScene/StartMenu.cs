using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [SerializeField]
    private MatchMakingMenu m_MatchMakingMenu;

    private bool m_LoginInProgress = false;

    public void OnStartClick()
    {
        if (m_LoginInProgress)
            return;

        // Since authenticating users from different devices/platforms beyond scope of this demo we simply use device Id
        // There is few options we may consider for future
        // 1) Simply use external platforms using PlayFab's oAuth features
        // 2) Generating server-side identifier then put relation with external identifiers such as Google's or Apple's user Id, Device Identifier e.g

#if UNITY_IOS
        var request = new LoginWithIOSDeviceIDRequest()
        {
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithIOSDeviceID(request, OnLoginSuccess, OnLoginFailure);
#elif UNITY_ANDROID
        //...
        //...
        //...
#else
        var request = new LoginWithCustomIDRequest
        {
            CustomId = Application.isEditor ? "Dev_Editor" : SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
			InfoRequestParameters = new() {
				GetUserAccountInfo = true,
			}
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
#endif

        m_LoginInProgress = true;
    }

    private void PlayerInitialization()
    {
        Snapshot.IsSnapshotExist((snapshotExist) =>
        {
            bool isFirstTime = !snapshotExist;
            if (!isFirstTime)
            {
                Snapshot.UpdateSnapshot((success) =>
                {
                    if (!success)
                    {
                        Debug.LogError("An error occured while getting the snapshot.");
                        m_LoginInProgress = false;
						DisplayMatchMakingMenu();
                        return;
                    }

                    using (var snapHandle = new SnapshotHandle(AcquireType.ReadWrite))
                    {
                        snapHandle.Value.Stats.LaunchCount++;
                    }

					DisplayMatchMakingMenu();
                });
                return;
            }

            // Do initial setup, resources etc. here
            using (var snapHandle = new SnapshotHandle(AcquireType.ReadWrite))
            {
                snapHandle.Value = new PlayerSnapshot();
                snapHandle.Value.Stats.LaunchCount = 1;
                snapHandle.Value.PlayerName = "demo_player_" + new System.Random().Next(0, int.MaxValue);
                snapHandle.Value.Items.Add(new InventoryItem("Wood", 0));
            }

			DisplayMatchMakingMenu();
        });
    }

    private void DisplayMatchMakingMenu()
    {
		MultiplayerManager.Instance.Connect();
        this.gameObject.SetActive(false);
        m_MatchMakingMenu.gameObject.SetActive(true);
    }

    private void OnLoginSuccess(LoginResult result)
    {
		CloudApi.EntityId = result.EntityToken.Entity.Id;
		Debug.Log("Entity Id: " + CloudApi.EntityId);
        PlayerInitialization();
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError($"An error occured while authenticating using PlayFab. Error: {error.GenerateErrorReport()}");
    }
}
