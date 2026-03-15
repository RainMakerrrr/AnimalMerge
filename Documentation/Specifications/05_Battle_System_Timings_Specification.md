# Спецификация системы таймингов боевой системы (Battle System Timings)

**Версия:** 1.0
**Дата:** 2026-03-15
**Статус:** Актуальная документация

---

## 1. Обзор

Данная спецификация описывает все тайминги и временные параметры боевой системы:
- Последовательность состояний боя (State Machine)
- Длительность движения юнитов
- Скорость и тайминги атак
- Время выполнения способностей
- Искусственные задержки в системе

**Ключевые компоненты:**
- `BattleStateMachine` - управление состояниями боя
- `TurnExecutor` - выполнение ходов юнитов
- `AnimalMovement` - передвижение и тайминги движения
- `AnimalAttack` / `ChickenAttack` - атаки и их длительность
- `AbilityManager` - управление способностями
- `Dodge` / `CounterAttack` - специальные способности

**Цель документа:**
- Дать полное понимание временных параметров системы
- Упростить настройку баланса длительностей
- Помочь в отладке и оптимизации боя

---

## 2. Архитектура BattleStateMachine

### 2.1 Граф состояний

```
┌─────────────────┐
│ PreBattleState  │ (Подготовка: спавн юнитов, ожидание Input)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│BattleStartState │ (Мгновенный переход)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ PlayerTurnState │◄─────────────────┐
└────────┬────────┘                  │
         │                           │
         ▼                           │
┌─────────────────┐                  │
│ EnemyTurnState  │                  │
└────────┬────────┘                  │
         │                           │
         ▼                           │
┌─────────────────┐                  │
│CheckVictoryState│                  │
└────────┬────────┘                  │
         │                           │
    ┌────┴────┐                      │
    │         │                      │
    ▼         ▼         ▼            │
Victory   Defeat   Ongoing───────────┘
    │         │         (новый раунд)
    ▼         │
┌─────────────────┐    │
│StageClearState  │    │
└────────┬────────┘    │
         │             │
    ┌────┴────┐        │
    │         │        │
    ▼         ▼        ▼
More Stages  All   Defeat
            Complete
    │         │        │
    ▼         ▼        ▼
PreBattle BattleEnd BattleEnd
           │         │
           ▼         ▼
        WinState  LoseState
```

### 2.2 Описание состояний

#### 2.2.1 PreBattleState
**Файл:** `Assets/Code/Battle/States/PreBattleState.cs`

**Функции:**
- Спавн игровых юнитов (первый уровень или подкрепление)
- Спавн врагов для текущей стадии
- Регистрация юнитов в `UnitTracker`
- Включение merge undo tracking
- Ожидание подтверждения игрока (Enter / кнопка "Start Battle")

**Тайминги:**
- Нет фиксированных задержек
- Ожидание пользовательского ввода (событие `StartBattleRequested`)

**Переход:**
```csharp
// PreBattleState.cs:171
await _stateMachine.ChangeStateAsync<PlayerTurnState>();
```

---

#### 2.2.2 BattleStartState
**Файл:** `Assets/Code/Battle/States/BattleStartState.cs`

**Функции:**
- Технический state, мгновенный переход

**Тайминги:**
- 0 секунд (мгновенно)

**Переход:**
```csharp
// BattleStartState.cs:25
return _stateMachine.ChangeStateAsync<PlayerTurnState>();
```

---

#### 2.2.3 PlayerTurnState
**Файл:** `Assets/Code/Battle/States/PlayerTurnState.cs`

**Функции:**
- Выполнение ходов всех живых игровых юнитов
- Последовательное выполнение (один за другим)

**Тайминги:**
```csharp
// PlayerTurnState.cs:25
await _turnExecutor.ExecutePlayerTurnsAsync();
```
- Длительность = сумма всех ходов юнитов (см. раздел 3)

**Переход:**
```csharp
// PlayerTurnState.cs:28
await _stateMachine.ChangeStateAsync<EnemyTurnState>();
```

---

#### 2.2.4 EnemyTurnState
**Файл:** `Assets/Code/Battle/States/EnemyTurnState.cs`

**Функции:**
- Аналогично `PlayerTurnState`, но для врагов
- Другой порядок сортировки юнитов

**Тайминги:**
- Длительность = сумма всех ходов врагов

**Переход:**
```csharp
// EnemyTurnState.cs:28
await _stateMachine.ChangeStateAsync<CheckVictoryState>();
```

---

#### 2.2.5 CheckVictoryState
**Файл:** `Assets/Code/Battle/States/CheckVictoryState.cs`

**Функции:**
- Проверка условий победы/поражения/продолжения

**Тайминги:**
- Мгновенная проверка (0 секунд)

**Переходы:**
```csharp
// CheckVictoryState.cs:32-43
switch (result)
{
    case BattleResult.Victory:
        return _stateMachine.ChangeStateAsync<StageClearState>();

    case BattleResult.Defeat:
        return _stateMachine.ChangeStateAsync<BattleEndState>();

    case BattleResult.Ongoing:
        return _stateMachine.ChangeStateAsync<PlayerTurnState>();
}
```

---

#### 2.2.6 StageClearState
**Файл:** `Assets/Code/Battle/States/StageClearState.cs`

**Функции:**
- Восстановление HP выживших юнитов (100%)
- Перемещение юнитов на merge grid
- Очистка врагов
- Переход к следующей стадии

**Тайминги:**
- Мгновенные операции (0 секунд)

**Переходы:**
```csharp
// StageClearState.cs:55-64
if (_flowController.HasMoreStages())
{
    return _stateMachine.ChangeStateAsync<PreBattleState>();
}
else
{
    return _stateMachine.ChangeStateAsync<BattleEndState>();
}
```

---

#### 2.2.7 BattleEndState
**Файл:** `Assets/Code/Battle/States/BattleEndState.cs`

**Функции:**
- Cleanup текущего уровня
- Переход в глобальные состояния (WinState / LoseState)

**Тайминги:**
- 0 секунд

**Переходы:**
```csharp
// BattleEndState.cs:37-46
if (result == BattleResult.Victory)
{
    _gameStateMachine.Enter<WinState>();
}
else
{
    _gameStateMachine.Enter<LoseState>();
}
```

---

## 3. Тайминги хода юнита (TurnExecutor)

### 3.1 Общая схема выполнения хода

**Файл:** `Assets/Code/Battle/Services/TurnExecutor.cs`

```
ExecuteSingleUnitTurnAsync (TurnExecutor.cs:90)
│
├─► 1. FindValidTarget (мгновенно)
│      └─► TargetFinder.FindClosestTarget()
│
├─► 2. GetClosestEnemyCell (мгновенно)
│      └─► Вычисление ближайшей клетки цели
│
├─► 3. IsCloseToTarget (мгновенно)
│      └─► Проверка дистанции до цели
│
└─► 4. Выбор действия:
    │
    ├─► ВАРИАНТ A: Близко к цели (TurnExecutor.cs:122)
    │   └─► Attack(target)
    │       └─► Длительность = _attackClip.length
    │           └─► См. раздел 4.1
    │
    └─► ВАРИАНТ B: Далеко от цели (TurnExecutor.cs:140)
        ├─► Movement.Move(targetCell, callback)
        │   └─► Длительность = pathLength / 2.0
        │       └─► См. раздел 3.2
        │
        └─► callback: Attack(target)
            └─► Длительность = _attackClip.length
```

### 3.2 Сортировка юнитов

**Игровые юниты** (слева-направо, сверху-вниз):
```csharp
// TurnExecutor.cs:39-43
var sortedUnits = playerUnits
    .OrderBy(u => u.Movement?.CurrentPathNode?.GridPosition.x ?? 0)
    .ThenByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.y ?? 0)
    .ToList();
```

**Вражеские юниты** (справа-налево, сверху-вниз):
```csharp
// TurnExecutor.cs:59-63
var sortedUnits = enemyUnits
    .OrderByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.x ?? 0)
    .ThenByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.y ?? 0)
    .ToList();
```

---

## 4. Тайминги атак

### 4.1 Обычная атака (AnimalAttack)

**Файл:** `Assets/Code/Animals/AnimalAttack.cs`

#### Последовательность:
```csharp
// AnimalAttack.cs:120-127
public virtual async Task Attack(ITarget target)
{
    _targetOverride = target;
    await _animator.WaitForAttackAnimation();  // ← Основная задержка
    _targetOverride = null;
}
```

#### Внутри WaitForAttackAnimation:
```csharp
// AnimalAnimator.cs:27-32
public async Task WaitForAttackAnimation()
{
    PlayAttackAnimation();  // Запуск триггера "Attack"

    await Task.Delay(TimeSpan.FromSeconds(_attackClip.length));  // ← ЗАДЕРЖКА
}
```

#### Настройка:
- **Где:** Inspector → AnimalAnimator компонент
- **Параметр:** `_attackClip` (Animation Clip)
- **Типичные значения:** 0.5-1.5 секунды

#### Процесс атаки:
```
0.0s   PlayAttackAnimation()
       │
       ├─► Запуск Animation Trigger "Attack"
       │
0.Xs   Animation Event вызывает AttackAnimationHandler()
       │
       ├─► AttackAnimationHandlerAsync()
       │   ├─► Physics detection (OverlapCapsule/OverlapSphere)
       │   ├─► Deduplication (HashSet<IDamageable>)
       │   └─► TakeDamageAsync() для каждой цели
       │       └─► См. раздел 6
       │
Ns     Завершение Task.Delay(_attackClip.length)
       └─► Конец атаки
```

---

### 4.2 Специальная атака - Chicken Jump

**Файл:** `Assets/Code/Animals/ChickenAttack.cs`

#### Параметры прыжка:
```csharp
// ChickenAttack.cs:15-18
[SerializeField] private float _jumpDuration = 0.5f;    // Длительность одного прыжка
[SerializeField] private float _jumpPower = 2f;         // Высота прыжка
[SerializeField] private int _numJumps = 1;             // Количество прыжков
[SerializeField] private float _jumpDistance = 1.5f;    // Дистанция прыжка
```

#### Последовательность:
```csharp
// ChickenAttack.cs:66-121
PerformJumpAttack:
│
├─► 1. DOJump к цели (_jumpDuration = 0.5s)
│   │
│   ├─► 0.25s: Task.Delay(halfDuration)
│   │   └─► ChickenAttack.cs:91
│   │
│   ├─► 0.25s: TakeDamageAsync(target)
│   │   └─► ChickenAttack.cs:94
│   │
│   └─► Ожидание завершения прыжка
│       └─► ChickenAttack.cs:97
│
└─► 2. DOJump обратно (_jumpDuration = 0.5s)
    └─► ChickenAttack.cs:102-107
```

#### Общая длительность:
```
Прыжок к цели:     0.5s
Прыжок обратно:    0.5s
─────────────────────────
Итого:             1.0s
```

#### Timeline атаки курицы:
```
0.0s   Начало прыжка к цели
       │
       ├─► transform.DOJump(target, power, numJumps, 0.5s)
       │
0.25s  Середина прыжка (пик)
       │
       ├─► await Task.Delay(250ms)
       ├─► await target.TakeDamageAsync(this)
       │   └─► Урон наносится в воздухе!
       │
0.5s   Приземление у цели
       │
       └─► await jumpTween.AsyncWaitForCompletion()

0.5s   Начало прыжка обратно
       │
       ├─► transform.DOJump(originalPos, power, numJumps, 0.5s)
       │
1.0s   Приземление на исходную позицию
       │
       └─► Конец атаки
```

---

## 5. Тайминги движения

### 5.1 Обычное движение (AnimalMovement)

**Файл:** `Assets/Code/Animals/Movement/AnimalMovement.cs`

#### Параметры скорости:
```csharp
// AnimalMovement.cs:30
[SerializeField] private int _tilesPerMove = 2;  // Клеток за ход
```

**Настройка:**
- `SetTilesPerMove(int value)` - установить напрямую (line 117)
- `Upgrade(int multiplier)` - умножить скорость (line 122)

#### Формула длительности движения:
```csharp
// AnimalMovement.cs:502
var tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
```

**Длительность = количество клеток в пути / 2.0**

#### Примеры:
| Длина пути | Длительность |
|-----------|-------------|
| 1 клетка  | 0.5 секунды |
| 2 клетки  | 1.0 секунда |
| 4 клетки  | 2.0 секунды |
| 6 клеток  | 3.0 секунды |

#### Ограничение пути:
```csharp
// AnimalMovement.cs:484-489
private List<GridCell> LimitPathBySpeed(List<GridCell> path)
{
    if (path.Count > _tilesPerMove + 1)
        return path.Take(_tilesPerMove + 1).ToList();
    return path;
}
```

**Примечание:** Путь включает стартовую клетку, поэтому:
- `_tilesPerMove = 2` → путь до 3 клеток (старт + 2 движения)

---

### 5.2 Движение Dodge (Shift)

**Файл:** `Assets/Code/Animals/Movement/AnimalMovement.cs`

#### Процесс:
```csharp
// AnimalMovement.cs:315-324
public async Task Shift()
{
    var dodgePositions = GetDodgePositions();
    var path = FindPath(dodgePositions);

    if (path == null || path.Count == 0) return;

    await ExecuteDodgeMovement(path);
    UpdateNodeOccupancy(path.Last());
}
```

#### Возможные позиции dodge:
```csharp
// AnimalMovement.cs:542-550
private Vector2Int[] GetDodgePositions()
{
    return new Vector2Int[]
    {
        new Vector2Int(_currentPathNode.X + 1, _currentPathNode.Y),  // Вправо
        new Vector2Int(_currentPathNode.X - 1, _currentPathNode.Y),  // Влево
        new Vector2Int(_currentPathNode.X, _currentPathNode.Y - 1),  // Вниз
    };
}
```

#### Длительность dodge:
```csharp
// AnimalMovement.cs:561
var tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
```

**Та же формула:** `pathLength / 2.0`

Обычно dodge = 1 клетка → **0.5 секунды**

---

### 5.3 Анимации поворотов во время движения

**Файл:** `Assets/Code/Animals/Movement/AnimalMovement.cs`

#### Параметры:
```csharp
// AnimalMovement.cs:34-38
[SerializeField] private float _rotationSpeed = 10f;
[SerializeField] private float _turnAnimationDuration = 0.5f;
[SerializeField] private float _turnDirectionSmoothSpeed = 4f;
[SerializeField] private float _turnAmplification = 1.3f;
[SerializeField] private float _turnAngleThreshold = 10f;
```

#### Процесс:
```csharp
// AnimalMovement.cs:502-514
.OnWaypointChange(i =>
{
    var nextIndex = i + 1;
    if (nextIndex >= pathPositions.Length) return;

    CalculateTurnDirection(pathPositions[i], pathPositions[nextIndex]);
})
.OnUpdate(() =>
{
    ApplyRotation();
    UpdateTurnTimer();
    _movementAnimator.PlayMovementAnimation(1f);
})
```

**Примечание:** Анимации поворотов не влияют на общую длительность движения, выполняются параллельно.

---

## 6. Тайминги способностей (Abilities)

### 6.1 Порядок выполнения при получении урона

**Файл:** `Assets/Code/Animals/Health/AnimalHealth.cs`

```csharp
// AnimalHealth.cs:119-144
public virtual async Task TakeDamageAsync(AnimalAttack attacker)
{
    // 1. СНАЧАЛА выполняются способности
    bool isBlockedDamage = await ApplyAbilities(attacker);  // ← КРИТИЧНО!

    // 2. Если урон заблокирован - возврат
    if (isBlockedDamage)
    {
        return;  // Урон НЕ применяется
    }

    // 3. Применение урона
    Current -= attacker.Damage;
    TakenDamage?.Invoke();
    _animator.TakeDamageAnimation();

    // 4. Проверка смерти
    if (IsDead)
    {
        Die();
    }
}
```

### 6.2 AbilityManager - централизованное управление

**Файл:** `Assets/Code/Abilities/AbilityManager.cs`

#### Процесс выполнения:
```csharp
// AbilityManager.cs:87-132
public async Task<bool> ExecuteAbilitiesAsync(AbilityContext context)
{
    // 1. Сортировка по приоритету (descending)
    _sortedAbilities = _abilities.OrderByDescending(a => a.Priority).ToList();

    bool damageBlocked = false;

    // 2. Выполнение ВСЕХ способностей, у которых CanUse = true
    foreach (var ability in _sortedAbilities)
    {
        if (!ability.CanUse(context.Attacker))
            continue;

        await ability.Apply();  // ← Выполнение способности

        if (ability.IsBlockingDamage)
        {
            damageBlocked = true;
            // ВАЖНО: продолжаем выполнение остальных способностей!
        }
    }

    return damageBlocked;
}
```

**Ключевое поведение:**
- Все способности выполняются **последовательно** (await)
- Даже если Dodge заблокировал урон, CounterAttack всё равно выполнится!
- Порядок: от высшего приоритета к низшему

---

### 6.3 Dodge (Уклонение)

**Файл:** `Assets/Code/Abilities/Dodge.cs`

#### Параметры:
```csharp
// Dodge.cs:15-16
public bool IsBlockingDamage => true;  // Блокирует урон
public int Priority => 1;              // Высокий приоритет (выполняется ПЕРВЫМ)
```

#### Условия срабатывания:
```csharp
// Dodge.cs:18-33
public bool CanUse(IAttacker attacker)
{
    // 1. НЕ работает против AoE
    if (attacker != null && attacker.IsAoE)
        return false;

    // 2. Первое использование: 100%
    if (_counter == 0) return true;

    // 3. Последующие использования:
    int successThreshold = _isOwner ? 80 : 50;  // 80% owner / 50% inherited
    return _randomProvider.Range(0, 100) < successThreshold;
}
```

#### Последовательность Apply:
```csharp
// Dodge.cs:43-65
public async Task Apply()
{
    // 1. Отключить коллайдеры (мгновенно)
    foreach (Collider collider in _colliders)
    {
        collider.enabled = false;
    }

    // 2. Телепорт в сторону
    _counter++;
    await _transformable.Shift();  // ← AnimalMovement.Shift()
                                   //   └─► ExecuteDodgeMovement
                                   //       └─► DOPath (pathLength / 2.0)

    // 3. Включить коллайдеры (мгновенно)
    foreach (Collider collider in _colliders)
    {
        if (collider != null)
        {
            collider.enabled = true;
        }
    }
}
```

#### Общая длительность Dodge:
**~0.5 секунды** (стандартный dodge на 1 клетку)

---

### 6.4 CounterAttack (Контратака)

**Файл:** `Assets/Code/Abilities/CounterAttack.cs`

#### Параметры:
```csharp
// CounterAttack.cs:20-21
public bool IsBlockingDamage => false;  // НЕ блокирует урон!
public int Priority => 0;               // Низкий приоритет (после Dodge)
```

#### Условия срабатывания:
```csharp
// CounterAttack.cs:23-40
public bool CanUse(IAttacker attacker)
{
    _currentAttacker = attacker;  // Сохраняем атакующего

    // 1. НЕ работает против AoE
    if (attacker != null && attacker.IsAoE)
        return false;

    // 2. Owner: всегда 100%
    if (_isOwner) return true;

    // 3. Inherited: 50%
    return _randomProvider.Range(0, 100) < 50;
}
```

#### Последовательность Apply:
```csharp
// CounterAttack.cs:51-74
public async Task Apply()
{
    // 1. Анимация контратаки (мгновенный триггер)
    if (_animator != null)
    {
        _animator.CounterAttackAnimation();
    }

    // 2. Получить IDamageable атакующего
    var attackerHealth = _currentAttacker.Damageable;

    // 3. Нанести урон обратно (РЕКУРСИЯ!)
    await attackerHealth.TakeDamageAsync(_attack);
    //    └─► Может вызвать способности АТАКУЮЩЕГО!
    //        └─► Например, атакующий может ТОЖЕ увернуться или контратаковать!
}
```

#### Длительность CounterAttack:
**Зависит от атаки защищающегося:**
- Если у защищающегося обычная атака → `_attackClip.length` секунд
- Если курица → ~1.0 секунда (прыжок туда-обратно)

**ВАЖНО:** Урон всё равно проходит защищающемуся! CounterAttack НЕ блокирует урон.

---

## 7. Искусственные задержки

### 7.1 Ожидание анимации атаки

**Файл:** `Assets/Code/Animals/AnimalAnimator.cs`

```csharp
// AnimalAnimator.cs:27-32
public async Task WaitForAttackAnimation()
{
    PlayAttackAnimation();

    await Task.Delay(TimeSpan.FromSeconds(_attackClip.length));  // ← ЗАДЕРЖКА
}
```

**Цель:** Синхронизация с Animation Event
**Настройка:** Через Inspector, поле `_attackClip`

---

### 7.2 Урон в середине прыжка (Chicken)

**Файл:** `Assets/Code/Animals/ChickenAttack.cs`

```csharp
// ChickenAttack.cs:89-94
var halfDuration = _jumpDuration * 0.5f;
await Task.Delay((int)(halfDuration * 1000));  // ← ЗАДЕРЖКА

await target.Damageable.TakeDamageAsync(this);
```

**Цель:** Нанести урон в пике прыжка
**Длительность:** `_jumpDuration / 2` (по умолчанию 0.25 секунды)

---

### 7.3 Задержка перед уничтожением трупа

**Файл:** `Assets/Code/Animals/Health/AnimalHealth.cs`

```csharp
// AnimalHealth.cs:167-172
private IEnumerator DestroyWithDelay()
{
    yield return new WaitForSeconds(3f);  // ← ЗАДЕРЖКА (Coroutine!)

    Destroy(gameObject);
}
```

**Цель:** Показать анимацию смерти перед уничтожением
**Длительность:** 3 секунды (фиксировано)

---

## 8. Полная схема хода одного юнита

```
┌──────────────────────────────────────────────────────────────┐
│ TurnExecutor.ExecuteSingleUnitTurnAsync                      │
└────────────┬─────────────────────────────────────────────────┘
             │
             ├─► FindValidTarget (мгновенно)
             │
             ├─► GetClosestEnemyCell (мгновенно)
             │
             └─► IsCloseToTarget?
                 │
       ┌─────────┴─────────┐
       │                   │
   ДА (близко)         НЕТ (далеко)
       │                   │
       │                   ├─► Move(targetCell, callback)
       │                   │   │
       │                   │   ├─► ExecuteMovement
       │                   │   │   └─► DOPath (pathLength / 2.0)
       │                   │   │       Длительность: 0.5-3.0s
       │                   │   │
       │                   │   └─► callback()
       │                   │       └─► Attack(target)
       │                   │
       └───────────────────┴─► Attack(target)
                               │
                               ├─► WaitForAttackAnimation
                               │   └─► Task.Delay(_attackClip.length)
                               │       Длительность: 0.5-1.5s
                               │       │
                               │       ├─► Animation Event
                               │       │   └─► AttackAnimationHandlerAsync
                               │       │       │
                               │       │       ├─► Physics detection
                               │       │       │
                               │       │       └─► target.TakeDamageAsync(this)
                               │       │           │
                               │       │           ├─► ApplyAbilities
                               │       │           │   │
                               │       │           │   ├─► Dodge?
                               │       │           │   │   └─► Shift() (0.5s)
                               │       │           │   │
                               │       │           │   └─► CounterAttack?
                               │       │           │       └─► TakeDamageAsync обратно
                               │       │           │           (рекурсивно!)
                               │       │           │
                               │       │           └─► Current -= Damage
                               │       │               (если не заблокировано)
                               │       │
                               │       └─► Завершение Task.Delay
                               │
                               └─► Конец атаки
```

---

## 9. Примеры расчётов длительности

### 9.1 Пример 1: Простая атака рядом с целью

**Условия:**
- Юнит уже рядом с целью
- Обычная атака (не курица)
- `_attackClip.length = 0.8s`
- Цель без способностей

**Расчёт:**
```
1. FindTarget:              0.0s
2. IsCloseToTarget: true    0.0s
3. Attack:                  0.8s  (_attackClip.length)
   ├─► WaitForAnimation     0.8s
   └─► TakeDamageAsync      0.0s  (нет способностей)
───────────────────────────────
ИТОГО:                      0.8s
```

---

### 9.2 Пример 2: Движение + атака + Dodge

**Условия:**
- Начальная позиция: (0, 0)
- Цель на позиции: (3, 0)
- Путь: 3 клетки
- `_tilesPerMove = 2` → ограничение пути до 3 клеток (старт + 2 движения)
- `_attackClip.length = 0.8s`
- У цели есть Dodge (первое использование, 100%)

**Расчёт:**
```
1. FindTarget:              0.0s
2. GetClosestEnemyCell:     0.0s
3. IsCloseToTarget: false   0.0s
4. Move:                    1.5s  (3 клетки / 2.0)
5. Attack:                  0.8s  (_attackClip.length)
   ├─► WaitForAnimation     0.8s
   └─► TakeDamageAsync:
       └─► Dodge.Apply:     0.5s  (shift на 1 клетку)
───────────────────────────────
ИТОГО:                      2.8s
```

---

### 9.3 Пример 3: Chicken Jump + CounterAttack

**Условия:**
- Курица атакует врага
- `_jumpDuration = 0.5s`
- У врага есть CounterAttack (owner, 100%)
- У врага обычная атака `_attackClip.length = 0.6s`

**Расчёт:**
```
1. FindTarget:                  0.0s
2. ChickenAttack:               1.0s  (прыжок туда + обратно)
   ├─► DOJump к цели            0.5s
   │   ├─► Delay 0.25s          0.25s
   │   └─► TakeDamageAsync:
   │       └─► CounterAttack:   0.6s  (контратака врага)
   │           └─► Курица получает урон
   ├─► DOJump обратно           0.5s
   └─► Завершение
───────────────────────────────────
ИТОГО:                          1.6s

Примечание: CounterAttack выполняется в СЕРЕДИНЕ прыжка,
но не блокирует возврат курицы на исходную позицию.
```

---

### 9.4 Пример 4: Полный раунд боя (3 игрока vs 2 врага)

**Условия:**
- 3 игровых юнита (обычные атаки, 0.8s)
- 2 врага (обычные атаки, 0.6s)
- Все рядом с целями (нет движения)
- Нет способностей

**Расчёт:**
```
PlayerTurnState:
├─► Юнит 1: Attack         0.8s
├─► Юнит 2: Attack         0.8s
└─► Юнит 3: Attack         0.8s
    └─► Итого:             2.4s

EnemyTurnState:
├─► Враг 1: Attack         0.6s
└─► Враг 2: Attack         0.6s
    └─► Итого:             1.2s

CheckVictoryState:         0.0s  (мгновенно)

Переход к PlayerTurnState: 0.0s
─────────────────────────────
ИТОГО (1 раунд):           3.6s
```

---

## 10. Настраиваемые параметры

### 10.1 Скорость движения

**Где:** `AnimalMovement` компонент → Inspector

| Параметр | Тип | Значение по умолчанию | Описание |
|----------|-----|----------------------|----------|
| `_tilesPerMove` | int | 2 | Клеток за ход |

**Программная настройка:**
```csharp
movement.SetTilesPerMove(4);      // Установить напрямую
movement.Upgrade(2);              // Умножить на 2 (2 → 4)
```

**Влияние на длительность:**
- Длительность движения **НЕ зависит** от `_tilesPerMove`
- `_tilesPerMove` только ограничивает максимальную длину пути
- Длительность = `actualPathLength / 2.0`

---

### 10.2 Длительность атак

**Где:** `AnimalAnimator` компонент → Inspector

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_attackClip` | AnimationClip | Анимация атаки |

**Влияние:**
```csharp
// AnimalAnimator.cs:31
await Task.Delay(TimeSpan.FromSeconds(_attackClip.length));
```

**Настройка:**
1. Выбрать Animation Clip в Inspector
2. Длительность определяется длиной клипа

---

### 10.3 Параметры прыжка курицы

**Где:** `ChickenAttack` компонент → Inspector

| Параметр | Тип | Значение по умолчанию | Описание |
|----------|-----|--------------------|----------|
| `_jumpDuration` | float | 0.5f | Длительность прыжка (секунды) |
| `_jumpPower` | float | 2f | Высота прыжка |
| `_numJumps` | int | 1 | Количество прыжков |
| `_jumpDistance` | float | 1.5f | Дистанция прыжка |

**Влияние на длительность:**
```
Общая длительность = _jumpDuration × 2
```

---

### 10.4 Анимации поворотов

**Где:** `AnimalMovement` компонент → Inspector

| Параметр | Тип | Значение по умолчанию | Описание |
|----------|-----|--------------------|----------|
| `_rotationSpeed` | float | 10f | Скорость поворота |
| `_turnAnimationDuration` | float | 0.5f | Длительность анимации поворота |
| `_turnDirectionSmoothSpeed` | float | 4f | Сглаживание направления |
| `_turnAmplification` | float | 1.3f | Усиление угла поворота |
| `_turnAngleThreshold` | float | 10f | Минимальный угол для анимации |

**Примечание:** Не влияют на общую длительность хода, выполняются параллельно.

---

## 11. Оптимизация и баланс

### 11.1 Рекомендации по балансу длительностей

**Атаки:**
- Обычные юниты: 0.5-1.0 секунды
- Медленные/тяжёлые юниты: 1.0-1.5 секунды
- Быстрые юниты: 0.3-0.5 секунды

**Движение:**
- Формула `pathLength / 2.0` обеспечивает линейное масштабирование
- 1 клетка = 0.5s → приемлемая скорость для мобильных устройств
- Не рекомендуется изменять делитель (2.0) без тестирования баланса

**Способности:**
- Dodge: 0.5s оптимально (быстрый телепорт)
- CounterAttack: зависит от атаки, но должен быть быстрее обычной атаки

---

### 11.2 Потенциальные проблемы производительности

**1. Длинные раунды боя:**
- Если юнитов много → раунд может длиться 10+ секунд
- Решение: ограничить количество юнитов на поле

**2. Рекурсивные CounterAttack:**
- Если оба юнита имеют CounterAttack → потенциально бесконечная рекурсия
- Текущая защита: CounterAttack НЕ блокирует урон → юниты умрут быстро

**3. Множественные Dodge подряд:**
- Если юнит успешно додживает несколько раз → задержка накапливается
- Решение: уменьшение шанса Dodge после первого использования (уже реализовано)

---

## 12. Changelog

### Версия 1.0 (2026-03-15)
- Первая версия документа
- Документирование всех таймингов боевой системы
- Схемы состояний BattleStateMachine
- Детальное описание длительностей атак, движения, способностей
- Примеры расчётов
- Рекомендации по балансу

---

## 13. Связанные документы

- **[04_Attack_And_Damage_System_Specification.md](04_Attack_And_Damage_System_Specification.md)** - Система атаки и урона
- **[01_Pathfinding_System_Specification.md](01_Pathfinding_System_Specification.md)** - Система поиска пути
- **[03_Merge_And_Skills_System_Specification.md](03_Merge_And_Skills_System_Specification.md)** - Система мерджа и навыков
- **[Концепт-док 2.0](../Концепт-док 2.0-2026012418375932.pdf)** - Геймдизайн документ

---

## 14. Контакты

По вопросам баланса таймингов и изменений обращаться к Game Designer.
По техническим вопросам реализации - к Lead Programmer.
