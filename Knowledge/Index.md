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

- [2026-05-27] Setup Codex, обновлены настройки проекта
- [2026-05-27] Добавлено рисование gizmos для AOE атаки
- [2026-05-27] Реализован cheetah texture changer (нужны доработки)
- [2026-05-27] Удалён кастомный MCP сервер, установлен официальный Unity MCP

## Текущее состояние
_Что сейчас в работе._

Ветка `iteration_2/Alexandr/Develop` активна. Cheetah texture changer реализован, помечен как требующий доработки. AOE gizmo drawing добавлено.

## Ключевые решения
Подробности в `Knowledge/Decisions/`. Краткий список:
- CLAUDE.md ≤70 строк — детали в AgentsDocs/
- No Singleton — только Zenject bindings
- UniTask — новый async код только на UniTask
- Deprecated Pathfinding/ — оставлены до удаления, не трогать

## Протокол сессии
**Начало**: этот файл загружен автоматически — ничего делать не нужно.
**Конец задачи**: когда задача завершена, запусти `/update-knowledge` — скилл сам обновит этот файл.
Если пользователь говорит "готово", "задача выполнена", "закончили" → проактивно запускай `/update-knowledge`.
