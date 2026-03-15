# Turn-Based Battler with Merge Mechanics - Claude Code Rules

## Project Overview
- **Genre**: Turn-based battler with merge mechanics
- **Unity Version**: [Specify your version]
- **Target Platform**: [PC/Mobile/Console]
- **Architecture**: Clean Architecture with SOLID principles
- **Dependency Injection**: Zenject
- **Async**: Unity Tasks (prefer over Coroutines)

## Documentation

### Technical Specifications
All technical specifications are located in `Documentation/Specifications/`:

- **[01_Pathfinding_System_Specification.md](../Documentation/Specifications/01_Pathfinding_System_Specification.md)** - Система поиска пути
  - Базируется на GridPathfinding
  - Поддержка разных размеров юнитов (1×1, 1×2, 2×2)
  - A* алгоритм с учетом габаритов
  - Direction только для прямоугольных юнитов (1×2, 2×1)
  - Алгоритм GetPossibleMoves для поиска позиций атаки

- **[02_Pathfinding_Test_Cases.md](../Documentation/Specifications/02_Pathfinding_Test_Cases.md)** - Тест-кейсы для системы поиска пути
  - 34 комплексных тест-кейса
  - Покрытие GetAnchorPointsForCell, GetAllTargetCells, GetPossibleMoves
  - Unit, Integration и Performance тесты
  - Регрессионные тесты для проверки исправлений

- **[03_Merge_And_Skills_System_Specification.md](../Documentation/Specifications/03_Merge_And_Skills_System_Specification.md)** - Система мерджа и навыков
  - Механики объединения животных
  - Система наследования способностей
  - Upgrade система

- **[04_Attack_And_Damage_System_Specification.md](../Documentation/Specifications/04_Attack_And_Damage_System_Specification.md)** - Система атаки и получения урона
  - Physics-based detection целей (OverlapCapsule/OverlapSphere)
  - Применение урона с дедупликацией (HashSet fix)
  - Интеграция с Ability System (Dodge, CounterAttack)
  - Специальные реализации (FoxHealth, HedgehogHealth)
  - Исправление бага множественного урона (2026-02-04)

- **[05_Battle_System_Timings_Specification.md](../Documentation/Specifications/05_Battle_System_Timings_Specification.md)** - Тайминги боевой системы
  - Граф состояний BattleStateMachine (PreBattle → PlayerTurn → EnemyTurn → CheckVictory → etc.)
  - Длительность движения юнитов (формула: pathLength / 2.0)
  - Тайминги атак (обычная атака через _attackClip.length, Chicken Jump = 1.0s)
  - Порядок и длительность выполнения способностей (Dodge, CounterAttack)
  - Все искусственные задержки (Task.Delay, WaitForSeconds)
  - Примеры расчётов полной длительности хода
  - Настраиваемые параметры для баланса

**ВАЖНО:** При работе с любой системой, для которой есть спецификация, ВСЕГДА сначала читай спецификацию, чтобы понять:
- Текущую архитектуру
- Принятые решения
- Геймплейные требования из концепт-дока
- План реализации

### Game Design Document
- **[Концепт-док 2.0](../Концепт-док 2.0-2026012418375932.pdf)** - Основной геймдизайн документ
  - Механики мерджа животных
  - Правила боя и передвижения
  - Баланс животных и боссов

## Code Style
- Follow **Microsoft C# Coding Conventions** strictly
- https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- Use PascalCase for public members, methods, classes
- Use camelCase for private fields with underscore prefix: `_privateField`
- Use PascalCase for properties
- Always use explicit access modifiers (public, private, protected)
- One class per file, file name matches class name
- **Use `var` for local variables**: Always use implicit typing (`var`) for local variable declarations when the type is obvious from the right side of the assignment
  ```csharp
  // ✅ CORRECT - Use var
  var path = FindPath(points);
  var neighbours = _gridManager.GetNeighborCells(position, size, direction);
  var tween = transform.DOPath(pathPositions, duration);

  // ❌ AVOID - Don't use explicit types for locals
  List<GridCell> path = FindPath(points);
  List<GridCell> neighbours = _gridManager.GetNeighborCells(position, size, direction);
  Tween tween = transform.DOPath(pathPositions, duration);

  // Note: Field declarations, parameters, and return types still use explicit types
  private IGridManager _gridManager; // Field - explicit type
  public List<GridCell> GetCells() { } // Return type - explicit
  public void Process(List<GridCell> cells) { } // Parameter - explicit
  ```

## Architecture Principles

### Clean Architecture Layers
```
Presentation Layer (MonoBehaviours, UI)
    ↓
Application Layer (Use Cases, Game Logic)
    ↓
Domain Layer (Entities, Value Objects, Interfaces)
    ↓
Infrastructure Layer (Data Access, Services)
```

### SOLID Principles - Always Apply
- **S**ingle Responsibility: One class = one reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Derived classes must be substitutable
- **I**nterface Segregation: Many specific interfaces > one general
- **D**ependency Inversion: Depend on abstractions, not concretions

### Design Patterns in Use
- **Dependency Injection**: Zenject for all dependencies
- **Command Pattern**: Optional, use when needed for undo/redo or complex action queuing
- **State Pattern**: For battle states, unit states
- **Factory Pattern**: For unit/enemy creation
- **Observer Pattern**: For events and reactions
- **Object Pool Pattern**: For VFX, UI elements, projectiles

## Dependency Injection with Zenject

### Installation Structure
```csharp
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Services as singletons
        Container.BindInterfacesAndSelfTo<BattleService>().AsSingle();
        
        // Factories
        Container.BindFactory<UnitData, Unit, Unit.Factory>();
        
        // From instance
        Container.Bind<GameSettings>().FromScriptableObject();
    }
}
```

### Constructor Injection (Preferred for non-MonoBehaviours)
```csharp
public class BattleController : IInitializable, IDisposable
{
    private readonly IBattleService _battleService;
    private readonly IInputService _inputService;
    private readonly SignalBus _signalBus;

    public BattleController(
        IBattleService battleService,
        IInputService inputService,
        SignalBus signalBus)
    {
        _battleService = battleService;
        _inputService = inputService;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        // Initialization logic
    }

    public void Dispose()
    {
        // Cleanup
    }
}
```

### Method Injection (For MonoBehaviours)
```csharp
public class UnitView : MonoBehaviour
{
    private IUnitFactory _unitFactory;
    private SignalBus _signalBus;

    [Inject]
    private void Construct(IUnitFactory unitFactory, SignalBus signalBus)
    {
        _unitFactory = unitFactory;
        _signalBus = signalBus;
    }
}
```

## Async Operations - Unity Tasks

### Prefer Tasks over Coroutines
```csharp
// ✅ CORRECT - Use async/await
public async Task ExecuteTurnAsync(CancellationToken cancellationToken)
{
    await MoveUnitAsync(cancellationToken);
    await PlayAttackAnimationAsync(cancellationToken);
    await DealDamageAsync(cancellationToken);
}

// ❌ AVOID - Don't use Coroutines for new code
// IEnumerator ExecuteTurn() { }
```

### Cancellation Token Pattern
```csharp
private CancellationTokenSource _cancellationTokenSource;

private void OnEnable()
{
    _cancellationTokenSource = new CancellationTokenSource();
}

private void OnDisable()
{
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
}

private async Task DoSomethingAsync()
{
    await SomeOperationAsync(_cancellationTokenSource.Token);
}
```

## Unity Best Practices

### Component Caching - MANDATORY
```csharp
// ✅ CORRECT - Cache in Awake
private Rigidbody2D _rigidbody;
private Animator _animator;
private SpriteRenderer _spriteRenderer;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
    _spriteRenderer = GetComponent<SpriteRenderer>();
}

// ❌ NEVER do this
private void Update()
{
    GetComponent<Animator>().Play("Attack"); // WRONG!
}
```

### Update Loop Optimization
```csharp
// ✅ Use events instead of Update checks
public class UnitHealth : MonoBehaviour
{
    public event Action<float> OnHealthChanged;
    
    private float _currentHealth;
    
    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        OnHealthChanged?.Invoke(_currentHealth);
    }
}

// ❌ AVOID polling in Update
private void Update()
{
    if (_health != _previousHealth) // Don't do this!
    {
        UpdateHealthUI();
    }
}
```

### Expensive Operations - FORBIDDEN in Update
```csharp
// ❌ NEVER in Update, FixedUpdate, or LateUpdate:
// - FindObjectOfType<T>()
// - GameObject.Find()
// - GetComponent<T>() (cache it!)
// - Camera.main (cache it!)
// - transform.Find() (cache references!)
// - Resources.Load()
// - Instantiate() without pooling

// ✅ CORRECT - Cache and use events
private Camera _mainCamera;
private Transform _targetTransform;

private void Awake()
{
    _mainCamera = Camera.main;
    _targetTransform = GameObject.FindGameObjectWithTag("Player").transform;
}
```

### Physics Best Practices
```csharp
// ✅ Physics operations in FixedUpdate
private void FixedUpdate()
{
    _rigidbody.MovePosition(newPosition);
    _rigidbody.AddForce(force);
}

// ✅ Use LayerMask for raycasts
[SerializeField] private LayerMask _enemyLayer;

private void CheckHit()
{
    if (Physics2D.Raycast(origin, direction, distance, _enemyLayer))
    {
        // Hit detected
    }
}
```

### UI Optimization
```csharp
// ✅ Use TextMeshPro, not legacy Text
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private Image _healthBar;
    
    // ✅ Batch UI updates
    public void UpdateUI(UnitData data)
    {
        _healthText.text = data.Health.ToString();
        _healthBar.fillAmount = data.Health / data.MaxHealth;
    }
}

// ✅ Use Canvas Groups for show/hide
private CanvasGroup _canvasGroup;

public void Show()
{
    _canvasGroup.alpha = 1f;
    _canvasGroup.interactable = true;
    _canvasGroup.blocksRaycasts = true;
}
```

### Object Pooling - REQUIRED
```csharp
// ✅ Use object pooling for:
// - VFX effects
// - Projectiles
// - UI popups
// - Damage numbers
// - Merged units

public class EffectPool : MonoBehaviour
{
    [Inject] private readonly DiContainer _container;
    
    private Queue<Effect> _pool = new Queue<Effect>();
    
    public Effect GetEffect()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }
        
        return _container.InstantiatePrefabForComponent<Effect>(_effectPrefab);
    }
    
    public void ReturnEffect(Effect effect)
    {
        effect.gameObject.SetActive(false);
        _pool.Enqueue(effect);
    }
}
```

## Game-Specific Architecture

### Battle System
```csharp
// Domain Layer
public interface IBattleService
{
    Task StartBattleAsync(BattleData battleData, CancellationToken token);
    Task ExecuteTurnAsync(ITurnAction action, CancellationToken token);
    bool CanExecuteAction(ITurnAction action);
}

// Application Layer
public class BattleService : IBattleService
{
    private readonly IUnitRepository _unitRepository;
    private readonly ITurnQueue _turnQueue;
    private readonly SignalBus _signalBus;
    
    // Implementation
}

// Presentation Layer
public class BattleController : MonoBehaviour
{
    [Inject] private readonly IBattleService _battleService;
}
```

### Merge Mechanics
```csharp
public interface IMergeService
{
    bool CanMerge(IUnit unit1, IUnit unit2);
    Task<IUnit> MergeUnitsAsync(IUnit unit1, IUnit unit2, CancellationToken token);
    MergeResult CalculateMergeResult(IUnit unit1, IUnit unit2);
}

public class MergeService : IMergeService
{
    private readonly IUnitFactory _unitFactory;
    private readonly IMergeRulesProvider _mergeRules;
    
    public MergeService(IUnitFactory unitFactory, IMergeRulesProvider mergeRules)
    {
        _unitFactory = unitFactory;
        _mergeRules = mergeRules;
    }
    
    public async Task<IUnit> MergeUnitsAsync(IUnit unit1, IUnit unit2, CancellationToken token)
    {
        MergeResult result = _mergeRules.GetMergeResult(unit1.Type, unit2.Type);
        await PlayMergeEffectAsync(token);
        return _unitFactory.Create(result.ResultUnitData);
    }
}
```

### Turn-Based Actions (Command Pattern)
```csharp
public interface ITurnAction
{
    Task ExecuteAsync(CancellationToken token);
    void Undo();
    bool CanExecute();
}

public class AttackAction : ITurnAction
{
    private readonly IUnit _attacker;
    private readonly IUnit _target;
    private readonly IDamageCalculator _damageCalculator;
    
    public AttackAction(IUnit attacker, IUnit target, IDamageCalculator damageCalculator)
    {
        _attacker = attacker;
        _target = target;
        _damageCalculator = damageCalculator;
    }
    
    public async Task ExecuteAsync(CancellationToken token)
    {
        float damage = _damageCalculator.Calculate(_attacker, _target);
        await _attacker.PlayAttackAnimationAsync(token);
        _target.TakeDamage(damage);
    }
    
    public bool CanExecute()
    {
        return _attacker.CanAct && !_target.IsDead;
    }
    
    public void Undo()
    {
        // Undo logic if needed
    }
}
```

## Signals (Zenject Event System)
```csharp
// Define signals
public class UnitDiedSignal
{
    public IUnit Unit { get; }
    public UnitDiedSignal(IUnit unit) => Unit = unit;
}

public class MergeCompletedSignal
{
    public IUnit ResultUnit { get; }
    public MergeCompletedSignal(IUnit resultUnit) => ResultUnit = resultUnit;
}

// Install signals
public class SignalsInstaller : Installer<SignalsInstaller>
{
    public override void InstallBindings()
    {
        Container.DeclareSignal<UnitDiedSignal>();
        Container.DeclareSignal<MergeCompletedSignal>();
    }
}

// Fire signals
public class Unit : IUnit
{
    private readonly SignalBus _signalBus;
    
    public void Die()
    {
        _signalBus.Fire(new UnitDiedSignal(this));
    }
}

// Subscribe to signals
public class BattleObserver : IInitializable, IDisposable
{
    private readonly SignalBus _signalBus;
    
    public void Initialize()
    {
        _signalBus.Subscribe<UnitDiedSignal>(OnUnitDied);
    }
    
    public void Dispose()
    {
        _signalBus.Unsubscribe<UnitDiedSignal>(OnUnitDied);
    }
    
    private void OnUnitDied(UnitDiedSignal signal)
    {
        // Handle unit death
    }
}
```

## ScriptableObject Data
```csharp
[CreateAssetMenu(fileName = "UnitData", menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    [SerializeField] private string _unitName;
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _attackPower;
    [SerializeField] private Sprite _sprite;
    [SerializeField] private UnitType _unitType;
    
    public string UnitName => _unitName;
    public int MaxHealth => _maxHealth;
    public int AttackPower => _attackPower;
    public Sprite Sprite => _sprite;
    public UnitType UnitType => _unitType;
}
```

## Error Handling
```csharp
// ✅ Always handle exceptions in async methods
public async Task<bool> TryExecuteActionAsync(ITurnAction action, CancellationToken token)
{
    try
    {
        await action.ExecuteAsync(token);
        return true;
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Action was cancelled");
        return false;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to execute action: {ex.Message}");
        return false;
    }
}
```

## Testing Guidelines
```csharp
// Use NUnit for unit tests
[Test]
public void MergeService_SameTypeUnits_CreatesUpgradedUnit()
{
    // Arrange
    var unit1 = new Unit(UnitType.Warrior, level: 1);
    var unit2 = new Unit(UnitType.Warrior, level: 1);
    
    // Act
    var result = _mergeService.CalculateMergeResult(unit1, unit2);
    
    // Assert
    Assert.AreEqual(UnitType.Warrior, result.UnitType);
    Assert.AreEqual(2, result.Level);
}
```

## Code Review Checklist
- [ ] Follows Microsoft C# coding conventions
- [ ] All dependencies injected via Zenject
- [ ] No GetComponent in Update/FixedUpdate
- [ ] No FindObjectOfType or GameObject.Find
- [ ] Async methods use Tasks, not Coroutines
- [ ] CancellationTokens used for async operations
- [ ] Components cached in Awake
- [ ] Events used instead of Update polling
- [ ] Object pooling for frequently instantiated objects
- [ ] SOLID principles applied
- [ ] Interfaces used for dependencies
- [ ] Proper error handling
- [ ] No warnings in Console
- [ ] Unit tests written for core logic

## MCP Tools Usage

### When to use ai-game-developer MCP
- Game design decisions and balancing
- Procedural generation algorithms
- AI behavior implementation
- Game mechanics implementation
- Performance optimization strategies

### When to use context7 MCP
- Understanding existing codebase
- Finding similar patterns in project
- Locating specific implementations
- Code navigation and exploration
- Refactoring suggestions based on project context

## Common Anti-Patterns to AVOID
```csharp
// ❌ Singleton pattern (use Zenject instead)
public static GameManager Instance;

// ❌ FindObjectOfType in runtime
var player = FindObjectOfType<Player>();

// ❌ String-based references
GameObject.Find("Player");
transform.Find("HealthBar");

// ❌ Coroutines for new async code
IEnumerator DoSomething() { }

// ❌ Update polling
void Update() {
    if (Input.GetKeyDown(KeyCode.Space)) { }
}

// ❌ Direct GameObject references
[SerializeField] private GameObject _player;

// ✅ Use interfaces and dependency injection instead
[Inject] private readonly IPlayer _player;
```

## Performance Targets
- Target FPS: 60 (mobile: 30-60)
- Battle turn execution: <500ms
- Merge animation: <1s
- UI response time: <100ms
- Memory budget: 512MB (mobile), 2GB (PC)

## When Making Changes
1. Ensure all dependencies are injected via Zenject
2. Use async/await for all asynchronous operations
3. Cache all component references
4. Follow Clean Architecture layers
5. Apply SOLID principles
6. Write unit tests for business logic
7. Test in Play mode
8. Check for warnings in Console
9. Verify no performance regressions
10. Update documentation if adding new systems

## Test
1. Use NSubstitute for mocks.
2. Use Fluent Assertions for asserts.Прод
3. Place editor tests in the Assets/Code/Tests/EditorTests folder.
4. Place tests intended for Play Mode in the Assets/Code/Tests/PlayModeTests folder.