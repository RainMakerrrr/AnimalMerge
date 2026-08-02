# Решение: стая курицы — футпринт операции, а не размер юнита; баланс синхронизирован с концепт-доком 3.0

**Дата:** 2026-08-01
**Статус:** принято, реализовано (§2.1–2.7 и §2.9 спеки 06; §2.8 и §3 заблокированы)

## Контекст

Спека `AgentsDocs/Specifications/06_Balance_Sync_With_Concept_Doc.md` (вне вольта Obsidian)
зафиксировала 9 расхождений фактических данных проекта с концепт-доком 3.0 (ClickUp «Heroes & Bosses»).
Пять из них — чистые числа в `.asset`, четыре требовали решений:

- **§2.6** — стая курицы должна быть **ровно 4** юнита блоком **2х2**. Фактически `AnimalFactory.SpawnAdditionalChickens`
  брал «до 3» соседних клеток произвольной формой, а `GridManager.HasCellFor(Chicken)` резервировал **одну** клетку 1х1.
  На тесной сетке получалось 1–3 курицы, форма недетерминирована. Прошлая сессия записала это как «безопасную деградацию»
  ([[2026-08-01-animal-spawn-pools-and-prefab-parity]], §«Почему так»); заказчик уточнил, что это **баг**.
- **§2.7** — клон курицы должен визуально быть ×0.75 от оригинала (логические 75% HP/Dmg уже были), причём **оба** юнита
  после мерджа — «мельче», а не только клон.
- **§2.9** — шанс контратаки ежа `50` захардкожен в `CounterAttack.cs`, тогда как по доку это 100% владелец / 50% унаследованный.
- **§2.2** — прямой конфликт с уже записанным решением: [[2026-06-27-fox-dodge-stats]] фиксировал додж 50/30, док требует 80/50.

## Рассмотренные варианты

**1. Как сделать стаю 2х2.**
- *Поднять `_unitSize` курицы до 2х2 на префабе* — буквальное прочтение «блок 2х2». Отвергнуто: курица перестала бы быть
  четырьмя юнитами. Один `AnimalMovement`, один `Health`, один ход на всю стаю, одна смерть — это уже другое животное,
  и вся механика «клон при мердже» теряет смысл.
- ✅ *Курица остаётся `_unitSize: 1x1`, а 2х2 — футпринт операции спавна.* Новый `ChickenFlock` (`Footprint = 2x2`, `Count = 4`)
  описывает **запрос на размещение**, а не юнит. Каждая курица независима: свои HP, свой ход, своя смерть, свой `ChickenAttack`.

**2. Как гарантировать «ровно 4 или ни одной».**
- *Guard в `AnimalSpawner` после создания главной курицы* — пришлось бы уничтожать уже созданный юнит, а на пути уничтожения
  в проекте несколько мест, которые не зовут `NotifyRemoved()`.
- ✅ *Ветвление до инстанцирования.* `AnimalSpawner.Spawn` уходит на flock-ветку **до** первого `Create`, фабрика получает
  готовый список из 4 клеток и создаёт всё атомарно. Нет блока 2х2 → не создаётся ни одной курицы.

**3. Чем чистить занятость сетки при откате неудачной стаи.**
- *`created.Occupancy` через `AnimalFacade`* — выглядит естественно, но **не работает** (см. ниже).
- ✅ *`created.Movement.ClearNodes()`.*

**4. Конфликт 50/30 vs 80/50 у лисы.**
- ✅ *Побеждает концепт-док.* Заметка 2026-06-27 фиксировала **рефакторинг** (где лежат числа), а не **баланс** (какие числа) —
  50/30 были перенесены дословно из старого хардкода и балансным решением никогда не были. В ту заметку добавлен
  датированный аддендум, структурная часть решения не изменилась.

## Принятое решение

### §2.6 — стая курицы

- Новый `Assets/Code/Animals/ChickenFlock.cs` — константы `Footprint` (2х2) и `Count` (4). Единственное место, где записано,
  что такое «стая».
- `GridManager.TryFindFreePlacement(size, direction, out anchor)` — **единственный** алгоритм поиска места.
  `HasCellFor` и `PlaceOnGrid` теперь оба делегируют сюда, дублирования логики больше нет.
  Ключевое: `AdjustPositionToFitBounds` вызывается **до** `CanPlaceUnit` и наружу отдаётся **скорректированный** anchor.
- `IAnimalFactory.SpawnAdditionalChickens(mainChicken)` заменён на `CreateChickenFlock(IReadOnlyList<IGridCell>)` — атомарная операция.
- `AnimalSpawner.Spawn` ветвится на flock-путь до инстанцирования.

> [!bug] Побочно починен латентный boundary-баг
> `PlaceOnGrid` раньше кормил `GetCell` **нескорректированной** позицией — то есть проверял одну клетку, а занимал другую.
> Симптом не проявлялся только потому, что до сих пор никто не запрашивал размещение у самой границы сетки.

> [!danger] Внутри фабрики сразу после инстанцирования доступны только `Awake`-инициализированные поля
> Откат стаи чистит занятость через `created.Movement.ClearNodes()`, а **не** через `created.Occupancy`.
> `AnimalFacade._occupancy` присваивается в `Start()`, который на момент синхронного `InstantiatePrefabForComponent`
> внутри `AnimalFactory` ещё не отработал → `Occupancy` был бы `null` ровно тогда, когда он нужен откату.
> `AnimalMovement._unitOccupancy` присваивается в `Awake()` и потому доступен. Правило общее, не только про курицу.

### §2.7 — визуал ×0.75 при мердже курицы

`MergeAnimationConfig._cloneScaleMultiplier` (0.75) применяется и к клону, и к оригиналу.
`MergeScaleAnimator.GrowBy`/`UndoGrowBy` переименованы в `ScaleBy`/`UndoScaleBy` (метод больше не только «растит»),
добавлен `Resync(Vector3)` — закрыт SUGGESTION прошлой фичи про дрейф `_logicalScale`.

> [!important] Два порядковых инварианта, которые нельзя переставлять
> 1. **`_targetAnimal.ScaleAnimator?.Resync(...)` обязан быть ПОСЛЕДНИМ выражением `MergeCommand.RestoreVisualState`**,
>    после `MergeView.UndoVisuals(...)`. Внутри `UndoVisuals` отрабатывает `ElephantMergeAttribute.Undo()`, который делит
>    `_logicalScale`; ресинк, выполненный раньше, оставил бы слона на `preMergeScale / 1.5`.
> 2. **Клона надо ужимать ДО `MergeAppearAnimation.Begin`.** `Begin` немедленно зовёт `CollapseScaleAndAttachGlow`,
>    который перезатирает `localScale`. Вызов `ScaleBy(multiplier, 0f, null)` снапит масштаб и одновременно записывает
>    `_logicalScale`, поэтому появляющаяся анимация стартует уже от правильной величины.

### §2.9 — шанс контратаки в данные

По образцу [[2026-06-27-fox-dodge-stats]]: новый `Code/Data/Animals/HedgehogStats.cs`
(`_ownerCounterChance: 100` / `_inheritedCounterChance: 50`, `[Range(0,100)]`), ассет `HedgehogStats.asset` сконвертирован
in-place (GUID ассета не менялся → ссылка в `AnimalDatabase` цела). `CounterAttack` стал value-agnostic:
`int successChance` вместо `bool isOwner`. `HedgehogFacade` читает `AnimalDatabase`, `HedgehogMergeSkill` хранит `_inheritedChance`.

### §2.1–2.5 — балансные числа · ❌ ЗАПИСЬ НЕВЕРНА, см. аддендум 2026-08-02

Здесь было записано: «Правки применены через `SerializedObject`, чтобы память редактора и диск совпадали:
ёж `_tilesPerMove` 3→1; лиса `_ownerDodgeChance` 50→80, `_inheritedDodgeChance` 30→50 (+ константы-дефолты в `FoxStats.cs`
подняты синхронно); курица `_health` 100→25 и `_tilesPerMove` 2→3; птеродактиль `_health` 200→350, `_tilesPerMove` 3→5».

**Проверка диска 2026-08-02 показала, что не сохранилась ни одна из этих правок.** Реально изменились
только константы в `FoxStats.cs` — обычная правка текста файла, а не запись в ассет.

### Попутная чистка

`AnimalFactory` избавлена от мёртвого DI: удалены `SetMergeGrid` (из интерфейса и реализации), поля `_mergeGrid`/`_gridManager`
и ctor-параметр `[Inject(Id = GridIdentifier.MergeGrid)] IGridManager`. Правок инсталлера не потребовалось —
`Container.Bind<IAnimalFactory>().To<AnimalFactory>().AsSingle()` резолвит ctor-аргументы через контейнер.
Закрыт старый `//todo Temp, delete later`.

## Почему так

- **Размер юнита и футпринт операции — разные понятия.** Смешать их означало бы переопределить курицу как один
  большой юнит и обесценить её мердж-скилл. Отдельный `ChickenFlock` стоит один файл и оставляет механику нетронутой.
- **Атомарность дешевле отката.** Ветвление до инстанцирования избавляет от необходимости корректно уничтожать
  наполовину созданную стаю — а корректное уничтожение в этом проекте исторически проблемное место.
- **Один алгоритм поиска места вместо двух.** `HasCellFor` и `PlaceOnGrid` расходились в трактовке границ; пока
  проверка и размещение были разным кодом, такое расхождение было лишь вопросом времени.
- **Концепт-док — источник истины для чисел, заметки Decisions — для структуры.** Отсюда форма правки конфликта:
  аддендум в старую заметку, а не её отмена.
- **Value-agnostic способности.** `CounterAttack`, как и `Dodge` до него, не должен знать про политику owner/inherited —
  это знание принадлежит фасаду животного.

> [!warning] `PoolExhaustedRule` + курица в стартовом пуле = потенциальный софт-лок
> Сейчас софт-лока **нет**: стартовый пул в `Main Scene` — `[Cheetah, Fox, Elephant]`, курицы там нет, а на фазах
> подкрепления правило неактивно. Но `HasFreeCellFor(Chicken)` стал строго жёстче (требует полный блок 2х2),
> поэтому при добавлении курицы в стартовый пул застрявшая курица навсегда заблокирует кнопку Battle на уровне 1.
> Перед таким изменением нужен escape-hatch или правило гарантированного места.

## Проверка

- Unity Console — ноль ошибок компиляции.
- EditMode, затронутые сьюты — **87/87 passed**. Новых тестов не писали, действует **TESTS PAUSED**;
  в `AbilityTestMocks` сделан компиляционный шим `bool isOwner` → константы шанса.
- Предсуществующие падения, не связанные с этой работой: `AnimalAttackPostAbilityTests`, `RetreatAbilityTests`,
  `TargetPositionCalculatorTests`.
- **Не проверено в Play Mode:** спавн курицы на тесной сетке и визуал ×0.75 при мердже — нужен ручной прогон.

## Затронутые файлы

**Новые:** `Assets/Code/Animals/ChickenFlock.cs`, `Assets/Code/Data/Animals/HedgehogStats.cs`.

**Изменены (код):**
`Assets/Code/Abilities/CounterAttack.cs`;
`Assets/Code/Animals/AnimalSpawner.cs`;
`Assets/Code/Animals/Facades/{AnimalFacade, HedgehogFacade}.cs`;
`Assets/Code/Animals/Merge/Commands/MergeCommand.cs`;
`Assets/Code/Animals/Merge/MergeAttributes/ElephantMergeAttribute.cs`;
`Assets/Code/Animals/Merge/MergeSkills/IMergeSkill.cs`;
`Assets/Code/Animals/Vfx/Config/MergeAnimationConfig.cs`;
`Assets/Code/Animals/Vfx/MergeScaleAnimator.cs`;
`Assets/Code/Data/Animals/FoxStats.cs`;
`Assets/Code/GridPathfinding/GridManager.cs`;
`Assets/Code/Infrastructure/Factories/Animals/{AnimalFactory, IAnimalFactory}.cs`.

**Изменены (тесты, только компиляционный шим):**
`Assets/Code/Tests/EditorTests/AttackAndDamageSystem/IntegrationTests/AoEAttackIntegrationTests.cs`;
`Assets/Code/Tests/EditorTests/AttackAndDamageSystem/RegressionTests/AttackAndDamageRegressionTests.cs`;
`Assets/Code/Tests/EditorTests/AttackAndDamageSystem/UnitTests/CounterAttackAbilityTests.cs`;
`Assets/Code/Tests/EditorTests/Helpers/AttackSystem/AbilityTestMocks.cs`.

**Изменены (ассеты):**
`Assets/Resources/MergeAnimationConfig.asset`;
`Assets/Settings/Animals/Stats/{ChickenStats, FoxStats, HedgehogStats, PterodactylStats}.asset`.

**Документация:** `AgentsDocs/Specifications/06_Balance_Sync_With_Concept_Doc.md` (статус + §7),
`AgentsDocs/Specifications/README.md`, [[2026-06-27-fox-dodge-stats]] (аддендум).

---

## Addendum 2026-08-02 — числа не доехали до диска; два процента отменены

Структурные решения этой заметки (стая как футпринт операции, `ChickenFlock`, атомарный спавн,
`TryFindFreePlacement`, вынос шансов в `HedgehogStats`, value-agnostic `CounterAttack`)
**остаются в силе полностью**. Поправки касаются только чисел.

**1. Ни одна балансная правка §2.1–2.5 не сохранилась на диск.** Прямое чтение
`Assets/Settings/Animals/Stats/*.asset` 2026-08-02: ёж оставался `_tilesPerMove: 3`, курица `100 / 2`,
птеродактиль `200 / 3`, лиса `50 / 30`. `SerializedObject` пишет в память редактора, и project-wide
`SaveAssets` из параллельной сессии выписал поверх своё in-memory состояние — тот же механизм,
что однажды обнулил `_glowMaterial` в `MergeAnimationConfig.asset`.

> **Правило:** после балансной правки ассета **перечитать значение с диска**, а не доверять
> успешному `ApplyModifiedProperties()`. Надёжнее — править YAML текстом и звать
> `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)`.

Переприменено и проверено (диск + `SerializedObject`): ёж `_tilesPerMove` 1, курица `25 / 3`,
птеродактиль `350 / 5`.

**2. Пункт «в конфликте заметка vs концепт-док по числам побеждает док» был сформулирован
слишком широко.** Док противоречит **сам себе**: таблица животных и страница «Abilities»
расходятся по процентам срабатывания скиллов. Решение владельца 2026-08-02 — **по процентам
главная «Abilities»**, по HP / дамагу / скорости / размеру — таблицы. Следствия:

- Додж лисы — **50 / 30**, а не 80 / 50. Ассет всё это время был прав; откачены константы
  `FoxStats.cs` (80/50 → 50/30). Правка §2.2 была ошибкой спеки.
- Контратака ежа — **50 / 50**, а не 100 / 50. `HedgehogStats.asset._ownerCounterChance 100 → 50`,
  `HedgehogStats.cs DefaultOwnerCounterChance 100 → 50`.

⚠️ Требует плейтеста: ёж не атакует сам, поэтому при 50% контратаки половину столкновений
не делает ничего.

Подробности и полная сверка всех девяти юнитов с доком — `06_Balance_Sync_With_Concept_Doc.md` §8.
См. также [[2026-06-27-fox-dodge-stats]] (аддендум 2026-08-02).
