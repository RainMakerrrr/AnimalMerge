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

- [2026-06-27] Вероятности Dodge для Fox вынесены из хардкода в данные — подкласс `FoxStats : AnimalStats` (Owner 50% / Inherited 30%, `[Range(0,100)]`), ассет `FoxStats.asset` сконвертирован на новый тип; `Dodge` теперь принимает `int successChance` вместо `bool isOwner`
- [2026-06-27] Текстовая анимация (popup) при кросс-тайп мердже животных — `MergePopupView`/`MergePopupController` (Code/Animals/UI/), событие `MergeTarget.Merged`, строки per-animal в `AnimalConfig.MergeInfo`
- [2026-06-27] Подсветка доступных ходов животного при удержании клика/тапа (BFS-радиус по TilesPerMove) — новая сущность `MoveRangeHighlighter`
- [2026-05-27] Setup Codex, обновлены настройки проекта
- [2026-05-27] Добавлено рисование gizmos для AOE атаки
- [2026-05-27] Реализован cheetah texture changer (нужны доработки)
- [2026-05-27] Удалён кастомный MCP сервер, установлен официальный Unity MCP

## Текущее состояние
_Что сейчас в работе._

Ветка `iteration_2/Alexandr/Develop` активна. Последняя задача завершена и проверена в Unity (0 ошибок компиляции; 26 EditMode тестов Dodge/AoE зелёные): вероятности срабатывания Dodge для Fox вынесены из хардкода `Dodge.CanUse` в данные.
- Создан подкласс `FoxStats : AnimalStats` (`Code/Data/Animals/FoxStats.cs`) с `_ownerDodgeChance`/`_inheritedDodgeChance` (геттеры `OwnerDodgeChance`/`InheritedDodgeChance`, константы `DefaultOwnerDodgeChance=50`/`DefaultInheritedDodgeChance=30`, `[Range(0,100)]`).
- Ассет `Assets/Settings/Animals/Stats/FoxStats.asset` сконвертирован in-place с типа `AnimalStats` на `FoxStats` (сменён `m_Script` GUID → `97b3a2ae85c1743b2ba7cc12efc71c60`, добавлены `_ownerDodgeChance:50`/`_inheritedDodgeChance:30`; Health/Damage/Tiles сохранены).
- `Dodge` теперь принимает `int successChance` вместо `bool isOwner` (магические 50/30 удалены). `FoxFacade` инжектит `AnimalDatabase`, читает `GetStats(Type) as FoxStats` (фоллбэк на дефолтные константы + LogWarning), передаёт ownerChance в `Dodge`, inheritedChance в `new FoxMergeSkill(inheritedChance)`. `FoxMergeSkill` хранит `_inheritedChance` и использует в обоих `Merge`-перегрузках.
- Изначально пробовал хранить данные в `AnimalConfig` (`DodgeSettings` + `GetDodgeSettings`), по просьбе пользователя переделал на подкласс `FoxStats` как более чистое решение — `AnimalDatabase.cs` возвращён в исходное состояние.

Новые/изменённые файлы пока НЕ закоммичены: `FoxStats.cs` (+`.meta`) новый, `FoxStats.asset`, `Dodge.cs`, `FoxFacade.cs`, `IMergeSkill.cs`, тестовые файлы. Тюнить вероятности можно прямо в Inspector на ассете Fox Stats.

Предыдущая задача (merge popup, `MergePopupView`/`MergePopupController`, событие `MergeTarget.Merged`, `AnimalConfig.MergeInfo`) — код на месте; могла остаться ручная настройка в Inspector (префаб `MergePopupView.prefab`, навеска `MergePopupController` на player-префабы, заполнение `MergeInfo`).

## Ключевые решения
Подробности в `Knowledge/Decisions/`. Краткий список:
- [2026-06-27] Merge popup показывается ТОЛЬКО при cross-type мердже (когда реально выдан новый скилл) и ключуется по типу SOURCE (потреблённого) животного; событие `Merged` гейтится флагом `grantedNewSkill` в `ExecuteMergeDirectly`, минует undo → `Decisions/2026-06-27-merge-popup.md`
- [2026-06-27] Подсветка хода вынесена в отдельную сущность (SRP) — `AnimalMovement` не нагружали; drag vs hold различается порогом смещения в пикселях, IInputService не трогали → `Decisions/2026-06-27-move-range-highlighter.md`
- [2026-06-27] Per-animal тюнинг-данные способностей (вероятности Dodge для Fox) хранятся в подклассе `AnimalStats` (`FoxStats`), а НЕ в общем `AnimalConfig` — данные живут со статами животного и не «протекают» в конфиги остальных; `Dodge` стал value-agnostic (`int successChance`), owner/inherited решается на стороне вызова → `Decisions/2026-06-27-fox-dodge-stats.md`
- CLAUDE.md ≤70 строк — детали в AgentsDocs/
- No Singleton — только Zenject bindings
- UniTask — новый async код только на UniTask
- Deprecated Pathfinding/ — оставлены до удаления, не трогать

## Протокол сессии
**Начало**: этот файл загружен автоматически — ничего делать не нужно.
**Конец задачи**: когда задача завершена, запусти `/update-knowledge` — скилл сам обновит этот файл.
Если пользователь говорит "готово", "задача выполнена", "закончили" → проактивно запускай `/update-knowledge`.
