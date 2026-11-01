# Learning & Research Plan

## Document metadata

| Field | Value |
| --- | --- |
| Project | Unity 3D Arena Combat Game |
| Document | `learning-roadmap.md` |
| Version | 0.2 |
| Status | Proposed study and research plan; no completed study or experiment claimed |
| Last updated | 2026-10-24, Asia/Bangkok |
| Source | Learning content extracted from SRS version 0.1 |
| Technical baseline | [Software Requirements Specification & Technical Design](srs.md) |
| Resource verification | Existing source checks dated 2026-10-23; links not reverified during this editorial split |

## 1. Purpose, prerequisites and boundaries

Prepare a Java/OOP developer new to Unity, C#, 3D physics, character controllers, combat, animation, AI and networking to build the specified MVP. Existing experience with software engineering, REST APIs, basic architecture and Git/GitHub is useful; Unity adds a frame-driven lifecycle and scene/component references.

This file owns prerequisite explanations, study resources, personal checkpoints, time-based learning plans and research tasks. [The SRS](srs.md) owns confirmed/proposed/future scope, technical decisions, requirements, acceptance criteria, test cases and delivery gates. Study exercises and research do not add product requirements or approve proposed decisions. Use the SRS IDs when recording findings; do not duplicate or renumber them here.

## 2. Priority and learning sequence

| Priority / order | Study focus | Ready to support |
| --- | --- | --- |
| P0 — 1 | C# syntax, references, null handling, Unity lifecycle and serialization; Editor, scenes, GameObjects, components and prefabs | M1 project setup |
| P0 — 2 | Vector3, coordinate frames, Input System, CharacterController, gravity, grounding, collisions and Cinemachine | M2 player/camera |
| P0 — 3 | Properties, events, enums, Health invariants and Canvas/UI | M3 stationary bot/HP |
| P0 — 4 | Attack phases, timing, hit queries, layers, deduplication and damage calculation | M4 punch combat |
| P0 — 5 | Trigger configuration, interaction eligibility, stats changes and once-only collection | M5 pickup |
| P0 — 6 | Match states, event ordering, pending-action cancellation and terminal UI | M6 victory |
| P1 — 7 | Edit/Play Mode testing, Windows builds, Profiler and usability evaluation | M7 testing/demo |
| P2 — later | Optional Animator polish, ScriptableObject configuration and broader sample study | Only if needed within existing scope |
| Future only | Ownership, authority, synchronization, latency and networking framework evaluation | Separate multiplayer phase after offline acceptance |

The ordered explanations below can be revisited as each milestone begins. Delivery objectives and Definitions of Done remain in [SRS development milestones](srs.md#part-6--development-milestones).

## 3. C# bridge from Java

| Topic | Difference / application | Priority |
| --- | --- | --- |
| Syntax and types | Namespaces and `using`; methods conventionally PascalCase; `bool`, `string`, `var`; structs such as Vector3 have value semantics | First |
| Properties | Controlled getter/setter access can expose read-only current HP; Unity usually serializes fields rather than ordinary properties | First |
| Events and delegates | Typed callback references and restricted event publication resemble observer/listener patterns | First; HealthChanged/Defeated |
| Interfaces | Contracts work similarly to Java; an IDamageable interface is optional for one target, not mandatory architecture | Later if reuse needs it |
| Collections | `List<T>`, `Dictionary<TKey,TValue>`, `HashSet<T>`; use target identity sets to deduplicate colliders | During combat |
| Enums | Named finite states make match transitions explicit | First |
| Components and references | Assign components through Inspector or obtain once; a reference to a prefab asset differs from a scene instance | First |
| Null handling | Ordinary C# null rules plus UnityEngine.Object equality behavior for destroyed native objects; validate required references explicitly | First |
| Lifecycle | Awake for local setup, OnEnable/OnDisable for subscriptions, explicit match initialization/Start for coordinated readiness, Update for frame logic | First |
| Serialization | Inspector fields persist in scenes/assets; changing script defaults may not replace serialized instance values | First |

Do not assume Microsoft Learn examples using newer C# features automatically compile in Unity. Stay within the chosen Editor's documented language subset.

## 4. Unity fundamentals needed for this MVP

This section is a targeted study guide, not an implementation tutorial. Prioritize concepts used in the next milestone rather than completing every course first.

| Concept | Working mental model for a Java developer | MVP exercise / priority |
| --- | --- | --- |
| Scene / GameObject | Scene is a saved object graph; GameObject is an entity holding components | Build one floor, player, bot and pickup; essential |
| Component architecture | Composition of focused objects instead of one deep inheritance hierarchy | Separate movement, combat and Health; essential |
| Transform | Local/world position, rotation and scale with parent relationships | Compare a player child hit point with world position; essential |
| MonoBehaviour | Engine-managed component receiving lifecycle callbacks, not a normal service constructed with `new` | Attach a script and assign references; essential |
| Prefab | Reusable serialized object template with overrides | Save player/bot/pickup and inspect instances; essential |
| Update / FixedUpdate | Frame callback / fixed simulation callback, unlike REST request handling | Sample input/move CharacterController per frame; understand physics cadence; essential |
| Rigidbody | Physics body affected by forces or controlled kinematically | Understand pickup kinematic body; do not add dynamic player body; essential basics |
| Collider / Trigger | Solid shape blocks motion; trigger reports overlap without blocking | Floor/wall collisions and pickup candidate range; essential |
| CharacterController | Capsule collision motor whose movement is explicitly requested | Move, apply gravity and gate jumps; essential |
| Animator | State machine driving visual animation | Optional basic punch visual; defer root motion and animation-driven hit timing |
| Input System | Input adapter mapping device bindings to logical actions | Move, Jump, Punch, Interact action map; essential |
| Cinemachine | Camera behavior configuration around follow targets | Fixed-yaw follow and damping; essential |
| Canvas / UI | Scene UI hierarchy, anchors and event-driven presentation | HP, controls, pickup feedback, victory; essential |
| ScriptableObject | Asset containing shared configuration, not a database | Optional tuning asset; never store per-match HP in shared configuration |

## 5. 3D and combat essentials

1. **Vector3 and coordinates:** +Y is up, XZ is the floor; distinguish points, directions and distances. Compare local chest-forward offset with world position.
2. **Movement and time:** velocity is distance per second; displacement depends on elapsed time. Flatten camera axes and clamp combined input.
3. **Gravity, grounding and jumping:** keep vertical velocity separate; only grounded fresh presses start a jump. Verify floor contact and wall contacts separately.
4. **Collision and queries:** colliders define contact geometry; triggers provide proximity. Raycasts check a line, SphereCast checks a swept volume, OverlapSphere checks a volume at one instant.
5. **Third-person camera:** follow position, orientation, framing and damping are separate concerns; a following camera can still lose sight of the bot.
6. **Combat rules:** an input request, accepted attempt, hit sample, valid hit and damage application are distinct events. A miss is an accepted attack with no valid target.
7. **Health and death:** centralize clamping and once-only defeat; treat post-defeat damage as rejected, not another death event.
8. **Pickup and modifiers:** collection changes player stats; attack resolution reads stats; current enemy HP is independent and never reset by collection.
9. **Match states:** finite states guard allowed actions. UI subscribes to outcomes rather than controlling rules.

## 6. Prioritized 7-, 14-, and 30-day plans

Planning assumption: approximately 2–3 focused hours per person per day, 2–3 students, shared integration time, and placeholder assets. These are capacity estimates, not delivery guarantees. Seven days is an accelerated prototype goal; 30 days is the safer academic baseline. The courses in Section 9 take longer to complete in full; use selected lessons.

| Horizon | Days / priorities | Expected checkpoint |
| --- | --- | --- |
| 7-day accelerated | D1 Editor/C# essentials, Git, URP, input and arena; D2 CharacterController movement/grounding; D3 jump/camera and decision review; D4 stationary bot/HP/HUD; D5 punch/cooldown/hit validity; D6 pickup/victory; D7 integrated test and build | Rough complete match if capacity permits; visual polish and broad quality assessment remain limited |
| 14-day practical | D1–2 setup and C# bridge; D3–5 movement/camera; D6 bot/Health/UI; D7–9 combat and edge tests; D10 pickup; D11 victory/gating; D12–13 regression/build/usability; D14 demo/document update | Playable MVP with focused automated tests and recorded manual acceptance evidence |
| 30-day recommended | D1–4 selected Unity Essentials/C# and setup; D5–9 movement/camera; D10–12 Health/UI; D13–17 combat; D18–20 pickup; D21–23 victory/integration; D24–27 tests/profile/usability; D28–30 fixes/build/report/demo buffer | Reviewed MVP and reproducible Windows build; no online scope added |

## 7. Research tasks and decision evidence

These tasks investigate existing SRS decisions and risks. They are proposed work, not execution evidence or additional MVP scope. Store conclusions against the linked decision, risk or test ID in the SRS.

### 7.1 Before and during the offline MVP

- [ ] **M1 / R-05:** Check the selected Editor and released package dependencies in a clean project; record exact versions and Windows build compatibility in [the technology baseline](srs.md#21-recommended-baseline-and-compatibility-policy).
- [ ] **M2 / D-01–D-03 / R-01–R-02:** Compare fixed facing with the team's alternative turning behavior; evaluate camera framing, jump stability and bot identification before approving detailed combat.
- [ ] **M2–M5 / R-04:** Investigate CharacterController grounding, collision layers and the pickup trigger/kinematic-body configuration in controlled scenes.
- [ ] **M4 / D-04, D-08–D-09 / R-03:** Evaluate proposed punch input, cooldown, hit dimensions, line of sight and duplicate-collider handling using existing TC expectations.
- [ ] **M5 / D-05–D-06 / R-07:** Review pickup interaction, damage value and wind-up ordering; obtain team decisions without treating study results as requirement approval.
- [ ] **M7 / NFR-001–NFR-007 / R-09:** Study the existing measurement and test procedures before gathering actual evidence; keep product acceptance in [the SRS test catalogue](srs.md#part-5--mvp-testing-strategy-and-planned-test-catalogue).
- [ ] **R-10–R-11:** Review asset/tool licenses and available study capacity before committing to imported content or the accelerated schedule.

### 7.2 Future multiplayer research — after offline acceptance

- [ ] Review the [future architecture and technology comparison](srs.md#part-7--future-four-player-pvp-architecture); recheck future package compatibility, pricing, quotas and licensing before adoption.
- [ ] Conduct a separate two-client then four-client spike to compare NGO, Mirror or Fusion options for movement feel, hit fairness, setup effort and hosting constraints.
- [ ] Investigate ownership, local camera binding, authoritative attacks/health/pickups, spawning and match synchronization.
- [ ] Evaluate representative 50/100/200 ms RTT with jitter/loss as already proposed in the SRS; assess interpolation, prediction/reconciliation and bounded lag compensation only where evidence warrants them.
- [ ] Resolve future FFA elimination, respawn, draws, late join and pickup rules, disconnect/host-departure policy, and eventual 2v2 team rules through the SRS decision process.

## 8. Personal study checkpoints

These checklists track understanding and research progress. They do not replace M1–M7 delivery gates, and completing them does not mean gameplay tests passed.

- [ ] Before M1: explain the scene/component model, serialized references and the C# compatibility boundary; complete selected Editor/C# lessons.
- [ ] Before M2: explain horizontal vectors, delta time, explicit gravity, grounded-only jumping and camera update order.
- [ ] Before M3: explain HP ownership, clamping, properties, event subscriptions and once-only defeat.
- [ ] Before M4: distinguish input request, accepted attempt, hit sample, valid target and damage; explain collider deduplication.
- [ ] Before M5: explain trigger versus interaction validation and why collection must not reset enemy HP.
- [ ] Before M6: explain match transitions and cancellation of pending combat work.
- [ ] Before M7: distinguish planned expectations from recorded results and explain Edit Mode, Play Mode, manual and performance verification.
- [ ] After offline acceptance only: explain authority versus transport versus session services before starting the multiplayer research spike.

## 9. Learning references

URLs below were checked through official source pages/search on 2026-10-23. A reachable resource is not proof every example matches 6000.3. Prefer versioned manuals over older video inspector layouts.

| Ref | Resource | Use and scope |
| --- | --- | --- |
| R12 | [Unity Learn — Unity Essentials](https://learn.unity.com/pathway/unity-essentials) | Editor, objects, scenes and basic scripting; select relevant units first |
| R13 | [Unity Learn — Junior Programmer](https://learn.unity.com/pathway/junior-programmer) | Longer practice path; selected OOP, debugging and project lessons |
| R14 | [Microsoft Learn — C# documentation and learning entry](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/) | Language bridge; cross-check feature support with Unity |
| R15 | [Unity Learn — 3D Beginner Roll-a-Ball, Unity 6.0](https://learn.unity.com/course/roll-a-ball/unity-version?version=6.0) | Beginner sample for colliders, collection and UI; Rigidbody ball locomotion is not this MVP's character motor |
| R06 | [Unity CharacterController.Move API article — SRS reference R06](srs.md#81-official-references) | Small official API example; gravity and jump behavior |
| R07 | [Unity Physics.OverlapSphere API article — SRS reference R07](srs.md#81-official-references) | Query behavior and target filtering; do not copy generic examples without deduplication |
| R08 | [Cinemachine 3.1 manual — SRS reference R08](srs.md#81-official-references) | Current chosen camera family concepts and setup |
| R22 | [Unity YouTube — Using Cinemachine: Getting Started](https://www.youtube.com/watch?v=x6Q5sKXjZOM) | Official introductory follow/focus video; 2017 UI/API is historical, use for concepts only |
| R23 | [Unity YouTube — Cinemachine Tutorial #1: Basics](https://www.youtube.com/watch?v=8aYvX7JyYGI) | Second official conceptual video; likewise predates Cinemachine 3 |
| R16 | [Unity CharacterControllerSamples](https://github.com/Unity-Technologies/CharacterControllerSamples) | Official advanced package samples; uses a different character-controller package/ECS context, optional later study rather than MVP foundation |

## 10. Document history

| Version | Date | Change |
| --- | --- | --- |
| 0.2 | 2026-10-24 | Extracted existing learning explanations, resources and 7/14/30-day plans; organized study sequence, research tasks and personal checkpoints with links to authoritative SRS decisions and delivery gates |
