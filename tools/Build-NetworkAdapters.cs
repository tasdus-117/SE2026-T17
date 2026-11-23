#if UNITY_EDITOR
using System.Linq;
using SmallArena.Client.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    public static class NetworkAdaptersBuilder
    {
        public static void Build()
        {
            const string path = "Assets/_Project/Prefabs/UI/NetworkHUD.prefab";
            var hud = PrefabUtility.LoadPrefabContents(path);
            var old = hud.transform.Find("PlayerPresentation"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var template = hud.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "NetworkDamageText");
            var text = Object.Instantiate(template, hud.transform); text.name = "PlayerPresentation";
            text.text = ""; text.fontSize = 22; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Left;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.zero;
            text.rectTransform.pivot = Vector2.zero; text.rectTransform.anchoredPosition = new Vector2(32, 32);
            text.rectTransform.sizeDelta = new Vector2(480, 40);
            var data = new SerializedObject(hud.GetComponent<NetworkHealthHUD>());
            data.FindProperty("presentationText").objectReferenceValue = text; data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(hud, path); PrefabUtility.UnloadPrefabContents(hud);
            AssetDatabase.SaveAssets();
            System.IO.File.WriteAllLines(path, System.IO.File.ReadAllLines(path).Select(line => line.TrimEnd()));
            Debug.Log("Network adapters: authoritative presentation label wired; existing HUD retained.");
        }
    }
}
#endif
