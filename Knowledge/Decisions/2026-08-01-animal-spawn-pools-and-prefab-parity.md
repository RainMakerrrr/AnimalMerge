# Решение: `AnimalSpawner` — единственный владелец пулов спавна; паритет префабов Deer/Hedgehog

**Дата:** 2026-08-01
**Статус:** принято, реализовано (код + префабы + сцены + аниматор-контроллер)

## Контекст

Задача: подключить к игре всех животных из `Assets/Resources/Prefabs/Animals/` и развести спавн на «стартовый ростер уровня 1» и «случайные подкрепления уровня 2+». Попутно — довести `Deer.prefab` и `Hedgehog.prefab` до паритета с рабочими `Cheetah`/`Fox`.

Исходное состояние оказалось не таким, как записано в [[Index]]:

- **Оба конфига были живыми и разведёнными по фазе**, а не дублирующими друг друга. `PreBattleConfig._startingPool` питал уровень 1 (`AllySpawnPool.RefillFromConfig()`), `AnimalSpawner._animalTypes` — случайные подкрепления уровня 2+ (`AllySpawnService.QueueReinforcements`). Прежняя запись «дублирование стартового пула и пула подкреплений — намеренное» описывала не дублирование, а нераспознанное разделение ответственности.
- Настоящим legacy был `AutoFight.cs` (весь `Update()` закомментирован, `Move()` без вызывающих, логику вытеснил `TurnExecutor`) и bookkeeping-список `IAnimalSpawner.Animals` / `AnimalSpawner._animals`, дублировавший `UnitTracker`.
- `Deer` и `Hedgehog` физически лежали в `Resources/`, но в игру попасть не могли: у Deer не было `AnimalFacade`, а `AnimalFactory` собирает словарь через `LoadCollection<AnimalFacade>("Prefabs/Animals")` — `Create(Deer)` бросал `KeyNotFoundException`.

## Рассмотренные варианты

**1. Где держать два пула.**
- *Оставить как было* — `PreBattleConfig` владеет стартовым, `AnimalSpawner` — случайным. Разделение по слоям формально корректное, но «какие животные бывают в игре» отвечается в двух местах, и уровень 1 нельзя увидеть рядом с уровнем 2+.
- *Свести оба в `PreBattleConfig` (SO)* — «данные в ассетах» по конвенции проекта, но `AnimalSpawner` — единственный, кто знает про `_mergeGrid`, `HasFreeCellFor` и фактическую доступность префабов; конфиг пришлось бы прокидывать внутрь спавнера.
- ✅ *Свести оба в `AnimalSpawner`* — один владелец, оба массива видны рядом в инспекторе, `PreBattleConfig` остаётся при readiness/экономике (`MinAlliesToStart`, `ReinforcementsPerLevel`).

**2. Как дать `Deer` анимацию смерти, которой нет в ассет-паке.**
- *Оставить как есть* — параметр `IsDead` был объявлен **Trigger**, а `AnimalAnimator.DeathAnimation()` зовёт `SetBool`: смерть оленя не могла сработать в принципе.
- *Переход `DIdle 1 → DDeath`* — так сделано у Cougar/Fox, но это строго слабее: смерть в движении или в середине атаки не проиграется.
- ✅ *`IsDead` → Bool + стейт `DDeath` + переход **Any State → DDeath*** (`IsDead == true`, no exit time, `canTransitionToSelf = false`), клип `Deer_Death.anim` собран из сабклипа `DGetHit L` как поза-коллапс (Loop Time off).

**3. Геометрия merge-триггера у Deer/Hedgehog.**
- *Подобрать радиус на глаз* — прошлый источник обоих дефектов.
- ✅ *Вывести из `AnimalMovement.TryPlace()`* — он делает `Physics.RaycastAll` сверху по маске `PathNode|Animal` и берёт **ближайший** хит, требуя `IRaycastable`. Отсюда ровно два требования: сфера обязана перекрывать собственные bone-капсулы юнита и обязана НЕ нависать над соседними клетками.

## Принятое решение

### Пулы спавна
- `AnimalSpawner` держит два сериализованных массива: `_startingPool` (уровень 1) и `_randomPool` (переименован из `_animalTypes` через `[FormerlySerializedAs]`, уровень 2+).
- `IAnimalSpawner` получил `IReadOnlyList<AnimalType> StartingPool`; `IAllySpawnService` — `QueueStartingPool()`.
- `AllySpawnPool.RefillFromConfig()` и поле `PreBattleConfig` **удалены** — пул стал «тупой» очередью с безаргументным конструктором.
- `PreBattleConfig._startingPool` / `StartingPool` **удалены**; конфиг владеет только `MinAlliesToStart` + `ReinforcementsPerLevel`.
- `PreBattleState` зовёт `_allySpawnService.QueueStartingPool()` вместо `_allySpawnPool.RefillFromConfig()`. Семантика `isStartingPool` сохранена — `PoolExhaustedRule` и `MinAllyCountRule` ведут себя идентично.
- Значения в сцене: `_startingPool` = `Cheetah, Fox, Elephant` (байт-в-байт прежнее значение `PreBattleConfig`, уровень 1 не изменился); `_randomPool` = `Elephant, Cheetah, Deer, Fox, Hedgehog, Chicken` — все 6 player-префабов из папки.

> [!warning] Не добавлять в пулы `TRex` / `Pterodactyl` / `Velociraptor`
> Они объявлены в `AnimalType`, но player-префабов в `Assets/Resources/Prefabs/Animals/` нет — только вражеские в `Prefabs/Enemies/`. Попадание в пул = `Create` вернёт `null`.

### Удалён мёртвый код
- `Assets/Code/AutoFight.cs` удалён целиком; компонент снят с GameObject `TargetFinder` в `Main Scene.unity` и с `Grid Mover` в `Develop.unity` (второй оставался missing-script stub и найден на ревью).
- `IAnimalSpawner.Animals`, `AnimalSpawner._animals`, `OnAnimalRemoved`, `OnDestroy` и подписки на `OnRemoved` удалены — учёт юнитов живёт в `UnitTracker`.

### `Deer.prefab` — паритет
Добавлены `AnimalUpgrade`, `DeerFacade` (9 bone capsules, `_mergeSkillMultiplier` 1.5), `MergePopupController`, дочерний `Merge` (layer 20, trigger `SphereCollider` + `MergeTarget` + `MergeView`), дочерний `AttackPoint`, `MergeVisualSlot(sourceType = Fox)` как **override на существующем вложенном инстансе** `Fox tail Variant` — общий префаб `Assets/Animals compilation/Prefabs/Fox tail Variant.prefab` не трогали, он шарится ~10 префабами.

Починены сломанные ссылки: `AnimalMovement._animator`, `AnimalAttack._animator` / `_attackPoint` / `_radius` (1) / `_mask` (Enemy = 8388608). Маска была **0** — Deer вообще не мог найти цель.

### `Deer` — зависание хода
`TurnExecutor` ждёт `_isAttackDone`, который выставляется **только** анимационным событием `AttackAnimationHandler`. Стейт `DAttack Horns 1` указывал на FBX-сабклип без событий → первый удар оленя вешал ход навсегда. Создан standalone `Deer_Attack_Horns_1.anim` с событием на `t = 0.45`, стейт перенацелен, клип назначен в `AnimalAnimator._attackClip`.

### `Hedgehog.prefab`
`HedgehogFacade._mergeView` подключён к `MergeView` на дочернем `Collider` (был `{fileID: 0}` — работал через fallback `GetComponentInChildren` с логом ошибки).

### Геометрия merge-триггеров (измерена, не подобрана)
- **Deer**: `radius 1.25 → 1`, `center {0, 1.2, 0.75} → {0, 1.45, 0.5}` — `z ∈ [-0.5, 1.5]`, ровно свой футпринт 1×2.
- **Hedgehog**: `radius 1.2 → 1.6`, `center {0, 1.03, -0.15} → {0, 1.2, 0.4}`.

> [!important] У корня `Hedgehog` `localScale = 0.35` — сериализованный радиус ≠ мировой
> Реальный дефект был **противоположен** гипотезе ревьюера: не перекрытие соседей, а недокрытие себя. Пробник `centre + 0.4` (это `_raycastOffset` у Cheetah/Deer/Elephant) попадал в клетку, то есть ни одно крупное животное не могло вмерджиться в ежа. Новые значения дают паритет с Fox.

`Deer._zOffset = -0.5` корректен: меш оленя смещён вперёд (визуальный центр `z 0.747` против `0.195` у Cheetah), офсет приводит его к `0.247`.

### Прочее
`AnimalFactory.Create` переведён на `TryGetValue` + `Debug.LogError` + `return null` вместо необработанного `KeyNotFoundException`. Все три вызывающих места защищены: `AnimalSpawner.Spawn`, `ChickenMergeSkill` (`IMergeSkill.cs`) и `MultipleCharacters.cs` (последний добавлен по итогам ревью).

## Почему так

- **Единственный владелец важнее «данные в SO»**, когда речь о том, какие животные вообще существуют в игре. Стартовый ростер переехал из SO-ассета в scene-компонент — осознанный трейд-офф против конвенции проекта ради одного источника правды. `PreBattleConfig` при этом не выродился: `MinAlliesToStart` / `ReinforcementsPerLevel` — балансные числа, им место в ассете.
- **`AllySpawnPool` не должен знать конфигурацию.** До правки пул умел «сам себя наполнить из конфига» — обязанность, которая делала невозможной подмену источника. Теперь наполняет сервис, пул только хранит очередь.
- **Any State вместо перехода из Idle** — переход только из `DIdle 1` не сработал бы при смерти в движении или атаке. У Cougar/Fox сделано слабее; тиражировать слабый вариант не стали.
- **Осознанные пропуски визуала.** У ежа **нет** donated merge-визуала: отдельного пропа «иголки» в проекте не существует, они запечены в единый `SkinnedMeshRenderer`. У оленя нет слота `Deer → Deer`: `Antlers 02` — его родные активные рога, слот там прятал бы собственные рога оленя при Undo. Цена обоих решений — безобидный warning `[SlotMergeAttribute] No MergeVisualSlot with sourceType=Deer`.
- **Курица в случайном пуле — сознательно.** `GridManager.HasCellFor(Chicken)` проверяет ОДНУ клетку 1×1, а `AnimalFactory.SpawnAdditionalChickens` берёт «до 3» соседних — на тесной сетке выходит 1–3 курицы вместо 4. Деградирует безопасно, guard решили не добавлять.

> [!danger] Откат правок AnimatorController через API бывает ЧАСТИЧНЫМ
> Подтверждено на практике, усиливает заметку из [[Index]]. Правки контроллера откатились **молча** и обнаружились только при повторной проверке: `anyStateTransitions = 0`, стейт `DDeath` исчез целиком, диф усох с 69 строк до ~14. При этом `DAttack Horns 1 → Deer_Attack_Horns_1.anim` и параметр `IsDead: Bool` **выжили**. Одного закрытия окна Animator недостаточно — обязательно перепроверять `git diff` после **каждой** правки контроллера.

## Проверка
- Unity Console — ноль ошибок компиляции.
- EditMode `Code.Tests.EditorTests.BattleSystem.*` — **51/51 passed** (тесты `AllySpawnPoolTests`, `AllySpawnServiceTests`, `BattleReadinessServiceTests`, `BattleTestHelper` адаптированы под новый контракт).
- Новых тестов не писали — действует **TESTS PAUSED**.
- Предсуществующие падения, не связанные с этой работой: `AnimalAttackPostAbilityTests`, `RetreatAbilityTests`, `TargetPositionCalculatorTests`.

## Затронутые файлы

**Удалены:** `Assets/Code/AutoFight.cs` (+ `.meta`).

**Новые:** `Assets/Malbers Animations/Animals Packs/01 Forest Pack/Deer/Anims/Deer_Attack_Horns_1.anim`, `.../Deer_Death.anim` (+ `.meta`).

**Изменены:**
`Assets/Code/Abilities/MultipleCharacters.cs`;
`Assets/Code/Animals/{AnimalSpawner, IAnimalSpawner}.cs`;
`Assets/Code/Battle/Config/PreBattleConfig.cs`;
`Assets/Code/Battle/PreBattle/{AllySpawnPool, AllySpawnService, IAllySpawnPool, IAllySpawnService}.cs`;
`Assets/Code/Battle/States/PreBattleState.cs`;
`Assets/Code/Infrastructure/Factories/Animals/AnimalFactory.cs`;
`Assets/Code/Tests/EditorTests/BattleSystem/Helpers/BattleTestHelper.cs`;
`Assets/Code/Tests/EditorTests/BattleSystem/UnitTests/{AllySpawnPoolTests, AllySpawnServiceTests, BattleReadinessServiceTests}.cs`;
`Assets/Malbers Animations/Animals Packs/01 Forest Pack/Deer/Anims/Deer Animations.controller`;
`Assets/Resources/Prefabs/Animals/{Deer, Hedgehog}.prefab`;
`Assets/Scenes/Develop.unity`; `Assets/Scenes/Main Scene.unity`;
`Assets/Settings/BattleConfigs/PreBattleConfig.asset`.
