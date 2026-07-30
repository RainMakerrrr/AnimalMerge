# Решение: размеры сеток → ScriptableObject GridConfig (ссылка в Inspector, не Zenject)

**Дата:** 2026-07-30
**Статус:** принято, реализовано

## Контекст
Размеры игровых полей были тремя сериализованными полями прямо в `GridManager` (`_width`/`_height`/`_cellSize`) и правились на каждом экземпляре сцены (`MergeGrid` 8×2, `GameGrid` 8×10). Значения нельзя было переиспользовать, версионировать отдельно от сцены или подставлять в тестах. Задача: вынести в настраиваемые данные, не сломав ~29 файлов, зависящих от `IGridManager`.

## Рассмотренные варианты
1. **Инжект `GridConfig` через Zenject** (`WithId(GridIdentifier.…)`). Минус: размеры нужны в `OnDrawGizmos` в edit-mode — до `Awake` и до создания DI-контейнера, гизмо ломались бы.
2. **Расширить `IGridManager`** свойством конфига. Минус: blast radius 29 файлов + правка всех NSubstitute-моков ради нулевой выгоды.
3. **Ссылка на ассет в Inspector + ленивое чтение** (выбрано).

## Принятое решение
`GridConfig : ScriptableObject` — `Code/GridPathfinding/Config/GridConfig.cs`, namespace `Code.GridPathfinding.Config`, `[CreateAssetMenu(menuName = "Game/Grid Config")]`. Поля `_width` `[Min(1)]`, `_height` `[Min(MinHeight)]`, `_cellSize` `[Min(0.01f)]`; read-only `Width`/`Height`/`CellSize`; константы `DefaultWidth=8`, `DefaultHeight=10`, `DefaultCellSize=1f`, `MinHeight=2`.

Ассеты `Assets/Settings/GridConfigs/MergeGridConfig.asset` (8×2×1) и `GameGridConfig.asset` (8×10×1) — значения ровно те же, что были на сцене, поведение игры не изменилось.

В `GridManager` три поля заменены одной ссылкой `[SerializeField] private GridConfig _config`. Размеры лениво засеваются через `EnsureDimensions()`/`ApplyConfig()`, все ~40 внутренних чтений переведены на свойства. `IGridManager` НЕ менялся. Ссылки `_config` проставлены на `MergeGrid`/`GameGrid` в `Main Scene.unity`; старые ключи `_width`/`_height`/`_cellSize` из сцены ушли.

`Code/Editor/GridManagerSetup.cs` починен: `FindProperty("_width")` вернул бы `null` после удаления поля → NRE. Теперь проставляет `_config` через хелпер `LoadOrCreateGridConfig`, который никогда не перезаписывает существующий ассет.

## Почему так
- **Ссылка вместо инжекта**: гизмо в edit-mode рисуются без DI-контейнера. `PathfindingInstaller` по-прежнему биндит сами `GridManager` через `FromInstance` + `WithId(GridIdentifier.MergeGrid/GameGrid)`; `GridConfig` в контейнере не регистрируется.
- **Ленивая инициализация + флаг `_hasRuntimeOverride`**: в edit-mode `EnsureDimensions()` перечитывает конфиг при каждом чтении свойства (правки ассета сразу видны на гизмо), в рантайме — один раз. `Rebuild(int,int,float)` сохранил сигнатуру, пишет только runtime-поля и выставляет флаг, из-за чего edit-mode-ветка перестаёт перетирать его результат. Без флага `Rebuild` молча игнорировал бы свои аргументы вне play mode (нашло ревью). Ассет `Rebuild` не мутирует.
- **`[Min(2)]` на `_height`**: `GridManager.HasCellFor`/`PlaceOnGrid` безусловно итерируют `y <= 1`, а `GridCell.CanPlace` хардкодит `Y == 0 || Y == 1` — двухрядная зона развёртывания. При height=1 `HasCellFor` мог вернуть true для строки, которую `PlaceOnGrid` затем резолвил в null. Вынос глубины зоны развёртывания в конфиг оставлен вне скоупа.
- **Расположение по конвенции проекта**: скрипт рядом с модулем (`Code/GridPathfinding/Config/`, зеркалит `Code/Battle/Config/`) — чтобы не создавать зависимость `GridPathfinding → Data`; ассеты в `Assets/Settings/GridConfigs/` рядом с `LevelConfigs/` и `Animals/Stats/`, вне `Resources/`.

## Проверка
- 0 ошибок компиляции.
- `Assets/Code/Tests/EditorTests/GridPathfinding/GridConfigTests.cs` — 5 EditMode тестов, зелёные.
- `Assets/Code/Tests/PlayModeTests/GridPathfinding/GridConfigRuntimeTests.cs` — 3 `[UnityTest]`, end-to-end путь конфиг → `Awake` → `ApplyConfig` → `InitializeGrid`; конфиг 6×3×2 намеренно отличается от всех дефолтов. Зелёные.
- Ни один существующий тест править не пришлось (моки `IGridManager` не тронуты).

## Грабли (Unity MCP)
`manage_scriptable_object` `action: "create"` с `patches` **не гарантирует** применение значений: Unity отдавал stale-данные из кеша импорта — на диске одни значения, в памяти редактора дефолты из инициализаторов полей. `AssetDatabase.ImportAsset` с `ForceUpdate | ForceSynchronousImport` не помог. Лечится записью через живой объект (`action: "modify"` + `SaveAssets`) и обязательной верификацией фактических значений и на диске, и в памяти. Из-за этого `MergeGridConfig` дважды оказывался 8×10 вместо 8×2 — поймано только явной проверкой.

## Затронутые файлы
Новые: `Code/GridPathfinding/Config/GridConfig.cs`, `Settings/GridConfigs/MergeGridConfig.asset`, `Settings/GridConfigs/GameGridConfig.asset`, `Code/Tests/EditorTests/GridPathfinding/GridConfigTests.cs`, `Code/Tests/PlayModeTests/GridPathfinding/GridConfigRuntimeTests.cs` (+ `.meta`).
Изменены: `Code/GridPathfinding/GridManager.cs`, `Code/Editor/GridManagerSetup.cs`, `Assets/Scenes/Main Scene.unity`.
