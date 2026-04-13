using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{

    private NetworkRunner _runner;

    async void StartGame(GameMode mode)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
   // public LobbyManager LobbyManager;
    public NetworkPrefabRef PlayerPrefabs;


    [Header("join - quit room")]
    public string lobbySceneName = "Lobby";
    private bool _isReturningToLobby ;
    private bool _isProcessingShutdown;
    private bool _callbackRegistered;

    public bool IsInlobby { get; private set; }
    public bool IsStartingLobby { get; private set; }
    public bool IsInRoom { get; private set; }

    private void Awake()
    {
        var spawners = FindObjectsByType<BasicSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (spawners.Length > 1)
        {
            Destroy(gameObject);
            return;
        }


        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        DontDestroyOnLoad(gameObject);

    }
    void RegisterCallback()
    {
         if (_runner != null || _callbackRegistered) return;
         _runner.AddCallbacks(this);
            _callbackRegistered = true;
    }

    public async Task LeaveRoomAndReturnToLobby()
    {
        if (_isReturningToLobby || _isProcessingShutdown) return;
        _isReturningToLobby = true;

        try
        {
            if (_runner != null)
            {
                await _runner.Shutdown();
                Destroy(_runner);
                _runner = null;
            }

            await ReturnToLobbyAndRestart();
        }
        finally
        {
            _isReturningToLobby = false;
            _isProcessingShutdown = false;
            IsInlobby = false;
            IsInRoom = false;
            IsStartingLobby = false;
        }
    }

    public LobbyManager LobbyManager { set; get; }
    private async Task ReturnToLobbyAndRestart()
    {
        if (!string.IsNullOrEmpty(lobbySceneName) && Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            RegisterCallback();
        }

        LobbyManager = FindFirstObjectByType<LobbyManager>();
        if (LobbyManager == null)
        {
            Debug.LogWarning("LobbyManager not found in scene.");
        }

        await StarLobby(); 
    }

    private async Task ReturnToLobbySceneOnly()
    {
        if (string.IsNullOrEmpty(lobbySceneName))
        {
            Debug.LogWarning("Lobby scene name is not set!");
            return;
        }

        // Chỉ load scene nếu nó tồn tại
        if (Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogError($"Scene {lobbySceneName} not found in Build Settings!");
        }

        await Task.CompletedTask;
    }


    public async Task StarLobby()
    {
        if(IsStartingLobby || IsInlobby) return;

        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();

        IsStartingLobby = true;
        _runner.ProvideInput = true;
        RegisterCallback();
        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        if(result.Ok)
        {
            IsInlobby = true;
            IsInRoom = false;
            Debug.Log("Joined Lobby successfully");
        }
        else
        {
            Debug.LogError("Failed to join lobby: " + result.ShutdownReason);
        }

    }

    public async Task StartHost(string sessionName, SceneRef scene)
    {
        if(_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        RegisterCallback();

        IsInlobby = false;
        IsInlobby = true;


        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        if(result.Ok)
        {
            Debug.Log("Host started successfully");
        }
        else
        {
            IsInRoom = false;
            Debug.LogError("Failed to start host: " + result.ShutdownReason);
        }
    }

    public async Task StartClient(string sessionName)
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        RegisterCallback();
        IsInlobby = false;
        IsInRoom = true;
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
        });
        if(result.Ok)
        {
            Debug.Log("Client started successfully");
        }
        else
        {
            Debug.LogError("Failed to start client: " + result.ShutdownReason);
        }
    }

    public PlayerClassInfo LocalPlayerProfile { get; set; }
    public void SetLocalPlayerProfile(PlayerClassInfo profile)
    {
        LocalPlayerProfile = profile;
        Debug.Log($"Local Player Profile set: {profile.Name} - {profile.Class}");
    }



    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new ();
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
       
        Debug.Log("Player Joined: " + player 
                                    + "Client count: " 
                                    + runner.ActivePlayers.ToList().Count);

        if (runner.IsServer)
        {
            var spawnPosition = new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f));
           var playerNetWorkObject = runner.Spawn(PlayerPrefabs, spawnPosition, Quaternion.identity, player);
            _spawnedCharacters.Add(player, playerNetWorkObject);
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
      Debug.Log("Player Left: " + player 
                                    + "Client count: " 
                                    + runner.ActivePlayers.ToList().Count);

        if (_spawnedCharacters.TryGetValue(player, out var networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var inputData = new NetworkinputData();

        inputData.MoveDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        input.Set(inputData);
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    {
        if(_isProcessingShutdown) return;
        _isProcessingShutdown = true;

        IsInlobby = false;
        IsInRoom = false;
        IsStartingLobby = false;

        _spawnedCharacters.Clear();

        if(runner != null)
        {
            runner.RemoveCallbacks(this);
        }
        _callbackRegistered = false;

        if (runner != null)
        {
           var oldRunner = _runner;
            _runner = null;
           
            var oldsceneManager = oldRunner.GetComponent<NetworkSceneManagerDefault>();
            if (oldsceneManager != null)
            {
                Destroy(oldsceneManager);
            }
            if(oldRunner != null)
            {
                Destroy(oldRunner);
            }
        }
        await ReturnToLobbyAndRestart();

         _isProcessingShutdown = false;
        _isReturningToLobby = false;
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("Session List Updated: " + sessionList.Count + " sessions available");

        if (LobbyManager == null)
        {
            LobbyManager = FindFirstObjectByType<LobbyManager>();
        }

        if (LobbyManager != null)
        {
            LobbyManager.UpdateRoomList(sessionList);
        }
        else
        {
            Debug.LogWarning("LobbyManager is null, cannot update room list.");
        }
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}