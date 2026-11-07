using Mirror;

namespace SmallArena.Networking
{
    /// <summary>Stateless last-survivor resolution for the current server's spawned players.</summary>
    internal static class NetworkMatchRules
    {
        public static void ReportDefeat()
        {
            if (!NetworkServer.active) return;
            NetworkHealth survivor = null;
            var participants = 0;
            var living = 0;
            foreach (var identity in NetworkServer.spawned.Values)
            {
                if (identity == null || !identity.TryGetComponent<NetworkPlayer>(out _)
                    || !identity.TryGetComponent<NetworkHealth>(out var health)) continue;
                participants++;
                if (!health.IsDead) { living++; survivor = health; }
            }
            if (participants >= 2 && living == 1) survivor.SetResult(PlayerMatchResult.Victory);
        }
    }
}
