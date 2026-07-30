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
