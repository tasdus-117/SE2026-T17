using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SmallArena.Arena
{
    /// <summary>
    /// Manages arena gameplay, spawn points, and match flow
    /// </summary>
    public class ArenaManager : NetworkBehaviour
    {
        public static ArenaManager Instance { get; private set; }
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnProtectionTime = 3f;
        
        [Header("Match Settings")]
        [SerializeField] private float matchDuration = 300f; // 5 minutes
        [SerializeField] private int scoreToWin = 20;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        
        // Network variables
        private NetworkVariable<float> matchTimeRemaining = new NetworkVariable<float>();
        private NetworkVariable<bool> matchInProgress = new NetworkVariable<bool>();
        
        private Dictionary<ulong, int> playerScores = new Dictionary<ulong, int>();
        private List<Transform> availableSpawnPoints = new List<Transform>();
        
        public float MatchTimeRemaining => matchTimeRemaining.Value;
        public bool IsMatchInProgress => matchInProgress.Value;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // Find spawn points in scene if not set
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                FindSpawnPoints();
            }
            
            InitializeSpawnPoints();
        }
        
        private void FindSpawnPoints()
        {
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPoints = new Transform[spawnPointObjects.Length];
            
            for (int i = 0; i < spawnPointObjects.Length; i++)
            {
                spawnPoints[i] = spawnPointObjects[i].transform;
            }
            
            if (spawnPoints.Length == 0)
            {
                Debug.LogWarning("No spawn points found! Creating default spawn points.");
                CreateDefaultSpawnPoints();
            }
        }
        
        private void CreateDefaultSpawnPoints()
        {
            // Create 4 spawn points in corners of arena
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-10, 1, -10),
                new Vector3(10, 1, -10),
                new Vector3(-10, 1, 10),
                new Vector3(10, 1, 10)
            };
            
            spawnPoints = new Transform[positions.Length];
            
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
                spawnPoint.tag = "SpawnPoint";
                spawnPoint.transform.position = positions[i];
                spawnPoints[i] = spawnPoint.transform;
            }
        }
        
        private void InitializeSpawnPoints()
        {
            availableSpawnPoints.Clear();
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    availableSpawnPoints.Add(spawnPoint);
                }
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                StartMatch();
            }
        }
        
        private void Update()
        {
            if (!IsServer) return;
            
            if (matchInProgress.Value)
            {
                matchTimeRemaining.Value -= Time.deltaTime;
                
                if (matchTimeRemaining.Value <= 0)
                {
                    EndMatch();
                }
            }
        }
        
        /// <summary>
        /// Start the match
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartMatchServerRpc()
        {
            StartMatch();
        }
        
        private void StartMatch()
        {
            if (!IsServer) return;
            
            matchTimeRemaining.Value = matchDuration;
            matchInProgress.Value = true;
            playerScores.Clear();
            
            Debug.Log("Match started!");
            
            // Spawn all players
            SpawnAllPlayers();
            
            NotifyMatchStartClientRpc();
        }
        
        [ClientRpc]
        private void NotifyMatchStartClientRpc()
        {
            Debug.Log("Match has started!");
        }
        
        /// <summary>
        /// End the match
        /// </summary>
        private void EndMatch()
        {
            if (!IsServer) return;
            
            matchInProgress.Value = false;
            
            // Determine winner
            ulong winnerId = DetermineWinner();
            
            Debug.Log($"Match ended! Winner: Player {winnerId}");
            
            NotifyMatchEndClientRpc(winnerId);
        }
        
        [ClientRpc]
        private void NotifyMatchEndClientRpc(ulong winnerId)
        {
            Debug.Log($"Match ended! Winner: Player {winnerId}");
            // Could show end game screen here
        }
        
        private ulong DetermineWinner()
        {
            ulong winnerId = 0;
            int highestScore = -1;
            
            foreach (var kvp in playerScores)
            {
                if (kvp.Value > highestScore)
                {
                    highestScore = kvp.Value;
                    winnerId = kvp.Key;
                }
            }
            
            return winnerId;
        }
        
        /// <summary>
        /// Spawn all connected players
        /// </summary>
        private void SpawnAllPlayers()
        {
            if (!IsServer) return;
            
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                SpawnPlayer(client.ClientId);
            }
        }
        
        /// <summary>
        /// Spawn a specific player
        /// </summary>
        public void SpawnPlayer(ulong clientId)
        {
            if (!IsServer) return;
            
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;
            
            // Get or create player object
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    // Respawn existing player
                    client.PlayerObject.transform.position = spawnPosition;
                    client.PlayerObject.transform.rotation = spawnRotation;
                    
                    // Reset health
                    var healthSystem = client.PlayerObject.GetComponent<Combat.HealthSystem>();
                    if (healthSystem != null)
                    {
                        healthSystem.Heal(healthSystem.MaxHealth);
                    }
                }
                else if (playerPrefab != null)
                {
                    // Spawn new player
                    GameObject playerObject = Instantiate(playerPrefab, spawnPosition, spawnRotation);
                    playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                }
            }
            
            Debug.Log($"Spawned player {clientId} at {spawnPosition}");
        }
        
        /// <summary>
        /// Get a spawn position (tries to find one far from other players)
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return new Vector3(0, 1, 0);
            }
            
            // Find spawn point farthest from all players
            Transform bestSpawn = spawnPoints[0];
            float bestDistance = 0;
            
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint == null) continue;
                
                float minDistanceToPlayer = float.MaxValue;
                
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject == null) continue;
                    
                    float distance = Vector3.Distance(spawnPoint.position, client.PlayerObject.transform.position);
                    if (distance < minDistanceToPlayer)
                    {
                        minDistanceToPlayer = distance;
                    }
                }
                
                if (minDistanceToPlayer > bestDistance)
                {
                    bestDistance = minDistanceToPlayer;
                    bestSpawn = spawnPoint;
                }
            }
            
            return bestSpawn.position;
        }
        
        /// <summary>
        /// Record a kill
        /// </summary>
        public void RecordKill(ulong killerId, ulong victimId)
        {
            if (!IsServer) return;
            
            if (!playerScores.ContainsKey(killerId))
            {
                playerScores[killerId] = 0;
            }
            
            playerScores[killerId]++;
            
            // Check if player reached score to win
            if (playerScores[killerId] >= scoreToWin)
            {
                EndMatch();
            }
        }
        
        /// <summary>
        /// Get player score
        /// </summary>
        public int GetPlayerScore(ulong clientId)
        {
            return playerScores.TryGetValue(clientId, out int score) ? score : 0;
        }
        
        private void OnDrawGizmos()
        {
            if (spawnPoints == null) return;
            
            Gizmos.color = Color.green;
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                    Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
                }
            }
        }
    }
}
