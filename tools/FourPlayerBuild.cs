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
    public static class FourPlayerBuild
    {
        [Serializable] private sealed class Report
        { public string UnityVersion, Target, Result, Backend; public int Errors, Warnings; public ulong Bytes; public string[] Scenes; }
        public static void Build()
        {
            if (Environment.GetEnvironmentVariable("SMALL_ARENA_FOUR_PLAYER_BUILD") != "1")
                throw new InvalidOperationException("Explicit instrumented build opt-in is required.");
            var root = Directory.GetParent(Application.dataPath).FullName;
            var output = Path.Combine(root, "Builds/WindowsStandalone"); var log = Path.Combine(root, "Logs/WindowsStandalone/build.log");
            Directory.CreateDirectory(Path.GetDirectoryName(log)); File.WriteAllText(log, "Instrumented four-player build; product settings unchanged.\n");
            try
            {
                var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (!paths.SequenceEqual(new[] { "Assets/_Project/Scenes/MainMenu.unity", "Assets/_Project/Scenes/Bootstrap.unity", "Assets/_Project/Scenes/Arena_MVP.unity" }))
                    throw new InvalidOperationException("Unexpected scene order.");
                if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != ScriptingImplementation.Mono2x)
                    throw new InvalidOperationException("Existing Mono backend required; settings will not be changed.");
                if (Directory.Exists(output)) Directory.Delete(output, true);
                Directory.CreateDirectory(output);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = paths, target = BuildTarget.StandaloneWindows64,
                    locationPathName = Path.Combine(output, "SmallArena.exe"), options = BuildOptions.StrictMode,
                    extraScriptingDefines = new[] { "SMALL_ARENA_FOUR_PLAYER_HARNESS" } });
                if (report == null) throw new InvalidOperationException("Missing BuildReport.");
                var s = report.summary;
                File.WriteAllText(Path.Combine(output, "build-report.json"), JsonUtility.ToJson(new Report { UnityVersion = Application.unityVersion,
                    Target = s.platform.ToString(), Result = s.result.ToString(), Backend = "Mono2x", Errors = s.totalErrors,
                    Warnings = s.totalWarnings, Bytes = s.totalSize, Scenes = paths }, true));
                File.AppendAllLines(log, report.steps.SelectMany(step => step.messages).Select(m => m.type + ": " + m.content));
                File.AppendAllText(log, $"{s.result}; errors={s.totalErrors}; warnings={s.totalWarnings}; bytes={s.totalSize}\n");
                if (s.result != BuildResult.Succeeded || s.totalErrors != 0 || s.totalSize == 0) throw new InvalidOperationException("Instrumented build failed.");
            }
            catch (Exception error) { File.AppendAllText(log, error + "\n"); Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
#endif
