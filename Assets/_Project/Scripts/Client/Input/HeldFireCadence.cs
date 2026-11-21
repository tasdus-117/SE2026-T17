namespace SmallArena.Client
{
    /// <summary>Traffic limiter only. No accumulation, retry, or authority over server cooldown.</summary>
    public sealed class HeldFireCadence
    {
        private bool released;
        private double nextRequest;
        private double lastTime = -1;
        public void Reset() { released = false; nextRequest = 0; lastTime = -1; }
        public bool TryRequest(bool allowed, bool held, double now, double interval)
        {
            if (!allowed || !Finite(now) || now < 0 || !Finite(interval) || interval <= 0
                || now < lastTime || !Finite(now + interval) || now + interval <= now)
            { Reset(); return false; }
            lastTime = now;
            if (!held) { released = true; return false; }
            if (!released || now < nextRequest) return false;
            nextRequest = now + interval;
            return true;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
