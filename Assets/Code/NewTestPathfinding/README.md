# A* Pathfinding System для Unity

Полноценная система поиска пути с поддержкой юнитов разных размеров (1x1, 2x1, 2x2) и автоматической боевой системой.

## 📋 Компоненты системы

### 1. Core Classes

#### GridNode (MonoBehaviour)
- Представляет одну клетку сетки
- Хранит данные A* (GCost, HCost, Parent)
- Управляет занятостью и резервацией
- Визуализация для отладки

#### GridManager (Singleton)
- Создает и управляет сеткой
- Предоставляет доступ к клеткам
- Управляет занятостью областей для юнитов разных размеров
- Конвертация между мировыми и сеточными координатами

### 2. Pathfinding

#### PathfindingSystem (Singleton)
- Реализация A* алгоритма
- Поддержка юнитов 1x1, 2x1, 2x2
- Diagonal movement support
- Оптимизации и защита от бесконечных циклов

### 3. Units

#### Unit (MonoBehaviour)
- Базовый класс юнита
- Поддержка разных размеров (UnitSize)
- HP, damage, attack range
- Команды (Team): Player, Enemy, Neutral

#### UnitMovementController (MonoBehaviour)
- State machine для управления поведением
- States: Idle, SearchingTarget, MovingToTarget, Attacking, Waiting
- Автоматический поиск и преследование врагов
- Collision avoidance через reservation system

### 4. Combat

#### TargetFindingSystem (Singleton)
- Поиск ближайших врагов
- Кэширование юнитов по командам
- Проверка attack range с учетом размеров
- Поиск оптимальной позиции для атаки

#### CombatSystem (Singleton)
- Управление атаками и damage
- События для эффектов
- Статистика боя

## 🚀 Быстрый старт

### Шаг 1: Настройка сцены

1. Создайте пустой GameObject и добавьте компонент `GridManager`
2. Настройте параметры:
   - Grid Width: 10
   - Grid Height: 10
   - Cell Size: 1
   - Auto Generate On Start: ✓

3. Создайте GameObject для систем и добавьте:
   - `PathfindingSystem`
   - `TargetFindingSystem`
   - `CombatSystem` (опционально)

### Шаг 2: Создание юнита

1. Создайте GameObject (Cube/Capsule)
2. Добавьте компоненты:
   - `Unit`
   - `UnitMovementController`

3. Настройте Unit:
   - Unit Size: выберите 1x1, 2x1 или 2x2
   - Team: Player или Enemy
   - Movement Speed: 3
   - Attack Range: 1
   - Damage: 10
   - Max Health: 100

### Шаг 3: Запуск

1. Создайте несколько юнитов разных команд
2. Расставьте их на сцене
3. Нажмите Play

Юниты автоматически:
- Найдут ближайшего врага
- Построят путь с учетом своего размера
- Будут преследовать врага
- Атакуют при достижении attack range

## 🎮 Размеры юнитов

### 1x1 (стандартный)
```csharp
unit.Size = UnitSize.Size_1x1;
```
Занимает 1 клетку

### 2x1 (вертикальный)
```csharp
unit.Size = UnitSize.Size_2x1;
```
Занимает 2 клетки по вертикали:
```
[X]
[X]
```

### 2x2 (большой)
```csharp
unit.Size = UnitSize.Size_2x2;
```
Занимает 4 клетки:
```
[X][X]
[X][X]
```

## ⚔️ Attack Range

Юниты могут атаковать по диагонали, если расстояние ≤ 1 клетка (Chebyshev distance).

Пример:
```
[A] [ ] [E]
[ ] [E] [ ]
```
Юнит A может атаковать обоих врагов E (diagonal distance = 1)

## 🔧 Основные настройки

### GridManager
- `gridWidth/Height` - размер сетки
- `cellSize` - размер клетки в мировых координатах
- `autoGenerateOnStart` - автосоздание при старте

### PathfindingSystem
- `maxIterations` - защита от зависания (1000)
- `useDiagonalMovement` - разрешить диагональное движение
- `diagonalCost` - стоимость диагонали (14 ≈ √2 * 10)

### UnitMovementController
- `pathRecalculationInterval` - как часто пересчитывать путь (1s)
- `targetSearchInterval` - как часто искать новую цель (0.5s)
- `blockedWaitTime` - сколько ждать, если путь заблокирован (2s)

## 🐛 Отладка

### Визуализация
- GridNode показывает цвет в зависимости от состояния:
  - Белый - свободная клетка
  - Красный - непроходимая
  - Желтый - занята юнитом
  - Cyan - зарезервирована

- PathfindingSystem показывает найденный путь зеленым
- Unit показывает занимаемые клетки и линию к цели

### Debug режим
Включите флаги в компонентах:
- `showDebugGizmos` в GridNode
- `showDebugPath` в PathfindingSystem
- `showDebugInfo` в UnitMovementController

## 📝 Расширение системы

### Добавить новый размер юнита
```csharp
public static UnitSize Size_3x2 => new UnitSize(2, 3);
```

### Создать кастомного юнита
```csharp
public class Warrior : Unit
{
    protected override void OnAttackPerformed(Unit target)
    {
        // Спецэффекты атаки
        PlayAttackAnimation();
        SpawnSlashEffect();
    }
}
```

### Подписка на события
```csharp
CombatSystem.OnAttackPerformed += (attacker, target, damage) => {
    Debug.Log($"{attacker.name} hit {target.name}!");
};

CombatSystem.OnUnitDied += (unit) => {
    Debug.Log($"{unit.name} died!");
    SpawnLoot(unit.transform.position);
};
```

## ⚡ Оптимизация

Система уже включает:
- ✓ Reservation system для избежания столкновений
- ✓ Кэширование юнитов по командам
- ✓ Защита от бесконечных циклов в A*
- ✓ Ограничение пересчета пути по времени

Для больших карт можно добавить:
- Job System для async pathfinding
- Spatial hashing для быстрого поиска соседей
- Flow fields для групп юнитов

## 📚 Примеры использования

### Программное создание юнита
```csharp
GameObject unitObj = new GameObject("MyUnit");
Unit unit = unitObj.AddComponent<Unit>();
unitObj.AddComponent<UnitMovementController>();

unit.Size = UnitSize.Size_2x2;
unit.Team = Team.Player;
unit.CurrentGridPosition = new Vector2Int(5, 5);
```

### Ручное управление путем
```csharp
List<GridNode> path = PathfindingSystem.Instance.FindPath(
    myUnit, 
    startPos, 
    goalPos
);

if (path != null)
{
    myUnit.CurrentPath = path;
}
```

### Проверка атаки вручную
```csharp
if (unit.IsInAttackRange(enemy))
{
    unit.Attack(enemy);
}
```

## 📊 Архитектура

```
GridManager (Singleton)
    ├── GridNode[,] grid
    └── Управление занятостью

PathfindingSystem (Singleton)
    └── A* algorithm + размеры юнитов

TargetFindingSystem (Singleton)
    └── Поиск врагов + кэш по командам

Unit (MonoBehaviour)
    ├── UnitSize
    ├── Team
    ├── HP/Damage/Range
    └── Attack logic

UnitMovementController (MonoBehaviour)
    ├── State Machine
    ├── Path following
    └── Auto-combat behavior

CombatSystem (Singleton)
    └── События + эффекты
```

## 🎯 Особенности реализации

### Collision Avoidance
- Reservation system для предотвращения столкновений
- Клетки резервируются перед началом движения
- Если клетка занята - юнит ждет или ищет новый путь

### Размеры юнитов в A*
- При поиске пути проверяются ВСЕ клетки, которые займет юнит
- Для 2x2 юнита проверяется 4 клетки на каждом шаге
- Позиция юнита = bottom-left corner

### Attack Range с размерами
- Проверяется расстояние между ВСЕМИ парами клеток
- Используется Chebyshev distance (диагональ = 1)
- Юнит 2x2 может атаковать с большей эффективностью

---

## 💡 Tips

1. Для тестирования создайте простую сцену с GridManager и несколькими юнитами разных команд
2. Используйте Gizmos для визуализации пути и состояний
3. Настройте цвета клеток в GridNode для лучшей читаемости
4. Для больших карт уменьшите pathRecalculationInterval
5. Экспериментируйте с размерами юнитов!

**Enjoy pathfinding! 🎮**

