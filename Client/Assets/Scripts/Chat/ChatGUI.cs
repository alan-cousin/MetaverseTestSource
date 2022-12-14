using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Colyseus;
using System.Collections.Generic;
using Invector.vCharacterController;
using UnityEngine.EventSystems;

public class ChatGUI : MonoBehaviour
{
    public Text messagesText;
    public InputField messageInput;
    

    public GameObject chatPanel;
    public GameObject character;
    public GameObject cameraObj;

    public bool debug = true;
    //Room chatRoom;

    private ArrayList _chatRecords;
    private float keyDelayTime = 0;

    [Header("Camera Input")]
    public string rotateCameraXInput = "Mouse X";
    public string rotateCameraYInput = "Mouse Y";

    public static ChatGUI Instance { get; set; }
    void Awake()
    {
        chatPanel.SetActive(false);
        _chatRecords = new ArrayList();
        Instance = this;
    }

    //When quit, release resource
    void Update()
    {
        if ((Input.GetKey(KeyCode.End) || Input.GetKey(KeyCode.Return)) && keyDelayTime >= 0.2f)
        {
            OnSendMessageClick();
            keyDelayTime = 0;
        }
        if (Input.GetKey(KeyCode.Escape) && uBuildManager.Instance.textChatPlaying)
        {
            OnExitClick();
        }

        var Y = Input.GetAxis(rotateCameraYInput);
        var X = Input.GetAxis(rotateCameraXInput);

        cameraObj.GetComponent<vThirdPersonCamera>().RotateCamera(X, Y);

        UpdateChatWindow();
    }

    private void FixedUpdate()
    {
        if (keyDelayTime > 10) keyDelayTime = 0;
        keyDelayTime += Time.deltaTime;    
    }

    void UpdateChatWindow()
    {
        if (_chatRecords == null)
            return;

        if (_chatRecords.Count > 100)
            _chatRecords.RemoveRange(0, _chatRecords.Count - 50);

        string messages = "";
        if (_chatRecords != null)
        {
            foreach (ChatRecord cr in _chatRecords)
            {
                messages += cr.name + ": " + cr.dialog + "\n";
            }
        }
        messagesText.text = messages;
    }
    //start text chatting
    public void OnStartChat()
    {
        if (!chatPanel.activeSelf)
        {
            chatPanel.SetActive(true);
           
            uBuildManager.Instance.textChatPlaying = true;
            NftManager.Instance.textChatPlaying = true;

            // set furniture, nftcollection list hiden
            if (uBuildManager.buildMode)
            {
                uBuildManager.buildMode = false;
                uBuildManager.Instance.changeCamera(true);
                GameObject.Find("FurnitureList").GetComponent<Animator>().SetTrigger("Slide out");
            }

            if (NftManager.Instance.nftBuildMode)
            {
                NftManager.Instance.nftBuildMode = false;
                uBuildManager.Instance.changeCamera(true);
                GameObject.Find("NFTCollectionList").GetComponent<Animator>().SetTrigger("Slide out");
            }

            character.GetComponent<vThirdPersonInput>().enabled = false;
            character.GetComponent<vThirdPersonController>().enabled = false;
            character.GetComponent<Rigidbody>().velocity = Vector3.zero;
            character.GetComponent<Animator>().SetFloat("InputMagnitude", 0);
        }
        else
        {
            character.GetComponent<vThirdPersonInput>().enabled = true;
            character.GetComponent<vThirdPersonController>().enabled = true;
            OnExitClick();
        }
    }

    public void OnSendMessageClick()
    {
        SendMessage(messageInput.text);
        messageInput.text = "";
        EventSystem.current.SetSelectedGameObject(messageInput.gameObject, null);
        messageInput.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    void SendMessage(string message)
    {
        ChatRecord me = new ChatRecord(ExampleManager.Instance.CurrentUser.id, message);
        _chatRecords.Add(me);
        Dictionary<string, string> msgData = new Dictionary<string, string>();
        msgData.Add("userName", ExampleManager.Instance.CurrentUser.id);
        msgData.Add("message", message);

        ExampleManager.SendChatMsg(msgData);
    }

    public void Receive(string user, string message)
    {
        if (!chatPanel.activeSelf) OnStartChat();
        ChatRecord other = new ChatRecord(user, message);
        _chatRecords.Add(other);
    }
    public void OnExitClick()
    {
        uBuildManager.Instance.textChatPlaying = false;
       
        character.GetComponent<vThirdPersonInput>().enabled = true;
        character.GetComponent<vThirdPersonController>().enabled = true;

        chatPanel.SetActive(false);
    }


}