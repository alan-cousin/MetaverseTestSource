using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using Colyseus.Schema;
using LucidSightTools;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GalleryGameManager : MonoBehaviour
{
    public GameObject welcomeModal;
    public GameObject settingMenu;

    //============================
    [SerializeField]
    public PlayerController prefab;

    public GameObject[] Rooms;
    public Cubemap[] cubemaps;
   
    public bool playable = false;

    public static GalleryGameManager Instance { get; private set; }

    //============================
    public float skyTime = 0;

    private void Awake()
    {
        GameObject.Find("PlayerMe").transform.localScale = Vector3.one;

        GameObject curRoom = Instantiate(Rooms[PlayerPrefs.GetInt("RoomType")]) as GameObject;

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

    }

    bool showOnce = false;
    private void FixedUpdate()
    {
        if (showOnce) return;
        if (skyTime > 1)
        {
            showOnce = true;
            skyTime = 0;
        }
        RenderSettings.skybox.SetTexture("_Tex", cubemaps[PlayerPrefs.GetInt("RoomType")]);
        skyTime += Time.deltaTime;
    }


    private IEnumerator Start()
    {
        while (ExampleManager.Instance.IsInRoom == false)
        {
            yield return 0;
        }
    }

    //Subscribe to messages that will be sent from the server
    private void OnEnable()
    {
        ExampleRoomController.onAddNetworkEntity += OnNetworkAdd;
        ExampleRoomController.onRemoveNetworkEntity += OnNetworkRemove;
    }

    //Unsubscribe
    private void OnDisable()
    {
        ExampleRoomController.onAddNetworkEntity -= OnNetworkAdd;
        ExampleRoomController.onRemoveNetworkEntity -= OnNetworkRemove;
    }

    private void OnNetworkAdd(ExampleNetworkedEntity entity)
    {
        if (ExampleManager.Instance.HasEntityView(entity.id))
        {
            LSLog.LogImportant("View found! For " + entity.id + ", ownerId:" + entity.ownerId + ", creationId" + entity.creationId);
        }
        else
        {
            LSLog.LogImportant("No View found for " + entity.id + ", ownerId:" + entity.ownerId + ", creationId" + entity.creationId);
            StartCoroutine(CreateView(entity));
        }
    }

    private void OnNetworkRemove(ExampleNetworkedEntity entity, ColyseusNetworkedEntityView view)
    {
        RemoveView(view);
    }

    IEnumerator CreateView(ExampleNetworkedEntity entity)
    {
        PlayerController newView = Instantiate(prefab, new Vector3(0, 0.52f, 0), Quaternion.identity);
        newView.GetComponent<PlayerController>().isCloned = true;
        while (!ExampleManager.Instance.userAvataUrls.ContainsKey(entity.ownerId))
        {
            yield return new WaitForSeconds(0.1f);
        }
        newView.GetComponent<ReadyPlayerMe.RuntimeTest>().GetMainPlayer(ExampleManager.Instance.userAvataUrls[entity.ownerId]);
        ExampleManager.Instance.RegisterNetworkedEntityView(entity, newView);
        newView.gameObject.SetActive(true);
        yield break;
    }

    private void RemoveView(ColyseusNetworkedEntityView view)
    {
        view.SendMessage("OnEntityRemoved", SendMessageOptions.DontRequireReceiver);
    }

    public PlayerController GetPlayerView(string entityID)
    {
        if (ExampleManager.Instance.HasEntityView(entityID))
        {
            return ExampleManager.Instance.GetEntityView(entityID) as PlayerController;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDestroy()
    {
        ExampleManager.Instance.OnEditorQuit();
    }
#endif

    public void OnClickSetting()
    {
        settingMenu.SetActive(!settingMenu.activeSelf);
    }

    public void OnClickWelcomeModalOk()
    {
        welcomeModal.SetActive(false);
        ExampleManager.RequestAgoraTokenBuild();
    }

    public void OnClickHelp()
    {
        welcomeModal.SetActive(true);
        settingMenu.SetActive(false);
    }

    public void OnClickExit()
    {
        if (ExampleManager.Instance.IsInRoom)
        {
            //Find playerController for this player
            PlayerController pc = GetPlayerView(ExampleManager.Instance.CurrentNetworkedEntity.id);
            if (pc != null)
            {
                pc.enabled = false; //Stop all the messages and updates
            }

            ExampleManager.Instance.LeaveAllRooms(() => { SceneManager.LoadScene("StartScene"); });
        }

        settingMenu.SetActive(false);
    }
}