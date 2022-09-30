using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    private AgoraConnecter _videoChat;
    private bool videoPause = false;
    // Start is called before the first frame update
    public static ChatManager Instance { get; set;}

    public string AppID;

    private void Awake()
    {
        _videoChat = new AgoraConnecter(); // create app
        Instance = this;
    }
    private void Start()
    {
        _videoChat.LoadEngine(AppID); // load engine
    }
    public void SetAgoraToken(string token)
    {
        _videoChat.SetAgoraToken(token);
    }

    //start voice chatting
    public void PlayVoiceChat()
    {
        if (videoPause == true)
            _videoChat.EnableVideo(true);
        else
            _videoChat.Join("coke", false, true, true);
    }

    public void StopVoiceChat()
    {
        videoPause = true;
        _videoChat.EnableVideo(false);
    }

}
