# Решение: гейт старта боя считается по high-water mark союзников, а не по текущему счётчику

**Дата:** 2026-07-31
**Статус:** принято, реализовано

## Контекст
Кнопка **Battle/fight** гейтится набором правил `IBattleReadinessRule`. Для уровней со второго и дальше нужно условие «у игрока больше одного союзника» — иначе можно выйти в бой одним животным и слить уровень.

Наивная реализация `AlivePlayerUnitsCount >= MinAlliesToStart` создаёт софт-лок: **мердж двух союзников в одного — это ядро механики игры**. Игрок мерджит двух своих животных, счётчик падает до 1, кнопка Battle гаснет, кнопка AddAnimal уже серая (стартовый пул израсходован) — обе кнопки мертвы. `MergeUndoService` спасает, но он доступен только через `KeyboardMergeUndoHelper`, забинденный внутри `#if UNITY_EDITOR`, — на устройстве выхода из состояния нет вообще.

## Рассмотренные варианты
1. **`AlivePlayerUnitsCount >= 2` в лоб.** Отброшено — описанный софт-лок на устройстве.
2. **Запретить мердж, если после него останется меньше `MinAlliesToStart` союзников.** Минус: ломает ядро механики ради валидации UI, и правило про UI начинает диктовать правила мерджа.
3. **Дать `MergeUndoService` кнопку в рантайм-UI.** Минус: не решение — лечит симптом, добавляет UI, не входящий в задачу.
4. **Считать по high-water mark за фазу** (выбрано).

## Принятое решение
`MinAllyCountRule.IsSatisfied`:

```csharp
if (_phaseStartedWithPool)
    return true;

int aliveNow = _unitTracker.AlivePlayerUnitsCount;
return aliveNow >= 1 && Mathf.Max(_highWaterMark, aliveNow) >= EffectiveMin;

// EffectiveMin => Mathf.Min(_config.MinAlliesToStart, _alliesAvailableThisPhase);
```

Контекст фазы захватывается в `PreBattlePhaseStartedSignal`: `_phaseStartedWithPool = signal.PoolRemaining > 0`, `_alliesAvailableThisPhase = aliveAtStart + signal.PoolRemaining`, `_highWaterMark = aliveAtStart`. Дальше mark растёт по `IUnitTracker.PlayerUnitsChanged`.

Смысл: игрок должен был **иметь** нужное число союзников в этой фазе и должен всё ещё иметь хотя бы одного.

Сопутствующее изменение: `IUnitTracker` получил `event Action PlayerUnitsChanged` — readiness переоценивается реактивно (мердж уничтожает фасад → событие → переоценка), а не поллингом в `Update`. Событие поднимается только для союзного ростера: `OnUnitDied` переименован в `OnPlayerUnitDied` и подписывается только из `RegisterPlayerUnit`; `RegisterEnemyUnit` на `Health.Died` не подписывается вовсе (враги считаются по списку).

## Почему так
- **High-water mark вместо живого счётчика** — мердж перестаёт быть наказуемым: два союзника, слитые в одного, оставляют mark = 2, бой стартует.
- **`aliveNow >= 1` обязателен** — иначе после гибели последнего союзника mark разрешил бы старт с пустым полем.
- **`Mathf.Max(_highWaterMark, aliveNow)`, а не голый mark** — если уведомление `PlayerUnitsChanged` до правила не дошло, ошибка может сделать правило только слишком мягким, но никогда — софт-локом.
- **Клэмп `EffectiveMin = Min(MinAlliesToStart, alliesAvailableThisPhase)`** — стадия, где выжил один союзник и подкрепление физически негде взять, иначе была бы незапускаемой навсегда.
- **Фаза с непустым стартовым пулом (первый уровень) гейтится только `PoolExhaustedRule`** — там условие «больше одного союзника» намеренно не применяется, `_phaseStartedWithPool` даёт ранний `true`.
- **Подписка в конструкторе правила** (а не в `Initialize`) — контекст фазы захватывается независимо от того, в каком порядке контейнер инстанцирует правила и их читателей.
- **Событие только для союзного ростера** — врагам реактивность не нужна, а лишние подписки на `Health.Died` у врагов были источником шума.

## Попутно исправленные предсуществующие баги
Стали load-bearing именно из-за нового гейта — при неверном `AlivePlayerUnitsCount` правило врёт:
- `ChickenMergeSkill.Merge` создавал клона через `IAnimalFactory` и нигде его не регистрировал → счётчик падал, а на поле оставалось 2 животных; заодно ломался `MergeCommand.DetectChickenClone` («found 0 new units»). Теперь скилл инжектит `IUnitTracker` и регистрирует клона; `ChickenFacade.ConstructChicken` получил параметр `IUnitTracker`.
- `MergeCommand.ReactivateSourceAnimal` (undo мерджа) восстанавливал GameObject и клетку, но не перерегистрировал юнита в трекере → счётчик не восстанавливался и `PlayerUnitsChanged` не поднимался. Теперь зовёт `RegisterPlayerUnit` (метод идемпотентен).
- `PreBattleState.OnStartBattleRequested` получил флаг `_isStarting` — двойной тап больше не ставит в очередь два перехода в `PlayerTurnState`.
- Хоткей **P** (`KeyboardStartBattleHelper`) теперь тоже гейтится правилами: `PreBattleState.OnStartBattleRequested` проверяет `CanStartBattle`.

## Проверка
- EditMode `BattleSystem`: 51/51 зелёные.
- Два существующих теста переписаны под новую семантику: `UT-READY-004` (`LaterLevel_WithSingleAlly_BlocksBattleStart` → `LaterLevel_MergedDownToSingleAlly_AllowsBattleStart`) и `UT-READY-007` (флип теперь от потери последнего союзника 2→1→0).
- `BattleReadinessServiceTests` покрывает агрегацию правил по И.

## Затронутые файлы
Новые: `Code/Battle/PreBattle/Rules/MinAllyCountRule.cs`, `Code/Battle/PreBattle/Rules/PoolExhaustedRule.cs`, `Code/Battle/Signals/PreBattlePhaseStartedSignal.cs`.
Изменены: `Code/Battle/Services/IUnitTracker.cs`, `Code/Battle/Services/UnitTracker.cs`, `Code/Battle/States/PreBattleState.cs`, `Code/Animals/Merge/MergeSkills/IMergeSkill.cs` (`ChickenMergeSkill`), `Code/Animals/Merge/Commands/MergeCommand.cs`, `Code/Animals/Facades/ChickenFacade.cs`, `Code/Animals/AnimalSpawner.cs`.

## Связанное
- [[2026-07-31-pre-battle-phase-architecture]] — слои пре-батл фазы, в которых живёт это правило
