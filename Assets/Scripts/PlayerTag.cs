using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerTag : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_TagText;

    private void Start()
    {
        if (MultiplayerManager.IsConnected)
        {
            PhotonView photonView = PhotonView.Get(this);
            m_TagText.text = photonView.Owner.NickName;
        }
        else
        {
            m_TagText.text = "Player";
        }
    }
}
