using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SmallArena.Network
{
    /// <summary>
    /// Simulates network latency and packet loss for testing
    /// </summary>
    public class LatencySimulator : MonoBehaviour
    {
        public static LatencySimulator Instance { get; private set; }
        
        [SerializeField] private NetworkConfig networkConfig;
        
        [Header("Runtime Controls")]
        [SerializeField] private bool enableSimulation = false;
        [SerializeField] [Range(0, 500)] private int latencyMs = 100;
        [SerializeField] [Range(0f, 100f)] private float packetLoss = 0f;
        [SerializeField] [Range(0, 200)] private int jitterMs = 20;
        
        private Queue<DelayedMessage> messageQueue = new Queue<DelayedMessage>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            if (networkConfig != null)
            {
                enableSimulation = networkConfig.enableLatencySimulation;
                latencyMs = networkConfig.artificialLatencyMs;
                packetLoss = networkConfig.packetLossPercentage;
            }
        }
        
        private void Update()
        {
            ProcessMessageQueue();
        }
        
        /// <summary>
        /// Simulate network delay for an action
        /// </summary>
        public void SimulateDelay(System.Action action)
        {
            if (!enableSimulation || action == null)
            {
                action?.Invoke();
                return;
            }
            
            // Simulate packet loss
            if (Random.Range(0f, 100f) < packetLoss)
            {
                Debug.Log("Simulated packet loss");
                return;
            }
            
            // Calculate delay with jitter
            int actualDelay = latencyMs + Random.Range(-jitterMs, jitterMs);
            actualDelay = Mathf.Max(0, actualDelay);
            
            if (actualDelay > 0)
            {
                messageQueue.Enqueue(new DelayedMessage
                {
                    Action = action,
                    ExecuteTime = Time.time + (actualDelay / 1000f)
                });
            }
            else
            {
                action.Invoke();
            }
        }
        
        /// <summary>
        /// Simulate network delay for a coroutine
        /// </summary>
        public IEnumerator SimulateDelayCoroutine(System.Action action)
        {
            if (!enableSimulation || action == null)
            {
                action?.Invoke();
                yield break;
            }
            
            // Simulate packet loss
            if (Random.Range(0f, 100f) < packetLoss)
            {
                Debug.Log("Simulated packet loss");
                yield break;
            }
            
            // Calculate delay with jitter
            int actualDelay = latencyMs + Random.Range(-jitterMs, jitterMs);
            actualDelay = Mathf.Max(0, actualDelay);
            
            if (actualDelay > 0)
            {
                yield return new WaitForSeconds(actualDelay / 1000f);
            }
            
            action.Invoke();
        }
        
        private void ProcessMessageQueue()
        {
            while (messageQueue.Count > 0 && messageQueue.Peek().ExecuteTime <= Time.time)
            {
                var message = messageQueue.Dequeue();
                message.Action?.Invoke();
            }
        }
        
        /// <summary>
        /// Set latency simulation parameters at runtime
        /// </summary>
        public void SetSimulationParameters(bool enabled, int latency, float loss)
        {
            enableSimulation = enabled;
            latencyMs = Mathf.Clamp(latency, 0, 500);
            packetLoss = Mathf.Clamp(loss, 0f, 100f);
            
            Debug.Log($"Latency simulation updated: Enabled={enabled}, Latency={latencyMs}ms, Loss={packetLoss}%");
        }
        
        /// <summary>
        /// Get current simulation status
        /// </summary>
        public (bool enabled, int latency, float packetLoss, int jitter) GetSimulationStatus()
        {
            return (enableSimulation, latencyMs, packetLoss, jitterMs);
        }
        
        /// <summary>
        /// Clear all pending messages
        /// </summary>
        public void ClearQueue()
        {
            messageQueue.Clear();
        }
        
        private struct DelayedMessage
        {
            public System.Action Action;
            public float ExecuteTime;
        }
        
        private void OnDestroy()
        {
            messageQueue.Clear();
        }
    }
}
