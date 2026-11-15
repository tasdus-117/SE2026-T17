using Mirror;
using SmallArena.Networking;
using SmallArena.Server.Combat;
using UnityEngine;

namespace SmallArena.Server.Simulation
{
    /// <summary>Use the existing death path; replicas never decide damage.</summary>
    public sealed class FallDamage : MonoBehaviour
    {
        private NetworkHealth networkHealth;
        private Health offlineHealth;
        private void Awake()
        {
            networkHealth = GetComponent<NetworkHealth>();
            offlineHealth = GetComponent<Health>();
        }
        private void FixedUpdate()
        {
            if (transform.position.y >= ArenaObstacles.FallHeight) return;
            if (networkHealth != null)
            {
                if (NetworkServer.active && networkHealth.isServer && networkHealth.CanAct)
                    networkHealth.TakeDamage(networkHealth.CurrentHealth);
            }
            else if (!NetworkServer.active && !NetworkClient.active && offlineHealth != null && !offlineHealth.IsDead)
                offlineHealth.TakeDamage(offlineHealth.CurrentHealth);
        }
    }
}
