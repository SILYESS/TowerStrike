using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Towers;
using UnityEngine;

namespace Network
{
    [DefaultExecutionOrder(-10)]
    public class MultiplayerManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static MultiplayerManager Instance;

        public NetworkRunner runner;
        [Header("Prefabs")]
        public NetworkObject playerNetworkPrefab;
        public NetworkObject towerPrefab;
        [Header("UI")]
        public GameObject hudPrefab;
        public string multiplayerScene = "MultiplayerArena";

        // Tower spawn positions
        public Vector3 playerTowerBasePos = new Vector3(-4, 0, 0);
        public Vector3 enemyTowerBasePos = new Vector3(4, 0, 0);
        public Transform levelParent;
        private Transform mainCamera;

        public PlayerRef HostPlayer;
        public PlayerRef ClientPlayer;

        public SearchingOpponentPanel searchingPanel;

        public event Action<string> OnMatchmakingStatus;
        public event Action OnOpponentFound;


        public bool gameInProgress = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async void StartMatchmaking()
        {
            GameManager.instance.gameMode = Mode.Multiplayer;

            // Open UI
            if (searchingPanel != null) searchingPanel.Open();

            UpdateStatus("Initializing Network...");

            // Ensure we have a clean runner
            if (runner != null)
            {
                Destroy(runner);
            }
            // Always try to get existing component just in case, or add new
            runner = gameObject.GetComponent<NetworkRunner>();
            if (runner == null) runner = gameObject.AddComponent<NetworkRunner>();

            runner.AddCallbacks(this);
            runner.ProvideInput = true;

            var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

            UpdateStatus("Searching for Opponent...");

            var result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                // SessionName = "TowerWar_PVP", // REMOVED: Randomized session name for scalability
                SceneManager = sceneManager,
                PlayerCount = 2,

            });

            if (!result.Ok)
            {
                Debug.LogError(result.ShutdownReason);
                UpdateStatus("Connection Failed: " + result.ShutdownReason);
                return;
            }

            // If I am the second player (Client) or if I am Host waiting
            if (runner.IsServer)
            {
                if (runner.SessionInfo.PlayerCount < 2)
                    UpdateStatus("Waiting for Opponent...");
                else
                    StartGameSequence();
            }
            else
            {
                UpdateStatus("Connected! Waiting for Host...");
            }

            EnsureInputManagerExists();
        }

        private void UpdateStatus(string status)
        {
            Debug.Log($"[MultiplayerManager] Status: {status}");
            OnMatchmakingStatus?.Invoke(status);
            if (searchingPanel != null) searchingPanel.UpdateStatus(status);
        }

        private void StartGameSequence()
        {
            gameInProgress = true;
            OnOpponentFound?.Invoke();
            UpdateStatus("Opponent Found! Starting...");
            // Load scene after short delay
            Invoke(nameof(LoadGameplayScene), 2f);
        }

        private void LoadGameplayScene()
        {
            if (runner.IsServer)
                runner.LoadScene(multiplayerScene);
        }


        private void EnsureInputManagerExists()
        {
            if (FindObjectOfType<InputManager>() == null)
            {
                GameObject obj = new GameObject("InputManager");
                obj.AddComponent<InputManager>();
            }
        }


        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[MultiplayerManager] Player Joined: {player} (IsServer: {runner.IsServer})");
            if (runner.IsServer)
            {
                if (player == runner.LocalPlayer) HostPlayer = player;
                else ClientPlayer = player;

                // Spawn Player Object
                if (playerNetworkPrefab != null)
                {
                    // Only spawn if not already spawned
                    if (runner.GetPlayerObject(player) == null)
                    {
                        Debug.Log($"[MultiplayerManager] Spawning Network Object for {player}");
                        NetworkObject playerObj = runner.Spawn(playerNetworkPrefab, Vector3.zero, Quaternion.identity, player);
                        runner.SetPlayerObject(player, playerObj);
                        DontDestroyOnLoad(playerObj);
                    }
                    else
                    {
                        Debug.Log($"[MultiplayerManager] Player {player} already has an object.");
                    }
                }
                else
                {
                    Debug.LogWarning("[MultiplayerManager] PlayerNetworkPrefab not assigned!");
                }

                // Check for game start condition
                if (runner.SessionInfo.PlayerCount == 2)
                {
                    Debug.Log("[MultiplayerManager] 2 Players Connected. Starting Game Sequence.");
                    StartGameSequence();
                }
            }
            // Client side logic if needed
            if (runner.LocalPlayer == player && !runner.IsServer)
            {
                // Client joined
                Debug.Log("[MultiplayerManager] I (Client) have joined the session.");
                OnMatchmakingStatus?.Invoke("Opponent Found! Starting...");
            }

            // Assign authority to existing scene towers (if we are already in the game scene)
            foreach (var tower in FindObjectsOfType<TowerBehavior>())
            {
                if (tower.Object == null) continue;
                if (tower.localType == TowerType.PlayerTower && HostPlayer != PlayerRef.None)
                {
                    tower.Object.AssignInputAuthority(HostPlayer);
                    tower.UpdateVisual();
                }
                else if (tower.localType == TowerType.RedTower && ClientPlayer != PlayerRef.None)
                {
                    tower.Object.AssignInputAuthority(ClientPlayer);
                    tower.UpdateVisual();
                }
                //LevelManager.Instance.PlayGame();
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player Left: {player}");
            // If the opponent left (Active Players < 2 or specific Check)
            if (GameStateManager.Instance != null && !GameStateManager.Instance.GameEnded)
            {
                // Simple logic: if anyone leaves in a 1v1, the other wins.
                // Or check if it was the opponent.
                if (player != runner.LocalPlayer)
                {
                    Debug.Log("Opponent Left! Force Win.");
                    GameStateManager.Instance.ForceWin(runner.LocalPlayer);
                }
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[MultiplayerManager] Shutdown: {shutdownReason}");
            if (gameInProgress)
            {
                gameInProgress = false;
                Debug.Log("[MultiplayerManager] Unexpected Shutdown during game -> VICTORY/Main Menu");

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (MultiplayerGameHUD.Instance != null && shutdownReason != ShutdownReason.Ok)
                {
                    MultiplayerGameHUD.Instance.ShowGameOver(true);
                }
                else
                {
                    // If HUD is missing or reason is Ok (but we were in game?), just go to Menu
                    Debug.Log("Returning to Menu from Shutdown");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                }

                // CRITICAL: Report Win to Tournament if applicable
                if (TournamentManager.Instance != null && TournamentManager.Instance.IsTournamentMatch)
                {
                    TournamentManager.Instance.ReportMatchResult(true);
                }
            }
        }

        public void LeaveSession()
        {
            gameInProgress = false;
            if (runner != null) runner.Shutdown();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log($"[MultiplayerManager] Disconnected from server: {reason}");

            if (gameInProgress)
            {
                gameInProgress = false;
                // If we were in a game and got disconnected, assume Host left/crashed.
                // Show Victory to the client.
                Debug.Log("[MultiplayerManager] Unexpected Disconnect during game -> VICTORY");

                // Unlock cursor just in case
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (MultiplayerGameHUD.Instance != null)
                {
                    MultiplayerGameHUD.Instance.ShowGameOver(true);
                }
                else
                {
                    // Fallback if HUD is gone (shouldn't happen if scene persists)
                    Debug.Log("You Won! (HUD missing) - Returning to Menu");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                }
            }
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            // Instantiate HUD locally on all clients if in multiplayer scene
            if (runner.SceneManager.MainRunnerScene.name == multiplayerScene || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == multiplayerScene)
            {
                gameInProgress = true; // Ensure flag is true when scene loads
                if (hudPrefab != null && FindObjectOfType<MultiplayerGameHUD>() == null)
                    if (hudPrefab != null && FindObjectOfType<MultiplayerGameHUD>() == null)
                    {
                        Instantiate(hudPrefab);
                    }
            }

            if (!runner.IsServer) return;

            // Random Level Spawning Logic
            if (LevelManager.Instance != null &&
                LevelManager.Instance.mapMultiplayer != null &&
                LevelManager.Instance.mapMultiplayer.Count > 0)
            {


                // Pick Random
                int randomMapIndex = UnityEngine.Random.Range(0, LevelManager.Instance.mapMultiplayer.Count);
                GameObject mapPrefab = LevelManager.Instance.mapMultiplayer[randomMapIndex];

                if (mapPrefab != null)
                {
                    Debug.Log($"[MultiplayerManager] Spawning Map: {mapPrefab.name}");
                    var mapObj = runner.Spawn(mapPrefab, mapPrefab.transform.position, mapPrefab.transform.rotation);
                    if (levelParent != null)
                    {
                        mapObj.transform.SetParent(levelParent);
                        mapObj.transform.localPosition = Vector3.zero;
                        mapObj.transform.localRotation = Quaternion.identity;
                    }
                }
            }

            // Ensure all players have an object
            foreach (var player in runner.ActivePlayers)
            {
                if (runner.GetPlayerObject(player) == null && playerNetworkPrefab != null)
                {
                    NetworkObject playerObj = runner.Spawn(playerNetworkPrefab, Vector3.zero, Quaternion.identity, player);
                    runner.SetPlayerObject(player, playerObj);
                    DontDestroyOnLoad(playerObj);
                }
            }

            foreach (var tower in FindObjectsOfType<TowerBehavior>())
            {
                if (tower.Object == null) continue;
                if (tower.localType == TowerType.PlayerTower && MultiplayerManager.Instance.HostPlayer != PlayerRef.None)
                {
                    tower.Object.AssignInputAuthority(HostPlayer);
                    tower.UpdateVisual();
                }
                else if (tower.localType == TowerType.RedTower && MultiplayerManager.Instance.ClientPlayer != PlayerRef.None)
                {
                    tower.Object.AssignInputAuthority(ClientPlayer);
                    tower.UpdateVisual();
                }
            }
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

    }
}
