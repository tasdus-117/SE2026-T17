using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Reliable teleports clear interpolation, never the ongoing wire delta-compression stream.</summary>
    public sealed class ArenaNetworkTransform : NetworkTransformReliable
    {
        protected override void OnTeleport(Vector3 destination) => CommitTeleport(destination, target.rotation);
        protected override void OnTeleport(Vector3 destination, Quaternion rotation) => CommitTeleport(destination, rotation);

        private void CommitTeleport(Vector3 destination, Quaternion rotation)
        {
            var character = target.GetComponent<CharacterController>();
            var restoreCollision = isServer && character != null && character.enabled;
            if (character != null) character.enabled = false;
            target.SetPositionAndRotation(destination, rotation);
            // Mirror 96's base OnTeleport calls the reliable override of ResetState(), resetting
            // lastSerialized/lastDeserializedPosition. A queued state and teleport can then disagree
            // about the delta origin. Preserve both endpoints' stream history across a teleport.
            serverSnapshots.Clear();
            clientSnapshots.Clear();
            Physics.SyncTransforms();
            if (restoreCollision) { character.enabled = true; Physics.SyncTransforms(); }
            // Pure client replicas always keep collision disabled, including the owning client.
            // This prefab uses Update interpolation (useFixedUpdate=false), with no pending physics pose.
        }
    }
}
