using System.Collections.Generic;
using Newtonsoft.Json;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class Game : MonoBehaviour
{
	[Header("UI")]
    [SerializeField]
    private TMP_Text m_ScoreText;

    [SerializeField]
    private int m_NumOfWood = 10;

    [SerializeField]
    private int m_Radius = 10;

    private Dictionary<int, PhotonView> m_SpawnedWoods = new();

    public static Game Instance;
    private const string m_ResourcePrefabPath = "Prefabs/";

	private int m_Score = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!CloudApi.IsConnected || MultiplayerManager.IsConnected == false)
        {
            Debug.Log("Start game as an offline");
            StartOffline();
            SpawnWoods(false);
            return;
        }

        StartOnline();
        if (MultiplayerManager.Instance.IsMasterClient)
        {
            SpawnWoods(true);
        }
    }

    private void StartOffline()
    {
        var playerPrefab = Resources.Load(m_ResourcePrefabPath + GetPrefabName(Character.Red));
        var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        player.GetComponent<DemoPlayer>().enabled = true;
    }

    private void StartOnline()
    {
        Vector3 randomPosition = new Vector3(Random.Range(0f, 3f), 0, Random.Range(0f, 3f));
        Debug.Log(JsonConvert.SerializeObject(SharedVariables.CurrentMultiplayerRoom));

        var currentPlayer = PhotonNetwork.Instantiate(
            m_ResourcePrefabPath
                + GetPrefabName(SharedVariables.CurrentMultiplayerRoom.MyCharacter),
            randomPosition,
            Quaternion.identity,
            0
        );
        currentPlayer.GetComponent<DemoPlayer>().enabled = true;
    }

    private void SpawnWoods(bool networked)
    {
        var woodPrefab = Resources.Load(m_ResourcePrefabPath + "Wood");
        for (int i = 0; i < m_NumOfWood; i++)
        {
            Vector3 randomWoodPosition = new Vector3(
                Random.Range(-m_Radius, m_Radius),
                0,
                Random.Range(-m_Radius, m_Radius)
            );

            if (networked)
            {
                var woodObj = PhotonNetwork.Instantiate(
                    m_ResourcePrefabPath + "Wood",
                    randomWoodPosition,
                    Quaternion.identity,
                    0
                );
            }
            else
            {
                Instantiate(woodPrefab, randomWoodPosition, Quaternion.identity);
            }
        }
    }

    public void AddWood(WoodResource woodObj)
    {
        if (MultiplayerManager.IsConnected)
        {
            var woodView = woodObj.GetComponent<PhotonView>();
            m_SpawnedWoods[woodView.ViewID] = woodView;
			Debug.Log($"Wood Added {woodView.ViewID} - {m_SpawnedWoods.Count}");
        }
    }

    public void DestroyWood(WoodResource obj)
    {
        if (MultiplayerManager.IsConnected)
        {
            var objView = obj.GetComponent<PhotonView>();
            PhotonView.Get(this).RPC(nameof(OnWoodDestroy), RpcTarget.MasterClient, objView.ViewID);
        }
        else
        {
            Destroy(obj.gameObject);
        }

		m_Score++;
		m_ScoreText.text = $"Score: {m_Score}";
    }

    public void SelectWoodOthers(WoodResource obj)
    {
        if (MultiplayerManager.IsConnected)
        {
            PhotonView
                .Get(this)
                .RPC(
                    nameof(OnSelectWoodOther),
                    RpcTarget.Others,
                    obj.GetComponent<PhotonView>().ViewID
                );
        }
    }

    public void DeSelectWoodOthers(WoodResource obj)
    {
        if (MultiplayerManager.IsConnected)
        {
            PhotonView
                .Get(this)
                .RPC(
                    nameof(OnDeSelectWoodOther),
                    RpcTarget.Others,
                    obj.GetComponent<PhotonView>().ViewID
                );
        }
    }

    [PunRPC]
    private void OnSelectWoodOther(int viewId)
    {
        var wood = m_SpawnedWoods[viewId].GetComponent<WoodResource>();
        wood.SelectOther();
    }

    [PunRPC]
    private void OnDeSelectWoodOther(int viewId)
    {
        var wood = m_SpawnedWoods[viewId].GetComponent<WoodResource>();
        wood.DeselectOther();
    }

    [PunRPC]
    private void OnWoodDestroy(int viewId)
    {
        PhotonNetwork.Destroy(m_SpawnedWoods[viewId]);
        m_SpawnedWoods.Remove(viewId);
    }

    private string GetPrefabName(Character character)
    {
        if (character == Character.Red)
            return "PlayerRed";
        else if (character == Character.Green)
            return "PlayerGreen";
        else if (character == Character.Yellow)
            return "PlayerYellow";

        throw new System.Exception($"Chracter type {character} does not exist.");
    }
}
