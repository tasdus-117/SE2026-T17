#if UNITY_EDITOR
using System;
using Mirror;
using SmallArena.Client.UI;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    public static class MatchLifecycleUIBuilder
    {
        public static void Build()
        {
            const string statePath = "Assets/_Project/Prefabs/Network/MatchState.prefab";
            var state = new GameObject("MatchState");
            state.AddComponent<NetworkIdentity>(); state.AddComponent<NetworkMatchController>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(state, statePath); Object.DestroyImmediate(state);
            var identity = new SerializedObject(prefab.GetComponent<NetworkIdentity>());
            identity.FindProperty("_assetId").uintValue = NetworkIdentity.AssetGuidToUint(new Guid(AssetDatabase.AssetPathToGUID(statePath)));
            identity.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SavePrefabAsset(prefab);
            const string rootPath = "Assets/_Project/Prefabs/Network/ArenaNetworkRoot.prefab";
            var root = PrefabUtility.LoadPrefabContents(rootPath);
            var controller = root.GetComponent<ServerMatchLifecycleController>() ?? root.AddComponent<ServerMatchLifecycleController>();
            Set(controller, "matchStatePrefab", prefab);
            var manager = root.GetComponent<ArenaNetworkManager>();
            if (!manager.spawnPrefabs.Contains(prefab)) manager.spawnPrefabs.Add(prefab);
            PrefabUtility.SaveAsPrefabAsset(root, rootPath); PrefabUtility.UnloadPrefabContents(root);
            const string hudPath = "Assets/_Project/Prefabs/UI/NetworkHUD.prefab";
            var hud = PrefabUtility.LoadPrefabContents(hudPath);
            var view = hud.GetComponent<MatchLifecycleHudView>() ?? hud.AddComponent<MatchLifecycleHudView>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var names = new[] { "MatchClock", "MatchPhase", "RespawnCountdown" };
            var fields = new[] { "clockText", "phaseText", "respawnText" };
            for (var i = 0; i < names.Length; i++)
            {
                var old = hud.transform.Find(names[i]); if (old != null) Object.DestroyImmediate(old.gameObject);
                var text = OfflineMatchSceneBuilder.Text(names[i], hud.transform, font, "", i == 0 ? 40 : 32, Color.white,
                    new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -40 - i * 52), new Vector2(650, 50));
                text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
                Set(view, fields[i], text);
            }
            PrefabUtility.SaveAsPrefabAsset(hud, hudPath); PrefabUtility.UnloadPrefabContents(hud);
            AssetDatabase.SaveAssets();
            foreach (var path in new[] { statePath, statePath + ".meta", rootPath, hudPath })
                System.IO.File.WriteAllLines(path, Array.ConvertAll(System.IO.File.ReadAllLines(path), line => line.TrimEnd()));
            Debug.Log("M3.1 lifecycle state, server root and HUD prefabs saved; scenes preserved.");
        }
        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
