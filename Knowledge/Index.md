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
| Tutorial | Code/Tutorial/ | Активен — каркас шагов `ITutorialStep`, merge-hint рука (нужна обвязка сцены) |
| GridPathfinding (A*) | Code/GridPathfinding/ | Активен — multi-size units |
| Animal System | Code/Animals/ | Активен |
| Шейдеры / VFX | Assets/Shaders/ · Code/Animals/Materials/ · Code/Animals/Vfx/ | Активен — IridescentSweep (loot shine + дух смерти + merge-свип), SpiritGhost, GridCellBorder |
| Ability System | Code/Abilities/ | Активен (Dodge, CounterAttack, Retreat) |
| Data / ScriptableObjects | Code/Data/ | Активен |
| Framework (bootstrap) | Code/Framework/ | Стабилен — не трогать |
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

- [2026-08-01] **Туториальная рука для мерджа + каркас туториалов**: новый модуль `Code/Tutorial/` — `TutorialRunner` идёт по `List<ITutorialStep>`, шаг сам ждёт свой триггер внутри `RunAsync` и возвращает `bool`; `MergeHintStep` показывает `TutorialHandView` (DOTween-слайд с одного животного на другое) до первого `AllyMergedSignal`, пара берётся по индексу спавна из `IUnitTracker`; прогресс — PlayerPrefs, ключ `Tutorial.{stepId}`; конфиг `Settings/TutorialConfigs/MergeHintStepConfig.asset` (уровень 1, режим `SpawnOrder`, индексы 0 → 1). **Обвязка сцены — на пользователе**
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

Ветка `iteration_2/Alexandr/Develop` активна. Последняя завершённая задача — **туториальная рука для мерджа** (расширяемая система туториалов). Параллельно в дереве лежит незакоммиченный **glow-свип при мердже Cheetah** из соседней сессии.

### Туториалы — модуль `Assets/Code/Tutorial/`
- **Прикладной слой поверх Battle**: читает состояние (`IUnitTracker`, `IPersistentProgressService`) и сигналы, командует только своим view. Геймплейный код о туториале не знает — единственное исключение — новый `AllyMergedSignal`, который фаерит `MergeTarget.ExecuteMergeDirectly`.
- **Расширяемость**: новый шаг = один класс `ITutorialStep` (`Id` · `CanRun` · `UniTask<bool> RunAsync(CancellationToken)`) + одна строка биндинга в `TutorialInstaller`. `TutorialRunner` править не надо — он идёт по `List<ITutorialStep>` в порядке биндингов, пропускает пройденные и `CanRun == false`, фаерит `TutorialStepCompletedSignal`. Шаг сам ждёт свой триггер внутри `RunAsync`.
- **Старт последовательности — по `PreBattlePhaseStartedSignal` с `IsLevelStart == true`, а НЕ в `Initialize()`.** Zenject зовёт `IInitializable.Initialize` из `MonoKernel.Start`, когда `GameBootstrapper.Awake` только вошёл в `BootstrapState` — `PlayerProgress` там может быть ещё `null`. К `PreBattlePhaseStartedSignal` прогресс гарантированно загружен.
- **Выбор пары — по индексу спавна** (`SpawnOrderTargetResolver`, default), альтернатива — по `AnimalType`. Координаты сетки непригодны: `GridManager.PlaceOnGrid` берёт первую свободную клетку рядов y=0..1, позиция зависит от `UnitSize` ранее поставленных юнитов и от `GridConfig`. Стратегия выбирается по полю `Mode`, обе реализации биндятся.
- **Прогресс — `ITutorialProgressService` на PlayerPrefs** (ключ `Tutorial.{stepId}`), а не поле в `PlayerProgress`: `Framework/` не трогаем, поле в сериализуемом JSON затронуло бы `SaveLoadService`, `WinState`, `LoseState`.
- ⚠️ **UniTask-адаптер DOTween (`ToUniTask`) в проекте НЕДОСТУПЕН.** `UniTask.DOTween.asmdef` гейтит всё за `UNITASK_DOTWEEN_SUPPORT`, а `versionDefines` эмитит define только для UPM-пакета `com.demigiant.dotween`; здесь DOTween из Asset Store (`Assets/Plugins/Demigiant/DOTween/DOTween.dll`) → `DOTweenAsyncExtensions` не компилируется. Ждать твин надо через `UniTask.WaitUntil(() => !seq.IsActive() || seq.IsComplete(), ct).SuppressCancellationThrow()`.
- ⚠️ **Отписка от `SignalBus` должна быть синхронной относительно teardown контейнера.** `UniTask.WaitUntil` poll-based и не возобновляется синхронно на `Cancel()` — к следующему тику Zenject уже сделал `SignalBus.LateDispose()`, и `Unsubscribe` бросил бы. Решение — `CancelAwareSignalSubscription<T>`: отписка ровно один раз, из `Dispose()` или из `CancellationTokenRegistration`, что раньше.
- Ввод не блокируется (`raycastTarget = false`, `blocksRaycasts = false`, оверлея нет) — подсказка требует, чтобы игрок сам сделал мердж. Мир→экран считается с камерой `null` в `ScreenPointToLocalPointInRectangle`, т.к. Canvas — Screen Space Overlay.
- Компромиссы (осознанные): `MergeCommand.Undo()` не фаерит контр-сигнал → отменённый мердж засчитывается; шаг завершается по ЛЮБОМУ `AllyMergedSignal`; биндится ровно один `MergeHintStepConfig` `AsSingle` (второй merge-hint потребует ID-биндингов).
- ⚠️ **Фича не активна до обвязки сцены (на пользователе)**: на `SceneContext` в `Assets/Scenes/Main Scene.unity` добавить `TutorialInstaller`, дописать в `Mono Installers`, назначить `_mergeHintConfig` = `Settings/TutorialConfigs/MergeHintStepConfig.asset`, `_handViewPrefab` = `Assets/Prefabs/TutorialHandView.prefab`. Сброс прохождения — удалить ключ PlayerPrefs `Tutorial.MergeHint`.

### Merge-свип (параллельная сессия, не закоммичено)
- **Шейдер `Assets/Shaders/IridescentSweep.shader`** получил явный world-режим развёртки: `_SweepWorldAxis` (компонента `w` = флаг включения) + `_SweepWorldOrigin`, `_PhaseOffset` расширен с `Range(0,1)` до `Float`. Объектная развёртка для целого животного непригодна — у каждого рендерера свой пивот/поворот/масштаб. Обратная совместимость: при `w = 0` математика бит-в-бит прежняя.
- **Драйвер `Code/Animals/Vfx/MergeSweepGlow.cs`** — обычный C#-класс (НЕ MonoBehaviour). Владеет одним рантайм-инстансом материала на всех рендереров цели, каждый кадр пересчитывает объединённый world-AABB → `extent`/`origin`, фазу гонит на `UniTask.Yield(PlayerLoopTiming.Update, ct)`.
- **`CheetahMergeAttribute`**: публичный контракт не менялся — `Apply`/`Undo` синхронные `void`, последовательность через `.Forget()`. Тайминги на `[SerializeField]`. Поле `_cheetahMaterial` намеренно НЕ переименовано (Unity сериализует по имени).
- ⚠️ **Ловушка: владельца merge-атрибута деактивируют через кадр после `Apply`** (`MergeTarget.cs:113` → `:124`). Корутина бросит исключение, очистку нельзя вешать на `OnDisable` (только `OnDestroy`), `UniTask.Yield`/`Delay` деактивацию переживают.
- ⚠️ **Правило: рантайм-оверлей нельзя снапшотить как «оригинал»** — цепочка мерджей Cheetah A → B → C записывала в рендерер `[cheetah, null]` навсегда. Починено фильтром по шейдеру `_glowMaterial.shader` в `CaptureOriginalMaterials`.
- Ограничение Unity (принято): при длине массива материалов больше `subMeshCount` лишний (glow) материал перерисовывает только ПОСЛЕДНИЙ сабмеш.

### Ранее закоммичено (`fdb244a9` loot shine, `6d5356aa` дух смерти, `e9d1cc10` пре-батл фаза + обвязка сцены)
- **Смерть курицы** — анимация падения уже была нарисована и не подключена; доразведены `Any State → down`/`wing_down`, `IsDead` сменён с Trigger на Bool. Эффект духа (`DeathSpiritSpawner` + `DeathSpiritView`) — запечённая через `BakeMesh` копия меша животного, шейдер `SpiritGhost.shader`. Стоит на `Chicken.prefab` и `EnemyChicken_Temp.prefab`. **Визуальный вид на экране НЕ проверен.**
- ⚠️ **Открытое окно Animator молча откатывает правки контроллера, сделанные через API** — закрывать окно перед правкой, проверять `git diff`.
- **Loot shine** — `IridescentSweepOverlay.mat` на `SkinnedMeshRenderer` `Fox Tail` внутри `Cheetah.prefab`, виден только после мерджа Fox в Cheetah. `Assets/Animals compilation/Prefabs/Fox tail Variant.prefab` НЕ трогать — общий для 10 префабов.
- **Пре-батл фаза** — обвязка сцены выполнена пользователем и закоммичена.

Не закоммичено: весь `Assets/Code/Tutorial/`, `TutorialInstaller.cs`, `AllyMergedSignal.cs`, `Assets/Prefabs/TutorialHandView.prefab`, `Assets/Settings/TutorialConfigs/`, изменённые `MergeTarget.cs`, `BattleInstaller.cs` (туториал) + `MergeSweepGlow.cs`/`.mat`, `IridescentSweep.shader`, `CheetahMergeAttribute.cs`, `Cheetah.prefab` (merge-свип, соседняя сессия). Это две независимые задачи — коммитить раздельно.

Открытые вопросы:
- Тесты: действует политика **TESTS PAUSED**. По туториалу, шейдерам, эффекту смерти и merge-свипу тесты не писались. Нет теста на `PreBattleHudPresenter`.
- Туториал ещё не проверен в рантайме — до обвязки сцены `TutorialInstaller` не установлен.
- Не применённые SUGGESTION по merge-свипу: `_bandMargin = 0.5` уводит полосу полностью за силуэт до свопа; ничто не гарантирует `_bandsPerBody * (1 + 2 * _bandMargin) <= 1` (нужен `OnValidate`); `_cts` не диспозится на happy path; `MergeSweepGlow` имеет `Dispose()`, но не реализует `IDisposable`.
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
- CLAUDE.md ≤70 строк — детали в AgentsDocs/
- No Singleton — только Zenject bindings
- UniTask — новый async код только на UniTask
- Deprecated Pathfinding/ — оставлены до удаления, не трогать

## Протокол сессии
**Начало**: этот файл загружен автоматически — ничего делать не нужно.
**Конец задачи**: когда задача завершена, запусти `/update-knowledge` — скилл сам обновит этот файл.
Если пользователь говорит "готово", "задача выполнена", "закончили" → проактивно запускай `/update-knowledge`.
