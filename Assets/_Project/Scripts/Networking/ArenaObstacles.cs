using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    [Serializable]
    public struct ArenaObstacle
    {
        public PrimitiveType Shape;
        public Vector3 Position;
        public Vector3 Size;
        public Bounds Bounds => new Bounds(Position, Size);
    }

    /// <summary>The server sends complete collider descriptions, including to late joiners.</summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class ArenaObstacles : NetworkBehaviour
    {
        public const float FallHeight = -5f;
        public const float Clearance = 0.35f;
        [SerializeField] private BoxCollider floor = null;
        [SerializeField] private Material material = null;
        [SerializeField] private GameObject[] reservedSpawns = new GameObject[0];
        private readonly SyncList<ArenaObstacle> networkLayout = new SyncList<ArenaObstacle>();
        private List<ArenaObstacle> offlineLayout = new List<ArenaObstacle>();
        public IReadOnlyList<ArenaObstacle> Layout => NetworkServer.active || NetworkClient.active ? networkLayout : offlineLayout;
        public IReadOnlyList<Bounds> ReservedBounds => GetReservations();
        public BoxCollider FloorCollider => floor;

        private void Awake()
        {
            if (!NetworkServer.active && !NetworkClient.active) Generate(false);
        }

        public override void OnStartServer()
        {
            // Awake may have previewed offline geometry when hosting directly in this scene.
            if (networkLayout.Count == 0) Generate(true);
        }

        public override void OnStartClient() => RenderLayout();

        private void Generate(bool networked)
        {
            var generated = CreateLayout(Guid.NewGuid().GetHashCode(), ColliderBounds(floor, floor.transform.position), GetReservations());
            if (networked) foreach (var obstacle in generated) networkLayout.Add(obstacle);
            else offlineLayout = generated;
            RenderLayout();
        }

        private List<Bounds> GetReservations()
        {
            var result = new List<Bounds>();
            foreach (var spawn in reservedSpawns)
            {
                if (spawn == null) continue;
                var collider = spawn.GetComponent<Collider>();
                // Disabled offline roots still reserve their authored collider dimensions.
                if (collider != null) result.Add(ColliderBounds(collider, spawn.transform.position));
                else
                {
                    var prefab = NetworkManager.singleton != null ? NetworkManager.singleton.playerPrefab : null;
                    var playerCollider = prefab != null ? prefab.GetComponent<Collider>() : reservedSpawns[0].GetComponent<Collider>();
                    result.Add(ColliderBounds(playerCollider, spawn.transform.position));
                }
            }
            return result;
        }

        public static Bounds ColliderBounds(Collider collider, Vector3 position)
        {
            Vector3 center, size;
            if (collider is CharacterController character)
            { center = character.center; size = new Vector3(character.radius * 2, character.height, character.radius * 2); }
            else if (collider is BoxCollider box) { center = box.center; size = box.size; }
            else if (collider is CapsuleCollider capsule)
            { center = capsule.center; size = Vector3.one * capsule.radius * 2; size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2); }
            else throw new InvalidOperationException("Unsupported spawn collider: " + collider);
            var scale = collider.transform.lossyScale;
            size = Vector3.Scale(size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            var rotation = collider.transform.rotation;
            var x = rotation * new Vector3(size.x, 0, 0);
            var y = rotation * new Vector3(0, size.y, 0);
            var z = rotation * new Vector3(0, 0, size.z);
            size = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(position + rotation * Vector3.Scale(center, scale), size);
        }

        public static List<ArenaObstacle> CreateLayout(int seed, Bounds surface, IReadOnlyList<Bounds> reservations)
        {
            var random = new System.Random(seed);
            var count = random.Next(3, 7);
            var shapes = new[] { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule };
            var firstShape = random.Next(3);
            var result = new List<ArenaObstacle>();
            for (var index = 0; index < count; index++)
            {
                var shape = shapes[index == 0 ? firstShape : index == 1 ? (firstShape + random.Next(1, 3)) % 3 : random.Next(3)];
                var diameter = 0.9f + (float)random.NextDouble() * 1.1f;
                var height = shape == PrimitiveType.Sphere ? diameter : shape == PrimitiveType.Capsule
                    ? diameter * (1.2f + (float)random.NextDouble()) : 1f + (float)random.NextDouble() * 1.8f;
                var size = new Vector3(diameter, height, shape == PrimitiveType.Cube ? 0.9f + (float)random.NextDouble() * 1.1f : diameter);
                var placed = false;
                for (var attempt = 0; attempt < 4096; attempt++)
                {
                    var available = surface.size - size - Vector3.one * (2 * Clearance);
                    if (available.x <= 0 || available.z <= 0) break;
                    var position = new Vector3(surface.center.x + ((float)random.NextDouble() - 0.5f) * available.x,
                        surface.max.y + height * 0.5f, surface.center.z + ((float)random.NextDouble() - 0.5f) * available.z);
                    var candidate = new ArenaObstacle { Shape = shape, Position = position, Size = size };
                    var padded = candidate.Bounds;
                    padded.Expand(Clearance * 2);
                    var blocked = false;
                    foreach (var reserved in reservations)
                        if (OverlapsHorizontal(padded, reserved)) { blocked = true; break; }
                    foreach (var previous in result)
                        if (OverlapsHorizontal(padded, previous.Bounds)) { blocked = true; break; }
                    if (blocked) continue;
                    result.Add(candidate); placed = true; break;
                }
                if (!placed) throw new InvalidOperationException("Arena cannot fit the requested obstacles with spawn clearance.");
            }
            return result;
        }

        private static bool OverlapsHorizontal(Bounds a, Bounds b) =>
            a.min.x <= b.max.x && a.max.x >= b.min.x && a.min.z <= b.max.z && a.max.z >= b.min.z;

        private void RenderLayout()
        {
            foreach (Transform child in transform)
            { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach (var obstacle in Layout)
            {
                var root = GameObject.CreatePrimitive(obstacle.Shape);
                root.name = "Obstacle_" + obstacle.Shape;
                root.layer = LayerMask.NameToLayer("Environment");
                root.transform.SetParent(transform, false);
                root.transform.position = obstacle.Position;
                root.transform.localScale = obstacle.Shape == PrimitiveType.Capsule
                    ? new Vector3(obstacle.Size.x, obstacle.Size.y / 2, obstacle.Size.z) : obstacle.Size;
                root.GetComponent<Renderer>().sharedMaterial = material;
            }
            Physics.SyncTransforms();
        }
    }
}
