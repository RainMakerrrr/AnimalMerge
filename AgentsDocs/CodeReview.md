# Code Review Checklist — AnimalMerge

Use this checklist before marking a task as complete or creating a PR.

---

## Correctness
- [ ] Logic matches the specification (check `AgentsDocs/Specifications/` for relevant system)
- [ ] Edge cases handled (null checks at boundaries, empty collections, cancelled tokens)
- [ ] No unintended side effects on other systems

## Architecture
- [ ] Change stays within the correct Clean Architecture layer
- [ ] New types added to the correct module under `Assets/Code/`
- [ ] No circular dependencies between modules
- [ ] Interfaces used for all cross-layer dependencies

## Dependency Injection
- [ ] All dependencies injected via Zenject — no `new`, no static singletons
- [ ] New bindings added to the appropriate Installer (`BattleInstaller`, `CurrentGameInstaller`, etc.)
- [ ] MonoBehaviours use `[Inject]` method injection, not constructor

## Unity
- [ ] No `GetComponent` / `FindObjectOfType` outside of `Awake`
- [ ] No `Camera.main`, `GameObject.Find`, or `transform.Find` in Update loops
- [ ] Component references cached in `Awake`
- [ ] New frequently-spawned objects use pooling

## Async
- [ ] Async code uses `UniTask`, not `Task` or Coroutines
- [ ] `CancellationToken` passed through and respected
- [ ] `OperationCanceledException` handled gracefully

## Code Style
- [ ] Follows Microsoft C# Coding Conventions (see `AgentsDocs/CodeStyle.md`)
- [ ] `var` used for locals where type is obvious
- [ ] Explicit access modifiers on all members
- [ ] Async method names end with `Async`
- [ ] No comments added — intent carried by naming and structure (`//`, `/* */`, `///` alike)

## Build & Tests
- [ ] All namespaces imported — no unresolved references
- [ ] Zero compile errors in Unity Console
- [ ] Zero warnings added (fix or document why unavoidable)
- [ ] Relevant tests pass
- ~~New behavior covered by tests (EditorTests preferred)~~ — **TESTS PAUSED**, not required for now

## Do Not Merge If
- Singleton pattern introduced
- `Pathfinding/` or `NewPathfinding/` folders modified
- Assets added to `Resources/` without a `Resources.Load()` call to justify it
- Coroutines used for new async code
