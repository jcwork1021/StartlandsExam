using UnityEngine;
using TMPro;
using Photon.Pun;

public class RoomEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;

    private string roomName;

    //This is called by the NetworkManager when spawning the button
    public void Initialize(string name, int currentPlayers, int maxPlayers)
    {
        roomName = name;
        roomNameText.text = $"{name} ({currentPlayers}/{maxPlayers})";
    }

    //Add Onclick in the parent button, added manually
    public void JoinThisRoom()
    {
        if (!NetworkManager.Instance.isConnected)
        {
            RememberMe.Instance.EnableNotificationCanvas("Not Connected to server yet, Try again later", false);
            return;
        }


        Debug.Log("Joining room: " + roomName);
        PhotonNetwork.JoinRoom(roomName);
    }
}