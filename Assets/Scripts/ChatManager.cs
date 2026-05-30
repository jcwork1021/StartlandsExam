using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Define the custom message data class
[System.Serializable]
public class ChatMessageData
{
    public string messageText;
    public string playerName;
}


public class ChatManager : MonoBehaviour, IChatClientListener
{
    ChatClient chatClient;

    [Header("From Project")]
    public GameObject othersChatPrefab;
    public GameObject usersChatPrefab;

    [Header("For Debug, Do Not Fill")]
    public string appIdChat;
    public string appVersion;

    public bool isConnected;
    public string messageContain;
    public string receiver;


    void OnEnable()
    {
        appIdChat = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat;
        appVersion = PhotonNetwork.AppVersion;

        JoinChat();
    }

    void OnDisable()
    {
        if (chatClient != null && isConnected)
        {
            chatClient.Disconnect();
        }
    }

    public void ReceiverOnValueChange(string valueIn)
    {
        receiver = valueIn;
    }

    public void ClickSend()
    {
        messageContain = MPManager.Instance.chatInput.text;

        ChatMessageData chatData = new ChatMessageData
        {
            messageText = messageContain,
            playerName = RememberMe.Instance.playerName
        };

        string jsonMessage = JsonUtility.ToJson(chatData);

        chatClient.PublishMessage("WorldChannel", jsonMessage);

        if (MPManager.Instance.tpc != null)
        {
            MPManager.Instance.tpc.DisplayChat(messageContain);
        }
        else
        {
            Debug.LogError("No Local Player in ChatManager: jcGameManagerCS.jcPetMPManagerCS is null.");
        }

        MPManager.Instance.chatInput.text = null;
    }

    public void JoinChat()
    {
        isConnected = true;
        chatClient = new ChatClient(this);
        chatClient.Connect(appIdChat, appVersion, new AuthenticationValues(RememberMe.Instance.playerName));
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        //Debug.Log(message);
    }

    public void OnChatStateChange(ChatState state)
    {
        //Debug.Log("ChatStateChange: " + state);
    }

    public void OnConnected()
    {
        isConnected = true;
        //Debug.Log("Connected");
        chatClient.Subscribe(new string[] { "WorldChannel" });
    }

    public void OnDisconnected()
    {
        isConnected = false;
        Debug.Log("Disconnected");
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            ChatMessageData parsedChatData = ParseChatMessage(messages[i]);

            string messageToDisplay = parsedChatData != null ? parsedChatData.messageText : messages[i].ToString();
            string senderName = parsedChatData != null ? parsedChatData.playerName : null;

            //string msgs = string.Format("{0} : {1}", senders[i], messageToDisplay);

            GameObject ms;

            if (senders[i] == RememberMe.Instance.playerName)
            {
                ms = Instantiate(usersChatPrefab, MPManager.Instance.chatHistoryContent);
                ChatEntry chatEntryCS = ms.GetComponentInChildren<ChatEntry>();
                chatEntryCS.nameTxt.text = senderName;
                chatEntryCS.messageTxt.text = messageToDisplay;
            }
            else
            {
                ms = Instantiate(othersChatPrefab, MPManager.Instance.chatHistoryContent);
                ChatEntry chatEntryCS = ms.GetComponentInChildren<ChatEntry>();
                chatEntryCS.nameTxt.text = senderName;
                chatEntryCS.messageTxt.text = messageToDisplay;
            }
        }
    }

    private ChatMessageData ParseChatMessage(object message)
    {
        string jsonMessage = message as string;

        if (string.IsNullOrEmpty(jsonMessage))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<ChatMessageData>(jsonMessage);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to parse chat message JSON: {jsonMessage} - Error: {ex.Message}");
            return null;
        }
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
        Debug.Log($"Status update for user {user}: status {status}, gotMessage {gotMessage}, message {message}");
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        Debug.Log("Subscribed to a Channel");
    }

    public void OnUnsubscribed(string[] channels)
    {
        Debug.Log("Unsubscribed from channels");
    }

    public void OnUserSubscribed(string channel, string user)
    {
        Debug.Log($"User {user} subscribed to channel {channel}");
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        Debug.Log($"User {user} unsubscribed from channel {channel}");
    }

    void Update()
    {
        if (isConnected)
        {
            chatClient.Service();
        }
    }

    void OnDestroy()
    {
        if (chatClient != null && isConnected)
        {
            chatClient.Disconnect();
        }
    }

    public void DisconnectChat()
    {
        if (chatClient != null && isConnected)
        {
            isConnected = false;
            chatClient.Disconnect();
            Debug.Log("Chat Client Disconnected via DisconnectChat()");
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        throw new System.NotImplementedException();
    }
}