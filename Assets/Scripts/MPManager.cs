using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MPManager : MonoBehaviourPunCallbacks
{
    public static MPManager Instance;

    [Header("Reference from Itself")]
    public ChatManager chatManagerCS;

    [Header("Reference from Its Child")]
    public GameObject mpCamera;
    public CinemachineFreeLook freeLook;
    public GameObject pauseCanvas;
    public GameObject chatCanvas;
    public TMP_InputField chatInput;
    public Transform chatHistoryContent;

    [Header("Reference from Project")]
    [SerializeField] private GameObject malePlayerPrefab;
    [SerializeField] private GameObject femalePlayerPrefab;

    [Header("Fill via Script")]
    public GameObject localPlayer;
    public ThirdPersonController tpc;

    [Header("For Debugging")]
    public bool isPause;
    public bool isChatting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        mpCamera.gameObject.SetActive(false); // Only Enable Locally once the game is loaded
        pauseCanvas.SetActive(false);
        chatCanvas.SetActive(false);

        // Only Master Client saves the data and fires the initial load
        if (PhotonNetwork.IsMasterClient)
        {
            // Save to room properties so ANY late-joiner (or re-joiner) can read it
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "mapName", RememberMe.Instance.playerMapName }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            photonView.RPC("RPC_LoadAdditiveScene", RpcTarget.All, RememberMe.Instance.playerMapName);
        }
    }

    //When user joined this will be called
    public override void OnJoinedRoom()
    {
        //Safety check: The Master Client already loaded the scene in Start(),
        //so only non-master clients (rejoiners) need to run this.
        if (!PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("mapName", out object mapName))
            {
                StartCoroutine(LoadAdditiveSceneAndSpawn(mapName.ToString()));
            }
        }
    }

    [PunRPC]
    private void RPC_LoadAdditiveScene(string sceneName)
    {
        StartCoroutine(LoadAdditiveSceneAndSpawn(sceneName));
    }

    private IEnumerator LoadAdditiveSceneAndSpawn(string sceneName)
    {
        //Disable the RPC etc since we are still loading the scene, it is not present yet
        PhotonNetwork.IsMessageQueueRunning = false;
        // Load the scene additively
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Wait until the scene is fully loaded
        yield return new WaitUntil(() => asyncLoad.isDone);

        yield return new WaitForEndOfFrame();

        //Turn the network processing since the additive scene is loaded
        PhotonNetwork.IsMessageQueueRunning = true;

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        GameObject spawnPointObj = GameObject.FindWithTag("SpawnPoint");

        if (spawnPointObj != null)
        {
            SpawnPoint spawnPointScript = spawnPointObj.GetComponent<SpawnPoint>();

            if (spawnPointScript != null && spawnPointScript.spawnPoint.Length > 0)
            {
                // Pick a random spawn point
                int randomIndex = Random.Range(0, spawnPointScript.spawnPoint.Length);
                Transform chosenSpawn = spawnPointScript.spawnPoint[randomIndex];

                //Spawn the player prefab over the network
                if (RememberMe.Instance.playerGender == "male")
                {
                    localPlayer =  PhotonNetwork.Instantiate(malePlayerPrefab.name, chosenSpawn.position, chosenSpawn.rotation);
                }
                else
                {
                    localPlayer = PhotonNetwork.Instantiate(femalePlayerPrefab.name, chosenSpawn.position, chosenSpawn.rotation);
                }

                Debug.Log("Player spawned at: " + chosenSpawn.name);
            }
            else
            {
                // Fallback: spawn at origin if no SpawnPoint found
                RememberMe.Instance.EnableNotificationCanvas("There is no spawner", true);

                if (RememberMe.Instance.playerGender == "male")
                {
                    localPlayer = PhotonNetwork.Instantiate(malePlayerPrefab.name, Vector3.zero, Quaternion.identity);
                }
                else
                {
                    localPlayer = PhotonNetwork.Instantiate(femalePlayerPrefab.name, Vector3.zero, Quaternion.identity);
                }

                localPlayer.name = RememberMe.Instance.playerName;
            }
        }

        FixCamera();
    }

    public void FixCamera()
    {
        tpc = localPlayer.GetComponent<ThirdPersonController>();

        if(tpc != null)
        {
            tpc.enabled = true;

            freeLook.Follow = tpc.target;
            freeLook.LookAt = tpc.target;


            //Disable the Loading Scene now
            RememberMe.Instance.notificationCanvas.SetActive(false);

            RememberMe.Instance.travelCamera.SetActive(false); //Disable travel camera
            mpCamera.gameObject.SetActive(true); //Only Enable Locally once the game is loaded
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseCanvas.activeSelf)
            {
                Cursor.visible = false;
                pauseCanvas.SetActive(false);
                isPause = false;
            }
            else
            {
                Cursor.visible = true;
                pauseCanvas.SetActive(true);
                isPause = true;
            }

        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!chatManagerCS.isConnected)
            {
                RememberMe.Instance.EnableNotificationCanvas("Chat is not ready yet, try again later", true);
                return;
            }

            if (chatCanvas.activeSelf)
            {
                //Check if we should send message or close the chat canvas
                if (string.IsNullOrEmpty(chatInput.text))
                {
                    //since input is empty lets close the canvas
                    Cursor.visible = false;
                    chatCanvas.SetActive(false);
                    isChatting = false;
                }
                else
                {
                    //Since chatinput content something lets send a chat
                    chatManagerCS.ClickSend();
                    chatInput.Select();
                    chatInput.ActivateInputField();
                }
            }
            else
            {
                Cursor.visible = true;
                chatCanvas.SetActive(true);
                chatInput.Select();
                chatInput.ActivateInputField();
                isChatting = true;
            }

        }
    }



    //onclick of LeaveRoom_Btn from PauseCanvas
    public void ClickLeaveRoomBtn()
    {
        RememberMe.Instance.EnableNotificationCanvas("Leaving Room", false);
        PhotonNetwork.LeaveRoom();
    }


    public override void OnLeftRoom()
    {
        // Always load scene AFTER fully leaving Photon room
        PhotonNetwork.JoinLobby();
        SceneManager.LoadScene("Lobby");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Disconnected from Photon because: {cause}");
        RememberMe.Instance.EnableNotificationCanvas("Cannot Connect to the Network", true);
        SceneManager.LoadScene("Lobby");

    }
}