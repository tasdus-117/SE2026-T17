using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SmallArena.Domain
{
    public static class RoomCodeGenerator
    {
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        public static string Normalize(string raw) => raw?.Trim().ToUpperInvariant() ?? "";
        public static bool IsValid(string code) => code != null && code.Length == 6 && code.All(c => Alphabet.IndexOf(c) >= 0);
        public static string Generate(Func<string, bool> isCodeInUse = null, int maxAttempts = 64)
        {
            if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            var bytes = new byte[6]; var chars = new char[6];
            using (var random = RandomNumberGenerator.Create())
                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    random.GetBytes(bytes);
                    for (var i = 0; i < chars.Length; i++) chars[i] = Alphabet[bytes[i] & 31];
                    var code = new string(chars);
                    if (isCodeInUse == null || !isCodeInUse(code)) return code;
                }
            throw new InvalidOperationException("Room code collision budget exhausted.");
        }
    }

    public enum RoomDirectoryPhase { Lobby, InMatch }
    public enum RoomJoinFailureReason { None, InvalidCodeFormat, RoomNotFound, RoomFull, MatchAlreadyInProgress, DirectoryUnavailable }

    public sealed class RoomListingInfo
    {
        public string RoomCode { get; }
        public string RoomName { get; }
        public string HostDisplayName { get; }
        public int CurrentPlayers { get; }
        public int MaxPlayers => 4;
        public RoomDirectoryPhase Phase { get; }
        public bool IsPublic { get; }
        public long LastHeartbeatUtcMs { get; }
        public RoomListingInfo(string roomCode, string roomName, string hostDisplayName, int currentPlayers,
            RoomDirectoryPhase phase = RoomDirectoryPhase.Lobby, bool isPublic = true, long lastHeartbeatUtcMs = 0)
        {
            if (!RoomCodeGenerator.IsValid(roomCode)) throw new ArgumentException("Invalid room code.", nameof(roomCode));
            RequireLabel(roomName, 64); RequireLabel(hostDisplayName, 32);
            if (currentPlayers < 0 || currentPlayers > 4) throw new ArgumentOutOfRangeException(nameof(currentPlayers));
            if (!Enum.IsDefined(typeof(RoomDirectoryPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            if (lastHeartbeatUtcMs < 0) throw new ArgumentOutOfRangeException(nameof(lastHeartbeatUtcMs));
            RoomCode = roomCode; RoomName = roomName; HostDisplayName = hostDisplayName;
            CurrentPlayers = currentPlayers; Phase = phase; IsPublic = isPublic; LastHeartbeatUtcMs = lastHeartbeatUtcMs;
        }
        private static void RequireLabel(string label, int bytes)
        {
            if (string.IsNullOrWhiteSpace(label) || Encoding.UTF8.GetByteCount(label) > bytes || label.Any(char.IsControl))
                throw new ArgumentException("Room label is empty, too long or contains control characters.");
        }
    }

    public sealed class RoomResolvedEndpoint
    {
        public string Host { get; }
        public ushort Port { get; }
        public string RoomCode { get; }
        public string RoomInstanceId { get; }
        public RoomResolvedEndpoint(string host, ushort port, string roomCode, string roomInstanceId)
        {
            if (string.IsNullOrWhiteSpace(host) || host.Length > 253 || Uri.CheckHostName(host) == UriHostNameType.Unknown || port == 0)
                throw new ArgumentException("Invalid endpoint.");
            if (!RoomCodeGenerator.IsValid(roomCode) || !Guid.TryParseExact(roomInstanceId, "N", out _))
                throw new ArgumentException("Invalid room identity.");
            Host = host; Port = port; RoomCode = roomCode; RoomInstanceId = roomInstanceId;
        }
    }

    public sealed class RoomJoinResolution
    {
        public bool Success => FailureReason == RoomJoinFailureReason.None;
        public RoomJoinFailureReason FailureReason { get; }
        public RoomListingInfo Listing { get; }
        public RoomResolvedEndpoint Endpoint { get; }
        public RoomJoinResolution(RoomJoinFailureReason failureReason, RoomListingInfo listing = null, RoomResolvedEndpoint endpoint = null)
        {
            if (!Enum.IsDefined(typeof(RoomJoinFailureReason), failureReason)
                || (failureReason == RoomJoinFailureReason.None && (listing == null || endpoint == null || listing.RoomCode != endpoint.RoomCode))
                || (failureReason != RoomJoinFailureReason.None && (listing != null || endpoint != null))) throw new ArgumentException("Invalid resolution.");
            FailureReason = failureReason; Listing = listing; Endpoint = endpoint;
        }
    }

    public interface IRoomDirectoryService : IDisposable
    {
        bool IsAvailable { get; }
        void PublishRoom(RoomListingInfo listing, RoomResolvedEndpoint endpoint);
        void RemoveRoom(string roomInstanceId);
        IReadOnlyList<RoomListingInfo> GetPublicRooms();
        RoomJoinResolution ResolveRoomCode(string rawCode);
        void Refresh();
        void Tick();
    }

    /// <summary>Receipt-time TTL and per-instance revision gate; UTC is display metadata only.</summary>
    public sealed class RoomDirectory
    {
        private sealed class Entry
        {
            public RoomListingInfo Listing;
            public RoomResolvedEndpoint Endpoint;
            public long Revision;
            public double Expires;
        }
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private readonly Func<double> now;
        private readonly double ttl;
        private double lastTime;
        public RoomDirectory(Func<double> clock, double ttlSeconds = 6)
        {
            now = clock ?? throw new ArgumentNullException(nameof(clock));
            if (double.IsNaN(ttlSeconds) || double.IsInfinity(ttlSeconds) || ttlSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(ttlSeconds));
            ttl = ttlSeconds;
        }
        public bool PublishRoom(RoomListingInfo listing, RoomResolvedEndpoint endpoint, long revision)
        {
            if (listing == null || endpoint == null || listing.RoomCode != endpoint.RoomCode || revision <= 0) throw new ArgumentException("Invalid listing envelope.");
            Prune();
            if (entries.TryGetValue(endpoint.RoomInstanceId, out var old) && revision <= old.Revision) return false;
            if (entries.Count >= 256 && !entries.ContainsKey(endpoint.RoomInstanceId)) return false;
            entries[endpoint.RoomInstanceId] = new Entry { Listing = listing, Endpoint = endpoint, Revision = revision, Expires = Now() + ttl };
            return true;
        }
        public bool RemoveRoom(string instance, long revision)
        {
            Prune();
            if (!Guid.TryParseExact(instance, "N", out _) || revision <= 0) throw new ArgumentException("Invalid removal.");
            if (!entries.TryGetValue(instance, out var entry))
            {
                if (entries.Count >= 256) return false;
                entries[instance] = new Entry { Revision = revision, Expires = Now() + ttl };
                return true;
            }
            if (revision <= entry.Revision) return false;
            entry.Listing = null; entry.Revision = revision; entry.Expires = Now() + ttl;
            return true;
        }
        public IReadOnlyList<RoomListingInfo> GetPublicRooms()
        {
            Prune();
            return entries.Values.Where(e => e.Listing != null).GroupBy(e => e.Listing.RoomCode).Where(g => g.Count() == 1)
                .Select(g => g.First().Listing).Where(r => r.IsPublic).OrderBy(r => r.RoomCode, StringComparer.Ordinal).Take(64).ToArray();
        }
        public RoomJoinResolution ResolveRoomCode(string rawCode)
        {
            var code = RoomCodeGenerator.Normalize(rawCode);
            if (!RoomCodeGenerator.IsValid(code)) return new RoomJoinResolution(RoomJoinFailureReason.InvalidCodeFormat);
            Prune(); var matches = entries.Values.Where(e => e.Listing != null && e.Listing.RoomCode == code).ToArray();
            if (matches.Length == 0) return new RoomJoinResolution(RoomJoinFailureReason.RoomNotFound);
            if (matches.Length != 1) return new RoomJoinResolution(RoomJoinFailureReason.DirectoryUnavailable);
            var room = matches[0];
            if (room.Listing.Phase != RoomDirectoryPhase.Lobby) return new RoomJoinResolution(RoomJoinFailureReason.MatchAlreadyInProgress);
            if (room.Listing.CurrentPlayers >= room.Listing.MaxPlayers) return new RoomJoinResolution(RoomJoinFailureReason.RoomFull);
            return new RoomJoinResolution(RoomJoinFailureReason.None, room.Listing, room.Endpoint);
        }
        private double Now()
        {
            var value = now();
            if (double.IsNaN(value) || double.IsInfinity(value) || value < lastTime) throw new InvalidOperationException("Invalid directory clock.");
            lastTime = value;
            return value;
        }
        public bool ClaimLost(string code, string instance)
        {
            Prune();
            return entries.Any(p => p.Value.Listing?.RoomCode == code && string.CompareOrdinal(p.Key, instance) < 0);
        }
        private void Prune()
        {
            var time = Now();
            foreach (var id in entries.Where(p => time >= p.Value.Expires).Select(p => p.Key).ToArray()) entries.Remove(id);
        }
    }
}
