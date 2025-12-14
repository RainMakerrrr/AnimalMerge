# 📚 A* Pathfinding System - Полная документация

## 🎯 Быстрая навигация

### Для начинающих
1. **[QUICKSTART.md](QUICKSTART.md)** - Начни отсюда! Самый быстрый способ запустить систему
2. **[README.md](README.md)** - Подробная документация с примерами

### Для разработчиков
3. **[ARCHITECTURE.md](ARCHITECTURE.md)** - Глубокое погружение в архитектуру
4. Исходный код с комментариями

---

## 📁 Структура файлов

### Core Systems (Основные системы)

#### Grid System
- **GridNode.cs** - MonoBehaviour для каждой клетки сетки
  - Данные A* (GCost, HCost, Parent)
  - Управление занятостью и резервацией
  - Визуализация для отладки

- **GridManager.cs** - Singleton для управления сеткой
  - Создание и управление GridNode[,]
  - Конвертация координат
  - Управление областями для юнитов разных размеров

#### Pathfinding System
- **PathfindingSystem.cs** - Singleton для A* алгоритма
  - Реализация A* с учетом размеров юнитов
  - Diagonal movement support
  - Оптимизации и защита от зависаний

#### Unit System
- **Unit.cs** - Базовый класс юнита
  - Поддержка размеров 1x1, 2x1, 2x2
  - HP, damage, attack range
  - Команды (Team): Player, Enemy, Neutral

- **UnitMovementController.cs** - Контроллер движения
  - State Machine: Idle, SearchingTarget, MovingToTarget, Attacking, Waiting
  - Автоматическое поведение AI
  - Collision avoidance через reservation system

#### Combat System
- **TargetFindingSystem.cs** - Singleton для поиска врагов
  - Кэширование юнитов по командам
  - Поиск ближайших врагов
  - Attack range проверка с учетом размеров

- **CombatSystem.cs** - Singleton для боевой системы
  - Управление атаками
  - События для эффектов
  - Статистика боя

### Data Structures (Структуры данных)

- **UnitSize.cs** - Структура размера юнита
  - Width, Height
  - Predefined: Size1X1, Size2X1, Size2X2
  - Методы для работы с занимаемыми клетками

- **Team.cs** - Enum для команд
  - Player, Enemy, Neutral

### Utility (Утилиты)

- **SceneSetupHelper.cs** - Автоматическая настройка сцены
  - Создание Grid, систем и тестовых юнитов
  - UI кнопки для быстрой настройки
  - Context menu команды

---

## 🚀 Что читать в зависимости от задачи

### "Хочу быстро протестировать систему"
→ **[QUICKSTART.md](QUICKSTART.md)** → Вариант 1 (автоматическая настройка)

### "Хочу понять, как это работает"
→ **[README.md](README.md)** → Раздел "Архитектура" и "Основные настройки"

### "Хочу расширить систему"
→ **[ARCHITECTURE.md](ARCHITECTURE.md)** → Разделы "Расширения" и "Настройка под проект"

### "Хочу создать своего юнита"
→ **[README.md](README.md)** → Раздел "Расширение системы" → "Создать кастомного юнита"

### "Хочу добавить новый размер юнита"
→ **[README.md](README.md)** → Раздел "Размеры юнитов"
→ **UnitSize.cs** → Добавить новый static property

### "Хочу изменить AI поведение"
→ **UnitMovementController.cs** → Изучить State Machine
→ **[ARCHITECTURE.md](ARCHITECTURE.md)** → Раздел "Movement Layer"

### "Хочу понять A* для разных размеров"
→ **[ARCHITECTURE.md](ARCHITECTURE.md)** → Раздел "Ключевые алгоритмы"
→ **PathfindingSystem.cs** → Метод `CanUnitFitAt()`

### "Хочу настроить attack range"
→ **[ARCHITECTURE.md](ARCHITECTURE.md)** → "Attack Range с Chebyshev Distance"
→ **Unit.cs** → Метод `IsInAttackRange()`

---

## 📊 Зависимости компонентов

```
SceneSetupHelper (опционально)
    ↓
GridManager (обязательно) ─┐
    ↓                      │
PathfindingSystem ────────┤
    ↓                      │
TargetFindingSystem       │
    ↓                      │
Unit + UnitMovementController
    ↓
CombatSystem (опционально)
```

---

## 🎓 Порядок изучения для новичков

### День 1: Основы
1. Прочитать **QUICKSTART.md**
2. Запустить автоматическую настройку (SceneSetupHelper)
3. Поиграть с параметрами в Inspector
4. Посмотреть на Gizmos визуализацию

### День 2: Понимание
1. Прочитать **README.md** полностью
2. Создать юнитов вручную
3. Протестировать разные размеры (1x1, 2x1, 2x2)
4. Изучить Debug режим

### День 3: Углубление
1. Прочитать **ARCHITECTURE.md**
2. Изучить исходный код GridManager и GridNode
3. Понять, как работает A* для разных размеров
4. Посмотреть State Machine в UnitMovementController

### День 4: Расширение
1. Создать кастомного юнита с анимацией
2. Добавить новый размер (например, 3x1)
3. Подписаться на события CombatSystem
4. Добавить UI для HP

---

## 🔍 Поиск по функциям

### "Как найти путь?"
```csharp
PathfindingSystem.Instance.FindPath(unit, start, goal)
```
→ **PathfindingSystem.cs** (строка ~50)

### "Как проверить attack range?"
```csharp
unit.IsInAttackRange(target)
```
→ **Unit.cs** метод `IsInAttackRange()` (строка ~120)

### "Как найти ближайшего врага?"
```csharp
TargetFindingSystem.Instance.FindNearestEnemy(unit)
```
→ **TargetFindingSystem.cs** (строка ~60)

### "Как занять клетки на сетке?"
```csharp
GridManager.Instance.OccupyArea(unit, position)
```
→ **GridManager.cs** (строка ~180)

### "Как проверить, свободна ли область?"
```csharp
GridManager.Instance.IsAreaClear(x, y, width, height, ignoreUnit)
```
→ **GridManager.cs** (строка ~160)

---

## 🎨 Визуальная шпаргалка

### Цвета клеток в Scene view
- 🤍 **Белый** - Свободная клетка (walkable)
- 🔴 **Красный** - Непроходимая клетка
- 💛 **Желтый** - Занята юнитом
- 💙 **Cyan** - Зарезервирована

### Цвета Gizmos
- 🟢 **Зеленый** - Путь юнита (PathfindingSystem)
- 🔵 **Синий** - Юниты Player команды
- 🔴 **Красный** - Юниты Enemy команды
- 💛 **Желтый** - Радиус атаки / Линия к цели

### Debug информация над юнитом
```
State: MovingToTarget
```

---

## 📝 Чек-лист для настройки

### Минимальная настройка (3 шага)
- [ ] Создать GameObject с GridManager
- [ ] Создать GameObject с PathfindingSystem
- [ ] Создать GameObject с TargetFindingSystem
- [ ] Создать 2+ юнитов разных команд
- [ ] Нажать Play

### Рекомендуемая настройка (5 шагов)
- [ ] Минимальная настройка ✓
- [ ] Создать GameObject с CombatSystem
- [ ] Включить Debug режимы (Show Debug Gizmos, Show Debug Path)
- [ ] Настроить цвета в GridNode
- [ ] Создать юнитов разных размеров (1x1, 2x1, 2x2)

### Продвинутая настройка (7+ шагов)
- [ ] Рекомендуемая настройка ✓
- [ ] Создать prefab юнита
- [ ] Добавить визуальные эффекты (hit, death)
- [ ] Настроить анимации
- [ ] Добавить UI (HP bars)
- [ ] Подписаться на события CombatSystem
- [ ] Создать кастомные юниты с особыми способностями

---

## 🐛 Troubleshooting (Быстрый поиск)

### Проблема: "Юниты не двигаются"
→ **QUICKSTART.md** → Раздел "Troubleshooting"

### Проблема: "Путь не строится"
→ **README.md** → Раздел "Отладка"
→ Включить `showDebugPath` в PathfindingSystem

### Проблема: "Юниты проходят друг сквозь друга"
→ **ARCHITECTURE.md** → Раздел "Collision Avoidance"
→ Проверить Reservation System

### Проблема: "Большой юнит застревает"
→ **ARCHITECTURE.md** → "A* для юнитов разных размеров"
→ Проверить `CanUnitFitAt()` в PathfindingSystem

### Проблема: "Attack range не работает"
→ **ARCHITECTURE.md** → "Attack Range с Chebyshev Distance"
→ Проверить `IsInAttackRange()` в Unit.cs

---

## 💡 Полезные ссылки

### Внутренние ссылки
- [Быстрый старт](QUICKSTART.md)
- [Полная документация](README.md)
- [Архитектура](ARCHITECTURE.md)

### Концепты для изучения
- A* Pathfinding Algorithm
- State Machine Pattern
- Singleton Pattern
- Grid-based Movement
- Chebyshev Distance
- Manhattan Distance

---

## 📞 Структура поддержки

### Вопросы по настройке
→ Читай **QUICKSTART.md**

### Вопросы по использованию
→ Читай **README.md**

### Вопросы по архитектуре
→ Читай **ARCHITECTURE.md**

### Вопросы по коду
→ Все классы имеют подробные комментарии

---

## ✨ Фичи системы

### Реализовано ✓
- [x] A* pathfinding с учетом размеров юнитов
- [x] Поддержка юнитов 1x1, 2x1, 2x2
- [x] Diagonal movement
- [x] Collision avoidance (Reservation System)
- [x] Автоматический поиск и атака врагов
- [x] State Machine для AI
- [x] Attack range с диагональной атакой
- [x] Кэширование врагов по командам
- [x] Debug визуализация (Gizmos)
- [x] HP система
- [x] События для эффектов

### Можно добавить в будущем
- [ ] Async pathfinding (Job System)
- [ ] Flow fields для больших групп
- [ ] Jump Point Search оптимизация
- [ ] Hierarchical pathfinding
- [ ] Формации юнитов
- [ ] Patrol system
- [ ] Abilities система
- [ ] Анимации
- [ ] Sound effects
- [ ] Particle effects

---

## 🎯 Итог

### Начинающий
**Начни с:** [QUICKSTART.md](QUICKSTART.md) → SceneSetupHelper → Play

### Разработчик
**Начни с:** [README.md](README.md) → Создай юнитов → Изучи код

### Архитектор
**Начни с:** [ARCHITECTURE.md](ARCHITECTURE.md) → Изучи паттерны → Расширяй

---

**Система готова к использованию! 🚀**

Выбери свой путь обучения и начинай творить!

