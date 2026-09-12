# Решение: открытие животных — один ростер + таблица разблокировок вместо двух пулов

**Дата:** 2026-09-08
**Статус:** принято, реализовано (код + ассет + валидация в редакторе). Не закоммичено на момент
записи

## Контекст

Задача звучала так: пройденный уровень добавляет новое животное в пул; стартовый пул и пул
подкреплений равны по умолчанию и растут вместе; дизайнер настраивает, какое животное открывается
за какой уровень.

Что в проекте определило решение сильнее самой формулировки:

- **Пулов было два, и они были независимы.** `PreBattleConfig._startingPool` (кого выдают на
  уровне 1) и `PreBattleConfig._randomPool` (из чего берут подкрепления на уровнях 2+). Требование
  «равны» при такой структуре — обещание дизайнера, а не свойство системы.
- **`Progress.Level` — единственное, что игра знает о прогрессе кампании.** Инкремент делает
  `WinState` после победы, значит во время уровня N в сейве лежит `N`, а после победы — `N+1`.
- **`GameBootstrapper.Awake()` входит в стейт-машину раньше, чем Zenject зовёт `Initialize()`.**
  Тот же капкан ранее заставил `AnimalSelectionService._selectionAllowed` дефолтиться в `true`
  ([[2026-08-01-animal-selection-and-stats-panel]]).
- **Зона деплоя конечна и мала**: `GameGridConfig._width` 8 × `DeploymentZone.Depth` 2 = **16 клеток**.
  Пока стартовый ростер был из трёх животных (8 клеток), запас казался бесконечным.
- **`PoolExhaustedRule` требует ОПУСТОШИТЬ стартовый пул**, чтобы кнопка FIGHT загорелась:
  `IsSatisfied => !_phaseUsesStartingPool || _pool.Remaining == 0`. Расширение стартового пула
  с 3 животных до 6 делает это условие опасным.

## Рассмотренные варианты

1. **Оставить два массива и синхронизировать их руками / скриптом апгрейда ассета.**
   ❌ Отклонено: «равны» остаётся дисциплиной. Первая же правка одного массива без второго даёт
   рассинхрон, который не ловится ни компилятором, ни тестами, ни валидацией — только плейтестом.
2. **Один `_baseRoster` + таблица разблокировок `AnimalUnlockEntry[] _unlocks`, оба пула
   вычисляются одной функцией.** ✅ Принято. `StartingPool(level) == RandomPool(level) ==
   Resolve(level)` — рассинхрон структурно невозможен, потому что второго множества больше нет.
3. **Хранить список открытых животных в `PlayerProgress` и дописывать при победе.**
   ❌ Отклонено: требует миграции сейва и заводит второй источник истины, который может разойтись
   с конфигом дизайнера (правка `_unlocks` не догонит уже сохранённых игроков). Разблокировки
   **выводятся** из `Progress.Level` — `PlayerProgress` не тронут вообще.
4. **Правило прогрессии внутри `AllySpawnService`.** ❌ Отклонено: сервис уже отвечает за спавн,
   пул и сигналы. В проекте есть прецедент вынесения чистого правила в static-класс —
   `TurnOrderCalculator`; резолвер сделан по его образцу и тестопригоден без Unity.
5. **`AnimalRosterService` со стейтом и `IInitializable` (посчитать ростер один раз на входе
   в фазу).** ❌ Отклонено: `Initialize()` приходит позже `GameBootstrapper.Awake()`, и первый
   же запрос ростера пришёлся бы на непроинициализированный сервис. Цена отказа — аллокация
   на каждом обращении, принята сознательно.
6. **Сравнение `entry.CompletedLevel <= Progress.Level`.** ❌ Отклонено: даёт сдвиг ровно на
   уровень (животное «за прохождение уровня 1» появлялось бы уже НА уровне 1). Выбрано `<`.
7. **Гарантировать новинку не первым подкреплением, а просто оставить рандом.**
   ❌ Отклонено: при `ReinforcementsPerLevel = 1` шанс увидеть только что открытое животное — 1/4,
   и «открытие» перестаёт читаться как событие.
8. **Сделать `GridManager.GetFootprint` публичным и звать его из валидации.**
   ❌ Отклонено — см. «Почему так»: его ищут рефлексией.

## Принятое решение

**Данные.** `PreBattleConfig` держит:

- `AnimalType[] _baseRoster` с `[FormerlySerializedAs("_startingPool")]` — старое поле
  переезжает без потери данных; дефолт `Cheetah, Fox, Elephant`;
- `AnimalUnlockEntry[] _unlocks` — `[Serializable]`-класс с `[Min(1)] _completedLevel` и `_animal`;
  дефолт `{1 → Deer, 2 → Hedgehog, 3 → Chicken}`;
- `bool _guaranteeNewlyUnlockedReinforcement` (дефолт `true`).

Свойства `StartingPool` и `RandomPool` **удалены**; ссылок на них в `Assets/` не осталось.

**Правило.** `AnimalRosterResolver` (static, `Code/Battle/PreBattle/`):

- `Resolve(config, level)` = `_baseRoster` в объявленном порядке + записи `_unlocks`,
  где `CompletedLevel < currentLevel`, отсортированные `OrderBy(CompletedLevel)` (стабильно),
  с дедупликацией через `HashSet` — **первое вхождение выигрывает**;
- `ResolveNewlyUnlocked(config, level)` = животные с `CompletedLevel == level - 1`, которых
  не было в ростере предыдущего уровня.

**Доставка.** `IAnimalRosterService` / `AnimalRosterService` — обёртка над резолвером и
`IPersistentProgressService` (`CurrentLevel`, `AvailableAnimals`, `NewlyUnlockedAnimals`),
без состояния, без `IInitializable`. Биндинг — одна строка в `BattleInstaller.BindPreBattle()`.

**Потребление.** `AllySpawnService.QueueStartingPool()` и `QueueReinforcements()` читают
`AvailableAnimals`. `EnqueueNewlyUnlocked()` кладёт новинки первыми, остаток добирается
`Random.Range` по тому же ростеру. Сигнатуры `IAllySpawnService` не менялись — `PreBattleState`
и `PreBattleHudPresenter` не тронуты.

**Побочно закрытый тупик.** `PoolExhaustedRule.IsSatisfied` получил `|| !_spawnService.CanSpawn`,
а `AllySpawnService` перешёл на **FIFO со скипом заблокированного**: `IAllySpawnPool` получил
`IReadOnlyList<AnimalType> PendingAnimals` и `TryTakeAt(int index, out AnimalType)`, внутри
`Queue<AnimalType>` заменена на `List<AnimalType>`.

**Футпринты и валидация.** Таблица футпринтов вынесена из `GridManager` в
`AnimalFootprints.For / CellCount` (`Code/GridPathfinding/`). `PreBattleConfig.OnValidate` получил
`ValidateRosterFitsDeploymentZone`: для каждого достижимого уровня резолвит ростер **тем же
резолвером, что и рантайм**, суммирует `CellCount` и сравнивает с
`GridConfig.Width * DeploymentZone.Depth`.

## Почему так

> [!danger] Порядок ростера — несущий, а не косметический
> Ёмкость зоны деплоя — **16 клеток**. Футпринты: Cheetah / Fox / Deer / Hedgehog 1×2 = 2 каждый,
> Elephant 2×2 = 4, Chicken 2×2 = 4. Полный ростер уровня 4 занимает **ровно 16/16, впритык,
> с нулевым запасом**. Упаковка проходит (колонки 0, 1, 2-3, 4, 5, 6-7) только потому, что
> курица (2×2) объявлена **последней** и ей достаются две чистые колонки. Одна занятая клетка
> или одна лишняя запись в `_unlocks` — и последнее животное не разместится.
> **Правило для дизайнера: самый крупный футпринт открывается последним.**

> [!danger] `PoolExhaustedRule` был молчаливым тупиком
> `IsSatisfied => !_phaseUsesStartingPool || _pool.Remaining == 0` — бой нельзя начать, пока
> стартовый пул не опустошён, а `RequestSpawn()` при нехватке места оставлял животное в пуле.
> Если хоть одно животное не влезало, **кнопка FIGHT не загоралась никогда**, выход только через
> RESTART, и **ни одной ошибки в консоли**. Расширение стартового пула с 3 до 6 животных сделало
> этот предсуществующий риск живым — поэтому чинилось здесь, а не «когда-нибудь потом».

> [!warning] Правило и `CanSpawn` правятся ТОЛЬКО парой
> Починка одного `PoolExhaustedRule` оставила бы `RequestSpawn()`, который долбится в
> заблокированную голову очереди: кнопка Add Animal горит, нажатие ничего не делает. Починка
> одного `RequestSpawn` без правила не открыла бы FIGHT. Вместе — тихая потеря животных
> превращается в корректный скип, а FIGHT загорается, когда разместить больше нечего.

- **`<`, а не `<=`.** Запись `{1 → Deer}` означает «пройден уровень 1 → Олень доступен с уровня 2».
  Во время уровня N в сейве `Progress.Level == N`, после победы — `N+1`. Альтернативное прочтение
  сдвигает всю таблицу на уровень и не ловится ничем, кроме плейтеста.
- **Дедупликация «первое вхождение выигрывает»**, повтор → warning в `OnValidate`. Иначе животное
  из `_baseRoster`, продублированное в `_unlocks`, встало бы в ростер дважды и съело бы двойной
  футпринт в зоне деплоя.
- **Сервис без состояния.** `GameBootstrapper.Awake()` опережает `Initialize()`; кэш, заполняемый
  в `Initialize()`, был бы пуст на первом обращении. Аллокация на обращении — принятая цена.
- ⚠️ **`GridManager.GetFootprint` намеренно оставлен `private static`** и стал однострочной
  делегацией в `AnimalFootprints.For`. `RecipePreflight.ValidateAllyFootprintTable` и
  `AnimalPrefabValidator.TryGetGridFootprint` ищут его **рефлексией** через
  `BindingFlags.NonPublic | Static` — сделать его публичным значит молча превратить оба валидатора
  в no-op.
- **`ValidateRosterFitsDeploymentZone` под `EditorApplication.delayCall`**, потому что `OnValidate`
  срабатывает и во время импорта ассетов, где запросы к `AssetDatabase` бросают исключение.
  Ёмкость **считается** из `GridConfig` и `DeploymentZone.Depth`, ничего не захардкожено.
- **Миграции сейва нет.** Разблокировки выводятся из `Progress.Level`, а не хранятся;
  `PlayerProgress` не тронут, старые сейвы читаются как есть.

## Проверка

- Консоль Unity: **0 ошибок, 0 предупреждений**.
- EditMode: `AllySpawnServiceTests` + `BattleReadinessServiceTests` + `AllySpawnPoolTests` —
  **20/20** (baseline 20/20). `GridConfigTests` — **5/5** (прогнаны, т.к. тронут `GridManager`).
- `Assets/Settings/BattleConfigs/PreBattleConfig.asset` перечитан с диска после сохранения:
  `_baseRoster: 010000000300000000000000` = Cheetah / Fox / Elephant; `_unlocks` =
  `{1 → Deer, 2 → Hedgehog, 3 → Chicken}`; `_guaranteeNewlyUnlockedReinforcement: 1`.
- Итоговая раскладка: ур. 1 Cheetah/Fox/Elephant → ур. 2 +Deer → ур. 3 +Hedgehog → ур. 4 +Chicken.
- Проверка вместимости в редакторе: capacity 16, требуется 8 / 10 / 12 / 16 клеток на уровнях 1-4,
  на шиппинг-конфиге варнинг не срабатывает. Добавление разблокировки на уровень 4 даёт
  `The level 5 roster needs 17 deployment cells but 'GameGridConfig' offers 16 (8 columns x 2 rows)`.
- ⚠️ **TESTS PAUSED соблюдён** — новых тестов не писалось. `AnimalRosterResolver`,
  `AnimalRosterService`, скип заблокированной головы и capacity-варнинг **автотестами не покрыты**,
  валидировались через `execute_code` в редакторе.

## Не сделано (вынесено владельцу)

- 🔴 **`LevelDatabase.asset` перечисляет уровни как `Level_4, Level_1, Level_2, Level_3`**, а
  `LevelFactory.LoadCurrentLevel()` резолвит контент как `_levels[Progress.Level - 1]` — прогресс-
  уровень 1 грузит префаб `Level_4`. Дизайнер, пишущий `_completedLevel: 1`, фактически говорит
  «после прохождения уровня с ИМЕНЕМ Level_4». Проблема **предсуществующая**, этой задачей не
  внесена, но напрямую подрывает контракт «какое животное за какой уровень». Не трогалось
  намеренно: переупорядочивание кампании меняет геймплей.
- Нет fallback-а, если гарантированное новинкой подкрепление не влезает — награда за уровень может
  молча пропасть (на ур. 4 это курица, самый тяжёлый футпринт).
- На шиппинг-конфиге (`ReinforcementsPerLevel = 1`, флаг гарантии включён, разблокировки на 1/2/3)
  подкрепления стали **полностью детерминированными**: ветка `Random.Range` — мёртвый код
  в 4-уровневой кампании. Это реальный сдвиг баланса относительно старого `_randomPool`.
- `AvailableAnimals` / `NewlyUnlockedAnimals` аллоцируют на каждом обращении (`List` + `HashSet`
  + LINQ).
- Элементы `_unlocks` разыменовываются без null-проверок.

## Затронутые файлы

**Новые**
- `Assets/Code/Battle/Config/AnimalUnlockEntry.cs`
- `Assets/Code/Battle/PreBattle/AnimalRosterResolver.cs`
- `Assets/Code/Battle/PreBattle/AnimalRosterService.cs`
- `Assets/Code/Battle/PreBattle/IAnimalRosterService.cs`
- `Assets/Code/GridPathfinding/AnimalFootprints.cs`

**Изменённые**
- `Assets/Code/Battle/Config/PreBattleConfig.cs`
- `Assets/Code/Battle/PreBattle/AllySpawnService.cs`
- `Assets/Code/Battle/PreBattle/AllySpawnPool.cs`
- `Assets/Code/Battle/PreBattle/IAllySpawnPool.cs`
- `Assets/Code/Battle/PreBattle/Rules/PoolExhaustedRule.cs`
- `Assets/Code/GridPathfinding/GridManager.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Code/Editor/BattleSceneBuilder/{BattleSceneBuildRequest,BattleSceneBuilder,BattleSceneBuilderWindow,EditorAllyPlacer}.cs`
- `Assets/Code/Tests/EditorTests/BattleSystem/Helpers/BattleTestHelper.cs`
- `Assets/Code/Tests/EditorTests/BattleSystem/UnitTests/{AllySpawnServiceTests,BattleReadinessServiceTests}.cs`
- `Assets/Settings/BattleConfigs/PreBattleConfig.asset`

## Связанное

- [[2026-07-31-pre-battle-phase-architecture]] — слои пре-батл фазы, `IBattleReadinessRule`
- [[2026-08-01-animal-spawn-pools-and-prefab-parity]] — предыдущая (откаченная) итерация владения пулами
- [[2026-08-01-chicken-flock-and-balance-sync]] — футпринт стаи 2×2, `TryFindFreePlacement`
- [[2026-08-17-single-game-grid-deployment-zone]] — `DeploymentZone.Depth`, гейт `FitsInDeploymentZone`
