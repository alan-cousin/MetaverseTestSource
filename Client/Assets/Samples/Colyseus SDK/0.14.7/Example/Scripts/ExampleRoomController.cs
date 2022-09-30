using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Colyseus;
using Colyseus.Schema;
using GameDevWare.Serialization;
using LucidSightTools;
using NativeWebSocket;
using UnityEngine;


//Manages the rooms of a server connection.

[Serializable]
public class ExampleRoomController
{
    // Network Events
    //==========================
    
    // OnNetworkEntityAdd delegate for OnNetworkEntityAdd event.
    // <param name="entity">Then entity that was just added to the room.</param>
    public delegate void OnNetworkEntityAdd(ExampleNetworkedEntity entity);

   
    //OnNetworkEntityRemoved delegate for OnNetworkEntityRemoved event.
   
    //<param name="entity">Then entity that was just removed to the room.</param>
    public delegate void OnNetworkEntityRemoved(ExampleNetworkedEntity entity, ColyseusNetworkedEntityView view);

    //Event for when a NetworkEntity is added to the room.
    public static OnNetworkEntityAdd onAddNetworkEntity;

    
    //Event for when a NetworkEntity is added to the room.
    public static OnNetworkEntityRemoved onRemoveNetworkEntity;

    //Our user object we get upon joining a room.
    [SerializeField]
    private static ExampleNetworkedUser _currentNetworkedUser;

  
    //The Client that is created when connecting to the Colyseus server.
    private ColyseusClient _client;

    private ColyseusSettings _colyseusSettings;

  
    // Collection of entity creation callbacks. Callbacks are added to
    // the collection when a <see cref="ExampleNetworkedEntity" /> is created.
    // The callbacks are invoked and removed from the collection once the
    // entity has been added to the room.
    private Dictionary<string, Action<ExampleNetworkedEntity>> _creationCallbacks =
        new Dictionary<string, Action<ExampleNetworkedEntity>>();
    //==========================

   
    // Collection for tracking entities that have been added to the room.

    private IndexedDictionary<string, ExampleNetworkedEntity> _entities =
        new IndexedDictionary<string, ExampleNetworkedEntity>();

    // Collection for tracking entity views that have been added to the room.
    private IndexedDictionary<string, ExampleNetworkedEntityView> _entityViews =
        new IndexedDictionary<string, ExampleNetworkedEntityView>();

    private ExampleNetworkedEntityFactory _factory;

  
    // Used to help calculate the latency of the connection to the server.
    private double _lastPing;

    //  Used to help calculate the latency of the connection to the server.
    private double _lastPong;

   
    //  The ID of the room we were just connected to.
    //  If there is an abnormal disconnect from the current room
    //  an automatic attempt will be made to reconnect to that room
    //  with this room ID.
 
    private string _lastRoomId;

    // Thread responsible for running
    private Thread _pingThread;

    // The current or active Room we get when joining or creating a room.
    private ColyseusRoom<ExampleRoomState> _room;

    // The time as received from the server in milliseconds.
    private double _serverTime = -1;

   
    // Collection for tracking users that have joined the room.
    private IndexedDictionary<string, ExampleNetworkedUser> _users =
        new IndexedDictionary<string, ExampleNetworkedUser>();

    // Used to help calculate the latency of the connection to the server.
    private bool _waitForPong;

    //  The name of the room clients will attempt to create or join on the Colyseus server.
    public string roomName = "NO_ROOM_NAME_PROVIDED";

    private Dictionary<string, object> roomOptionsDictionary = new Dictionary<string, object>();

    // All the connected rooms.
  
    public List<IColyseusRoom> rooms = new List<IColyseusRoom>();

  
    // Returns the synchronized time from the server in milliseconds.

    public double GetServerTime
    {
        get { return _serverTime; }
    }

    
    // Returns the synchronized time from the server in seconds.

    public double GetServerTimeSeconds
    {
        get { return _serverTime / 1000; }
    }

  
    // The latency in milliseconds between client and server.

    public double GetRoundtripTime
    {
        get { return _lastPong - _lastPing; }
    }

    public ColyseusRoom<ExampleRoomState> Room
    {
        get { return _room; }
    }

    public string LastRoomID
    {
        get { return _lastRoomId; }
    }

    public IndexedDictionary<string, ExampleNetworkedEntity> Entities
    {
        get { return _entities; }
    }

    public IndexedDictionary<string, ExampleNetworkedEntityView> EntityViews
    {
        get { return _entityViews; }
    }

    public Dictionary<string, Action<ExampleNetworkedEntity>> CreationCallbacks
    {
        get { return _creationCallbacks; }
    }

    public ExampleNetworkedUser CurrentNetworkedUser
    {
        get { return _currentNetworkedUser; }
    }

    // Checks if a <see cref="ExampleNetworkedEntityView" /> exists for
    // the given ID.
    // <param name="entityId">The ID of the <see cref="ExampleNetworkedEntity" /> we're checking for.</param>
    public bool HasEntityView(string entityId)
    {
        return EntityViews.ContainsKey(entityId);
    }

    /// <summary>
    ///     Returns a <see cref="ExampleNetworkedEntityView" /> given <see cref="entityId" />
    /// </summary>
    /// <param name="entityId"></param>
    /// <returns>
    ///     Returns <see cref="ExampleNetworkedEntityView" /> if one exists for the given <see cref="entityId" />
    /// </returns>
    public ExampleNetworkedEntityView GetEntityView(string entityId)
    {
        
        if (EntityViews.ContainsKey(entityId))
        {
            return EntityViews[entityId];
        }

        return null;
    }

    /// <summary>
    ///     Set the dependencies.
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="settings"></param>
    public void SetDependencies(ColyseusSettings settings)
    {
        _colyseusSettings = settings;

        ColyseusClient.onAddRoom += AddRoom;
    }

    public void SetRoomOptions(Dictionary<string, object> options)
    {
        roomOptionsDictionary = options;
    }

    /// <summary>
    ///     Set the <see cref="NetworkedEntitExampleNetworkedEntityFactoryyFactory" /> of the RoomManager.
    /// </summary>
    /// <param name="factory"></param>
    public void SetNetworkedEntityFactory(ExampleNetworkedEntityFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Set the client of the <see cref="ColyseusRoomManager" />.
    /// </summary>
    /// <param name="client"></param>
    public void SetClient(ColyseusClient client)
    {
        _client = client;
    }

    /// <summary>
    ///     Adds the given room to <see cref="rooms" /> and
    ///     initiates its connection to the server.
    /// </summary>
    /// <param name="roomToAdd"></param>
    /// <returns></returns>
    public void AddRoom(IColyseusRoom roomToAdd)
    {
        Debug.Log("ExampleRoomController.AddRoom");

        roomToAdd.OnLeave += code => { rooms.Remove(roomToAdd); };
        rooms.Add(roomToAdd);
    }

    /// <summary>
    ///     Create a room with the given roomId.
    /// </summary>
    /// <param name="roomId">The ID for the room.</param>
    public async Task CreateSpecificRoom(ColyseusClient client, string roomName, string roomId)
    {
        LSLog.LogImportant($"Creating Room {roomId}");

        try
        {
            //Populate an options dictionary with custom options provided elsewhere as well as the critical option we need here, roomId
            Dictionary<string, object> options = new Dictionary<string, object> {["roomId"] = roomId};
            foreach (KeyValuePair<string, object> option in roomOptionsDictionary)
            {
                options.Add(option.Key, option.Value);
            }
            options.Add("avatarUrl", PlayerPrefs.GetString("PlayerUrl"));
            _room = await client.Create<ExampleRoomState>(roomName, options);
        }
        catch (Exception ex)
        {
            LSLog.LogError($"Failed to create room {roomId} : {ex.Message}");
            return;
        }

        LSLog.LogImportant($"Created Room: {_room.Id}");
        _lastRoomId = roomId;
        RegisterRoomHandlers();
    }
    //join lobby room
    public async void JoinOrCreateRoom()
    {
        try
        {
            LSLog.LogImportant($"Join Or Create Room - Name = {roomName}.... ");
            // Populate an options dictionary with custom options provided elsewhere
            Dictionary<string, object> options = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> option in roomOptionsDictionary)
            {
                
                options.Add(option.Key, option.Value);
            }
            
            _room = await _client.JoinOrCreate<ExampleRoomState>(roomName, options);

            LSLog.LogImportant($"Joined / Created Room: {_room.Id}");
            _lastRoomId = _room.Id;
            RegisterRoomHandlers();
        }
        catch (Exception e)
        {
            LSLog.LogError($"Room Controller Error - {e.Message + e.StackTrace}");
        }
    }
    //when a user leave from net
    public async Task LeaveAllRooms(bool consented, Action onLeave = null)
    {
        if (_room != null && rooms.Contains(_room) == false)
        {
            await _room.Leave(consented);
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            await rooms[i].Leave(consented);
        }

        _entities.Clear();
        _entityViews.Clear();
        _users.Clear();

        
        ClearRoomHandlers();

        onLeave?.Invoke();
    }

    // Subscribes the manager to <see cref="room" />'s networked events
    // and starts measuring latency to the server.

    public virtual void RegisterRoomHandlers()
    {
        LSLog.LogImportant($"sessionId: {_room.SessionId}");

        if (_pingThread != null)
        {
            _pingThread.Abort();
            _pingThread = null;
        }

        _pingThread = new Thread(RunPingThread);
        _pingThread.Start(_room);

        _room.OnLeave += OnLeaveRoom;


        _room.OnMessage<ExampleNetworkedUser>("onJoin", currentNetworkedUser =>
        {
         
            Debug.Log($"Received 'ExampleNetworkedUser' after join/creation call {currentNetworkedUser.id}!");
            Debug.Log(Json.SerializeToString(currentNetworkedUser));

            _currentNetworkedUser = currentNetworkedUser;
        });

        _room.OnMessage<ExampleRFCMessage>("onRFC", _rfc =>
        {
            if (_entityViews.Keys.Contains(_rfc.entityId))
            {
                _entityViews[_rfc.entityId].RemoteFunctionCallHandler(_rfc);
            }
        });

        _room.OnMessage<ExamplePongMessage>(0, message =>
        {
            _lastPong = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _serverTime = message.serverTime;
            _waitForPong = false;
        });

        _room.OnMessage<Dictionary<string, string>>("onChatMsg", msgData =>
        {
            ChatGUI.Instance.Receive(msgData["userName"], msgData["message"]);
        });

        _room.OnMessage<PieceData>("onFurniture", fData => 
        {
            FurnitureManager.Instance.PutFurniture(fData);
        });

        _room.OnMessage<string>("onTokenBuild", token =>
        {
            ChatManager.Instance.SetAgoraToken(token);
        });


        //========================
        Debug.Log($"Adding OnAdd/OnRemove callbacks for all {_room.State.networkedEntities.Count} entities! ***");
        _room.State.networkedEntities.OnAdd += OnEntityAdd;  
        _room.State.networkedEntities.OnRemove += OnEntityRemoved; //"error"

        _room.State.networkedUsers.OnAdd += OnUserAdd;
        _room.State.networkedUsers.OnRemove += OnUserRemove;

        _room.State.TriggerAll();
        //========================

        _room.colyseusConnection.OnError += Room_OnError;
        _room.colyseusConnection.OnClose += Room_OnClose;
    }

    private void ColyseusConnection_OnClose(WebSocketCloseCode closeCode)
    {
        throw new NotImplementedException();
    }

    private void OnLeaveRoom(WebSocketCloseCode code)
    {
        //WebSocketCloseCode closeCode = WebSocketHelpers.ParseCloseCodeEnum(code);
        WebSocketCloseCode closeCode = code;
        LSLog.Log(string.Format("ROOM: ON LEAVE =- Reason: {0} ({1})",closeCode, code));
        _pingThread.Abort();
        _pingThread = null;
        _room = null;

        if (closeCode != WebSocketCloseCode.Normal && !string.IsNullOrEmpty(_lastRoomId))
        {
            JoinRoomId(_lastRoomId);
        }
    }

    // Unsubscribes <see cref="Room" /> from networked events."/>
    private void ClearRoomHandlers()
    {
        if (_pingThread != null)
        {
            _pingThread.Abort();
            _pingThread = null;
        }

        if (_room == null)
        {
            return;
        }

        _room.State.networkedEntities.OnAdd -= OnEntityAdd;
        _room.State.networkedEntities.OnRemove -= OnEntityRemoved;
        _room.State.networkedUsers.OnAdd -= OnUserAdd;
        _room.State.networkedUsers.OnRemove -= OnUserRemove;

        _room.colyseusConnection.OnError -= Room_OnError;
        _room.colyseusConnection.OnClose -= Room_OnClose;


        _room.OnLeave -= OnLeaveRoom;

        _room = null;
        _currentNetworkedUser = null;
    }

    //user get all current exists rooms using this function
    public async Task<ColyseusRoomAvailable[]> GetRoomListAsync()
    {
        ColyseusRoomAvailable[] allRooms = await _client.GetAvailableRooms(roomName);

        return allRooms;
    }

    //user join room with room name(roomId)
    public async Task JoinRoomId(string roomId)
    {
        LSLog.Log($"Joining Room ID {roomId}....");
        ClearRoomHandlers();

        try
        {
            while (_room == null || !_room.colyseusConnection.IsOpen)
            {
                Debug.Log("you aren't in room yet! ");
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("avatarUrl", PlayerPrefs.GetString("PlayerUrl"));
                _room = await _client.JoinById<ExampleRoomState>(roomId, options);
                Debug.Log("IsInRoom: " + _room.Name);

                if (_room == null || !_room.colyseusConnection.IsOpen)
                {
                    LSLog.LogImportant($"Failed to Connect to {roomId}.. Retrying in 5 Seconds...");
                    await Task.Delay(5000);
                }
            }
            LSLog.LogImportant($"Connected to {roomId}..");
            _lastRoomId = roomId;
            RegisterRoomHandlers();
        }
        catch (Exception ex)
        {
            LSLog.LogError(ex.Message);
            LSLog.LogError("Failed to join room");
            //await CreateSpecificRoom(_client, roomName, roomId, onJoin);
        }
    }

    // The callback for the event when a <see cref="ExampleNetworkedEntity" /> is added to a room.
    // <param name="entity">The entity that was just added.</param>
    // <param name="key">The entity's key</param>
   
    private async void OnEntityAdd(string key, ExampleNetworkedEntity entity)
    {
        LSLog.LogImportant(
            $"Entity [{entity.__refId} | {entity.id} | {entity.ownerId}] add: x => {entity.xPos}, y => {entity.yPos}, z => {entity.zPos}");

        _entities.Add(entity.id, entity);

        //Creation ID is only Registered with the owner so only owners callback will be triggered
        if (!string.IsNullOrEmpty(entity.creationId) && _creationCallbacks.ContainsKey(entity.creationId))
        {
            _creationCallbacks[entity.creationId].Invoke(entity);
            _creationCallbacks.Remove(entity.creationId);
        }

        onAddNetworkEntity?.Invoke(entity);

        if (_entityViews.ContainsKey(entity.id) == false && !string.IsNullOrEmpty(entity.attributes["prefab"]))
        {
            await _factory.CreateFromPrefab(entity);
        }
    }

    // The callback for the event when a <see cref="ExampleNetworkedEntity" /> is removed from a room.
    // <param name="entity">The entity that was just removed.</param>
    // <param name="key">The entity's key</param>
    private void OnEntityRemoved(string key, ExampleNetworkedEntity entity)
    {
        if (_entities.ContainsKey(entity.id))
        {
            _entities.Remove(entity.id);
        }

        ColyseusNetworkedEntityView view = null;

        if (_entityViews.ContainsKey(entity.id))
        {
            view = _entityViews[entity.id];
            _entityViews.Remove(entity.id);
        }

        onRemoveNetworkEntity?.Invoke(entity, view);
    }

    // Callback for when a <see cref="ExampleNetworkedUser" /> is added to a room.
    // <param name="user">The user object</param>
    // <param name="key">The user key</param>
    private void OnUserAdd(string key, ExampleNetworkedUser user)
    {
        LSLog.LogImportant($"user [{user.__refId} | {user.id} | key {key}] Joined");

        // Add "player" to map of players
        Debug.Log("OnUserAdd: " + key + " - " + user.avatarUrl);
        _users.Add(key, user);

        ExampleManager.Instance.userAvataUrls.Add(key, user.avatarUrl);

        // On entity update...
        user.OnChange += changes =>
        {
            user.updateHash = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(); 
        };
    }

    // Callback for when a user is removed from a room.
    // <param name="user">The removed user.</param>
    // <param name="key">The user key.</param>
    private void OnUserRemove(string key, ExampleNetworkedUser user)
    {
        LSLog.LogImportant($"user [{user.__refId} | {user.id} | key {key}] Left");

        _users.Remove(key);
    }

    // Callback for when the room's connection closes.
    // <param name="closeCode">Code reason for the connection close.</param>
    private static void Room_OnClose(WebSocketCloseCode closeCode)
    {
        LSLog.LogError("Room_OnClose: " + closeCode);
    }

    //     Callback for when the room get an error.
    // <param name="errorMsg">The error message.</param>
    private static void Room_OnError(string errorMsg)
    {
        LSLog.LogError("Room_OnError: " + errorMsg);
    }

    // Sends "ping" message to current room to help measure latency to the server.
    // <param name="roomToPing">The <see cref="ColyseusRoom{T}" /> to ping.</param>
    private void RunPingThread(object roomToPing)
    {
        ColyseusRoom<ExampleRoomState> currentRoom = (ColyseusRoom<ExampleRoomState>) roomToPing;

        const float pingInterval = 0.5f; // seconds
        const float pingTimeout = 15f; //seconds

        int timeoutMilliseconds = Mathf.FloorToInt(pingTimeout * 1000);
        int intervalMilliseconds = Mathf.FloorToInt(pingInterval * 1000);

        DateTime pingStart;
        while (currentRoom != null)
        {
            _waitForPong = true;
            pingStart = DateTime.Now;
            _lastPing = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _ = currentRoom.Send("ping");

            while (currentRoom != null && _waitForPong &&
                   DateTime.Now.Subtract(pingStart).TotalSeconds < timeoutMilliseconds)
            {
                Thread.Sleep(200);
            }

            if (_waitForPong)
            {
                LSLog.LogError("Ping Timed out");
            }

            Thread.Sleep(intervalMilliseconds);
        }
    }

    //Increments the known <see cref="_serverTime" /> by <see cref="Time.fixedDeltaTime" />
    //converted into milliseconds.

    public void IncrementServerTime()
    {
        _serverTime += Time.fixedDeltaTime * 1000;
    }

    public async void CleanUp()
    {
        _pingThread?.Abort();

        List<Task> leaveRoomTasks = new List<Task>();

        foreach (IColyseusRoom roomEl in rooms)
        {
            leaveRoomTasks.Add(roomEl.Leave(false));
        }

        if (_room != null)
        {
            leaveRoomTasks.Add(_room.Leave(false));
        }

        await Task.WhenAll(leaveRoomTasks.ToArray());
    }
}