# Battle State System - Setup Guide

## Overview

The Battle State System replaces AutoFight.cs with a formal state machine that manages turn-based combat across multiple stages per level. This guide explains how to set up and use the system.

## Architecture Summary

```
GameStateMachine (Framework)
    ↓ enters GameLoopState
BattleInitializer detects ExtendedLevel
    ↓ starts
BattleFlowController
    ↓ manages
BattleStateMachine
    ├── PreBattleState (auto-transition placeholder)
    ├── BattleStartState (spawn enemies for current stage)
    ├── PlayerTurnState (all player units move/attack)
    ├── EnemyTurnState (all enemy units move/attack)
    ├── CheckVictoryState (check win/lose/continue)
    ├── StageClearState (restore HP, advance to next stage)
    └── BattleEndState (transition to WinState or LoseState)
```

## Key Components

### 1. ExtendedLevel (Code.Infrastructure)
Extends Framework's Level class to add multi-stage battle support.
- **Location**: `Assets/Code/Infrastructure/ExtendedLevel.cs`
- **Purpose**: Stores array of LevelStageConfig for multi-stage battles
- **Usage**: Replace Level component with ExtendedLevel on level prefabs

### 2. BattleLoopState (Code.Infrastructure.States)
IState implementation that starts the battle flow.
- **Location**: `Assets/Code/Infrastructure/States/BattleLoopState.cs`
- **Purpose**: Bridges Framework's GameStateMachine with Battle system
- **Triggered by**: BattleInitializer when ExtendedLevel is detected

### 3. BattleInitializer (Code.Battle)
Monitors GameStateMachine and starts battles for ExtendedLevel.
- **Location**: `Assets/Code/Battle/BattleInitializer.cs`
- **Purpose**: Automatically starts battle flow when entering GameLoopState with ExtendedLevel
- **Binding**: Automatically bound in BattleInstaller

### 4. BattleFlowController (Code.Battle)
Orchestrates battle flow across multiple stages.
- **Location**: `Assets/Code/Battle/BattleFlowController.cs`
- **Responsibilities**:
  - Track current stage index
  - Provide stage configuration to states
  - Manage stage progression
  - Register player units from AnimalSpawner

### 5. BattleStateMachine (Code.Battle.StateMachine)
State machine managing battle states.
- **Location**: `Assets/Code/Battle/StateMachine/BattleStateMachine.cs`
- **States**: 7 battle states (see architecture summary)
- **Initialized**: In BattleInstaller's InitializeBattleStates

## Installation Steps

### Step 1: Add BattleInstaller to Scene

1. Open your main game scene (`Assets/Scenes/Develop.unity` or `Assets/Scenes/Main Scene.unity`)
2. Find the GameObject with SceneContext component (or create one)
3. Add BattleInstaller MonoBehaviour to this GameObject (or create a child GameObject)
4. In BattleInstaller inspector:
   - **Target Finder**: Drag the TargetFinder component from the scene
   - **Animal Spawner**: Drag the AnimalSpawner component from the scene

**Example Scene Hierarchy:**
```
Scene
├── SceneContext
│   ├── MainSceneInstaller
│   ├── CurrentGameInstaller
│   └── BattleInstaller  ← Add this
├── TargetFinder  ← Reference in BattleInstaller
├── AnimalSpawner  ← Reference in BattleInstaller
└── ...
```

### Step 2: Create LevelStageConfig ScriptableObjects

For each stage in your levels, create a LevelStageConfig:

1. Right-click in Project window
2. Select `Create > Game > Level Stage Config`
3. Configure the stage:
   - **Stage Number**: Index of this stage (0, 1, 2)
   - **Is Boss Stage**: Check if this stage has a boss
   - **Enemies**: Array of enemy configurations
     - **Prefab**: Enemy AnimalFacade prefab
     - **Grid Position**: Vector2Int position on grid
     - **Is Boss**: Check if this specific enemy is the boss

**Example Stage Configuration:**
```
LevelStageConfig: "Level_1_Stage_1"
- Stage Number: 0
- Is Boss Stage: false
- Enemies:
  [0]
    - Prefab: ChickenEnemy
    - Grid Position: (3, 3)
    - Is Boss: false
  [1]
    - Prefab: ChickenEnemy
    - Grid Position: (4, 3)
    - Is Boss: false
```

**Example Boss Stage:**
```
LevelStageConfig: "Level_1_Stage_3"
- Stage Number: 2
- Is Boss Stage: true
- Enemies:
  [0]
    - Prefab: ElephantBoss
    - Grid Position: (3, 4)
    - Is Boss: true ← IMPORTANT: Must be set for victory detection
```

### Step 3: Convert Level Prefabs to ExtendedLevel

For each level prefab:

1. Open the level prefab (e.g., `Assets/Prefabs/Levels/Level_1.prefab`)
2. Select the root GameObject
3. **Remove** the `Level` component (Framework version)
4. **Add** the `ExtendedLevel` component (CodeBase version)
5. Fill in ExtendedLevel properties:
   - **Id**: Level identifier (same as before)
   - **Stages**: Array of LevelStageConfig ScriptableObjects
     - Add 1-3 stage configs per level

**Example Level Setup:**
```
Level_1 (Prefab)
├── ExtendedLevel component
│   ├── Id: "level_1"
│   └── Stages (Size: 3)
│       ├── [0]: Level_1_Stage_1
│       ├── [1]: Level_1_Stage_2
│       └── [2]: Level_1_Stage_3 (Boss)
```

### Step 4: Verify Dependencies

Ensure these components exist in your scenes:
- ✅ **TargetFinder**: Used by TurnExecutor to find attack targets
- ✅ **AnimalSpawner**: Tracks player units, referenced in BattleInstaller
- ✅ **Grid**: Used for unit placement and pathfinding

## Battle Flow Explained

### 1. Level Load & Initialization
```
LoadLevelState loads ExtendedLevel prefab
    ↓
User taps to start
    ↓
GameStateMachine.Enter<GameLoopState>()
    ↓
BattleInitializer detects ExtendedLevel
    ↓
BattleFlowController.StartBattle(extendedLevel)
    ↓
Register player units from AnimalSpawner
    ↓
BattleStateMachine.ChangeState<BattleStartState>()
```

### 2. Battle Start (Per Stage)
```
BattleStartState.Enter()
    ↓
Get current stage config from BattleFlowController
    ↓
EnemySpawnService spawns enemies from config
    ↓
Set enemy.IsBoss for boss units
    ↓
Register enemies with UnitTracker
    ↓
Transition to PlayerTurnState
```

### 3. Turn Execution Loop
```
PlayerTurnState
    ↓ TurnExecutor.ExecutePlayerTurnsAsync()
    ↓ Sort units left-to-right, top-to-bottom
    ↓ Execute each unit's turn (move + attack)
    ↓
EnemyTurnState
    ↓ TurnExecutor.ExecuteEnemyTurnsAsync()
    ↓ Sort units bottom-to-top
    ↓ Execute each unit's turn
    ↓
CheckVictoryState
    ↓ VictoryConditionChecker.CheckBattleConditions()
    ↓
    ├── All enemies dead + no boss alive → BattleEndState (Victory)
    ├── All player units dead → BattleEndState (Defeat)
    └── Battle ongoing → PlayerTurnState (next round)
```

### 4. Stage Clear
```
CheckVictoryState detects all enemies dead
    ↓
StageClearState.Enter()
    ↓
HealthRestorationService restores player units to full HP
    ↓
EnemySpawnService.ClearEnemies()
    ↓
BattleFlowController.AdvanceToNextStage()
    ↓
Check if more stages exist
    ├── YES → BattleStartState (next stage)
    └── NO → BattleEndState (Victory)
```

### 5. Battle End
```
BattleEndState.Enter()
    ↓
Check battle result
    ├── Victory → GameStateMachine.Enter<WinState>()
    │             (Progress saved, next level loaded)
    └── Defeat → GameStateMachine.Enter<LoseState>()
                  (Same level reloaded)
```

## Turn Order Details

### Player Units
Sorted by grid position:
1. **Primary sort**: X position (left-to-right)
2. **Secondary sort**: Y position (top-to-bottom, descending)

**Example:**
```
Grid:
  (0,2)[Unit A]  (1,2)[Unit B]
  (0,1)[Unit C]  (1,1)[Unit D]

Turn order: A → B → C → D
```

### Enemy Units
Sorted by grid position:
1. **Primary sort**: X position (left-to-right)
2. **Secondary sort**: Y position (bottom-to-top, ascending)

## Victory & Defeat Conditions

### Victory
- **Primary**: All enemies dead
- **Boss Check**: `!UnitTracker.HasAliveBoss`
  - ⚠️ **CRITICAL**: Boss units MUST have `IsBoss = true` set in StageEnemyConfig
  - Boss detection checks `AnimalFacade.IsBoss` property
- **Multi-stage**: Victory declared only after final stage complete

### Defeat
- **Condition**: All player units dead
- **Result**: GameStateMachine.Enter<LoseState>()
- **Behavior**: Entire level restarts (not just current stage)

## Services Reference

### UnitTracker
Tracks alive/dead units via event subscriptions.
- **Methods**:
  - `RegisterPlayerUnit(AnimalFacade unit)`
  - `RegisterEnemyUnit(AnimalFacade unit)`
  - `GetAlivePlayerUnits()` → sorted by grid position
  - `GetAliveEnemyUnits()` → sorted by grid position
- **Properties**:
  - `AlivePlayerUnitsCount`
  - `AliveEnemyUnitsCount`
  - `HasAliveBoss` ← used for victory detection

### EnemySpawnService
Spawns enemies from stage configuration.
- **Methods**:
  - `SpawnEnemiesForStageAsync(LevelStageConfig, CancellationToken)`
  - `ClearEnemies()`
- **Behavior**:
  - Instantiates via DiContainer
  - Places units on grid at configured positions
  - Sets `gameObject.layer = "Enemy"`
  - Sets `IsBoss` property for boss units

### TurnExecutor
Executes unit turns in order.
- **Methods**:
  - `ExecutePlayerTurnsAsync(CancellationToken)`
  - `ExecuteEnemyTurnsAsync(CancellationToken)`
- **Logic** (per unit):
  1. Find target via TargetFinder
  2. Check if unit is close to target (attack range)
  3. If close: attack only
  4. If far: move towards target, then attack if in range
  5. Await completion, check cancellation token

### VictoryConditionChecker
Checks win/lose conditions.
- **Method**: `CheckBattleConditions()` → `BattleResult` enum
- **Returns**:
  - `BattleResult.Victory`: All enemies dead AND no alive boss
  - `BattleResult.Defeat`: All player units dead
  - `BattleResult.Ongoing`: Battle continues

### HealthRestorationService
Restores HP between stages.
- **Method**: `RestoreHealthForSurvivingUnits(IEnumerable<AnimalFacade>)`
- **Behavior**: Calls `unit.Health.Restore(unit.Health.Max)` for alive units

## Testing Checklist

### Single-Stage Level Test
- [ ] Create LevelStageConfig with 2-3 enemies
- [ ] Create ExtendedLevel with 1 stage
- [ ] Load level, verify enemies spawn
- [ ] Defeat all enemies, verify WinState transition
- [ ] Check progress saves, next level loads

### Multi-Stage Level Test
- [ ] Create 3 LevelStageConfig (stages 0, 1, 2)
- [ ] Last stage has boss (IsBoss = true)
- [ ] Create ExtendedLevel with 3 stages
- [ ] Complete stage 1, verify HP restoration
- [ ] Verify stage 2 enemies spawn
- [ ] Complete all stages, verify WinState

### Defeat Test
- [ ] Start battle with weak player units
- [ ] Let all player units die
- [ ] Verify LoseState transition
- [ ] Verify level restarts (not next level)

### Boss Detection Test
- [ ] Create stage with boss enemy
- [ ] Set `IsBoss = true` in StageEnemyConfig
- [ ] Kill all non-boss enemies first
- [ ] Verify battle continues (HasAliveBoss = true)
- [ ] Kill boss
- [ ] Verify victory (HasAliveBoss = false)

## Troubleshooting

### Battle doesn't start
- ✅ Check BattleInstaller is added to scene
- ✅ Verify TargetFinder and AnimalSpawner references set
- ✅ Confirm level prefab uses ExtendedLevel (not Level)
- ✅ Check Unity console for errors

### Enemies don't spawn
- ✅ Verify LevelStageConfig has enemies configured
- ✅ Check enemy prefabs are valid AnimalFacade
- ✅ Confirm grid positions are valid
- ✅ Check enemy layer is set correctly

### Victory not detected after killing all enemies
- ✅ **Check boss IsBoss property**: Most common issue!
  - Open StageEnemyConfig in inspector
  - Verify boss enemy has `Is Boss` checked
- ✅ Check VictoryConditionChecker logic in CheckVictoryState
- ✅ Verify UnitTracker.HasAliveBoss returns false

### HP not restoring between stages
- ✅ Check HealthRestorationService is bound in BattleInstaller
- ✅ Verify StageClearState is called
- ✅ Check AnimalHealth.Restore() method exists

### Compilation errors
- ✅ Ensure no circular assembly dependencies
- ✅ Verify all "using Code.Battle;" directives present
- ✅ Check Zenject bindings in BattleInstaller

## Integration with Existing Systems

### AnimalSpawner Integration
- BattleFlowController registers player units from `AnimalSpawner.Animals`
- Units are tracked in UnitTracker for victory/defeat detection
- AnimalSpawner remains responsible for player unit spawning

### TargetFinder Integration
- TurnExecutor uses TargetFinder to locate attack targets
- Layer masks: "Animal" for player units, "Enemy" for enemy units
- Reuses existing target detection logic from AutoFight

### Grid Integration
- Enemies spawned at grid positions from LevelStageConfig
- Turn order based on grid positions
- Pathfinding uses existing IPathfindingService

### State Machine Integration
- BattleStateMachine runs independently of GameStateMachine
- GameStateMachine stays in GameLoopState during battle
- BattleEndState transitions GameStateMachine to WinState/LoseState

## Future Enhancements (Not Implemented)

- PreBattleState merge UI (currently auto-transitions)
- Turn queue based on unit speed stats
- AI behavior customization per enemy type
- Ability activation UI highlights
- Multiple victory conditions (survive X turns, etc.)

## File Reference

### Core Files
- `Assets/Code/Infrastructure/ExtendedLevel.cs`
- `Assets/Code/Infrastructure/States/BattleLoopState.cs`
- `Assets/Code/Battle/BattleInitializer.cs`
- `Assets/Code/Battle/BattleFlowController.cs`

### State Machine
- `Assets/Code/Battle/StateMachine/BattleStateMachine.cs`
- `Assets/Code/Battle/StateMachine/IBattleState.cs`

### Battle States (7 total)
- `Assets/Code/Battle/States/PreBattleState.cs`
- `Assets/Code/Battle/States/BattleStartState.cs`
- `Assets/Code/Battle/States/PlayerTurnState.cs`
- `Assets/Code/Battle/States/EnemyTurnState.cs`
- `Assets/Code/Battle/States/CheckVictoryState.cs`
- `Assets/Code/Battle/States/StageClearState.cs`
- `Assets/Code/Battle/States/BattleEndState.cs`

### Services (5 total)
- `Assets/Code/Battle/Services/UnitTracker.cs`
- `Assets/Code/Battle/Services/TurnExecutor.cs`
- `Assets/Code/Battle/Services/EnemySpawnService.cs`
- `Assets/Code/Battle/Services/VictoryConditionChecker.cs`
- `Assets/Code/Battle/Services/HealthRestorationService.cs`

### Configuration
- `Assets/Code/Battle/Config/LevelStageConfig.cs`
- `Assets/Code/Battle/BattleResult.cs`

### Installer
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`

## Summary

The Battle State System provides a clean, extensible architecture for turn-based combat with multi-stage level support. Key benefits:

- ✅ Formal state machine (no more keyboard-driven AutoFight)
- ✅ Multi-stage levels with HP restoration
- ✅ Proper victory/defeat detection
- ✅ Clean separation from Framework (no circular dependencies)
- ✅ Zenject DI for all dependencies
- ✅ Async/await for all operations
- ✅ SOLID principles throughout

The system is ready for content creation (LevelStageConfig) and in-game testing.
