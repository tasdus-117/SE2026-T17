using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SmallArena.Domain;

namespace SmallArena.Networking
{
    /// <summary>IPv4 multicast with loopback delivers to every local listener; pumped on the caller's thread.</summary>
    public sealed class LanRoomDirectoryService : IRoomDirectoryService
    {
        public const ushort DefaultDiscoveryPort = 47777;
        private const uint Protocol = 0x31445241; // ARD1; unrelated to the KCP gameplay wire.
        private readonly UdpClient socket;
        private readonly IPEndPoint multicast, broadcast;
        private readonly Func<double> now;
        private readonly RoomDirectory cache;
        private RoomListingInfo hosted;
        private RoomResolvedEndpoint endpoint;
        private long revision;
        private double nextHeartbeat, nextQuery, nextReply;
        public bool IsAvailable { get; private set; } = true;
        public bool HostedClaimLost => endpoint != null && cache.ClaimLost(hosted.RoomCode, endpoint.RoomInstanceId);
        public LanRoomDirectoryService(ushort discoveryPort = DefaultDiscoveryPort, Func<double> clock = null, double ttlSeconds = 6)
        {
            if (discoveryPort == 0) throw new ArgumentOutOfRangeException(nameof(discoveryPort));
            var watch = Stopwatch.StartNew(); now = clock ?? (() => watch.Elapsed.TotalSeconds);
            cache = new RoomDirectory(now, ttlSeconds);
            var group = IPAddress.Parse("239.255.42.99");
            multicast = new IPEndPoint(group, discoveryPort);
            broadcast = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
            socket = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                socket.ExclusiveAddressUse = false;
                socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
                socket.JoinMulticastGroup(group);
                socket.MulticastLoopback = true;
                socket.EnableBroadcast = true;
                socket.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
                socket.Client.Blocking = false;
            }
            catch { socket.Close(); throw; }
        }
        public void PublishRoom(RoomListingInfo listing, RoomResolvedEndpoint location)
        {
            if (!IsAvailable) throw new InvalidOperationException("Directory unavailable.");
            if (listing == null || location == null || listing.RoomCode != location.RoomCode) throw new ArgumentException("Invalid publication.");
            if (endpoint != null && endpoint.RoomInstanceId != location.RoomInstanceId) RemoveRoom(endpoint.RoomInstanceId);
            hosted = listing; endpoint = location;
            Publish();
        }
        private void Publish()
        {
            if (hosted == null) return;
            hosted = new RoomListingInfo(hosted.RoomCode, hosted.RoomName, hosted.HostDisplayName, hosted.CurrentPlayers,
                hosted.Phase, hosted.IsPublic, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            revision = checked(revision + 1);
            cache.PublishRoom(hosted, endpoint, revision);
            Send(Encode(2)); nextHeartbeat = now() + 2;
        }
        public void RemoveRoom(string roomInstanceId)
        {
            if (endpoint == null || endpoint.RoomInstanceId != roomInstanceId) return;
            revision = checked(revision + 1);
            cache.RemoveRoom(roomInstanceId, revision);
            if (IsAvailable) Send(Encode(3));
            hosted = null; endpoint = null;
        }
        public void Refresh()
        {
            if (!IsAvailable || now() < nextQuery) return;
            nextQuery = now() + 1; Send(Encode(1));
        }
        public void Tick()
        {
            if (!IsAvailable) return;
            try
            {
                for (var budget = 0; budget < 128 && socket.Available > 0; budget++)
                {
                    IPEndPoint sender = null; var bytes = socket.Receive(ref sender);
                    if (bytes.Length > 1200) continue;
                    try { Read(bytes, sender); }
                    catch (Exception error) when (error is IOException || error is ArgumentException || error is OverflowException || error is FormatException) { }
                }
                if (hosted != null && now() >= nextHeartbeat) Publish();
            }
            catch (SocketException) { IsAvailable = false; socket.Close(); }
        }
        public IReadOnlyList<RoomListingInfo> GetPublicRooms() { Tick(); return cache.GetPublicRooms(); }
        public RoomJoinResolution ResolveRoomCode(string rawCode)
        {
            if (!RoomCodeGenerator.IsValid(RoomCodeGenerator.Normalize(rawCode))) return new RoomJoinResolution(RoomJoinFailureReason.InvalidCodeFormat);
            if (!IsAvailable) return new RoomJoinResolution(RoomJoinFailureReason.DirectoryUnavailable);
            Tick();
            return IsAvailable ? cache.ResolveRoomCode(rawCode) : new RoomJoinResolution(RoomJoinFailureReason.DirectoryUnavailable);
        }
        private byte[] Encode(byte kind)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Protocol); writer.Write(kind);
                if (kind != 1)
                {
                    writer.Write(endpoint.RoomInstanceId); writer.Write(revision);
                    if (kind == 2)
                    {
                        writer.Write(hosted.RoomCode); writer.Write(hosted.RoomName); writer.Write(hosted.HostDisplayName);
                        writer.Write(hosted.CurrentPlayers); writer.Write((byte)hosted.Phase); writer.Write(hosted.IsPublic);
                        writer.Write(hosted.LastHeartbeatUtcMs); writer.Write(endpoint.Port);
                    }
                }
                return stream.ToArray();
            }
        }
        private void Read(byte[] bytes, IPEndPoint sender)
        {
            using (var reader = new BinaryReader(new MemoryStream(bytes), new UTF8Encoding(false, true)))
            {
                if (reader.ReadUInt32() != Protocol) return;
                var kind = reader.ReadByte();
                if (kind == 1)
                {
                    if (reader.BaseStream.Position != bytes.Length) return;
                    if (hosted != null && now() >= nextReply) { nextReply = now() + 1; Publish(); }
                    return;
                }
                if (kind != 2 && kind != 3) return;
                var id = reader.ReadString(); var version = reader.ReadInt64();
                if (!Guid.TryParseExact(id, "N", out _) || version <= 0) return;
                if (kind == 3)
                {
                    if (reader.BaseStream.Position == bytes.Length) cache.RemoveRoom(id, version);
                    return;
                }
                var listing = new RoomListingInfo(reader.ReadString(), reader.ReadString(), reader.ReadString(),
                    reader.ReadInt32(), (RoomDirectoryPhase)reader.ReadByte(), reader.ReadBoolean(), reader.ReadInt64());
                var location = new RoomResolvedEndpoint(sender.Address.ToString(), reader.ReadUInt16(), listing.RoomCode, id);
                if (reader.BaseStream.Position == bytes.Length) cache.PublishRoom(listing, location, version);
            }
        }
        private void Send(byte[] bytes)
        {
            try { socket.Send(bytes, bytes.Length, multicast); }
            catch (SocketException) { IsAvailable = false; socket.Close(); return; }
            // Broadcast reaches LAN peers whose multicast routing is unavailable. Duplicate revisions are ignored.
            try { socket.Send(bytes, bytes.Length, broadcast); }
            catch (SocketException) { /* Multicast remains the local discovery path. */ }
        }
        public void Dispose()
        {
            if (IsAvailable && endpoint != null) RemoveRoom(endpoint.RoomInstanceId);
            IsAvailable = false; socket.Close();
        }
    }
}
