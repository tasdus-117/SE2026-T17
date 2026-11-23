#if SMALL_ARENA_FOUR_PLAYER_HARNESS
using System;
using System.Linq;
using Mirror;
using UnityEngine;

namespace SmallArena.Networking.Verification
{
    // Test setup only: compiled into an isolated instrumented build, never the product build.
    public static class FourPlayerHarnessBridge
    {
        public static void Arrange(int attackerSlot, int victimSlot, bool atLoot)
        {
            if (!NetworkServer.active) throw new InvalidOperationException("Only the authoritative host arranges fixtures.");
            if (attackerSlot != 0 || victimSlot < 1 || victimSlot > 3) throw new ArgumentException("Fixture must distinguish the observed client slot from the host.");
            var actors = NetworkServer.spawned.Values.Where(i => i != null).Select(i => i.GetComponent<NetworkHealth>())
                .Where(h => h != null && h.IsLifecycleBound).OrderBy(h => h.SlotIndex).ToArray();
            if (actors.Length != 4 || actors.Any(h => !h.CanAct)) throw new InvalidOperationException("Four alive participants required for fixture arrangement.");
            var arena = UnityEngine.Object.FindAnyObjectByType<ArenaObstacles>();
            if (arena == null || arena.FloorCollider == null) throw new InvalidOperationException("Missing production arena floor.");
            var floor = arena.FloorCollider.bounds;
            var y = floor.max.y + 1.04f;
            Physics.SyncTransforms();
            Func<Vector3, bool> clear = p => p.x > floor.min.x + 1 && p.x < floor.max.x - 1
                && p.z > floor.min.z + 1 && p.z < floor.max.z - 1
                && !Physics.CheckCapsule(p + Vector3.up * .5f, p - Vector3.up * .5f, .49f, 1 << 6, QueryTriggerInteraction.Ignore);
            if (atLoot)
            {
                var loot = NetworkServer.spawned.Values.Select(i => i.GetComponent<NetworkLootItem>()).FirstOrDefault(l => l != null);
                if (loot == null) throw new InvalidOperationException("No authoritative loot available.");
                var point = new Vector3(loot.transform.position.x, y, loot.transform.position.z);
                if (!clear(point)) throw new InvalidOperationException("Loot fixture has no capsule clearance.");
                actors.Single(a => a.SlotIndex == victimSlot).GetComponent<ArenaNetworkTransform>().ServerTeleport(point, Quaternion.identity);
                return;
            }
            var points = (from x in Enumerable.Range(0, 7) from z in Enumerable.Range(0, 7)
                select new Vector3(floor.min.x + 2 + x * (floor.size.x - 4) / 6, y,
                    floor.min.z + 2 + z * (floor.size.z - 4) / 6)).Where(clear).ToArray();
            foreach (var point in points)
            {
                var victim = point + Vector3.forward * 1.2f;
                if (!clear(victim)) continue;
                var idle = points.Where(p => Vector3.Distance(p, point) > 3 && Vector3.Distance(p, victim) > 3).ToArray();
                if (idle.Length < 2) continue;
                var second = idle.FirstOrDefault(p => Vector3.Distance(p, idle[0]) > 2);
                if (second == default) continue;
                var index = 0;
                foreach (var actor in actors)
                {
                    var destination = actor.SlotIndex == attackerSlot ? point : actor.SlotIndex == victimSlot ? victim : index++ == 0 ? idle[0] : second;
                    actor.GetComponent<ArenaNetworkTransform>().ServerTeleport(destination, Quaternion.identity);
                }
                return;
            }
            throw new InvalidOperationException("No obstacle-free four-player fixture found in the actual arena.");
        }
    }
}
#endif
