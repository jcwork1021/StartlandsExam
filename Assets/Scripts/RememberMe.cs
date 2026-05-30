using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RememberMe : MonoBehaviour
{

    public static RememberMe Instance;

    [Header("Reference from its Child")]
    public GameObject notificationCanvas;
    public TMP_Text notificationTxt;
    public GameObject notificationBtn;
    public GameObject travelCamera;

    [Header("Player Data")]
    public string playerName;
    public string playerGender;
    public string playerMapName;
    public string playerRoomNameEntered;

    private void Awake()
    {
        //Check if an instance already exists
        if (Instance == null)
        {
            Instance = this;
            //Keeps this object when changing scenes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destroy duplicate instances
            Destroy(gameObject);
        }

        notificationCanvas.SetActive(false);
    }


    void Start()
    {

    }


    public void EnableNotificationCanvas(string message, bool enableOkayBtn)
    {
        notificationCanvas.SetActive(true);
        notificationTxt.text = message;

        if (enableOkayBtn)
        {
            notificationBtn.SetActive(true);
        }
        else
        {
            notificationBtn.SetActive(false);
        }
    }


    public void ClickOkayBtn()
    {
        notificationCanvas.SetActive(false);
    }


}