using System;
using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Network identity and local-player lifecycle only; gameplay lives in its own components.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        public int ConnectionId { get; [Server] private set; } = -1;
        public bool IsLocalControllable => isLocalPlayer;

        public static event Action<NetworkPlayer> OnLocalPlayerSpawned;
        public static event Action<NetworkPlayer> OnLocalPlayerDespawned;
        private bool localLifecycleStarted;

        public override void OnStartLocalPlayer()
        {
            if (localLifecycleStarted) return;
            localLifecycleStarted = true;
            OnLocalPlayerSpawned?.Invoke(this);
        }

        public override void OnStopLocalPlayer()
        {
            if (!localLifecycleStarted) return;
            localLifecycleStarted = false;
            OnLocalPlayerDespawned?.Invoke(this);
        }

        public override void OnStopClient() => OnStopLocalPlayer();

        [Server]
        public void SetConnectionId(int connId) => ConnectionId = connId;
    }
}
