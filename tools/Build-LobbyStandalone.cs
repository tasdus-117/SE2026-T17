#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SmallArena.Tools
{
    public static class LobbyStandaloneBuilder
    {
        public static void Build()
        {
            var output = Environment.GetEnvironmentVariable("SMALL_ARENA_M2_BUILD");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Set SMALL_ARENA_M2_BUILD to the verification executable path.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Disabled);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/MainMenu.unity", "Assets/_Project/Scenes/Bootstrap.unity", "Assets/_Project/Scenes/Arena_MVP.unity" },
                locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                extraScriptingDefines = new[] { "SMALL_ARENA_M2_SMOKE" }
            });
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build-result.txt"),
                $"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; bytes={report.summary.totalSize}");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Standalone verification build failed.");
        }
    }
}
#endif
