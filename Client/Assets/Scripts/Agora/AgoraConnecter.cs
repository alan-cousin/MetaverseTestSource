using UnityEngine;
using UnityEngine.UI;

using agora_gaming_rtc;
using agora_utilities;

public class AgoraConnecter
{

    // instance of agora engine
    private IRtcEngine mRtcEngine;
    private Text MessageText;
    private string tokenKey = "00608e973a20c344b08a34d36b83597f223IAAxdAp0u4x/pU/sVETn4gIywTfGx8b8QB7ORgO2HDsXnDtY++cAAAAAEACjPQT83j6UYgEAAQDvPZRi";
    // load agora engine

    public void LoadEngine(string appId)
    {
        // start sdk
        Debug.Log("initializeEngine");

        if (mRtcEngine != null)
        {
            Debug.Log("Engine exists. Please unload it first!");
            return;
        }

        // init engine
        mRtcEngine = IRtcEngine.GetEngine(appId);

        // enable log
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
    }
    public void SetAgoraToken(string token)
    {
        tokenKey = token;
    }

    public void Join(string channel, bool enableVideoOrNot, bool muted = false, bool isHost = false)
    {
        Debug.Log("calling join (channel = " + channel + ")");

        if (mRtcEngine == null)
            return;

        Debug.Log("loaded mRtcEngine");
        // set callbacks (optional)
        mRtcEngine.OnJoinChannelSuccess =OnJoinChannelSuccess;
        mRtcEngine.OnUserJoined = OnUserJoined;
        mRtcEngine.OnUserOffline = OnUserOffline;
        mRtcEngine.OnLeaveChannel += OnLeaveChannelHandler;
        mRtcEngine.OnWarning = (int warn, string msg) =>
        {
            Debug.LogWarningFormat("Warning code:{0} msg:{1}", warn, IRtcEngine.GetErrorDescription(warn));
        };
        mRtcEngine.OnError = HandleError;

        // enable video
        if (enableVideoOrNot)
        {
            mRtcEngine.EnableVideo();
            // allow camera output callback
            mRtcEngine.EnableVideoObserver();
        }
        //mRtcEngine.SetClientRole(isHost ? CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER : CLIENT_ROLE_TYPE.CLIENT_ROLE_AUDIENCE);
        var _orientationMode = ORIENTATION_MODE.ORIENTATION_MODE_FIXED_LANDSCAPE;
        if (muted)
        {
            mRtcEngine.DisableAudio();
            mRtcEngine.EnableLocalAudio(false);
            mRtcEngine.MuteLocalAudioStream(true);
        }

        VideoEncoderConfiguration config = new VideoEncoderConfiguration
        {
            orientationMode = _orientationMode,
            degradationPreference = DEGRADATION_PREFERENCE.MAINTAIN_FRAMERATE,
            mirrorMode = VIDEO_MIRROR_MODE_TYPE.VIDEO_MIRROR_MODE_DISABLED
            // note: mirrorMode is not effective for WebGL
        };
#if !UNITY_EDITOR && UNITY_WEBGL
        mRtcEngine.SetCloudProxy(CLOUD_PROXY_TYPE.UDP_PROXY);
#else
        mRtcEngine.SetCloudProxy(CLOUD_PROXY_TYPE.NONE_PROXY);
        mRtcEngine.SetCloudProxy(CLOUD_PROXY_TYPE.UDP_PROXY);

#endif

        mRtcEngine.SetVideoEncoderConfiguration(config);
        // join channel
        mRtcEngine.JoinChannelByKey(tokenKey, channel);
        //mRtcEngine.JoinChannel(channel, null, 0);

    }
    
    void OnLeaveChannelHandler(RtcStats stats)
    {
        Debug.Log("OnLeaveChannelSuccess ---- TEST");
    }

    public string GetSdkVersion()
    {
        string ver = IRtcEngine.GetSdkVersion();
        return ver;
    }

    public void Leave()
    {
        mRtcEngine.SetCloudProxy(CLOUD_PROXY_TYPE.NONE_PROXY);
        Debug.Log("calling leave");

        if (mRtcEngine == null)
            return;

        // leave channel
        mRtcEngine.LeaveChannel();
        // deregister video frame observers in native-c code
        mRtcEngine.DisableVideoObserver();
    }

    // unload agora engine
    public void UnloadEngine()
    {
        Debug.Log("calling unloadEngine");

        // delete
        if (mRtcEngine != null)
        {
            IRtcEngine.Destroy();  // Place this call in ApplicationQuit
            mRtcEngine = null;
        }
    }

    public void EnableVideo(bool pauseVideo)
    {
        if (mRtcEngine != null)
        {
            if (!pauseVideo)
            {
                mRtcEngine.EnableVideo();
            }
            else
            {
                mRtcEngine.DisableVideo();
            }
        }
    }

    // accessing GameObject in Scnene1
    // set video transform delegate for statically created GameObject
    public void onSceneHelloVideoLoaded()
    {
        // Attach the SDK Script VideoSurface for video rendering
        GameObject quad = GameObject.Find("Quad");
        if (ReferenceEquals(quad, null))
        {
            Debug.Log("failed to find Quad");
            return;
        }
        else
        {
            quad.AddComponent<VideoSurface>();
        }

        GameObject cube = GameObject.Find("Cube");
        if (ReferenceEquals(cube, null))
        {
            Debug.Log("failed to find Cube");
            return;
        }
        else
        {
            cube.AddComponent<VideoSurface>();
        }

        GameObject text = GameObject.Find("MessageText");
        if (!ReferenceEquals(text, null))
        {
            MessageText = text.GetComponent<Text>();
        }
    }

    // implement engine callbacks
    private void OnJoinChannelSuccess(string channelName, uint uid, int elapsed)
    {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);
        GameObject textVersionGameObject = GameObject.Find("VersionText");
        textVersionGameObject.GetComponent<Text>().text = "SDK Version : " + GetSdkVersion();
    }

    // When a remote user joined, this delegate will be called. Typically
    // create a GameObject to render video on it
    private void OnUserJoined(uint uid, int elapsed)
    {
        Debug.Log("onUserJoined: uid = " + uid + " elapsed = " + elapsed);
        // this is called in main thread

        // find a game object to render video stream from 'uid'
        GameObject go = GameObject.Find(uid.ToString());
        Debug.Log("makeImageSurface call");
        if (!ReferenceEquals(go, null))
        {
            Debug.Log("go name=" + go.name);
            return; // reuse
        }
        // create a GameObject and assign to this new user
        VideoSurface videoSurface = MakeImageSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
        }
    }

    public VideoSurface MakePlaneSurface(string goName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

        if (go == null)
        {
            return null;
        }
        go.name = goName;
        // set up transform
        go.transform.Rotate(-90.0f, 0.0f, 0.0f);
        float yPos = Random.Range(3.0f, 5.0f);
        float xPos = Random.Range(-2.0f, 2.0f);
        go.transform.position = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }

    private const float Offset = 100;
    public VideoSurface MakeImageSurface(string goName)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;

        // to be renderered onto
        go.AddComponent<RawImage>();

        // make the object draggable
        go.AddComponent<UIElementDragger>();
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            go.transform.parent = canvas.transform;
        }
        // set up transform
        go.transform.Rotate(0f, 0.0f, 180.0f);
        float xPos = Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
        float yPos = Random.Range(Offset, Screen.height / 2f - Offset);
        go.transform.localPosition = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(3*1.6666f, 3f, 1f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
    // When remote user is offline, this delegate will be called. Typically
    // delete the GameObject for this user
    private void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        // remove video stream
        Debug.Log("onUserOffline: uid = " + uid + " reason = " + reason);
        // this is called in main thread
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            Object.Destroy(go);
        }
    }

    #region Error Handling
    private int LastError { get; set; }
    private void HandleError(int error, string msg)
    {
        if (error == LastError)
        {
            return;
        }

        msg = string.Format("Error code:{0} msg:{1}", error, IRtcEngine.GetErrorDescription(error));

        switch (error)
        {
            case 101:
                msg += "\nPlease make sure your AppId is valid and it does not require a certificate for this demo.";
                break;
        }

        Debug.LogError(msg);
        if (MessageText != null)
        {
            if (MessageText.text.Length > 0)
            {
                msg = "\n" + msg;
            }
            MessageText.text += msg;
        }

        LastError = error;
    }

    #endregion
}
