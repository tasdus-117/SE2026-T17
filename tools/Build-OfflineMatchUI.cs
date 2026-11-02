// To regenerate: copy this file into a validation project's Assets/Editor and run
// -executeMethod SmallArena.Tools.OfflineMatchSceneBuilder.Build with Unity 6000.6.7f1.
// First run tools/Import-TmpResources.ps1 -ProjectPath <validation-project>.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using SmallArena.Client.UI;
using SmallArena.Core;
using SmallArena.Pickups;
using SmallArena.Player;
using SmallArena.Server.Combat;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    public static class OfflineMatchSceneBuilder
    {
        public static void Build()
        {
            const string fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null) throw new InvalidOperationException("TMP Essential Resources font was not imported.");
            const string scenePath = "Assets/_Project/Scenes/Arena_MVP.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var manager = roots.Single(item => item.name == "_GameManager").GetComponent<GameManager>();
            var player = roots.Single(item => item.name == "Player");
            var health = roots.Single(item => item.name == "Bot").GetComponent<Health>();
            var combat = player.GetComponent<CombatController>();
            var pickup = roots.Single(item => item.name == "WeaponPickup").GetComponent<WeaponPickup>();
            var oldCanvas = roots.SingleOrDefault(item => item.name == "MatchCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            var canvasObject = new GameObject("MatchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var hud = Panel("HUD", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(32f, -32f), new Vector2(470f, 126f), new Color(0.035f, 0.055f, 0.09f, 0.9f));
            var hp = Text("BotHpText", hud, font, "Bot HP: 10 / 10", 32f, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -15f), new Vector2(426f, 42f));
            var damage = Text("DamageText", hud, font, "Damage: 1 HP (Unarmed)", 26f,
                new Color(0.66f, 0.85f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -66f), new Vector2(426f, 36f));
            var controls = Text("ControlsText", canvasObject.transform, font, "WASD Move   SPACE Jump\nJ Punch   E Collect", 24f,
                new Color(0.85f, 0.9f, 0.98f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-32f, -36f), new Vector2(470f, 76f));
            controls.alignment = TextAlignmentOptions.TopRight;
            var banner = Panel("VictoryBanner", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(660f, 190f), new Color(0.035f, 0.055f, 0.09f, 0.96f));
            var victory = Text("VictoryText", banner, font, "VICTORY!", 76f, new Color(1f, 0.82f, 0.32f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 130f));
            victory.fontStyle = FontStyles.Bold;
            victory.alignment = TextAlignmentOptions.Center;
            banner.gameObject.SetActive(false);

            var ui = canvasObject.AddComponent<MatchUI>();
            var serializedUI = new SerializedObject(ui);
            Bind(serializedUI, "gameManager", manager);
            Bind(serializedUI, "botHealth", health);
            Bind(serializedUI, "playerCombat", combat);
            Bind(serializedUI, "botHpText", hp);
            Bind(serializedUI, "damageText", damage);
            Bind(serializedUI, "victoryBanner", banner.gameObject);
            Bind(serializedUI, "victoryText", victory);
            serializedUI.ApplyModifiedPropertiesWithoutUndo();

            var readiness = manager.GetComponent<OfflineMatchReadinessProbe>();
            if (readiness == null) readiness = manager.gameObject.AddComponent<OfflineMatchReadinessProbe>();
            var serializedReadiness = new SerializedObject(readiness);
            Bind(serializedReadiness, "gameManager", manager);
            Bind(serializedReadiness, "botHealth", health);
            Bind(serializedReadiness, "playerCombat", combat);
            Bind(serializedReadiness, "weaponPickup", pickup);
            Bind(serializedReadiness, "matchUI", ui);
            serializedReadiness.ApplyModifiedPropertiesWithoutUndo();
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("autoStart").boolValue = true;
            Bind(serializedManager, "readinessProvider", readiness);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Arena UI scene save failed.");
            AssetDatabase.SaveAssets();
            Debug.Log("TASK 2.5: Overlay HUD, TMP resources, Victory banner and real offline readiness wired and saved.");
        }

        private static void Bind(SerializedObject owner, string property, Object value) =>
            owner.FindProperty(property).objectReferenceValue = value;

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var owner = new GameObject(name, typeof(RectTransform));
            owner.layer = LayerMask.NameToLayer("UI");
            var rect = owner.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(name, parent, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value,
            float size, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 dimensions)
        {
            var rect = Rect(name, parent, anchor, pivot, position, dimensions);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }
    }
}
#endif
