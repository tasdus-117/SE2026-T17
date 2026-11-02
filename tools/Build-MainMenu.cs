// Copy alongside Build-OfflineMatchUI.cs into the validation project's Assets/Editor,
// then run -executeMethod SmallArena.Tools.MenuSceneBuilder.Build.
#if UNITY_EDITOR
using System;
using SmallArena.Client.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SmallArena.Tools
{
    public static class MenuSceneBuilder
    {
        public static void Build()
        {
            OfflineMatchSceneBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string path = "Assets/_Project/Scenes/MainMenu.unity";
            scene.name = "MainMenu";
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.065f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var controller = canvasObject.AddComponent<MainMenuController>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Import the bundled TMP resources first.");
            var center = new Vector2(0.5f, 0.5f);
            var card = OfflineMatchSceneBuilder.Panel("MenuCard", canvasObject.transform, center, center,
                Vector2.zero, new Vector2(640f, 800f), new Color(0.055f, 0.08f, 0.12f, 0.98f));
            OfflineMatchSceneBuilder.Panel("Accent", card, center, center, new Vector2(0f, 350f),
                new Vector2(72f, 6f), new Color(0.95f, 0.74f, 0.22f));
            var title = OfflineMatchSceneBuilder.Text("Title", card, font, "SMALL ARENA", 58f, Color.white,
                center, center, new Vector2(0f, 275f), new Vector2(590f, 100f));
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            var subtitle = OfflineMatchSceneBuilder.Text("Subtitle", card, font, "A quick match against the bot", 24f,
                new Color(0.65f, 0.78f, 0.9f), center, center, new Vector2(0f, 200f), new Vector2(560f, 50f));
            subtitle.alignment = TextAlignmentOptions.Center;
            var single = OfflineMatchSceneBuilder.CreateButton("SinglePlayerButton", card, font, "Single Player",
                new Vector2(0f, 90f), new Vector2(480f, 80f), new Color(0.95f, 0.74f, 0.22f));
            single.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.04f, 0.06f, 0.09f);
            single.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            var multiplayer = OfflineMatchSceneBuilder.CreateButton("MultiplayerButton", card, font, "Multiplayer (Coming Soon)",
                new Vector2(0f, -10f), new Vector2(480f, 80f), new Color(0.14f, 0.2f, 0.28f));
            var tutorial = OfflineMatchSceneBuilder.CreateButton("TutorialButton", card, font, "Tutorial (Coming Soon)",
                new Vector2(0f, -110f), new Vector2(480f, 80f), new Color(0.14f, 0.2f, 0.28f));
            foreach (var future in new[] { multiplayer, tutorial })
            {
                future.interactable = false;
                future.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.5f, 0.6f, 0.7f);
            }
            var exit = OfflineMatchSceneBuilder.CreateButton("ExitButton", card, font, "Exit",
                new Vector2(0f, -210f), new Vector2(480f, 80f), new Color(0.14f, 0.2f, 0.28f));
            UnityEventTools.AddPersistentListener(single.onClick, controller.OnSinglePlayerClicked);
            UnityEventTools.AddPersistentListener(exit.onClick, controller.OnExitClicked);
            var hint = OfflineMatchSceneBuilder.Text("Hint", card, font, "Single Player: play with the bot", 20f,
                new Color(0.65f, 0.73f, 0.82f), center, center, new Vector2(0f, -320f), new Vector2(560f, 50f));
            hint.alignment = TextAlignmentOptions.Center;
            OfflineMatchSceneBuilder.EnsureEventSystem(scene).firstSelectedGameObject = single.gameObject;
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Main menu save failed.");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(path, true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Arena_MVP.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("TASK 2.6: Main Menu, Victory navigation, Input System event systems and build indices 0/1/2 saved.");
        }
    }
}
#endif
