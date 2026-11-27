# TV3 source review — 27-Nov-2026

Agent-assisted review for TV3 @tasdus-117. Recommendation: **needs more evidence**
for final acceptance. This is source review, not a human PR approval, runtime
certification or rollout decision. No production changes, merge or cherry-pick.

## Reviewed identities

| Tracker | Reviewed revision | Scope |
|---|---|---|
| #40 / #13 | `1bc4e4a90900255e080f6c3ec65f9fd99dc12b2c` and merged `89f2674fa993ff13e30fcd56cca46cec5333494c` | No script diff between these revisions; #13 is merged, superseding open/draft wording in #40 |
| #20/#24 / #30 | `d34ecfae1bed68a7552c381e2b1f9504b92a7eb1` and merged `89f2674fa993ff13e30fcd56cca46cec5333494c` | Current audit/oracle preserved; historical text retains its dated scope; movement report differs after integration |
| #41 / #39 | `78130ba39c503476f91845df200ef6020966c7d9` | Fetched current open head; review diff against its own base `74a6c56f8e9baeddfa2ef11273b2bf5f21cf9837`, not a reverse diff removing merged movement/contracts |

## Movement / #40

`PlayerController.Update` submits world XZ intent and jump edges, with network
physics confined to server. `ServerPlayerMovement.ClearIntent` zeros move,
vertical velocity, pending jump, freshness and receipt time while retaining
sequence/jump watermarks. Disable, server stop, lost CharacterController and
CanAct revocation reach it. ReceiveInput rejects disabled/old sequence/wrong
life/non-finite input; jump edges are consumed airborne, not queued for landing.
FixedUpdate uses server delta, configured capped speed and receipt freshness.
`PlayerInputHandler` clears edges and raises GameplayInvalidated on focus,
pause/menu/disable/lifecycle changes; Value movement may perform its normal
initial held-state check on re-enable, as documented in source.

Existing tests cover restored fresh sequence, revoked jump, counter wrap,
grounded/airborne jump and replica authority. Source inspection found no new
TV3 defect requiring a production patch. Real held movement/jump through focus,
pause, disable, death/respawn and rematch remains unverified in this review.
Do not call #13 merge a reviewer approval. Original HP9/HP10 CI failure and
#42/#43 follow-ups remain separate; an unchanged retry is not root-cause proof.

The unseen higher-sequence/current-life packet delayed across temporary revoke
can still qualify by **server receipt time** after restore. Whether this is
permitted is the existing TV1/TV2 contract question in #40, not a TV3 protocol fix.

## Camera, aim, layers, HUD and oracle / #20/#24

`ThirdPersonCameraController` fixes yaw0/pitch30 and follows only the local
actor. `MouseAim` intersects the camera ray with a horizontal plane at the
serialized muzzle; zero/invalid rays are rejected. Player.prefab's ShotMuzzle
is `(0,.25,0)`, matching `NetworkCombat.ShotMuzzleOffset`; server derives its own
origin. Environment6/Player7/Bot8 ray layers ignore triggers/self, nearest
collider determines hit/block, Environment overlap handles origin inside cover.

Enabled HeldFireInput references the existing shot sink. NetworkCombatInput
suppresses bound mouse fallback while that adapter is active; E and legacy J
remain in the existing collector. No second LMB collector is introduced.
Presentation adapter joins matching session/life/phase health/combat/score
replicas; binding rejects stale context/revision and clears on detach. HP and
score are authoritative, never predicted. HUD maps Hit/Missed/Blocked/rejection
to literal copy; kill feed distinguishes environment death, fades after4s.
Tab is hold-to-show, hidden when paused; result follows replicated standings.

Oracle O06–O18 literal HP, actual damage, K/D, environment feed, respawn,
protection, loot30s, ranked result and Ready/feed reset align with inspected
consumers. O19 requires device/window evidence. M3 remains **H10/D1/C1/R1.8**,
pickup adds1, 5kills/180s; gun **H100/D25/C.5/R30** is a Configure-seam profile,
not the expected M3 HUD. Maintainers own #29 acceptance/rollout; enabled wiring
does not settle that decision. No claim of LAN or four-client convergence here.

## Cooldown candidate / #41

Reviewed new tests first: independent before/exact/after.5 boundaries, invalid
config, .75 custom interval, future timestamp, collider-surface30m, D17/custom
and D25/profile, protected victim, normal Command and shared melee deadline.
Reviewed all three changed runtime files and unchanged client consumers.

`BeginAttack` samples ServerNow once, commits through Session.TryAttack, then
Publish copies Character.NextShotAtSeconds into the existing combat SyncVar.
Bound TryBeginAttack delegates to runtime; legacy/unbound still uses local
deadline. ResetForLife preserves bound deadline while resetting damage. Accepted
hit/miss/cover consumes once; rejection does not publish a new gameplay deadline.
Client cadence remains a traffic limiter using replicated configured interval;
it does not decide server hit/cooldown. Cadence reset requires release before
held-fire resumes; it drops accumulated time rather than bursting. Feedback
is shooter-only and guarded by session/life/phase, with lifecycle invalidation.

No concrete regression found in the scoped source review across correctness,
readability, architecture, authority/security and performance. BeginAttack's
Publish is an existing bounded four-player path; no new dependency/protocol.
Fresh focused native verification used a separate exported `Candidate39` project,
never merged/cherry-picked into main or the acceptance branch. Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File TestResults/TV3-20261127/Candidate39-run/Run-Native.ps1 -Phase PlayMode -Filter SmallArena.Tests.PlayMode.NetworkShotAndPresentationIntegrationTests
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-UnityTestResults.ps1 -XmlPath TestResults/TV3-20261127/Candidate39-run/playmode.xml -MinPassedCount 12 -MinProjectPassedCount 12 -MaxVendorSkippedCount 0
```

Unity/runner exit0, gate exit0, **12/12 project pass**, 0 fail/skip/inconclusive.
All 2915 exported tracked paths checked against Git blob hashes and post-test
files. Scripts/tests/prefabs match head; Unity import cleared only the serialized
`m_RuntimeSettings.m_List` in UniversalRenderPipelineGlobalSettings.asset. Actual
tested file hashes and this config deviation are in ignored
`candidate39-source-manifest.json`; original is preserved separately. This is
native head-code evidence with that recorded import setting, not a candidate
player build or exact unchanged serialized-config certification.

Deliberate mutation in this isolated copy replaced bound TryBeginAttack's
runtime guard with `return true`. The single existing test
`GunRules_DeterministicRuntimeBoundaryAndOutcomeCooldown` failed (Unity exit2,
one failure); original bytes restored in `finally` and SHA256 equality checked.
Successful original 12-test run and red mutation use separate output directories;
this is a sensitivity check, not an unchanged retry to hide a failure. One early
launch before extraction finished failed with missing runner and ran no test.

Raw ignored [focused XML](../../TestResults/TV3-20261127/Candidate39-run/playmode.xml),
log/args/exit and [mutation XML](../../TestResults/TV3-20261127/Candidate39-mutation/playmode.xml).
Author/CI counts remain historical, not added to these totals. No candidate
build, full native suites, hardware held-fire/cadence, latency/LAN or lifecycle
window evidence. Recommendation remains needs more evidence for final #41/#26/#27;
no human approval or profile/rollout decision inferred.
