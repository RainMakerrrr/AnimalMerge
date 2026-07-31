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

Ветка `iteration_2/Alexandr/Develop` активна. Последняя задача завершена и проверена в Unity (0 ошибок компиляции, 0 варнингов; EditMode `BattleSystem` 51/51 зелёные, PlayMode `UI` 9/12 — 3 падения `DamagePopup*` предсуществующие): массовый спавн союзников заменён на пошаговую пре-батл фазу с кнопками.
- Слои: тупые View (`AddAnimalButtonView`, `BattleButtonView`, `UiPulseAnimator`) → `PreBattleHudPresenter` (единственный клей) → прикладной слой (`IAllySpawnService`, `IAllySpawnPool`, `IBattleReadinessService`, `IBattleReadinessRule[]`) → домен (`IAnimalSpawner`, `IUnitTracker`, `IAnimalFactory`). Команды идут внутрь через интерфейсы, факты наружу через `SignalBus` (4 новых сигнала, все `.OptionalSubscriber()`).
- Поведение: клик по AddAnimal ставит одно животное из пула (все созданные фасады регистрируются в `IUnitTracker` — курица порождает 3 дополнительных); пул исчерпан → AddAnimal гаснет, Battle загорается и пульсирует (DOTween). На поздних уровнях Battle гейтится `MinAllyCountRule`.
- Новое условие старта боя = один класс `IBattleReadinessRule` + одна строка биндинга. Стартовый пул вынесен в данные: `PreBattleConfig` (`Code/Battle/Config/`, меню `Game/Pre Battle Config`), ассет `Settings/BattleConfigs/PreBattleConfig.asset` (`Cheetah, Fox, Elephant`, `MinAlliesToStart = 2`). `AnimalSpawner._animalTypes` теперь — только пул подкреплений для `SpawnRandom()`.
- `IUnitTracker` получил `event Action PlayerUnitsChanged` (реактивная переоценка readiness вместо поллинга). Удалены `Code/Battle/Input/SpawnAnimalsButton.cs` и `StartBattleButton.cs`.
- Попутно исправлены предсуществующие баги: `ChickenMergeSkill` не регистрировал клона в трекере; `MergeCommand.ReactivateSourceAnimal` (undo) не перерегистрировал юнита; `AnimalSpawner` получил ленивый `EnsureFactoryReady()`; `PreBattleState` защищён флагом `_isStarting` от двойного тапа; хоткей **P** теперь тоже гейтится readiness.

**НЕ СДЕЛАНО — обвязка сцены на пользователе.** `Main Scene.unity` намеренно не редактировалась (общий Unity Editor между параллельными сессиями). Нужно: снять missing script с `Spawn Animals Button` → повесить `AddAnimalButtonView`; на `Start Battle button` → `UiPulseAnimator` (Scale 1.08, Duration 0.5) + `BattleButtonView`; в `SceneContext → BattleInstaller` заполнить `Pre Battle Config`, `Add Animal Button`, `Battle Button`; разложить кнопки по макету. `ValidateSceneReferences()` логирует ошибку на каждый незаполненный слот. Полный чек-лист — `Sessions/2026-07-31.md`.

Не закоммичено: все новые файлы `Code/Battle/{PreBattle,UI,Signals,Config}/`, `Code/Animals/IAnimalSpawner.cs`, `Settings/BattleConfigs/`, 5 новых тестов; изменённые `AnimalSpawner.cs`, `ChickenFacade.cs`, `IMergeSkill.cs`, `MergeCommand.cs`, `IUnitTracker.cs`/`UnitTracker.cs`, `PreBattleState.cs`, `BattleStateMachine.cs`, `BattleInstaller.cs`. Плюс незакоммиченная предшествующая работа: `GridConfig` и оба `.asset` (задача 2026-07-30).

Открытые вопросы:
- Тесты: в `.claude/CLAUDE.md` действует политика **TESTS PAUSED** — новые тесты после её появления не добавлялись. Нет теста на `PreBattleHudPresenter`.
- Не применённые SUGGESTION из ревью: `BattleButtonView._hideUntilReady` по умолчанию `false`; `PreBattleHudPresenter` берёт конкретный `StartBattleService`, а не интерфейс; `AllySpawnService.RequestSpawn` делает dequeue до получения результата спавна (безопасно из-за пред-проверки `HasFreeCellFor`).
- Дублирование стартового пула (`PreBattleConfig.StartingPool`) и пула подкреплений (`AnimalSpawner._animalTypes`) — намеренное, объединение вне скоупа.
- Предсуществующие красные тесты (НЕ регрессии): ~16 `TargetPositionCalculatorTests`, ~9 `AnimalAttackPostAbility`/`Retreat`, 4 `HealthBarViewTests`, 3 `DamagePopup*`.

## Ключевые решения
Подробности в `Knowledge/Decisions/`. Краткий список:
- [2026-06-27] Merge popup показывается ТОЛЬКО при cross-type мердже (когда реально выдан новый скилл) и ключуется по типу SOURCE (потреблённого) животного; событие `Merged` гейтится флагом `grantedNewSkill` в `ExecuteMergeDirectly`, минует undo → `Decisions/2026-06-27-merge-popup.md`
- [2026-06-27] Подсветка хода вынесена в отдельную сущность (SRP) — `AnimalMovement` не нагружали; drag vs hold различается порогом смещения в пикселях, IInputService не трогали → `Decisions/2026-06-27-move-range-highlighter.md`
- [2026-06-27] Per-animal тюнинг-данные способностей (вероятности Dodge для Fox) хранятся в подклассе `AnimalStats` (`FoxStats`), а НЕ в общем `AnimalConfig` — данные живут со статами животного и не «протекают» в конфиги остальных; `Dodge` стал value-agnostic (`int successChance`), owner/inherited решается на стороне вызова → `Decisions/2026-06-27-fox-dodge-stats.md`
- [2026-07-30] `GridConfig` доставляется в `GridManager` ссылкой в Inspector, а НЕ через Zenject-инжект — размеры нужны в `OnDrawGizmos` в edit-mode, до `Awake` и до создания DI-контейнера; `IGridManager` намеренно не расширялся (blast radius 3 файла вместо 29) → Decisions/2026-07-30-grid-config.md
- [2026-07-31] Пре-батл фаза разложена по слоям: команды идут внутрь через интерфейсы (view → `PreBattleHudPresenter` → `IAllySpawnService`), факты наружу через `SignalBus`; новое условие старта боя = один класс `IBattleReadinessRule` + одна строка биндинга; стартовый пул вынесен в `PreBattleConfig` → Decisions/2026-07-31-pre-battle-phase-architecture.md
- [2026-07-31] Гейт старта боя считается по high-water mark союзников за фазу, а не по живому счётчику — иначе мердж двух союзников в одного (ядро механики) вешал софт-лок с обеими мёртвыми кнопками; `IUnitTracker` стал реактивным (`PlayerUnitsChanged`) → Decisions/2026-07-31-min-ally-count-high-water-mark.md
- CLAUDE.md ≤70 строк — детали в AgentsDocs/
- No Singleton — только Zenject bindings
- UniTask — новый async код только на UniTask
- Deprecated Pathfinding/ — оставлены до удаления, не трогать

## Протокол сессии
**Начало**: этот файл загружен автоматически — ничего делать не нужно.
**Конец задачи**: когда задача завершена, запусти `/update-knowledge` — скилл сам обновит этот файл.
Если пользователь говорит "готово", "задача выполнена", "закончили" → проактивно запускай `/update-knowledge`.
