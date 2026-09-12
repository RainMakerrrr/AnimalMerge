# Решение: скиллы в карточке животного берутся из данных, а не из статического текста префаба

**Дата:** 2026-09-12
**Статус:** принято, реализовано

## Контекст
Карточка статов животного (`AnimalStatsPanelView`/`EnemyCardView`) показывала строки способностей
как статический текст префаба: контракт `abilityLines`, заведённый в итерации 1 HUD
(`Decisions/2026-09-07-battle-hud-layout-iteration-1.md`), был трёхзначным — `null` означал «не
трогать лейблы, оставить текст префаба» (константа `KeepPrefabAbilityLines`), и оба презентера
всегда передавали именно `null`, потому что `IAbility` не отдавала ни имени, ни шанса. В префабе
были захардкожены заглушки «30% Dodge» / «50% Kickback» — способности `Kickback` в проекте не
существует вообще; у `EnemyCardView.prefab` массив `_abilityLabels` был вообще пустым, то есть
врагам физически негде было рисовать строки. Задача: строки должны реально браться из способностей,
которыми обладает животное (включая полученные мерджем), и если способности нет — строка не
показывается; то же должно работать для врагов.

## Рассмотренные варианты
1. **Добавить `Describe()`/шанс прямо в `IAbility`.** Отклонено: `IAbility` — контракт, под который
   в тестовой сборке написаны рукописные моки (`MockDodge`/`MockCounterAttack` в
   `AbilityTestMocks.cs`); расширение интерфейса ломает компиляцию тестовой сборки, а TESTS PAUSED
   запрещает их переписывать ради фичи.
2. **Отдельный opt-in интерфейс `IDescribableAbility`** с методом `Describe()`, который реализуют
   только те способности, которые нужно показывать в UI; провайдер карточки берёт `as
   IDescribableAbility`. Выбрано — не трогает `IAbility` и существующие моки, новая способность без
   реализации интерфейса не ломает сборку, а лишь не попадает в карточку (с явным предупреждением).
3. **Оставить трёхзначный контракт** (`null`/пусто/данные) ради обратной совместимости. Отклонено:
   сама суть тикета — убрать состояние «текст остаётся висеть, если данных нет», трёхзначность и
   есть источник бага с «Kickback».

## Принятое решение
- `Code/Abilities/AbilityKind.cs` (enum `Dodge`/`CounterAttack`/`Retreat`),
  `Code/Abilities/AbilityDescription.cs` (readonly struct `Kind` + `ChancePercent`),
  `Code/Abilities/IDescribableAbility.cs` (`AbilityDescription Describe()`) — новый, но
  необязательный контракт, сознательно НЕ добавленный в `IAbility`. `Dodge`, `CounterAttack`,
  `RetreatAbility` реализуют его и отдают свой приватный `_successChance`.
- `Code/Animals/UI/AbilityLinesProvider.cs` (`IAbilityLinesProvider`): идёт по
  `AnimalFacade.AbilityManager.Abilities` в порядке регистрации, группирует по `AbilityKind` с
  сохранением порядка первого появления, внутри группы берёт МАКСИМАЛЬНЫЙ шанс — ровно случай лисы,
  влитой в лису: два независимых `Dodge` (owner 50 + inherited 30) схлопываются в одну строку «50%
  Dodge». Формат: шанс ≤ 0 или ≥ 100 → только имя способности, иначе `"{chance}% {name}"`. Гардит
  `animal == null` и `AbilityManager == null` (фасад создаёт менеджер в `Start()`, до этого момента
  карточка не должна падать). Способность без `IDescribableAbility` пишет `Debug.LogWarning` вместо
  тихого выпадения строки из карточки.
- Контракт `abilityLines` в `IAnimalStatsPanelView`/`IEnemyCardView` перестал быть трёхзначным:
  `null` теперь трактуется как «ноль строк», `AnimalStatsPanelView.ApplyAbilityLines` деактивирует
  ВСЕ лейблы. `KeepPrefabAbilityLines` из итерации 1 удалена вместе со своей семантикой.
- Оба презентера (`AnimalStatsPanelPresenter`, `EnemyCardPresenter`) передают
  `_abilityLinesProvider.Build(animal)`. Ally-презентер дополнительно подписан на
  `IMergeUndoService.OnStackCountChanged`, чтобы открытая карточка обновлялась после UNDO MERGE,
  который снимает замердженную способность — подписка на сам мердж была бы мёртвым кодом, потому
  что выделение уже сбрасывается в `AnimalMover.BeginDrag` до завершения мерджа.
- Zenject: `BindAbilityLines()` в `BattleInstaller`.
- Префабы: у `AnimalStatsPanelView.prefab` очищены тексты-заглушки. У `EnemyCardView.prefab`
  (`_abilityLabels` было `[]`) добавлены два лейбла по образцу ally-префаба, неактивные, привязаны
  сверху вниз.
- Новое поле `_abilityLineHeight` на `AnimalStatsPanelView`: высота карточки = `base + visibleLines
  * lineHeight`, `AttackRow`/`HealthRow` переякорены к верху, чтобы карточка росла вниз, а не
  наезжала статами на строки способностей. У ally-префаба `0` (не трогать выверенный layout), у
  enemy-префаба `32`. Компенсация роста сделана в коде (`ScreenOffsetKeepingTopEdge` сдвигает
  позицию вниз на половину прироста высоты), а НЕ переворотом пивота в префабе —
  `ClampInsideCanvas`/`CoversAnchor` из итерации 1 предполагают центральный пивот.
- Шанс `RetreatAbility` переведён на данные животного тем же паттерном, что `FoxStats`/
  `HedgehogStats` ([[2026-06-27-fox-dodge-stats]]): новый `Code/Data/Animals/VelociraptorStats.cs`
  (`_ownerRetreatChance`/`_inheritedRetreatChance`, дефолты 100/50). `VelociraptorStats.asset`
  сконвертирован in-place на новый класс (GUID и запись в `AnimalDatabase` не менялись).
  `VelociraptorFacade` читает шанс через `AnimalDatabase` и передаёт конструктором в
  `RetreatAbility`; константы 100/50 из `RetreatAbility` убраны — теперь один источник и для
  `CanUse`, и для `Describe`. `RetreatAbility` сохранила параметр `isOwner` (существующие тесты
  конструируют её без явного шанса) — известный компромисс, `Code.Abilities` теперь ссылается на
  `Code.Data.Animals` за дефолтами, кандидат на вынос дефолтов обратно в фасад.

## Почему так
- Opt-in интерфейс вместо расширения `IAbility` — минимальный blast radius: не трогает тестовые
  моки под TESTS PAUSED и не требует переписывать реализации `IAbility`, которым в карточке
  показывать нечего.
- Схлопывание дублей по максимальному шансу — сознательный компромисс, а не точность: два
  независимых броска 50%+30% дают ~65% совокупно, карточка показывает номинал 50%, а не эффективную
  вероятность. Считать и показывать точный эффективный шанс в UI-слое сочтено избыточным ради одной
  строки текста.
- `Debug.LogWarning`, а не тихий пропуск способности без `IDescribableAbility` — иначе следующая
  добавленная в проект способность молча выпадала бы из карточки, и баг находился бы только
  плейтестом. Сейчас это недостижимый случай (`MultipleCharacters` — единственная реализация
  `IAbility` без `IDescribableAbility` — не задействована ни на одном животном).
- Отображаемые названия на английском (`Dodge`/`Counter`/`Retreat`), а не переводы — консистентно с
  остальным HUD (LEVEL / FIGHT / ADD ANIMAL / UNDO MERGE).

## Проверка
- Компиляция: 0 ошибок.
- EditMode-тесты, которые трогает область (Ability/Dodge/CounterAttack/Retreat): число падений не
  выросло относительно baseline. `RetreatAbilityTests` (4) и `AnimalAttackPostAbilityTests` (5)
  падают уже на baseline с коммита `6d472b58` из-за сигнатур `[Test] public async Task` — не
  чинилось, действует TESTS PAUSED.
- Проверено вручную в редакторе: карточка союзника показывает Dodge/CounterAttack там, где они
  реально есть (включая полученные мерджем), и не показывает ничего там, где способности нет;
  карточка врага (`VelociraptorFacade`) показывает Retreat, у остальных пяти врагов (без
  способностей) строк нет; UNDO MERGE обновляет уже открытую карточку.
- Известные ограничения на будущее: ally-карточка вмещает ровно 2 строки без запаса — третий вид
  способности у союзника был бы обрезан (сейчас недостижимо, максимум видов у одного союзника — 2:
  Dodge лисы + CounterAttack ежа); из шести врагов способность есть только у `VelociraptorFacade`
  (`TRex`/`Pterodactyl` — пустой `InitBehaviours()`, `Cheetah`/`Chicken`/`Elephant` задают только
  `MergeSkill`) — это ожидаемое поведение, а не баг.

## Затронутые файлы
Новые: `Assets/Code/Abilities/AbilityDescription.cs`, `Assets/Code/Abilities/AbilityKind.cs`,
`Assets/Code/Abilities/IDescribableAbility.cs`, `Assets/Code/Animals/UI/AbilityLinesProvider.cs`,
`Assets/Code/Animals/UI/IAbilityLinesProvider.cs`, `Assets/Code/Data/Animals/VelociraptorStats.cs`.

Изменены: `Assets/Code/Abilities/CounterAttack.cs`, `Assets/Code/Abilities/Dodge.cs`,
`Assets/Code/Abilities/RetreatAbility.cs`, `Assets/Code/Animals/Facades/VelociraptorFacade.cs`,
`Assets/Code/Animals/UI/AnimalStatsPanelPresenter.cs`, `Assets/Code/Animals/UI/AnimalStatsPanelView.cs`,
`Assets/Code/Animals/UI/IAnimalStatsPanelView.cs`, `Assets/Code/Battle/UI/EnemyCardPresenter.cs`,
`Assets/Code/Battle/UI/IEnemyCardView.cs`, `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`,
`Assets/Prefabs/AnimalStatsPanelView.prefab`, `Assets/Prefabs/EnemyCardView.prefab`,
`Assets/Settings/Animals/Stats/VelociraptorStats.asset`.
