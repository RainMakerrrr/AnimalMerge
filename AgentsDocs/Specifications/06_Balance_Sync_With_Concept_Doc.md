# 06. Синхронизация баланса с концепт-доком 3.0

**Статус:** реализовано 2026-08-01 — §2.1–2.7, §2.9. Остаются §2.8 (блокер: арт) и §3 (блокер: геймдизайн)
**Дата составления:** 2026-08-01
**Источник истины:** ClickUp, концепт-док 3.0, страница «Heroes & Bosses»
https://app.clickup.com/24401093/v/dc/q8n65-4592/q8n65-3572

---

## 0. Контекст

После задачи «префабы Deer/Hedgehog + пулы AnimalSpawner» была проведена сверка
фактических данных проекта с концепт-доком. Совпало большинство, но найдено
9 расхождений. Этот документ — их полный список с точными значениями и файлами.

Что сверялось: `Assets/Settings/Animals/Stats/*.asset`, `_unitSize` и
`_mergeSkillMultiplier` на префабах в `Assets/Resources/Prefabs/Animals/`,
`Assets/Code/Animals/Merge/MergeSkills/IMergeSkill.cs`,
`Assets/Code/Abilities/Dodge.cs`, `Assets/Code/Abilities/CounterAttack.cs`,
`Assets/Resources/MergeAnimationConfig.asset`.

---

## 1. Что уже совпадает — НЕ ТРОГАТЬ

| Животное | HP / Dmg / Скорость / Размер | Мердж-скилл | Визуал |
|---|---|---|---|
| Слон | 500 / 150 / 2 / 2х2 ✅ | ×1.5 HP ✅ | ×1.5 размер ✅ |
| Олень | 300 / 350 / 5 / 1х2 ✅ | ×1.5 Дамаг ✅ | рога ✅ |
| Гепард | 250 / 250 / 7 / 1х2 ✅ | ×2 Скорость ✅ | текстура ✅ |
| Ёж | 100 / 100 / — / 1х1 ✅ | контратака 100%/50% ✅ | — (см. §2.7) |
| Лиса | 150 / 100 / 3 / 1х1 ✅ | первый додж гарантирован ✅ | хвост ✅ |
| Курица | — / 50 / — / — ✅ | клон 75% HP+Dmg ✅ | — (см. §2.6) |

Боссы: T-Rex `1000 / 350 / 2` ✅, Велоцираптор `150 / 250 / 4` ✅.

Слон: `_isAoE: 1`, `_maxTargets: 4` — соответствует «Ауе дамаг (на дыбы)» ✅.
Ёж: `AnimalAttack` хардкодом возвращается на `AnimalType.Hedgehog` —
соответствует «Не атакует, только контратакует» ✅.

---

## 2. Расхождения

### Группа A — правки только в `.asset` (безопасно, без кода)

#### 2.1 Ёж — скорость
- **Файл:** `Assets/Settings/Animals/Stats/HedgehogStats.asset`
- **Сейчас:** `_tilesPerMove: 3`
- **Должно:** `_tilesPerMove: 1`
- Ёж задуман медленным танком-контратакером; 3 ломает эту роль.

#### 2.2 Лиса — оба шанса доджа занижены
- **Файл:** `Assets/Settings/Animals/Stats/FoxStats.asset`
- **Сейчас:** `_ownerDodgeChance: 50`, `_inheritedDodgeChance: 30`
- **Должно:** `_ownerDodgeChance: 80`, `_inheritedDodgeChance: 50`
- Логика «первый раз всегда 100%» (`Dodge.cs:33`, `_counter == 0 => true`)
  уже верна — меняются ТОЛЬКО проценты.
- ⚠️ Есть более старая запись `Knowledge/Decisions/2026-06-27-fox-dodge-stats.md` —
  проверить, не было ли 50/30 осознанным балансным решением, отменяющим док.
  Если было — решить, что главнее, и обновить либо док, либо заметку.

#### 2.3 Курица — HP вчетверо больше
- **Файл:** `Assets/Settings/Animals/Stats/ChickenStats.asset`
- **Сейчас:** `_health: 100`
- **Должно:** `_health: 25`
- Док пишет «Хп Х25» — по 25 у каждой из четырёх. Сейчас каждая курица
  инстанцируется со 100 HP, то есть стая имеет 400 HP вместо 100.
  Дамаг (`_damage: 50` против «Х50») совпадает — похоже на опечатку именно в HP.

#### 2.4 Курица — скорость
- **Файл:** `Assets/Settings/Animals/Stats/ChickenStats.asset`
- **Сейчас:** `_tilesPerMove: 2`
- **Должно:** `_tilesPerMove: 3`

#### 2.5 Птеродактиль — HP и дальность хода
- **Файл:** `Assets/Settings/Animals/Stats/PterodactylStats.asset`
- **Сейчас:** `_health: 200`, `_tilesPerMove: 3`
- **Должно:** `_health: 350`, `_tilesPerMove: 5`
- Док: «полёт на 5 клеток и обратно», размер 2х1.
- ⚠️ Проверить `_unitSize` на префабе птеродактиля — док требует 2х1.

---

### Группа B — требуют кода

#### 2.6 Курица — стая должна быть ровно 4 в блоке 2х2

**Требование (уточнено заказчиком):** размер 2х2 означает **ровно 4 курицы,
гарантированно**, занимающие квадрат 2х2. Не «до 4», не произвольная форма.

- **Сейчас:**
  - префаб `Assets/Resources/Prefabs/Animals/Chicken.prefab` → `_unitSize: 1x1`
  - `AnimalFactory.SpawnAdditionalChickens` → `FindFreeNeighborCells(mainCell, 3)`
    (`AnimalFactory.cs:74`), то есть «до 3» соседей в порядке обхода
  - при 0 свободных соседей — `LogWarning` + основная курица остаётся одна
    (`AnimalFactory.cs:75-79`)
  - `GridManager.HasCellFor(Chicken)` (`GridManager.cs:682`) проверяет одну
    клетку 1х1
- **Должно:**
  - спавн курицы резервирует блок 2х2 целиком и ставит ровно 4 юнита
  - `HasFreeCellFor(Chicken)` / `HasCellFor` должны проверять доступность
    всего блока 2х2, а не одной клетки — иначе спавн начнётся и не завершится
  - если блока 2х2 нет — спавн не должен начинаться вообще
- ⚠️ **Побочный эффект, который надо обработать:** более строгая проверка
  заставит `AllySpawnService.CanSpawn` чаще возвращать false. На **уровне 1**
  это через `PoolExhaustedRule` (`PoolExhaustedRule.cs:22`) заблокирует кнопку
  Battle, если курица застрянет в стартовом пуле. Сейчас курица только в
  рандомном пуле (уровень 2+), где правило неактивно, — но при добавлении
  курицы в стартовый пул это выстрелит.
- Связанное: `AllySpawnedSignal.Unit` сообщает только главную курицу
  (`AllySpawnService.cs:95-99`), хотя `UnitTracker` регистрирует всех.

#### 2.7 Курица — нет визуала «уменьшение размера ×0.75»

- **Док:** «Удвоение юнита, уменьшение размера x0.75»
- **Сейчас:** `ChickenMergeSkill.PlayCloneAppearAnimation` вызывает
  `MergeAppearAnimation.Begin(clone.transform, clone.transform.localScale, config)`
  (`IMergeSkill.cs:359-362`) — целевой масштаб равен обычному.
  Удвоение есть, уменьшения нет.
- Статы при этом корректны: и оригинал, и клон получают 75% HP и Дамага.
- **Должно:** и оригинал, и клон визуально уменьшаются в 0.75 раза.
- Аналог для référence: слон растёт через `MergeAnimationConfig._growScaleMultiplier: 1.5`
  — уменьшение стоит сделать тем же механизмом (новое поле в конфиге,
  а не хардкод).

#### 2.8 Ёж — нет визуала «иглы» при мердже

- **Док:** визуал при мердже — «Иглы».
- **Сейчас:** ёж — единственное merge-способное животное без
  `VisualMergeAttribute`. Осознанно отложено в предыдущей задаче:
  отдельного пропа «иголки» в проекте нет, они запечены в единый
  `SkinnedMeshRenderer` префаба ежа.
- **Нужно:** арт — отдельный префаб иголок, затем `SlotMergeAttribute(_type = Hedgehog)`
  на ежа и неактивный `MergeVisualSlot(_sourceType = Hedgehog)` + позиционированный
  проп на **Cheetah, Fox, Elephant, Deer, Chicken** (5 префабов, руками по скелету).
- **Блокер:** нужен арт. Без него пункт не делается.

#### 2.9 Шанс контратаки при мердже захардкожен

- **Файл:** `Assets/Code/Abilities/CounterAttack.cs:39`
- **Сейчас:** `return _randomProvider.Range(0, 100) < 50;`
- Значение **правильное** (док: мердж = 50%), но лежит в коде, в отличие от
  лисы, у которой оба шанса вынесены в `FoxStats`.
- **Должно:** вынести в `HedgehogStats` (`_ownerCounterChance` / `_inheritedCounterChance`)
  по образцу `FoxStats._ownerDodgeChance` / `_inheritedDodgeChance`.
- Не срочно, но при следующей правке баланса значение придётся искать в коде.

---

## 3. Расхождение по механике прогрессии (не баланс)

Док: *«Как открываются новые животные? За победу над боссами. В первой итерации
захардкожены по определенным уровням.»*

Сейчас: `AnimalSpawner._randomPool` — плоский `Random.Range` по всем шести
животным начиная с уровня 2. Никакой привязки к уровням или победам над боссами нет.

Следствие: разброс ценности подкрепления ×7 (Олень 300/350 против Курицы 25/50
после правки §2.3), причём Олень доступен с первого же подкрепления.

**Решение не принято.** Варианты: гейт по уровню, веса на пул, разблокировка
за боссов. Требует обсуждения с геймдизайном.

---

## 4. Не реализовано вовсе (из док)

Животные (7 из 13 отсутствуют): **Орёл, Хамелеон, Кошка, Обезьяна, Кенгуру,
Скунс, Заяц**.

Боссы: **Дракон**, **Змей-Горыныч**.

Отдельная большая работа, вне этого документа.

---

## 5. Предлагаемый порядок работ

1. **Группа A** (§2.1–2.5) — пять правок в `.asset`, независимы друг от друга,
   без кода. Сделать одним проходом, проверить `git diff`.
   Перед §2.2 — прочитать `Knowledge/Decisions/2026-06-27-fox-dodge-stats.md`.
2. **§2.6** — блок 2х2 для куриц. Самый крупный пункт, затрагивает
   `AnimalFactory`, `GridManager.HasCellFor`, `AnimalSpawner.HasFreeCellFor`.
   Обязательно проверить взаимодействие с `PoolExhaustedRule`.
3. **§2.7** — масштаб ×0.75 для клона курицы. Небольшой, через
   `MergeAnimationConfig`.
4. **§2.9** — вынести шанс контратаки в `HedgehogStats`. Мелкий рефакторинг.
5. **§2.8** — иглы ежа. **Заблокировано на арте.**
6. **§3** — механика прогрессии. **Требует решения геймдизайна.**

---

## 6. Проверка после правок

- Unity Console — ноль ошибок компиляции.
- EditMode тесты `Code.Tests.EditorTests.BattleSystem.*` — были 51/51.
- Известные пре-существующие падения, НЕ связанные с этой работой:
  `AnimalAttackPostAbilityTests`, `RetreatAbilityTests`, `TargetPositionCalculatorTests`.
- Play Mode: спавн курицы на тесной сетке (§2.6), мердж курицы в слона
  (масштаб §2.7), додж лисы несколько раз подряд (§2.2).
- ⚠️ TESTS PAUSED — новые тесты не писать.

---

## 7. Что сделано 2026-08-01

### §2.1–2.5 — данные
Все семь значений применены через `SerializedObject` (чтобы состояние редактора совпало с диском):
`HedgehogStats._tilesPerMove 3→1`, `FoxStats._ownerDodgeChance 50→80` / `_inheritedDodgeChance 30→50`,
`ChickenStats._health 100→25` / `_tilesPerMove 2→3`, `PterodactylStats._health 200→350` / `_tilesPerMove 3→5`.

По §2.2 конфликта с `Knowledge/Decisions/2026-06-27-fox-dodge-stats.md` нет: та запись фиксирует
**рефакторинг** (где лежат данные), а не баланс — 50/30 были перенесены дословно из хардкода.
Дефолты в коде (`FoxStats.DefaultOwnerDodgeChance` / `DefaultInheritedDodgeChance`) обновлены до 80/50,
чтобы не создавать второй источник истины. В заметку добавлен датированный аддендум.

**§2.5, не выполнено:** префаба птеродактиля в проекте нет вообще — требование «`_unitSize` 2х1»
применить не к чему. Сделана только правка `.asset`.

### §2.6 — стая ровно из 4 куриц в блоке 2х2
Префаб курицы намеренно остаётся `_unitSize: 1x1` — каждая курица должна быть независимым
юнитом (свои HP, ход, смерть, `ChickenAttack`). «2х2» — это **след операции спавна**, а не размер юнита.

- `Code/Animals/ChickenFlock.cs` (новый): `Count = 4`, `Footprint = 2x2`.
- `GridManager.TryFindFreePlacement(size, direction, out anchor)` — единый поиск, возвращающий
  **скорректированный** якорь (`AdjustPositionToFitBounds` применяется до `CanPlaceUnit`).
  `HasCellFor` и `PlaceOnGrid` переведены на него; в `PlaceOnGrid` попутно устранён
  граничный баг — раньше в `GetCell` уходила нескорректированная позиция.
- Курица в таблице следов теперь `ChickenFlock.Footprint` вместо `1x1`, поэтому
  `HasCellFor(Chicken)` / `AnimalSpawner.HasFreeCellFor` проверяют весь блок.
- `IAnimalFactory.SpawnAdditionalChickens(mainChicken)` («до 3 соседей») заменён на
  `CreateChickenFlock(IReadOnlyList<IGridCell> cells)` — ровно 4, атомарно: при неудаче любой
  из четырёх уже созданные уничтожаются и возвращается пустой список.
- `AnimalSpawner.Spawn` уходит на ветку стаи **до** инстанцирования: нет блока 2х2 —
  не создаётся ни одна курица (раньше главная курица оставалась на поле одна).

**Взаимодействие с `PoolExhaustedRule` — только анализ, кода не добавлено.** Софт-лока сегодня нет:
в стартовом пуле (`Main Scene`: Cheetah, Fox, Elephant) курицы нет, а в фазах подкрепления правило
неактивно. Каверза сработает, если курицу добавят в стартовый пул.

**Не исправлено (вне объёма):** `AllySpawnService` шлёт `AllySpawnedSignal.Unit` только по первой
курице, хотя `UnitTracker` регистрирует всех четырёх.

### §2.7 — визуальное уменьшение ×0.75
- `MergeAnimationConfig._cloneScaleMultiplier = 0.75` (+ ассет `Resources/MergeAnimationConfig.asset`);
  заголовок `Elephant Growth` → `Merge Scale Change`, длительность/кривая роста переиспользуются.
- `MergeScaleAnimator.GrowBy/UndoGrowBy` → `ScaleBy/UndoScaleBy` (имена больше не врут о направлении),
  добавлен `Resync(scale)`.
- `ChickenMergeSkill` уменьшает и оригинал (с анимацией), и клона (мгновенно, **до**
  `MergeAppearAnimation.Begin` — иначе `CollapseScaleAndAttachGlow` перетрёт `localScale`).
- `MergeCommand.RestoreVisualState` вызывает `Resync` **последним**, после `UndoVisuals`:
  `ElephantMergeAttribute.Undo` делит `_logicalScale`, и ресинк до него оставил бы слона в `scale/1.5`.
- Если `MergeAnimationConfig` не заинжектен, уменьшение просто пропускается — фоллбэк-константы не заводились.

Цепочка мерджей курицы компаундится (0.75 за раз) — как и статы. Пола нет, это осознанно.

### §2.9 — шанс контратаки в `HedgehogStats`
Ровно по образцу `FoxStats`/`Dodge`: `Code/Data/Animals/HedgehogStats.cs` (новый,
`_ownerCounterChance: 100` / `_inheritedCounterChance: 50`), `HedgehogStats.asset` сконвертирован
in-place (сменён только `m_Script` GUID, GUID ассета сохранён → ссылка из `AnimalDatabase` цела).
`CounterAttack` стал value-agnostic (`int successChance` вместо `bool isOwner`), `HedgehogFacade`
инжектит `AnimalDatabase`, `HedgehogMergeSkill` хранит `_inheritedChance`.
Owner = 100 сохраняет «всегда»: `Range(0, 100)` не включает верхнюю границу.

### Проверка
- Компиляция чистая (единственная ошибка в консоли — пре-существующий `NullReferenceException`
  из графа аниматора, к этой работе отношения не имеет).
- EditMode, затронутые системы: `CounterAttackAbilityTests`, `AoEAbilityTests`,
  `AoEAttackIntegrationTests`, `AttackAndDamageRegressionTests`, вся группа `BattleSystem` — **80/80 зелёные**.
- Пре-существующие падения подтверждены и не трогались: `AnimalAttackPostAbilityTests`,
  `RetreatAbilityTests`, `TargetPositionCalculatorTests`.
- Новых тестов не писалось (TESTS PAUSED).
- Play Mode не прогонялся — спавн курицы на тесной сетке и масштаб при мердже требуют ручной проверки.
