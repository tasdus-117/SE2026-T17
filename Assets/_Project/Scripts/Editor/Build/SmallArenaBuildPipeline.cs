#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SmallArena.Tools
{
    public static class SmallArenaBuildPipeline
    {
        private static readonly string[] RequiredScenes =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Bootstrap.unity",
            "Assets/_Project/Scenes/Arena_MVP.unity"
        };

        [Serializable]
        private sealed class BuildManifest
        {
            public string UnityVersion;
            public string Target;
            public string Result;
            public string Backend;
            public int Errors;
            public int Warnings;
            public ulong Bytes;
            public string[] Scenes;
        }

        public static void BuildWindowsStandalone()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var builds = Path.GetFullPath(Path.Combine(root, "Builds"));
            var output = Path.GetFullPath(Path.Combine(builds, "WindowsStandalone"));
            if (!output.StartsWith(builds + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Build output must remain inside this project's Builds folder.");
            var log = Path.Combine(root, "Logs/WindowsStandalone/build.log");
            Directory.CreateDirectory(Path.GetDirectoryName(log));
            File.WriteAllText(log, $"Unity {Application.unityVersion}; StandaloneWindows64\n");
            try
            {
                var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
                var paths = scenes.Select(scene => scene.path).ToArray();
                if (!paths.SequenceEqual(RequiredScenes))
                    throw new InvalidOperationException("Enabled scenes must be MainMenu (0), Bootstrap (1), Arena_MVP (2), in that order.");
                foreach (var scene in scenes)
                {
                    if (!File.Exists(scene.path) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null ||
                        AssetDatabase.AssetPathToGUID(scene.path) != scene.guid.ToString())
                        throw new InvalidOperationException("Missing scene or mismatched build-settings GUID: " + scene.path);
                }
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new InvalidOperationException("Install Windows Build Support for this Unity Editor.");
                var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
                if (backend != ScriptingImplementation.Mono2x)
                    throw new InvalidOperationException("Windows cross-build requires the existing Mono backend; build settings will not be changed automatically.");

                // Only the fixed generated output below this project's Builds folder is cleared.
                if (Directory.Exists(output)) Directory.Delete(output, true);
                Directory.CreateDirectory(output);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = paths,
                    target = BuildTarget.StandaloneWindows64,
                    locationPathName = Path.Combine(output, "SmallArena.exe"),
                    options = BuildOptions.StrictMode
                });
                if (report == null) throw new InvalidOperationException("BuildPipeline returned no BuildReport.");
                var summary = report.summary;
                var manifest = new BuildManifest
                {
                    UnityVersion = Application.unityVersion,
                    Target = summary.platform.ToString(),
                    Result = summary.result.ToString(),
                    Backend = backend.ToString(),
                    Errors = summary.totalErrors,
                    Warnings = summary.totalWarnings,
                    Bytes = summary.totalSize,
                    Scenes = paths
                };
                File.WriteAllText(Path.Combine(output, "build-report.json"), JsonUtility.ToJson(manifest, true));
                File.AppendAllLines(log, report.steps.SelectMany(step => step.messages)
                    .Select(message => message.type + ": " + message.content));
                var result = $"{summary.result}; errors={summary.totalErrors}; warnings={summary.totalWarnings}; bytes={summary.totalSize}";
                File.AppendAllText(log, result + "\n");
                Debug.Log(result);
                if (summary.result != BuildResult.Succeeded || summary.totalErrors != 0 || summary.totalSize == 0)
                    throw new InvalidOperationException("Windows standalone build did not succeed. See " + log);
            }
            catch (Exception error)
            {
                File.AppendAllText(log, error + "\n");
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
#endif
