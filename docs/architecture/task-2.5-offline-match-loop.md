# Task 2.5: Offline lifecycle and HUD

Open `Assets/_Project/Scenes/Arena_MVP.unity` and enter Play mode. The scene's real `OfflineMatchReadinessProbe` validates the player input/movement/combat, fresh bot Health, uncollected weapon and bound HUD. GameManager then asks the Domain state machine to enter Playing. Missing dependencies keep startup in Initializing; entity presence alone is insufficient.

Bot Health publishes health changes and death. GameManager observes death and calls the Domain machine's existing guarded `TriggerVictory()` API. The private transition implementation remains encapsulated. A death during a Playing notification is handled immediately after that notification completes, preserving the Domain reentrancy guard.

Before publishing Victory, GameManager permanently ends this player's local combat interactions and disables input/movement. Further punches, direct weapon collection and damage-stat changes are blocked; dead Health continues rejecting damage. The camera and HUD remain active. Reloading the arena creates fresh lifecycle, health, damage and pickup state.

## Assembly boundaries

`MatchUI` lives in `SmallArena.Client` and observes the pure Domain contracts `IMatchStateSource`, `IHealthSource` and `IDamageSource`. Its serialized `gameManager`, `botHealth` and `playerCombat` fields are MonoBehaviour references wired to the concrete scene components, then read through these contracts. This avoids adding Client dependencies on Runtime or Server, which would create a cycle with Runtime's existing Client dependency.

GameManager, Health and CombatController implement those read-only contracts. Runtime remains the local composition module and references Client/Server. Client adds only the installed `Unity.TextMeshPro` and `UnityEngine.UI` package references; Domain retains zero engine and assembly references. All seven project assemblies remain acyclic.

## Presentation

`MatchCanvas` is Screen Space Overlay with a CanvasScaler reference resolution of 1920 x 1080 and Match Width or Height scaling. A top-left panel shows `Bot HP: current / max` and `Damage: value HP (Unarmed/Buffed)`. Keyboard hints are top-right. The centered bold `VICTORY!` banner starts inactive.

Health, damage and state events update the display without per-frame polling or layout groups. Enabling the UI refreshes all displayed values; disabling or destroying it removes all three subscriptions. GameManager also removes its bot-death subscription while disabled or destroyed and catches up if a dead bot is observed on re-enable.

The bundled TMP Essential Resources, including Liberation Sans font/materials and licenses, are included in `Assets/TextMesh Pro`. The scene builder is kept outside production assemblies in `tools/Build-OfflineMatchUI.cs`. To regenerate in an isolated validation project, first run `tools/Import-TmpResources.ps1 -ProjectPath <project>`, copy the builder into that project's `Assets/Editor`, and invoke `SmallArena.Tools.OfflineMatchSceneBuilder.Build` through Unity's `-executeMethod` option.

## Validation

- Native Unity run: 169 passed, zero failures, two existing package tests ignored. Eight new VictoryFlow tests cover startup, real keyboard punching, immediate UI updates, damage buffs, terminal behavior, subscription cleanup and readiness; a ninth test covers death during Playing notification.
- Saved-scene verifier: 2,260 layout/reference assertions passed, including source component bindings, overlay scaling, hidden initial banner and real offline startup wiring.
- Independent compilation: all seven project assemblies compile with project warnings treated as errors. Domain remains engine-free.
- Tests ran against the cached validation project because the original project was open in another Unity instance. Production source, scene and test hashes are compared against that snapshot after execution.

Native results are saved to `TestResults/VictoryFlow-PlayMode.xml`; the layout report is `TestResults/ArenaLayout/results.json`. These diagnostics and the validation project remain Git-ignored. Existing assembly edits and `Assets/_Recovery` content were preserved.
