#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.UI;
using SmallArena.Core;
using SmallArena.Domain;
using SmallArena.Input;
using SmallArena.Pickups;
using SmallArena.Player;
using SmallArena.Server.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class MenuNavigationTests : InputTestFixture
    {
        private Mouse mouse;
        private float previousCaptureDeltaTime;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;

        public override void Setup()
        {
            base.Setup();
            previousCaptureDeltaTime = Time.captureDeltaTime;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator UnloadNavigationScene()
        {
            var previous = SceneManager.GetActiveScene();
            if (previous.buildIndex >= 0)
            {
                var cleanup = SceneManager.CreateScene("MenuNavigationTests Cleanup");
                SceneManager.SetActiveScene(cleanup);
                yield return SceneManager.UnloadSceneAsync(previous);
            }
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = previousCaptureDeltaTime;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MainMenu_HasFourConfiguredButtons_AndInputSystemMouseSupport()
        {
            yield return LoadMainMenu();
            var canvas = Object.FindAnyObjectByType<MainMenuController>().GetComponent<Canvas>();
            Assert.That(SceneManager.GetActiveScene().buildIndex, Is.Zero);
            Assert.That(SceneUtility.GetScenePathByBuildIndex(1), Does.EndWith("Bootstrap.unity"));
            Assert.That(SceneUtility.GetScenePathByBuildIndex(2), Does.EndWith("Arena_MVP.unity"));
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(canvas.GetComponent<GraphicRaycaster>().enabled, Is.True);
            Assert.That(canvas.GetComponentsInChildren<Button>().Length, Is.EqualTo(4));
            Assert.That(ButtonNamed("SinglePlayerButton").interactable, Is.True);
            Assert.That(ButtonNamed("MultiplayerButton").interactable, Is.True);
            Assert.That(ButtonNamed("TutorialButton").interactable, Is.False);
            Assert.That(ButtonNamed("ExitButton").interactable, Is.True);
            Assert.That(ButtonNamed("ExitButton").onClick.GetPersistentMethodName(0), Is.EqualTo("OnExitClicked"));
            Assert.That(EventSystem.current.currentInputModule, Is.TypeOf<InputSystemUIInputModule>());
            Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
            AssertMenuCursor();
        }

        [UnityTest]
        public IEnumerator SinglePlayerButtonCallback_LoadsArenaAndStartsPlaying()
        {
            yield return LoadMainMenu();
            ButtonNamed("SinglePlayerButton").onClick.Invoke();
            yield return WaitForScene("Arena_MVP");
            AssertFreshMatch();
        }

        [UnityTest]
        public IEnumerator MouseClickOnSinglePlayer_UsesRealUiRaycastAndLoadsArena()
        {
            yield return LoadMainMenu();
            // InputTestFixture resets the global input runtime between tests. Use a fresh instance
            // of the same default UI actions so shared package assets cannot retain old mock controls.
            var module = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
            module.AssignDefaultActions();
            Canvas.ForceUpdateCanvases();
            var button = ButtonNamed("SinglePlayerButton");
            var point = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            SetMouse(point, false);
            yield return null;
            var hitResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hitResults);
            Assert.That(hitResults.Any(hit => hit.gameObject == button.gameObject), Is.True,
                $"Button must be raycastable at {point}; screen={Screen.width}x{Screen.height}; hits={string.Join(",", hitResults.Select(hit => hit.gameObject.name))}");
            Assert.That(module.point.action.enabled && module.leftClick.action.enabled, Is.True);
            Assert.That(module.point.action.ReadValue<Vector2>(), Is.EqualTo(point), "UI point action must receive the mock mouse position.");
            SetMouse(point, true);
            yield return null;
            SetMouse(point, false);
            yield return WaitForScene("Arena_MVP");
            AssertFreshMatch();
        }

        [UnityTest]
        public IEnumerator PlayAgainButton_ReloadsWithFreshMatchStateHealthDamageAndPickup()
        {
            yield return LoadMainMenu();
            Object.FindAnyObjectByType<MainMenuController>().OnSinglePlayerClicked();
            yield return WaitForScene("Arena_MVP");
            var oldManager = Object.FindAnyObjectByType<GameManager>();
            var combat = Object.FindAnyObjectByType<CombatController>();
            var pickup = Object.FindAnyObjectByType<WeaponPickup>();
            Assert.That(pickup.TryCollect(combat, pickup.transform.position), Is.True);
            Object.FindAnyObjectByType<Health>().TakeDamage(10);
            var ui = Object.FindAnyObjectByType<MatchUI>();
            for (var cycle = 0; cycle < 3; cycle++) { ui.enabled = false; ui.enabled = true; }
            AssertMenuCursor();
            var oldScene = SceneManager.GetActiveScene();
            var loads = 0;
            void OnLoaded(Scene scene, LoadSceneMode mode) { if (scene.name == "Arena_MVP") loads++; }
            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                ButtonNamed("PlayAgainButton").onClick.Invoke();
                yield return WaitForReload(oldScene);
                Assert.That(oldManager == null, Is.True, "Replay must destroy the previous lifecycle instance.");
                Assert.That(loads, Is.EqualTo(1), "Re-enabling the HUD must not duplicate button callbacks.");
                AssertFreshMatch();
            }
            finally { SceneManager.sceneLoaded -= OnLoaded; }
        }

        [UnityTest]
        public IEnumerator ExitToMenuButton_ReturnsToMainMenu_AndSinglePlayerCanStartAgain()
        {
            yield return LoadMainMenu();
            ButtonNamed("SinglePlayerButton").onClick.Invoke();
            yield return WaitForScene("Arena_MVP");
            var previousManager = Object.FindAnyObjectByType<GameManager>();
            Object.FindAnyObjectByType<Health>().TakeDamage(10);
            Assert.That(previousManager.CurrentState, Is.EqualTo(MatchState.Victory));
            ButtonNamed("ExitToMenuButton").onClick.Invoke();
            yield return WaitForScene("MainMenu");
            Assert.That(previousManager == null, Is.True);
            Assert.That(Object.FindAnyObjectByType<GameManager>(), Is.Null);
            Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
            AssertMenuCursor();
            ButtonNamed("SinglePlayerButton").onClick.Invoke();
            yield return WaitForScene("Arena_MVP");
            AssertFreshMatch();
        }

        [UnityTest]
        public IEnumerator DisabledHud_RemovesButtonCallbacks_AndReenableRestoresThem()
        {
            yield return LoadMainMenu();
            ButtonNamed("SinglePlayerButton").onClick.Invoke();
            yield return WaitForScene("Arena_MVP");
            Object.FindAnyObjectByType<Health>().TakeDamage(10);
            var ui = Object.FindAnyObjectByType<MatchUI>();
            var replay = ButtonNamed("PlayAgainButton");
            var toMenu = ButtonNamed("ExitToMenuButton");
            var previousScene = SceneManager.GetActiveScene();
            ui.enabled = false;
            replay.onClick.Invoke();
            toMenu.onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previousScene));
            ui.enabled = true;
            replay.onClick.Invoke();
            yield return WaitForReload(previousScene);
            AssertFreshMatch();
        }

        [UnityTest]
        public IEnumerator MultiplayerOpensConnectionPanel_WhileTutorialRemainsComingSoon()
        {
            yield return LoadMainMenu();
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            controller.OnMultiplayerClicked();
            Assert.That(controller.GetComponent<LobbyRoomView>().ConnectionVisible, Is.True);
            LogAssert.Expect(LogType.Log, "Tutorial Coming Soon");
            controller.OnTutorialClicked();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        private static IEnumerator LoadMainMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
        }

        private static IEnumerator WaitForScene(string name)
        {
            for (var frame = 0; frame < 180 && SceneManager.GetActiveScene().name != name; frame++) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name));
            yield return null; // Let the new arena's Start/readiness gate run.
        }

        private static IEnumerator WaitForReload(Scene previousScene)
        {
            for (var frame = 0; frame < 180 && SceneManager.GetActiveScene() == previousScene; frame++) yield return null;
            Assert.That(SceneManager.GetActiveScene(), Is.Not.EqualTo(previousScene));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Arena_MVP"));
            yield return null;
        }

        private static Button ButtonNamed(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(item => item.GetComponentsInChildren<Button>(true)).Single(item => item.name == name);

        private static void AssertFreshMatch()
        {
            Assert.That(SceneManager.GetActiveScene().buildIndex, Is.EqualTo(2));
            Assert.That(Object.FindAnyObjectByType<GameManager>().CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(Object.FindAnyObjectByType<Health>().CurrentHealth, Is.EqualTo(10));
            Assert.That(Object.FindAnyObjectByType<CombatController>().CurrentDamage, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<WeaponPickup>().IsCollected, Is.False);
            Assert.That(Object.FindAnyObjectByType<PlayerController>().enabled, Is.True);
            Assert.That(Object.FindAnyObjectByType<PlayerInputHandler>().enabled, Is.True);
            Assert.That(ButtonNamed("PlayAgainButton").gameObject.activeInHierarchy, Is.False);
        }

        private static void AssertMenuCursor()
        {
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
        }

        private void SetMouse(Vector2 position, bool pressed)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = (ushort)(pressed ? 1 : 0) });
            InputSystem.Update();
            // Advance the real UI module in the same frame as the injected pointer event.
            EventSystem.current.currentInputModule.Process();
        }
    }
}
#endif
