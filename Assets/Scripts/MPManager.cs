using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Cinemachine;
using UnityEngine.SceneManagement;

public class MPManager : MonoBehaviourPunCallbacks
{
    public static MPManager Instance;

    [Header("Reference from Its Child")]
    public GameObject mpCamera;
    public CinemachineFreeLook freeLook;
    public GameObject pauseCanvas;

    [Header("Reference from Project")]
    [SerializeField] private GameObject malePlayerPrefab;
    [SerializeField] private GameObject femalePlayerPrefab;

    [Header("Fill via Script")]
    public GameObject localPlayer;
    public ThirdPersonController tpc;

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
        mpCamera.gameObject.SetActive(false); //Only Enable Locally once the game is loaded
        pauseCanvas.SetActive(false);

        // Only Master Client loads the additive scene for everyone
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_LoadAdditiveScene", RpcTarget.AllBuffered, RememberMe.Instance.playerMapName);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
            pauseCanvas.SetActive(true);
        }
    }

    [PunRPC]
    private void RPC_LoadAdditiveScene(string sceneName)
    {
        StartCoroutine(LoadAdditiveSceneAndSpawn(sceneName));
    }

    private System.Collections.IEnumerator LoadAdditiveSceneAndSpawn(string sceneName)
    {
        // Load the scene additively
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
            sceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Additive
        );

        // Wait until the scene is fully loaded
        yield return new WaitUntil(() => asyncLoad.isDone);

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        // Look for a GameObject tagged "SpawnPoint"
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
}