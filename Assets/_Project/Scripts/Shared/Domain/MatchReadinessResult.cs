namespace SmallArena.Domain
{
    /// <summary>Immutable validation result supplied by the initialization owner.</summary>
    public readonly struct MatchReadinessResult
    {
        private readonly string diagnosticMessage;

        public MatchReadinessResult(bool isReady, string diagnosticMessage = "")
        {
            IsReady = isReady;
            this.diagnosticMessage = diagnosticMessage;
        }

        public bool IsReady { get; }

        public static MatchReadinessResult Success() => new MatchReadinessResult(true);

        public static MatchReadinessResult Fail(string diagnosticMessage) => new MatchReadinessResult(false, diagnosticMessage);

        // A default struct is safely not ready and has an empty diagnostic.
        public string DiagnosticMessage => diagnosticMessage ?? string.Empty;
    }
}
