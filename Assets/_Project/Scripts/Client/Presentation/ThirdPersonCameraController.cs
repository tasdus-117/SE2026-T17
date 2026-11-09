using Mirror;
using SmallArena.Networking;
using UnityEngine;

namespace SmallArena.Client.Presentation
{
    /// <summary>World-relative, fixed-heading presentation follow after character movement.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 4.5f, -6f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float pitchAngle = 30f;

        private Vector3 currentVelocity;

        public Transform Target => target;

        public void SetTarget(Transform newTarget)
        {
            if (newTarget != null && newTarget.TryGetComponent<NetworkIdentity>(out var identity)
                && !identity.isLocalPlayer) newTarget = null;
            target = newTarget;
            currentVelocity = Vector3.zero;
        }

        private void Awake() => ApplyFixedRotation();

        private void OnEnable()
        {
            currentVelocity = Vector3.zero;
            NetworkPlayer.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
            NetworkPlayer.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
            // The scene camera can enable after the local-player spawn callback.
            if (NetworkClient.localPlayer != null) SetTarget(NetworkClient.localPlayer.transform);
        }

        private void OnDisable()
        {
            NetworkPlayer.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
            NetworkPlayer.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
            currentVelocity = Vector3.zero;
        }

        private void HandleLocalPlayerSpawned(NetworkPlayer player)
        {
            if (player.IsLocalControllable) SetTarget(player.transform);
        }

        private void HandleLocalPlayerDespawned(NetworkPlayer player)
        {
            if (target == player.transform) SetTarget(null);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (target.TryGetComponent<NetworkIdentity>(out var identity) && !identity.isLocalPlayer)
            {
                SetTarget(null);
                return;
            }

            var desiredPosition = target.position + followOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
                ref currentVelocity, smoothTime);
            ApplyFixedRotation();
        }

        private void ApplyFixedRotation() => transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }
}
