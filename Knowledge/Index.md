# AnimalMerge — Knowledge Base
> Загружается автоматически при старте каждой сессии. Обновляй после значимой работы.

## Проект
Turn-based mobile battler с механикой merge. Unity 2022.3.62f3 · C# · Zenject · UniTask · DOTween · TextMeshPro.
Платформы: Android & iOS. Основная ветка разработки: `iteration_2/Alexandr/Develop`.

## Архитектура
- **Clean Architecture**: Presentation → Application → Domain → Infrastructure
- **DI**: Zenject — никаких Singleton
- **Async**: UniTask везде — никаких Coroutines в новом коде
- **Паттерны**: State (BattleStateMachine), Command (Merge undo), Factory, Facade (AnimalFacade), Observer (SignalBus)
- **Детали**: `AgentsDocs/ProjectArchitecture.md`

## Карта модулей
| Модуль | Путь | Статус |
|---|---|---|
| Battle State Machine | Code/Battle/StateMachine/ | Активен |
| Pre-Battle фаза | Code/Battle/PreBattle/ · UI/ · Signals/ | Активен — спавн союзников, readiness-правила |
| GridPathfinding (A*) | Code/GridPathfinding/ | Активен — multi-size units |
| Animal System | Code/Animals/ | Активен |
| Шейдеры / VFX | Assets/Shaders/ · Code/Animals/Materials/ · Code/Animals/Vfx/ | Активен — IridescentSweep (loot shine + дух смерти), GridCellBorder |
| Ability System | Code/Abilities/ | Активен (Dodge, CounterAttack, Retreat) |
| Data / ScriptableObjects | Code/Data/ | Активен |
| Framework (bootstrap) | Code/Framework/ | Стабилен — не трогать |
| Pathfinding/, NewPathfinding/ | Code/Pathfinding/ | DEPRECATED — удалить позже |

## Важные файлы
- `AgentsDocs/ProjectArchitecture.md` — полная документация модулей
- `AgentsDocs/ZenjectPatterns.md` — паттерны DI и SignalBus
- `AgentsDocs/Battle_State_System_Setup_Guide.md` — setup battle системы
- `AgentsDocs/Specifications/` — спецификации систем
- `AgentsDocs/CodeStyle.md` — соглашения по коду
- `.claude/CLAUDE.md` — инструкции для агента

## Последние изменения
_Обновляй при каждой сессии._

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

Ветка `iteration_2/Alexandr/Develop` активна. Последняя завершённая задача — **смерть курицы + вылет духа-призрака**.
- Ключевое: анимация падения курицы **уже была нарисована, но не подключена** — `IsDead` вёл в стейт `jump` (курица подпрыгивала), а готовые стейты `down` (`down.anim`, 1.33 с) и `wing_down` висели без единого входящего перехода. Доразведены через `Any State`, `IsDead` сменён с Trigger на Bool, `canTransitionToSelf = false` (обязательно — иначе Bool перезапускал бы стейт каждый кадр). Переход смерти стоит индексом 0, раньше `Any State → damage`, потому что `AnimalHealth.TakeDamageAsync:142-147` зовёт `TakeDamageAnimation()` прямо перед `Die()`.
- Эффект духа — новый модуль `Code/Animals/Vfx/`: `DeathSpiritSpawner` (на корне `Chicken.prefab`, подписка на `AnimalHealth.Died`, `UniTask.Delay` 0.35 с, `BakeMesh(useScale: true)`, спавн **без родителя** — тело умирает через 3 с) + `DeathSpiritView` (DOTween 1.8 с: подъём 2.5 ю, scale ×1.4, yaw +40°, покачивание, затухание на последних 45 %). Материал `Code/Animals/Materials/GhostSpirit.mat` на существующем `IridescentSweep`. Zenject не задействован — только сериализованные ссылки, по образцу `DamagePopupController`. C# существующих систем не трогался.
- Проверено данными: обе прослойки уходят в `down`/`wing_down`; `down.anim` — реальное падение (центроид Y −42 %, измерено запеканием вершин); гонку `damage` vs `down` выигрывает смерть; кривая полёта проскраблена через `Sequence.Goto`; 10 смертей подряд → дельта мешей 0, материалов 0. **Визуальный вид на экране НЕ проверен** — подбор оттенка требует глаз.
- Эффект стоит на **обеих** курицах: `Resources/Prefabs/Animals/Chicken.prefab` и `Resources/Prefabs/Enemies/EnemyChicken_Temp.prefab` (по +18 строк каждый). Вражеская делит тот же `ChickentwoLayerController`, поэтому разводку аниматора унаследовала — доставлен только `DeathSpiritSpawner`. Замерено на обеих: `down` достигается за 0.10 с (= длительность перехода), в `damage` не заходит, центроид 0.3018 → 0.1748.
- Материал духа переведён с аддитива на **альфа-блендинг**: новый шейдер `Assets/Shaders/SpiritGhost.shader` (`Voodoo_LaunchOps/FX/SpiritGhost`), `_Alpha` (дефолт 0.75) управляет плотностью, `_Fade` — единый параметр затухания. Аддитивный `IridescentSweep` для духа не годился: он только прибавляет свет, поэтому на тёмном фоне тело оставалось чёрным, а на светлом выцветало в белое — проверено офскрин-рендером. Сам `IridescentSweep.shader` не тронут (общий с loot shine на гепарде).
- ⚠️ **Ловушка: открытое окно Animator молча откатывает правки контроллера, сделанные через API.** Поймано на этой задаче: через полчаса после успешной правки контроллер на диске оказался откатан частично — смена `IsDead` на Bool уцелела, а оба `Any State`-перехода исчезли и удалённые вернулись. Перед правкой контроллера закрывать окно Animator, после — проверять `git diff`, а не только перечитывание ассета.
- Скоуп — только курицы. У `Deer` `IsDead` не ведёт никуда, у `Fox` — в `Fox_Stun`: у них смерти тоже нет, код обобщённый. У `T-Rex` и `Velociraptor` свои анимации смерти уже есть.

Предыдущая задача — эффект **«loot shine»**: шейдер `Assets/Shaders/IridescentSweep.shader` (`Voodoo_LaunchOps/FX/IridescentSweep`), Built-in RP, unlit аддитивный оверлей **вторым слотом материала**, анимация целиком на `_Time.y`, ни строки C#; материал `Code/Animals/Materials/IridescentSweepOverlay.mat` повешен на `SkinnedMeshRenderer` `Fox Tail` внутри `Resources/Prefabs/Animals/Cheetah.prefab` (элемент 0 = `Fox Skin Mane Wolf` не тронут).
- **Эффект по умолчанию не виден**: `Fox tail Variant` неактивен и гейтится `MergeVisualSlot` (`AnimalType.Fox`) + `SlotMergeAttribute` — появляется только после мерджа Fox в Cheetah. Для превью временно включить объект активным в префабе или смерджить Fox в Cheetah в play mode.
- `Assets/Animals compilation/Prefabs/Fox tail Variant.prefab` НЕ трогать — он общий для 10 префабов; эффект применён override-ом на вложенном инстансе внутри `Cheetah.prefab`.
- Переиспользование материала на другом животном требует выставить `_SweepAxisExtent` под размах его меша вдоль `_SweepAxis` — цена отказа от C#. В `DeathSpiritView` это ограничение снято: `_SweepAxisExtent` выставляется в рантайме из `bounds.size.y` запечённого меша.

**НЕ СДЕЛАНО — обвязка сцены на пользователе (задача пре-батл фазы от 2026-07-31).** `Main Scene.unity` намеренно не редактировалась (общий Unity Editor между параллельными сессиями). Нужно: снять missing script с `Spawn Animals Button` → повесить `AddAnimalButtonView`; на `Start Battle button` → `UiPulseAnimator` (Scale 1.08, Duration 0.5) + `BattleButtonView`; в `SceneContext → BattleInstaller` заполнить `Pre Battle Config`, `Add Animal Button`, `Battle Button`; разложить кнопки по макету. `ValidateSceneReferences()` логирует ошибку на каждый незаполненный слот. Полный чек-лист — `Sessions/2026-07-31.md`.

Не закоммичено: `Code/Animals/Vfx/` (+`.meta`), `Assets/Prefabs/DeathSpiritView.prefab` (+`.meta`), `Code/Animals/Materials/` (+`.meta`, оба материала), `Assets/Shaders/IridescentSweep.shader` (+`.meta`), изменённые `Animals3D/Animators/Chicken/ChickentwoLayerController.controller`, `Resources/Prefabs/Animals/Chicken.prefab`, `Resources/Prefabs/Animals/Cheetah.prefab`. Плюс вся предшествующая работа: пре-батл фаза (`Code/Battle/{PreBattle,UI,Signals,Config}/`, `Code/Animals/IAnimalSpawner.cs`, `Settings/BattleConfigs/`, 5 тестов; изменённые `AnimalSpawner.cs`, `ChickenFacade.cs`, `IMergeSkill.cs`, `MergeCommand.cs`, `IUnitTracker.cs`/`UnitTracker.cs`, `PreBattleState.cs`, `BattleStateMachine.cs`, `BattleInstaller.cs`) и `GridConfig` с обоими `.asset` (задача 2026-07-30).

Открытые вопросы:
- Тесты: действует политика **TESTS PAUSED**. По шейдеру и по эффекту смерти тесты не писались. Нет теста на `PreBattleHudPresenter`.
- Не применённый SUGGESTION по шейдеру: overdraw — один лишний аддитивный проход полного меша в transparent-очереди ломает early-Z на тайловых мобильных GPU. Незаметно на пропсе в 42 вершины, но растёт линейно при переносе эффекта на тела животных целиком или на много юнитов сразу. Дух смерти — как раз полный меш животного (~1400 вершин), но живёт 1.8 с и в единичных экземплярах.
- `AnimalHealth.Die()` по-прежнему на корутине (`StartCoroutine(DestroyWithDelay())`) — нарушение правила «только UniTask», предсуществующее, в скоуп не бралось.
- 4 предсуществующих варнинга ассет-пака в `ChickentwoLayerController` (`peck`, `to_flapping` ×2, `to_landing` — условия на несуществующие параметры) в стейтах, которых задача смерти не касалась.
- Не применённые SUGGESTION пре-батл фазы: `BattleButtonView._hideUntilReady` по умолчанию `false`; `PreBattleHudPresenter` берёт конкретный `StartBattleService`, а не интерфейс; `AllySpawnService.RequestSpawn` делает dequeue до получения результата спавна (безопасно из-за пред-проверки `HasFreeCellFor`).
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
- CLAUDE.md ≤70 строк — детали в AgentsDocs/
- No Singleton — только Zenject bindings
- UniTask — новый async код только на UniTask
- Deprecated Pathfinding/ — оставлены до удаления, не трогать

## Протокол сессии
**Начало**: этот файл загружен автоматически — ничего делать не нужно.
**Конец задачи**: когда задача завершена, запусти `/update-knowledge` — скилл сам обновит этот файл.
Если пользователь говорит "готово", "задача выполнена", "закончили" → проактивно запускай `/update-knowledge`.
