# Решение: Level Sets — прямые ссылки на префабы вместо строковых путей + Editor-тулза подмены наборов

**Дата:** 2026-09-12
**Статус:** принято, реализовано (модель данных + Editor-тулза + валидатор + рантайм-подмена). Не
закоммичено на момент записи. Фаза E (снятие legacy `LevelDataBase`) сознательно не выполнена.

## Контекст

Задача: геймдизайнер должен уметь создать новый набор уровней со своими врагами/параметрами,
сохранить его в отдельную папку, назначить стандартным и подменять наборы — в том числе на живой
игре в Play Mode, без перезапуска редактора.

Что в проекте определило решение сильнее самой формулировки:

- **Уровни грузятся строго через `Resources.Load`.** `LevelFactory` резолвит текущий уровень по
  строковому пути (`AssetPath.LEVELS + index`), а `LevelDataBase.asset` лежит в `Resources/`.
  Любая подмена набора «на ходу» обязана попасть в тот же механизм загрузки, иначе `LevelFactory`
  продолжит грузить старый набор до перезапуска.
- **`ILevelFactory` — существующий интерфейс с семью потребителями.** Правка сигнатуры каскадом
  тронула бы все семь; правка *реализации* без изменения сигнатуры — нет.
- **Две ловушки сетки, которые рантайм не проверяет вообще** (см. «Почему так») — валидатор
  дизайн-тулзы должен ловить их до того, как дизайнер соберёт стадию, потому что игра сама
  промолчит.
- **`GridManager.GetFootprint` и `_cells` ищутся рефлексией** двумя существующими валидаторами
  (`RecipePreflight`, `AnimalPrefabValidator`) — любой рефакторинг `GridManager` обязан сохранить
  эти поля/методы `private` под теми же именами.
- **`GameBootstrapper.Awake()` входит в FSM раньше, чем Zenject зовёт `Initialize()`** — тот же
  капкан, что уже фигурировал в `AnimalSelectionService` и `AnimalRosterService`
  ([[2026-08-01-animal-selection-and-stats-panel]], [[2026-09-08-animal-unlock-progression]]).
- **Смена набора не перезагружает сцену** — значит DI-контейнер и все `AsSingle`-биндинги,
  сделанные из старого набора, продолжают жить.

## Рассмотренные варианты

1. **Подпапки `Resources/Levels/<SetName>/` + строковые пути в `LevelDataBase`.**
   ❌ Отклонено: нет проверки битых ссылок (опечатка в пути — молчаливый `null` в рантайме), нет
   подмены набора в рантайме без переписывания путей и `Resources.UnloadUnusedAssets`, и все
   префабы ВСЕХ наборов обязаны физически лежать в `Resources/` — раздувает билд и подсказывает
   дизайнеру плодить копии в неправильном месте.
2. **`LevelSet` с прямыми ссылками на префабы `ExtendedLevel` + единственный
   `LevelSetLibrary.asset` в `Resources/`.** ✅ Принято. Zenject-биндинг ссылается на библиотеку;
   Unity сама разрешает битую ссылку (красный warning в инспекторе) вместо тихого `null` в
   рантайме; префабы уровней могут лежать где угодно на диске — зависимость от ассета в
   `Resources/` сама протаскивает их в билд.
3. **Отдельный `Editor`-asmdef под новую тулзу.** ❌ Отклонено — в проекте такого нет ни для одной
   из существующих Editor-тулз (`AnimalPrefabBuilder`, `BattleSceneBuilder`); все 12 файлов
   `Code/Editor/LevelSets/` целиком под `#if UNITY_EDITOR`, консистентно с прецедентом.
4. **Подмена набора через `ProjectContext` / статический синглтон.** ❌ Отклонено: `ValidateCurrentSceneSetup()`
   оставляет `ProjectContext(Clone)` в сцене и ломает следующий вход в Play Mode
   (см. память агента `zenject_validate_leaves_projectcontext`); `LevelSetRuntimeBridge` резолвит
   ТОЛЬКО через `SceneContext` живой сцены.
5. **`IPreBattleConfigProvider` вместо прямого `AsSingle`-биндинга `PreBattleConfig`**, чтобы
   подмена набора меняла и ростер союзников, а не только уровни. ❌ Отклонено на этой итерации:
   `AllySpawnService`/`AnimalRosterService`/`MinAllyCountRule` конструируют `PreBattleConfig`
   напрямую в конструкторах, эти три сигнатуры используются существующими тестами, а TESTS PAUSED
   запрещает их переписывать ради фичи. Ограничение задокументировано и показано владельцу в
   HelpBox окна.
6. **`LevelSetProvider` как `IInitializable` с кэшем набора, посчитанным один раз.** ❌ Отклонено —
   тот же капкан порядка инициализации: `LoadLevelState.Enter()` может вызваться раньше
   `Initialize()`. Библиотека грузится лениво в геттере, по прецеденту `AnimalRosterService`.
7. **Не проверять пересечение футпринтов врагов и оставить как есть.** ❌ Отклонено — именно эта
   тишина стоила ручной сборки уровней раньше; валидатор существует затем, чтобы поймать это до
   плейтеста.

## Принятое решение

**Модель данных** (`Assets/Code/Levels/`, namespace `Code.Levels`):

- `LevelSet` (ScriptableObject) — `_displayName`, `_description`, `_tutorialLevels`, `_levels`
  (прямые ссылки на префабы `ExtendedLevel`), `_preBattleConfig` (опционально).
- `LevelSetLibrary` (ScriptableObject, `Assets/Resources/LevelSetLibrary.asset`) — `_sets` +
  `_defaultSet`. **Единственный ассет, который грузится через `Resources.Load`.**
- `ILevelSetProvider` / `LevelSetProvider`, `ILevelSetSwitchService` / `LevelSetSwitchService`,
  забиндены в `MainSceneInstaller` (`SceneContext`).
- `LevelFactory` инжектит `ILevelSetProvider` вместо `IAssetProvider`; интерфейс `ILevelFactory`
  не менялся → все семь потребителей скомпилировались без правок. `EnsureLoaded()` сравнивает
  `_activeSet != provider.ActiveSet`, чтобы после подмены фабрика не отдавала старый набор из кэша.
- Стандартный набор `Assets/Settings/Levels/Standard/StandardLevelSet.asset` зеркалит legacy
  `LevelDatabase.asset` **побитово**: те же GUID префабов, тот же порядок `Level_1..Level_4`,
  0 tutorial-уровней.

**Editor-тулза** (`Assets/Code/Editor/LevelSets/`, 12 файлов, namespace тот же паттерн, каждый
целиком в `#if UNITY_EDITOR`). Меню `Tools/AnimalMerge/Level Sets` и
`Tools/AnimalMerge/Validate Level Sets`: Create / Duplicate / Register / Remove From Library /
Set As Default, полное редактирование дерева Набор → Уровень → Стадия → Враг, превью стадии
делегировано существующему `BattleSceneBuilder`, миграция legacy `LevelDatabase`.

**Валидатор** — правила L00–L17, переиспользуют существующие `ValidationFinding`/`FindingSeverity`
из `Code.Editor.AnimalPrefabBuilder` (тот же прецедент общей инфраструктуры находок, что и
`AnimalPrefabValidator`/`RecipePreflight`).

**Подмена на живой игре** (Фаза D) — `LevelSetRuntimeBridge` резолвит через `SceneContext`
(НЕ `ProjectContext`, без `ValidateCurrentSceneSetup`). `SwitchTo` отбивает пустой набор **до**
любых разрушительных действий и сбрасывает прогресс на уровень 1.

## Почему так

> [!danger] Ловушка сетки №1 — занятость клеток и визуал расходятся молча
> `GridManager.GetOccupiedCellsInternal` первым делом клампит якорь через
> `AdjustPositionToFitBounds`, а `EnemySpawnService.SpawnEnemyAtPosition` ставит модель по **сырому**
> якорю (`GetCell(gridPosition).WorldPosition` + чисто визуальный офсет `Utilities.GetMovementOffset`).
> Пример: враг 2×2 на x=7 при `Width=8` логически занимает x=6..7, но визуально стоит на x=7 —
> ни ошибки, ни варнинга. Валидатор ловит это правилом **L10**.

> [!danger] Ловушка сетки №2 — пересечение футпринтов проходит тихо
> `SpawnEnemyAtPosition` проверяет только `null` якорной клетки и **никогда не зовёт**
> `CanPlaceUnit` → два врага с пересекающимися футпринтами оба заспавнятся, второй перетрёт
> занятость первого. Тишина в консоли. Валидатор ловит это правилом **L11**.

> [!warning] `UnitFootprint` вынесен из `GridManager`, а не переписан заново
> Математика футпринта перенесена из `GridManager` **вербатим** (прецедент — так же вынесли
> `AnimalFootprints` в [[2026-09-08-animal-unlock-progression]]). Оба приватных метода `GridManager`
> стали однострочными делегациями. Эквивалентность **доказана** дифференциальными прогонами, а не
> заявлена: 374 544 случая на ревью + 21 600 после оптимизации буфера — 0 расхождений, включая
> порядок клеток. `GridManager.GetFootprint` и `_cells` **остались `private`** — их ищут рефлексией
> `RecipePreflight:231`, `AnimalPrefabValidator:661`, `GridManagerEditorAccess:67`; публичными их
> сделать — значит молча превратить оба валидатора в no-op. Побочный эффект: `GridManager` получил
> переиспользуемый буфер `_footprintPositions`, чтобы `CanPlaceUnit` внутри цикла A* не аллоцировал
> список на каждый вызов.

> [!danger] Подмена набора обязана сбрасывать прогресс на уровень 1 — иначе игра встаёт насмерть
> Без сброса в более коротком наборе `LevelFactory.LoadCurrentLevel()` уходит за границу массива →
> `Create()` отдаёт `null` → `LoadLevelState.Enter()` делает ранний return → игра зависает, выход
> только рестартом. `BootstrapState` страхует лишь старт приложения, а не подмену на ходу. По
> результатам ревью `SwitchTo` дополнительно отбивает пустой набор **до** любых разрушительных
> действий — плохой набор больше не выжигает прогресс и юнитов раньше проверки.

> [!warning] Per-set `PreBattleConfig` меняет уровни, но не ростер, до повторного входа в Play Mode
> Биндинг `AsSingle` резолвится один раз за жизнь контейнера, а сцена при подмене не
> перезагружается. «Правильный» `IPreBattleConfigProvider` не введён — три ctor-сигнатуры
> (`AllySpawnService`/`AnimalRosterService`/`MinAllyCountRule`) заморожены существующими тестами
> под TESTS PAUSED. Показано дизайнеру HelpBox’ом в окне тулзы.

- **`LevelSetProvider` — не `IInitializable`**, библиотека грузится лениво в геттере (прецедент
  `AnimalRosterService`): `LoadLevelState.Enter()` может вызваться раньше, чем Zenject позовёт
  `Initialize()`.
- **Фаза E выполнена в тот же день, после явной приёмки владельцем.** Снесены `LevelDataBase.cs`,
  `Assets/Resources/LevelDatabase.asset`, константы `LEVELS_DATABASE`/`LEVELS`/`TUTORIAL_LEVELS`
  в `AssetPath.cs` и `LevelSetLegacyMigration.cs` вместе с кнопкой «Migrate Legacy LevelDatabase».
  🔴 **Поправка к плану:** план утверждал, что legacy-константы читает только `LevelFactory`, и
  это устарело к моменту реализации — последним потребителем `LevelDataBase` и констант
  `TUTORIAL_LEVELS`/`LEVELS` оказалась сама миграция в тулзе. Снос без неё не скомпилировался бы.
  Миграция одноразовая и уже отработана (стандартный набор создан и проверен), поэтому удалена
  целиком, а не законсервирована. Четыре префаба уровней переехали `AssetDatabase.MoveAsset` в
  `Assets/Settings/Levels/Standard/Levels/` — GUID сохранены, все 4 ссылки набора живы; пустые
  `Resources/Levels` и `Resources/TutorialLevels` удалены. **4 варнинга L17 ушли.**

## Проверка

- Стандартный набор `StandardLevelSet.asset` сверен с `LevelDatabase.asset` побитово: те же GUID,
  тот же порядок `Level_1..Level_4`, 0 tutorial-уровней — нумерация уровней и сохранённый прогресс
  игроков не сдвинулись.
- Эквивалентность `UnitFootprint` доказана дифференциальными прогонами против исходных приватных
  методов `GridManager`: 374 544 случая на ревью + 21 600 после оптимизации буфера, 0 расхождений
  (включая порядок возвращаемых клеток).
- Валидатор на стандартном наборе после Фазы E: **L17 больше не возникают**, остались 6 варнингов
  **L18** (подменные враги уровня 1, см. ниже) и 3 варнинга L14 (предсуществующая аномалия
  нумерации стадий) — ошибок нет.
- Play Mode после Фазы E: `Level_4` инстанцирован (`id=Level_4`, 1 стадия), T-Rex заспавнился,
  0 ошибок в консоли. `Resources.Load<GameObject>("Levels/Level_1")` больше не резолвится — так и
  должно быть, уровни грузятся по прямой ссылке из набора.
- ⚠️ **TESTS PAUSED соблюдён** — новых тестов не писалось; поведение подмены набора в Play Mode
  проверялось вручную через окно тулзы.

## 🔴 Пустой пикер Unity для компонентных полей (найдено и исправлено после приёмки)

Геймдизайнер не смог выбрать врага: пикер «Select Animal Facade» показывал только `None`.

**Причина не в тулзе.** `AssetDatabase.FindAssets("t:<тип>")` возвращает **0** для любого
компонентного типа в этом проекте — не только для абстрактного `AnimalFacade` (11 наследников), но
и для конкретного `t:ChickenFacade`, и для `t:ExtendedLevel`/`t:Level`. Штатный `ObjectField`
строит список ровно из этого типового индекса, поэтому **любое** поле с типом-компонентом
невыбираемо. Проблема предсуществующая: тот же пустой пикер в штатном инспекторе
`LevelStageConfig_1..4`, а существующие 4 стадии были заполнены драг-н-дропом — он работает, потому
что проверяет фактический тип, а не индекс. ScriptableObject-типы (`t:LevelStageConfig` → 5,
`t:LevelSet` → 2) индексируются нормально.

**Решение:** `PrefabComponentCatalog` (скан префабов, у которых искомый компонент на **корне**) +
`PrefabComponentField` (свой дропдаун с группировкой по папке, плюс собственная обработка
драг-н-дропа через `DragAndDrop`). Скан ленивый, по открытию списка — кэшировать нечего, список
открывает пользователь. Заменены оба сломанных поля: враг стадии (`AnimalFacade`) и привязка
существующего уровня (`ExtendedLevel`).

## Правило L18 — враг с `MergeTarget` мерджится игроком

Все три `Enemy*_Temp` префаба сидели на ветке `PlayerAnimalFacade` и несли компонент `MergeTarget`,
ровно как игровой `Chicken`. Настоящие враги — `Pteranodon`, `T-Rex`, `Velociraptor` — `MergeTarget`
не несут. На **уровне 1 стандартного набора стоят шесть `EnemyChicken_Temp`**, то есть игрок может
обращаться с врагами уровня 1 как с целью мерджа.

> [!important] Правило кейится на `MergeTarget`, а НЕ на типе фасада
> Первая версия L18 ругалась на «не `EnemyAnimalFacade`». Это **неверно** и противоречит
> задокументированному правилу проекта (см. Index.md: «сторона юнита — runtime-принадлежность, а НЕ
> compile-time тип фасада; один и тот же префаб играет и за союзника, и за врага»). Переиспользование
> игрового префаба как врага — намеренный дизайн, само по себе дефектом не является. Реальный
> признак — наличие `MergeTarget`, потому что именно он делает юнит целью мерджа. Правило
> переложено на него; тип фасада остался в тексте находки как контекст.

**Решение владельца (2026-09-12):** `EnemyCheetah_Temp` и `EnemyElephant_Temp` удалены — на них не
ссылался ни один ассет, ни строчка кода (кроме осиротевших записей в таблице высот
`HealthBarSetupTool.HeightOffsets`, тоже убраны). `EnemyChicken_Temp` **оставлен намеренно**: он
стоит на уровне 1, и валидатор продолжает давать по нему 6 варнингов L18 — это ожидаемо.

Рантайм удалением не затронут: `AnimalFactory` грузит `Resources.LoadAll` только по
`AssetPath.Animals` (`Prefabs/Animals` — игровые префабы), а враги спавнятся по прямым ссылкам из
`LevelStageConfig`. Папку `Prefabs/Enemies` перебирают только editor-тулзы
(`HealthBarSetupTool`, `AnimalPrefabPaths`, `AnimalPortraitBaker`) — они найдут на два префаба меньше.

## Не сделано (вынесено владельцу)

- **`EnemyChicken_Temp` оставлен как враг уровня 1** — он несёт `MergeTarget`, и 6 варнингов L18 на
  стандартном наборе никуда не денутся, пока префаб не заменят настоящим врагом (см. выше).
- `LevelStageConfig_2/3/4` нумеруют себя 2/3/4, хотя каждый — стадия 1 своего уровня (3 варнинга
  L14 валидатора) — предсуществующая аномалия данных, влияет только на `Debug.Log`, не трогалась.
- Per-set `PreBattleConfig` не подхватывается «на ходу» (см. предупреждение выше) — нужен
  `IPreBattleConfigProvider`, который требует правки трёх замороженных тестами конструкторов.

## Затронутые файлы

**Новые**
- `Assets/Code/Levels/{LevelSet,LevelSetLibrary,ILevelSetProvider,LevelSetProvider,ILevelSetSwitchService,LevelSetSwitchService}.cs`
- `Assets/Code/GridPathfinding/UnitFootprint.cs`
- `Assets/Code/Editor/LevelSets/{LevelPrefabWriter,LevelSetCatalog,LevelSetEditorDrawer,LevelSetFactory,LevelSetFolders,LevelSetLegacyMigration,LevelSetLibraryAccess,LevelSetPaths,LevelSetPreviewBridge,LevelSetRuntimeBridge,LevelSetValidator,LevelSetWindow}.cs`
- `Assets/Resources/LevelSetLibrary.asset`
- `Assets/Settings/Levels/Standard/StandardLevelSet.asset`

**Изменённые**
- `Assets/Code/Framework/Code/AssetPath.cs`
- `Assets/Code/Framework/Code/Factories/Levels/LevelFactory.cs`
- `Assets/Code/Framework/Code/MainSceneInstaller.cs`
- `Assets/Code/GridPathfinding/GridManager.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`

## Связанное

- [[2026-08-13-battle-scene-builder]] — превью стадии переиспользует `BattleSceneBuilder` целиком
- [[2026-08-17-single-game-grid-deployment-zone]] — `DeploymentZone`, гейт по границе зоны, тот же
  класс ловушек «рантайм не проверяет выход за границу»
- [[2026-09-08-animal-unlock-progression]] — прецедент выноса `AnimalFootprints` из `GridManager`
  и паттерн сервиса без состояния/без `IInitializable`
