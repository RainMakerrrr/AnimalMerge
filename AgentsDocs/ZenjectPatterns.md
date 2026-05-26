# Zenject Patterns — AnimalMerge

## Installer Structure

Each major system has its own `MonoInstaller`:

| Installer | Scope |
|---|---|
| `CurrentGameInstaller` | Grid, AnimalFactory, AnimalDatabase, Camera, InputService |
| `BattleInstaller` | Battle services, BattleStateMachine, BattleFlowController |
| `PathfindingInstaller` | Pathfinder, PathNodeFactory |
| `ServicesInstaller` | Shared services |
| `MainSceneInstaller` (Framework) | Framework-level bindings |

---

## Constructor Injection (non-MonoBehaviour)

Preferred pattern for services, state machine states, use cases:

```csharp
public class TurnExecutor : ITurnExecutor
{
    private readonly IUnitTracker _unitTracker;
    private readonly IBattleStateMachine _stateMachine;

    public TurnExecutor(IUnitTracker unitTracker, IBattleStateMachine stateMachine)
    {
        _unitTracker = unitTracker;
        _stateMachine = stateMachine;
    }
}
```

---

## Method Injection (MonoBehaviour)

MonoBehaviours cannot use constructor injection — use `[Inject]` method:

```csharp
public class AnimalFacade : MonoBehaviour
{
    private IAbilitySystem _abilitySystem;

    [Inject]
    private void Construct(IAbilitySystem abilitySystem)
    {
        _abilitySystem = abilitySystem;
    }
}
```

---

## Common Binding Patterns

```csharp
// Interface → implementation, singleton
Container.Bind<ITurnExecutor>().To<TurnExecutor>().AsSingle();

// Bind interface AND concrete type (use when both are needed)
Container.BindInterfacesAndSelfTo<UnitTracker>().AsSingle();

// From ScriptableObject in Resources folder
Container.Bind<AnimalDatabase>()
    .FromScriptableObjectResource("AnimalDatabase")
    .AsSingle();

// From scene instance
Container.Bind<TargetFinder>().FromInstance(_targetFinder).AsSingle();

// MonoBehaviour on a new GameObject (editor-only debug helpers)
Container.Bind<KeyboardStartBattleHelper>()
    .FromNewComponentOnNewGameObject()
    .WithGameObjectName("KeyboardStartBattleHelper (Debug)")
    .AsSingle()
    .NonLazy();
```

---

## SignalBus

Signals decouple systems that shouldn't reference each other directly.

**Declare in installer:**
```csharp
Container.DeclareSignal<StateChangedSignal>();
```

**Fire:**
```csharp
_signalBus.Fire(new StateChangedSignal { State = activeState });
```

**Subscribe (implement `IInitializable` / `IDisposable`):**
```csharp
public void Initialize() => _signalBus.Subscribe<StateChangedSignal>(OnStateChanged);
public void Dispose()   => _signalBus.Unsubscribe<StateChangedSignal>(OnStateChanged);
```

Current signals in project:
- `StateChangedSignal` — fired by `GameStateMachine` on every state transition

---

## `IInitializable` / `IDisposable`

Non-MonoBehaviour classes that need lifecycle hooks:

```csharp
public class BattleObserver : IInitializable, IDisposable
{
    public void Initialize() { /* called after all bindings resolved */ }
    public void Dispose()    { /* called on container disposal */ }
}

// Installer:
Container.BindInterfacesAndSelfTo<BattleObserver>().AsSingle();
```
