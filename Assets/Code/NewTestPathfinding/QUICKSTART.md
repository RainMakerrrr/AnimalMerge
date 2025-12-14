# 🚀 Быстрый старт - A* Pathfinding System

## Вариант 1: Автоматическая настройка (самый быстрый)

### Шаг 1
1. Создайте пустую сцену
2. Создайте пустой GameObject
3. Добавьте компонент `SceneSetupHelper`

### Шаг 2
4. В Inspector настройте параметры:
   - Grid Width: 10
   - Grid Height: 10
   - Player Units Count: 3
   - Enemy Units Count: 3
   - Randomize Unit Sizes: ✓

### Шаг 3
5. В режиме Play нажмите кнопку **"Setup Scene"** в левом верхнем углу
6. Готово! Юниты начнут искать врагов и сражаться

---

## Вариант 2: Ручная настройка

### Системы (создать по одному GameObject для каждой)

1. **GridManager** (обязательно)
   ```
   GameObject -> Add Component -> GridManager
   Grid Width: 10
   Grid Height: 10
   Cell Size: 1
   Auto Generate On Start: ✓
   ```

2. **PathfindingSystem** (обязательно)
   ```
   GameObject -> Add Component -> PathfindingSystem
   ```

3. **TargetFindingSystem** (обязательно)
   ```
   GameObject -> Add Component -> TargetFindingSystem
   ```

4. **CombatSystem** (опционально)
   ```
   GameObject -> Add Component -> CombatSystem
   ```

### Юниты

1. Создайте Capsule или Cube
2. Добавьте компоненты:
   - `Unit`
   - `UnitMovementController`

3. Настройте Unit:
   - **Unit Size**: 1x1 / 2x1 / 2x2
   - **Team**: Player / Enemy
   - **Movement Speed**: 3
   - **Attack Range**: 1
   - **Damage**: 10
   - **Max Health**: 100

4. Установите цвет (опционально):
   - Синий для Player
   - Красный для Enemy

5. Создайте несколько юнитов обеих команд

---

## 🎮 Тестирование

### Проверка работы:
1. Нажмите Play
2. Юниты должны:
   - ✓ Найти ближайшего врага
   - ✓ Построить путь к нему
   - ✓ Двигаться по пути
   - ✓ Атаковать при достижении

### Debug информация:
- В Scene view включите Gizmos
- Увидите:
  - Сетку (серые линии)
  - Занятые клетки (желтые)
  - Пути юнитов (зеленые линии)
  - Текущее состояние (над юнитом)

---

## 🎯 Быстрые тесты размеров

### Тест 1x1 юнитов
```
Создайте 2 юнита Size 1x1
Player: (1, 1)
Enemy: (8, 8)
```
Должны двигаться по диагонали и сражаться

### Тест 2x2 юнитов
```
Создайте юнита Size 2x2
Player: (1, 1)  
Enemy: (7, 7)
```
Большой юнит должен обходить препятствия

### Тест смешанных размеров
```
Player 1x1: (1, 1)
Player 2x2: (1, 3)
Enemy 2x1: (8, 5)
```

---

## ⚡ Горячие клавиши (в Play mode с SceneSetupHelper)

- **Setup Scene** - создать тестовую сцену
- **Clear All Units** - удалить всех юнитов
- **Clear Systems** - удалить все системы

---

## 🐛 Troubleshooting

### Юниты не двигаются
- ✓ Проверьте, что GridManager создан
- ✓ Auto Generate On Start включен
- ✓ В Console нет ошибок

### Юниты проходят друг сквозь друга
- ✓ Проверьте, что занимают клетки (желтый цвет в Gizmos)
- ✓ Reservation system работает

### Путь не строится
- ✓ Включите Show Debug Path в PathfindingSystem
- ✓ Проверьте, что клетки walkable (белые)
- ✓ Max Iterations достаточно (1000)

### Юниты не атакуют
- ✓ Разные команды (Player vs Enemy)
- ✓ Attack Range = 1
- ✓ Юниты в радиусе атаки

---

## 📝 Примеры кода

### Создать юнита программно
```csharp
GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
Unit unit = unitObj.AddComponent<Unit>();
unitObj.AddComponent<UnitMovementController>();

// Настроить через reflection (или сделать поля public)
// ... см. SceneSetupHelper.cs
```

### Найти путь вручную
```csharp
List<GridNode> path = PathfindingSystem.Instance.FindPath(
    myUnit,
    new Vector2Int(0, 0),
    new Vector2Int(9, 9)
);
```

### Проверить attack range
```csharp
if (unit1.IsInAttackRange(unit2))
{
    unit1.Attack(unit2);
}
```

---

## 🎨 Визуальные настройки

В GridNode можно настроить цвета:
- **Walkable Color**: белый (свободная клетка)
- **Unwalkable Color**: красный (стена)
- **Occupied Color**: желтый (занята юнитом)
- **Reserved Color**: cyan (зарезервирована)

---

## 💡 Советы

1. Начните с малой сетки (5x5)
2. Используйте только 1x1 юнитов для первого теста
3. Включите все Debug флаги
4. Смотрите Console для логов
5. Используйте Scene view для визуализации

**Happy coding! 🎮**

