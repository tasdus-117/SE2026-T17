using Mirror;
using SmallArena.Domain;
using SmallArena.Input;
using SmallArena.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SmallArena.Client.UI
{
    [RequireComponent(typeof(InGameMenuView))]
    public sealed class InGameMenuController : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler offlineInput = null;
        [SerializeField] private MonoBehaviour matchStateSource = null;
        private InGameMenuView view;
        private PlayerInputHandler localInput;
        private InputAction toggle;
        private IMatchStateSource match;
        private bool leaving;
        public bool IsPaused => view != null && view.PauseVisible;
        private bool OfflineVictory => !NetworkClient.active && !NetworkServer.active && matchStateSource != null && match != null && match.CurrentState == MatchState.Victory;
        private bool Blocked => leaving || IsPaused || view.ResultVisible || OfflineVictory || MatchSessionNavigation.IsReturning;

        private void OnEnable()
        {
            view = GetComponent<InGameMenuView>();
            if (toggle == null) toggle = new InputAction("InGameMenu", InputActionType.Button, "<Keyboard>/escape");
            toggle.performed += Toggle;
            toggle.Enable();
            view.ResumeButton.onClick.AddListener(Resume);
            view.LeaveButton.onClick.AddListener(LeaveMatch);
            view.ResultReturnButton.onClick.AddListener(LeaveMatch);
            NetworkPlayer.OnLocalPlayerSpawned += LocalSpawned;
            NetworkPlayer.OnLocalPlayerDespawned += LocalDespawned;
            match = matchStateSource as IMatchStateSource;
            if (match != null) match.OnMatchStateChanged += MatchChanged;
            BindInput();
        }
        private void Start()
        {
            if (Blocked) SceneNavigation.ShowMenuCursor();
            else GameplayCursor();
        }
        private void Toggle(InputAction.CallbackContext context)
        {
            if (leaving || view.ResultVisible || OfflineVictory || MatchSessionNavigation.IsReturning) return;
            if (IsPaused) Resume();
            else
            {
                view.ShowPause(true);
                ApplyInputGate();
                SceneNavigation.ShowMenuCursor();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(view.ResumeButton.gameObject);
            }
        }
        public void Resume()
        {
            if (leaving || view.ResultVisible || OfflineVictory || MatchSessionNavigation.IsReturning) return;
            view.ShowPause(false);
            ApplyInputGate();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            GameplayCursor();
        }
        public void LeaveMatch()
        {
            if (leaving) return;
            leaving = true;
            view.SetLeaving();
            ApplyInputGate();
            SceneNavigation.ShowMenuCursor();
            SceneNavigation.LoadMainMenu();
        }
        /// <summary>Presentation hook for a future authoritative M3 result; does not decide the outcome.</summary>
        public void ShowMatchResult(bool isWinner)
        {
            if (leaving) return;
            view.ShowResult(isWinner);
            ApplyInputGate();
            SceneNavigation.ShowMenuCursor();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(view.ResultReturnButton.gameObject);
        }
        private static void GameplayCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        private void LocalSpawned(NetworkPlayer player) { if (player.isLocalPlayer) BindInput(); }
        private void LocalDespawned(NetworkPlayer player)
        {
            if (localInput != null && player.gameObject == localInput.gameObject) { localInput.SetMenuInputBlocked(false); localInput = null; }
        }
        private void BindInput()
        {
            var next = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<PlayerInputHandler>()
                : !NetworkClient.active && !NetworkServer.active ? offlineInput : null;
            if (localInput != next && localInput != null) localInput.SetMenuInputBlocked(false);
            localInput = next;
            ApplyInputGate();
        }
        private void ApplyInputGate() { if (localInput != null) localInput.SetMenuInputBlocked(Blocked); }
        private void MatchChanged(MatchState state)
        {
            if (state != MatchState.Victory || NetworkClient.active || NetworkServer.active) return;
            view.ShowPause(false); // Preserve the existing offline Victory banner/replay controls.
            ApplyInputGate();
            SceneNavigation.ShowMenuCursor();
        }
        private void OnDisable()
        {
            if (toggle != null) { toggle.performed -= Toggle; toggle.Disable(); }
            view.ResumeButton.onClick.RemoveListener(Resume);
            view.LeaveButton.onClick.RemoveListener(LeaveMatch);
            view.ResultReturnButton.onClick.RemoveListener(LeaveMatch);
            NetworkPlayer.OnLocalPlayerSpawned -= LocalSpawned;
            NetworkPlayer.OnLocalPlayerDespawned -= LocalDespawned;
            if (match != null) match.OnMatchStateChanged -= MatchChanged;
            if (localInput != null) localInput.SetMenuInputBlocked(false);
            localInput = null;
            SceneNavigation.ShowMenuCursor();
        }
        private void OnDestroy() => toggle?.Dispose();
    }
}
