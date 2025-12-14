# 🏗️ Архитектура A* Pathfinding System

## Обзор системы

Модульная система поиска пути с поддержкой юнитов разных размеров, автоматическим боевым поведением и избежанием столкновений.

---

## 📊 Диаграмма компонентов

```
┌─────────────────────────────────────────────────────────┐
│                    UNITY SCENE                          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐     ┌───────────────────────┐        │
│  │ GridManager  │────>│ GridNode[,] (2D Array)│        │
│  │ (Singleton)  │     │  - GCost, HCost       │        │
│  └──────────────┘     │  - IsOccupied         │        │
│         │             │  - ReservedBy         │        │
│         │             └───────────────────────┘        │
│         │                                               │
│         │             ┌───────────────────────┐        │
│         └────────────>│ PathfindingSystem     │        │
│                       │ (Singleton)           │        │
│                       │  - A* Algorithm       │        │
│                       └───────────────────────┘        │
│                                  │                      │
│                                  │                      │
│  ┌──────────────┐                │                      │
│  │TargetFinding │                │                      │
│  │System        │<───────────────┘                      │
│  │(Singleton)   │                                       │
│  └──────────────┘                                       │
│         │                                               │
│         │                                               │
│         ▼                                               │
│  ┌──────────────┐         ┌──────────────┐            │
│  │    Unit      │<───────>│UnitMovement  │            │
│  │(MonoBehaviour│         │Controller    │            │
│  │     - Size   │         │- State Machine│            │
│  │     - Team   │         │- Path Follow │            │
│  │     - HP     │         └──────────────┘            │
│  └──────────────┘                                       │
│         │                                               │
│         ▼                                               │
│  ┌──────────────┐                                       │
│  │CombatSystem  │                                       │
│  │(Singleton)   │                                       │
│  └──────────────┘                                       │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 🧩 Основные компоненты

### 1. Grid Layer (Сеточный слой)

#### GridNode (MonoBehaviour)
**Назначение**: Представляет одну клетку сетки

**Данные**:
- `GridX, GridY` - координаты в сетке
- `IsWalkable` - проходимость
- `IsOccupied` - занятость юнитом
- `OccupyingUnit` - ссылка на юнита
- `ReservedBy` - кто зарезервировал
- `GCost, HCost, Parent` - данные A*

**Методы**:
- `Occupy(unit)` - занять клетку
- `Free()` - освободить
- `TryReserve(unit)` - зарезервировать
- `IsAvailable(requester)` - проверка доступности

**Визуализация**: Gizmos для отладки, цветовая индикация

#### GridManager (Singleton)
**Назначение**: Управление всей сеткой

**Данные**:
- `GridNode[,] grid` - 2D массив клеток
- `Dictionary<Vector2Int, GridNode>` - быстрый доступ
- `gridWidth, gridHeight, cellSize` - параметры

**Методы**:
- `GenerateGrid()` - создание сетки
- `GetNode(x, y)` - доступ к клетке
- `GetNeighbors(node)` - получить соседей (8 направлений)
- `GetNodesInArea(x, y, width, height)` - область для юнита
- `IsAreaClear(...)` - проверка свободной области
- `OccupyArea(unit, pos)` - занять область
- `FreeArea(unit)` - освободить область
- `GetWorldPosition(x, y)` - конвертация координат
- `ResetPathfindingData()` - сброс A* данных

**Паттерн**: Singleton для глобального доступа

---

### 2. Pathfinding Layer (Слой поиска пути)

#### PathfindingSystem (Singleton)
**Назначение**: Реализация A* алгоритма

**Алгоритм A***:
```
1. OpenList (priority queue по FCost)
2. ClosedList (HashSet)
3. Для каждого соседа:
   - Проверить CanUnitFitAt() - ВСЕ клетки юнита
   - Рассчитать GCost, HCost
   - Diagonal cost = 14, Straight = 10
4. Восстановить путь через Parent links
```

**Ключевая особенность**: Учет размера юнита!

Для юнита 2x2:
```
Текущая клетка: (5, 5)
Проверяем ВСЕ 4 клетки:
  (5,5), (6,5), (5,6), (6,6)
Если хотя бы одна занята - позиция недоступна
```

**Методы**:
- `FindPath(unit, start, goal)` - найти путь
- `CanUnitFitAt(unit, node, goal)` - валидация размера
- `GetHeuristic(from, to)` - Manhattan distance
- `RetracePath(start, end)` - восстановление
- `FindNearestWalkablePosition()` - ближайшая свободная

**Оптимизации**:
- Max iterations = 1000 (защита от зависания)
- Эвристика для ускорения
- Ранний выход при достижении цели

---

### 3. Unit Layer (Слой юнитов)

#### UnitSize (struct)
**Назначение**: Описание размера юнита

```csharp
struct UnitSize {
    int Width;   // X-axis
    int Height;  // Y-axis
}

Predefined:
- Size1X1 (1, 1)
- Size2X1 (1, 2) - вертикальный
- Size2X2 (2, 2) - большой
```

**Важно**: Позиция юнита = bottom-left corner

```
2x2 юнит на позиции (3, 3):
  [*][*]  <- (3,4), (4,4)
  [*][*]  <- (3,3), (4,3)  [3,3 = base]
```

#### Team (enum)
```csharp
enum Team {
    Player,
    Enemy,
    Neutral
}
```

#### Unit (MonoBehaviour)
**Назначение**: Базовый класс юнита

**Свойства**:
- `Size` - размер (UnitSize)
- `Team` - команда
- `MovementSpeed` - скорость
- `AttackRange` - дальность атаки
- `Damage`, `MaxHealth`, `CurrentHealth`
- `CurrentGridPosition` - позиция в сетке
- `CurrentPath` - текущий путь
- `CurrentTarget` - текущая цель

**Методы**:
- `GetOccupiedCells()` - список занимаемых клеток
- `IsInAttackRange(target)` - проверка дистанции
- `CanAttackTarget(target)` - может ли атаковать
- `Attack(target)` - атака
- `TakeDamage(amount)` - получить урон
- `MoveToGridPosition(pos)` - переместить на сетке

**Attack Range Logic**:
```csharp
// Chebyshev distance (диагональ = 1)
foreach (myCell in GetOccupiedCells()) {
    foreach (targetCell in target.GetOccupiedCells()) {
        distance = Max(|x1-x2|, |y1-y2|)
        if (distance <= AttackRange) return true
    }
}
```

Пример:
```
Юнит A на (0,0)
Юнит B на (1,1)
Distance = Max(1, 1) = 1 ✓ Может атаковать
```

---

### 4. Movement Layer (Слой движения)

#### UnitMovementController (MonoBehaviour)
**Назначение**: Управление поведением юнита

**State Machine**:
```
┌──────┐
│ Idle │<──────────┐
└───┬──┘           │
    │              │
    ▼              │
┌────────────────┐ │
│SearchingTarget │ │
└───┬────────────┘ │
    │              │
    ▼              │
┌────────────────┐ │
│ MovingToTarget │─┤
└───┬────────────┘ │
    │              │
    ▼              │
┌──────────┐       │
│Attacking │───────┘
└──────────┘
    │
    ▼
┌─────────┐
│ Waiting │ (если путь заблокирован)
└─────────┘
```

**Логика каждого состояния**:

**Idle**:
- Ничего не делает
- Периодически переходит в SearchingTarget

**SearchingTarget**:
- Ищет ближайшего врага (TargetFindingSystem)
- Если нашел и в радиусе → Attacking
- Если нашел далеко → строит путь → MovingToTarget
- Если не нашел → Idle

**MovingToTarget**:
- Двигается по пути (List<GridNode>)
- Проверяет доступность следующей клетки
- Резервирует клетки перед занятием
- Пересчитывает путь каждые N секунд
- Если достиг радиуса атаки → Attacking
- Если путь заблокирован → Waiting

**Attacking**:
- Атакует цель
- Проверяет cooldown
- Если цель вышла из радиуса → MovingToTarget
- Если цель умерла → SearchingTarget

**Waiting**:
- Ждет, пока путь освободится
- Timeout = 2 секунды
- Если освободился → MovingToTarget
- Если timeout → SearchingTarget (пересчитать путь)

**Методы**:
- `UpdateIdle/SearchingTarget/MovingToTarget/...()` - обновление состояний
- `BuildPathToTarget()` - построение пути
- `MoveAlongPath()` - движение по пути
- `ReserveNode(node)` - резервация клетки
- `IsNodeAvailable(node)` - проверка для размера юнита

---

### 5. Combat Layer (Боевой слой)

#### TargetFindingSystem (Singleton)
**Назначение**: Поиск врагов и управление целями

**Кэш юнитов**:
```csharp
Dictionary<Team, List<Unit>> unitsByTeam
```
Обновляется каждые 0.5 секунд для производительности

**Методы**:
- `FindNearestEnemy(seeker)` - ближайший враг
- `FindEnemiesInRadius(seeker, radius)` - все в радиусе
- `GetGridDistance(a, b)` - Manhattan distance по сетке
- `GetAttackPosition(attacker, target)` - позиция для атаки
- `RegisterUnit(unit)` - добавить в систему
- `UnregisterUnit(unit)` - удалить из системы

**Grid Distance**:
```
Для юнитов разных размеров:
- Берем все клетки обоих юнитов
- Находим минимальное расстояние между парами клеток
- Manhattan: |x1-x2| + |y1-y2|
```

#### CombatSystem (Singleton)
**Назначение**: Управление боем и событиями

**События**:
```csharp
event OnAttackPerformed(attacker, target, damage)
event OnDamageTaken(source, target, damage)
event OnUnitDied(unit)
```

**Методы**:
- `PerformAttack(attacker, target)` - выполнить атаку с эффектами
- `ApplyDamage(target, damage, source)` - нанести урон
- `IsInAttackRange(attacker, target)` - проверка дистанции
- `GetAttackDistance(attacker, target)` - Chebyshev distance

**Расширение**:
```csharp
// Подписка на события
CombatSystem.OnAttackPerformed += (atk, tgt, dmg) => {
    SpawnHitEffect(tgt.transform.position);
    PlaySound("hit");
};
```

---

## 🔄 Жизненный цикл юнита

```
1. Start()
   ├─> Инициализация позиции на сетке
   ├─> GridManager.OccupyArea(this, position)
   └─> State = SearchingTarget

2. SearchingTarget
   ├─> TargetFindingSystem.FindNearestEnemy()
   ├─> Если нашел врага
   │   ├─> Проверка IsInAttackRange()
   │   ├─> Да → State = Attacking
   │   └─> Нет → PathfindingSystem.FindPath()
   │           └─> State = MovingToTarget
   └─> Если не нашел → State = Idle

3. MovingToTarget
   ├─> Foreach node in path:
   │   ├─> IsNodeAvailable() - проверка для всех клеток юнита
   │   ├─> ReserveNode() - резервация
   │   ├─> MoveTowards() - движение
   │   ├─> При достижении:
   │   │   ├─> MoveToGridPosition() - обновление в сетке
   │   │   └─> ReleaseReservation()
   │   └─> Следующая клетка
   ├─> Пересчет пути каждые 1 сек
   ├─> Если достиг цели → State = Attacking
   └─> Если путь заблокирован → State = Waiting

4. Attacking
   ├─> IsInAttackRange() - проверка для всех клеток обоих юнитов
   ├─> CanAttackTarget() - cooldown, валидность
   ├─> Attack(target)
   │   ├─> target.TakeDamage(damage)
   │   ├─> CombatSystem.OnAttackPerformed event
   │   └─> Эффекты, анимация
   ├─> Если цель вышла из радиуса → State = MovingToTarget
   └─> Если цель умерла → State = SearchingTarget

5. OnDeath()
   ├─> GridManager.FreeArea(this)
   ├─> TargetFindingSystem.UnregisterUnit(this)
   ├─> CombatSystem.OnUnitDied event
   └─> Destroy(gameObject)
```

---

## 🛡️ Collision Avoidance (Избежание столкновений)

### Reservation System

**Проблема**: Два юнита идут к одной клетке

**Решение**: Клетки резервируются перед занятием

```
Клетка имеет:
- ReservedBy: Unit
- ReservationTime: float

Юнит перед движением:
1. Проверяет IsAvailable(nextNode)
2. Если свободна → TryReserve(this)
3. Двигается к клетке
4. При достижении:
   - Occupy(this) - занимает
   - ReleaseReservation() - снимает резерв
```

**Для юнитов >1x1**:
```csharp
// Резервируем ВСЕ клетки юнита
var cells = unit.Size.GetAllOccupiedCells(targetPosition);
foreach (var cell in cells) {
    GridManager.GetNode(cell).TryReserve(unit);
}
```

**Timeout**:
- Если клетка заблокирована > 2 сек → пересчет пути
- Резервация автоматически освобождается при занятии

---

## 🎯 Ключевые алгоритмы

### A* для юнитов разных размеров

```csharp
bool CanUnitFitAt(Unit unit, GridNode node, GridNode goal) {
    Vector2Int pos = node.GridPosition;
    UnitSize size = unit.Size;
    
    // Проверяем ВСЕ клетки
    for (int x = 0; x < size.Width; x++) {
        for (int y = 0; y < size.Height; y++) {
            GridNode checkNode = GetNode(pos.x + x, pos.y + y);
            
            // Клетка не существует
            if (checkNode == null) return false;
            
            // Целевая клетка (может быть занята врагом)
            if (checkNode == goal) continue;
            
            // Проверка доступности
            if (!checkNode.IsAvailable(unit)) return false;
        }
    }
    
    return true;
}
```

### Attack Range с Chebyshev Distance

```csharp
bool IsInAttackRange(Unit target) {
    var myCells = GetOccupiedCells();
    var targetCells = target.GetOccupiedCells();
    
    // Проверяем ВСЕ пары клеток
    foreach (var myCell in myCells) {
        foreach (var targetCell in targetCells) {
            // Chebyshev distance (диагональ = 1)
            int dx = Abs(myCell.x - targetCell.x);
            int dy = Abs(myCell.y - targetCell.y);
            int distance = Max(dx, dy);
            
            if (distance <= AttackRange) 
                return true;
        }
    }
    
    return false;
}
```

**Почему Chebyshev?**
- Разрешает диагональное движение
- Distance = 1 для всех 8 соседей
- Проще для grid-based систем

---

## ⚡ Оптимизации

### Текущие:
1. ✓ **Reservation System** - избежание столкновений
2. ✓ **Кэш юнитов по командам** - быстрый поиск врагов
3. ✓ **Max Iterations в A*** - защита от зависания
4. ✓ **Throttling пересчета пути** - раз в 1 сек
5. ✓ **HashSet для ClosedList** - O(1) проверка
6. ✓ **Dictionary для Grid** - быстрый доступ к клеткам

### Возможные улучшения:
1. **Job System** - async pathfinding в фоновом потоке
2. **Spatial Hashing** - разбить сетку на чанки 10x10
3. **Flow Fields** - для групп юнитов к одной цели
4. **Jump Point Search** - оптимизация A* для больших карт
5. **Path Smoothing** - сглаживание пути после A*
6. **Hierarchical Pathfinding** - для очень больших карт

---

## 📐 Координатные системы

### Grid Coordinates (int)
```
(0, 0) в левом нижнем углу
(gridWidth-1, gridHeight-1) в правом верхнем
```

### World Coordinates (float)
```
worldPos.x = gridOrigin.x + gridX * cellSize + cellSize/2
worldPos.z = gridOrigin.y + gridY * cellSize + cellSize/2
worldPos.y = 0 (игнорируется)
```

### Unit Position (Vector2Int)
```
Базовая позиция = bottom-left corner
Для 2x2 юнита на (3,3):
  Занимает: (3,3), (4,3), (3,4), (4,4)
  WorldPos: center = (3.5, 3.5) * cellSize
```

---

## 🧪 Тестирование

### Unit Tests (концепты)
```csharp
// Grid
[Test] GetNode_ValidPosition_ReturnsNode()
[Test] GetNode_InvalidPosition_ReturnsNull()
[Test] IsAreaClear_AllClearFor2x2_ReturnsTrue()

// Pathfinding
[Test] FindPath_SimpleCase_ReturnsCorrectPath()
[Test] FindPath_WithObstacles_FindsAlternatePath()
[Test] FindPath_NoPath_ReturnsNull()
[Test] FindPath_For2x2Unit_ChecksAllCells()

// Unit
[Test] IsInAttackRange_AdjacentEnemy_ReturnsTrue()
[Test] IsInAttackRange_DiagonalEnemy_ReturnsTrue()
[Test] IsInAttackRange_FarEnemy_ReturnsFalse()
```

---

## 🔧 Настройка под проект

### Изменить размеры сетки
```csharp
// GridManager
gridWidth = 20;
gridHeight = 20;
cellSize = 0.5f; // для более детальной сетки
```

### Добавить новый размер юнита
```csharp
// UnitSize.cs
public static UnitSize Size3X3 => new UnitSize(3, 3);
```

### Изменить поведение AI
```csharp
// UnitMovementController.cs
pathRecalculationInterval = 0.5f; // чаще пересчитывать
targetSearchInterval = 0.2f; // быстрее реагировать
```

### Добавить препятствия
```csharp
// GridNode
IsWalkable = false; // непроходимая клетка

// Или программно
GridManager.Instance.GetNode(x, y).IsWalkable = false;
```

---

## 📚 Расширения

### Добавить анимацию
```csharp
public class AnimatedUnit : Unit {
    Animator animator;
    
    protected override void OnAttackPerformed(Unit target) {
        animator.SetTrigger("Attack");
    }
}
```

### Добавить способности
```csharp
public class Ability {
    public virtual void Use(Unit caster, Unit target) { }
}

public class FireballAbility : Ability {
    public override void Use(Unit caster, Unit target) {
        // AOE damage
        var enemies = TargetFindingSystem.FindEnemiesInRadius(target, 2f);
        foreach (var enemy in enemies) {
            enemy.TakeDamage(damage);
        }
    }
}
```

### Добавить UI
```csharp
CombatSystem.OnDamageTaken += (source, target, damage) => {
    FloatingText.Show(target.transform.position, $"-{damage}");
};
```

---

## 🎓 Принципы дизайна

### SOLID Principles
- **S** - каждый класс имеет одну ответственность
- **O** - легко расширяется (Unit → CustomUnit)
- **L** - Liskov substitution для Unit
- **I** - нет больших интерфейсов (можно добавить)
- **D** - зависимости от абстракций (Singleton)

### Design Patterns
- **Singleton** - для систем (Grid, Pathfinding, TargetFinding)
- **State Machine** - для AI поведения
- **Observer** - события в CombatSystem
- **Component** - MonoBehaviour architecture

---

**Архитектура готова к production! 🚀**

