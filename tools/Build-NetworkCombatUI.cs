#if UNITY_EDITOR
using System;
using System.Linq;
using SmallArena.Client.Presentation;
using SmallArena.Client.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    public static class NetworkCombatUIBuilder
    {
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("TMP font missing.");
            var overhead = CanvasRoot("OpponentHealthBar", true);
            Bar(overhead.transform, font, true);
            var barAsset = PrefabUtility.SaveAsPrefabAsset(overhead, "Assets/_Project/Prefabs/UI/OpponentHealthBar.prefab");
            Object.DestroyImmediate(overhead);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            Bind(player.GetComponent<OpponentHealthPresenter>(), "healthBarPrefab", barAsset);
            PrefabUtility.SavePrefabAsset(player);

            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Arena_MVP.unity");
            var old = scene.GetRootGameObjects().FirstOrDefault(root => root.GetComponent<NetworkHealthHUD>() != null);
            if (old != null) Object.DestroyImmediate(old);
            var root = CanvasRoot("NetworkHUD", false);
            root.SetActive(false);
            var panel = OfflineMatchSceneBuilder.Panel("LocalHealthPanel", root.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -32), new Vector2(400, 130), new Color(0.035f, 0.055f, 0.09f, 0.95f));
            // A HUD click is UI, too. This panel intentionally consumes pointer clicks.
            panel.GetComponent<Image>().raycastTarget = true;
            var health = Bar(panel, font, false);
            var controls = OfflineMatchSceneBuilder.Text("NetworkControls", root.transform, font,
                "WASD Move   SPACE Jump\nLEFT CLICK Punch   ESC Menu", 24, Color.white,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -32), new Vector2(560, 90));
            controls.alignment = TextAlignmentOptions.TopRight;
            var hud = root.AddComponent<NetworkHealthHUD>();
            Bind(hud, "panel", panel.gameObject); Bind(hud, "healthView", health);
            Bind(hud, "controls", controls.gameObject);
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/_Project/Prefabs/UI/NetworkHUD.prefab", InteractionMode.AutomatedAction);
            Bind(hud, "matchMenu", Object.FindAnyObjectByType<InGameMenuController>());
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Network health HUD/overhead prefabs, player reference and arena instance saved.");
        }
        private static GameObject CanvasRoot(string name, bool world)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            root.layer = 5;
            root.GetComponent<Canvas>().renderMode = world ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = world ? 0 : 20;
            if (world)
            {
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 70);
                root.transform.localScale = Vector3.one * 0.01f;
            }
            else
            {
                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                root.AddComponent<GraphicRaycaster>();
            }
            return root;
        }
        private static NetworkHealthBarView Bar(Transform parent, TMP_FontAsset font, bool world)
        {
            var size = world ? new Vector2(200, 14) : new Vector2(352, 26);
            var rect = OfflineMatchSceneBuilder.Rect("HealthBar", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -14), size);
            var background = rect.gameObject.AddComponent<Image>(); background.color = new Color(0.15f, 0.18f, 0.23f); background.raycastTarget = false;
            var fill = OfflineMatchSceneBuilder.Panel("Fill", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.2f, 0.86f, 0.48f));
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            var slider = rect.gameObject.AddComponent<Slider>(); slider.fillRect = fill; slider.minValue = 0; slider.maxValue = 100; slider.value = 100; slider.wholeNumbers = true; slider.interactable = false;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = OfflineMatchSceneBuilder.Text("HealthText", parent, font, "HP: 100/100", world ? 22 : 30, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, world ? 15 : 27), new Vector2(world ? 220 : 352, 42));
            text.alignment = TextAlignmentOptions.Center;
            var view = parent.gameObject.AddComponent<NetworkHealthBarView>();
            Bind(view, "healthBar", slider); Bind(view, "healthText", text);
            return view;
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
