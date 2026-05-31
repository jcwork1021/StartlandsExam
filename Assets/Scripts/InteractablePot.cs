using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class InteractablePot : MonoBehaviourPun
{
    [Header("UI Elements")]
    [SerializeField] GameObject interactableCanvas;
    [SerializeField] GameObject instructionTxt;
    [SerializeField] GameObject choicesTxt;

    [Header("Reference from its Child")]
    public Renderer vase;

    [Header("Network Sync")]
    [SerializeField] private int interactingActorNumber = -1;

    private bool isLocalPlayerInside = false;

    void Start()
    {
        interactableCanvas.SetActive(false);
        instructionTxt.SetActive(false);
        choicesTxt.SetActive(false);
    }

    void Update()
    {
        // Check for opening the menu
        if (isLocalPlayerInside && interactingActorNumber == -1)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                //Send local ActorNumber (like 1) to lock it for everyone
                int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("RPC_SetInteractedPlayer", RpcTarget.AllBuffered, myActorNumber);
            }
        }

        //If I am the current interacting player, listen for color selection inputs
        if (interactingActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            // Pass integers (1 to 4) instead of Color objects
            if (Input.GetKeyDown(KeyCode.Alpha1)) photonView.RPC("RPC_ChangePotColor", RpcTarget.AllBuffered, 1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) photonView.RPC("RPC_ChangePotColor", RpcTarget.AllBuffered, 2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) photonView.RPC("RPC_ChangePotColor", RpcTarget.AllBuffered, 3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) photonView.RPC("RPC_ChangePotColor", RpcTarget.AllBuffered, 4);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();

        //Only trigger if it's a Player AND it belongs to this local machine instance
        if (other.CompareTag("Player") && pv != null && pv.IsMine)
        {
            isLocalPlayerInside = true;

            if (interactingActorNumber == -1)
            {
                interactableCanvas.SetActive(true);
                instructionTxt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && pv != null && pv.IsMine)
        {
            isLocalPlayerInside = false;

            interactableCanvas.SetActive(false);
            instructionTxt.SetActive(false);
            choicesTxt.SetActive(false);

            // If I step away while occupying it, unlock it
            if (interactingActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                photonView.RPC("RPC_SetInteractedPlayer", RpcTarget.AllBuffered, -1);
            }
        }
    }

    [PunRPC]
    void RPC_SetInteractedPlayer(int actorNumber)
    {
        interactingActorNumber = actorNumber;

        if (interactingActorNumber == -1)
        {
            // Pot is freed up
            choicesTxt.SetActive(false);
            if (isLocalPlayerInside)
            {
                interactableCanvas.SetActive(true);
                instructionTxt.SetActive(true);
            }
        }
        else
        {
            // Pot is occupied
            if (interactingActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                // Locally enable choices for the player interacting (Actor 1)
                instructionTxt.SetActive(false);
                choicesTxt.SetActive(true);
            }
            else
            {
                // Hide prompt for everyone else sitting inside the radius
                if (isLocalPlayerInside)
                {
                    interactableCanvas.SetActive(false);
                }
            }
        }
    }

    [PunRPC]
    void RPC_ChangePotColor(int colorIndex)
    {
        if (vase != null)
        {
            Color targetColor = Color.white; // Default fallback

            // Translate the simple integer back into a Unity color locally on every machine
            switch (colorIndex)
            {
                case 1: targetColor = Color.blue; break;
                case 2: targetColor = Color.green; break;
                case 3: targetColor = Color.red; break;
                case 4: targetColor = Color.yellow; break;
            }

            vase.material.color = targetColor;
        }

        // Kick the player out of interaction mode automatically after picking a color
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetInteractedPlayer", RpcTarget.AllBuffered, -1);
        }
    }
}