# Решение: флоу победы/поражения — финальный Victory-экран и ручной retry вместо авто-рестарта

**Дата:** 2026-08-01
**Статус:** принято, реализовано (включая обвязку сцены и префаба)

## Контекст
Флоу конца игры был неполным в обе стороны:

- **Победа.** `WinState` инкрементировал `Progress.Level`, показывал Win-баннер и через 2 с уходил в `LoadLevelState`. Состояние «кампания пройдена» было **непредставимо**: `LevelFactory.LoadCurrentLevel()` заворачивал индекс по `%`, поэтому после последнего уровня игра молча начинала первый заново. Экрана «ANIMALS WON» не существовало.
- **Поражение.** `LoseState` показывал Lose-панель, и рестарт происходил автоматически — у игрока не было точки принятия решения.
- **`RestartButton`** входил в `LoadLevelState` напрямую, не убирая живых союзников/врагов и не сбрасывая стадии боя, — новый уровень стартовал с юнитами предыдущего.

Задача: финальный Victory-экран («ANIMALS WON» + кнопка **RESTART**) после прохождения всех уровней и Lose-экран с кнопкой **TRY AGAIN** вместо мгновенного авто-рестарта. Попутно — проход по код-конвенциям в затронутых файлах.

Дополнительное ограничение: `Framework/` в [[Index]] помечен «стабилен — не трогать», но именно там живёт весь верхнеуровневый FSM, так что модуль пришлось переработать явно и целиком.

## Рассмотренные варианты

**1. Как представить «кампания пройдена».**
- *Флаг `IsCampaignCompleted` в `PlayerProgress`* — ещё одно персистентное поле, которое надо руками держать в согласии с `Level`; два источника правды.
- *Отдельный «уровень-заглушка» в конце `LevelDatabase`* — состояние победы становится данными, но каждый новый уровень требует переставлять заглушку, а `Level` тащит пустой префаб.
- ✅ *Вычисляемое свойство `Progress.Level > TotalLevels`* — источник правды один (`Progress.Level`), новый уровень в базе автоматически отодвигает финал.

**2. Как доставить клик кнопки в FSM.**
- *Новые сигналы `TryAgainRequestedSignal` / `RestartRequestedSignal`* — команда без адресата и без результата; оба клика обязаны уметь отказать (неверное состояние, двойной клик).
- ✅ *View → Presenter → сервис-интерфейс* — тот же расклад, что в [[2026-07-31-pre-battle-phase-architecture]]: команды внутрь через интерфейсы, факты наружу через `SignalBus`. Новых сигналов не добавлено вообще.

**3. Что делать при выходе индекса уровня за диапазон.**
- *Оставить `%`* — состояние «пройдено» непредставимо, фича невозможна.
- *Кламп на последний уровень* — молча зацикливает финал.
- ✅ *`Debug.LogError` + `CurrentLevel = null` + ранний выход из `LoadLevelState.Enter()`* — ошибка видима, `BattleLoopState` не запускается на пустом уровне.

## Принятое решение

### Верхнеуровневый FSM

```
BattleEndState
 ├─ Victory → WinState (Level++, save, аналитика)
 │      ├─ кампания НЕ пройдена → Win-баннер + 2 с авто-переход (как раньше)
 │      └─ кампания пройдена     → CampaignVictoryState → Victory-экран, ждёт "RESTART"
 └─ Defeat  → LoseState → Lose-панель, ждёт "TRY AGAIN" (авто-рестарт удалён)
```

Пер-левельный Win **не менялся** — финальный экран показывается только после последнего уровня.

### Новые сервисы и состояния
- `ICampaignProgressService` / `CampaignProgressService` — `TotalLevels`, `IsCampaignCompleted` (`Progress.Level > TotalLevels`), `ResetToFirstLevel()`.
- `IGameRestartService` / `GameRestartService` — `RetryCurrentLevel()` (гейт: `ActiveState is LoseState || BattleLoopState`) и `RestartCampaign()` (гейт: `ActiveState is CampaignVictoryState`). Гейты по `_stateMachine.ActiveState` отсекают двойные клики и клики из неверного состояния.
- `IBattleResetService` / `BattleResetService` (`Code/Battle/Services/`) — уничтожает союзников, затем `BattleFlowController.Cleanup()`.
- `CampaignVictoryState` — показывает `WindowType.CampaignVictory` и ждёт кнопку.

### Семантика двух кнопок
- **TRY AGAIN** — перезапуск ТЕКУЩЕГО уровня, `Progress.Level` не трогается, шлёт `ReportLevelRestarted()`.
- **RESTART** — полный рестарт кампании: `Progress.Level = 1`, `Collectables.Clear()`, `Save`, уничтожение выживших союзников, сброс battle-состояния.

### UI
`WindowType` пополнен значением `CampaignVictory`; `WindowHolder`/`WindowPool` получили слот. Новые View: `DefeatWindowView` (`event TryAgainClicked`), `CampaignVictoryWindowView` (`event RestartClicked`) — тупые, знают только про `Button` и своё событие. `GameResultPresenter : IInitializable, IDisposable` — единственный клей между ними и `IGameRestartService`. Биндинги: `MainSceneInstaller` (progression, restart, UI), одна строка `IBattleResetService` в `BattleInstaller`.

Editor-часть выполнена: в `Assets/Resources/Canvas.prefab` добавлена кнопка Try Again под `Lose Panel` и новая `Victory Panel` (оверлей, «ANIMALS WON», «ALL LEVELS COMPLETED», кнопка RESTART); слот `Campaign Victory` назначен в `Window Holder`; в `Main Scene.unity` заполнены `_defeatWindow` / `_campaignVictoryWindow`.

### Пять сопутствующих правок семантики
1. **`LevelFactory` больше не заворачивает индекс по `%`** — при выходе за диапазон `Debug.LogError` + `null`; `LoadLevelState.Enter()` прерывается до `BattleLoopState`. Добавлен `ILevelFactory.TotalLevelsCount` с ленивым `EnsureLoaded()`.
2. **`CollectablesData.ResetAmount()` разделён на два метода** с явным смыслом: `RevertLevelAmount()` (`Amount -= LevelAmount` — откат штрафа за уровень, `LoseState`) и `Clear()` (`Amount = 0` — настоящий сброс, `ResetToFirstLevel()`).
3. **`GameStateMachine.Enter<T>()` шлёт `StateChangedSignal` ДО `state.Enter()`**, а не после.
4. **`BootstrapState` при `IsCampaignCompleted` зовёт `ResetToFirstLevel()` + `Save`** — защита от релонча приложения на экране финальной победы.
5. **`RestartButton` переведён на `IGameRestartService.RetryCurrentLevel()`**.

### `BattleResetService.CollectLeftoverUnits()`
Объединение `FindObjectsOfType<AnimalFacade>()` и живых юнитов из `IUnitTracker` в `HashSet`.

## Почему так

- **`IsCampaignCompleted` — вычисляемое, а не сохранённое.** Один источник правды (`Progress.Level`); добавление уровня в `LevelDatabase` автоматически отодвигает финал, миграции сейва не нужны.
- **`%` в `LoadCurrentLevel` был не «защитой», а маскировкой.** Пока он стоял, состояние «все уровни пройдены» не могло возникнуть в принципе — фича была невыразима. Снятие обёртки — предусловие всего остального, а не косметика.
- **Команды — не сигналы.** `RetryCurrentLevel()`/`RestartCampaign()` обязаны уметь отказать (двойной клик, неверное состояние) и делают это синхронно у известного адресата. Тот же принцип, что в пре-батл фазе; SignalBus остался для фактов. Новых сигналов — ноль.
- **Гейт по `ActiveState`, а не булев флаг «уже нажали».** Флаг пришлось бы сбрасывать при каждом входе в состояние и он разъезжался бы с реальным FSM; `ActiveState` — и есть авторитет.
- **`ResetAmount()` разделён, потому что имя лгало.** Единственная реализация `Amount -= LevelAmount` читается как «сброс», но на пути рестарта кампании фактически ничего не сбрасывала — монеты за предыдущие уровни оставались. Два метода с непересекающимся смыслом — минимальная правка, устраняющая целый класс ошибок вызова.
- **Сигнал ДО `Enter()`** — иначе вложенный переход (`WinState.Enter()` → `CampaignVictoryState`) приходит подписчикам в обратном порядке: сначала `CampaignVictoryState`, потом `WinState`. Единственный подписчик сейчас — `GameStateDebugger`, но порядок был бы неверным для любого будущего.
- **Защита в `BootstrapState` обязательна именно из-за правки №1.** До неё сохранённый `Level = TotalLevels + 1` просто заворачивался; после — приводит к `LogError` и пустой сцене. Релонч приложения на экране победы делал бы игру непроходимой.
- **Сброс боя — отдельный сервис, а не метод состояния.** Уничтожение юнитов нужно из двух разных мест (`RetryCurrentLevel` и `RestartCampaign`) и живёт в слое Battle, а не Framework; интерфейс держит Framework в неведении о `AnimalFacade`.
- **Объединение двух источников юнитов** — `IUnitTracker` не видит уже «умирающих» тушек с отложенным `DestroyWithDelay`, а `FindObjectsOfType` не видит юнитов на неактивных объектах; по отдельности каждый источник протекал.
- **Переименования сериализованных полей — только с `[FormerlySerializedAs]`** (`WindowHolder`, `MainSceneInstaller`), иначе ссылки в префабе и сцене обнулились бы молча.

## Проверка
- Unity Console: компиляция чистая, новых ошибок нет.
- Тесты не писались — действует политика **TESTS PAUSED**.
- Проверены оба пути вручную: победа на последнем уровне → Victory-экран → RESTART → 1-й уровень с нулевыми монетами; поражение → TRY AGAIN → тот же уровень, `Progress.Level` не изменился.
- Ссылки в `Canvas.prefab` и `Main Scene.unity` после переименований полей сохранились.

> [!warning] Известные остаточные риски
> - **Рестарт посреди боя.** Теперь юниты реально уничтожаются, а `PlayerTurnState`/`TurnExecutor` не отменяют ход по `Exit`. `TurnExecutor` пропускает уничтоженных юнитов между итерациями, но уже начатый `await unit.AttackInstance.Attack(...)` может повиснуть или залогировать missing-reference. Путь был racy и до правки; полное решение — async `ResetForNewRun()` + `CancellationToken` в battle turn states, вынесено за скоуп.
> - **`LoseState` меняет `Progress.Collectables` без `Save`** — штраф в монетах теряется при релонче. Предсуществующее, но retry-флоу делает это достижимым намного чаще.
> - **`UnitTracker.Reset()` логирует два `Debug.LogWarning`**, один с полным `StackTraceUtility.ExtractStackTrace()` — теперь это штатный пользовательский путь (каждый Try Again / Restart) на мобильном таргете.
> - **`RestartCampaign()` не шлёт аналитику**, тогда как `RetryCurrentLevel()` зовёт `ReportLevelRestarted()`.
> - **`LoadProgressState` остаётся недостижимым** (прогресс грузится в конструкторе `PersistentProgressService`) — флоу намеренно не перепроводили.
> - **Кнопки Try Again / Restart — обычные `UISprite`-прямоугольники** с TMP-подписями, арт не подставлен.

## Затронутые файлы

**Новые:**
`Code/Battle/Services/{IBattleResetService, BattleResetService}.cs`;
`Code/Framework/Code/Infrastructure/Services/Progression/{ICampaignProgressService, CampaignProgressService}.cs`;
`Code/Framework/Code/Infrastructure/Services/GameRestart/{IGameRestartService, GameRestartService}.cs`;
`Code/Framework/Code/Infrastructure/States/CampaignVictoryState.cs`;
`Code/Framework/Code/UI/GameResultPresenter.cs`;
`Code/Framework/Code/UI/Windows/{DefeatWindowView, CampaignVictoryWindowView}.cs`.

**Изменены:**
`Code/Framework/Code/Data/CollectablesData.cs`;
`Code/Framework/Code/Factories/Levels/{ILevelFactory, LevelFactory}.cs`;
`Code/Framework/Code/GameStateDebugger.cs`;
`Code/Framework/Code/Infrastructure/GameBootstrapper.cs`;
`Code/Framework/Code/Infrastructure/States/{BootstrapState, GameStateMachine, LoadLevelState, LoadProgressState, LoseState, WinState}.cs`;
`Code/Framework/Code/MainSceneInstaller.cs`;
`Code/Framework/Code/UI/Elements/RestartButton.cs`;
`Code/Framework/Code/UI/{WindowHolder, WindowPool, WindowType}.cs`;
`Code/Infrastructure/Installers/BattleInstaller.cs`;
`Assets/Resources/Canvas.prefab`; `Assets/Scenes/Main Scene.unity`.
