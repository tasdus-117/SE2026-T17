#if UNITY_EDITOR
using System.Linq;
using Mirror;
using SmallArena.Client.Presentation;
using SmallArena.Client.UI;
using SmallArena.Networking;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace SmallArena.Tools
{
    public static class LootParityBuilder
    {
        public static void Build()
        {
            NetworkCombatUIBuilder.Build();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Combat/WeaponPickup.prefab");
            var loot = Object.Instantiate(source); loot.name = "NetworkLootItem";
            Object.DestroyImmediate(loot.GetComponent<SmallArena.Pickups.WeaponPickup>());
            loot.AddComponent<NetworkIdentity>(); loot.AddComponent<NetworkLootItem>();
            var trigger = loot.AddComponent<SphereCollider>(); trigger.isTrigger = true;
            trigger.radius = NetworkLootItem.InteractionRadius / loot.transform.lossyScale.x;
            var lootAsset = PrefabUtility.SaveAsPrefabAsset(loot, "Assets/_Project/Prefabs/Network/NetworkLootItem.prefab");
            var identityData = new SerializedObject(lootAsset.GetComponent<NetworkIdentity>());
            identityData.FindProperty("_assetId").longValue = NetworkIdentity.AssetGuidToUint(new System.Guid(AssetDatabase.AssetPathToGUID("Assets/_Project/Prefabs/Network/NetworkLootItem.prefab")));
            identityData.FindProperty("sceneId").longValue = 0;
            identityData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SavePrefabAsset(lootAsset);
            Object.DestroyImmediate(loot);
            var root = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/Network/ArenaNetworkRoot.prefab");
            ConfigureRoot(root, lootAsset); PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Prefabs/Network/ArenaNetworkRoot.prefab"); PrefabUtility.UnloadPrefabContents(root);
            var bar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/OpponentHealthBar.prefab");
            var bot = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/Characters/Bot.prefab");
            var presenter = bot.GetComponent<OfflineHealthPresenter>(); if (presenter == null) presenter = bot.AddComponent<OfflineHealthPresenter>();
            Bind(presenter, "healthSource", bot.GetComponent<SmallArena.Server.Combat.Health>()); Bind(presenter, "healthBarPrefab", bar); PrefabUtility.SaveAsPrefabAsset(bot, "Assets/_Project/Prefabs/Characters/Bot.prefab"); PrefabUtility.UnloadPrefabContents(bot);
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Arena_MVP.unity");
            var match = Object.FindAnyObjectByType<MatchUI>();
            var texts = match.GetComponentsInChildren<TextMeshProUGUI>(true);
            var hp = texts.Single(item => item.name == "BotHpText"); var damage = texts.Single(item => item.name == "DamageText");
            var controls = texts.Single(item => item.name == "ControlsText");
            controls.text = "WASD Move   SPACE Jump\nLMB/J Punch   E Collect   ESC Menu";
            controls.rectTransform.sizeDelta = new Vector2(560, 90); controls.rectTransform.anchoredPosition = new Vector2(-32, -32);
            var panel = hp.transform.parent.GetComponent<RectTransform>(); panel.sizeDelta = new Vector2(400, 130);
            Layout(hp, 30, 30, 42); Layout(damage, -45, 22, 32);
            foreach (var child in panel.Cast<Transform>().Where(item => item.name == "HealthBar" || item.name == "HealthText").ToArray()) Object.DestroyImmediate(child.gameObject);
            var view = NetworkCombatUIBuilder.Bar(panel, hp.font, false);
            Bind(match, "botHpBar", view.HealthBar);
            // MatchUI already owns this existing text; use only the shared slider style offline.
            Object.DestroyImmediate(panel.Find("HealthText").gameObject); Object.DestroyImmediate(view);
            EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap.unity");
            ConfigureRoot(Object.FindAnyObjectByType<ArenaNetworkManager>().gameObject, lootAsset);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Arena_MVP.unity");
            Debug.Log("10 HP parity, shared bars, network loot registration/spawn and offline billboard saved.");
        }
        private static void Layout(TextMeshProUGUI text, float y, float size, float height)
        {
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(352, height);
            text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
        }
        private static void ConfigureRoot(GameObject root, GameObject loot)
        {
            var manager = root.GetComponent<ArenaNetworkManager>();
            if (!manager.spawnPrefabs.Contains(loot)) manager.spawnPrefabs.Add(loot);
            var spawner = root.GetComponent<ArenaLootSpawner>(); if (spawner == null) spawner = root.AddComponent<ArenaLootSpawner>();
            Bind(spawner, "lootPrefab", loot);
        }
        private static void Bind(Object owner, string name, Object target)
        {
            var serialized = new SerializedObject(owner); serialized.FindProperty(name).objectReferenceValue = target;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
