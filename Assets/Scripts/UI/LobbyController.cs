using System;
using System.Collections;
using System.Collections.Generic;
using Colyseus;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime.InteropServices;

public class LobbyController : MonoBehaviour
{

    public GameObject connectingCover = null;
    public GameObject loadingCover;

    public GameObject roomPanel;
    public string NextScene;
    public string myRoomId;
    public bool isNft;
    public int minRequiredPlayers = 2;
    public int numberOfTargetRows = 5;

    //Variables to initialize the room controller
    public string roomName;

    private Coroutine _refreshRoutine;
    private float _refreshTimer = 1.0f;

    private ColyseusRoomAvailable[] myRooms;
    //private RoomSelectionMenu selectRoomMenu = null;
    private string avatarUrl;
    public string walletAddress;


#if !UNITY_EDITOR && UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void browserAlert(string msg);

#endif

    private void Awake()
    {
        Instance = this;
    }

    public static LobbyController Instance { get; private set; }
  
    private IEnumerator Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        while (!ExampleManager.IsReady)
        {
            yield return new WaitForEndOfFrame();
        }

        Dictionary<string, object> roomOptions = new Dictionary<string, object>
        {
            ["logic"] = "pirate", //The name of our custom logic file
            ["minReqPlayers"] = minRequiredPlayers.ToString(),
            ["numberOfTargetRows"] = numberOfTargetRows.ToString()
        };

        //walletObject = GameObject.Find("dragonWeb3Locker");
        connectingCover.SetActive(false);

        ExampleManager.Instance.Initialize(roomName, roomOptions);
        ExampleManager.onRoomsReceived += OnRoomsReceived;

        PlayerPrefs.SetString("PlayerUrl", "");
        PlayerPrefs.SetInt("loading", 0);
    }

    public void CreateAvatar()
    {
        Application.OpenURL("https://readyplayer.me/avatar");
    }

    public void GetMyAvatar()
    {
        avatarUrl = GameObject.Find("AvatarUrl").GetComponent<TMP_InputField>().text;
        if (avatarUrl == "")
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            browserAlert("Please input avatar Url.");
#endif
            Debug.Log("Please input avatar Url");
        } else
        {
            GameObject.Find("ReadyMeAvatarController").GetComponent<ReadyPlayerMe.RuntimeTest>().GetMainPlayer(avatarUrl);
            loadingCover.SetActive(true);
        }
    }

    public void OnGotoRoomPlanel()
    {
        GameObject readyPlayerAvatar = GameObject.Find("PlayerMe");
        if (readyPlayerAvatar.transform.childCount == 0)
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            browserAlert("Please download your ReadyPlayer avatar.");
#endif
        }
        else
        {
            readyPlayerAvatar.transform.localScale = Vector3.zero;
            GameObject.Find("AvatarPanel").SetActive(false);
            roomPanel.SetActive(true);
        }
    }

    public async void EnterRoom(int index)
    {
        if (walletAddress == "")
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            browserAlert("Please connect your wallet.");
#endif
            return;
        }
        if (PlayerPrefs.GetInt("loading") == 0) return;
        GameObject.Find("RoomPanel").SetActive(false);
        Destroy(GameObject.Find("ReadyMeAvatarController"));

        switch (index)
        {
            case 0:
                myRoomId = "MansionRoom";
                break;
            case 1:
                myRoomId = "ApartmentRoom";
                break;
            case 2:
                myRoomId = "CondoRoom";
                break;
            case 3:
                myRoomId = "ShipRoom";
                break;
        }

        PlayerPrefs.SetInt("RoomType", index);

        PlayerPrefs.SetString("PlayerUrl", avatarUrl);

        bool isExist = false;
        if(myRooms != null)
            for(int i = 0; i < myRooms.Length; i++)
            {
                if(myRooms[i].roomId == myRoomId)
                {
                    isExist = true;
                    break;
                }
            }
       
        if (!isExist)
        {
            GetComponent<LobbyController>().CreateRoom(myRoomId);
            PlayerPrefs.SetInt("network", 1);   //create
        }
        else
        {
            GetComponent<LobbyController>().JoinRoom(myRoomId);
            PlayerPrefs.SetInt("network", 2);   //join
        }

        if (isNft == true) PlayerPrefs.SetInt("NftState", 1);
        else PlayerPrefs.SetInt("NftState", 0);
    }

    public void UserLogin(string walletAddress, string signedMsg)
    {
        ExampleManager.Instance.UserName = walletAddress;
        ExampleManager.Instance.UserLogin(walletAddress, signedMsg);
    }

    public void CreateRoom(string id)
    {
        connectingCover.SetActive(true);
        LoadGallery(() => { ExampleManager.Instance.CreateNewRoom(id); });
    }

    public void JoinOrCreateRoom()
    {
        connectingCover.SetActive(true);
        string desiredRoomName = myRoomId;
        LoadGallery(() => { ExampleManager.Instance.JoinOrCreateNewRoom(desiredRoomName); });
    }

    public void JoinRoom(string id)
    {
        connectingCover.SetActive(true);
        LoadGallery(() => { ExampleManager.Instance.JoinExistingRoom(id); });
    }

    public void OnConnectedToServer()
    {
        connectingCover.SetActive(false);
    }

    private void LoadGallery(Action onComplete)
    {
        connectingCover.SetActive(true);
        StartCoroutine(LoadSceneAsync("GameRoom", onComplete));
    }

    private IEnumerator LoadSceneAsync(string scene, Action onComplete)
    {
        Scene currScene = SceneManager.GetActiveScene();
        AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        while (op.progress <= 0.9f)
        {
            //Wait until the scene is loaded
            yield return new WaitForEndOfFrame();
        }

        onComplete.Invoke();
        op.allowSceneActivation = true;
        SceneManager.UnloadSceneAsync(currScene);
    }

    public void OnRoomsReceived(ColyseusRoomAvailable[] rooms)
    {
        if (_refreshRoutine == null)
        {
            _refreshRoutine = StartCoroutine(RefreshRoutine());
        }
        
        Debug.Log("roomCount: " + rooms.Length);
        myRooms = rooms;
    }

    private IEnumerator RefreshRoutine()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(_refreshTimer);

            GetAvailableRooms();
        }
    }
    public void GetAvailableRooms()
    {
        ExampleManager.Instance.GetAvailableRooms();
    }

    private void OnDestroy()
    {
        ExampleManager.onRoomsReceived -= OnRoomsReceived;
    }
}