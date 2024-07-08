using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
	public static MultiplayerManager Instance;

	private void Awake() {
		Instance = this;
		DontDestroyOnLoad(this);
	}

	public int Ping {
		get {
			return PhotonNetwork.GetPing();
		}
	}

	public bool IsConnected {
		get {
			return PhotonNetwork.IsConnected;
		}
	}

    public void Connect()
    {
        Debug.Log("Trying to connect photon network..");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = Application.version;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");

		RoomOptions roomOptions = new RoomOptions();
		roomOptions.IsVisible = false;
		//PhotonNetwork.JoinOrCreateRoom("Room01", roomOptions, null);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat(
            "PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}",
            cause
        );
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("Room list fetch...");
        foreach (var room in roomList)
        {
            Debug.Log(room.Name);
        }
        //PhotonNetwork.JoinRoom("RoomOne");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(
            $"PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room. Number: {PhotonNetwork.CountOfPlayersInRooms}. IsMaster: {PhotonNetwork.IsMasterClient}"
        );
    }
}
