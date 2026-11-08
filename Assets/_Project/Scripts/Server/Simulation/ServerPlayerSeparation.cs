using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace SmallArena.Server.Simulation
{
    /// <summary>One deterministic solver after all authoritative movement. Never writes client transforms.</summary>
    [DefaultExecutionOrder(1000), RequireComponent(typeof(CharacterController))]
    public sealed class ServerPlayerSeparation : NetworkBehaviour
    {
        private readonly List<ServerPlayerSeparation> players = new List<ServerPlayerSeparation>();
        private CharacterController body;
        private void Awake() => body = GetComponent<CharacterController>();
        private void FixedUpdate()
        {
            if (!isServer || !NetworkServer.active || !body.enabled) return;
            players.Clear();
            foreach (var identity in NetworkServer.spawned.Values)
            {
                if (identity == null || !identity.TryGetComponent<ServerPlayerSeparation>(out var player)
                    || !player.isActiveAndEnabled || !player.isServer || !player.body.enabled) continue;
                if (identity.netId < netId) return; // Lowest live netId owns this physics step; no static singleton.
                players.Add(player);
            }
            players.Sort((a, b) => a.netId.CompareTo(b.netId));
            for (var pass = 0; pass < 8; pass++)
            {
                var moved = false;
                for (var i = 0; i < players.Count; i++)
                    for (var j = i + 1; j < players.Count; j++) moved |= Resolve(players[i].body, players[j].body);
                if (!moved) break;
            }
        }
        private static bool Resolve(CharacterController a, CharacterController b)
        {
            var centerA = a.transform.TransformPoint(a.center);
            var centerB = b.transform.TransformPoint(b.center);
            var scaleA = a.transform.lossyScale; var scaleB = b.transform.lossyScale;
            var radiusA = a.radius * Mathf.Max(Mathf.Abs(scaleA.x), Mathf.Abs(scaleA.z));
            var radiusB = b.radius * Mathf.Max(Mathf.Abs(scaleB.x), Mathf.Abs(scaleB.z));
            var segmentA = Mathf.Max(0, a.height * Mathf.Abs(scaleA.y) * 0.5f - radiusA);
            var segmentB = Mathf.Max(0, b.height * Mathf.Abs(scaleB.y) * 0.5f - radiusB);
            var verticalGap = Mathf.Max(0, Mathf.Abs(centerA.y - centerB.y) - segmentA - segmentB);
            var sum = radiusA + radiusB;
            if (verticalGap >= sum) return false;
            var required = Mathf.Sqrt(sum * sum - verticalGap * verticalGap) + 0.001f;
            var delta = centerB - centerA; delta.y = 0;
            var distance = delta.magnitude;
            var penetration = required - distance;
            if (penetration <= 0.0001f) return false;
            var normal = distance > 0.00001f ? delta / distance : Vector3.right;
            var beforeA = a.transform.position; var beforeB = b.transform.position;
            // CharacterController.Move sweeps against walls; a blocked half transfers to the other body.
            MoveBody(a, -normal * (penetration * 0.5f));
            var movedA = Mathf.Max(0, Vector3.Dot(beforeA - a.transform.position, normal));
            MoveBody(b, normal * Mathf.Max(0, penetration - movedA));
            var movedB = Mathf.Max(0, Vector3.Dot(b.transform.position - beforeB, normal));
            var remaining = penetration - movedA - movedB;
            if (remaining > 0.0001f) MoveBody(a, -normal * remaining);
            return (a.transform.position - beforeA).sqrMagnitude + (b.transform.position - beforeB).sqrMagnitude > 0.00000001f;
        }
        private static void MoveBody(CharacterController body, Vector3 correction)
        {
            // Preserve floor contact for the next jump check; a purely horizontal Move can clear isGrounded.
            body.Move(correction + (body.isGrounded ? Vector3.down * 0.001f : Vector3.zero));
        }
    }
}
