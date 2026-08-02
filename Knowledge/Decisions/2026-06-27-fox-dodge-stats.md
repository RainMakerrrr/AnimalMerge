# Решение: вероятности Dodge для Fox → подкласс FoxStats

**Дата:** 2026-06-27
**Статус:** принято, реализовано

## Контекст
В `Dodge.CanUse` вероятность срабатывания была захардкожена:
```csharp
int successThreshold = _isOwner ? 50 : 30; // 50% владелец, 30% унаследованный
```
Значения Fox-специфичные и зашиты в логику способности — дизайнер не мог их менять без правки кода. Задача: вынести в данные.

## Рассмотренные варианты
1. **Поля в `AnimalConfig`** (нач. реализация): `DodgeSettings` (OwnerChance/InheritedChance) + `AnimalDatabase.GetDodgeSettings`. Минус: поле появляется у КАЖДОГО животного в Inspector, хотя читает его только Fox (как `MergeInfo`).
2. **Подкласс `FoxStats : AnimalStats`** (выбрано): данные о доже живут со статами Fox.

## Принятое решение
Подкласс `FoxStats : AnimalStats` (`Code/Data/Animals/FoxStats.cs`):
- `_ownerDodgeChance` / `_inheritedDodgeChance` (`[SerializeField, Range(0,100)]`), геттеры `OwnerDodgeChance`/`InheritedDodgeChance`.
- Константы `DefaultOwnerDodgeChance=50` / `DefaultInheritedDodgeChance=30` — единый источник дефолтов (инициализаторы полей + фоллбэк в фасаде).

Ассет `Assets/Settings/Animals/Stats/FoxStats.asset` сконвертирован in-place с `AnimalStats` на `FoxStats`: сменён `m_Script` GUID на `97b3a2ae85c1743b2ba7cc12efc71c60`, добавлены `_ownerDodgeChance:50`/`_inheritedDodgeChance:30`. GUID ассета (`3e14d4c6…`) не менялся → ссылка в `AnimalDatabase` сохранилась; Health/Damage/Tiles сохранены (имена полей базового класса совпадают).

`Dodge` стал value-agnostic: конструктор принимает `int successChance` вместо `bool isOwner`; различие owner/inherited решается на стороне вызова. `FoxFacade` инжектит `AnimalDatabase`, читает `GetStats(Type) as FoxStats` (pattern-match с фоллбэком на дефолты + `LogWarning`), передаёт ownerChance в `Dodge` и inheritedChance в `new FoxMergeSkill(inheritedChance)`. `FoxMergeSkill` хранит `_inheritedChance` и применяет в обеих перегрузках `Merge` (инстанс Fox-скилла пропагируется по `MergeSkills` и вызывается из `MergeTarget`, поэтому значение доходит и при мердже Fox в другое животное).

## Почему так
- Данные тюнинга способности логически принадлежат статам животного, а не общему конфигу — нет «протечки» Fox-полей в записи других животных.
- `Dodge` не знает о политике owner/inherited — проще и переиспользуемо.
- Чтение из БД в фасаде не зависит от порядка вызова `ApplyStats` (self-contained в `InitBehaviours`).

## Проверка
- 0 ошибок компиляции.
- Runtime: `GetStats(Fox)` → `FoxStats`, Owner=50/Inherited=30, Health=150/Damage=100/Tiles=3 (сохранены).
- 26 EditMode тестов Dodge/AoE зелёные.

## Затронутые файлы
`Code/Data/Animals/FoxStats.cs` (новый), `Settings/Animals/Stats/FoxStats.asset`, `Code/Abilities/Dodge.cs`, `Code/Animals/Facades/FoxFacade.cs`, `Code/Animals/Merge/MergeSkills/IMergeSkill.cs`, тесты (`AbilityTestMocks`, `DodgeAbilityTests`, `AoEAbilityTests`, `AoEAttackIntegrationTests`, `AttackAndDamageIntegrationTests`).

---

## Addendum 2026-08-01 — значения 50/30 заменены на 80/50

Структурное решение выше (где живут данные: `FoxStats`, value-agnostic `Dodge`, owner/inherited решается на стороне вызова) **остаётся в силе без изменений**.

Меняются только сами числа. Значения 50/30 были перенесены дословно из существовавшего хардкода и балансным решением не являлись. Концепт-док 3.0 (ClickUp «Heroes & Bosses») требует 80/50, поэтому по §2.2 спецификации `AgentsDocs/Specifications/06_Balance_Sync_With_Concept_Doc.md`:

- `Settings/Animals/Stats/FoxStats.asset`: `_ownerDodgeChance: 50 → 80`, `_inheritedDodgeChance: 30 → 50`
- `Code/Data/Animals/FoxStats.cs`: `DefaultOwnerDodgeChance = 50 → 80`, `DefaultInheritedDodgeChance = 30 → 50` (фоллбэк-дефолты должны совпадать с ассетом, иначе появляется второй несогласованный источник истины)

Логика «первый додж гарантирован» (`Dodge.CanUse`, `_counter == 0 => true`) не менялась.

По этому же образцу в тот же день вынесен шанс контратаки ежа: `Code/Data/Animals/HedgehogStats.cs` (`_ownerCounterChance: 100` / `_inheritedCounterChance: 50`), `CounterAttack` стал value-agnostic (`int successChance` вместо `bool isOwner`), `HedgehogFacade` читает `AnimalDatabase`, `HedgehogMergeSkill` хранит `_inheritedChance`.

---

## Addendum 2026-08-02 — ❌ аддендум 2026-08-01 ОТМЕНЁН, вернулись к 50/30

Структурное решение (где живут данные) по-прежнему в силе. Отменяются только числа из аддендума выше.

**Аддендум 2026-08-01 опирался на таблицу животных концепт-дока («потом с 80% вер.» / «потом с 50% вер.») и не заметил, что страница «Abilities» того же дока говорит прямо противоположное: «срабатывает в 50% случаев у самой лисы и в 30% у наследника».** Обе страницы правлены в один заход с разницей в пару минут — по свежести не рассудить.

**Решение владельца 2026-08-02: по процентам срабатывания скиллов главная — страница «Abilities».** Таблица животных в этой части устарела. Значит:

- `Settings/Animals/Stats/FoxStats.asset`: **50 / 30** — на диске так и оставалось, менять не пришлось (правка 2026-08-01 до диска не доехала, см. `06_Balance_Sync_With_Concept_Doc.md` §8).
- `Code/Data/Animals/FoxStats.cs`: `DefaultOwnerDodgeChance = 80 → 50`, `DefaultInheritedDodgeChance = 50 → 30` — вот тут правка была реальной и создала второй источник истины; откачена.

**Контратака ежа переоценена там же и по той же причине:** таблица даёт владельцу 100%, но проза обеих страниц пишет «даёт сдачи в 50% случаев», а «Abilities» прямо снимает «всегда» — «У хозяина срабатывает в каком-то % случаев». Принято **50 / 50**: `HedgehogStats.asset._ownerCounterChance 100 → 50`, `HedgehogStats.cs DefaultOwnerCounterChance 100 → 50`.

⚠️ Побочный эффект, требующий плейтеста: ёж не атакует сам (`AnimalAttack` хардкодом возвращается на `AnimalType.Hedgehog`), поэтому при 50% он ровно половину столкновений не делает вообще ничего — роль «медленный танк-контратакер» заметно слабеет.

**Урок:** концепт-док внутренне противоречив; таблицы и страница «Abilities» расходятся по процентам. Перед правкой баланса по числу из таблицы — проверять, не сказано ли на «Abilities» иное. См. [[2026-08-01-chicken-flock-and-balance-sync]].
