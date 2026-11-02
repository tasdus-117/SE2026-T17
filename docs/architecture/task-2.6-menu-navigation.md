# Task 2.6: Menu and match navigation

Enabled build scenes are MainMenu at index 0, Bootstrap at index 1, and Arena_MVP at index 2. MainMenu is the application entry point. Single Player loads Arena_MVP directly; the arena's existing readiness provider and Domain state machine start the match. Multiplayer and Tutorial are visibly disabled Coming Soon options. Their controller methods log status without loading scenes. Exit quits the player and stops Play mode in the Editor.

`MainMenuController` and `MatchUI` delegate scene changes to `SceneNavigation`. This layer uses SceneManager and has no access to gameplay rules. MainMenu's Awake/OnEnable and MatchUI's Victory display unlock and show the cursor. The menu buttons use serialized persistent callbacks; after-match buttons use MatchUI's lifecycle-managed listeners, removed in OnDisable/OnDestroy.

MainMenu uses a solid-color camera backdrop, a 1920 x 1080 overlay Canvas, TMP title and fixed vertical button stack. Both menu scenes have GraphicRaycaster and an EventSystem with InputSystemUIInputModule and the installed default UI actions, so mouse input uses the project's Input System. No legacy StandaloneInputModule is added.

On Victory, Play Again reloads the active build scene. It destroys the old GameManager and gameplay instances, creating fresh state, 10 HP, unarmed damage, an uncollected pickup and hidden Victory panel. Main Menu loads MainMenu in Single mode. No match state, stats, listeners or navigation controllers are kept alive between scenes.

## Regeneration and verification

Copy `tools/Build-OfflineMatchUI.cs` and `tools/Build-MainMenu.cs` into an isolated validation project's Assets/Editor, then run Unity with `-executeMethod SmallArena.Tools.MenuSceneBuilder.Build`. The existing bundled TMP resources must be present. The builder saves both UI scenes and the 0/1/2 build order.

`MenuNavigationTests` covers configured menu options and cursor state, Single Player callbacks, a simulated mouse click through the real UI raycaster, replay reset, return-to-menu/re-entry, lifecycle-managed button callbacks, and future-option no-op behavior. The mouse case assigns a fresh instance of the same default UI actions for the mock input runtime, then processes the real UI module alongside injected events to keep pointer frame flags deterministic. Exit's persistent callback is inspected without invoking Quit during the test run.

The saved-file verifier checks menu callbacks, interactability, UI input modules, after-match bindings and build order alongside arena geometry and HUD references. The seven-assembly boundary is unchanged. Native results are written to `TestResults/MenuNavigation-PlayMode.xml`; scene assertions are in `TestResults/ArenaLayout/results.json`. Tests execute in the matching cached validation copy so the open editor remains untouched.

Local validation: 176 native tests passed, zero failed, and two existing package tests were ignored. All seven new navigation cases passed. The saved-file verifier passed 2,674 assertions. All seven project assemblies compile with project warnings treated as errors; assembly graph and metadata checks pass.
