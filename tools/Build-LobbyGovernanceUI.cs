#if UNITY_EDITOR
using System;
using System.Linq;
using SmallArena.Client.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tools
{
    /// <summary>Additive upgrade of the existing menu. Run only in a validation copy.</summary>
    public static class LobbyGovernanceSceneBuilder
    {
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
            var view = Object.FindAnyObjectByType<LobbyRoomView>();
            var serialized = new SerializedObject(view);
            var room = ((GameObject)serialized.FindProperty("roomPanel").objectReferenceValue).transform;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var header = (TextMeshProUGUI)serialized.FindProperty("membersText").objectReferenceValue;
            header.rectTransform.anchoredPosition = new Vector2(0, 245);
            header.rectTransform.sizeDelta = new Vector2(590, 55);
            foreach (var name in new[] { "PlayerSlots", "RoomErrorModal" })
            {
                var old = (name == "PlayerSlots" ? room : view.transform).Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var center = new Vector2(0.5f, 0.5f);
            var slots = OfflineMatchSceneBuilder.Rect("PlayerSlots", room, center, center, new Vector2(0, 65), new Vector2(600, 290));
            var names = serialized.FindProperty("slotNames"); names.arraySize = 4;
            var statuses = serialized.FindProperty("slotStatuses"); statuses.arraySize = 4;
            for (var i = 0; i < 4; i++)
            {
                var row = OfflineMatchSceneBuilder.Panel("Slot" + (i + 1), slots, center, center,
                    new Vector2(0, 108 - i * 72), new Vector2(590, 64), new Color(0.1f, 0.14f, 0.2f));
                var label = Text("PlayerName", row, font, (i + 1) + ". Empty slot", new Vector2(-85, 0), new Vector2(390, 60), 23);
                var status = Text("ReadyStatus", row, font, "Waiting...", new Vector2(205, 0), new Vector2(170, 60), 23);
                status.color = Color.gray;
                names.GetArrayElementAtIndex(i).objectReferenceValue = label;
                statuses.GetArrayElementAtIndex(i).objectReferenceValue = status;
            }
            var error = OfflineMatchSceneBuilder.Panel("RoomErrorModal", view.transform, center, center,
                Vector2.zero, new Vector2(700, 300), new Color(0.12f, 0.06f, 0.06f, 1));
            var errorText = Text("ErrorMessage", error, font, "Cannot join: Room is full (4/4)", new Vector2(0, 50), new Vector2(650, 130), 30);
            errorText.alignment = TextAlignmentOptions.Center;
            errorText.textWrappingMode = TextWrappingModes.Normal;
            var dismiss = OfflineMatchSceneBuilder.CreateButton("DismissErrorButton", error, font, "OK",
                new Vector2(0, -90), new Vector2(300, 65), new Color(0.6f, 0.24f, 0.16f));
            serialized.FindProperty("errorPanel").objectReferenceValue = error.gameObject;
            serialized.FindProperty("errorText").objectReferenceValue = errorText;
            serialized.FindProperty("dismissErrorButton").objectReferenceValue = dismiss;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            error.gameObject.SetActive(false);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save lobby governance UI.");
            AssetDatabase.SaveAssets();
            Debug.Log("Lobby governance UI saved: four stable slots and dismissible rejection modal.");
        }
        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var text = OfflineMatchSceneBuilder.Text(name, parent, font, value, fontSize, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}
#endif
