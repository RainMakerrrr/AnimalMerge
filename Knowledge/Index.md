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
| GridPathfinding (A*) | Code/GridPathfinding/ | Активен — multi-size units |
| Animal System | Code/Animals/ | Активен |
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

Ветка `iteration_2/Alexandr/Develop` активна. Последняя задача завершена и проверена в Unity (0 ошибок компиляции; 5 новых EditMode + 3 новых PlayMode теста зелёные): размеры игровых полей вынесены из хардкода `GridManager` в ScriptableObject-конфиги.
- `GridConfig : ScriptableObject` (`Code/GridPathfinding/Config/GridConfig.cs`, `[CreateAssetMenu("Game/Grid Config")]`) — `_width` `[Min(1)]` / `_height` `[Min(2)]` / `_cellSize` `[Min(0.01f)]`, read-only `Width`/`Height`/`CellSize`, константы дефолтов `8`/`10`/`1f`.
- Ассеты `Assets/Settings/GridConfigs/MergeGridConfig.asset` (8×2) и `GameGridConfig.asset` (8×10) — значения идентичны прежним из сцены, поведение игры не изменилось. Тюнить размеры теперь можно прямо в Inspector на ассетах.
- `GridManager`: три поля заменены ссылкой `_config`, размеры лениво засеваются через `EnsureDimensions()`/`ApplyConfig()`; `Rebuild(int,int,float)` сохранил сигнатуру и защищён флагом `_hasRuntimeOverride`. `IGridManager` не менялся → ноль правок в 29 зависимых файлах и во всех существующих тестах.
- `Main Scene.unity`: `_config` проставлен на `MergeGrid` и `GameGrid`. `Code/Editor/GridManagerSetup.cs` починен (иначе NRE на `FindProperty("_width")`), новый хелпер `LoadOrCreateGridConfig` не перезаписывает существующие ассеты.

Не закоммичено: новые `GridConfig.cs`, оба `.asset`, `GridConfigTests.cs`, `GridConfigRuntimeTests.cs` (+ `.meta`); изменённые `GridManager.cs`, `GridManagerSetup.cs`, `Main Scene.unity`.

Открытые вопросы:
- В диффе `Main Scene.unity` помимо двух блоков `_config` удалены GameObject `bg_cell_lvl5` и PrefabInstance `Assets/Cells.prefab` — вероятно, собственная работа пользователя в редакторе, зафиксированная MCP-save сцены. Не откатывалось, **нужно подтверждение пользователя**.
- Предсуществующие (НЕ регрессии этой задачи) падения тестов: ~24 в `TargetPositionCalculatorTests` (`GridTestHelper.CreateMockGrid` стабит только перегрузку `CanPlaceUnit(pos, size, dir, bool)`, а `TargetPositionCalculator` зовёт перегрузку с `HashSet<Vector2Int>`); ~9 «Method has non-void return value» в `AnimalAttackPostAbilityTests`/`RetreatAbilityTests`; NRE в `GridManager.GetCell` при спавне врагов из-за race порядка `Awake` между `GameBootstrapper` и `GridManager` (`_cells` по-прежнему аллоцируется только в `Awake`).
- В рабочем дереве лежит незакоммиченная предшествующая работа пользователя: `GridCell.cs`, `IGridManager.cs` (`Rebuild`/`ClearCells`/`SetSize`), `GridCellMaterial.mat`, `GridCell.prefab`, новый `Assets/Shaders/GridCellBorder.shader`.

Предыдущие задачи (Fox dodge → `FoxStats`, merge popup, подсветка радиуса хода) — код на месте; по merge popup могла остаться ручная настройка в Inspector (префаб `MergePopupView.prefab`, навеска `MergePopupController` на player-префабы, заполнение `MergeInfo`).

## Ключевые решения
Подробности в `Knowledge/Decisions/`. Краткий список:
- [2026-06-27] Merge popup показывается ТОЛЬКО при cross-type мердже (когда реально выдан новый скилл) и ключуется по типу SOURCE (потреблённого) животного; событие `Merged` гейтится флагом `grantedNewSkill` в `ExecuteMergeDirectly`, минует undo → `Decisions/2026-06-27-merge-popup.md`
- [2026-06-27] Подсветка хода вынесена в отдельную сущность (SRP) — `AnimalMovement` не нагружали; drag vs hold различается порогом смещения в пикселях, IInputService не трогали → `Decisions/2026-06-27-move-range-highlighter.md`
- [2026-06-27] Per-animal тюнинг-данные способностей (вероятности Dodge для Fox) хранятся в подклассе `AnimalStats` (`FoxStats`), а НЕ в общем `AnimalConfig` — данные живут со статами животного и не «протекают» в конфиги остальных; `Dodge` стал value-agnostic (`int successChance`), owner/inherited решается на стороне вызова → `Decisions/2026-06-27-fox-dodge-stats.md`
- [2026-07-30] `GridConfig` доставляется в `GridManager` ссылкой в Inspector, а НЕ через Zenject-инжект — размеры нужны в `OnDrawGizmos` в edit-mode, до `Awake` и до создания DI-контейнера; `IGridManager` намеренно не расширялся (blast radius 3 файла вместо 29) → Decisions/2026-07-30-grid-config.md
- CLAUDE.md ≤70 строк — детали в AgentsDocs/
- No Singleton — только Zenject bindings
- UniTask — новый async код только на UniTask
- Deprecated Pathfinding/ — оставлены до удаления, не трогать

## Протокол сессии
**Начало**: этот файл загружен автоматически — ничего делать не нужно.
**Конец задачи**: когда задача завершена, запусти `/update-knowledge` — скилл сам обновит этот файл.
Если пользователь говорит "готово", "задача выполнена", "закончили" → проактивно запускай `/update-knowledge`.
