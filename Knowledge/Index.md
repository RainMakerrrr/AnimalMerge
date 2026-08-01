# AnimalMerge — Knowledge Base
> Загружается автоматически при старте каждой сессии. Обновляй после значимой работы.

## Проект
Turn-based mobile battler с механикой merge. Unity 2022.3.62f3 · C# · Zenject · UniTask · DOTween · TextMeshPro.
Платформы: Android & iOS. Основная ветка разработки: `iteration_2/Alexandr/Develop`.

## Архитектура
- **Clean Architecture**: Presentation → Application → Domain → Infrastructure
- **DI**: Zenject — никаких Singleton
- **Async**: UniTask везде — никаких Coroutines в новом коде
- **Паттерны**: State (BattleStateMachine), Command (Merge undo), Factory, Facade (AnimalFacade), Observer (SignalBus), Strategy (ITutorialStep, IMergeHintTargetResolver, IBattleReadinessRule)
- **Детали**: `AgentsDocs/ProjectArchitecture.md`

## Карта модулей
| Модуль | Путь | Статус |
|---|---|---|
| Battle State Machine | Code/Battle/StateMachine/ | Активен |
| Pre-Battle фаза | Code/Battle/PreBattle/ · UI/ · Signals/ | Активен — спавн союзников, readiness-правила |
| Tutorial | Code/Tutorial/ | Активен — каркас шагов `ITutorialStep`, merge-hint рука, обвязка сцены закоммичена |
| GridPathfinding (A*) | Code/GridPathfinding/ | Активен — multi-size units |
| Animal System | Code/Animals/ | Активен |
| Шейдеры / VFX | Assets/Shaders/ · Code/Animals/Materials/ · Code/Animals/Vfx/ | Активен — IridescentSweep (loot shine + дух смерти + merge-свип), SpiritGhost, GridCellBorder, система merge-анимаций (`MergeAnimationConfig`) |
| Ability System | Code/Abilities/ | Активен (Dodge, CounterAttack, Retreat) |
| Data / ScriptableObjects | Code/Data/ | Активен |
| Framework (bootstrap + верхнеуровневый FSM) | Code/Framework/ | Активен — переработан 2026-08-01: флоу победы/поражения, `CampaignVictoryState`, сервисы Progression/GameRestart, окна результата |
| Сброс боя | Code/Battle/Services/ | Активен — `IBattleResetService` (уничтожение юнитов + `BattleFlowController.Cleanup()`) |
| Pathfinding/, NewPathfinding/ | Code/Pathfinding/ | DEPRECATED — удалить позже |

## Важные файлы
- `AgentsDocs/ProjectArchitecture.md` — полная документация модулей
- `AgentsDocs/ZenjectPatterns.md` — паттерны DI и SignalBus
- `AgentsDocs/Battle_State_System_Setup_Guide.md` — setup battle системы
- `AgentsDocs/Specifications/` — спецификации систем (`README.md` — индекс; 01–05 по pathfinding/merge/attack/timings, `2026-06-14-agent-pipeline-design.md`)
- `AgentsDocs/CodeStyle.md` — соглашения по коду
- `.claude/CLAUDE.md` — инструкции для агента

## Последние изменения
_Обновляй при каждой сессии._

- [2026-08-01] **Флоу победы/поражения переработан**: после последнего уровня — экран финальной Victory (`CampaignVictoryState` → «ANIMALS WON» + кнопка RESTART, полный рестарт кампании), Lose-панель получила кнопку TRY AGAIN вместо мгновенного авто-рестарта. Новые `ICampaignProgressService`, `IGameRestartService`, `IBattleResetService` (`Code/Battle/Services/`), `WindowType.CampaignVictory`, `DefeatWindowView`/`CampaignVictoryWindowView` + `GameResultPresenter`. `LevelFactory` больше не заворачивает индекс уровня по `%` (кампания молча зацикливалась), `CollectablesData.ResetAmount()` разделён на `RevertLevelAmount()`/`Clear()`, `GameStateMachine` шлёт `StateChangedSignal` до `Enter()`, `RestartButton` переведён на сервис. Обвязка `Canvas.prefab` и `Main Scene.unity` выполнена
- [2026-08-01] **Общая система анимаций при мердже**: новые типы в `Code/Animals/Vfx/` — `MergeAnimationConfig` (ScriptableObject со всеми тюнингами, ассет в `Resources/`), `MergeScaleTween`, `RendererMaterialOverlay`, `MergeAppearAnimation`, `MergeScaleAnimator`. Слон растёт скейл-анимацией с перелётом, слот-детали (хвост/рога — везде, где есть `MergeVisualSlot`) появляются со scale-in параллельно с glow-пассом, клон курицы получает ту же appear-анимацию, окрас гепарда визуально не изменился. Побочно починены три предсуществующих бага: undo слона в цепочке A→B→C, взаимное затирание двух `RendererMaterialOverlay` на одном рендерере, `Deer.prefab` `SlotMergeAttribute._type: 0 → 2` (рога не появлялись никогда)
- [2026-08-01] **Туториальная рука для мерджа + каркас туториалов**: новый модуль `Code/Tutorial/` — `TutorialRunner` идёт по `List<ITutorialStep>`, шаг сам ждёт свой триггер внутри `RunAsync` и возвращает `bool`; `MergeHintStep` показывает `TutorialHandView` (DOTween-слайд с одного животного на другое) до первого `AllyMergedSignal`, пара берётся по индексу спавна из `IUnitTracker`; прогресс — PlayerPrefs, ключ `Tutorial.{stepId}`; конфиг `Settings/TutorialConfigs/MergeHintStepConfig.asset` (уровень 1, режим `SpawnOrder`, индексы 0 → 1)
- [2026-08-01] **Glow-свип при мердже Cheetah**: волна света проезжает по всему животному → под ней накладывается материал гепарда → волна уезжает обратно → эффект убирается полностью. `IridescentSweep.shader` получил явный world-режим развёртки (`_SweepWorldAxis.w` = флаг, `_SweepWorldOrigin`), новый драйвер `Code/Animals/Vfx/MergeSweepGlow.cs` (обычный C#-класс, не MonoBehaviour) гонит фазу на `UniTask.Yield`, `CheetahMergeAttribute` переписан с сохранением синхронных `Apply`/`Undo` через `.Forget()`
- [2026-07-31] **Смерть курицы** (союзной и вражеской): анимация падения уже была нарисована, но не подключена (`IsDead` вёл в `jump`) — доразведены существующие стейты `down`/`wing_down` через `Any State`, `IsDead` стал Bool; плюс новый эффект вылета духа — `Code/Animals/Vfx/` (`DeathSpiritSpawner`/`DeathSpiritView`), дух = запечённая через `BakeMesh` копия меша курицы с материалом `GhostSpirit.mat`. `EnemyChicken_Temp.prefab` делит тот же контроллер, поэтому получил разводку автоматически — доставлен только компонент
- [2026-07-31] Эффект **«loot shine»** — переливающаяся волна света по объекту: шейдер `Assets/Shaders/IridescentSweep.shader` (Built-in RP, unlit аддитивный оверлей **вторым слотом материала**, анимация целиком на `_Time.y`, ни строки C#), материал `Code/Animals/Materials/IridescentSweepOverlay.mat` повешен на `Fox Tail` в `Resources/Prefabs/Animals/Cheetah.prefab`
- [2026-07-31] Массовый спавн союзников заменён на кнопки **AddAnimal** (одно животное за клик) и **Battle** с пульсацией; логика спавна вынесена из UI в прикладной слой — новые модули `Code/Battle/PreBattle/`, `Code/Battle/UI/`, `Code/Battle/Signals/`, конфиг `PreBattleConfig`
- [2026-07-30] Размеры игровых полей (`MergeGrid` 8×2 / `GameGrid` 8×10) вынесены из хардкода `GridManager` в ScriptableObject-конфиги `GridConfig` (`Code/GridPathfinding/Config/`, ассеты в `Settings/GridConfigs/`) — `IGridManager` не менялся
- [2026-06-27] Вероятности Dodge для Fox вынесены из хардкода в данные — подкласс `FoxStats : AnimalStats` (Owner 50% / Inherited 30%, `[Range(0,100)]`), ассет `FoxStats.asset` сконвертирован на новый тип; `Dodge` теперь принимает `int successChance` вместо `bool isOwner`
- [2026-06-27] Текстовая анимация (popup) при кросс-тайп мердже животных — `MergePopupView`/`MergePopupController` (Code/Animals/UI/), событие `MergeTarget.Merged`, строки per-animal в `AnimalConfig.MergeInfo`
- [2026-06-27] Подсветка доступных ходов животного при удержании клика/тапа (BFS-радиус по TilesPerMove) — новая сущность `MoveRangeHighlighter`
- [2026-05-27] Setup Codex, обновлены настройки проекта
- [2026-05-27] Добавлено рисование gizmos для AOE атаки
- [2026-05-27] Реализован cheetah texture changer (нужны доработки)
- [2026-05-27] Удалён кастомный MCP сервер, установлен официальный Unity MCP

## Текущее состояние
_Что сейчас в работе._

Ветка `iteration_2/Alexandr/Develop` активна. Последняя завершённая задача — **переработка флоу победы/поражения** (`Framework/`, UI-окна результата). Параллельно завершена **общая система анимаций при мердже**; merge-свип гепарда и туториальная рука закоммичены (`9f7ed831`, `fd66f1d9`), обвязка сцены туториала внутри коммита — модуль активен.

### Флоу победы/поражения — `Assets/Code/Framework/Code/Infrastructure/`
- **Верхнеуровневый FSM**: `BattleEndState` → Victory ведёт в `WinState` (`Level++`, save, аналитика), оттуда либо прежний Win-баннер с 2-секундным авто-переходом, либо — если кампания пройдена — `CampaignVictoryState` (экран «ANIMALS WON», ждёт RESTART). Defeat ведёт в `LoseState`, который теперь ждёт TRY AGAIN (авто-рестарт удалён). Пер-левельный Win не менялся.
- **Три новых сервиса**: `ICampaignProgressService` (`TotalLevels`, `IsCampaignCompleted`, `ResetToFirstLevel()`), `IGameRestartService` (`RetryCurrentLevel()` / `RestartCampaign()`, гейты по `_stateMachine.ActiveState` отсекают двойные клики и клики из неверного состояния), `IBattleResetService` в `Code/Battle/Services/` (уничтожение союзников + `BattleFlowController.Cleanup()`).
- **Семантика кнопок**: TRY AGAIN — текущий уровень, `Progress.Level` не трогается, шлётся `ReportLevelRestarted()`. RESTART — `Progress.Level = 1`, `Collectables.Clear()`, `Save`, уничтожение выживших союзников, сброс battle-состояния.
- **UI**: `WindowType.CampaignVictory` + слот в `WindowHolder`/`WindowPool`; тупые `DefeatWindowView` / `CampaignVictoryWindowView` (только `event`), клей — `GameResultPresenter : IInitializable, IDisposable`. **Новых сигналов не добавлено** — команды идут внутрь через интерфейсы. Биндинги в `MainSceneInstaller` + одна строка в `BattleInstaller`. Обвязка `Canvas.prefab` и `Main Scene.unity` выполнена.
- ⚠️ **`LevelFactory.LoadCurrentLevel()` больше НЕ заворачивает индекс по `%`.** Пока обёртка стояла, состояние «все уровни пройдены» было непредставимо — кампания молча зацикливалась. Теперь выход за диапазон = `Debug.LogError` + `CurrentLevel = null` + ранний выход из `LoadLevelState.Enter()`. Появился `ILevelFactory.TotalLevelsCount` с ленивым `EnsureLoaded()`. Парная защита — `BootstrapState` при `IsCampaignCompleted` зовёт `ResetToFirstLevel()` + `Save` (релонч на экране победы иначе делал игру непроходимой).
- ⚠️ **`CollectablesData.ResetAmount()` разделён на `RevertLevelAmount()` (`Amount -= LevelAmount`) и `Clear()` (`Amount = 0`).** Старое имя лгало: на пути рестарта кампании метод фактически ничего не сбрасывал.
- ⚠️ **`GameStateMachine.Enter<T>()` шлёт `StateChangedSignal` ДО `state.Enter()`** — иначе вложенный переход (`WinState.Enter()` → `CampaignVictoryState`) приходит подписчикам в обратном порядке.
- **`RestartButton` переведён на `IGameRestartService.RetryCurrentLevel()`** — раньше входил в `LoadLevelState` напрямую и тащил живых юнитов и состояние стадий в новый уровень.
- Переименования сериализованных полей (`WindowHolder`, `MainSceneInstaller`) сделаны с `[FormerlySerializedAs]`.

### Система merge-анимаций — `Assets/Code/Animals/Vfx/`
- **Пять маленьких независимых кусков, без фреймворка (KISS)**: `Config/MergeAnimationConfig.cs` (ScriptableObject, секции Elephant Growth / Appear / Glow, `OnValidate` клампит `_glowBandsPerBody ≤ 1/(1+2·_glowBandMargin)`), `MergeScaleTween` (статик-lerp на `UniTask.Yield` + `AnimationCurve` через `LerpUnclamped`), `RendererMaterialOverlay` (материальная логика гепарда, обобщённая параметром `Material skinOverride`), `MergeAppearAnimation` (композиция «glow ‖ scale-in»), `MergeScaleAnimator` (MonoBehaviour-арбитр рутового скейла).
- **Арбитр скейла живёт на ЦЕЛИ, а не в атрибуте.** `MergeScaleAnimator` добавляется в рантайме на корень цели (`GetComponent ?? AddComponent`) и владеет `_logicalScale`. Три эффекта одним ходом: владелец на активной цели → ловушка деактивации источника не действует; два `ElephantMergeAttribute` в одном кадре не дерутся за трансформ (второй домножает логический скейл); undo имеет одно место отмены и порядко-независим. Правок префабов не требует.
- **Доставка конфига — `[Inject] ConstructVisual([InjectOptional] MergeAnimationConfig)` в `VisualMergeAttribute`**, биндинг `FromScriptableObjectResource` в `CurrentGameInstaller` (прецедент `AnimalDatabase`). Метод-инъекция достаёт до НЕАКТИВНЫХ детей префаба, т.к. `AnimalFactory.Create` использует `InstantiatePrefabForComponent`. Все потребители null-гардят конфиг → отсутствие биндинга деградирует к старому мгновенному поведению.
- **Рост слона чисто косметический** — логическое значение переключается синхронно, интерполируется только трансформ. Занятость сетки — из `AnimalMovement._unitSize`, pathfinding — из `UnitSize`/`Direction`, радиус атаки — из статов. Единственный логический потребитель `localScale` — `MergeStateSnapshot`. Undo — мгновенный снап.
- ⚠️ **Правило: оверлей, который может делить рендерер с другим писателем, обязан восстанавливать состояние УСЛОВНО** — по сверке с тем, что он сам записал (`RendererState.AppliedSharedMaterials`, `ReferenceEquals` на glow-инстанс), а не безусловно из снапшота. Реальный сценарий: слот активируется первым, `CollectPaintableRenderers` гепарда подхватывает уже активный рендерер рогов, `Dispose()` слотового оверлея затирал гепардовый скин. В `Dispose()` порядок — restore-then-`DisposeGlow`.
- ⚠️ **Кривые с перелётом — это данные, а не код.** Первая версия ассета уехала с плоским smoothstep, и «перелёт» не воспроизводился, хотя код его поддерживал. Итог — 3-ключевой back-out `(0,0,out 2.2) → (0.65,1.12) → (1,1)`; пик 1.56× слон, 1.114× деталь. **При вынесении тюнинга в SO дефолты ассета — часть фичи, проверять отдельно от кода.**
- ⚠️ **`_appearStartScale` = 0.05, а не 0** — нулевой стартовый скейл схлопывал коллайдеры клона курицы, и свежий клон был невыбираемым для следующего мерджа все 0.35 с.
- **Побочно исправлены три предсуществующих бага**: undo слона в цепочке A→B→C (возвращал `1.5S` вместо `S`), взаимное затирание оверлеев, `Deer.prefab` `SlotMergeAttribute._type: 0 → 2` (`_sourceType` слота «Antlers 02» = 2 (Deer), рога не появлялись никогда, мердж писал warning).
- **Закрыты два SUGGESTION прошлой фичи**: `MergeSweepGlow` теперь `: IDisposable`; `OnValidate` гарантирует `_glowBandsPerBody * (1 + 2 * _glowBandMargin) ≤ 1`.

### Туториалы — модуль `Assets/Code/Tutorial/` (закоммичен, обвязка сцены в коммите)
- **Прикладной слой поверх Battle**: читает состояние (`IUnitTracker`, `IPersistentProgressService`) и сигналы, командует только своим view. Геймплейный код о туториале не знает — единственное исключение — новый `AllyMergedSignal`, который фаерит `MergeTarget.ExecuteMergeDirectly`.
- **Расширяемость**: новый шаг = один класс `ITutorialStep` (`Id` · `CanRun` · `UniTask<bool> RunAsync(CancellationToken)`) + одна строка биндинга в `TutorialInstaller`. `TutorialRunner` править не надо.
- **Старт последовательности — по `PreBattlePhaseStartedSignal` с `IsLevelStart == true`, а НЕ в `Initialize()`.** Zenject зовёт `IInitializable.Initialize` из `MonoKernel.Start`, когда `GameBootstrapper.Awake` только вошёл в `BootstrapState` — `PlayerProgress` там может быть ещё `null`.
- **Выбор пары — по индексу спавна** (`SpawnOrderTargetResolver`, default). Координаты сетки непригодны: позиция зависит от `UnitSize` ранее поставленных юнитов и от `GridConfig`.
- **Прогресс — `ITutorialProgressService` на PlayerPrefs** (ключ `Tutorial.{stepId}`), чтобы не трогать `Framework/`. Сброс прохождения — удалить ключ `Tutorial.MergeHint`.
- ⚠️ **UniTask-адаптер DOTween (`ToUniTask`) в проекте НЕДОСТУПЕН.** `UniTask.DOTween.asmdef` гейтит всё за `UNITASK_DOTWEEN_SUPPORT`, а `versionDefines` эмитит define только для UPM-пакета `com.demigiant.dotween`; здесь DOTween из Asset Store → `DOTweenAsyncExtensions` не компилируется. Ждать твин надо через `UniTask.WaitUntil(...).SuppressCancellationThrow()`.
- ⚠️ **Отписка от `SignalBus` должна быть синхронной относительно teardown контейнера** — отсюда `CancelAwareSignalSubscription<T>`.
- Компромиссы (осознанные): `MergeCommand.Undo()` не фаерит контр-сигнал → отменённый мердж засчитывается; шаг завершается по ЛЮБОМУ `AllyMergedSignal`; биндится ровно один `MergeHintStepConfig` `AsSingle`.

### Merge-свип гепарда (закоммичен, `9f7ed831`)
- **Шейдер `Assets/Shaders/IridescentSweep.shader`** — явный world-режим развёртки: `_SweepWorldAxis` (компонента `w` = флаг) + `_SweepWorldOrigin`. Объектная развёртка для целого животного непригодна — у каждого рендерера свой пивот/поворот/масштаб. При `w = 0` математика бит-в-бит прежняя.
- **Драйвер `MergeSweepGlow.cs`** — обычный C#-класс (НЕ MonoBehaviour), владеет одним рантайм-инстансом материала на всех рендереров цели, каждый кадр пересчитывает объединённый world-AABB, фазу гонит на `UniTask.Yield(PlayerLoopTiming.Update, ct)`.
- ⚠️ **Ловушка: владельца merge-атрибута деактивируют через кадр после `Apply`** (`MergeTarget.cs:113` → `:124`). Корутина бросит исключение, очистку нельзя вешать на `OnDisable` (только `OnDestroy`), `UniTask.Yield`/`Delay` деактивацию переживают.
- ⚠️ **Правило: рантайм-оверлей нельзя снапшотить как «оригинал»** — цепочка мерджей Cheetah A → B → C писала в рендерер `[cheetah, null]` навсегда. Починено фильтром по шейдеру `_glowMaterial.shader` в захвате оригиналов.
- Ограничение Unity (принято): при длине массива материалов больше `subMeshCount` лишний (glow) материал перерисовывает только ПОСЛЕДНИЙ сабмеш.

### Ранее закоммичено (`fdb244a9` loot shine, `6d5356aa` дух смерти, `e9d1cc10` пре-батл фаза)
- **Смерть курицы** — анимация падения уже была нарисована и не подключена; доразведены `Any State → down`/`wing_down`, `IsDead` сменён с Trigger на Bool. Эффект духа (`DeathSpiritSpawner` + `DeathSpiritView`) — запечённая через `BakeMesh` копия меша животного, шейдер `SpiritGhost.shader`. **Визуальный вид на экране НЕ проверен.**
- ⚠️ **Открытое окно Animator молча откатывает правки контроллера, сделанные через API** — закрывать окно перед правкой, проверять `git diff`.
- **Loot shine** — `IridescentSweepOverlay.mat` на `SkinnedMeshRenderer` `Fox Tail` внутри `Cheetah.prefab`. `Assets/Animals compilation/Prefabs/Fox tail Variant.prefab` НЕ трогать — общий для 10 префабов.
- **Пре-батл фаза** — обвязка сцены выполнена пользователем и закоммичена.

### ⚠️ Состояние рабочего дерева
- **По фиче merge-анимаций не закоммичено**: `Assets/Code/Animals/Vfx/{MergeScaleTween, RendererMaterialOverlay, MergeAppearAnimation, MergeScaleAnimator}.cs`, `Vfx/Config/MergeAnimationConfig.cs`, `Assets/Resources/MergeAnimationConfig.asset` (**untracked** — значения кривых, `_appearStartScale` и ссылка `_glowMaterial` уедут только вместе с ним), изменённые `VisualMergeAttribute`, `ElephantMergeAttribute`, `SlotMergeAttribute`, `MergeVisualSlot`, `CheetahMergeAttribute`, `IMergeSkill`, `MergeStateSnapshot`, `AnimalFacade`, `ChickenFacade`, `MergeSweepGlow`, `CurrentGameInstaller`, `Deer.prefab`.
- **По фиче флоу победы/поражения не закоммичено** (это отдельная фича, коммитить раздельно от merge-анимаций): `Assets/Code/Framework/**` (`GameStateMachine`, `BootstrapState`, `LoadLevelState`, `LoadProgressState`, `WinState`, `LoseState`, `LevelFactory`/`ILevelFactory`, `CollectablesData`, `WindowHolder`/`WindowPool`/`WindowType`, `RestartButton`, `MainSceneInstaller`, `GameStateDebugger`, `GameBootstrapper`), новые `Code/Battle/Services/{IBattleResetService, BattleResetService}`, `Framework/.../Services/GameRestart/`, `Services/Progression/`, `UI/Windows/`, `CampaignVictoryState`, `GameResultPresenter`, изменённые `Assets/Scenes/Main Scene.unity`, `Assets/Resources/Canvas.prefab`, `BattleInstaller.cs`.
- ⚠️ **Общий редактор с параллельной сессией портит ссылки в ассетах**: ссылка `_glowMaterial` в `MergeAnimationConfig.asset` один раз обнулилась — project-wide `SaveAssets` выписал на диск in-memory состояние, где она уже была null. Переназначена и проверена.

Открытые вопросы:
- ⚠️ **Рестарт посреди боя**: юниты теперь реально уничтожаются, а `PlayerTurnState`/`TurnExecutor` не отменяют ход по `Exit`. `TurnExecutor` пропускает уничтоженных юнитов между итерациями, но уже начатый `await unit.AttackInstance.Attack(...)` может повиснуть или залогировать missing-reference. Путь был racy и до правки; полное решение — async `ResetForNewRun()` + `CancellationToken` в battle turn states, вынесено за скоуп.
- `LoseState` меняет `Progress.Collectables` без `Save` — штраф в монетах теряется при релонче. Предсуществующее, но новый retry-флоу делает это достижимым намного чаще.
- `UnitTracker.Reset()` логирует два `Debug.LogWarning`, один с полным `StackTraceUtility.ExtractStackTrace()` — теперь это штатный пользовательский путь (каждый Try Again / Restart) на мобильном таргете.
- `RestartCampaign()` не шлёт аналитику, тогда как `RetryCurrentLevel()` зовёт `ReportLevelRestarted()`.
- `LoadProgressState` остаётся недостижимым состоянием (прогресс грузится в конструкторе `PersistentProgressService`) — флоу намеренно не перепроводили.
- Кнопки Try Again / Restart — обычные `UISprite`-прямоугольники с TMP-подписями, арт не подставлен.
- Тесты: действует политика **TESTS PAUSED**. По туториалу, шейдерам, эффекту смерти, merge-свипу и merge-анимациям тесты не писались. Нет теста на `PreBattleHudPresenter`.
- Туториал ещё не проверен в рантайме.
- Не применённые SUGGESTION по merge-анимациям: `MergeScaleAnimator._logicalScale` дрейфует и не ресинхронизируется с трансформом (нужен `Resync(Vector3)`); `GrowScaleMultiplier` — балансная величина слона в общем конфиге, по конвенции проекта место в `ElephantStats : AnimalStats`, плюс `FallbackScaleMultiplier = 1.5f` дублирует дефолт ассета; `SlotMergeAttribute.Apply` рано выходит ДО `StopAppearAnimation()`, если на новой цели нет слота; `MergeScaleAnimator._cancellation` не диспозится на happy path; `MergeAppearAnimation.Begin` разыменовывает `_config` без null-гарда; `OnValidate` клампит `_glowBandsPerBody` разрушительно; три новых `GetComponent`/`AddComponent` вне `Awake`.
- Не применённые SUGGESTION по merge-свипу: `_bandMargin = 0.5` уводит полосу полностью за силуэт до свопа; `_cts` не диспозится на happy path.
- Не применённый SUGGESTION по шейдеру: overdraw — лишний аддитивный проход полного меша в transparent-очереди ломает early-Z на тайловых мобильных GPU.
- `AnimalHealth.Die()` по-прежнему на корутине (`StartCoroutine(DestroyWithDelay())`) — нарушение правила «только UniTask», предсуществующее.
- 4 предсуществующих варнинга ассет-пака в `ChickentwoLayerController` и 5 ошибок параметров того же контроллера из `EnemySpawnService.cs:86`.
- Не применённые SUGGESTION пре-батл фазы: `BattleButtonView._hideUntilReady` по умолчанию `false`; `PreBattleHudPresenter` берёт конкретный `StartBattleService`, а не интерфейс; `AllySpawnService.RequestSpawn` делает dequeue до получения результата спавна.
- Дублирование стартового пула (`PreBattleConfig.StartingPool`) и пула подкреплений (`AnimalSpawner._animalTypes`) — намеренное, объединение вне скоупа.
- Предсуществующие красные тесты (НЕ регрессии): ~16 `TargetPositionCalculatorTests`, ~9 `AnimalAttackPostAbility`/`Retreat`, 4 `HealthBarViewTests`, 3 `DamagePopup*`. В Console остаётся предсуществующий NRE `UnityEditor.Graphs.Edge.WakeUp` от аниматор-контроллеров.

## Ключевые решения
Подробности в `Knowledge/Decisions/`. Краткий список:
- [2026-06-27] Merge popup показывается ТОЛЬКО при cross-type мердже (когда реально выдан новый скилл) и ключуется по типу SOURCE (потреблённого) животного; событие `Merged` гейтится флагом `grantedNewSkill` в `ExecuteMergeDirectly`, минует undo → `Decisions/2026-06-27-merge-popup.md`
- [2026-06-27] Подсветка хода вынесена в отдельную сущность (SRP) — `AnimalMovement` не нагружали; drag vs hold различается порогом смещения в пикселях, IInputService не трогали → `Decisions/2026-06-27-move-range-highlighter.md`
- [2026-06-27] Per-animal тюнинг-данные способностей (вероятности Dodge для Fox) хранятся в подклассе `AnimalStats` (`FoxStats`), а НЕ в общем `AnimalConfig` — данные живут со статами животного и не «протекают» в конфиги остальных; `Dodge` стал value-agnostic (`int successChance`), owner/inherited решается на стороне вызова → `Decisions/2026-06-27-fox-dodge-stats.md`
- [2026-07-30] `GridConfig` доставляется в `GridManager` ссылкой в Inspector, а НЕ через Zenject-инжект — размеры нужны в `OnDrawGizmos` в edit-mode, до `Awake` и до создания DI-контейнера; `IGridManager` намеренно не расширялся (blast radius 3 файла вместо 29) → Decisions/2026-07-30-grid-config.md
- [2026-07-31] Пре-батл фаза разложена по слоям: команды идут внутрь через интерфейсы (view → `PreBattleHudPresenter` → `IAllySpawnService`), факты наружу через `SignalBus`; новое условие старта боя = один класс `IBattleReadinessRule` + одна строка биндинга; стартовый пул вынесен в `PreBattleConfig` → Decisions/2026-07-31-pre-battle-phase-architecture.md
- [2026-07-31] Гейт старта боя считается по high-water mark союзников за фазу, а не по живому счётчику — иначе мердж двух союзников в одного (ядро механики) вешал софт-лок с обеими мёртвыми кнопками; `IUnitTracker` стал реактивным (`PlayerUnitsChanged`) → Decisions/2026-07-31-min-ally-count-high-water-mark.md
- [2026-07-31] Эффект свечения сделан аддитивным оверлеем во **втором слоте материала** (базовый lit-материал не трогается) и целиком на `_Time.y` — без C#-контроллера, Zenject-биндинга и сигналов; `_SweepTiling` меряется в «полосах на объект» через `_SweepAxisExtent`, иначе полоса выходит шире меша и волна вырождается в мигание → Decisions/2026-07-31-iridescent-sweep-shader.md
- [2026-07-31] Смерть курицы: перед тем как «делать анимацию», выяснилось, что она уже нарисована и не подключена — правка свелась к доразводке существующих стейтов (`Any State → down`/`wing_down`, `IsDead` → Bool + `canTransitionToSelf = false`); дух сделан запечённой через `BakeMesh` копией меша самого животного, а не отдельным артом, поэтому переносится на любое животное; `BakeMesh` и `Renderer.material` создают объекты, которые НЕ собираются GC — без `Destroy` в `OnDestroy` каждая смерть течёт → Decisions/2026-07-31-chicken-death-spirit.md
- [2026-08-01] Merge-свип: синхронные `Apply`/`Undo` сохранены, асинхронная последовательность запускается через `.Forget()` (иначе `ApplyAsync` расползся бы на 7 файлов и сломал `MergeCommand.Execute() : bool`), а таймлайн крутится на `UniTask.Yield(PlayerLoopTiming.Update)`, потому что владельца атрибута деактивируют через кадр после `Apply`; развёртка полосы вынесена в явный world-режим шейдера (`_SweepWorldAxis.w`), рантайм-оверлей отфильтровывается при захвате «оригинальных» материалов → Decisions/2026-08-01-merge-sweep-glow.md
- [2026-08-01] Туториал вынесен в модуль-надстройку `Code/Tutorial/` над Battle: шаг = один класс `ITutorialStep` + строка биндинга, `RunAsync` сам ждёт свой триггер и возвращает `bool`; последовательность стартует по `PreBattlePhaseStartedSignal` (в `Initialize()` `PlayerProgress` ещё `null`); пара животных выбирается по индексу спавна `IUnitTracker`, а не по координатам сетки; завершение шага — по новому `AllyMergedSignal`, а не по событиям `MergeTarget`; прогресс — отдельный PlayerPrefs-сервис, чтобы не трогать `Framework/` → Decisions/2026-08-01-tutorial-hand-merge-hint.md
- [2026-08-01] Система merge-анимаций: арбитр рутового скейла `MergeScaleAnimator` живёт на ЦЕЛИ, а не в атрибуте — цель остаётся активной (ловушка деактивации источника не действует), два `ElephantMergeAttribute` в одном кадре домножают общий `_logicalScale` вместо драки за трансформ, undo становится порядко-независимым и структурно чинит предсуществующий баг цепочки A→B→C; тюнинг вынесен в `MergeAnimationConfig` (`FromScriptableObjectResource` + `[InjectOptional]` метод-инъекция, достающая до неактивных детей префаба), все потребители null-гардят его; оверлей, делящий рендерер с другим писателем, восстанавливает состояние условно — по сверке с тем, что записал сам → Decisions/2026-08-01-merge-animation-system.md
- [2026-08-01] Флоу победы/поражения: «кампания пройдена» — вычисляемое `Progress.Level > TotalLevels`, а не сохранённый флаг; предусловием фичи стало снятие обёртки `%` в `LevelFactory.LoadCurrentLevel()` (пока она стояла, состояние было непредставимо — кампания молча зацикливалась), парная защита от релонча — сброс в `BootstrapState`; клики Try Again / Restart идут внутрь как команды через `IGameRestartService` (View → `GameResultPresenter` → сервис, новых сигналов ноль), двойные клики отсекаются гейтом по `_stateMachine.ActiveState`, а не булевым флагом; `CollectablesData.ResetAmount()` разделён на `RevertLevelAmount()`/`Clear()`, потому что имя лгало и на пути рестарта кампании метод ничего не сбрасывал → Decisions/2026-08-01-victory-defeat-flow.md
- CLAUDE.md ≤70 строк — детали в AgentsDocs/
- No Singleton — только Zenject bindings
- UniTask — новый async код только на UniTask
- Deprecated Pathfinding/ — оставлены до удаления, не трогать

## Протокол сессии
**Начало**: этот файл загружен автоматически — ничего делать не нужно.
**Конец задачи**: когда задача завершена, запусти `/update-knowledge` — скилл сам обновит этот файл.
Если пользователь говорит "готово", "задача выполнена", "закончили" → проактивно запускай `/update-knowledge`.
