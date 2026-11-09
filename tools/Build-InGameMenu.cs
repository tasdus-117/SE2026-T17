#if UNITY_EDITOR
using System;
using System.Linq;
using SmallArena.Client.UI;
using SmallArena.Input;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    public static class InGameMenuBuilder
    {
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Arena_MVP.unity");
            var old = scene.GetRootGameObjects().FirstOrDefault(root => root.GetComponent<InGameMenuController>() != null);
            if (old != null) Object.DestroyImmediate(old);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Missing TMP resources.");
            var root = new GameObject("InGameMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.SetActive(false);
            root.layer = LayerMask.NameToLayer("UI");
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 30;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var pause = Modal("PausePanel", root.transform);
            var result = Modal("MatchResultPanel", root.transform);
            Label("PauseHeader", pause, font, "MATCH MENU", 52, 140);
            Label("PauseHint", pause, font, "Gameplay input paused. The match keeps running.", 22, 65);
            var resume = OfflineMatchSceneBuilder.CreateButton("ResumeMatchButton", pause, font, "Resume", new Vector2(0, -30), new Vector2(520, 78), new Color(0.95f, 0.74f, 0.22f));
            resume.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.04f, 0.06f, 0.09f);
            var leave = OfflineMatchSceneBuilder.CreateButton("LeaveMatchButton", pause, font, "Leave Match / Main Menu", new Vector2(0, -140), new Vector2(520, 78), new Color(0.14f, 0.2f, 0.28f));
            var header = Label("MatchResultHeader", result, font, "VICTORY", 64, 110);
            header.fontStyle = FontStyles.Bold;
            var returnButton = OfflineMatchSceneBuilder.CreateButton("MatchResultReturnButton", result, font, "Return to Main Menu", new Vector2(0, -85), new Vector2(520, 78), new Color(0.14f, 0.2f, 0.28f));
            pause.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            var view = root.AddComponent<InGameMenuView>();
            Bind(view, "pausePanel", pause.gameObject); Bind(view, "resultPanel", result.gameObject);
            Bind(view, "resultHeader", header); Bind(view, "resumeButton", resume);
            Bind(view, "leaveButton", leave); Bind(view, "resultReturnButton", returnButton);
            root.AddComponent<InGameMenuController>();
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/_Project/Prefabs/UI/InGameMenu.prefab", InteractionMode.AutomatedAction);
            var controller = root.GetComponent<InGameMenuController>();
            var roots = scene.GetRootGameObjects();
            Bind(controller, "offlineInput", roots.Single(item => item.name == "Player").GetComponent<PlayerInputHandler>());
            Bind(controller, "matchStateSource", roots.Single(item => item.name == "_GameManager").GetComponents<MonoBehaviour>().Single(item => item is SmallArena.Domain.IMatchStateSource));
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save arena menu wiring.");
            AssetDatabase.SaveAssets();
            Debug.Log("In-game menu prefab and arena overrides saved. Offline HUD, actors, markers and EventSystem preserved.");
        }
        private static RectTransform Modal(string name, Transform parent)
        {
            var dim = OfflineMatchSceneBuilder.Panel(name, parent, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero, new Color(0.01f,0.02f,0.04f,0.82f));
            dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.offsetMin = dim.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().raycastTarget = true;
            return dim;
        }
        private static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string value, float size, float y)
        {
            var text = OfflineMatchSceneBuilder.Text(name, parent, font, value, size, Color.white, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0,y), new Vector2(800,100));
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }
        private static void Bind(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
