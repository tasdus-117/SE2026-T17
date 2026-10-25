using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Build scripts for CI/CD pipeline
/// </summary>
public class BuildScripts
{
    [MenuItem("Build/Build Server")]
    public static void BuildServer()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = GetScenes();
        buildPlayerOptions.locationPathName = "builds/Server/SmallArena3DServer";
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.EnableHeadlessMode | BuildOptions.CompressWithLz4;
        
        // Server-specific defines
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            BuildTargetGroup.Standalone,
            "SERVER_BUILD"
        );
        
        Debug.Log("Building dedicated server...");
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Server build succeeded: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Server build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
    
    [MenuItem("Build/Build Client (Windows)")]
    public static void BuildClientWindows()
    {
        BuildClient(BuildTarget.StandaloneWindows64, "builds/Client/Windows/SmallArena3D.exe");
    }
    
    [MenuItem("Build/Build Client (Linux)")]
    public static void BuildClientLinux()
    {
        BuildClient(BuildTarget.StandaloneLinux64, "builds/Client/Linux/SmallArena3D");
    }
    
    [MenuItem("Build/Build Client (macOS)")]
    public static void BuildClientMacOS()
    {
        BuildClient(BuildTarget.StandaloneOSX, "builds/Client/macOS/SmallArena3D.app");
    }
    
    public static void BuildClient()
    {
        // Called by CI - determines platform from command line
        string[] args = System.Environment.GetCommandLineArgs();
        BuildTarget target = BuildTarget.StandaloneWindows64;
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-buildTarget" && i + 1 < args.Length)
            {
                target = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), args[i + 1]);
                break;
            }
        }
        
        string extension = target == BuildTarget.StandaloneWindows64 ? ".exe" : "";
        BuildClient(target, $"builds/Client/SmallArena3D{extension}");
    }
    
    private static void BuildClient(BuildTarget target, string locationPath)
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = GetScenes();
        buildPlayerOptions.locationPathName = locationPath;
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = BuildOptions.CompressWithLz4;
        
        // Client-specific defines
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            GetBuildTargetGroup(target),
            "CLIENT_BUILD"
        );
        
        Debug.Log($"Building client for {target}...");
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Client build succeeded: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Client build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
    
    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildServer();
        BuildClientWindows();
        BuildClientLinux();
        BuildClientMacOS();
        Debug.Log("All builds completed!");
    }
    
    private static string[] GetScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }
    
    private static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneOSX:
                return BuildTargetGroup.Standalone;
            default:
                return BuildTargetGroup.Unknown;
        }
    }
}
