using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SmallArena.UI
{
    /// <summary>
    /// UI controller for the lobby scene
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Connection Panel")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private TMP_InputField serverIPInput;
        [SerializeField] private TMP_InputField serverPortInput;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        
        [Header("Lobby Panel")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TMP_Text statusText;
        
        [Header("Settings")]
        [SerializeField] private Network.NetworkConfig networkConfig;
        
        private Dictionary<ulong, GameObject> playerListItems = new Dictionary<ulong, GameObject>();
        private bool isReady = false;
        
        private void Start()
        {
            // Setup connection panel
            if (networkConfig != null)
            {
                serverIPInput.text = networkConfig.serverIP;
                serverPortInput.text = networkConfig.serverPort.ToString();
            }
            
            playerNameInput.text = $"Player{Random.Range(1000, 9999)}";
            
            hostButton.onClick.AddListener(OnHostClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
            readyButton.onClick.AddListener(OnReadyClicked);
            startButton.onClick.AddListener(OnStartClicked);
            leaveButton.onClick.AddListener(OnLeaveClicked);
            
            // Show connection panel initially
            ShowConnectionPanel();
            
            // Subscribe to network events
            Network.GameNetworkManager.Instance.OnClientConnected += OnConnectedToServer;
            Network.GameNetworkManager.Instance.OnClientDisconnected += OnDisconnectedFromServer;
        }
        
        private void OnDestroy()
        {
            if (Network.GameNetworkManager.Instance != null)
            {
                Network.GameNetworkManager.Instance.OnClientConnected -= OnConnectedToServer;
                Network.GameNetworkManager.Instance.OnClientDisconnected -= OnDisconnectedFromServer;
            }
        }
        
        private void OnHostClicked()
        {
            UpdateNetworkConfig();
            
            bool success = Network.GameNetworkManager.Instance.StartHost();
            
            if (success)
            {
                OnConnectedToServer();
            }
            else
            {
                statusText.text = "Failed to start host";
            }
        }
        
        private void OnJoinClicked()
        {
            UpdateNetworkConfig();
            
            bool success = Network.GameNetworkManager.Instance.StartClient();
            
            if (!success)
            {
                statusText.text = "Failed to connect to server";
            }
            else
            {
                statusText.text = "Connecting...";
            }
        }
        
        private void OnReadyClicked()
        {
            isReady = !isReady;
            
            if (Lobby.LobbyManager.Instance != null)
            {
                ulong clientId = NetworkManager.Singleton.LocalClientId;
                Lobby.LobbyManager.Instance.SetPlayerReadyServerRpc(clientId, isReady);
            }
            
            UpdateReadyButton();
        }
        
        private void OnStartClicked()
        {
            if (Lobby.LobbyManager.Instance != null && NetworkManager.Singleton.IsHost)
            {
                Lobby.LobbyManager.Instance.ForceStartMatch();
            }
        }
        
        private void OnLeaveClicked()
        {
            Network.GameNetworkManager.Instance.Shutdown();
            ShowConnectionPanel();
        }
        
        private void UpdateNetworkConfig()
        {
            if (networkConfig != null)
            {
                networkConfig.serverIP = serverIPInput.text;
                
                if (ushort.TryParse(serverPortInput.text, out ushort port))
                {
                    networkConfig.serverPort = port;
                }
            }
        }
        
        private void OnConnectedToServer()
        {
            ShowLobbyPanel();
            statusText.text = "Connected to server";
            
            // Set player name
            if (Lobby.LobbyManager.Instance != null)
            {
                ulong clientId = NetworkManager.Singleton.LocalClientId;
                Lobby.LobbyManager.Instance.SetPlayerNameServerRpc(clientId, playerNameInput.text);
            }
            
            // Subscribe to lobby events
            if (Lobby.LobbyManager.Instance != null)
            {
                Lobby.LobbyManager.Instance.OnPlayerJoinedLobby += OnPlayerJoinedLobby;
                Lobby.LobbyManager.Instance.OnPlayerLeftLobby += OnPlayerLeftLobby;
                Lobby.LobbyManager.Instance.OnPlayerReadyChanged += OnPlayerReadyChanged;
                Lobby.LobbyManager.Instance.OnMatchStarting += OnMatchStarting;
                
                // Refresh player list
                RefreshPlayerList();
            }
            
            // Only host can start the match
            startButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
        }
        
        private void OnDisconnectedFromServer()
        {
            ShowConnectionPanel();
            statusText.text = "Disconnected from server";
            ClearPlayerList();
        }
        
        private void OnPlayerJoinedLobby(Lobby.LobbyPlayerData playerData)
        {
            RefreshPlayerList();
        }
        
        private void OnPlayerLeftLobby(ulong clientId)
        {
            RefreshPlayerList();
        }
        
        private void OnPlayerReadyChanged(ulong clientId, bool ready)
        {
            RefreshPlayerList();
        }
        
        private void OnMatchStarting()
        {
            statusText.text = "Match starting...";
        }
        
        private void RefreshPlayerList()
        {
            ClearPlayerList();
            
            if (Lobby.LobbyManager.Instance == null) return;
            
            var players = Lobby.LobbyManager.Instance.GetLobbyPlayers();
            
            foreach (var player in players)
            {
                CreatePlayerListItem(player);
            }
            
            // Update status text
            statusText.text = $"Players: {players.Count}/{networkConfig.maxPlayersPerArena}";
        }
        
        private void CreatePlayerListItem(Lobby.LobbyPlayerData playerData)
        {
            if (playerListItemPrefab == null || playerListContainer == null) return;
            
            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
            
            // Setup player info
            var nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = playerData.PlayerName.ToString();
            }
            
            var readyText = item.transform.Find("ReadyText")?.GetComponent<TMP_Text>();
            if (readyText != null)
            {
                readyText.text = playerData.IsReady ? "READY" : "NOT READY";
                readyText.color = playerData.IsReady ? Color.green : Color.red;
            }
            
            playerListItems[playerData.ClientId] = item;
        }
        
        private void ClearPlayerList()
        {
            foreach (var item in playerListItems.Values)
            {
                Destroy(item);
            }
            
            playerListItems.Clear();
        }
        
        private void UpdateReadyButton()
        {
            if (readyButton != null)
            {
                var buttonText = readyButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = isReady ? "UNREADY" : "READY";
                }
                
                var colors = readyButton.colors;
                colors.normalColor = isReady ? Color.red : Color.green;
                readyButton.colors = colors;
            }
        }
        
        private void ShowConnectionPanel()
        {
            connectionPanel?.SetActive(true);
            lobbyPanel?.SetActive(false);
        }
        
        private void ShowLobbyPanel()
        {
            connectionPanel?.SetActive(false);
            lobbyPanel?.SetActive(true);
        }
    }
}
