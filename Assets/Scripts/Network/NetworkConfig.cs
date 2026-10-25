using UnityEngine;

namespace SmallArena.Network
{
    /// <summary>
    /// Configuration settings for network gameplay
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "SmallArena/Network Config")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("Connection Settings")]
        [Tooltip("Server IP address")]
        public string serverIP = "127.0.0.1";
        
        [Tooltip("Server port")]
        public ushort serverPort = 7777;
        
        [Tooltip("Maximum number of players per arena")]
        [Range(2, 8)]
        public int maxPlayersPerArena = 4;
        
        [Header("Game Settings")]
        [Tooltip("Arena match duration in seconds")]
        public float matchDuration = 300f; // 5 minutes
        
        [Tooltip("Respawn delay after death")]
        public float respawnDelay = 3f;
        
        [Header("Network Settings")]
        [Tooltip("Client tick rate")]
        public int tickRate = 60;
        
        [Tooltip("Connection timeout in seconds")]
        public float connectionTimeout = 10f;
        
        [Tooltip("Snapshot interpolation delay (ms)")]
        public int snapshotInterpolationMs = 100;
        
        [Header("Anti-Cheat Settings")]
        [Tooltip("Maximum allowed client-server position difference")]
        public float maxPositionDrift = 2f;
        
        [Tooltip("Movement validation threshold")]
        public float movementValidationThreshold = 1.5f;
        
        [Tooltip("Maximum allowed movement speed")]
        public float maxMovementSpeed = 15f;
        
        [Header("Latency Simulation")]
        [Tooltip("Enable artificial latency")]
        public bool enableLatencySimulation = false;
        
        [Tooltip("Artificial latency in milliseconds")]
        [Range(0, 500)]
        public int artificialLatencyMs = 0;
        
        [Tooltip("Packet loss percentage")]
        [Range(0f, 100f)]
        public float packetLossPercentage = 0f;
    }
}
