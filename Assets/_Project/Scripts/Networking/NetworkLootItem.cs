using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Single server commit before buff/hooks/destroy. Mirror Commands execute serially on Unity's main thread.</summary>
    [DisallowMultipleComponent]
    public sealed class NetworkLootItem : NetworkBehaviour
    {
        public const float InteractionRadius = 1.5f;
        [SyncVar] private bool isCollected;
        public bool IsCollected => isCollected;

        [Server]
        public bool TryCollectServer(NetworkPlayer player)
        {
            if (!isServer || isCollected || player == null || !player.isServer
                || !NetworkServer.spawned.TryGetValue(netId, out var registered) || registered != netIdentity
                || !NetworkServer.spawned.TryGetValue(player.netId, out var owner) || owner != player.netIdentity
                || !player.TryGetComponent<NetworkCombat>(out var combat) || !combat.CanReceiveWeapon
                || (player.transform.position - transform.position).sqrMagnitude > InteractionRadius * InteractionRadius) return false;
            // Never invoke user code or publish the buff before claiming the item.
            isCollected = true;
            foreach (var collider in GetComponents<Collider>()) collider.enabled = false;
            combat.GrantWeapon();
            NetworkServer.Destroy(gameObject);
            return true;
        }
    }
}
