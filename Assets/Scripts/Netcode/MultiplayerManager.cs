using System;
using System.Linq;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetcodeCallbackResult
{
    bool IsSuccess;
    string ErrorMessage;

    public NetcodeCallbackResult() { }
    public NetcodeCallbackResult(bool isSuccess, string errorMessage = "") { IsSuccess = isSuccess; ErrorMessage = errorMessage; }
};

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerManager Instance;

    private Action<NetcodeCallbackResult> m_JoinCallback;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public int Ping
    {
        get
        {
            return PhotonNetwork.GetPing();
        }
    }

    public static bool IsConnected
    {
        get
        {
            return PhotonNetwork.IsConnected;
        }
    }

    public string NetcodeUserId
    {
        get
        {
            return PhotonNetwork.LocalPlayer.UserId;
        }
    }

    public bool IsMasterClient
    {
        get
        {
            return PhotonNetwork.LocalPlayer.IsMasterClient;
        }
    }

    public void Connect()
    {
        Debug.Log("Trying to connect photon network..");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = Application.version;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void JoinOrCreateRoom(string roomId, Action<NetcodeCallbackResult> callback, IEnumerable<string> reservedSlots = null)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = false;
        roomOptions.PublishUserId = true;
        PhotonNetwork.JoinOrCreateRoom(roomId, roomOptions, null, reservedSlots != null ? reservedSlots.ToArray() : null);
		m_JoinCallback = callback;
    }

    public override void OnConnectedToMaster()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = false;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat(
            "OnDisconnected() was called by PUN with reason {0}",
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
    }

    public override void OnJoinedRoom()
    {
        m_JoinCallback?.Invoke(new NetcodeCallbackResult(true));
    }
}
