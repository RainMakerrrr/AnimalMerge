# Спецификация системы мерджа и скиллов (Merge & Skills System)

**Версия:** 1.0
**Дата:** 2026-02-02
**Статус:** В разработке

---

## Содержание

1. [Обзор системы](#1-обзор-системы)
2. [Архитектура системы мерджа](#2-архитектура-системы-мерджа)
3. [Система скиллов (Abilities)](#3-система-скиллов-abilities)
4. [Животные и их характеристики](#4-животные-и-их-характеристики)
5. [Сравнение с дизайн-документом](#5-сравнение-с-дизайн-документом)
6. [Критические расхождения](#6-критические-расхождения)
7. [План приведения к соответствию](#7-план-приведения-к-соответствию)

---

## 1. Обзор системы

### 1.1. Основная концепция

Система мерджа позволяет объединять животных через drag-and-drop, передавая скиллы и улучшая характеристики целевого животного.

**Из концепт-дока 2.0:**
> "Суть: мерджить доступных, радномно выпавших, животных в одно/двух/трёх, совмещая их абилки между собой"

### 1.2. Ключевые принципы (по дизайн-доку)

- Животное, которое "принимает в себя", получает скиллы
- Владелец скилла использует его всегда (100%)
- При передаче скилл срабатывает с вероятностью, которая увеличивается с количеством мерджей
- При мердже одинаковых животных: `(HP1 + HP2) * 0.75`, скиллы не бафаются
- Возможность безболезненно отменять мерджи

---

## 2. Архитектура системы мерджа

### 2.1. Интерфейс IMergeSkill

**Расположение:** `Assets/Code/Animals/Merge/MergeSkills/IMergeSkill.cs`

```csharp
public interface IMergeSkill
{
    AnimalType AnimalType { get; }
    void Merge(AnimalFacade animal);                    // Применяется к "принимающему"
    void Merge(AnimalFacade animal, AnimalFacade other); // Каскадирование скиллов
}
```

### 2.2. Реализованные MergeSkills

#### 2.2.1. ElephantMergeSkill
- **Тип:** Upgrade
- **Эффект:** Умножает HP на `1.5f`
- **Метод:** `animal.UpgradeHealth(_multiplier)`
- **Каскадирование:** Да, применяет скиллы второго животного

```csharp
public class ElephantMergeSkill : IMergeSkill
{
    private readonly float _multiplier = 1.5f; // Configurable via Facade

    public void Merge(AnimalFacade animal)
    {
        animal.UpgradeHealth(_multiplier);
    }

    public void Merge(AnimalFacade animal, AnimalFacade other)
    {
        Merge(animal);
        // Cascade other animal's merge skills
        foreach (var skill in other.MergeSkills)
            skill.Merge(animal);
    }
}
```

#### 2.2.2. CheetahMergeSkill
- **Тип:** Upgrade
- **Эффект:** Умножает Speed на `2` (int)
- **Метод:** `animal.UpgradeSpeed(_multiplier)`

#### 2.2.3. DeerMergeSkill
- **Тип:** Upgrade
- **Эффект:** Умножает Damage на `1.5f`
- **Метод:** `animal.UpgradeDamage(_multiplier)`

#### 2.2.4. HedgehogMergeSkill
- **Тип:** Ability Grant
- **Эффект:** Добавляет CounterAttack ability
- **Метод:** `animal.AddAbility(new CounterAttack(...))`
- **Хранение:** `animal.MergeSkills.Add(this)`

#### 2.2.5. FoxMergeSkill
- **Тип:** Ability Grant
- **Эффект:** Добавляет Dodge ability (50% probability)
- **Метод:** `animal.AddAbility(new Dodge(probability: 0.5f, ...))`
- **Хранение:** `animal.MergeSkills.Add(this)`

### 2.3. MergeTarget - Механизм принятия мерджа

**Расположение:** `Assets/Code/Animals/Merge/MergeTarget.cs`

```csharp
public class MergeTarget : MonoBehaviour, IRaycastable
{
    public event Action<List<AnimalType>> Merge;

    public void Accept(AnimalFacade animal)
    {
        // 1. Собирает типы всех смердженных животных
        List<AnimalType> mergedTypes = CollectMergedTypes(animal);

        // 2. Применяет MergeSkill
        animal.MergeSkill.Merge(targetAnimal, animal);

        // 3. Деактивирует смердженное животное
        animal.gameObject.SetActive(false);

        // 4. Вызывает событие для визуала
        Merge?.Invoke(mergedTypes);
    }
}
```

### 2.4. MergeView - Визуальные эффекты

**Расположение:** `Assets/Code/Animals/Merge/MergeView.cs`

- Подписывается на событие `MergeTarget.Merge`
- Применяет визуальные атрибуты (VisualMergeAttribute) для каждого типа животного
- Кэширует атрибуты в Dictionary для O(1) доступа

### 2.5. VisualMergeAttribute - Визуальные атрибуты

**Базовый класс:** `Assets/Code/Animals/Merge/MergeAttributes/VisualMergeAttribute.cs`

```csharp
public class VisualMergeAttribute : MonoBehaviour
{
    public virtual void Apply() => gameObject.SetActive(true);
}
```

**ElephantMergeAttribute:**
- Увеличивает scale на `1.5f`
- `transform.root.localScale *= ScaleFactor`

---

## 3. Система скиллов (Abilities)

### 3.1. Интерфейс IAbility

**Расположение:** `Assets/Code/Abilities/IAbility.cs`

```csharp
public interface IAbility
{
    bool IsBlockingDamage { get; }  // Может ли блокировать урон
    int Priority { get; }            // Порядок выполнения (меньше = раньше)
    bool CanUse { get; }             // Доступна ли сейчас
    void Apply();                    // Применить эффект
}
```

### 3.2. Реализованные Abilities

#### 3.2.1. CounterAttack (Контратака)

**Файл:** `Assets/Code/Abilities/CounterAttack.cs`

**Характеристики:**
- **Priority:** `0` (выполняется первым)
- **IsBlockingDamage:** `false` (не блокирует урон)
- **Эффект:** Наносит ответный урон атакующему

**Логика:**
```csharp
public void Apply()
{
    // 1. Проигрывает анимацию контратаки
    _animalAnimation.PlayCounterAttackAnimation();

    // 2. Наносит урон атакующему
    _attacker.TakeDamage(_animalAttack.Damage);
}
```

**Связь с животными:**
- Владелец: Hedgehog (ёж)
- Передаётся через: HedgehogMergeSkill

#### 3.2.2. Dodge (Уклонение)

**Файл:** `Assets/Code/Abilities/Dodge.cs`

**Характеристики:**
- **Priority:** `1`
- **IsBlockingDamage:** `true` (БЛОКИРУЕТ урон)
- **Вероятность:** Настраиваемая (50% для Hedgehog, 20% для Fox)

**Логика:**
```csharp
public void Apply()
{
    // 1. Проверяет вероятность
    if (Random.value > _probability) return;

    // 2. Отключает коллайдеры
    _colliders.ForEach(c => c.enabled = false);

    // 3. Сдвигается в сторону (ITransformable.Shift())
    _transformable.Shift();
}
```

**Связь с животными:**
- Владелец: Fox (лиса)
- Передаётся через: FoxMergeSkill
- Hedgehog тоже имеет Dodge (50% вероятность)

#### 3.2.3. MultipleCharacters (Множественные персонажи)

**Файл:** `Assets/Code/Abilities/MultipleCharacters.cs`

**Характеристики:**
- **Priority:** `2`
- **IsBlockingDamage:** `false`
- **Эффект:** Создаёт дополнительных персонажей того же типа

**Логика:**
```csharp
public void Apply()
{
    // 1. Находит свободные клетки вокруг
    List<GridCell> freeCells = FindFreeCellsAround(_currentPosition);

    // 2. Создаёт копии (для курицы = 3)
    for (int i = 0; i < _charactersAmount; i++)
    {
        AnimalFacade clone = _animalFactory.Create(_animalType);
        PlaceOnGrid(clone, freeCells[i]);
    }

    // 3. Синхронизирует движение между всеми экземплярами
    SynchronizeMovement();
}
```

**Связь с животными:**
- Владелец: Chicken (курица)
- Создаёт: 3 дополнительных копии

### 3.3. Применение Abilities в бою

**В AnimalHealth.cs:**

```csharp
public void TakeDamage(float damage)
{
    // 1. Применяем abilities по приоритету
    foreach (var ability in _abilities.OrderBy(a => a.Priority))
    {
        if (ability.CanUse)
        {
            ability.Apply();

            // 2. Если ability блокирует урон, выходим
            if (ability.IsBlockingDamage)
            {
                ReenableColliders();
                return;
            }
        }
    }

    // 3. Применяем урон
    _currentHealth -= damage;

    // 4. Проверяем смерть
    if (_currentHealth <= 0)
        Die();
}
```

---

## 4. Животные и их характеристики

### 4.1. AnimalType Enum

**Расположение:** `Assets/Code/Animals/AnimalType.cs`

```csharp
public enum AnimalType
{
    Elephant,   // Слон
    Cheetah,    // Гепард
    Deer,       // Олень
    Fox,        // Лиса
    Hedgehog,   // Ёж
    Chicken     // Курица
}
```

### 4.2. Реализованные Facades

#### 4.2.1. ElephantFacade
```csharp
MergeSkill = new ElephantMergeSkill(multiplier: 1.5f);
```

#### 4.2.2. CheetahFacade
```csharp
MergeSkill = new CheetahMergeSkill(multiplier: 2);
```

#### 4.2.3. DeerFacade
```csharp
MergeSkill = new DeerMergeSkill(multiplier: 1.5f);
```

#### 4.2.4. HedgehogFacade
```csharp
MergeSkill = new HedgehogMergeSkill();
AdditionalAbilities.Add(new CounterAttack(...));
```

#### 4.2.5. FoxFacade
```csharp
MergeSkill = new FoxMergeSkill();
AdditionalAbilities.Add(new Dodge(probability: 0.2f, ...)); // 20%!
```

#### 4.2.6. Chicken (нет специального Facade)
- Использует базовый AnimalFacade
- Имеет MultipleCharacters ability

### 4.3. AnimalFacade - Базовый класс

**Расположение:** `Assets/Code/Animals/Facades/AnimalFacade.cs`

**Ключевые поля:**
```csharp
public IMergeSkill MergeSkill { get; protected set; }        // Основной скилл
public List<IMergeSkill> MergeSkills = new List<IMergeSkill>(); // Накопленные
public List<IAbility> AdditionalAbilities = new List<IAbility>(); // Abilities
```

**Методы апгрейда:**
```csharp
public void UpgradeHealth(float multiplier)
{
    _animalUpgrade.UpgradeHealth(multiplier);
}

public void UpgradeDamage(float multiplier)
{
    _animalUpgrade.UpgradeDamage(multiplier);
}

public void UpgradeSpeed(int multiplier)
{
    _animalUpgrade.UpgradeSpeed(multiplier);
}

public void AddAbility(IAbility ability)
{
    AdditionalAbilities.Add(ability);
    _animalHealth.AddAbility(ability);
}
```

---

## 5. Сравнение с дизайн-документом

### 5.1. Таблица животных из концепт-дока 2.0

| Животное | HP | Damage | Скорость | Размер | Скилл | Скилл при мердже | Визуал при мердже |
|----------|-----|---------|----------|--------|-------|------------------|-------------------|
| Слон | 500 | 150 | 2 | 2×2 | Много хп | ×1.5 ХП | Увеличение размера ×1.5 |
| Олень | 300 | 350 | 5 | 1×2 | Большой дамаг | ×1.5 Дамаг | Рога оленя |
| Гепард | 250 | 250 | 7 | 1×2 | Быстрый | ×2 скорость | Текстура гепарда |
| Ёж | 100 | 100 | 1 | 1×1 | Контратака 100% на 100% | Контратака 50% на 100% | Иглы |
| Лиса | 150 | 100 | 3 | 1×1 | Додж (1 раз, потом 80%) | Отскочит (1 раз, потом 50%) | Хвост лисы |
| 4 Курицы | ×25 | ×50 | 3 | 2×2 | Их 4 | Клонируется 75% ХП и Дамага | Удвоение, уменьшение ×0.75 |
| Орёл | 150 | 200 | 10 | — | Летает далеко | Ход ×0.3 в полёте | Крылья |

### 5.2. Сравнительная таблица: Дизайн vs Реализация

| Аспект | Дизайн-док | Текущая реализация | Статус |
|--------|------------|-------------------|--------|
| **Слон - HP buff** | ×1.5 | ×1.5 | ✅ Соответствует |
| **Слон - Визуал** | Увеличение размера ×1.5 | Увеличение scale ×1.5 | ✅ Соответствует |
| **Гепард - Speed buff** | ×2 | ×2 | ✅ Соответствует |
| **Олень - Damage buff** | ×1.5 | ×1.5 | ✅ Соответствует |
| **Ёж - Контратака владельца** | 100% вероятность | Нет вероятности (всегда) | ⚠️ Требует уточнения |
| **Ёж - Контратака при мердже** | 50% вероятность | Не реализовано | ❌ Не соответствует |
| **Лиса - Dodge владельца** | 1 раз 100%, потом 80% | 20% вероятность | ❌ **КРИТИЧНО** |
| **Лиса - Dodge при мердже** | 1 раз 100%, потом 50% | 50% у Hedgehog | ❌ Не соответствует |
| **Курицы - количество** | 4 копии | 3 дополнительных (4 всего) | ✅ Соответствует |
| **Курицы - характеристики** | 75% HP и Damage | Не указано явно | ⚠️ Требует проверки |
| **Орёл** | Есть в таблице | НЕТ в коде | ❌ Не реализован |
| **Мердж одинаковых** | (HP1+HP2)×0.75 | Не реализовано | ❌ Не соответствует |
| **Вероятность скилла** | Увеличивается с мерджами | Не реализовано | ❌ Не соответствует |
| **Отмена мерджа** | Безболезненно отменять | Не реализовано | ❌ Не соответствует |

---

## 6. Критические расхождения

### 6.1. ❌ КРИТИЧНО: Dodge у лисы (Fox)

**Дизайн-док (стр. 3):**
> "Додж: при попытке врага атаковать юнита, юнит избегает урона первый раз, сдвигаясь на клетку вбок. Далее додж срабатывает в 80% случаев у самой лисы и в 50% у любого другого, унаследовавшего этот скилл."

**Реализация в FoxFacade.cs:**
```csharp
AdditionalAbilities.Add(new Dodge(probability: 0.2f, ...)); // 20%!
```

**Проблема:**
- Дизайн: 100% (первый раз) → 80% (последующие)
- Код: 20% всегда

**Решение:**
Dodge должен иметь:
- Флаг `_isFirstUse = true`
- В `Apply()`: если первый раз → 100%, иначе → 80%

### 6.2. ❌ Мердж одинаковых животных

**Дизайн-док (стр. 2):**
> "если мерджим одно и то же животное, то скиллы не бафаются; жизни и дамаг складываются и умножаются на 0.75; пример: (500+500)*0.75 = 750"

**Реализация:**
Отсутствует логика проверки одинаковых типов и специальный расчёт.

**Решение:**
В `MergeTarget.Accept()` добавить:
```csharp
if (animal.AnimalType == targetAnimal.AnimalType)
{
    // Специальная логика для одинаковых
    float newHP = (animal.MaxHealth + targetAnimal.MaxHealth) * 0.75f;
    float newDamage = (animal.Damage + targetAnimal.Damage) * 0.75f;
    // НЕ применяем скиллы повторно
}
else
{
    // Обычный мердж со скиллами
}
```

### 6.3. ❌ Вероятность скиллов при мердже

**Дизайн-док (стр. 2):**
> "У владельца Скилла работает всегда. [...] При передаче скилла, он срабатывает в каком то % случаев, тем больше, чем больше раз был вмерджен."

**Реализация:**
Все скиллы применяются всегда, вероятность не учитывается.

**Решение:**
1. Добавить `int MergeCount` в IMergeSkill
2. В `Merge(animal, other)` проверять:
   - Если владелец → 100%
   - Если передаётся → вероятность = `BaseChance + (MergeCount × Increment)`

### 6.4. ❌ Контратака с вероятностью

**Дизайн-док (стр. 2):**
> "при атаке на юнита, он даёт сдачи в 50% случаев на 100% своего урона"

**Реализация:**
CounterAttack не имеет вероятностной логики.

**Решение:**
Добавить в CounterAttack:
```csharp
private readonly float _probability;

public void Apply()
{
    if (Random.value > _probability) return;
    // ... rest of logic
}
```

### 6.5. ❌ Отсутствует Орёл

**Дизайн-док:** Орёл присутствует в таблице животных
**Код:** Нет в AnimalType enum

**Решение:**
1. Добавить `Eagle` в AnimalType
2. Создать EagleFacade и EagleMergeSkill
3. Реализовать механику полёта (Speed ×0.3)

### 6.6. ❌ Отмена мерджа

**Дизайн-док (стр. 2):**
> "возможность безболезненно отменять мерджи взад"

**Реализация:**
Отсутствует механизм отмены.

**Решение:**
Реализовать Command Pattern:
```csharp
public class MergeCommand : ICommand
{
    private AnimalFacade _target;
    private AnimalFacade _source;
    private List<IMergeSkill> _appliedSkills;

    public void Execute() { /* merge */ }
    public void Undo() { /* restore state */ }
}
```

---

## 7. План приведения к соответствию

### Фаза 1: Критические исправления (Приоритет: ВЫСОКИЙ)

#### Задача 1.1: Исправить Dodge у Fox
- [ ] Добавить флаг `_isFirstUse` в Dodge
- [ ] Изменить логику: первый раз 100%, потом 80%
- [ ] Изменить вероятность в FoxFacade с 0.2f на 0.8f
- [ ] **Файлы:** `Dodge.cs`, `FoxFacade.cs`

#### Задача 1.2: Реализовать мердж одинаковых животных
- [ ] Добавить проверку `if (animal.AnimalType == target.AnimalType)` в MergeTarget
- [ ] Реализовать формулу: `(HP1 + HP2) * 0.75`, `(DMG1 + DMG2) * 0.75`
- [ ] Скиллы НЕ применяются при мердже одинаковых
- [ ] **Файлы:** `MergeTarget.cs`
- [ ] **Тесты:** Создать юнит-тесты для проверки

#### Задача 1.3: Добавить вероятность в CounterAttack
- [ ] Добавить `private float _probability` в CounterAttack
- [ ] Владелец (Hedgehog): 100% вероятность
- [ ] При мердже: 50% вероятность
- [ ] **Файлы:** `CounterAttack.cs`, `HedgehogMergeSkill.cs`

### Фаза 2: Вероятностная система скиллов (Приоритет: СРЕДНИЙ)

#### Задача 2.1: Добавить MergeCount в IMergeSkill
- [ ] Добавить `int MergeCount { get; set; }` в интерфейс
- [ ] Инкрементить при каждом мердже
- [ ] **Файлы:** `IMergeSkill.cs`, все реализации

#### Задача 2.2: Реализовать вероятностную передачу скиллов
- [ ] Определить базовую вероятность: 30%?
- [ ] Инкремент за мердж: +10%?
- [ ] Макс вероятность: 80%?
- [ ] Формула: `min(BaseChance + MergeCount * Increment, MaxChance)`
- [ ] Применить в `Merge(animal, other)`
- [ ] **Файлы:** Все `*MergeSkill.cs`

#### Задача 2.3: Тестирование вероятностей
- [ ] Создать PlayMode тесты для проверки вероятностей
- [ ] Замерить распределение на 1000 итераций
- [ ] **Папка:** `Assets/Code/Tests/PlayModeTests/MergeSkillProbabilityTests.cs`

### Фаза 3: Новые животные (Приоритет: СРЕДНИЙ)

#### Задача 3.1: Добавить Орла (Eagle)
- [ ] Добавить `Eagle` в AnimalType enum
- [ ] Создать EagleFacade.cs
- [ ] Создать EagleMergeSkill.cs (Speed ×0.3 modifier)
- [ ] Создать префаб орла в `Assets/Resources/Prefabs/Animals/`
- [ ] Добавить в AnimalFactory
- [ ] **Характеристики:** HP=150, Damage=200, Speed=10

### Фаза 4: Система отмены мерджа (Приоритет: НИЗКИЙ)

#### Задача 4.1: Реализовать Command Pattern для мерджа
- [ ] Создать интерфейс `ICommand` с `Execute()` и `Undo()`
- [ ] Создать `MergeCommand : ICommand`
- [ ] Сохранять состояние до мерджа:
  - Характеристики target
  - Список скиллов
  - Список abilities
- [ ] Реализовать `Undo()`: восстановить состояние, вернуть source
- [ ] **Файлы:** `ICommand.cs`, `MergeCommand.cs`

#### Задача 4.2: Интеграция с UI
- [ ] Добавить кнопку "Отмена" в UI мерджа
- [ ] History stack для хранения последних N команд
- [ ] Визуальная индикация возможности отмены
- [ ] **Файлы:** UI префабы, MergeController

### Фаза 5: Полировка и тестирование (Приоритет: СРЕДНИЙ)

#### Задача 5.1: Создать юнит-тесты для системы мерджа
- [ ] Тест: ElephantMergeSkill увеличивает HP на 1.5x
- [ ] Тест: CheetahMergeSkill увеличивает Speed на 2x
- [ ] Тест: DeerMergeSkill увеличивает Damage на 1.5x
- [ ] Тест: HedgehogMergeSkill добавляет CounterAttack
- [ ] Тест: FoxMergeSkill добавляет Dodge
- [ ] Тест: Мердж одинаковых применяет формулу 0.75
- [ ] Тест: Каскадирование скиллов работает корректно
- [ ] **Папка:** `Assets/Code/Tests/EditorTests/MergeSystemTests.cs`

#### Задача 5.2: Создать PlayMode тесты
- [ ] Тест: Полный флоу мерджа от драга до визуала
- [ ] Тест: CounterAttack срабатывает с правильной вероятностью
- [ ] Тест: Dodge срабатывает 100% первый раз, 80% потом
- [ ] Тест: MultipleCharacters создаёт правильное количество копий
- [ ] **Папка:** `Assets/Code/Tests/PlayModeTests/MergeIntegrationTests.cs`

#### Задача 5.3: Документация и рефакторинг
- [ ] Добавить XML-комментарии ко всем public методам
- [ ] Создать диаграммы классов для системы мерджа
- [ ] Обновить README.md с примерами использования
- [ ] Code review: соблюдение SOLID принципов

---

## 8. Технические детали реализации

### 8.1. Пример: Исправление Dodge

**Было в FoxFacade.cs:**
```csharp
AdditionalAbilities.Add(new Dodge(probability: 0.2f, ...));
```

**Стало в Dodge.cs:**
```csharp
public class Dodge : IAbility
{
    private readonly float _ownerProbability = 0.8f;  // 80% для владельца
    private readonly float _inheritedProbability;     // Передаётся при мердже
    private bool _isFirstUse = true;
    private readonly bool _isOwner;

    public Dodge(bool isOwner, float inheritedProbability = 0.5f)
    {
        _isOwner = isOwner;
        _inheritedProbability = inheritedProbability;
    }

    public void Apply()
    {
        float currentProbability;

        if (_isFirstUse)
        {
            currentProbability = 1.0f; // 100% первый раз
            _isFirstUse = false;
        }
        else
        {
            currentProbability = _isOwner ? _ownerProbability : _inheritedProbability;
        }

        if (Random.value > currentProbability) return;

        // ... rest of dodge logic
    }
}
```

**В FoxFacade.cs:**
```csharp
AdditionalAbilities.Add(new Dodge(isOwner: true));
```

**В FoxMergeSkill.cs:**
```csharp
public void Merge(AnimalFacade animal, AnimalFacade other)
{
    animal.AddAbility(new Dodge(isOwner: false, inheritedProbability: 0.5f));
    // ... cascade other skills
}
```

### 8.2. Пример: Мердж одинаковых животных

**В MergeTarget.cs:**
```csharp
public void Accept(AnimalFacade draggedAnimal)
{
    if (draggedAnimal.AnimalType == _targetAnimal.AnimalType)
    {
        // Мердж одинаковых животных
        MergeSameType(draggedAnimal, _targetAnimal);
    }
    else
    {
        // Обычный мердж с передачей скиллов
        MergeDifferentTypes(draggedAnimal, _targetAnimal);
    }

    draggedAnimal.gameObject.SetActive(false);
    Merge?.Invoke(CollectMergedTypes(draggedAnimal));
}

private void MergeSameType(AnimalFacade source, AnimalFacade target)
{
    // Формула из дизайн-дока: (HP1 + HP2) * 0.75
    float combinedHP = (source.MaxHealth + target.MaxHealth) * 0.75f;
    float combinedDamage = (source.Damage + target.Damage) * 0.75f;

    // Устанавливаем новые значения
    target.SetHealth(combinedHP);
    target.SetDamage(combinedDamage);

    // ВАЖНО: Скиллы НЕ применяются повторно
    Debug.Log($"Merged same type: {source.AnimalType}. HP: {combinedHP}, Damage: {combinedDamage}");
}

private void MergeDifferentTypes(AnimalFacade source, AnimalFacade target)
{
    // Применяем скиллы как обычно
    source.MergeSkill.Merge(target, source);
}
```

### 8.3. Пример: Вероятностная передача скиллов

**В IMergeSkill.cs:**
```csharp
public interface IMergeSkill
{
    AnimalType AnimalType { get; }
    int MergeCount { get; set; }
    float InheritanceProbability { get; } // Вероятность передачи

    void Merge(AnimalFacade animal);
    void Merge(AnimalFacade animal, AnimalFacade other);
}
```

**В базовом классе MergeSkillBase.cs (новый):**
```csharp
public abstract class MergeSkillBase : IMergeSkill
{
    public int MergeCount { get; set; } = 0;

    private const float BaseInheritanceChance = 0.3f;
    private const float InheritanceIncrement = 0.1f;
    private const float MaxInheritanceChance = 0.8f;

    public float InheritanceProbability =>
        Mathf.Min(BaseInheritanceChance + MergeCount * InheritanceIncrement, MaxInheritanceChance);

    public void Merge(AnimalFacade animal, AnimalFacade other)
    {
        // Применяем свой эффект всегда
        Merge(animal);

        // Каскадируем скиллы другого животного с вероятностью
        foreach (var skill in other.MergeSkills)
        {
            if (Random.value <= skill.InheritanceProbability)
            {
                skill.Merge(animal);
                skill.MergeCount++;
                Debug.Log($"{skill.AnimalType} skill inherited! Probability: {skill.InheritanceProbability:P0}");
            }
            else
            {
                Debug.Log($"{skill.AnimalType} skill failed to inherit. Probability was: {skill.InheritanceProbability:P0}");
            }
        }
    }

    public abstract void Merge(AnimalFacade animal);
}
```

---

## 9. Связанные файлы

### Код:
- `Assets/Code/Animals/Merge/MergeSkills/IMergeSkill.cs`
- `Assets/Code/Animals/Merge/MergeSkills/ElephantMergeSkill.cs`
- `Assets/Code/Animals/Merge/MergeSkills/CheetahMergeSkill.cs`
- `Assets/Code/Animals/Merge/MergeSkills/DeerMergeSkill.cs`
- `Assets/Code/Animals/Merge/MergeSkills/HedgehogMergeSkill.cs`
- `Assets/Code/Animals/Merge/MergeSkills/FoxMergeSkill.cs`
- `Assets/Code/Animals/Merge/MergeTarget.cs`
- `Assets/Code/Animals/Merge/MergeView.cs`
- `Assets/Code/Animals/Merge/MergeAttributes/VisualMergeAttribute.cs`
- `Assets/Code/Animals/Merge/MergeAttributes/ElephantMergeAttribute.cs`
- `Assets/Code/Abilities/IAbility.cs`
- `Assets/Code/Abilities/CounterAttack.cs`
- `Assets/Code/Abilities/Dodge.cs`
- `Assets/Code/Abilities/MultipleCharacters.cs`
- `Assets/Code/Animals/Facades/AnimalFacade.cs`
- `Assets/Code/Animals/Facades/ElephantFacade.cs`
- `Assets/Code/Animals/Facades/CheetahFacade.cs`
- `Assets/Code/Animals/Facades/DeerFacade.cs`
- `Assets/Code/Animals/Facades/FoxFacade.cs`
- `Assets/Code/Animals/Facades/HedgehogFacade.cs`
- `Assets/Code/Animals/Health/AnimalHealth.cs`
- `Assets/Code/Animals/Upgrade/AnimalUpgrade.cs`

### Спецификации:
- `Documentation/Specifications/01_Pathfinding_System_Specification.md`
- `Documentation/Specifications/02_Pathfinding_Test_Cases.md`
- `Documentation/Specifications/03_Merge_And_Skills_System_Specification.md` (этот файл)

### Дизайн:
- `Концепт-док 2.0-2026012418375932.pdf`

---

## 10. Заключение

Система мерджа и скиллов имеет хорошую архитектурную основу с применением Strategy Pattern для скиллов и чистым разделением ответственности. Однако существуют **критические расхождения** с дизайн-документом, особенно в:

1. **Вероятностях скиллов** (Dodge у Fox: 20% vs 80%)
2. **Мердже одинаковых животных** (отсутствует формула 0.75)
3. **Контратаке** (нет вероятности 50%)
4. **Отсутствии Орла**
5. **Механизме отмены мерджа**

Приоритет исправлений должен быть на **Фазе 1** (критические исправления), так как они влияют на геймплейный баланс и ощущения игроков от скиллов.

**Следующие шаги:**
1. Обсудить с командой расхождения
2. Принять решение: код → дизайн или дизайн → код
3. Выполнить Фазу 1 плана
4. Создать юнит-тесты для предотвращения регрессий
