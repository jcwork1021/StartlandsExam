using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("Child of Canvas")]
    public GameObject infoRoomPanel;
    public GameObject selectionPanel;
    public GameObject joinRoomPanel;
    public GameObject createRoomPanel;

    [Header("Child of InfoRoom_Panel")]
    public TMP_InputField nameInput;
    public Button maleBtn;
    public Button femaleBtn;

    [Header("Child of CreateRoom_Panel")]
    public TMP_Dropdown mapDropdown;
    public RawImage mapRawImg;

    [Header("Child of JoinRoom_Panel")]
    public Transform roomListContentPanel;

    [Header("Reference from Heirarchy")]
    public GameObject fBot;
    public GameObject mBot;

    [Header("Reference from Project")]
    public Texture2D dustI;
    public Texture2D dungeon;

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
        infoRoomPanel.SetActive(true);
        selectionPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        createRoomPanel.SetActive(false);

        RememberMe.Instance.notificationCanvas.SetActive(false); //Disable cause it is open if we are from Game scene
        RememberMe.Instance.travelCamera.SetActive(true);
        

        ClickGender("male"); //Make sure that the default gender is male
    }


    //onclick of M_Btn and F_Btn from Info_Panel
    public void ClickGender(string selectedGender)
    {
        maleBtn.GetComponent<Image>().color = Color.white;
        femaleBtn.GetComponent<Image>().color = Color.white;

        mBot.SetActive(false);
        fBot.SetActive(false);


        if (selectedGender == "male")
        {
            maleBtn.GetComponent<Image>().color = Color.green;
            mBot.SetActive(true);

            RememberMe.Instance.playerGender = "male";
        }
        else
        {
            femaleBtn.GetComponent<Image>().color = Color.green;
            fBot.SetActive(true);

            RememberMe.Instance.playerGender = "female";
        }

    }

    //Attached to OnValueChange() of MapDropdown from CreateRoom_Panel
    public void MapDropdownChangeValue()
    {
        if(mapDropdown.options[mapDropdown.value].text == "DUNGEON")
        {
            mapRawImg.texture = dungeon;
        }
        else
        {
            mapRawImg.texture = dustI;
        }
    }

    //onclick of Enter_Btn from Info_Panel
    public void ClickEnter()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text))
        {
            RememberMe.Instance.EnableNotificationCanvas("Please Enter a Valid Name", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(RememberMe.Instance.playerGender))
        {
            RememberMe.Instance.EnableNotificationCanvas("Choose your Gender", true);
            return;
        }

        RememberMe.Instance.playerName = nameInput.text;

        infoRoomPanel.SetActive(false);
        selectionPanel.SetActive(true);
    }

    //onclick of CreateRoom_Btn from Selection_Panel
    public void ClickCreateRoom()
    {
        selectionPanel.SetActive(false);
        createRoomPanel.SetActive(true);
    }

    //onclick of JoinRoom_Btn from Selection_Panel
    public void ClickJoinRoom()
    {
        selectionPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
    }


    //onclick of BackBtn from JoinRoom_Panel and CreateRoom_Panel
    public void ClickBack()
    {
        selectionPanel.SetActive(true);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
    }

    //onclick of CreateRoom_Btn from CreateRoom_Panel
    public void ClickCreateRoomBtn()
    {
        if (!NetworkManager.Instance.isConnected)
        {
            RememberMe.Instance.EnableNotificationCanvas("Not Connected to server yet, Try again later", true);
            return;
        }

        RememberMe.Instance.EnableNotificationCanvas("Preparing Scene", false);
        RememberMe.Instance.playerMapName = mapDropdown.options[mapDropdown.value].text;
        NetworkManager.Instance.ClickCreateRoom();
    }

}