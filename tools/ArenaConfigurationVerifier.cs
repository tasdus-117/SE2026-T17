// Standalone saved-file checks. This tool never enters a Unity gameplay assembly.
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

internal static class ArenaConfigurationVerifier
{
    private static int assertions;
    private static int yamlFiles;
    private static int jsonFiles;

    public static int Main(string[] args)
    {
        try
        {
            Verify(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]), args[2]);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL: " + error.Message);
            return 1;
        }
    }

    private static void Verify(string root, string editor, string reportPath)
    {
        var projectSettings = Path.Combine(root, "ProjectSettings");
        var settings = Path.GetFullPath(Path.Combine(root, "Assets/_Project/Settings"));
        var files = Directory.GetFiles(projectSettings, "*.asset")
            .Concat(Directory.GetFiles(settings, "*.asset", SearchOption.AllDirectories));
        var parsed = new Dictionary<string, YamlStream>();
        foreach (var path in files)
        {
            if (File.ReadAllText(path).TrimStart().StartsWith("{"))
            {
                // Unity's template stores XRSettings.asset as JSON, despite its extension.
                using (var json = JsonDocument.Parse(File.ReadAllText(path)))
                    Check(json.RootElement.ValueKind == JsonValueKind.Object, "Expected JSON settings object.");
                jsonFiles++;
            }
            else parsed.Add(Path.GetFullPath(path), ParseUnityYaml(path));
        }

        YamlMappingNode Project(string file, string name) => Mapping(
            Document(parsed[Path.GetFullPath(Path.Combine(projectSettings, file))], name));
        var tags = Project("TagManager.asset", "TagManager");
        var layers = Sequence(Field(tags, "layers")).Children.Select(Scalar).ToArray();
        Check(layers.Length == 32, "Layer array must contain 32 slots.");
        Check(layers[0] == "Default", "Built-in Default layer was changed.");
        var slots = new Dictionary<string, int>
        {
            { "Environment", 6 }, { "Player", 7 }, { "Bot", 8 }, { "Pickup", 9 }
        };
        foreach (var pair in slots)
        {
            Check(layers[pair.Value] == pair.Key, "Unexpected canonical layer slot: " + pair.Key);
            Check(layers.Count(name => name == pair.Key) == 1, "Duplicate/missing named layer: " + pair.Key);
        }

        var allowed = new Dictionary<string, string[]>
        {
            { "Environment", new[] { "Default", "Environment", "Player", "Bot" } },
            { "Player", new[] { "Default", "Environment", "Bot", "Pickup" } },
            { "Bot", new[] { "Default", "Environment", "Player" } },
            { "Pickup", new[] { "Player" } }
        };
        var physics = Project("DynamicsManager.asset", "PhysicsManager");
        var matrix = Mapping(Field(physics, "m_LayerCollisionMatrix"));
        Check(Scalar(Field(matrix, "_typeName")) == "unsigned int", "Unexpected matrix element type.");
        var hex = Scalar(Field(matrix, "_data"));
        Check(Regex.IsMatch(hex, "\\A[0-9a-fA-F]{256}\\z"), "Expected 128-byte serialized matrix.");
        var bytes = Convert.FromHexString(hex);
        var masks = Enumerable.Range(0, 32)
            .Select(index => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(index * 4, 4))).ToArray();
        bool Enabled(int first, int second) => (masks[first] & (1u << second)) != 0;
        for (var first = 0; first < 32; first++)
        {
            for (var second = 0; second < 32; second++)
            {
                var expected = allowed.TryGetValue(layers[first], out var partners)
                    ? partners.Contains(layers[second])
                    : allowed.TryGetValue(layers[second], out partners)
                        ? partners.Contains(layers[first])
                        : true; // Non-arena pairs retain the all-enabled template defaults.
                Check(Enabled(first, second) == expected, $"Collision policy mismatch: {first}/{second}.");
                Check(Enabled(first, second) == Enabled(second, first), $"Asymmetric matrix: {first}/{second}.");
            }
        }

        var assetsByGuid = new Dictionary<string, string>();
        foreach (var meta in Directory.GetFiles(Path.Combine(root, "Assets"), "*.meta", SearchOption.AllDirectories))
        {
            var guid = ReadGuid(meta);
            Check(!assetsByGuid.ContainsKey(guid), "Duplicate asset GUID: " + meta);
            assetsByGuid.Add(guid, meta.Substring(0, meta.Length - 5));
        }
        string Resolve(YamlNode node)
        {
            var guid = Scalar(Field(Mapping(node), "guid"));
            Check(assetsByGuid.ContainsKey(guid), "Unresolved project asset GUID: " + guid);
            var path = Path.GetFullPath(assetsByGuid[guid]);
            Check(path.StartsWith(settings + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "Render asset must live under Assets/_Project/Settings: " + path);
            Check(File.Exists(path), "Missing render asset: " + path);
            return path;
        }
        var package = Path.Combine(editor, "Data/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.universal");
        var pipelineScriptGuid = ReadGuid(Directory.GetFiles(package, "UniversalRenderPipelineAsset.cs.meta", SearchOption.AllDirectories).Single());
        var rendererScriptGuid = ReadGuid(Path.Combine(package, "Runtime/UniversalRendererData.cs.meta"));
        var validatedPipelines = new HashSet<string>();
        void ValidatePipeline(string path)
        {
            if (!validatedPipelines.Add(path)) return;
            var asset = Mapping(Document(parsed[path], "MonoBehaviour"));
            Check(Scalar(Field(Mapping(Field(asset, "m_Script")), "guid")) == pipelineScriptGuid,
                "Assigned pipeline is not a UniversalRenderPipelineAsset: " + path);
            var renderers = Sequence(Field(asset, "m_RendererDataList"));
            var rendererIndex = Integer(Field(asset, "m_DefaultRendererIndex"));
            Check(rendererIndex >= 0 && rendererIndex < renderers.Children.Count, "Invalid default renderer index.");
            foreach (var reference in renderers.Children)
            {
                var rendererPath = Resolve(reference);
                var renderer = parsed[rendererPath].Documents
                    .Select(document => Mapping(Field(Mapping(document.RootNode), "MonoBehaviour")))
                    .Single(node => Scalar(Field(node, "m_Name")) == Path.GetFileNameWithoutExtension(rendererPath));
                Check(Scalar(Field(Mapping(Field(renderer, "m_Script")), "guid")) == rendererScriptGuid,
                    "Pipeline renderer is not UniversalRendererData: " + rendererPath);
            }
        }

        var graphics = Project("GraphicsSettings.asset", "GraphicsSettings");
        var defaultPipeline = Resolve(Field(graphics, "m_CustomRenderPipeline"));
        ValidatePipeline(defaultPipeline);
        Check(Path.GetFileName(defaultPipeline) == "PC_RPAsset.asset", "Default pipeline must be PC URP.");
        var globalMap = Mapping(Field(graphics, "m_RenderPipelineGlobalSettingsMap"));
        Resolve(Field(globalMap, "UnityEngine.Rendering.Universal.UniversalRenderPipeline"));

        var quality = Project("QualitySettings.asset", "QualitySettings");
        var levels = Sequence(Field(quality, "m_QualitySettings"));
        var current = Integer(Field(quality, "m_CurrentQuality"));
        Check(current >= 0 && current < levels.Children.Count, "Invalid active quality index.");
        foreach (var level in levels.Children)
        {
            ValidatePipeline(Resolve(Field(Mapping(level), "customRenderPipeline")));
        }
        var selected = Mapping(levels.Children[current]);
        var activePipeline = Resolve(Field(selected, "customRenderPipeline"));
        Check(Scalar(Field(selected, "name")) == "PC" && activePipeline == defaultPipeline,
            "Selected quality must use the PC pipeline.");
        Check(Integer(Field(Mapping(Field(quality, "m_PerPlatformDefaultQuality")), "Standalone")) == current,
            "Standalone platform must select the PC quality level.");
        var playerSettings = Project("ProjectSettings.asset", "PlayerSettings");
        Check(Integer(Field(playerSettings, "m_ActiveColorSpace")) == 1, "Color space must be Linear.");

        var names = new[] { "Default", "Environment", "Player", "Bot", "Pickup" };
        var pairs = names.ToDictionary(first => first,
            first => names.ToDictionary(second => second,
                second => Enabled(Array.IndexOf(layers, first), Array.IndexOf(layers, second))));
        var layerMapping = names.ToDictionary(name => name, name => Array.IndexOf(layers, name));
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            LayerMapping = layerMapping,
            CollisionMatrix = pairs,
            MatrixEntriesChecked = 1024,
            SymmetryEntriesChecked = 1024,
            YamlFilesParsed = yamlFiles,
            JsonFilesParsed = jsonFiles,
            AssertionsPassed = assertions,
            ConfiguredActivePipeline = Path.GetRelativePath(root, activePipeline),
            ColorSpace = "Linear",
            VerificationMode = "Saved YAML only; Unity runtime checks require PlayMode execution."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS: {assertions} assertions; {yamlFiles} valid Unity YAML files and {jsonFiles} JSON settings file; 1024 matrix entries and symmetry checked.");
        Console.WriteLine("Layers: " + string.Join(", ", layerMapping.Select(pair => pair.Key + "=" + pair.Value)));
        Console.WriteLine("Configured active pipeline: " + Path.GetRelativePath(root, activePipeline) + "; Linear color space.");
    }

    private static YamlStream ParseUnityYaml(string path)
    {
        var text = File.ReadAllText(path);
        Check(text.StartsWith("%YAML 1.1"), "Missing Unity YAML header: " + path);
        Check(Regex.IsMatch(text, @"(?m)^--- !u!\d+ &-?\d+\r?$"), "Missing Unity object header: " + path);
        // Unity repeats the !u! shorthand across documents without repeating %TAG.
        // Expand only object-header tags to their equivalent verbatim YAML URI.
        text = Regex.Replace(text, @"(?m)^(--- )!u!(\d+)( &-?\d+)", "$1!<tag:unity3d.com,2011:$2>$3");
        var stream = new YamlStream();
        using (var reader = new StringReader(text)) stream.Load(reader);
        Check(stream.Documents.Count > 0, "Empty Unity YAML file: " + path);
        foreach (var document in stream.Documents) Mapping(document.RootNode);
        yamlFiles++;
        return stream;
    }

    private static YamlNode Document(YamlStream stream, string name) => Field(Mapping(stream.Documents.Single().RootNode), name);
    private static YamlMappingNode Mapping(YamlNode node) => node as YamlMappingNode ?? throw new InvalidDataException("Expected YAML mapping.");
    private static YamlSequenceNode Sequence(YamlNode node) => node as YamlSequenceNode ?? throw new InvalidDataException("Expected YAML sequence.");
    private static YamlNode Field(YamlMappingNode node, string name) => node.Children.TryGetValue(new YamlScalarNode(name), out var value)
        ? value : throw new InvalidDataException("Missing YAML field: " + name);
    private static string Scalar(YamlNode node) => node is YamlScalarNode scalar ? scalar.Value ?? string.Empty
        : throw new InvalidDataException("Expected YAML scalar.");
    private static int Integer(YamlNode node) => int.Parse(Scalar(node), System.Globalization.CultureInfo.InvariantCulture);
    private static string ReadGuid(string path)
    {
        var match = Regex.Match(File.ReadAllText(path), @"(?m)^guid: ([0-9a-f]{32})\s*$");
        Check(match.Success, "Invalid GUID sidecar: " + path);
        return match.Groups[1].Value;
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
        assertions++;
    }
}
