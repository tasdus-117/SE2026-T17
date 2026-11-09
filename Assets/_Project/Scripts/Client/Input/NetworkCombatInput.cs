using Mirror;
using SmallArena.Input;
using SmallArena.Networking;
using UnityEngine;

namespace SmallArena.Client
{
    [DisallowMultipleComponent]
    public sealed class NetworkCombatInput : MonoBehaviour
    {
        private NetworkPlayer player;
        private NetworkHealth health;
        private PlayerInputHandler input;
        private void Awake()
        {
            player = GetComponent<NetworkPlayer>();
            health = GetComponent<NetworkHealth>();
            input = GetComponent<PlayerInputHandler>();
        }
        private void Update()
        {
            if (player == null || !player.isLocalPlayer || !NetworkClient.ready || input == null) return;
            // Both reliable Commands preserve E-before-attack ordering, matching the offline loop.
            if (input.ConsumeInteractPressed() && health != null && health.CanAct) RequestNearbyLoot();
            if (input.ConsumePunchPressed() && health != null && health.CanAct) player.RequestAttack(Vector3.forward);
        }
        private void RequestNearbyLoot()
        {
            NetworkLootItem closest = null;
            var best = NetworkLootItem.InteractionRadius * NetworkLootItem.InteractionRadius;
            foreach (var collider in Physics.OverlapSphere(transform.position, NetworkLootItem.InteractionRadius, 1 << 9, QueryTriggerInteraction.Collide))
            {
                var item = collider.GetComponentInParent<NetworkLootItem>();
                if (item == null || !item.isClient || item.IsCollected) continue;
                var distance = (item.transform.position - transform.position).sqrMagnitude;
                if (distance <= best && (closest == null || distance < best || item.netId < closest.netId))
                { closest = item; best = distance; }
            }
            // Only request: replicas remain visible until the server's destroy message.
            if (closest != null) player.RequestPickup(closest.netId);
        }
    }
}
