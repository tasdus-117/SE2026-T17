# TV2 source audit and integration packet — 25-Nov-2026

Prepared by Codex for Long / 23001901 / @HoangLong205. Source inspection is
separate from runtime execution and human approval. Runtime baseline is main
`74a6c56f8e9baeddfa2ef11273b2bf5f21cf9837`, Unity6000.6.8f1 (237b6593792b).
PR #31 merged as49ab265836217886046c3d2a4a1d20c4356d0b96; #37 as
8fba1c127f650615faec4f14b34b16da7552c858; #38 as the baseline above.

## CT / FR / AC / test map

All source paths below are relative to `Assets/_Project/`. Method references
identify the audited entry points; the linked source files carry current lines.

| Contract / requirement | Production ingress, gates and server mutation | Feedback / state / evidence |
|---|---|---|
| CT-201 / FR/AC-201,202 / TC-201,202 | `Client/Presentation/PlayerController` → [NetworkMovement](../../Assets/_Project/Scripts/Networking/NetworkMovement.cs) owner unreliable Command; MovementInput uint Sequence/JumpSequence, float MoveX/MoveZ, long LifeId. [ServerPlayerMovement](../../Assets/_Project/Scripts/Server/Simulation/ServerPlayerMovement.cs) checks current life, CanAct, finite/clamped world XZ, serial uint order. Trusted receipt age <= configured timeout; speed min(5, config), fixed server delta; grounded jump only | Reliable server-to-client ArenaNetworkTransform includes owner. NetworkMovementTests, NetworkMatchLifecycleTests; #13 remains separate movement-revocation candidate, absent from this baseline |
| CT-202 / FR/AC-204 / TC-203,204 | [HeldFireInput](../../Assets/_Project/Scripts/Client/Input/HeldFireInput.cs) → IShotIntentSink → [NetworkShotIntentAdapter.CmdSubmitShot](../../Assets/_Project/Scripts/Networking/NetworkShotIntentAdapter.cs). ShotIntent: float AimX/AimZ, double Timestamp, ulong Sequence, long LifeId. Reliable owner Command; exact authenticated active connection/identity, bound Alive/current life, InProgress/CanAct before ShotRequestGate. Strict increasing per-life sequence; processed rejection consumes sequence, never gameplay cooldown. Age <=.25s; future timestamp clamped to server now, no lag compensation | No client origin, target, hit, damage, HP or score. Server gate reset on new life; no app resend. Legacy CmdPerformAttack(Vector3,long) remains +Z melee fallback; CmdRequestPickup(uint,long) remains E interaction |
| CT-203 / FR/AC-203,204,205,207 / TC-203–207 | [NetworkCombat.PerformShot](../../Assets/_Project/Scripts/Networking/NetworkCombat.cs) normalizes horizontal direction; origin=server transform.position+(0,.25,0); configured ShotRangeMetres. Environment overlap radius .02 blocks origin-inside cover. RaycastAll layers6/7/8, ignore triggers/self. Nearest eligible same-session target; other collider is blocker; cover wins equal distance. ShotHitRules uses inclusive collider-surface range. One TakeDamageFrom call → lifecycle Damage → MatchSessionRuntime.TryDamage, actual HP loss, one death/kill per life | Shooter-only reliable TargetFeedback(sessionId,lifeId,request,outcome,rejection). Hit/Missed/Blocked consume accepted cooldown; rejection changes no gameplay HP/score/deadline. MatchKillFeedEntry is retained **five-entry SyncList**, not elimination RPC. NetworkRaycastOcclusionAndHudTests, NetworkShotAndPresentationIntegrationTests, ShotResolutionAndPresentationCleanupTests, NetworkScoreboardAndRematchTests |
| CT-204 / FR/AC-208 / TC-205–209 | [NetworkPlayerPresentationAdapter](../../Assets/_Project/Scripts/Networking/NetworkPlayerPresentationAdapter.cs) reads replicated health/combat + same-session MatchState scores/life/phase/clock. Context/revision is **local presentation ordering**, not a global wire revision. Server state SyncVars and scores/feed SyncLists remain authoritative | PlayerPresentationBinding → real HUD. Shooter feedback hides after1.5s and clears death/respawn/end/Lobby. KillFeedHudView reads Feed and fades entries after4s. NetworkHealthHUD remains a live consumer; adapter module is now production-wired. No fabricated provider required |
| CT-205 / FR/AC-201,202,208 / TC-201,206,208 | [ServerMatchLifecycleController](../../Assets/_Project/Scripts/Server/Sessions/ServerMatchLifecycleController.cs) binds exact connection/session/participant/life; ServerNow is monotonic max across frame/fixed/Command contexts. BeginSession seals roster; death Publish clears motor/vertical input, disables collision; respawn clears before teleport/new-life Publish. Waiting/Countdown/Ended and non-Alive reject gameplay; protection blocks attack/damage, permits movement | PlayerInputHandler lifecycle, menu/focus/disable/despawn invalidate collection; adapter clears context and old feedback. No total order across reliable Commands and unreliable movement. Retained-session reconnect/proof/barrier remains later TV1 scope. #13/#26/#27 must be reviewed on final integration build |

## Wiring and profiles

Inspected Player.prefab, NetworkHUD.prefab, ArenaNetworkRoot.prefab, MatchState.prefab,
Arena_MVP and MainMenu/Bootstrap scene order. Player already contains enabled
NetworkShotIntentAdapter, NetworkPlayerPresentationAdapter and HeldFireInput;
HeldFireInput's sinkProvider references the shot adapter and its muzzle anchor is
serialized. NetworkCombatInput suppresses the second bound LMB collector when the
adapter is active. Thus raycast/held-fire wiring **already exists and is enabled
in source**, despite #29 retaining melee acceptance. This is a scope/approval
discrepancy for maintainers; this docs PR does not enable/disable components.

Approved08-Nov TV2 rules: H100/D25/C.5/R30, infinite ammunition/no reload.
Observed BeginSession default: H10/D1/C1/R1.8, pickup adds1 damage; speed5,
freshness.25, countdown3, duration180, respawn3, protection1, safe distance1.5,
loot30, targetKills5, unlimited lives, postmatch10. Existing Configure seam can
supply the approved profile. No evidence approves production gun tuning rollout.

Main still gates bound attacks with both NetworkCombat.nextAttackTime and Domain
NextShotAtSeconds, calculating a second deadline after mutation. PR #39 at
`78130ba39c503476f91845df200ef6020966c7d9` independently unifies that deadline;
it is not merged/copied here. Its scoped protocol addition must be retained when
maintainers integrate both PRs. Both branches append independent sections to
tv2-004-contracts.md; reconcile both sections, do not select one whole file.

## Remaining decisions and acceptance

| Gap / source | Owner | Exact next action / required evidence |
|---|---|---|
| #29 melee scope versus enabled #31/#37 held-fire/raycast wiring | Maintainers + TV1/TV3, TV2 rules | Review baseline74a6c56 and #39 head78130ba; explicitly choose acceptance route and approve or defer H100/D25/C.5/R30 rollout, ammo and pickup policy. No implicit approval from CI |
| Movement revocation in motor | TV1 review/integrate #13; TV3 #26 | Review final #13 head, preserve uint movement/jump watermarks; test disabled/control-loss/death/respawn and fresh input, then record integrated SHA/build |
| Raycast policy versus original FR-204 (non-target colliders/protected/dead handling) | TV1/TV3 technical GD-09, TV2-006 later | Review PerformShot target.CanAct/else-blocker policy, muzzle/inside-cover/layers, protected-front-target and multi-collider geometry; expected + actual traces. No new FR-204 implementation in this run |
| Reconnect and global baseline/wire policy | TV1 TECH-001/GD-09 | Approve proof/rebind/revocation/grace/current-state barrier and clock/order/wire decisions; current destroy-on-disconnect is not retained reconnect |
| Final contract acceptance #20 | TV1 + TV3; TV2 records | Actual reviewer/date/head/outcome plus #24 oracle review and #26/#27 same integrated SHA/build; historical notes and automated review do not substitute |
| #19 scaffold closure | TV3 | Current code omits root Assets.meta; owner still supplies negative missing-child/duplicate/orphan fixtures and human review, not issue closure from positive gate |

Saved Player–Player collision is enabled and ArenaConfigurationTests expects it.
Current ArenaConfigurationVerifier now includes layer7 in Player's partners;
the pair7/7 failure reported22-Nov is historical, not a current open mismatch.
Fresh verifier outcome is recorded in this PR's results, separately from tests.

Use [the four-player oracle and runnable matrix](tv2-four-player-oracle.md).
No final integrated SHA has yet been agreed: #13/#39 are drafts and #26 has no
real-window evidence. LAN, impairment and human visual/input QA remain separate.
