using Mirror;
using SmallArena.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmallArena.Client.UI
{
    /// <summary>Observer lives on the session root; visible UI exists only during one local load.</summary>
    public sealed class ArenaLoadingOverlay : MonoBehaviour
    {
        private ArenaNetworkManager manager;
        private GameObject canvas;
        public bool IsVisible => canvas != null && canvas.activeInHierarchy;

        public static void Observe(ArenaNetworkManager manager)
        {
            if (manager != null && manager.GetComponent<ArenaLoadingOverlay>() == null)
                manager.gameObject.AddComponent<ArenaLoadingOverlay>();
        }
        private void OnEnable()
        {
            manager = GetComponent<ArenaNetworkManager>();
            manager.ClientArenaLoadStarted += Show;
            manager.ClientArenaLoadFinished += Hide;
            manager.ClientArenaLoadAborted += Hide;
        }
        private void Show()
        {
            if (canvas != null) return;
            canvas = new GameObject("Arena loading overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvas);
            var root = canvas.GetComponent<Canvas>();
            root.renderMode = RenderMode.ScreenSpaceOverlay;
            root.sortingOrder = 1000;
            var panel = new GameObject("Loading", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.09f, 0.96f);
            var label = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(panel.transform, false);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.text = "Loading arena...";
            text.fontSize = 30; text.alignment = TextAlignmentOptions.Center;
            var labelRect = label.GetComponent<RectTransform>(); labelRect.sizeDelta = new Vector2(600, 100);
        }
        private void Hide()
        {
            if (canvas == null) return;
            canvas.SetActive(false); Destroy(canvas); canvas = null;
        }
        private void OnDisable()
        {
            if (manager != null)
            {
                manager.ClientArenaLoadStarted -= Show;
                manager.ClientArenaLoadFinished -= Hide;
                manager.ClientArenaLoadAborted -= Hide;
            }
            Hide();
        }
    }
}
