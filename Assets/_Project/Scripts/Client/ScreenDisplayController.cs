using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallArena.Client
{
    /// <summary>Global presentation preference, independent of room/gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class ScreenDisplayController : MonoBehaviour
    {
        public event Action<bool> FullscreenRequested;
        public bool LastRequestedFullscreen { get; private set; }
        public Vector2Int LastRequestedResolution { get; private set; }
        public FullScreenMode LastRequestedMode { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<ScreenDisplayController>() == null)
                new GameObject("_ScreenDisplay").AddComponent<ScreenDisplayController>();
        }
        private void Awake()
        {
            foreach (var other in FindObjectsByType<ScreenDisplayController>())
                if (other != this) { Destroy(gameObject); return; }
            DontDestroyOnLoad(gameObject);
            LastRequestedFullscreen = Screen.fullScreen;
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame) ToggleFullscreen();
        }
        public void ToggleFullscreen()
        {
            // Screen applies asynchronously; retain the last request so fast presses also alternate.
            LastRequestedFullscreen = !LastRequestedFullscreen;
            var nativeWidth = Mathf.Max(1, Display.main.systemWidth);
            var nativeHeight = Mathf.Max(1, Display.main.systemHeight);
            LastRequestedResolution = LastRequestedFullscreen ? new Vector2Int(nativeWidth, nativeHeight)
                : new Vector2Int(Mathf.Max(1, nativeWidth / 2), Mathf.Max(1, nativeHeight / 2));
            LastRequestedMode = LastRequestedFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(LastRequestedResolution.x, LastRequestedResolution.y, LastRequestedMode);
            FullscreenRequested?.Invoke(LastRequestedFullscreen);
        }
    }
}
