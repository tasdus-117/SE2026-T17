You are responsible for updating requirements, architecture, roadmap, and persistent project documentation for a student game project.

Repository: https://github.com/tasdus-117/SE2026-T17

Perform concrete Markdown documentation changes. Do not implement gameplay, networking, tests, CI workflows, or Unity configuration.

1. OBJECTIVE

Create a coherent documentation baseline describing the game and decomposing development into bounded, independently assignable features with explicit dependencies and acceptance criteria.

This is a product and implementation roadmap, NOT a Unity/C# study schedule.

The team consists of three students learning Unity and C#, using Codex and Claude agents. The planning horizon is approximately 60 days from 2026-11-01. Do not restart that horizon from your execution date, invent weekly availability, or guarantee delivery.

The current implementation baseline is four-player online FFA. Preserve 2v2 as a future product direction outside the current 60-day implementation baseline.

2. REPOSITORY INSPECTION AND SAFETY

Before editing:
- Read applicable AGENTS.md files and relevant persistent project records.
- Inspect current branch, working tree, staged changes, ongoing Git operations, recent history, and available refs.
- Verify repository identity.
- Distinguish live evidence from stale documentation.
- Preserve unrelated committed and uncommitted work.
- Do not automatically stage, stash, discard, relocate, or delete files.
- Do not install dependencies or modify application/configuration files.
- Do not create or switch branches without authorization covering that action.
- I explicitly authorize committing and pushing documentation changes belonging to this task on the intended personal documentation branch, after verification.
- Review modified and untracked files, stage only task-related paths, inspect the staged diff, and choose a concise Conventional Commit message describing the actual changes.
- Push the current branch to origin; set upstream if needed. If there are no task-related changes, do not create an empty commit.
- Do not merge, cherry-pick, rebase, reset, force-push, create a PR, or delete branches.
- Do not edit project files on protected/shared branches.
- If repository rules prevent editing on the current branch, finish useful read-only discovery, identify the exact blocker, and propose the appropriate documentation branch. Do not bypass the rule.

Relevant branches:
- chore/23001901/project-agent-setup
- assignment/AD-065-23001901
- Proposed personal integration branch: long23001901

Historical observations, to verify:
- Setup branch: 1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e.
- Assignment branch: f4835ce35aea6bff79ce5fcd266813abb3c733bd.
- main: b5054a61258e8a4d274fe56198dd5d0a5304124b.
- long23001901 was absent.

Previously observed branch conventions:
- feature/23001901/<feature-name>
- docs/23001901/<document-name>
- fix/23001901/<bug-name>
- chore/23001901/<task-name>

Follow verified repository instructions. Do not use long23001901/feature/... merely because it appeared in an earlier discussion. Prefixes do not create branch ancestry or automatic synchronization.

Do not create long23001901 or select its foundation implicitly. Document the required foundation decision and documentation incorporation plan.

3. EXISTING MATERIAL AND EVIDENCE

Verify these previously observed paths:
- AGENTS.md
- docs/PROJECT_STATE.md
- docs/TASKS.md
- docs/DECISIONS.md
- docs/WORK_LOG.md
- Long_document/srs.md
- Long_document/learning-roadmap.md
- assignment/AD-065/README.md
- assignment/AD-065/implementation/CONTRACT.md
- Relevant assignment source and evidence.

Previous inspection found:
- SRS v0.2 describing an offline stationary-bot prototype.
- A minimal root README containing "git init".
- No Unity project in the two inspected branches.
- An isolated Node.js/JavaScript + SQLite authoritative combat experiment on the assignment branch.

Verify before repeating these conclusions.

The assignment experiment does not establish implemented Unity movement, real 3D hit validation, multiplayer sessions, cooldown, or reconnect.

Assess reusable concepts and test scenarios, such as authority, command deduplication, HP invariants, and exactly-once elimination. Do not copy code, transfer commits, or adopt HTTP/SQLite architecture by default.

Preserve assignment history and evidence. Do not import assignment-specific concurrency, retention, response-time, or database requirements.

Other branches may contain Godot/.NET or Go experiments. They are not automatically an approved Unity foundation. Do not broaden this task into an exhaustive audit of unrelated branches.

4. CONFIRMED PRODUCT BASELINE

Project:
Small Arena: multiplayer 3D authoritative server.

Goal:
Build a four-player 3D arena game where the server decides key match state to reduce cheating and client inconsistencies.

Confirmed:
- Unity + C#.
- Windows PC.
- Four participants per match.
- Current release: FFA, also called 1v1v1v1.
- Future direction: 2v2, outside the current implementation baseline.
- Combat is the main activity.
- One 3D arena with terrain and walls.
- Fall Guys inspires movement and camera behavior, not obstacle-course gameplay.
- Brawlhalla is a reference, not an implicit rules specification.
- Keyboard movement and left-mouse-button basic attack.
- Elevated third-person follow camera with fixed heading.
- No mouse-controlled camera orbit.
- Character faces movement direction and attacks forward relative to that facing.
- Both HP depletion and ring-out eliminate players.
- No respawn during a match.
- At least one collectible, usable weapon type in the FIRST online MVP.
- Private rooms joined by code.
- One participant hosts both the authoritative server role and their local client.
- No matchmaking.
- No admission of new participants into an ongoing match.
- Reconnect is mandatory.

Mandatory course MVP:
- Lobby.
- One arena.
- Movement and combat.
- Authoritative hit validation.
- Reconnect.
- Latency simulation.

Mandatory GitHub evidence:
- Protocol documentation.
- Load and latency tests.
- Cheat test cases.
- CI server and client builds.

Technology shortlist:
- Unity Netcode for GameObjects (NGO) OR Mirror, with C#.
- Choose one framework after evaluation and approval.
- Do not implement both.
- Godot is not the selected engine.

The authoritative-server requirement does not automatically require rented dedicated hosting.

5. CONFIRMED GAMEPLAY RULES

Combat:
- Unarmed basic attacks cause HP damage and knockback.
- Holding the initial melee weapon doubles unarmed damage and knockback distance.
- The 2x knockback requirement describes intended displacement, not an assumption that doubling physics force doubles travel distance.
- Specify reproducible measurement conditions for the knockback acceptance test.
- Initial HP, damage, knockback distance, attack range, cooldown, and other tuning values remain undecided unless current owner-approved evidence supplies them.
- Do not carry the old stationary bot's 10 HP and 1 damage into online PvP as confirmed values.
- No percentage-based damage/knockback system initially.
- No combos or alternate attacks in the current MVP.

Weapons:
- One weapon type is required in the first online MVP.
- Weapons do not stack.
- An unarmed character automatically picks up an available weapon upon valid contact.
- E drops the currently held weapon.
- E does not collect weapons.
- Holding one weapon is supported; no inventory system is required.
- The server resolves pickup contention and controls weapon ownership, dropping, damage, and knockback.
- Additional weapon types with different attack speeds, ranges, damage, and knockback belong to the future backlog.

Document unresolved weapon details as PROPOSED:
- Contact with another pickup while already armed.
- Dropped-weapon placement.
- Immediate re-pickup prevention.
- Weapon availability after elimination.
- Weapons falling out of the arena.
- Initial pickup count and spawn positions.

Unified elimination:
- A player is eliminated when HP reaches zero OR they cross the arena's ring-out boundary.
- Both causes use one server-authoritative elimination flow.
- Record HP_DEPLETED or RING_OUT as the cause.
- Apply elimination exactly once, including when both conditions occur in the same simulation step.
- Disable gameplay actions, remove the participant from the active contender set, transition them to spectator mode, and evaluate the match outcome.
- Eliminated characters cannot move, attack, collect weapons, or affect gameplay.
- Reconnect preserves elimination state.
- Being airborne is not elimination; define an explicit ring-out boundary.
- Determine contenders from authoritative state, not physical floor contact.

Match outcomes:
- Resolve all eliminations in the same authoritative simulation step before evaluating the outcome.
- One remaining contender wins.
- Zero remaining contenders produces a draw.
- With two or more contenders, continue until the match time limit.
- If at least two contenders remain at timeout, the match is a draw.
- Match duration is configurable; the initial value is undecided.
- Eliminated participants may spectate until the match ends or leave early.
- Spectator mode is confirmed; spectator camera controls remain PROPOSED.

2v2:
- Preserve its future-product traceability.
- Do not create mandatory current-release milestones or detailed implementation tasks for it.
- Earlier proposed team rules are not current MVP requirements.

6. ROOMS, DISCONNECT, AND RECONNECT

Rooms:
- Use private rooms joined by code.
- FFA is the only current mode; no mode selector is required.
- The room owner starts when four participants are connected and required loading/initialization is complete.
- A separate player-ready button is PROPOSED, not confirmed.
- No new participants may join an ongoing match.
- Reconnect restores an existing participant and is not late admission.

Reconnect:
- Grace period is configurable; initial duration is undecided.
- During disconnection, accept no new player input, but the avatar remains vulnerable and subject to arena rules.
- Verify the returning participant's identity.
- Restore current authoritative state, including HP, weapon, elimination status, and match state.
- Never reset HP, duplicate a participant, or revive an eliminated participant.
- A returning eliminated participant spectates.
- Grace-period expiry causes forfeiture.
- Document how forfeiture updates contenders and match results.

Session termination:
- Host departure or loss terminates the session with a clear message.
- Host migration is outside scope.
- Distinguish an aborted session from an authoritative win/draw; do not invent a winner after host loss.

Document these unresolved details as proposals:
- Intentional departure versus temporary interruption.
- How departure and reconnect-timeout forfeiture reuse idempotent contender-removal/outcome logic with distinct reason codes.
- Priority when elimination, timeout, and reconnect expiry coincide.
- Policy for reconnect attempts after match completion.
- Handling old, duplicate, delayed, or superseded connections and input.

7. ARCHITECTURE AND TECHNOLOGY ASSESSMENT

Briefly compare NGO and Mirror using official sources.

Assess:
- Compatibility with a suitable supported Unity version.
- Host/client and headless-server execution.
- Authority and movement synchronization.
- Private internet rooms, room codes, connectivity/NAT traversal or relay needs.
- Reconnect identity and state restoration.
- Network impairment tools.
- Client/server CI builds and licensing requirements.
- Service costs, quotas, operational effort, and limitations.

Recommend one direction with rationale, but mark unapproved framework/service/version selections PROPOSED.

Verify version-sensitive, pricing, licensing, quota, and service claims using current official sources. Cite URLs and verification dates. Mark unavailable evidence UNVERIFIED rather than inventing it.

Explain:
- Server authority versus transport versus session services.
- Host trust: remote-client validation does not prevent host-process tampering.
- Input ownership and local camera binding.
- Server ownership of movement validation, hits, HP, knockback, pickups, elimination, and results.
- Why server-owned HP alone is insufficient if arbitrary client positions or hits are accepted.
- How terrain, walls, knockback, and ring-out affect movement and synchronization.
- Where interpolation or prediction may be needed and what experiments would justify them.

Do not add databases, REST backends, microservices, persistent progression, or paid hosting without a concrete need.

No user-facing registration/account system is requested. Anonymous service authentication or participant identity may be proposed if needed for connectivity and reconnect; distinguish it from registration.

Support:
- Host plus three remote clients for the main demo.
- Client and headless server build artifacts for CI/testing.
- No assumption that dedicated hosting must be rented.

8. ROADMAP AND FEATURE CATALOGUE

Organize the roadmap into:
A. Foundation and mechanic/networking validation prototypes.
B. First playable online MVP containing all mandatory requirements.
C. Verified current FFA release.
D. Future product direction: 2v2 and additional weapons.
E. Other optional extensions.

Introduce a networking spike early, before building a large offline game. Validate two instances first, then four; include different-network connectivity verification.

A two-client spike is not the completed MVP.

Define observable, testable features covering:
- Reproducible Unity foundation and arena skeleton.
- Movement, jump, facing, and local camera.
- Early networking and ownership spike.
- Terrain/wall collision and ring-out boundary.
- Private room codes, capacity, loading readiness, and owner-controlled start.
- Authoritative spawning and match start.
- Authoritative movement validation/synchronization.
- Basic melee, cooldown, geometry/obstruction checks, and hit validation.
- HP, knockback, unified elimination, and match outcomes.
- Automatic weapon pickup, contention, E-to-drop, replicated state, and 2x effects.
- FFA lifecycle, results, and proposed repeat-session flow.
- Disconnect, reconnect, and authoritative resynchronization.
- HUD, errors, spectator mode, and session-abort feedback.
- Latency simulation and reproducible tests.
- Cheat/negative tests.
- Load testing appropriate to four-player matches.
- Client/server CI builds.
- Integrated release/demo acceptance.

For every current-scope feature provide:
- Stable ID and name.
- Observable outcome.
- Decision status, separate from implementation status.
- Included/excluded scope.
- Dependencies and sequence.
- Priority.
- Linked SRS requirements.
- Concrete acceptance criteria.
- Demo/verification procedure.
- Evidence to retain.
- Risks and open decisions.
- Shared scenes/prefabs/documents requiring coordination.

“Independently assignable” means bounded ownership with explicit dependencies, not absence of dependencies.

Use concise catalogue entries for all features. Create detailed feature specifications only for the next actionable feature(s), using a reusable structure for later expansion.

For every current milestone provide:
- Feature IDs.
- Playable outcome.
- Entry conditions.
- Definition of Done.
- Demo scenario.
- Evidence required.
- Remaining scope.

The FIRST online MVP must include:
- Four-player FFA.
- Lobby/private room flow.
- Arena and movement/combat.
- Authoritative hit validation.
- One collectible and droppable weapon.
- HP/ring-out elimination.
- Reconnect.
- Latency simulation.
- Required protocol/test/CI evidence.

Do not defer mandatory reconnect or weapon pickup beyond MVP to simplify planning.

Use the 60-day horizon for risk and scope control. Identify optional polish/content that can be cut while retaining mandatory requirements. Do not invent precise delivery schedules.

Do not launch sub-agents or parallel implementation in this task.

9. DOCUMENTATION STRUCTURE AND TRACEABILITY

Prefer existing documents and the smallest coherent set of files.

Inspect actual paths before editing.

Responsibilities:
- README.md: project overview, current evidence-based status, navigation, setup/build entry points.
- Long_document/srs.md: canonical requirements, release scope, FR/NFR/UC/AC/TC traceability.
- Long_document/learning-roadmap.md: learning only; update stale networking sequence.
- Product/gameplay document if needed: loop, rules, and decisions.
- Roadmap: milestones and sequencing.
- Feature catalogue and selective detailed specs.
- Architecture: topology, authority, ownership, synchronization.
- Protocol: messages/RPCs, validation, identity, ordering, reconnect, compatibility.
- Testing/evidence: acceptance, cheat, load, latency, CI, demo procedures.
- Existing docs/DECISIONS.md: significant decisions and supersession.
- Existing PROJECT_STATE.md, TASKS.md, WORK_LOG.md: actual progress and handoff.
- AGENTS.md: stable instructions and source-of-truth map.

Choose concrete paths consistent with the repository. Avoid duplicate normative sources; link by stable IDs.

Preserve existing FR/NFR/UC/AC/TC identifiers:
- Do not renumber.
- Record revisions, replacements, retirements, and rationale.
- Preserve historical meaning.
- Add new IDs where needed.
- Clearly distinguish the old offline prototype from the current online baseline.
- Do not retain obsolete offline assumptions as current requirements.
- Do not mark old requirements implemented without evidence.

Separate:
- CONFIRMED / PROPOSED / UNDECIDED.
- CURRENT / FUTURE / OUT OF SCOPE.
- Implementation status.
- Planned versus executed verification.
- Committed/pushed/integrated evidence.

Follow task statuses TODO, IN_PROGRESS, BLOCKED, DONE. DONE requires acceptance evidence.

Document future branch workflow:
- long23001901 becomes the integrated authority only after authorized establishment.
- Feature branches inherit the approved baseline.
- Specs/tests/state updates travel with feature integration.
- Branch memories do not synchronize automatically.
- No separate SRS is created merely because multiple branches exist.

Do not perform integration operations now.

Write explanatory Markdown in Vietnamese, retaining consistent technical terms and identifiers.

10. DOCUMENTATION-ONLY BOUNDARY

In this task:
- Describe CI, protocol, tests, and artifacts in Markdown.
- Do not create CI YAML, Unity projects, source code, executable tests, package manifests, or runtime configuration.
- Distinguish planned contracts/workflows from implemented ones.
- Keep framework-dependent protocol details provisional until framework approval.
- Do not invent commands, installed tools, package versions, or passing builds.
- Leave blocked implementation details documented while continuing independent documentation work.

11. VERIFICATION

Check:
- Markdown links and navigation.
- Unique/stable IDs.
- Feature -> requirement -> acceptance/test traceability.
- Milestone dependencies and absence of cycles.
- Complete mandatory MVP coverage.
- Consistent FFA-only current scope and future-only 2v2.
- Correct automatic pickup and E-to-drop semantics.
- 2x damage and knockback measurement clarity.
- Shared HP/ring-out elimination and simultaneous elimination tests.
- Reconnect without duplication, HP reset, or revival.
- Consistent host/server/client/room/match terminology.
- No invented approval, implementation, test result, version, price, commit, or PR.
- No imported assignment-specific constraints.
- Unity/C# recorded as confirmed.
- Stale project-state claims corrected using live evidence; historical logs preserved with appended updates.
- Documentation-only diff.

Documentation checks are not gameplay evidence. Do not claim unexecuted gameplay/build/load/latency tests passed.
Before committing, verify the current branch is authorized and is not protected/shared. Inspect staged paths and content for unrelated files, secrets, and generated artifacts. Commit and push only after the relevant checks pass. If push is rejected, report the blocker; do not automatically pull, rebase, merge, or force-push.

12. FINAL REPORT

Return:
- Verified branch/commit context.
- Summary of changes.
- Changed file paths and responsibilities.
- Current milestone/feature overview.
- Confirmed decisions and unresolved proposals.
- Networking recommendation with official sources and limitations.
- Verification actually performed and results.
- Blockers and owner decisions.
- Next bounded task with dependencies and acceptance criteria.
- Final Git status, commit message, commit SHA, pushed branch, and push result; explain any skipped or failed operation.
- Confirm that no merge, force-push, or PR creation was performed.


Continue useful independent work when details remain unresolved. Ask only when missing authorization or a consequential decision prevents safe progress. Never silently convert a proposal into owner approval.


