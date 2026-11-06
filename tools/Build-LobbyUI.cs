#if UNITY_EDITOR
using System;
using System.Linq;
using Mirror;
using SmallArena.Client.UI;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    /// <summary>Run in an isolated copy, then copy back MainMenu and the generated network prefab with metadata.</summary>
    public static class LobbySceneBuilder
    {
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var canvas = menu.gameObject;
            canvas.SetActive(false);
            foreach (var root in scene.GetRootGameObjects().Where(root => root.GetComponent<ServerLobbyController>() != null)) Object.DestroyImmediate(root);
            foreach (var name in new[] { "ConnectionPanel", "RoomPanel" })
            {
                var old = canvas.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            foreach (var old in canvas.GetComponents<LobbyRoomController>()) Object.DestroyImmediate(old);
            foreach (var old in canvas.GetComponents<LobbyRoomView>()) Object.DestroyImmediate(old);
            var bootstrap = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap.unity", OpenSceneMode.Additive);
            var template = bootstrap.GetRootGameObjects().Single(root => root.GetComponent<ArenaNetworkManager>() != null);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            var networkRoot = Object.Instantiate(template);
            networkRoot.name = "_NetworkManager";
            foreach (var debugHud in networkRoot.GetComponents<NetworkManagerHUD>()) Object.DestroyImmediate(debugHud);
            var manager = networkRoot.GetComponent<ArenaNetworkManager>();
            manager.offlineScene = "Assets/_Project/Scenes/MainMenu.unity";
            manager.onlineScene = "";
            manager.autoCreatePlayer = false;
            manager.dontDestroyOnLoad = false;
            manager.maxConnections = 4;
            manager.headlessStartMode = HeadlessStartOptions.DoNothing;
            Set(manager, "roomEnabled", true);
            networkRoot.AddComponent<ServerLobbyController>();
            PrefabUtility.SaveAsPrefabAssetAndConnect(networkRoot, "Assets/_Project/Prefabs/Network/ArenaNetworkRoot.prefab", InteractionMode.AutomatedAction);
            EditorSceneManager.CloseScene(bootstrap, true);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var center = new Vector2(0.5f, 0.5f);
            var background = new Color(0.055f, 0.08f, 0.12f, 0.98f);
            var buttonColor = new Color(0.14f, 0.2f, 0.28f);
            var accent = new Color(0.95f, 0.74f, 0.22f);
            var connection = OfflineMatchSceneBuilder.Panel("ConnectionPanel", canvas.transform, center, center, Vector2.zero, new Vector2(680, 820), background);
            var room = OfflineMatchSceneBuilder.Panel("RoomPanel", canvas.transform, center, center, Vector2.zero, new Vector2(680, 820), background);
            Text("ConnectionTitle", connection, font, "MULTIPLAYER", 46, 325, 590, 80);
            Text("AddressLabel", connection, font, "Server address", 25, 225, 570, 50);
            var inputRoot = OfflineMatchSceneBuilder.Rect("ServerAddress", connection, center, center, new Vector2(0, 145), new Vector2(560, 74));
            inputRoot.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.17f, 0.24f);
            var input = inputRoot.gameObject.AddComponent<TMP_InputField>();
            var viewport = OfflineMatchSceneBuilder.Rect("Viewport", inputRoot, center, center, Vector2.zero, new Vector2(520, 66));
            viewport.gameObject.AddComponent<RectMask2D>();
            var inputText = Text("AddressText", viewport, font, "", 30, 0, 510, 64);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            input.textViewport = viewport;
            input.textComponent = inputText;
            input.text = "localhost";
            input.characterLimit = 253;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            var host = OfflineMatchSceneBuilder.CreateButton("HostRoomButton", connection, font, "Host Room", new Vector2(0, 25), new Vector2(520, 78), accent);
            var join = OfflineMatchSceneBuilder.CreateButton("JoinRoomButton", connection, font, "Join Room", new Vector2(0, -75), new Vector2(520, 78), buttonColor);
            var back = OfflineMatchSceneBuilder.CreateButton("LobbyBackButton", connection, font, "Back", new Vector2(0, -210), new Vector2(520, 78), buttonColor);
            var status = Text("ConnectionStatus", connection, font, "", 23, -320, 590, 100);
            status.textWrappingMode = TextWrappingModes.Normal;
            Text("RoomTitle", room, font, "ROOM LOBBY", 46, 325, 590, 80);
            var roster = Text("RoomMembers", room, font, "ROOM  0 / 4", 27, 70, 590, 330);
            roster.textWrappingMode = TextWrappingModes.Normal;
            var ready = OfflineMatchSceneBuilder.CreateButton("RoomReadyButton", room, font, "Ready", new Vector2(0, -135), new Vector2(520, 78), accent);
            var start = OfflineMatchSceneBuilder.CreateButton("RoomStartButton", room, font, "Start Match", new Vector2(0, -235), new Vector2(520, 78), buttonColor);
            var leave = OfflineMatchSceneBuilder.CreateButton("RoomLeaveButton", room, font, "Leave Room", new Vector2(0, -335), new Vector2(520, 78), buttonColor);
            var view = canvas.AddComponent<LobbyRoomView>();
            Bind(view, "menuCard", canvas.transform.Find("MenuCard").gameObject);
            Bind(view, "connectionPanel", connection.gameObject); Bind(view, "roomPanel", room.gameObject);
            Bind(view, "address", input); Bind(view, "hostButton", host); Bind(view, "joinButton", join);
            Bind(view, "backButton", back); Bind(view, "readyButton", ready); Bind(view, "startButton", start);
            Bind(view, "leaveButton", leave); Bind(view, "statusText", status); Bind(view, "membersText", roster);
            Bind(view, "readyLabel", ready.GetComponentInChildren<TextMeshProUGUI>());
            var controller = canvas.AddComponent<LobbyRoomController>();
            Bind(controller, "manager", manager);
            Bind(menu, "lobbyController", controller);
            var multiplayer = canvas.GetComponentsInChildren<Button>(true).Single(button => button.name == "MultiplayerButton");
            multiplayer.interactable = true;
            multiplayer.GetComponentInChildren<TextMeshProUGUI>().text = "Multiplayer";
            multiplayer.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            for (var index = multiplayer.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
                UnityEventTools.RemovePersistentListener(multiplayer.onClick, index);
            UnityEventTools.AddPersistentListener(multiplayer.onClick, menu.OnMultiplayerClicked);
            TextMeshProUGUI hint = canvas.GetComponentsInChildren<TextMeshProUGUI>(true).Single(text => text.name == "Hint");
            hint.text = "Single Player or up to 4 players online";
            connection.gameObject.SetActive(false); room.gameObject.SetActive(false);
            canvas.SetActive(true);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save MainMenu lobby wiring.");
            AssetDatabase.SaveAssets();
            Debug.Log("M2 lobby UI and ArenaNetworkRoot prefab saved; Bootstrap/Arena/build order preserved.");
        }
        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value, float size, float y, float width, float height)
        {
            var text = OfflineMatchSceneBuilder.Text(name, parent, font, value, size, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(width, height));
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }
        private static void Bind(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object owner, string field, bool value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
