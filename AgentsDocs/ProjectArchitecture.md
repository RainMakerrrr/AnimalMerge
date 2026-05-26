# Project Architecture — AnimalMerge

## Clean Architecture Layers

```
Presentation   — MonoBehaviours, UI views, Facades, Animators
Application    — Use cases, services, state machines, turn logic
Domain         — Entities, interfaces, value objects, rules
Infrastructure — Factories, installers, data access, external services
```

Each layer depends only on the layer below it. Interfaces defined in Domain, implemented in Infrastructure.

---

## Assets/Code Modules

### `Abilities/`
Ability system: Dodge, CounterAttack, Retreat. Each ability is a standalone class implementing a common interface, injected into animals via factory.

### `Animals/`
Core animal module split into subdomains:
- `Facades/` — `AnimalFacade`: single entry point for all animal interactions (encapsulates health, movement, abilities)
- `Health/` — health components including special implementations (`FoxHealth`, `HedgehogHealth`)
- `Merge/` — merge commands, merge undo service, merge attributes and skills
- `Movement/` — pathfinding-driven movement logic
- `Upgrade/` — upgrade mechanics

### `Battle/`
Turn-based battle system:
- `StateMachine/` — `BattleStateMachine` with `IBattleState`
- `States/` — `PreBattleState → BattleStartState → PlayerTurnState → EnemyTurnState → CheckVictoryState → StageClearState → BattleEndState`
- `Services/` — `TurnExecutor`, `VictoryConditionChecker`, `EnemySpawnService`, `HealthRestorationService`, `UnitRepositioningService`
- `Config/` — battle timing and balance ScriptableObjects
- See `AgentsDocs/Battle_State_System_Setup_Guide.md` for full setup

### `Data/`
ScriptableObject configs. `AnimalDatabase` is the central registry loaded via `Resources.Load("AnimalDatabase")`. Contains animal stats and merge rules.

### `Framework/`
Internal bootstrap module — treat as a stable base, avoid modifying core classes.
- `GameBootstrapper` (MonoBehaviour) — entry point, sets target framerate, enters `BootstrapState`
- `GameStateMachine` — top-level FSM with states: `BootstrapState → LoadProgressState → LoadLevelState → BattleLoopState → WinState / LoseState`
- `Level` / `ExtendedLevel` — level data carrier; `ExtendedLevel` adds multi-stage battle support
- `WindowPool` — UI window pool
- Signal: `StateChangedSignal` fired on every state transition

### `GridPathfinding/`
Active pathfinding implementation. A* algorithm with support for multi-size units (1×1, 1×2, 2×1, 2×2). `Direction` applies only to rectangular units. Key classes: `IPathfinder`, `Pathfinder`, `Grid`, `GridCell`.
See `AgentsDocs/Specifications/01_Pathfinding_System_Specification.md` for full spec.

### `Infrastructure/`
App-level wiring:
- `Installers/` — `CurrentGameInstaller`, `BattleInstaller`, `PathfindingInstaller`, `ServicesInstaller`
- `Factories/` — `AnimalFactory`, `AbilityFactory`, `PathNodeFactory`
- `Services/` — `InputService`
- `States/` — app-level states used by `GameStateMachine`

### `Services/`
Shared cross-cutting utilities:
- `Physics/` — physics helpers (overlap queries)
- `Random/` — randomization service

### `Tests/`
- `EditorTests/` — unit and integration tests (no Unity runtime needed)
- `PlayModeTests/` — tests requiring Unity runtime

---

## Key Design Patterns

| Pattern | Where used |
|---|---|
| State | `GameStateMachine`, `BattleStateMachine` |
| Command | Merge undo system (`IMergeCommand`, `MergeUndoService`) |
| Factory | `AnimalFactory`, `AbilityFactory`, `PathNodeFactory` |
| Observer | Zenject `SignalBus` (e.g. `StateChangedSignal`) |
| Facade | `AnimalFacade` — single interface for all animal interactions |
| Object Pool | `WindowPool` (UI), VFX, projectiles |

---

## Data Flow: Battle Turn

```
PlayerInput → StartBattleService → BattleFlowController
  → BattleStateMachine.Enter<PlayerTurnState>
    → TurnExecutor.ExecuteTurnAsync()
      → AnimalFacade.MoveAsync() → GridPathfinding
      → AnimalFacade.AttackAsync() → AbilitySystem
    → BattleStateMachine.Enter<CheckVictoryState>
      → VictoryConditionChecker → Win / NextStage / EnemyTurn
```
