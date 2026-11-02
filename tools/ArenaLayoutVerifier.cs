// Saved-file assertions only; no Unity runtime or asset-import success is implied.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

internal static class ArenaLayoutVerifier
{
    private sealed class Record
    {
        public long Id;
        public string Type;
        public bool Stripped;
        public YamlMappingNode Data;
    }

    private sealed class Asset
    {
        public string Path;
        public Dictionary<long, Record> Records;
        public IEnumerable<Record> OfType(string type) => Records.Values.Where(item => item.Type == type);
        public Record Only(string type) => OfType(type).Single();
        public Record Named(string name) => OfType("GameObject").Where(item => !item.Stripped).Single(item => Text(item.Data, "m_Name") == name);
        public Record Component(Record owner, string type) => Sequence(Field(owner.Data, "m_Component"))
            .Select(node => Records[Id(Field(Map(node), "component"))]).Single(item => item.Type == type);
    }

    private readonly struct Box
    {
        public Box(Vector3 center, Vector3 size) { Min = center - size / 2; Max = center + size / 2; }
        public Vector3 Min { get; }
        public Vector3 Max { get; }
        public Vector3 Size => Max - Min;
        public bool Overlaps(Box other) => Min.X < other.Max.X && Max.X > other.Min.X
            && Min.Y < other.Max.Y && Max.Y > other.Min.Y && Min.Z < other.Max.Z && Max.Z > other.Min.Z;
    }

    private static int assertions;
    private static readonly Dictionary<string, Asset> parsed = new Dictionary<string, Asset>(StringComparer.OrdinalIgnoreCase);

    public static int Main(string[] args)
    {
        try { Verify(System.IO.Path.GetFullPath(args[0]), System.IO.Path.GetFullPath(args[1]), args[2]); return 0; }
        catch (Exception error) { Console.Error.WriteLine("FAIL: " + error.Message); return 1; }
    }

    private static void Verify(string root, string editor, string resultPath)
    {
        var prefix = System.IO.Path.Combine(root, "Assets/_Project");
        var scenePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(prefix, "Scenes/Arena_MVP.unity"));
        var assetsByGuid = Directory.GetFiles(System.IO.Path.Combine(root, "Assets"), "*.meta", SearchOption.AllDirectories)
            .ToDictionary(ReadGuid, path => path.Substring(0, path.Length - 5));
        var scene = Read(scenePath);
        var mainMenuPath = System.IO.Path.Combine(prefix, "Scenes/MainMenu.unity");
        var mainMenu = Read(mainMenuPath);
        var names = new[] { "Player", "Bot", "WeaponPickup" };
        var prefabs = names.ToDictionary(name => name, name => Read(System.IO.Path.Combine(prefix, "Prefabs/" + name + ".prefab")));
        var materialNames = new[] { "PlayerBlue", "BotRed", "PickupGold", "FloorSlate", "WallGray" };
        foreach (var name in materialNames) Read(System.IO.Path.Combine(prefix, "Art/Materials/" + name + ".mat"));

        var packageRoot = System.IO.Path.Combine(editor, "Data/Resources/PackageManager/BuiltInPackages");
        var packageReferences = new[]
        {
            "com.unity.render-pipelines.universal/Shaders/Lit.shader.meta",
            "com.unity.render-pipelines.universal/Runtime/UniversalAdditionalCameraData.cs.meta",
            "com.unity.render-pipelines.universal/Runtime/UniversalAdditionalLightData.cs.meta",
            "com.unity.render-pipelines.universal/Editor/AssetVersion.cs.meta",
            "com.unity.ugui/Runtime/TMP/TextMeshProUGUI.cs.meta",
            "com.unity.ugui/Runtime/UGUI/UI/Core/Layout/CanvasScaler.cs.meta",
            "com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs.meta",
            "com.unity.ugui/Runtime/UGUI/UI/Core/Button.cs.meta",
            "com.unity.ugui/Runtime/UGUI/UI/Core/GraphicRaycaster.cs.meta",
            "com.unity.ugui/Runtime/UGUI/EventSystem/EventSystem.cs.meta",
            "com.unity.render-pipelines.core/Runtime/Volume/Volume.cs.meta"
        }.Select(path => ReadGuid(System.IO.Path.Combine(packageRoot, path))).ToHashSet();
        var packageCache = System.IO.Path.Combine(root, "Library/PackageCache");
        var inputModuleGuid = ReadGuid(Directory.GetFiles(packageCache, "InputSystemUIInputModule.cs.meta", SearchOption.AllDirectories).Single());
        var uiActionsGuid = ReadGuid(Directory.GetFiles(packageCache, "DefaultInputActions.inputactions.meta", SearchOption.AllDirectories).Single());
        packageReferences.Add(inputModuleGuid);
        packageReferences.Add(uiActionsGuid);
        var litGuid = ReadGuid(System.IO.Path.Combine(packageRoot, "com.unity.render-pipelines.universal/Shaders/Lit.shader.meta"));
        foreach (var asset in parsed.Values.ToArray()) ValidateReferences(asset, assetsByGuid, packageReferences);

        var tagManager = Read(System.IO.Path.Combine(root, "ProjectSettings/TagManager.asset")).Only("TagManager");
        var layers = Sequence(Field(tagManager.Data, "layers")).Select(Scalar).ToArray();
        var expectedLayers = new Dictionary<string, int> { { "Environment", 6 }, { "Player", 7 }, { "Bot", 8 }, { "Pickup", 9 } };
        foreach (var pair in expectedLayers) Check(layers[pair.Value] == pair.Key, "Named layer slot mismatch: " + pair.Key);

        var allowedTypes = new HashSet<string> { "GameObject", "Transform", "MeshFilter", "MeshRenderer", "CharacterController", "CapsuleCollider", "BoxCollider", "Rigidbody", "MonoBehaviour" };
        foreach (var name in names)
        {
            var prefab = prefabs[name];
            Check(prefab.OfType("GameObject").Count() == 1 && prefab.OfType("Transform").Count() == 1, "Expected one primitive root per prefab.");
            Check(prefab.Records.Values.All(item => allowedTypes.Contains(item.Type)), "Non-structural component in " + name);
            var expectedTypes = new HashSet<string> { "GameObject", "Transform", "MeshFilter", "MeshRenderer" };
            expectedTypes.Add(name == "Player" ? "CharacterController" : name == "Bot" ? "CapsuleCollider" : "BoxCollider");
            if (name == "WeaponPickup") expectedTypes.Add("Rigidbody");
            expectedTypes.Add("MonoBehaviour");
            var expectedCount = expectedTypes.Count + (name == "Player" ? 2 : 0);
            Check(prefab.Records.Count == expectedCount && expectedTypes.SetEquals(prefab.Records.Values.Select(item => item.Type)), "Unexpected/duplicate prefab component.");
            var owner = prefab.Named(name);
            var layerName = name == "WeaponPickup" ? "Pickup" : name;
            Check(Number(owner.Data, "m_Layer") == expectedLayers[layerName], "Wrong entity layer: " + name);
            Check(Number(owner.Data, "m_IsActive") == 1, "Inactive prefab: " + name);
            var transform = prefab.Component(owner, "Transform");
            foreach (var component in prefab.Records.Values.Where(item => item.Type != "GameObject"))
                Check(Id(Field(component.Data, "m_GameObject")) == owner.Id, "Component ownership mismatch.");
            Check(Id(Field(transform.Data, "m_Father")) == 0, "Prefab root must have no parent.");
            IdentityRotation(transform);
            Check(Vector(transform.Data, "m_LocalPosition") == Vector3.Zero, "Prefab must store origin-relative pose.");
            var mesh = prefab.Component(owner, "MeshFilter");
            Check(Id(Field(mesh.Data, "m_Mesh")) == (name == "WeaponPickup" ? 10202 : 10208), "Incorrect built-in mesh: " + name);
            var renderer = prefab.Component(owner, "MeshRenderer");
            var materialRef = Sequence(Field(renderer.Data, "m_Materials")).Single();
            var material = Read(assetsByGuid[Text(Map(materialRef), "guid")]).Only("Material");
            Check(Text(Map(Field(material.Data, "m_Shader")), "guid") == litGuid, "Placeholder material must use URP Lit.");
            Check(Text(material.Data, "m_Name") == (name == "Player" ? "PlayerBlue" : name == "Bot" ? "BotRed" : "PickupGold"), "Entity material identity mismatch.");
            Check(prefab.OfType("MeshRenderer").Count() == 1 && prefab.OfType("MeshFilter").Count() == 1, "Primitive render components missing.");
        }
        var player = prefabs["Player"];
        var controller = player.Only("CharacterController");
        var inputGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Client/Input/PlayerInputHandler.cs.meta"));
        var movementGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Client/Input/PlayerController.cs.meta"));
        var inputAdapter = player.OfType("MonoBehaviour").Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == inputGuid);
        var movement = player.OfType("MonoBehaviour").Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == movementGuid);
        Check(Number(inputAdapter.Data, "m_Enabled") == 1 && Number(movement.Data, "m_Enabled") == 1, "Player adapters must be enabled.");
        Check(Id(Field(movement.Data, "characterController")) == controller.Id && Id(Field(movement.Data, "inputHandler")) == inputAdapter.Id, "Player locomotion references are not wired.");
        Check(Text(Map(Field(inputAdapter.Data, "inputActions")), "guid") == ReadGuid(System.IO.Path.Combine(prefix, "Input/SmallArena.inputactions.meta")), "Input action asset is not wired.");
        Near(Number(movement.Data, "moveSpeed"), 5); Near(Number(movement.Data, "jumpHeight"), 1.2);
        Near(Number(movement.Data, "gravity"), -20); Near(Number(movement.Data, "groundedGravity"), -2);
        Check(!player.OfType("Rigidbody").Any() && !player.OfType("CapsuleCollider").Any(), "Player must have no Rigidbody or duplicate collider.");
        Near(Number(controller.Data, "m_Height"), 2); Near(Number(controller.Data, "m_Radius"), 0.5);
        Near(Number(controller.Data, "m_SlopeLimit"), 45); Near(Number(controller.Data, "m_StepOffset"), 0.3);
        Near(Number(controller.Data, "m_SkinWidth"), 0.08); Near(Number(controller.Data, "m_MinMoveDistance"), 0.001);
        Check(Number(controller.Data, "m_IsTrigger") == 0 && Number(controller.Data, "m_Enabled") == 1, "Player collision must be solid and enabled.");
        var bot = prefabs["Bot"];
        var healthGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Server/Combat/Health.cs.meta"));
        var combatGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Player/CombatController.cs.meta"));
        var health = bot.OfType("MonoBehaviour").Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == healthGuid);
        var combat = player.OfType("MonoBehaviour").Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == combatGuid);
        Check(Number(health.Data, "m_Enabled") == 1 && Number(combat.Data, "m_Enabled") == 1, "Combat and Health must be enabled.");
        Near(Number(health.Data, "maxHealth"), 10); Near(Number(health.Data, "currentHealth"), 10);
        Check(Id(Field(combat.Data, "inputHandler")) == inputAdapter.Id, "Combat input reference is not wired.");
        Check(Number(Map(Field(combat.Data, "targetLayerMask")), "m_Bits") == (1 << expectedLayers["Bot"]), "Punches must target only the Bot layer.");
        Near(Number(combat.Data, "punchCooldown"), 0.5); Near(Number(combat.Data, "punchDamage"), 1);
        Near(Number(combat.Data, "hitSphereRadius"), 0.6);
        Check(Vector(combat.Data, "hitSphereOffset") == new Vector3(0, 1, 0.8f), "Unexpected punch sphere offset.");
        Check(!bot.OfType("Rigidbody").Any(), "Bot collider must be static.");
        var botCollider = bot.Only("CapsuleCollider");
        Check(Number(botCollider.Data, "m_IsTrigger") == 0 && Number(botCollider.Data, "m_Enabled") == 1, "Bot collider must be solid.");
        Near(Number(botCollider.Data, "m_Height"), 2); Near(Number(botCollider.Data, "m_Radius"), 0.5);
        var pickup = prefabs["WeaponPickup"];
        var pickupGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Pickups/WeaponPickup.cs.meta"));
        var pickupComponent = pickup.OfType("MonoBehaviour").Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == pickupGuid);
        Check(Number(pickupComponent.Data, "m_Enabled") == 1, "WeaponPickup must be enabled.");
        Near(Number(pickupComponent.Data, "interactionRadius"), 1.5); Near(Number(pickupComponent.Data, "buffedDamage"), 2);
        Check(Id(Field(pickupComponent.Data, "pickupCollider")) == pickup.Only("BoxCollider").Id, "Pickup collider reference is not wired.");
        Check(Id(Field(pickupComponent.Data, "pickupRenderer")) == pickup.Only("MeshRenderer").Id, "Pickup renderer reference is not wired.");
        Check(Number(pickup.Only("BoxCollider").Data, "m_IsTrigger") == 1, "Pickup collider must be a trigger.");
        Check(Number(pickup.Only("BoxCollider").Data, "m_Enabled") == 1, "Pickup trigger must be enabled.");
        Check(Number(pickup.Only("Rigidbody").Data, "m_IsKinematic") == 1, "Pickup body must be kinematic.");
        Check(Number(pickup.Only("Rigidbody").Data, "m_UseGravity") == 0, "Pickup gravity must be disabled.");

        var geometry = scene.Named("ArenaGeometry");
        var parent = scene.Component(geometry, "Transform");
        IdentityRotation(parent);
        Check(Vector(parent.Data, "m_LocalPosition") == Vector3.Zero && Vector(parent.Data, "m_LocalScale") == Vector3.One, "Geometry parent must be identity.");
        var geometryNames = new[] { "Floor", "WallNorth", "WallSouth", "WallEast", "WallWest" };
        Check(scene.OfType("BoxCollider").Count() == 5 && !scene.OfType("Rigidbody").Any(), "Expected floor and four static walls only.");
        var geometryBoxes = new Dictionary<string, Box>();
        foreach (var name in geometryNames)
        {
            var owner = scene.Named(name);
            Check(Number(owner.Data, "m_Layer") == expectedLayers["Environment"], "Wrong geometry layer: " + name);
            var transform = scene.Component(owner, "Transform");
            Check(Id(Field(transform.Data, "m_Father")) == parent.Id, "Geometry parent mismatch.");
            Check(Sequence(Field(parent.Data, "m_Children")).Any(item => Id(item) == transform.Id), "Missing parent child link.");
            var collider = scene.Component(owner, "BoxCollider");
            Check(Number(collider.Data, "m_IsTrigger") == 0 && Number(collider.Data, "m_Enabled") == 1, "Geometry must be solid and enabled.");
            geometryBoxes[name] = Bounds(transform, collider, Vector3.Zero);
        }
        var floor = geometryBoxes["Floor"];
        Near(floor.Min.X, -8); Near(floor.Max.X, 8); Near(floor.Min.Z, -8); Near(floor.Max.Z, 8); Near(floor.Max.Y, 0);
        foreach (var name in geometryNames.Skip(1))
        {
            var wall = geometryBoxes[name];
            Near(wall.Min.Y, 0);
            Check(wall.Size.Y >= 3, "Wall is shorter than 3m.");
            Check(wall.Size.Y > 1.2 + Number(controller.Data, "m_StepOffset") + Number(controller.Data, "m_SkinWidth"), "Insufficient jump/step margin.");
            if (name == "WallNorth" || name == "WallSouth")
            {
                Near(name == "WallNorth" ? wall.Min.Z : wall.Max.Z, name == "WallNorth" ? 8 : -8);
                Check(wall.Min.X <= floor.Min.X && wall.Max.X >= floor.Max.X, "Wall does not span arena width.");
            }
            else
            {
                Near(name == "WallEast" ? wall.Min.X : wall.Max.X, name == "WallEast" ? 8 : -8);
                Check(wall.Min.Z <= floor.Min.Z && wall.Max.Z >= floor.Max.Z, "Wall does not span arena depth.");
            }
        }
        foreach (var northSouth in new[] { "WallNorth", "WallSouth" })
            foreach (var eastWest in new[] { "WallEast", "WallWest" })
                Check(geometryBoxes[northSouth].Overlaps(geometryBoxes[eastWest]), "Open corner seam.");

        var instances = scene.OfType("PrefabInstance").ToArray();
        Check(instances.Length == 3, "Expected exactly three prefab instances.");
        Check(!scene.OfType("GameObject").Where(item => !item.Stripped).Any(item => names.Contains(Text(item.Data, "m_Name"))), "Entities must be linked prefab instances, not loose duplicates.");
        Check(!scene.OfType("GameObject").Where(item => !item.Stripped).Any(item => new[] { expectedLayers["Player"], expectedLayers["Bot"], expectedLayers["Pickup"] }.Contains((int)Number(item.Data, "m_Layer"))), "Loose duplicate on an entity layer.");
        var spawns = new Dictionary<string, Vector3> { { "Player", new Vector3(0, 1, -4) }, { "Bot", new Vector3(0, 1, 4) }, { "WeaponPickup", new Vector3(-3, 0.3f, 0) } };
        var entityBoxes = new Dictionary<string, Box>();
        var entityObjects = new Dictionary<string, long>();
        var roots = Sequence(Field(scene.Only("SceneRoots").Data, "m_Roots")).Select(Id)
            .Select(id => scene.Records[id].Stripped ? Id(Field(scene.Records[id].Data, "m_PrefabInstance")) : id).ToHashSet();
        var expectedRoots = scene.OfType("Transform").Concat(scene.OfType("RectTransform"))
            .Where(item => !item.Stripped && Id(Field(item.Data, "m_Father")) == 0).Select(item => item.Id)
            .Concat(instances.Select(item => item.Id));
        Check(roots.SetEquals(expectedRoots), "SceneRoots must include every root transform or prefab instance exactly once.");
        foreach (var name in names)
        {
            var prefab = prefabs[name];
            var guid = ReadGuid(prefab.Path + ".meta");
            var instance = instances.Single(item => Text(Map(Field(item.Data, "m_SourcePrefab")), "guid") == guid);
            Check(Id(Field(instance.Data, "m_SourcePrefab")) == 100100000, "Invalid source prefab asset reference.");
            var modification = Map(Field(instance.Data, "m_Modification"));
            Check(Id(Field(modification, "m_TransformParent")) == 0, "Entity must be a scene root.");
            foreach (var key in new[] { "m_RemovedComponents", "m_RemovedGameObjects", "m_AddedGameObjects", "m_AddedComponents" })
                Check(!Sequence(Field(modification, key)).Any(), "Unexpected structural prefab override.");
            var changes = Sequence(Field(modification, "m_Modifications")).Select(Map).ToArray();
            Check(changes.Length == 3, "Only XYZ spawn overrides are allowed.");
            var transform = prefab.Only("Transform");
            foreach (var change in changes) Check(Id(Field(change, "target")) == transform.Id && Text(Map(Field(change, "target")), "guid") == guid, "Invalid prefab override target.");
            float Coordinate(string axis) => (float)Number(changes.Single(item => Text(item, "propertyPath") == "m_LocalPosition." + axis), "value");
            var position = new Vector3(Coordinate("x"), Coordinate("y"), Coordinate("z"));
            Check(Vector3.Distance(position, spawns[name]) < 0.00001, "Unexpected spawn: " + name);
            Check(roots.Contains(instance.Id), "Prefab instance is missing from SceneRoots.");
            foreach (var strippedTransform in scene.OfType("Transform").Where(item => item.Stripped && Id(Field(item.Data, "m_PrefabInstance")) == instance.Id))
                Check(Id(Field(strippedTransform.Data, "m_CorrespondingSourceObject")) == transform.Id, "Stripped transform source mismatch.");
            var strippedObject = scene.OfType("GameObject").Single(item => item.Stripped && Id(Field(item.Data, "m_PrefabInstance")) == instance.Id);
            Check(Id(Field(strippedObject.Data, "m_CorrespondingSourceObject")) == prefab.Only("GameObject").Id,
                "Stripped GameObject source mismatch.");
            Check(Text(Map(Field(strippedObject.Data, "m_CorrespondingSourceObject")), "guid") == guid,
                "Stripped GameObject prefab GUID mismatch.");
            entityObjects[name] = strippedObject.Id;
            var collider = prefab.Records.Values.Single(item => item.Type == "CharacterController" || item.Type == "CapsuleCollider" || item.Type == "BoxCollider");
            var box = Bounds(transform, collider, position);
            entityBoxes[name] = box;
            Check(box.Min.X > floor.Min.X && box.Max.X < floor.Max.X && box.Min.Z > floor.Min.Z && box.Max.Z < floor.Max.Z, "Spawn outside playable area: " + name);
            Near(box.Min.Y, floor.Max.Y);
            foreach (var wall in geometryNames.Skip(1)) Check(!box.Overlaps(geometryBoxes[wall]), "Spawn intersects wall: " + name);
        }
        for (var first = 0; first < names.Length; first++)
            for (var second = first + 1; second < names.Length; second++)
                Check(!entityBoxes[names[first]].Overlaps(entityBoxes[names[second]]), "Entity spawns overlap.");
        Check(scene.OfType("Camera").Count() == 1 && scene.OfType("Light").Count(item => Number(item.Data, "m_Type") == 1) == 1, "Missing follow camera/directional light.");
        var followGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Client/Presentation/ThirdPersonCameraController.cs.meta"));
        var sceneBehaviours = scene.OfType("MonoBehaviour").Where(item => !item.Stripped).ToArray();
        var occlusionGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Client/Presentation/CameraOcclusionController.cs.meta"));
        var follow = sceneBehaviours.Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == followGuid);
        var occlusion = sceneBehaviours.Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == occlusionGuid);
        var mainCameraObject = scene.Named("Main Camera");
        Check(Id(Field(follow.Data, "m_GameObject")) == mainCameraObject.Id && Id(Field(occlusion.Data, "m_GameObject")) == mainCameraObject.Id, "Follow components must belong to Main Camera.");
        Check(Number(follow.Data, "m_Enabled") == 1 && Number(occlusion.Data, "m_Enabled") == 1, "Follow components must be enabled.");
        var target = scene.Records[Id(Field(follow.Data, "target"))];
        Check(target.Type == "Transform" && target.Stripped && Text(Map(Field(target.Data, "m_CorrespondingSourceObject")), "guid") == ReadGuid(prefabs["Player"].Path + ".meta"), "Follow target must be the Player instance transform.");
        Check(Vector(follow.Data, "followOffset") == new Vector3(0, 4.5f, -6), "Unexpected follow offset.");
        Near(Number(follow.Data, "smoothTime"), 0.15); Near(Number(follow.Data, "pitchAngle"), 30);
        Check(Number(Map(Field(occlusion.Data, "occlusionLayers")), "m_Bits") == (1 << expectedLayers["Environment"]), "Occlusion must affect only the Environment layer.");
        var cameraTransform = scene.Component(mainCameraObject, "Transform");
        Check(Vector(cameraTransform.Data, "m_LocalPosition") == spawns["Player"] + Vector(follow.Data, "followOffset"), "Camera must start at the Player follow offset.");
        var cameraRotation = Map(Field(cameraTransform.Data, "m_LocalRotation"));
        Near(Number(cameraRotation, "x"), Math.Sin(Math.PI / 12)); Near(Number(cameraRotation, "w"), Math.Cos(Math.PI / 12));
        Near(Number(cameraRotation, "y"), 0); Near(Number(cameraRotation, "z"), 0);
        var managerGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Core/GameManager.cs.meta"));
        var managerObject = scene.Named("_GameManager");
        var manager = sceneBehaviours.Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == managerGuid);
        Check(Text(Map(Field(manager.Data, "m_Script")), "guid") == managerGuid, "Coordinator script GUID mismatch.");
        Check(Id(Field(manager.Data, "m_GameObject")) == managerObject.Id && Number(manager.Data, "m_Enabled") == 1,
            "Coordinator ownership/enabled state mismatch.");
        Check(sceneBehaviours.Count(item => Text(Map(Field(item.Data, "m_Script")), "guid") == managerGuid) == 1,
            "Expected exactly one GameManager component.");
        Check(Id(Field(manager.Data, "playerInstance")) == entityObjects["Player"], "GameManager Player binding mismatch.");
        Check(Id(Field(manager.Data, "botInstance")) == entityObjects["Bot"], "GameManager Bot binding mismatch.");
        Check(Id(Field(manager.Data, "pickupInstance")) == entityObjects["WeaponPickup"], "GameManager Pickup binding mismatch.");
        Check(Id(Field(manager.Data, "sceneCamera")) == scene.Only("Camera").Id, "GameManager Camera binding mismatch.");
        var readinessGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Core/OfflineMatchReadinessProbe.cs.meta"));
        var uiGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Client/UI/MatchUI.cs.meta"));
        var readiness = sceneBehaviours.Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == readinessGuid);
        var ui = sceneBehaviours.Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == uiGuid);
        Check(Number(manager.Data, "autoStart") == 1 && Id(Field(manager.Data, "readinessProvider")) == readiness.Id,
            "Offline arena must auto-start through the real readiness provider.");
        Check(Id(Field(ui.Data, "gameManager")) == manager.Id && Id(Field(readiness.Data, "gameManager")) == manager.Id,
            "HUD and readiness manager references must match.");
        Check(Id(Field(readiness.Data, "matchUI")) == ui.Id, "Readiness HUD reference is not wired.");
        foreach (var property in new[] { "botHealth", "playerCombat" })
        {
            var source = scene.Records[Id(Field(ui.Data, property))];
            Check(source.Stripped && source.Type == "MonoBehaviour", "HUD source must reference an inherited prefab component.");
            var reference = Map(Field(source.Data, "m_CorrespondingSourceObject"));
            var expectedPrefab = property == "botHealth" ? bot : player;
            Check(Text(reference, "guid") == ReadGuid(expectedPrefab.Path + ".meta"), "HUD source prefab mismatch.");
            Check(Id(reference) == (property == "botHealth" ? health.Id : combat.Id), "HUD source component mismatch.");
            Check(Id(Field(readiness.Data, property)) == source.Id, "Readiness source binding differs from HUD.");
        }
        var canvasObject = scene.Named("MatchCanvas");
        var canvas = scene.Component(canvasObject, "Canvas");
        Check(Number(canvas.Data, "m_RenderMode") == 0 && Number(canvas.Data, "m_Enabled") == 1, "HUD Canvas must use Screen Space Overlay.");
        Check(Id(Field(ui.Data, "m_GameObject")) == canvasObject.Id, "MatchUI must belong to the Canvas.");
        var scalerGuid = ReadGuid(System.IO.Path.Combine(packageRoot, "com.unity.ugui/Runtime/UGUI/UI/Core/Layout/CanvasScaler.cs.meta"));
        var scaler = sceneBehaviours.Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == scalerGuid);
        var resolution = Map(Field(scaler.Data, "m_ReferenceResolution"));
        Near(Number(scaler.Data, "m_UiScaleMode"), 1); Near(Number(resolution, "x"), 1920); Near(Number(resolution, "y"), 1080);
        var hpLabel = scene.Records[Id(Field(ui.Data, "botHpText"))];
        var damageLabel = scene.Records[Id(Field(ui.Data, "damageText"))];
        var victoryLabel = scene.Records[Id(Field(ui.Data, "victoryText"))];
        Check(Text(hpLabel.Data, "m_text") == "Bot HP: 10 / 10", "Initial HP label is incorrect.");
        Check(Text(damageLabel.Data, "m_text") == "Damage: 1 HP (Unarmed)", "Initial damage label is incorrect.");
        Check(Text(victoryLabel.Data, "m_text") == "VICTORY!", "Victory label is incorrect.");
        Check(Id(Field(ui.Data, "victoryBanner")) == scene.Named("VictoryBanner").Id && Number(scene.Named("VictoryBanner").Data, "m_IsActive") == 0,
            "Victory banner must start hidden.");
        var buttonGuid = ReadGuid(System.IO.Path.Combine(packageRoot, "com.unity.ugui/Runtime/UGUI/UI/Core/Button.cs.meta"));
        foreach (var binding in new[] { "playAgainButton", "exitToMenuButton" })
        {
            var button = scene.Records[Id(Field(ui.Data, binding))];
            Check(Text(Map(Field(button.Data, "m_Script")), "guid") == buttonGuid && Number(button.Data, "m_Interactable") == 1,
                "Victory navigation button is not wired or interactable.");
            var persistent = Map(Field(Map(Field(button.Data, "m_OnClick")), "m_PersistentCalls"));
            Check(!Sequence(Field(persistent, "m_Calls")).Any(), "Victory buttons must use only MatchUI lifecycle-managed listeners.");
        }
        Check(sceneBehaviours.Count(item => Text(Map(Field(item.Data, "m_Script")), "guid") == inputModuleGuid) == 1,
            "Arena needs one Input System UI module for after-match mouse input.");
        Check(sceneBehaviours.All(item => packageReferences.Contains(Text(Map(Field(item.Data, "m_Script")), "guid"))
            || item.Id == manager.Id || item.Id == follow.Id || item.Id == occlusion.Id || item.Id == readiness.Id || item.Id == ui.Id), "Unexpected scene gameplay script.");
        var build = Read(System.IO.Path.Combine(root, "ProjectSettings/EditorBuildSettings.asset")).Only("EditorBuildSettings");
        var buildScenes = Sequence(Field(build.Data, "m_Scenes")).Select(Map).ToArray();
        Check(buildScenes.Length == 3 && Text(buildScenes[0], "path") == "Assets/_Project/Scenes/MainMenu.unity" && Number(buildScenes[0], "enabled") == 1, "MainMenu must be the enabled startup scene.");
        Check(Text(buildScenes[1], "path") == "Assets/_Project/Scenes/Bootstrap.unity" && Number(buildScenes[1], "enabled") == 1, "Bootstrap must be enabled at index 1.");
        Check(Text(buildScenes[2], "path") == "Assets/_Project/Scenes/Arena_MVP.unity" && Number(buildScenes[2], "enabled") == 1, "Arena must be enabled at index 2.");
        Check(Text(buildScenes[2], "guid") == ReadGuid(scenePath + ".meta") && Text(buildScenes[0], "guid") == ReadGuid(mainMenuPath + ".meta"), "Build scene GUID mismatch.");
        var menuControllerGuid = ReadGuid(System.IO.Path.Combine(prefix, "Scripts/Client/UI/MainMenuController.cs.meta"));
        var menuController = mainMenu.OfType("MonoBehaviour").Single(item => Text(Map(Field(item.Data, "m_Script")), "guid") == menuControllerGuid);
        var menuCanvasObject = mainMenu.Named("MainMenuCanvas");
        Check(Id(Field(menuController.Data, "m_GameObject")) == menuCanvasObject.Id && Number(mainMenu.Component(menuCanvasObject, "Canvas").Data, "m_RenderMode") == 0,
            "Main menu controller and overlay Canvas must be wired.");
        foreach (var buttonName in new[] { "SinglePlayerButton", "MultiplayerButton", "TutorialButton", "ExitButton" })
        {
            var owner = mainMenu.Named(buttonName);
            var button = mainMenu.OfType("MonoBehaviour").Single(item => Id(Field(item.Data, "m_GameObject")) == owner.Id && Text(Map(Field(item.Data, "m_Script")), "guid") == buttonGuid);
            var enabled = buttonName == "SinglePlayerButton" || buttonName == "ExitButton";
            Check(Number(button.Data, "m_Interactable") == (enabled ? 1 : 0), "Menu option interactability mismatch.");
            if (enabled)
            {
                var calls = Sequence(Field(Map(Field(Map(Field(button.Data, "m_OnClick")), "m_PersistentCalls")), "m_Calls")).Select(Map).ToArray();
                Check(calls.Length == 1 && Id(Field(calls[0], "m_Target")) == menuController.Id, "Menu button callback target is incorrect.");
                Check(Text(calls[0], "m_MethodName") == (buttonName == "SinglePlayerButton" ? "OnSinglePlayerClicked" : "OnExitClicked"), "Menu button callback method is incorrect.");
            }
        }
        Check(mainMenu.OfType("MonoBehaviour").Count(item => Text(Map(Field(item.Data, "m_Script")), "guid") == inputModuleGuid) == 1,
            "Main menu needs one Input System UI module.");
        File.WriteAllText(resultPath, JsonSerializer.Serialize(new
        {
            AssertionsPassed = assertions, UnityYamlFilesParsed = parsed.Count,
            Scene = "Assets/_Project/Scenes/Arena_MVP.unity", LinkedEntityInstances = instances.Length,
            FloorMetres = "16 x 16", WallHeightMetres = 3,
            GameManagerWiredToPrefabRootsAndCamera = true, OfflineAutoStart = true, FullReadinessProviderAssigned = true, HudWired = true,
            Spawns = spawns.ToDictionary(pair => pair.Key, pair => new[] { pair.Value.X, pair.Value.Y, pair.Value.Z }),
            VerificationMode = "Saved-file references and geometry only; Unity import and native physics require licensed PlayMode execution."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS: {assertions} saved-layout assertions; {parsed.Count} Unity YAML files; 3 linked prefab instances; 16x16 floor; 3m walls; supported, separate spawns.");
    }

    private static Box Bounds(Record transform, Record collider, Vector3 positionOverride)
    {
        IdentityRotation(transform);
        var scale = Vector(transform.Data, "m_LocalScale");
        Check(scale.X > 0 && scale.Y > 0 && scale.Z > 0, "Scale must be positive.");
        var position = positionOverride + Vector(transform.Data, "m_LocalPosition");
        var size = collider.Type == "BoxCollider" ? Vector(collider.Data, "m_Size")
            : new Vector3((float)Number(collider.Data, "m_Radius") * 2, (float)Number(collider.Data, "m_Height"), (float)Number(collider.Data, "m_Radius") * 2);
        return new Box(position + Vector(collider.Data, "m_Center") * scale, size * scale);
    }

    private static void ValidateReferences(Asset asset, Dictionary<string, string> project, HashSet<string> package)
    {
        void Visit(YamlNode node)
        {
            if (node is YamlSequenceNode sequence) { foreach (var child in sequence.Children) Visit(child); }
            if (!(node is YamlMappingNode mapping)) return;
            if (mapping.Children.TryGetValue(new YamlScalarNode("fileID"), out var reference) && long.Parse(Scalar(reference), CultureInfo.InvariantCulture) != 0)
            {
                if (mapping.Children.TryGetValue(new YamlScalarNode("guid"), out var guidNode))
                {
                    var guid = Scalar(guidNode);
                    Check(project.ContainsKey(guid) || package.Contains(guid) || Regex.IsMatch(guid, "^0000000000000000[ef]000000000000000$"), "Unresolved asset GUID in " + asset.Path + ": " + guid);
                    if (project.ContainsKey(guid)) Check(File.Exists(project[guid]), "Missing referenced asset.");
                }
                else Check(asset.Records.ContainsKey(long.Parse(Scalar(reference), CultureInfo.InvariantCulture)), "Unresolved local fileID in " + asset.Path);
            }
            foreach (var child in mapping.Children.Values) Visit(child);
        }
        foreach (var record in asset.Records.Values) Visit(record.Data);
    }

    private static Asset Read(string path)
    {
        path = System.IO.Path.GetFullPath(path);
        if (parsed.TryGetValue(path, out var existing)) return existing;
        var text = File.ReadAllText(path);
        Check(text.StartsWith("%YAML 1.1"), "Missing Unity YAML header: " + path);
        var headers = Regex.Matches(text, @"(?m)^--- !u!(\d+) &(-?\d+)( stripped)?\r?$");
        text = Regex.Replace(text, @"(?m)^--- !u!(\d+) &(-?\d+)( stripped)?\r?$", "--- !<tag:unity3d.com,2011:$1> &$2");
        var stream = new YamlStream(); using (var reader = new StringReader(text)) stream.Load(reader);
        Check(stream.Documents.Count == headers.Count, "YAML document/header mismatch: " + path);
        var result = new Asset { Path = path, Records = new Dictionary<long, Record>() };
        for (var index = 0; index < headers.Count; index++)
        {
            var mapping = Map(stream.Documents[index].RootNode);
            Check(mapping.Children.Count == 1, "Invalid Unity object document.");
            var id = long.Parse(headers[index].Groups[2].Value, CultureInfo.InvariantCulture);
            var pair = mapping.Children.Single();
            result.Records.Add(id, new Record { Id = id, Type = Scalar(pair.Key), Data = Map(pair.Value), Stripped = headers[index].Groups[3].Success });
        }
        parsed.Add(path, result); return result;
    }
    private static void IdentityRotation(Record transform)
    {
        var rotation = Map(Field(transform.Data, "m_LocalRotation"));
        Near(Number(rotation, "x"), 0); Near(Number(rotation, "y"), 0); Near(Number(rotation, "z"), 0); Near(Number(rotation, "w"), 1);
    }
    private static YamlMappingNode Map(YamlNode node) => node as YamlMappingNode ?? throw new InvalidDataException("Expected YAML mapping.");
    private static IEnumerable<YamlNode> Sequence(YamlNode node) => (node as YamlSequenceNode ?? throw new InvalidDataException("Expected YAML sequence.")).Children;
    private static YamlNode Field(YamlMappingNode node, string name) => node.Children[new YamlScalarNode(name)];
    private static string Scalar(YamlNode node) => (node as YamlScalarNode ?? throw new InvalidDataException("Expected YAML scalar.")).Value ?? string.Empty;
    private static string Text(YamlMappingNode node, string name) => Scalar(Field(node, name));
    private static double Number(YamlMappingNode node, string name) => double.Parse(Text(node, name), CultureInfo.InvariantCulture);
    private static long Id(YamlNode node) => long.Parse(Text(Map(node), "fileID"), CultureInfo.InvariantCulture);
    private static Vector3 Vector(YamlMappingNode node, string name)
    {
        var value = Map(Field(node, name)); return new Vector3((float)Number(value, "x"), (float)Number(value, "y"), (float)Number(value, "z"));
    }
    private static string ReadGuid(string path)
    {
        var match = Regex.Match(File.ReadAllText(path), @"(?mi)^guid: ([0-9a-f]{32})\s*$");
        Check(match.Success, "Invalid GUID metadata: " + path); return match.Groups[1].Value;
    }
    private static void Near(double actual, double expected) => Check(Math.Abs(actual - expected) < 0.00001, $"Expected {expected}, found {actual}.");
    private static void Check(bool condition, string diagnostic) { if (!condition) throw new InvalidDataException(diagnostic); assertions++; }
}
