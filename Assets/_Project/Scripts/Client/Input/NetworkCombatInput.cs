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
            if (input.ConsumePunchPressed() && health != null && health.CanAct) player.RequestAttack(Vector3.forward);
        }
    }
}
