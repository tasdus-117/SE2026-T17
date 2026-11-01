# Small Arena: AI Agent Codex Operating Framework & System Engineering Manual

This document synthesizes the **Software Requirements Specification (`srs (1).md`)** for *Small Arena: 3D Combat Game* with principles from **Software Engineering Lectures 04–07 (VNU 2026)**:
- **Lecture 04:** Architecture, Module & Runtime Views, Quality Attributes, ADRs
- **Lecture 05:** Cohesion, Coupling, SOLID, Code Smells, Technical Debt Registers
- **Lecture 06:** Agentic Engineering (Spec-Plan-Generate-Verify-Own), Context Engineering, Guardrails
- **Lecture 07:** Risk-based Testing, Equivalence Partitioning, Boundaries, State Transitions, Mutation Testing

This acts as the master working context and operational protocol for autonomous or semi-autonomous AI coding agents (such as Codex, Claude Code, or Cursor) working on the repository.

---

## 1. Executive Summary: `srs (1).md` (Small Arena MVP)

### 1.1 Project Identity & Scope Boundaries
* **Vision:** Windows PC, 3rd-person 3D arena PvP combat game for up to 4 players Free-For-All (FFA), drawing inspiration from *Fall Guys* (movement/visuals) and *Brawlhalla* (arena melee/pickups).
* **MVP Scope (Confirmed):** 
  - Single-player local offline match in a bounded, flat 3D arena ($16\text{ m} \times 16\text{ m}$).
  - One keyboard-controlled player (`WASD` movement, `Space` jump, `J` punch, `E` interact).
  - One stationary bot target with $10\text{ HP}$, no movement, no AI, no attacks.
  - Base unarmed punch deals $1\text{ HP}$ damage.
  - Exactly one weapon pickup modifying punch damage to $2\text{ HP}$ (no stacking, no dropping, single match lifetime).
  - Victory triggers once when bot reaches $0\text{ HP}$; combat actions freeze immediately.
* **Explicit MVP Non-Goals (Strict Out-of-Scope):** Networking, NGO/Relay, 4-player FFA, player damage/death, inventory systems, moving bots, menus/pausing.

### 1.2 Key Architectural & Mechanistic Decisions (Decision Register D-01 to D-12)
* **D-01 to D-03 (Locomotion & Facing):** Fixed camera yaw aligned to world $+Z$, elevated follow offset. Player movement is mapped to $+Z/+X$. Player root facing remains $+Z$; punching fires strictly forward along $+Z$.
* **D-04 to D-06 (Combat & Stats):** Fresh press `J` fires attack (no auto-repeat). Recovery/cooldown is $0.50\text{ s}$ (wind-up $0.10\text{ s}$, sample at $0.10\text{ s}$, recovery $0.40\text{ s}$). Hit detection sphere radius $0.50\text{ m}$, center $1.0\text{ m}$ forward. Weapon pickup at $1.5\text{ m}$ sets punch damage from $1 \to 2\text{ HP}$.
* **D-10 to D-12 (Match Lifecycle):** Defeat freezes movement and rejects combat inputs. Direct auto-start into arena; flat closed floor with high walls.

### 1.3 Technical Stack
* **Engine:** Unity 6.3 LTS (6000.3) with Universal Render Pipeline (URP).
* **Runtime Language:** C# 9 (Unity-supported subset; avoid records for serialized objects).
* **Locomotion:** Unity `CharacterController` with explicit gravity integration (do not use dynamic `Rigidbody` on the player).
* **Input & Camera:** New Input System (`InputActionAsset`), Cinemachine 3.1.
* **Testing:** Unity Test Framework (UTF) with EditMode (pure C# logic) and PlayMode (physics/scenes).

---

## 2. Theoretical Principles Derived from SE Lectures

| Lecture | Core Tenet | Implementation in Small Arena Agent Workflow |
| :--- | :--- | :--- |
| **L04: Architecture** | Structure connects quality attributes to components; boundaries preserve modularity; ADRs record costly decisions. | Maintain clear module boundaries: `Core`, `Input`, `Player`, `Combat`, `Health`, `Pickups`, `UI`. Avoid global singletons, service locators, or UI scripts modifying state. |
| **L05: Code Quality & Debt** | High cohesion, low coupling, information hiding, SOLID; technical debt must be tracked with evidence and repayment triggers. | Pure C# helpers for logic (`DamageCalculator`, state invariants). Keep Unity lifecycle (`MonoBehaviour`) decoupled from pure rule evaluation. Log debt in `DEBT.md`. |
| **L06: AI & Vibe Coding** | "Vibe coding" without guardrails causes architecture drift. Replace with: **Spec $\to$ Plan $\to$ Generate $\to$ Verify $\to$ Integrate**. | Agents must generate small diffs, adhere to a strict task contract, never edit out-of-scope files, and verify with tests before claiming completion. |
| **L07: Quality Engineering** | Tests are evidence, not proof of perfection. Focus on risk coverage, partitions, boundaries, and deliberate mutations. | Unit test combat formulas; integration test trigger overlaps; boundary test cooldowns ($0.49\text{ s}$ rejected vs $0.50\text{ s}$ accepted) and health clamps ($1 - 2 = 0$). |

---

## 3. Recommended Repository & Working Directory Structure

```text
small-arena/
├── .github/
│   ├── workflows/
│   │   └── unity-ci.yml             # Automated UTF runner (EditMode + PlayMode)
│   └── pull_request_template.md     # Mandatory AI disclosure and evidence template
├── docs/
│   ├── srs.md                       # The authoritative SRS (from srs (1).md)
│   ├── architecture/
│   │   ├── module_view.md           # Component ownership & dependency direction
│   │   ├── runtime_view.md          # Sequence flows: combat, pickup, victory
│   │   └── adr/
│   │       ├── ADR-001-character-controller-locomotion.md
│   │       ├── ADR-002-pure-logic-damage-calculator.md
│   │       └── ADR-003-host-authority-preparation.md
│   ├── debt/
│   │   └── technical_debt_register.md # Debt items, evidence, repayment triggers
│   └── testing/
│       ├── test_strategy.md         # Equivalence classes, boundaries, mutation plan
│       └── traceability_matrix.md   # FR/NFR to AC and TC mappings
├── AGENT_INSTRUCTIONS.md            # Codex Guardrails & Execution Protocols
├── Assets/
│   └── _Project/
│       ├── Scenes/
│       │   ├── Arena_MVP.unity
│       │   └── Tests/
│       │       └── Test_CombatSandBox.unity
│       ├── Scripts/
│       │   ├── Core/                # GameManager, MatchState enum, Event channels
│       │   ├── Input/               # PlayerInputHandler (Input System adapter)
│       │   ├── Player/              # PlayerController (CharacterController motor)
│       │   ├── Camera/              # Cinemachine follow adapters
│       │   ├── Combat/              # CombatController, HitDetection, DamageCalculator
│       │   ├── Health/              # Health component (invariants & clamp logic)
│       │   ├── Pickups/             # WeaponPickup (proximity & E-trigger logic)
│       │   └── UI/                  # UIManager (HUD, prompt, victory overlay)
│       ├── Input/                   # Input Actions configuration asset
│       ├── Prefabs/                 # Player, Bot, WeaponPickup, HUD
│       ├── Settings/                # URP Assets, Physics Layers
│       └── Tests/
│           ├── EditMode/            # Pure C# tests (HP arithmetic, cooldowns, clamp)
│           │   ├── HealthTests.cs
│           │   ├── DamageCalculatorTests.cs
│           │   └── MatchStateTests.cs
│           └── PlayMode/            # Integration tests (movement, trigger, queries)
│               ├── MovementBoundaryTests.cs
│               ├── CombatHitDetectionTests.cs
│               └── PickupInteractionTests.cs
└── ProjectSettings/                 # TagManager (layers), InputSystem, DynamicsManager
```

---

## 4. System Architecture & Module Boundaries (Lecture 04)

### 4.1 Module View & Dependency Rules
Dependencies must point inward toward domain contracts and downward away from UI/Presentation:

```
[ Presentation Layer: UI ] ──> (reads events/state) ──┐
[ Hardware: Input / Camera ] ─> [ InputHandler ]       │
                                       │               │
                                       v               v
                        [ Player / Combat Controllers ]
                                       │
                ┌──────────────────────┴──────────────────────┐
                v                                             v
     [ Domain / Logic: Health, ]                   [ Infrastructure: ]
     [ DamageCalculator, Stats ]                   [ CharacterController, ]
                ^                                  [ Physics.OverlapSphere]
                │                                             ^
     [ Scene: GameManager ] ──────────────────────────────────┘
```

* **Strict Rule 1:** `UIManager` shall NEVER calculate damage, mutate `Health`, or control game state. It strictly consumes C# events.
* **Strict Rule 2:** `PlayerInputHandler` only translates raw key events into semantic intents (`MoveIntent`, `JumpIntent`, `PunchIntent`, `InteractIntent`). It performs no physics calculations.
* **Strict Rule 3:** Domain invariants (e.g., $HP \ge 0$, defeat event triggers exactly once) are protected in `Health.cs` and cannot be bypassed.

### 4.2 Runtime View: Melee Combat & Transaction Isolation
To prevent duplicate hit registration across multi-collider targets or frame updates:
1. `PlayerInputHandler` triggers `CombatController.RequestPunch()`.
2. `CombatController` verifies state (`MatchState == Playing`) and cooldown ($t_{now} - t_{last} \ge 0.50\text{ s}$).
3. A unique `AttackId` is generated.
4. After $0.10\text{ s}$ wind-up, `HitDetection.ExecuteQuery()` calls `Physics.OverlapSphere`.
5. Colliders are mapped to their root `Health` owner and **deduplicated using a `HashSet<Health>`**.
6. `DamageCalculator` calculates final damage based on `PlayerCombatStats`.
7. `Health.ApplyDamage(amount, attackId)` clamps HP and raises `OnHealthChanged` or `OnDefeated`.

---

## 5. Architectural Decision Records (ADRs)

### ADR-001: CharacterController Locomotion over Rigidbody Physics
* **Status:** Approved
* **Context (Lecture 04, L04.14):** Small Arena requires precise, arcade-like movement, deterministic jump curves, and crisp stops without physical inertia or knockback forces.
* **Decision:** Use Unity's built-in `CharacterController` component with manually integrated vertical velocity ($v_{y} \mathrel{+}= g \cdot \Delta t$).
* **Alternatives Considered:** 
  1. *Dynamic Rigidbody + Forces:* Prone to contact jitter, friction sticking, and floaty jumps.
  2. *Kinematic Rigidbody:* Requires manual collision resolution against walls.
* **Consequences:** 
  - *Positive:* Direct displacement control, simple boundary containment, no rotation tipping.
  - *Negative:* Gravity and ground detection must be explicitly implemented and tested.

### ADR-002: Pure C# Domain Invariant Isolation
* **Status:** Approved
* **Context (Lecture 05, L05.08 & Lecture 07, L07.09):** Tying all health and combat calculation logic to `MonoBehaviour` lifecycle makes automated testing slow and reliant on full Unity engine initialization.
* **Decision:** Extract damage calculation, cooldown timers, and health clamp logic into plain C# structs/classes or testable static functions.
* **Alternatives Considered:** Monolithic scripts where `CombatController` directly handles physics, damage math, and UI updates.
* **Consequences:**
  - *Positive:* Fast, reliable EditMode unit tests executing in milliseconds without scene loading.
  - *Negative:* Slight separation overhead between pure C# logic and Unity Inspector properties.

---

## 6. Technical Debt Register (Lecture 05)

| Debt ID | Classification | Item Description & Evidence | Technical Impact & Risk | Repayment Trigger | Owner |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **DEBT-01** | *Deliberate & Prudent* | **D-03 Fixed Facing (+Z):** Player root cannot rotate; punches strictly strike forward along world $+Z$. | Attack usability feels awkward when approaching targets from the north or sides. | Milestone M2 playtest review or when turning mechanics are approved. | Player Lead |
| **DEBT-02** | *Deliberate & Prudent* | **Synchronous Main-Thread Transactions:** Weapon pickup and damage calculations assume local zero-latency execution. | Networking / multiplayer refactoring will require command/RPC rewrites. | Post-MVP phase when introducing Netcode for GameObjects (NGO). | Architecture Lead |
| **DEBT-03** | *Accidental & Reckless Risk* | **`Physics.OverlapSphere` Garbage Allocations:** Using non-allocating variants (`OverlapSphereNonAlloc`) is deferred. | Can create minor GC pauses if invoked at high frequency on low-spec hardware. | Prior to Milestone M7 standalone performance benchmarking. | Combat Lead |

---

## 7. Quality Engineering & Test Specification (Lecture 07)

### 7.1 Equivalence Partitioning & Boundary Value Analysis (BVA)

#### 1. Cooldown Boundary ($0.50\text{ s}$)
* **Partitions:** Inputs arriving $< 0.50\text{ s}$ (Invalid / Rejected); Inputs arriving $\ge 0.50\text{ s}$ (Valid / Accepted).
* **Boundaries:** $t = 0.49\text{ s}$ (Reject, do not buffer); $t = 0.50\text{ s}$ (Accept, start new attack); $t = 0.51\text{ s}$ (Accept).

#### 2. Health Clamping & Overkill
* **Partitions:** $HP > 0$ (Living); $HP = 0$ (Defeated); $HP < 0$ (Illegal State / Must Clamp).
* **Boundaries:** Bot at $1\text{ HP}$ takes $2\text{ HP}$ damage $\implies HP$ must equal $0$ (not $-1$), victory event emitted exactly once. Further hits at $0\text{ HP} \implies$ rejected, no duplicate victory events.

#### 3. Interaction Range ($1.5\text{ m}$)
* **Partitions:** Dist $\le 1.50\text{ m}$ with Line-of-Sight (Valid); Dist $> 1.50\text{ m}$ (Invalid); Dist $\le 1.50\text{ m}$ with wall obstruction (Invalid).
* **Boundaries:** $1.49\text{ m}$ (Success); $1.51\text{ m}$ (Rejected).

### 7.2 Mutation Testing Plan ("Who Tests the Tests?")
To prevent "Test Theater" where AI-generated tests pass trivially:
1. **Mutation 1:** Change `if (currentHP <= 0)` to `if (currentHP < 0)` in `Health.cs`. 
   - *Requirement:* `HealthTests.Overkill_ClampsToZero_AndEmitsDefeatOnce` must fail.
2. **Mutation 2:** Change cooldown check from `>= cooldownDuration` to `> cooldownDuration`.
   - *Requirement:* `CombatTests.Attack_ExactlyAtCooldown_IsAccepted` must fail.
3. **Mutation 3:** Bypass deduplication in `HitDetection.cs` by removing the `HashSet<Health>`.
   - *Requirement:* `CombatHitDetectionTests.MultiColliderTarget_AppliesDamageOnce` must fail.

---

## 8. AI Agent Codex Operational Protocol (Lecture 06)

When prompting or executing tasks via autonomous coding agents (Codex, Claude, Copilot), the agent **must strictly follow the 5-stage loop**:

```
 ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
 │    1. SPEC   │ ──> │   2. PLAN    │ ──> │ 3. GENERATE  │
 └──────────────┘     └──────────────┘     └──────────────┘
                                                  │
                                                  v
 ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
 │ 5. INTEGRATE │ <── │  HUMAN GATE  │ <── │  4. VERIFY   │
 └──────────────┘     └──────────────┘     └──────────────┘
```

### Protocol Checklist for the Agent
1. **Phase 1: Spec**
   - Read the relevant functional requirement (FR) and acceptance criteria (AC) from `srs.md`.
   - Explicitly identify boundary conditions and failure states.
2. **Phase 2: Plan**
   - Declare the exact files to be created or modified.
   - Declare the files that are **frozen** (out-of-bounds).
   - Formulate the test strategy before writing implementation code.
3. **Phase 3: Generate**
   - Generate single-responsibility, highly cohesive components.
   - Adhere strictly to Unity LTS 6000.3 / C# 9 compatibility.
   - Do not invent dependencies or import third-party packages without explicit permission.
4. **Phase 4: Verify**
   - Provide automated EditMode/PlayMode tests covering happy paths, edge cases, and boundaries.
   - Check for accidental scope creep or architecture drift.
5. **Phase 5: Human Gate & Integrate**
   - Present a clean diff summary.
   - Present test execution evidence.
   - Highlight any introduced technical debt in `technical_debt_register.md`.