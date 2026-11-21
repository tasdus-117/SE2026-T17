#if UNITY_EDITOR
using System;
using SmallArena.Client.UI;
using SmallArena.Server.Sessions;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    public static class MatchScoreUIBuilder
    {
        public static void Build()
        {
            const string path = "Assets/_Project/Prefabs/UI/NetworkHUD.prefab";
            var hud = PrefabUtility.LoadPrefabContents(path);
            foreach (var name in new[] { "ScoreboardPanel", "KillFeed", "MatchResultPanel" })
            { var old = hud.transform.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject); }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var lifecycle = hud.GetComponent<MatchLifecycleHudView>();
            var board = hud.GetComponent<ScoreboardHudView>() ?? hud.AddComponent<ScoreboardHudView>();
            var feed = hud.GetComponent<KillFeedHudView>() ?? hud.AddComponent<KillFeedHudView>();
            var result = hud.GetComponent<MatchResultHudView>() ?? hud.AddComponent<MatchResultHudView>();
            var center = new Vector2(.5f, .5f); var dark = new Color(.035f, .055f, .09f, .96f);
            var panel = OfflineMatchSceneBuilder.Panel("ScoreboardPanel", hud.transform, center, center, Vector2.zero, new Vector2(1080, 390), dark);
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var heading = Text("Heading", panel, font, "SCOREBOARD — HOLD TAB", new Vector2(0, 135), new Vector2(1000, 55), 36);
            heading.alignment = TextAlignmentOptions.Center;
            var rows = Text("Rows", panel, font, "", new Vector2(0, -10), new Vector2(1000, 230), 27);
            Set(board, "lifecycleHud", lifecycle); Set(board, "panel", panel.gameObject); Set(board, "rowsText", rows);
            panel.gameObject.SetActive(false);
            var feedText = OfflineMatchSceneBuilder.Text("KillFeed", hud.transform, font, "", 24, Color.white,
                Vector2.one, Vector2.one, new Vector2(-32, -145), new Vector2(640, 170));
            feedText.alignment = TextAlignmentOptions.TopRight; feedText.raycastTarget = false;
            Set(feed, "lifecycleHud", lifecycle); Set(feed, "feedText", feedText);
            var resultPanel = OfflineMatchSceneBuilder.Panel("MatchResultPanel", hud.transform, center, center, Vector2.zero, new Vector2(1240, 690), dark);
            var title = Text("ResultHeader", resultPanel, font, "", new Vector2(0, 245), new Vector2(1180, 90), 64); title.alignment = TextAlignmentOptions.Center;
            var standings = Text("Standings", resultPanel, font, "", new Vector2(0, 30), new Vector2(1100, 275), 27);
            var countdown = Text("LobbyCountdown", resultPanel, font, "", new Vector2(0, -155), new Vector2(1100, 50), 30); countdown.alignment = TextAlignmentOptions.Center;
            var button = OfflineMatchSceneBuilder.CreateButton("ReturnToLobby", resultPanel, font, "Return to Lobby (Host)", new Vector2(-250, -255), new Vector2(440, 70), new Color(.14f, .35f, .25f));
            var leave = OfflineMatchSceneBuilder.CreateButton("LeaveMatch", resultPanel, font, "Leave Match / Main Menu", new Vector2(250, -255), new Vector2(440, 70), new Color(.25f, .16f, .18f));
            Set(result, "lifecycleHud", lifecycle); Set(result, "panel", resultPanel.gameObject); Set(result, "headerText", title);
            Set(result, "standingsText", standings); Set(result, "countdownText", countdown); Set(result, "returnButton", button); Set(result, "leaveButton", leave);
            resultPanel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(hud, path); PrefabUtility.UnloadPrefabContents(hud);
            const string rootPath = "Assets/_Project/Prefabs/Network/ArenaNetworkRoot.prefab";
            var root = PrefabUtility.LoadPrefabContents(rootPath);
            var config = new SerializedObject(root.GetComponent<ServerMatchLifecycleController>());
            config.FindProperty("targetKills").intValue = 5; config.FindProperty("postMatchSeconds").floatValue = 10;
            config.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, rootPath); PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            foreach (var file in new[] { path, rootPath })
                System.IO.File.WriteAllLines(file, Array.ConvertAll(System.IO.File.ReadAllLines(file), line => line.TrimEnd()));
            Debug.Log("M3.2 scoreboard, feed and competitive result HUD saved; scenes preserved.");
        }
        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var text = OfflineMatchSceneBuilder.Text(name, parent, font, value, fontSize, Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false; return text;
        }
        private static void Set(Object target, string field, Object value)
        { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
