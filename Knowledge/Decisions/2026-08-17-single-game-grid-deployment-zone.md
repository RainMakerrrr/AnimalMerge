# Решение: MergeGrid удалён — одна `GameGrid` 8×12, зона деплоя как логическое понятие (`DeploymentZone`)

**Дата:** 2026-08-17
**Статус:** принято, реализовано (рантайм + сцена + ассеты). Не закоммичено на момент записи

## Контекст

В игре было две независимые сетки: `GameGrid` 8×10 в `(0,0,0)` и `MergeGrid` 8×2 в `(0,0,−3)` —
отдельный `GridManager`, отдельный `GridConfig`, отдельный Zenject-биндинг через `GridIdentifier`.
Животные создавались и мерджились на `MergeGrid`, воевали на `GameGrid`.

Расплата за это была структурной, а не косметической:

- **Между сетками была дыра.** `MergeGrid` занимала z ∈ [−3, −1], `GameGrid` начиналась с z = 0 —
  визуальный разрыв в одну клетку.
- **Союзник на `MergeGrid` логически алиасился на `GameGrid(x, 0/1)`**: координаты клеток у двух
  сеток совпадают, `GridCell.Equals` сравнивает по X/Y. Отсюда телепорт юнита на первом ходу,
  костыль `MoveRangeHighlighter.IsOnGameGrid` (сравнение по `ReferenceEquals`, чтобы отличить
  клетку одной сетки от одноимённой клетки другой) и `ChickenMergeSkill`, искавший место под клона
  на мерж-сетке даже для юнита, стоящего в середине боевого поля.
- Девять сайтов `[Inject(Id = GridIdentifier.MergeGrid / GameGrid)]` — каждый новый потребитель
  сетки обязан был знать, какая из двух ему нужна.

## Рассмотренные варианты

1. **Оставить две сетки, чинить алиасинг точечно** (сравнение по ссылке везде, где сравниваются
   клетки). Отклонено: лечит симптом. Число мест, где «клетка (3,1)» неоднозначна, растёт с каждой
   новой фичей, и каждое из них ломается молча.
2. **Одна сетка, геометрия сохраняется байт-в-байт: `GameGrid` остаётся 8×10 в `(0,0,0)`, зона
   деплоя — существующие строки 0–1, враги остаются на y = 9.** Отклонено владельцем: боевой баланс
   сохранялся идеально, но враги визуально уезжали на 2 юнита к камере — весь мир смещался.
3. **Вариант A: `GameGrid` 8×12 в `(0,0,−2)`, враги переезжают с y = 9 на y = 11.** ✅ Принято.

## Принятое решение

### Геометрия — Вариант A

`MergeGrid` удалена целиком: из кода, из `Main Scene`, ассет `MergeGridConfig.asset` стёрт.
`GameGridConfig._height` 10 → 12, transform `GameGrid` z: 0 → −2. Враги во всех четырёх
`LevelStageConfig` переехали с y = 9 на y = 11 (13 записей).

**Мир визуально не изменился**: боевое поле и враги остались на тех же мировых координатах
(враги по-прежнему z = 9.5). Зона деплоя при этом сдвинулась на одну клетку **ближе** к полю
(была z ∈ [−3, −1], стала z ∈ [−2, 0]) — дыра между сетками исчезла.

> [!warning] Логическая дистанция «союзник → враг» осознанно выросла на 2 клетки
> Раньше союзник стоял на отдельной сетке и логически алиасился на строку 0/1 `GameGrid`; теперь
> он реально стоит в строке 0/1 сетки высотой 12, а враг — в строке 11. Медленным юнитам
> (`TilesPerMove` 1–3: ёж, гепард, олень) нужен примерно **+1 ход** на сближение. Визуальная
> стабильность мира была признана важнее сохранения баланса байт-в-байт.

### `DeploymentZone` — зона деплоя стала явным понятием

Новый статический класс `Assets/Code/GridPathfinding/DeploymentZone.cs`: `Depth = 2`,
`Contains(int y)`, `ContainsAll(IReadOnlyList<IGridCell>)`. Заменил магическую единицу в четырёх
местах `GridCell` (`CanPlace`, `IsInDeploymentZone`, два сравнения в `IsPositionValidForPlacement`)
и `GridConfig.MinHeight`.

### DI: `GridIdentifier` удалён

`PathfindingInstaller` держит один биндинг:

```csharp
Container.Bind<IGridManager>().FromInstance(_gameGridManager).AsSingle();
```

Попутно исправлено `.AsTransient()` на `FromInstance` — на инстансе это ничего не давало, кроме
ложного сигнала о времени жизни. Все 9 `[Inject(Id = ...)]` разыменованы.

### 🔴 Границу зоны теперь держит логика, а не геометрия — главная ловушка задачи

> [!danger] Выход футпринта за зону не ловился никакой проверкой — его предотвращала высота сетки
> Раньше выход юнита за строки 0–1 предотвращала сама высота `MergeGrid` (ровно 2):
> `AdjustPositionToFitBounds` сдвигал якорь обратно внутрь. `CanPlaceUnit` выход за границы ловить
> **не умеет**: `GetOccupiedCellsInternal` молча выбрасывает `null`-клетки, поэтому проверка идёт
> только по найденным. На сетке высотой 12 футпринты 1×2 (Cheetah/Deer/Fox/Hedgehog) и 2×2
> (Elephant, стая куриц) с якорем y = 1 молча заняли бы боевую строку 2 — **без единой ошибки
> в консоли**.

Поэтому в `GridManager.TryFindFreePlacement` добавлен гейт `FitsInDeploymentZone`:

```csharp
var footprintCells = GetOccupiedCells(anchor, size, direction);

return footprintCells.Count == size.Width * size.Height
       && DeploymentZone.ContainsAll(footprintCells);
```

**Проверка «количество клеток == площадь футпринта» здесь несущая**, а не защитная: без неё
свисающий за границу футпринт проходит именно потому, что недостающие клетки отбрасываются как
`null`, и `ContainsAll` радостно подтверждает оставшиеся.

Мёртвый `GetSortedFreeCells` (собирал свободные клетки «только двух нижних строк») удалён —
единственным алгоритмом поиска места остался `TryFindFreePlacement`.

### 🔴 `ChickenMergeSkill.FindFreeMergeCell` — та же ловушка, найдена только на ревью

Метод искал клетку под клона курицы «по всей сетке», и это было безопасно **ровно потому**, что
сетка была 8×2. На 8×12 мердж в строке 1 ронял клона в строку 2, а column-major фолбэк предпочитал
клетку `(0,5)` в боевом поле свободной `(1,0)` в зоне деплоя.

Починено: adjacent-probe гейтится новым `IsFreeDeploymentPlacement` (bounds → `DeploymentZone` →
`CanPlaceUnit` → та же проверка полноты футпринта), фолбэк делегирован в зоно-осведомлённый
`TryFindFreePlacement`. Ради этого `TryFindFreePlacement` поднят в интерфейс `IGridManager`.

> [!important] Урок шире этой задачи
> **При снятии неявного ограничения искать надо не только код, который его проверял, но и код,
> который на нём молча ехал.** Первый находится грепом по константе, второй — нет.

### `MoveRangeHighlighter` — гейт убран, радиус подсвечивается везде

Костыль `IsOnGameGrid` (`ReferenceEquals` на клетку инжектированной сетки) стал бессмысленным —
сетка одна. Сначала на его место встал явный `if (DeploymentZone.Contains(...Y)) return;`
(владелец выбрал «сохранить поведение»), но при проверке в Play Mode выяснилось, что владелец
понял вопрос иначе: подсветка нужна и на стартовых позициях. Гейт удалён совсем — `Show`
отсекает только `unit == null` / `CurrentPathNode == null`.

> [!note] Это возврат к исходному замыслу, а не новое решение
> [[2026-06-27-move-range-highlighter]] прямо фиксировал: «радиус хода по ВСЕЙ сетке
> (без ограничения зоной деплоя Y<=1) — это превью боевого перемещения, а не зона размещения».
> `IsOnGameGrid` появился позже и был чистым артефактом двух сеток: он гасил подсветку
> не по смыслу, а потому что клетка `MergeGrid` не была объектом клетки `GameGrid`.

Ограничение «только первые две клетки» относится к ручному размещению (`GridCell.Accept`),
а не к ходу в бою, поэтому BFS корректно растекается из строк 0–1 в поле.

## Почему так

- **Двусмысленность координат лечится удалением второй системы координат, а не аккуратностью
  в каждой точке сравнения.** Три бага (телепорт на первом ходу, костыль в подсветке, поиск клетки
  для клона курицы) имели одну причину и закрылись структурно, без отдельных правок.
- **Визуальная стабильность мира дороже байт-в-байт баланса.** Вариант с сохранением врагов на
  y = 9 был математически чище, но двигал всю картинку; +1 ход медленным юнитам — измеримая
  и обратимая цена (правится `_height` и координатами врагов).
- **Гейт зоны — на выходе из поиска места, а не внутри `CanPlaceUnit`.** `CanPlaceUnit` — общая
  проверка занятости, она используется и для боевых перемещений, где ограничение зоны не нужно
  и вредно. Зона — свойство операции «разместить нового юнита», поэтому и живёт в
  `TryFindFreePlacement` / `IsFreeDeploymentPlacement`.
- **`DeploymentZone.Depth` — константа кода, а не поле `GridConfig`**, потому что зона деплоя
  сейчас нигде не настраивается дизайнером и вводить незадействованную настройку — лишний
  контракт. Записано в follow-up на случай, если понадобится.

## Проверка

- **Ёмкость зоны проверена экспериментально на сетке 8×12**: 1×1 → 16 юнитов, 1×2 → 8, 2×2 → 4.
  Ни один футпринт не залез в строку 2. **Ёмкость идентична старой `MergeGrid` 8×2.**
- `GridConfigTests` 5/5 и `GridConfigRuntimeTests` 6/6 — зелёные.
- Новых тестов не писали (**TESTS PAUSED**).
- **Два предсуществующих красных кластера подтверждены сверкой с `git show HEAD` и этой задачей
  НЕ вызваны**:
  - `AnimalMovementRetreatTests` (3) — сигнатура `[Test] public async Task`, NUnit её отвергает;
    тесты не проходили никогда.
  - `TargetPositionCalculatorTests` (25) + `EndToEndMovementTests` (4) — `GridTestHelper.CreateMockGrid`
    и `PlayModeTestHelper.CreateMockGrid` стабят только `bool`-перегрузку `CanPlaceUnit`, а
    `TargetPositionCalculator` зовёт перегрузку с `HashSet<Vector2Int>` → NSubstitute возвращает
    `false` → все якоря отвергаются. Отдельная задача.
- ❌ **Отклонено на ревью и откачено**: в диффе оказался `ProjectSettings/EditorSettings.asset`
  с отключёнными domain reload и scene reload. Project-wide настройка, меняющая время жизни статики
  между Play-сессиями, к задаче отношения не имеет.

## Затронутые файлы

**Создан:** `Assets/Code/GridPathfinding/DeploymentZone.cs` (+ `.meta`)

**Удалены:** `Assets/Code/GridPathfinding/GridIdentifier.cs` (+ `.meta`),
`Assets/Settings/GridConfigs/MergeGridConfig.asset` (+ `.meta`)

**Изменены — сетка и DI:** `GridPathfinding/GridManager.cs` · `GridCell.cs` · `IGridManager.cs` ·
`PathfindingService.cs` · `Config/GridConfig.cs` · `Infrastructure/Installers/PathfindingInstaller.cs`

**Изменены — потребители сетки:** `Animals/AnimalAttack.cs` · `AnimalSpawner.cs` ·
`Facades/ChickenFacade.cs` · `Merge/Commands/MergeCommand.cs` · `Merge/Commands/MergeStateSnapshot.cs` ·
`Merge/MergeSkills/IMergeSkill.cs` · `Movement/AnimalMovement.cs` · `Movement/MoveRangeHighlighter.cs` ·
`Movement/TargetPositionCalculator.cs` · `Battle/PreBattle/AllySpawnService.cs` ·
`Battle/Services/EnemySpawnService.cs` · `Battle/Services/IUnitRepositioningService.cs` ·
`Battle/Services/UnitRepositioningService.cs` · `Battle/States/StageClearState.cs` · `TestEnemiesSpawner.cs`

**Изменены — Editor:** `Editor/BattleSceneBuilder/{BattleSceneBuildRequest, BattleSceneBuilder,
BattleSceneBuilderWindow, BattleSceneReferenceResolver, EditorAllyPlacer}.cs` · `Editor/GridManagerSetup.cs`

**Изменены — ассеты и сцена:** `Assets/Scenes/Main Scene.unity` ·
`Assets/Settings/GridConfigs/GameGridConfig.asset` · `Assets/Settings/LevelConfigs/LevelStageConfig_1..4.asset`

**Изменена документация:** `AgentsDocs/Specifications/05_Battle_System_Timings_Specification.md` ·
`Knowledge/GameDesignerGuide.md`

## Открытые пункты (follow-up, не сделано)

- **Зона деплоя визуально ничем не отличается от боевого поля** — раньше отличие давала отдельная
  сетка. Нужно геймдизайнерское решение по выделению строк 0–1.
- **`DeploymentZone.Depth` — константа в коде**, тогда как размеры сетки редактируются дизайнером
  в `GameGridConfig`. Кандидат на перенос в `GridConfig`.
- **`UnitRepositioningService` кастует `IGridManager` к конкретному `GridManager`** — `PlaceOnGrid`
  и `HasCellFor` просятся в интерфейс следом за `TryFindFreePlacement`.
- Расхождение `GridManager.GetFootprint` (Fox/Hedgehog 1×2) с реальным `_unitSize` 1×1 в префабах —
  предсуществующее, к задаче не относится.
- `.DS_Store` не в `.gitignore`.

## Связанное

- [[2026-07-30-grid-config]] — размеры сеток вынесены в `GridConfig`; `MinHeight = 2` оттуда теперь
  выражен через `DeploymentZone.Depth`
- [[2026-08-01-chicken-flock-and-balance-sync]] — `TryFindFreePlacement` как единственный алгоритм
  поиска места; стая курицы 2×2 — крупнейший футпринт, который зона обязана вмещать
- [[2026-08-13-battle-scene-builder]] — инструмент раскладывал союзников на `MergeGrid`,
  переведён на единую сетку
- [[2026-06-27-move-range-highlighter]] — `MoveRangeHighlighter`, чей костыль `IsOnGameGrid`
  закрылся структурно
