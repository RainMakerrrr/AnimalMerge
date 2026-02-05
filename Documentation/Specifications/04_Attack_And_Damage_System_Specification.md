# Спецификация системы атаки и получения урона (Attack & Damage System)

**Версия:** 1.0
**Дата:** 2026-02-04
**Статус:** В разработке

---

## 1. Обзор

Система атаки и получения урона отвечает за:
- Обнаружение целей в радиусе атаки
- Применение урона к целям
- Обработку специальных способностей (Dodge, CounterAttack)
- Анимации атаки и получения урона
- Смерть юнитов

**Ключевые принципы:**
- Physics-based detection (OverlapCapsule/OverlapSphere)
- Event-driven damage application
- Ability system integration
- Support for multiple colliders per unit

---

## 2. Геймплейные требования из концепт-дока

### 2.1 Базовые правила боя

**Атака:**
- Атаковать можно по диагонали ("достаем")
- Атака срабатывает через Animation Event в момент нанесения удара
- Дальность атаки определяется радиусом и forward reach

**Урон:**
- Каждое животное имеет базовый урон (damage)
- Урон применяется мгновенно при попадании
- Урон может быть заблокирован способностями (Dodge)
- При HP ≤ 0 юнит умирает

**Очередность:**
- Первыми атакуют юниты игрока (слева-направо, сверху-вниз)
- Затем враги (слева-направо, снизу-вверх)

### 2.2 Специальные способности

**Dodge (Лиса):**
- При попытке атаки уклоняется в сторону
- Первый раз срабатывает 100%
- Владелец: 80% последующие разы
- Унаследовавший: 50%
- **Блокирует урон**
- Не работает против AoE атаки и атаки с воздуха

**CounterAttack (Ёж):**
- При получении урона контратакует
- Владелец: 100% срабатывание
- Унаследовавший: 50%
- **Не блокирует урон**
- Ёж не ходит, только контратакует

**Multiple Characters (4 Курицы):**
- 4 юнита действуют как один
- При merge передают способность

### 2.3 Размеры и коллайдеры животных

Каждое животное состоит из нескольких коллайдеров:
- **Body collider** - основное тело
- **Head collider** - голова
- **Limbs colliders** - конечности

**Важно:** Все коллайдеры одного животного ссылаются на один компонент `AnimalHealth` через `GetComponentInParent<IDamageable>()`.

---

## 3. Архитектура системы

### 3.1 Основные компоненты

```
Attack & Damage System
│
├── Attack Components
│   ├── AnimalAttack.cs              - Обнаружение целей, применение урона
│   └── AnimalAnimator.cs            - Управление анимациями атаки
│
├── Health Components
│   ├── IDamageable.cs               - Интерфейс для получения урона
│   ├── AnimalHealth.cs              - Базовая реализация HP системы
│   ├── FoxHealth.cs                 - Реализация с Dodge ability
│   └── HedgehogHealth.cs            - Реализация с CounterAttack ability
│
└── Ability System
    ├── IAbility.cs                  - Интерфейс способностей
    ├── Dodge.cs                     - Уклонение (блокирует урон)
    ├── CounterAttack.cs             - Контратака (не блокирует урон)
    └── MultipleCharacters.cs        - Множественные персонажи
```

### 3.2 Диаграмма взаимодействия компонентов

```
┌─────────────────┐
│  AnimalAttack   │
│  - _attackPoint │
│  - _radius      │──┐
│  - _damage      │  │
│  - _maxTargets  │  │ AttackAnimationHandler()
└─────────────────┘  │ (вызывается через Animation Event)
                     │
                     ↓
        ┌────────────────────────────┐
        │ Physics.OverlapCapsuleNonAlloc()
        │ Обнаруживает все Collider'ы
        │ в радиусе атаки
        └────────────────────────────┘
                     │
                     ↓
        ┌────────────────────────────┐
        │ HashSet<IDamageable>       │
        │ Дедупликация целей         │ ◄─── FIX: Множественный урон
        └────────────────────────────┘
                     │
                     ↓
        ┌────────────────────────────┐
        │ health.TakeDamage(attacker)│
        └────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────┐
│        AnimalHealth.TakeDamage()    │
│                                     │
│  1. LastAttack = attacker          │
│  2. await ApplyAbilities()         │◄───┐
│     ├─ Dodge (priority 1)          │    │
│     └─ CounterAttack (priority 0)  │    │ Abilities могут
│  3. if (blocked) return            │    │ блокировать урон
│  4. Current -= attacker.Damage     │    │
│  5. TakenDamage?.Invoke()          │    │
│  6. _animator.TakeDamageAnimation()│    │
│  7. if (IsDead) Die()              │    │
└─────────────────────────────────────┘    │
                                           │
        ┌──────────────────────────────────┘
        │
        ↓
┌─────────────────────────────────────┐
│         IAbility System             │
│                                     │
│  interface IAbility:                │
│    - IsBlockingDamage: bool        │
│    - Priority: int                 │
│    - CanUse: bool                  │
│    - Apply(): Task                 │
└─────────────────────────────────────┘
```

---

## 4. Детальное описание компонентов

### 4.1 AnimalAttack.cs

**Файл:** `Assets/Code/Animals/AnimalAttack.cs`

**Назначение:**
- Обнаружение целей в радиусе атаки
- Применение урона к обнаруженным целям
- Управление атакой через Animation Events

**Ключевые поля:**

```csharp
[SerializeField] private Transform _attackPoint;      // Точка начала атаки
[SerializeField] private float _radius;               // Радиус атаки
[SerializeField] private float _forwardReach = 0.25f; // Дальность вперед
[SerializeField] private float _damage;               // Урон атаки
[SerializeField] private int _maxTargets;             // Максимум целей
[SerializeField] private LayerMask _mask;             // LayerMask для поиска
private Collider[] _colliders;                        // Кэш коллайдеров
```

**Основные методы:**

#### `AttackAnimationHandler()`

**Вызов:** Animation Event в момент нанесения удара в анимации атаки

**Логика:**
1. **Проверки:**
   - Если животное - Hedgehog → return (Ёж не атакует)
   - Если `_attackPoint == null` → return

2. **Обнаружение целей (Capsule Cast):**
   ```csharp
   Vector3 a = _attackPoint.position;
   Vector3 b = _attackPoint.position + transform.forward * (_forwardReach + _radius);
   int count = Physics.OverlapCapsuleNonAlloc(a, b, _radius, _colliders, _mask);
   ```

3. **Fallback (Sphere Cast):**
   Если capsule не нашел цели:
   ```csharp
   Vector3 center = _attackPoint.position + transform.forward * _forwardReach;
   count = Physics.OverlapSphereNonAlloc(center, _radius, _colliders, _mask);
   ```

4. **Дедупликация целей (FIX):**
   ```csharp
   var damagedTargets = new HashSet<IDamageable>();

   for (int i = 0; i < count; i++)
   {
       Collider col = _colliders[i];
       if (col == null) continue;

       var health = col.GetComponentInParent<IDamageable>();
       if (health == null) continue;

       damagedTargets.Add(health);  // HashSet дедуплицирует
   }
   ```

5. **Применение урона:**
   ```csharp
   foreach (var health in damagedTargets)
   {
       health.TakeDamage(this);
   }
   ```

**Визуализация (Gizmos):**
- В режиме редактора рисует wireframe sphere в месте атаки
- Цвет: красный

#### `Attack()`

**Назначение:** Запуск анимации атаки и ожидание её завершения

```csharp
public async Task Attack()
{
    if (GetComponent<Animal>().Type == AnimalType.Hedgehog) return;

    await _animator.WaitForAttackAnimation();
}
```

**Особенность:** Hedgehog не ходит и не атакует, только контратакует.

---

### 4.2 AnimalHealth.cs

**Файл:** `Assets/Code/Animals/Health/AnimalHealth.cs`

**Назначение:**
- Управление HP юнита
- Обработка получения урона
- Интеграция с Ability System
- Уведомления о событиях (TakenDamage, Died)

**Интерфейс:**

```csharp
public interface IDamageable
{
    float Current { get; }          // Текущее HP
    float Max { get; }              // Максимальное HP
    bool IsDead { get; }            // Мёртв ли юнит
    void TakeDamage(AnimalAttack attacker);
}
```

**Ключевые поля:**

```csharp
public event Action TakenDamage;            // Событие: получен урон
public event Action Died;                   // Событие: смерть

[SerializeField] private float _max;        // Макс HP
[SerializeField] protected AnimalAnimator _animator;

public IAbility Ability { get; protected set; }              // Основная способность
protected List<IAbility> MergedAbilities = new List<IAbility>(); // Унаследованные

public float Current { get; private set; }  // Текущее HP
public float Max { get; private set; }      // Максимальное HP
public bool IsDead => Current <= 0;

private Collider[] _colliders;              // Массив коллайдеров животного
public AnimalAttack LastAttack { get; protected set; }  // Последняя атака
```

**Основные методы:**

#### `TakeDamage(AnimalAttack attacker)`

**Главный метод получения урона:**

```csharp
public virtual async void TakeDamage(AnimalAttack attacker)
{
    Debug.Log($"[TakeDamage] {name} took {attacker.Damage} damage from {attacker.name}");

    LastAttack = attacker;

    // 1. Применяем способности (могут заблокировать урон)
    bool isBlockedDamage = await ApplyAbilities();

    // 2. Если урон заблокирован (например, Dodge) - выход
    if (isBlockedDamage)
    {
        Debug.Log($"[AnimaHealth] {name} Damage blocked, return");
        EnableColliders();
        return;
    }

    Debug.Log($"[AnimaHealth] {name} Damage taken");

    // 3. Применяем урон
    Current -= attacker.Damage;
    TakenDamage?.Invoke();

    // 4. Анимация получения урона
    _animator.TakeDamageAnimation();

    // 5. Проверка смерти
    if (IsDead)
    {
        Die();
    }
}
```

#### `ApplyAbilities()`

**Применяет все способности в порядке приоритета:**

```csharp
private async Task<bool> ApplyAbilities()
{
    // Объединяем основную способность и унаследованные
    List<IAbility> abilities = new List<IAbility>(MergedAbilities) {Ability};
    abilities = abilities.Where(a => a != null).ToList();

    bool isBlockedDamage = false;

    // Сортировка по Priority (Dodge = 1, CounterAttack = 0)
    foreach (IAbility ability in abilities.OrderBy(a => a.Priority))
    {
        Debug.Log($"[AnimaHealth] {name} apply ability - {ability.GetType().Name}");

        if (ability.CanUse)
        {
            Debug.Log($"[AnimaHealth]{name} apply ability can use");

            // Способность может блокировать урон (Dodge)
            if (ability.IsBlockingDamage)
            {
                Debug.Log($"[AnimaHealth]{name} apply ability blocked damage");
                isBlockedDamage = true;
            }

            await ability.Apply();
        }
    }

    return isBlockedDamage;
}
```

**Порядок выполнения:**
1. Dodge (priority = 1) - блокирует урон, уклоняется
2. CounterAttack (priority = 0) - не блокирует урон, контратакует

#### `Die()`

**Обработка смерти юнита:**

```csharp
private void Die()
{
    Debug.Log("Die");
    Died?.Invoke();

    _animator.DeathAnimation();

    StartCoroutine(DestroyWithDelay());
}

private IEnumerator DestroyWithDelay()
{
    yield return new WaitForSeconds(3f);

    Destroy(gameObject);
}
```

#### Управление способностями:

```csharp
public void SetAbility(IAbility ability) => Ability = ability;

public void AddAbility(IAbility ability)
{
    MergedAbilities.Add(ability);
    Debug.Log($"[AnimaHealth] add ability: {ability.GetType().Name}, {name}, abilities count - {MergedAbilities.Count}");
}
```

#### Upgrade системы:

```csharp
public void Upgrade(float multiplier)
{
    _max *= multiplier;
    Current = _max;
}

public void SetMaxHealth(float newMaxHealth)
{
    Max = newMaxHealth;
    _max = newMaxHealth;
    Current = Max; // Full heal
}
```

---

### 4.3 FoxHealth.cs

**Файл:** `Assets/Code/Animals/Health/FoxHealth.cs`

**Назначение:** Специальная реализация для Лисы с Dodge ability

**Переопределение TakeDamage:**

```csharp
public override async void TakeDamage(AnimalAttack attacker)
{
    LastAttack = attacker;

    // Применяем способности (включая Dodge)
    bool isAbilityApply = await ApplyAbilities();

    // Если Dodge сработал - урон не применяется
    if (isAbilityApply) return;

    // Иначе вызываем базовую логику
    base.TakeDamage(attacker);
}
```

**Особенности:**
- Приоритет обработки abilities через `ApplyAbilities()`
- Если хотя бы одна способность сработала - возврат без применения урона
- Merge abilities применяются первыми

---

### 4.4 HedgehogHealth.cs

**Файл:** `Assets/Code/Animals/Health/HedgehogHealth.cs`

**Назначение:** Специальная реализация для Ежа с CounterAttack

**Переопределение TakeDamage:**

```csharp
public override async void TakeDamage(AnimalAttack attacker)
{
    LastAttack = attacker;

    // Применяем способности (CounterAttack)
    await ApplyAbilities();

    // Всегда получаем урон (CounterAttack не блокирует)
    base.TakeDamage(attacker);
}
```

**Особенности:**
- CounterAttack не блокирует урон (`IsBlockingDamage = false`)
- Ёж всегда получает урон, но контратакует
- Применяются только merged abilities (своя способность закомментирована)

---

## 5. Система Abilities

### 5.1 Интерфейс IAbility

**Файл:** `Assets/Code/Abilities/IAbility.cs`

```csharp
public interface IAbility
{
    bool IsBlockingDamage { get; }  // Блокирует ли урон
    int Priority { get; }           // Приоритет выполнения (выше = раньше)
    bool CanUse { get; }            // Может ли использоваться сейчас
    Task Apply();                   // Применение способности
}
```

**Принципы:**
- **Priority** определяет порядок выполнения (высокий priority = первым)
- **IsBlockingDamage** определяет, блокируется ли урон после применения
- **CanUse** проверяет условия использования (вероятность, кулдауны и т.д.)
- **Apply()** - асинхронное выполнение эффекта способности

---

### 5.2 Dodge (Уклонение)

**Файл:** `Assets/Code/Abilities/Dodge.cs`

**Назначение:** Уклонение в сторону, блокирует урон

**Свойства:**

```csharp
public bool IsBlockingDamage => true;  // БЛОКИРУЕТ урон
public int Priority => 1;              // Высокий приоритет (выполняется первым)
```

**Механика CanUse:**

```csharp
public bool CanUse
{
    get
    {
        // Первое использование: всегда 100%
        if (_counter == 0) return true;

        // Последующие использования:
        // - Владелец (isOwner = true): 80%
        // - Унаследовавший (isOwner = false): 50%
        int successThreshold = _isOwner ? 80 : 50;
        return Random.Range(0, 100) < successThreshold;
    }
}
```

**Применение:**

```csharp
public async Task Apply()
{
    // 1. Отключаем все коллайдеры (чтобы не попасть повторно)
    foreach (Collider collider in _colliders)
    {
        collider.enabled = false;
    }

    // 2. Увеличиваем счетчик использований
    _counter++;

    // 3. Сдвигаемся в сторону
    await _transformable.Shift();
}
```

**Примечание:** Коллайдеры включаются обратно в `AnimalHealth.EnableColliders()` если урон заблокирован.

---

### 5.3 CounterAttack (Контратака)

**Файл:** `Assets/Code/Abilities/CounterAttack.cs`

**Назначение:** Контратака по атакующему, НЕ блокирует урон

**Свойства:**

```csharp
public bool IsBlockingDamage => false; // НЕ блокирует урон
public int Priority => 0;              // Низкий приоритет (после Dodge)
```

**Механика CanUse:**

```csharp
public bool CanUse
{
    get
    {
        // Владелец (Ёж): всегда 100%
        if (_isOwner) return true;

        // Унаследовавший: 50%
        return Random.Range(0, 100) < 50;
    }
}
```

**Применение:**

```csharp
public async Task Apply()
{
    // 1. Анимация контратаки
    _animator.CounterAttackAnimation();

    // 2. Наносим урон атакующему
    _health.LastAttack.GetComponent<IDamageable>().TakeDamage(_attack);

    await Task.CompletedTask;
}
```

**Примечание:** Ёж не перемещается и не атакует сам, только контратакует при получении урона.

---

### 5.4 MultipleCharacters (Множественные персонажи)

**Файл:** `Assets/Code/Abilities/MultipleCharacters.cs`

**Назначение:** Способность для 4 Куриц и подобных множественных юнитов

**Особенности:**
- Представляет несколько юнитов как один
- При merge передает способность
- Детали реализации в отдельном файле

---

## 6. Исправление бага: Множественное нанесение урона

### 6.1 Описание проблемы

**Баг:** Когда животное атакует врага с несколькими коллайдерами, урон применяется несколько раз (умножается на количество коллайдеров врага).

**Root Cause:**

1. Каждое животное имеет **несколько коллайдеров** (body, head, limbs)
2. `Physics.OverlapCapsuleNonAlloc()` обнаруживает **все коллайдеры** врага
3. Все коллайдеры ссылаются на **один и тот же** экземпляр `IDamageable` через `GetComponentInParent<IDamageable>()`
4. Метод вызывал `TakeDamage()` для каждого коллайдера
5. Результат: урон × N, где N = количество коллайдеров

**Пример:**
- Враг имеет 3 коллайдера (Body, Head, Leg)
- Урон атаки = 10
- `OverlapCapsule` находит 3 коллайдера
- Каждый коллайдер → `GetComponentInParent<IDamageable>()` → один и тот же `AnimalHealth`
- `TakeDamage()` вызывается 3 раза
- **Результат: враг получил 30 урона вместо 10!**

### 6.2 Решение: HashSet дедупликация

**Подход:** Дедупликация `IDamageable` экземпляров перед вызовом `TakeDamage()`.

**Преимущества:**
- Гарантированная уникальность (HashSet использует reference equality)
- Performance: O(n) - один проход по коллайдерам
- Чистый и понятный код
- Соответствует Microsoft C# Coding Conventions

**Реализация:**

```csharp
// FIX: Используем HashSet для дедупликации IDamageable экземпляров
var damagedTargets = new HashSet<IDamageable>();

for (int i = 0; i < count; i++)
{
    Collider col = _colliders[i];
    if (col == null) continue;

    var health = col.GetComponentInParent<IDamageable>();
    if (health == null) continue;

    damagedTargets.Add(health);  // HashSet автоматически игнорирует дубликаты
}

// Применяем урон только к уникальным целям
foreach (var health in damagedTargets)
{
    health.TakeDamage(this);
}
```

**Новый execution flow (ПРАВИЛЬНЫЙ):**

```
1. Physics.OverlapCapsuleNonAlloc() обнаруживает коллайдеры
   → [EnemyBodyCollider, EnemyHeadCollider, EnemyLegCollider] (3 коллайдера)

2. Первый цикл - собираем уникальные IDamageable в HashSet:
   i=0: EnemyBodyCollider → EnemyHealth instance → Add to HashSet
   i=1: EnemyHeadCollider → EnemyHealth instance (ТОТ ЖЕ!) → Игнорируется
   i=2: EnemyLegCollider → EnemyHealth instance (ТОТ ЖЕ!) → Игнорируется

   → HashSet содержит 1 уникальный EnemyHealth

3. Второй цикл - применяем урон к уникальным целям:
   EnemyHealth.TakeDamage(10 damage) → HP -= 10  // ОДИН РАЗ! ✅

4. РЕЗУЛЬТАТ: Враг получил 10 урона (правильно!) ✅
```

### 6.3 Изменения в коде

**Файл:** `Assets/Code/Animals/AnimalAttack.cs`

**Изменения:**

1. **Добавлен using:**
   ```csharp
   using System.Collections.Generic;
   ```

2. **Изменен метод `AttackAnimationHandler()` (строки 67-89):**
   - Добавлен `HashSet<IDamageable>` для дедупликации
   - Изменена логика применения урона на два цикла:
     1. Сбор уникальных целей
     2. Применение урона к уникальным целям

**Дата исправления:** 2026-02-04

**Статус:** ✅ Исправлено, требует тестирования

---

## 7. Особые случаи и edge cases

### 7.1 Hedgehog (Ёж)

**Особенности:**
- Не перемещается (`AnimalMovement` отключен)
- Не атакует активно (`AnimalAttack.Attack()` возвращается сразу)
- Контратакует при получении урона (100% для владельца)
- `AttackAnimationHandler()` пропускается для Hedgehog

**Код в `AnimalAttack.AttackAnimationHandler()`:**
```csharp
if (animal != null && animal.Type == AnimalType.Hedgehog) return;
```

### 7.2 Fox (Лиса) с Dodge

**Особенности:**
- Может уклоняться от атак
- Dodge срабатывает ПЕРЕД применением урона
- Если Dodge успешен - урон блокируется полностью
- Коллайдеры отключаются на время уклонения

**Execution flow при атаке на Fox:**
```
1. Атакующий вызывает FoxHealth.TakeDamage()
2. FoxHealth.ApplyAbilities():
   - Dodge.CanUse проверяет вероятность
   - Если true: Dodge.Apply() → отключает коллайдеры, сдвигается
   - Возвращает true (ability applied)
3. FoxHealth.TakeDamage() видит isAbilityApply = true → return
4. Урон НЕ применяется ✅
```

### 7.3 Множественные цели в радиусе

**Сценарий:** Атака захватывает нескольких врагов

**Обработка:**
```csharp
var damagedTargets = new HashSet<IDamageable>();

// Сбор ВСЕХ уникальных целей
for (int i = 0; i < count; i++)
{
    // ... получаем IDamageable из каждого коллайдера
    damagedTargets.Add(health);
}

// Каждая уникальная цель получает урон ОДИН РАЗ
foreach (var health in damagedTargets)
{
    health.TakeDamage(this);
}
```

**Результат:**
- Враг A с 3 коллайдерами → 1 вызов TakeDamage
- Враг B с 2 коллайдерами → 1 вызов TakeDamage
- Враг C с 4 коллайдерами → 1 вызов TakeDamage
- **Итого:** 3 уникальные цели → 3 вызова (правильно!)

### 7.4 Abilities с разными приоритетами

**Пример:** Животное унаследовало и Dodge (priority 1), и CounterAttack (priority 0)

**Порядок выполнения:**
```csharp
abilities.OrderBy(a => a.Priority)

// Priority 1: Dodge
if (Dodge.CanUse)
{
    await Dodge.Apply();  // Блокирует урон
    if (Dodge.IsBlockingDamage)
        isBlockedDamage = true;
}

// Priority 0: CounterAttack
if (CounterAttack.CanUse)
{
    await CounterAttack.Apply();  // Контратакует, но не блокирует
}

return isBlockedDamage;  // true если Dodge сработал
```

**Результат:**
- Если Dodge сработал → урон заблокирован, CounterAttack НЕ срабатывает
- Если Dodge НЕ сработал → урон применяется, CounterAttack может сработать

---

## 8. Performance соображения

### 8.1 Object Pooling

**Рекомендация:** Использовать object pooling для:
- VFX эффектов урона
- Damage numbers
- Частые Attack/TakeDamage вызовы

**Не используется сейчас:** Direct Instantiate/Destroy

### 8.2 Physics Queries

**Оптимизация:**
- `Physics.OverlapCapsuleNonAlloc()` используется вместо `OverlapCapsule()` (без аллокаций)
- `Physics.OverlapSphereNonAlloc()` используется как fallback (без аллокаций)
- `_colliders` массив кэшируется и переиспользуется

**Частота вызовов:** Только в момент Animation Event (не в Update)

### 8.3 HashSet Performance

**Complexity:**
- `HashSet.Add()` - O(1)
- Итерация через HashSet - O(n)
- **Общая сложность:** O(n) где n = количество обнаруженных коллайдеров

**Memory:**
- HashSet создается локально в методе
- GC собирается автоматически после выхода из метода
- Negligible memory impact

---

## 9. Диаграммы последовательности

### 9.1 Обычная атака (без abilities)

```
Player Animal          AnimalAttack         Physics          Enemy Health
     │                      │                   │                  │
     │  Attack Animation    │                   │                  │
     │─────────────────────>│                   │                  │
     │                      │                   │                  │
     │       [Animation Event: AttackAnimationHandler()]          │
     │                      │                   │                  │
     │                      │ OverlapCapsule()  │                  │
     │                      │──────────────────>│                  │
     │                      │                   │                  │
     │                      │  [Collider[]]     │                  │
     │                      │<──────────────────│                  │
     │                      │                   │                  │
     │                      │  [Deduplicate to HashSet]           │
     │                      │                   │                  │
     │                      │     TakeDamage(attacker)            │
     │                      │─────────────────────────────────────>│
     │                      │                   │                  │
     │                      │                   │  [Apply Damage]  │
     │                      │                   │  Current -= Damage
     │                      │                   │  TakeDamageAnim()
     │                      │                   │                  │
```

### 9.2 Атака на Fox с Dodge

```
Attacker               AnimalAttack       Fox Health         Dodge           Grid
   │                        │                 │                │              │
   │  Attack Animation      │                 │                │              │
   │───────────────────────>│                 │                │              │
   │                        │                 │                │              │
   │         [Animation Event]                │                │              │
   │                        │                 │                │              │
   │                        │  TakeDamage()   │                │              │
   │                        │────────────────>│                │              │
   │                        │                 │                │              │
   │                        │                 │  CanUse?       │              │
   │                        │                 │───────────────>│              │
   │                        │                 │                │              │
   │                        │                 │  true (RNG)    │              │
   │                        │                 │<───────────────│              │
   │                        │                 │                │              │
   │                        │                 │  Apply()       │              │
   │                        │                 │───────────────>│              │
   │                        │                 │                │              │
   │                        │                 │                │ Find Free Cell
   │                        │                 │                │─────────────>│
   │                        │                 │                │              │
   │                        │                 │                │ Cell Position │
   │                        │                 │                │<─────────────│
   │                        │                 │                │              │
   │                        │                 │  [Move to cell]│              │
   │                        │                 │                │              │
   │                        │                 │  Blocked=true  │              │
   │                        │                 │<───────────────│              │
   │                        │                 │                │              │
   │                        │  return (no damage applied)      │              │
   │                        │                 │                │              │
```

### 9.3 Атака на Hedgehog с CounterAttack

```
Attacker              AnimalAttack     Hedgehog Health   CounterAttack   Attacker Health
   │                       │                 │                 │               │
   │  Attack Animation     │                 │                 │               │
   │──────────────────────>│                 │                 │               │
   │                       │                 │                 │               │
   │        [Animation Event]                │                 │               │
   │                       │                 │                 │               │
   │                       │  TakeDamage()   │                 │               │
   │                       │────────────────>│                 │               │
   │                       │                 │                 │               │
   │                       │                 │  CanUse?        │               │
   │                       │                 │────────────────>│               │
   │                       │                 │                 │               │
   │                       │                 │  true (100%)    │               │
   │                       │                 │<────────────────│               │
   │                       │                 │                 │               │
   │                       │                 │  Apply()        │               │
   │                       │                 │────────────────>│               │
   │                       │                 │                 │               │
   │                       │                 │  [Counter animation]            │
   │                       │                 │                 │               │
   │                       │                 │  TakeDamage(hedgehogAttack)    │
   │                       │                 │                 │──────────────>│
   │                       │                 │                 │               │
   │                       │                 │                 │  [Damage applied]
   │                       │                 │                 │               │
   │                       │                 │  [Not blocking damage!]         │
   │                       │                 │                 │               │
   │                       │  [Hedgehog takes damage too]     │               │
   │                       │                 │  Current -= Damage              │
   │                       │                 │                 │               │
```

---

## 10. Тест-кейсы

### 10.1 Unit Tests

#### Test Case 1: Базовое нанесение урона

**Цель:** Проверить, что урон применяется корректно один раз

**Setup:**
- Создать атакующего (damage = 10)
- Создать врага (HP = 100, 3 коллайдера)

**Execution:**
- Вызвать `AttackAnimationHandler()`

**Assertions:**
- `TakeDamage()` вызван **1 раз**
- HP врага = 90
- Лог "[TakeDamage]" появляется 1 раз

**Expected Result:** ✅ Pass

---

#### Test Case 2: Дедупликация множественных коллайдеров

**Цель:** Проверить работу HashSet дедупликации

**Setup:**
- Враг с 5 коллайдерами (body, head, 2 arms, 2 legs)
- Все коллайдеры ссылаются на один `AnimalHealth`
- Damage = 15

**Execution:**
- `OverlapCapsule` находит все 5 коллайдеров
- `AttackAnimationHandler()` обрабатывает их

**Assertions:**
- HashSet содержит 1 уникальный `IDamageable`
- `TakeDamage()` вызван **1 раз**
- HP -= 15 (не 75!)

**Debug Logs:**
```
[Attack] Total colliders found: 5, Unique targets: 1
[Attack] Applying damage to: Enemy(AnimalHealth)
[TakeDamage] Enemy took 15 damage
```

**Expected Result:** ✅ Pass

---

#### Test Case 3: Множественные враги в радиусе

**Цель:** Проверить корректную обработку нескольких целей

**Setup:**
- Враг A (3 коллайдера, HP = 100)
- Враг B (2 коллайдера, HP = 80)
- Враг C (4 коллайдера, HP = 120)
- Damage = 20

**Execution:**
- Атака захватывает всех трех врагов

**Assertions:**
- HashSet содержит 3 уникальных `IDamageable`
- `TakeDamage()` вызван **3 раза** (по одному на врага)
- Враг A: HP = 80
- Враг B: HP = 60
- Враг C: HP = 100

**Expected Result:** ✅ Pass

---

#### Test Case 4: Hedgehog не атакует

**Цель:** Проверить, что Ёж не вызывает AttackAnimationHandler

**Setup:**
- Hedgehog (AnimalType.Hedgehog)
- Враг в радиусе атаки

**Execution:**
- Вызвать `AttackAnimationHandler()`

**Assertions:**
- Метод возвращается сразу (early return)
- `TakeDamage()` **НЕ вызван**
- Враг не получил урон

**Expected Result:** ✅ Pass

---

### 10.2 Integration Tests

#### Test Case 5: Fox Dodge блокирует урон

**Цель:** Проверить, что Dodge успешно блокирует урон

**Setup:**
- Fox с Dodge ability (isOwner = true)
- Установить `Dodge._counter = 0` (первое использование, 100%)
- Attacker (damage = 25)

**Execution:**
- Вызвать `FoxHealth.TakeDamage(attacker)`

**Assertions:**
- `Dodge.CanUse` возвращает `true`
- `Dodge.Apply()` вызван
- `isBlockedDamage = true`
- HP Fox **НЕ изменилось**
- Коллайдеры Fox отключены
- Лог: "[AnimaHealth] Damage blocked"

**Expected Result:** ✅ Pass

---

#### Test Case 6: Fox Dodge не сработал - урон применяется

**Цель:** Проверить поведение когда Dodge не срабатывает

**Setup:**
- Fox с Dodge ability (isOwner = false, унаследованный)
- Установить `Dodge._counter = 5` (последующие использования, 50%)
- Mock `Random.Range(0, 100)` → return 51 (fail)
- Attacker (damage = 30)
- Fox HP = 100

**Execution:**
- Вызвать `FoxHealth.TakeDamage(attacker)`

**Assertions:**
- `Dodge.CanUse` возвращает `false`
- `Dodge.Apply()` **НЕ вызван**
- `isBlockedDamage = false`
- HP Fox = 70 (100 - 30)
- Лог: "[AnimaHealth] Damage taken"

**Expected Result:** ✅ Pass

---

#### Test Case 7: Hedgehog CounterAttack срабатывает

**Цель:** Проверить, что CounterAttack наносит урон атакующему

**Setup:**
- Hedgehog (HP = 50, isOwner = true)
- Hedgehog Attack (damage = 15)
- Attacker (HP = 100, damage = 20)

**Execution:**
- Attacker атакует Hedgehog
- `HedgehogHealth.TakeDamage(attacker)` вызывается

**Assertions:**
- `CounterAttack.CanUse` возвращает `true` (100% для owner)
- `CounterAttack.Apply()` вызван
- Hedgehog HP = 30 (50 - 20) ← **получил урон**
- Attacker HP = 85 (100 - 15) ← **получил контратаку**
- Анимация контратаки воспроизведена

**Expected Result:** ✅ Pass

---

#### Test Case 8: Abilities с приоритетами

**Цель:** Проверить порядок выполнения abilities

**Setup:**
- Животное с двумя abilities:
  - Dodge (priority = 1, isBlockingDamage = true)
  - CounterAttack (priority = 0, isBlockingDamage = false)
- Dodge.CanUse = true
- CounterAttack.CanUse = true

**Execution:**
- Вызвать `TakeDamage()`

**Assertions:**
- **Порядок вызовов:**
  1. `Dodge.Apply()` вызван первым
  2. `Dodge.IsBlockingDamage = true` → урон заблокирован
  3. `CounterAttack.Apply()` вызван вторым
- HP **НЕ изменилось** (Dodge заблокировал)
- Контратака всё равно произошла

**Expected Result:** ✅ Pass

---

### 10.3 Regression Tests

#### Test Case 9: Не ломается при null коллайдерах

**Цель:** Проверить устойчивость к null значениям

**Setup:**
- `_colliders` массив содержит null элементы
- Damage = 10

**Execution:**
- `OverlapCapsule` возвращает count = 3
- `_colliders = [Collider1, null, Collider2]`

**Assertions:**
- Цикл пропускает null (`if (col == null) continue`)
- Обрабатываются только валидные коллайдеры
- Нет NullReferenceException

**Expected Result:** ✅ Pass

---

#### Test Case 10: Враг без IDamageable

**Цель:** Проверить поведение при отсутствии компонента

**Setup:**
- Collider без компонента `IDamageable` в иерархии

**Execution:**
- `GetComponentInParent<IDamageable>()` возвращает `null`

**Assertions:**
- Цикл пропускает коллайдер (`if (health == null) continue`)
- HashSet не содержит null
- Нет NullReferenceException

**Expected Result:** ✅ Pass

---

### 10.4 Performance Tests

#### Test Case 11: Большое количество коллайдеров

**Цель:** Проверить производительность с большим количеством целей

**Setup:**
- 50 врагов в радиусе атаки
- Каждый враг имеет 4 коллайдера
- **Итого:** 200 коллайдеров обнаружено

**Execution:**
- `AttackAnimationHandler()` обрабатывает все коллайдеры

**Assertions:**
- HashSet содержит 50 уникальных `IDamageable`
- `TakeDamage()` вызван **50 раз** (не 200)
- Время выполнения < 16ms (60 FPS)
- Нет GC аллокаций (кроме локального HashSet)

**Expected Result:** ✅ Pass

---

## 11. Известные ограничения

### 11.1 Animation Events timing

**Проблема:** `AttackAnimationHandler()` зависит от правильного размещения Animation Event

**Решение:**
- Animation Event должен быть размещен в момент визуального контакта удара
- Если Event слишком рано/поздно - урон применится не синхронно с анимацией

### 11.2 Capsule vs Sphere detection

**Текущая реализация:**
- Сначала пытается `OverlapCapsule` (направленный поиск вперед)
- Если не находит - fallback на `OverlapSphere` (радиальный поиск)

**Ограничение:**
- Sphere fallback может захватить цели сзади атакующего
- Геймплейное решение: радиус подобран так, чтобы минимизировать false positives

### 11.3 LayerMask конфигурация

**Важно:** `_mask` должен быть настроен корректно:
- Включать слои врагов/союзников (в зависимости от атакующего)
- Исключать слои UI, Ground, и т.д.

**Проверка:** В случае проблем с обнаружением целей - проверить LayerMask в Inspector

### 11.4 Async void в TakeDamage

**Текущая реализация:** `TakeDamage()` объявлен как `async void`

**Проблема:**
- Невозможно await вызов
- Исключения могут быть проглочены
- Сложно тестировать

**Рекомендация для будущего:** Рассмотреть изменение интерфейса на `Task TakeDamage()`

---

## 12. Roadmap и будущие улучшения

### 12.1 Краткосрочные (MVP)

- [x] ✅ Исправить баг множественного урона (HashSet дедупликация)
- [ ] 🔄 Удалить Debug.Log после финального тестирования
- [ ] ⏳ Написать Unit Tests для AnimalAttack
- [ ] ⏳ Написать Integration Tests для Abilities
- [ ] ⏳ Балансировка урона после исправления бага

### 12.2 Среднесрочные

- [ ] Реализовать AoE (Area of Effect) атаки
- [ ] Добавить Critical Hits систему
- [ ] Реализовать Damage Types (Physical, Magic, True)
- [ ] Добавить Armor/Resistance систему
- [ ] Улучшить VFX для различных типов урона

### 12.3 Долгосрочные

- [ ] Refactor `IDamageable.TakeDamage()` → `Task TakeDamage()`
- [ ] Реализовать Damage Prediction для AI
- [ ] Добавить Replay System для боев
- [ ] Оптимизация с Object Pooling для VFX
- [ ] Combat Log система

---

## 13. Ссылки и связанные документы

### 13.1 Внутренние документы

- **Концепт-док 2.0** - `Концепт-док 2.0-2026012418375932.pdf`
  - Геймплейные требования
  - Баланс животных
  - Описание способностей

- **Pathfinding System Spec** - `01_Pathfinding_System_Specification.md`
  - Система передвижения
  - Взаимодействие с атаками

- **Merge System Spec** - `03_Merge_And_Skills_System_Specification.md`
  - Наследование способностей
  - Merge mechanics

### 13.2 Code Files

**Attack System:**
- `Assets/Code/Animals/AnimalAttack.cs`
- `Assets/Code/Animals/AnimalAnimator.cs`

**Health System:**
- `Assets/Code/Animals/Health/IDamageable.cs`
- `Assets/Code/Animals/Health/AnimalHealth.cs`
- `Assets/Code/Animals/Health/FoxHealth.cs`
- `Assets/Code/Animals/Health/HedgehogHealth.cs`

**Ability System:**
- `Assets/Code/Abilities/IAbility.cs`
- `Assets/Code/Abilities/Dodge.cs`
- `Assets/Code/Abilities/CounterAttack.cs`
- `Assets/Code/Abilities/MultipleCharacters.cs`

### 13.3 External References

- **Microsoft C# Coding Conventions**: https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- **Unity Physics Best Practices**: https://docs.unity3d.com/Manual/PhysicsBestPractices.html
- **Unity Animation Events**: https://docs.unity3d.com/Manual/script-AnimationWindowEvent.html

---

## 14. Changelog

### Version 1.0 (2026-02-04)

**Added:**
- ✅ Полная спецификация системы атаки и получения урона
- ✅ Документация исправления бага множественного урона
- ✅ 11 тест-кейсов (Unit, Integration, Regression, Performance)
- ✅ Диаграммы последовательности для основных сценариев
- ✅ Архитектурные диаграммы компонентов

**Fixed:**
- ✅ Баг множественного нанесения урона (HashSet дедупликация)

**Changed:**
- ✅ `AnimalAttack.AttackAnimationHandler()` - добавлена дедупликация
- ✅ Добавлен Debug logging для верификации

---

## Приложение A: Глоссарий

| Термин | Описание |
|--------|----------|
| **IDamageable** | Интерфейс для объектов, которые могут получать урон |
| **AttackAnimationHandler** | Метод, вызываемый через Animation Event для применения урона |
| **HashSet Deduplication** | Техника удаления дубликатов через HashSet коллекцию |
| **Ability** | Специальная способность животного (Dodge, CounterAttack, и т.д.) |
| **IsBlockingDamage** | Свойство ability, определяющее блокировку урона |
| **Priority** | Приоритет выполнения ability (выше = раньше) |
| **OverlapCapsule** | Physics query для обнаружения коллайдеров в капсуле |
| **GetComponentInParent** | Unity метод для поиска компонента в родительских объектах |
| **Reference Equality** | Сравнение объектов по ссылке (не по значению) |

---

## Приложение B: Таблица баланса урона (пример)

| Животное | Базовый Урон | Атак/Ход | DPS | Примечания |
|----------|--------------|----------|-----|------------|
| Лиса | 10 | 1 | 10 | Может уклониться |
| Ёж | 15 | 0 | 0 | Только контратака (15) |
| Гепард | 20 | 1 | 20 | Высокая скорость |
| Слон | 30 | 1 | 30 | Низкая скорость |
| 4 Курицы | 5 × 4 | 1 | 20 | 4 атаки за ход |

**Примечание:** Баланс требует пересмотра после исправления бага множественного урона.

---

**Конец спецификации**
