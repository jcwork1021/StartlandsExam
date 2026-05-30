using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [Header("Room List UI")]
    [SerializeField] private GameObject roomEntryPrefab;
    
    //Keeps track of active rooms sent by Photon
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private List<GameObject> spawnedRoomEntries = new List<GameObject>();

    [Header("For Debug")]
    public bool isConnected;

    private void Awake()
    {
        //Check if an instance already exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // Destroy duplicate instances
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("Connecting to Photon Server...");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;

    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server!");
        PhotonNetwork.JoinLobby(); //calls so we can enter Lobby so we can view active rooms, how many rooms is active and player list etc
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby!");
        isConnected = true;
        ClearRoomListUI();      // refresh on lobby entry
        GenerateRoomListUI();
    }


    //calls in LobbyManager.cs ClickCreateRoomBtn()
    public void ClickCreateRoom()
    {
        // Generates a random name, e.g., "Room 4821"
        string roomName = "Room " + Random.Range(1000, 9999);
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 4,
            IsOpen = true,      // explicitly set
            IsVisible = true    // explicitly set
        };

        RememberMe.Instance.playerRoomNameEntered = roomName; //Save the room name on player RememberMe
        PhotonNetwork.CreateRoom(roomName, roomOptions); //calls OnJoinedRoom when success
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        RememberMe.Instance.EnableNotificationCanvas("Create Room Failed!, Retrying", false);
        Debug.LogWarning("Room creation failed: " + message);
        ClickCreateRoom(); // retry with a new random name
    }


    // PHOTON CALLBACK: Automatically runs whenever rooms are created, removed, or changed
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        ClearRoomListUI();
        GenerateRoomListUI();
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Remove room from cache if it was removed from server or marked invisible
            if (info.RemovedFromList || !info.IsVisible || !info.IsOpen)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                // Update or add the room info
                cachedRoomList[info.Name] = info;
            }
        }
    }

    private void ClearRoomListUI()
    {
        foreach (GameObject entry in spawnedRoomEntries)
        {
            Destroy(entry);
        }
        spawnedRoomEntries.Clear();
    }

    private void GenerateRoomListUI()
    {
        // ADD THIS NULL CHECK
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("LobbyManager Has No Instance");
            return;
        }

        if(LobbyManager.Instance.roomListContentPanel == null)
        {
            Debug.LogError("roomListContentPanel is null!");
            return;
        }

        foreach (RoomInfo info in cachedRoomList.Values)
        {
            GameObject newEntry = Instantiate(roomEntryPrefab, LobbyManager.Instance.roomListContentPanel);
            RoomEntry script = newEntry.GetComponent<RoomEntry>();
            script.Initialize(info.Name, info.PlayerCount, info.MaxPlayers);
            spawnedRoomEntries.Add(newEntry);
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);

        RememberMe.Instance.EnableNotificationCanvas("Loading Scene Please Wait", false);
        RememberMe.Instance.travelCamera.SetActive(true); //enable travel camera
        PhotonNetwork.LoadLevel("Game");
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Disconnected from Photon because: {cause}");
        RememberMe.Instance.EnableNotificationCanvas("Cannot Connect to the Network", true);

    }
}
