# AnimalMerge

Turn-based mobile battler with merge mechanics. Platforms: Android & iOS.

## Tech Stack
- Unity 2022.3.62f3 · C#
- Zenject — dependency injection
- UniTask — async/await (replaces Coroutines)
- NiceVibrations — haptic feedback
- NaughtyAttributes — inspector attributes
- DOTween — animations
- TextMeshPro — UI text rendering

## Repository Structure
```
Assets/
  3rdParty/           — third-party assets and 3D models
  Animals*/           — animal models (pending restructure, do not reorganize)
  Code/
    Abilities/        — ability system: Dodge, CounterAttack, Retreat
    Animals/          — facades, health, merge, movement, upgrade
    Battle/           — battle logic, services, state machine, config
    Data/             — ScriptableObject configs (AnimalDatabase, etc.)
    Framework/        — internal bootstrap module: GameBootstrapper, GameStateMachine, Level
    GridPathfinding/  — active pathfinding implementation (A*, multi-size units)
    Infrastructure/   — app-level factories, installers, services, states
    Services/         — shared utilities: physics, random
    Tests/            — EditorTests/ · PlayModeTests/
    NewPathfinding/ · Pathfinding/  — IGNORE, deprecated, to be deleted
  Plugins/            — third-party plugins
  Prefabs/            — all prefabs
  Resources/          — prefabs/assets for Resources.Load() API only
```

## Architecture
- Clean Architecture: Presentation → Application → Domain → Infrastructure
- OOP · GoF patterns · SOLID · KISS — apply pragmatically, not dogmatically
- Zenject for all DI — no Singletons
- Interfaces for all cross-layer dependencies
- Full details: [ProjectArchitecture.md](../AgentsDocs/ProjectArchitecture.md)

## Code Style
- Microsoft C# Coding Conventions
- PascalCase — public members/classes, `_camelCase` — private fields, `IName` — interfaces
- `var` for locals when type is clear from right-hand side
- One class per file · explicit access modifiers always
- Full details: [CodeStyle.md](../AgentsDocs/CodeStyle.md)

## Testing
- NUnit · NSubstitute (mocks) · FluentAssertions
- EditorTests → `Assets/Code/Tests/EditorTests/`
- PlayModeTests → `Assets/Code/Tests/PlayModeTests/`
- Details: [Testing.md](../AgentsDocs/Testing.md)

## After Every Change
- Verify all namespaces are imported
- Check Unity Console — zero compile errors before finishing
- Run tests relevant to the changed system

## Do Not Do
- No Singletons — use Zenject bindings instead
- No `GetComponent` / `FindObjectOfType` in Update or FixedUpdate
- No Coroutines for new async code — use UniTask
- No new code in `NewPathfinding/` or `Pathfinding/` (deprecated)
- Do not place assets in `Resources/` unless loaded via `Resources.Load()`

## References
- [Architecture](../AgentsDocs/ProjectArchitecture.md)
- [Code Style](../AgentsDocs/CodeStyle.md)
- [Testing](../AgentsDocs/Testing.md)
- [Code Review Checklist](../AgentsDocs/CodeReview.md)
- [Zenject & Signals](../AgentsDocs/ZenjectPatterns.md)
- [Unity Patterns](../AgentsDocs/UnityPatterns.md)
- [Specifications](../AgentsDocs/Specifications/README.md)
- [Battle State System](../AgentsDocs/Battle_State_System_Setup_Guide.md)
