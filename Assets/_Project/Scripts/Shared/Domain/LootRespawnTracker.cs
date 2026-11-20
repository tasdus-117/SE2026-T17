using System;
using System.Collections.Generic;

namespace SmallArena.Domain
{
    public enum LootLifecycleState { AvailableOnMap, CarriedByPlayer, RespawningAfterDeath, Cancelled }

    /// <summary>One weapon per session. Becoming ready never claims that a physical spawn succeeded.</summary>
    public sealed class LootRespawnTracker
    {
        public LootRespawnTracker(double delaySeconds = 30)
        {
            if (!Finite(delaySeconds) || delaySeconds <= 0) throw new ArgumentOutOfRangeException(nameof(delaySeconds));
            DelaySeconds = delaySeconds;
        }
        public double DelaySeconds { get; }
        public LootLifecycleState State { get; private set; }
        public string CarrierId { get; private set; }
        public double RemainingSeconds { get; private set; }
        public bool IsReady => State == LootLifecycleState.RespawningAfterDeath && RemainingSeconds == 0;
        public bool TryPickUp(string participantId)
        {
            if (State != LootLifecycleState.AvailableOnMap || string.IsNullOrWhiteSpace(participantId)) return false;
            CarrierId = participantId; State = LootLifecycleState.CarriedByPlayer; return true;
        }
        public bool CarrierLost(string participantId)
        {
            if (State != LootLifecycleState.CarriedByPlayer || CarrierId != participantId) return false;
            CarrierId = null; RemainingSeconds = DelaySeconds; State = LootLifecycleState.RespawningAfterDeath; return true;
        }
        public void Tick(double deltaSeconds)
        {
            if (!Finite(deltaSeconds) || deltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (State == LootLifecycleState.RespawningAfterDeath) RemainingSeconds = Math.Max(0, RemainingSeconds - deltaSeconds);
        }
        public bool ConfirmSpawn()
        {
            if (!IsReady) return false;
            RemainingSeconds = 0; State = LootLifecycleState.AvailableOnMap; return true;
        }
        public void Cancel() { State = LootLifecycleState.Cancelled; CarrierId = null; RemainingSeconds = 0; }
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public readonly struct LootPoint
    {
        public LootPoint(double x, double z)
        {
            if (!LootRespawnTracker.Finite(x) || !LootRespawnTracker.Finite(z)) throw new ArgumentOutOfRangeException(nameof(x));
            X = x; Z = z;
        }
        public double X { get; }
        public double Z { get; }
        public double DistanceSquared(LootPoint other) => (X - other.X) * (X - other.X) + (Z - other.Z) * (Z - other.Z);
    }

    public static class FairLootSpawnSelector
    {
        // Rotating the scan origin gives deterministic, seed-controlled tie breaking without
        // ever accepting a worse nearest-player distance merely because of random jitter.
        public static int SelectIndex(IReadOnlyList<LootPoint> candidates, IReadOnlyList<LootPoint> players, int tieOffset = 0)
        {
            if (candidates == null || players == null) throw new ArgumentNullException();
            if (candidates.Count == 0) return -1;
            var start = (int)(((long)tieOffset % candidates.Count + candidates.Count) % candidates.Count);
            var best = -1; var bestScore = double.NegativeInfinity;
            for (var n = 0; n < candidates.Count; n++)
            {
                var i = (start + n) % candidates.Count; var score = players.Count == 0 ? 0 : double.PositiveInfinity;
                foreach (var player in players) score = Math.Min(score, candidates[i].DistanceSquared(player));
                if (best < 0 || score > bestScore) { best = i; bestScore = score; }
            }
            return best;
        }
    }
}
