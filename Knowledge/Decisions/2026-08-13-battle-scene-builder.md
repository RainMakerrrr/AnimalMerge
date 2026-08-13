# Решение: Battle Scene Builder — превью боевой сцены в Edit Mode через засев приватного `GridManager._cells`

**Дата:** 2026-08-13
**Статус:** принято, реализовано (15 файлов, только Editor-код; рантайм не менялся). ⚠️ Не импортировано редактором — `.meta` не сгенерированы, пункт меню в живом Unity не проверен

## Контекст

Геймдизайнеру и художнику нужно видеть уровень целиком, не входя в Play Mode: сетки, союзники
стартового пула, враги стадии — на своих местах, с возможностью подвигать камеру и посмотреть
композицию. В Edit Mode ничего этого нет: уровень существует только как данные
(`LevelStageConfig` + `PreBattleConfig`), а расстановкой занимаются рантайм-сервисы.

Отдельная сложность: в `Assets/Resources/Levels/Level_1..4.prefab` лежит **только** `ExtendedLevel`
с массивом `_stages` — ни сетки, ни геометрии. `LevelStageConfig` тоже не содержит сетки, только
список врагов (`_prefab`, `_gridPosition`, `_isBoss`). Сетки живут исключительно в `Main Scene`
(`GameGrid` 8×10 в (0,0,0), `MergeGrid` 8×2 в (0,0,−3)). Значит, «отрисовать уровень» — это не
инстанцировать префаб, а воспроизвести работу трёх рантайм-сервисов.

## Рассмотренные варианты

1. **`[ExecuteAlways]` на `GridManager` / спавнерах.** Отклонено: правит рантайм-код ради
   редакторской фичи, тянет за собой Awake/Start-семантику всей цепочки. В `Assets/Code` нет ни
   одного `[ExecuteAlways]`/`[ExecuteInEditMode]` — прецедента заводить не стали.
2. **Продублировать алгоритм раскладки в редакторском коде.** Отклонено: `TryFindFreePlacement`
   с корректировкой по границам, `GetOccupiedCells`, `GetFootprint` — вторая копия немедленно
   разъедется с рантаймом, и превью начнёт врать именно там, где нужно точнее всего.
3. **Звать `GridManager.Rebuild(w, h, cellSize)`.** Отклонено: внутри `ClearCells()` зовёт
   `Object.Destroy`, что в Edit Mode запрещено, плюс выставляет `_hasRuntimeOverride`.
4. **Заспавнить клетки самому и отдать их живому `GridManager` через приватное поле.** ✅ Принято.

## Принятое решение

### Ядро — засев `GridManager._cells` рефлексией

`EditorGridSpawner` спавнит клетки зеркалом `GridManager.InitializeGrid` (`_cellPrefab` читается
через `SerializedObject`), а затем `GridManagerEditorAccess.TrySeedCells` кладёт готовый
`GridCell[,]` в приватное поле живого компонента сцены. После этого **настоящие**
`TryFindFreePlacement` / `CanPlaceUnit` / `GetOccupiedCells` / `SetOccupied` / `HasCellFor`
работают в Edit Mode ровно как в рантайме.

> [!danger] Без засева инструмент молча врёт, а не падает
> `EnsureGridInitialized()` (`GridManager.cs:84`) делает ранний выход при `!Application.isPlaying`,
> поэтому `_cells == null` и `GetCell(...)` всегда `null`. Дальше по цепочке:
> `CanPlaceUnit` (`:303`) итерирует **пустой** список клеток и возвращает `true` для ЛЮБОЙ позиции →
> `TryFindFreePlacement` (`:699`) отдаёт `(0,0)` на первой же итерации → все союзники схлопываются
> в один угол. Ошибок в консоли при этом ноль.

Рефлексия, а не `SerializedObject`: поле объявлено `private GridCell[,] _cells`, двумерный массив
Unity не сериализует. Весь доступ изолирован в одном классе `GridManagerEditorAccess`; при
переименовании поля — явный `Debug.LogError`, а не тихий мусор. `Clear()` возвращает `_cells = null`
— ровно pre-Awake состояние, поэтому следующий Play Mode инициализирует сетку штатно.

### Что можно звать у заспавненных животных в Edit Mode

`AnimalMovement._unitOccupancy` присваивается в `Awake`, которого в Edit Mode нет, поэтому
`SetCurrentNode` / `FillNodes` / `ClearNodes` / `SetNewNode` дают NRE. Безопасны только
`Place(Vector3)` и `RotateToTarget(Vector3)`.

Следствие: `GridManager.PlaceOnGrid` целиком непригоден (внутри `SetCurrentNode`) — он **инлайнен**:
`TryFindFreePlacement` → `GetCell` → `Place` → `SetOccupied`. Поворот в рантайме ставит
`AnimalMovement.Start()`, в Edit Mode он не выполняется — инструмент зовёт `RotateToTarget` сам.
`AnimalFacade.Movement` и `.Type` — сериализованные поля, доступны без Awake; `AnimalFacade.Occupancy`
присваивается в `Start` → в Edit Mode `null`.

### Трекинг созданного

Всё уходит под единый root `[AnimalMerge] SceneBuilder` с маркер-компонентом `BattleSceneBuildRoot`.
Поиск — обходом `GetRootGameObjects()`, а не по полю окна, поэтому превью переживает domain reload.
`Clear()` удаляет root и обнуляет `_cells` у всех `GridManager` сцены — то, что стояло в сцене
до запуска, не трогается никогда. Весь `Build` идёт одной collapsed Undo-группой.

### Guard от Play Mode и от сохранения сцены

`BattleSceneBuilderSceneGuard` (`[InitializeOnLoad]`) снимает превью на
`PlayModeStateChange.ExitingEditMode` и на `EditorSceneManager.sceneSaving`, вызывая полноценный
`Clear()` (а не просто удаляя объект — иначе на диске/в Play Mode останется засеянный `_cells`).

> [!warning] Оставленное превью ломает игру двумя разными способами
> 1. Клетки лежат на слое `PathNode`, союзники на `Animal` — ровно те два слоя, по которым райкастит
>    `AnimalMovement.TryPlace` (`AnimalMovement.cs:225`). Мёртвое превью сломало бы и мердж, и выбор
>    животного в Play Mode.
> 2. `BattleSceneBuildRoot` компилируется только под `#if UNITY_EDITOR` — сохранённая с превью сцена
>    дала бы missing script в плеерном билде.

Сцена инструментом **никогда не сохраняется**, `EditorSceneManager.SaveScene` не вызывается.

### Тумблера «Build Grid» нет намеренно

Он был в первой версии и оказался мёртвой опцией: `Build` всегда начинается с `Clear()`, который
обнуляет `_cells`, а без сетки размещать юнитов негде. Переиспользовать «уже стоящую» сетку
невозможно по построению — опция убрана, а не «починена».

## Почему так

- **Паритет с рантаймом важнее чистоты.** Рефлексия в редакторском коде — плата за то, что логика
  раскладки существует в одном экземпляре. Альтернатива (вторая копия алгоритма) разъезжается молча.
- **Инструмент воспроизводит рантайм как есть, включая известные расхождения, а не чинит их.**
  Гейт `HasCellFor` по таблице `GridManager.GetFootprint` (Fox/Hedgehog там 1×2 при фактическом
  `_unitSize` 1×1 в префабах), ветка `ChickenFlock` 2×2 на 4 курицы, порядок пула, паритет врагов с
  `EnemySpawnService.SpawnEnemyAtPosition` (те же клетки, слой `Enemy` только на корне, имя с
  `_{index}`). Превью, которое «лучше» игры, бесполезно как превью.
- **Весь Editor-код целиком под `#if UNITY_EDITOR`.** `Assets/Code/CodeBase.asmdef` имеет
  `includePlatforms: []`, отдельного Editor-asmdef в проекте нет — иначе папка `Assets/Code/Editor/`
  уезжает в плеерную сборку. Тот же приём, что в `AnimalPrefabBuilder`.
- `ApplyStats` врагам не зовётся: нужен `AnimalDatabase`, к визуальной композиции отношения не имеет.

## Проверка

- Компиляция: **0 ошибок** — проверено в обход редактора (временный csproj + msbuild с
  определённым `UNITY_EDITOR`), потому что Unity MCP в сессии был недоступен.
- Прочитаны с диска и подтверждены фактические данные, на которых стоит инструмент:
  `PreBattleConfig._startingPool` = `[Cheetah, Fox, Elephant]`; `LevelStageConfig_1` = 6×
  `EnemyChicken_Temp` в клетках (1..6, 9); `_unitSize` союзников — Cheetah 1×2, Deer 1×2,
  Elephant 2×2, Chicken/Fox/Hedgehog 1×1, `_direction` у всех North, `_zOffset` −0.5 у Deer и Elephant.
- ❌ **Не проверено в живом редакторе**: файлы не импортированы, `.meta` не сгенерированы, пункт
  меню `Tools/AnimalMerge/Scene Builder` не открывался, Play Mode после Build/Clear не отсматривался.

## Затронутые файлы

Все 15 — новые, в `Assets/Code/Editor/BattleSceneBuilder/`, namespace `Code.Editor.BattleSceneBuilder`:
`BattleSceneBuilderWindow` · `BattleSceneBuilder` · `BattleSceneBuildRequest` · `BattleSceneBuildReport` ·
`BattleSceneRoot` · `BattleSceneBuildRoot` · `BattleSceneBuilderPaths` · `BattleSceneBuilderSceneGuard` ·
`BattleSceneReferenceResolver` · `EditorGridSpawner` · `GridManagerEditorAccess` · `EditorUnitSpawner` ·
`AllyPrefabCatalog` · `EditorAllyPlacer` · `EditorEnemyPlacer`.

**Существующих файлов не изменено ни одного** — рантайм-код нетронут.

## Открытые пункты (ревью, не применено)

- Занятость клеток в превью не видна: `GridCell.UpdateVisual` — no-op в Edit Mode, `_meshRenderer`
  присваивается в `Awake`.
- `BattleSceneRoot.Find()` зовётся каждый `OnGUI`.
- `PreBattleConfig` резолвится по хардкод-пути, а не из `BattleInstaller._preBattleConfig`.
- `BattleSceneReferenceResolver` при переименовании полей молча падает на эвристику по `Height`.
- Семантика пула отличается от `AllySpawnService.RequestSpawn`: рантайм при неудаче стопорится,
  инструмент пропускает тип и идёт дальше.
- `EditorUnitSpawner.DirectionVector` — третья копия `DirectionToVector3`.

## Связанное

- [[2026-08-02-animal-prefab-validator]] — прецедент Editor-тулзы целиком под `#if UNITY_EDITOR`
- [[2026-08-01-chicken-flock-and-balance-sync]] — `TryFindFreePlacement` как единственный алгоритм
  поиска места и правило «внутри фабрики доступны только `Awake`-инициализированные поля»
- [[2026-07-30-grid-config]] — размеры сеток в `GridConfig`, ссылка в Inspector ради edit-mode
