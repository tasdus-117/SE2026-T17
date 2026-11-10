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
            Screen.fullScreen = LastRequestedFullscreen;
            FullscreenRequested?.Invoke(LastRequestedFullscreen);
        }
    }
}
