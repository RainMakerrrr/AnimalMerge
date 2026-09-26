# Решение: баннеры стадий «MERGE» / «FIGHT!» — блокирующий шаг состояний через порт `IStageAnnouncer` + ref-counted блокировка ввода `IPlayerInputLock`

**Дата:** 2026-09-26
**Статус:** принято, реализовано (код + конфиг). Не закоммичено на момент записи. Заменяет
[[2026-09-26-stage-banners-signal-driven|неблокирующий презентер на сигналах]] — отклонённый там
вариант «шаг state machine» теперь выбран.

## Контекст

Первая версия баннеров была неблокирующей: `StageBannerPresenter` слушал сигналы фаз и показывал
баннер fire-and-forget. Выяснилось, что так нельзя:

- Во время MERGE игрок уже мог тащить животных, жать ADD ANIMAL и FIGHT — баннер висел поверх
  живого геймплея.
- FIGHT! перекрывался первым ходом: юниты начинали двигаться до того, как баннер уехал.
- Рука туториала появлялась одновременно с MERGE (~2.35 с перекрытия).
- В проекте не было **никакого** общего гейта ввода для drag/merge — только readiness-гейт кнопки
  FIGHT.

Ограничения:

- Рестарт боя **не вызывает `Exit`** у `PreBattleState` — он только отменяет
  `BattleFlowController.BattleToken`. Состояние, ждущее баннер, должно корректно выходить по токену
  и не трогать поля, перезаписанные новым прогоном.
- Отменённый прогон может отпустить блокировку ввода позже, чем новый её возьмёт.
- Внешний kill твина (teardown сцены, `DOTween.KillAll`) не должен оставлять игру в софт-локе.

## Рассмотренные варианты

1. **Оставить сигналы и добавить отдельную блокировку ввода по тем же сигналам.** ❌ Отклонено:
   два независимых подписчика должны синхронно знать, когда баннер закончился; первый ход всё равно
   не ждёт FIGHT!.
2. **Состояние ждёт View напрямую (`IStageBannerView` в `PreBattleState`).** ❌ Отклонено:
   Application-слой зависел бы от Presentation; нет места для null-object при отсутствии префаба.
3. **Булев флаг «ввод заблокирован».** ❌ Отклонено: поздний `false` от отменённого прогона
   разблокирует новый прогон посреди его MERGE.
4. **Порт `IStageAnnouncer` в Application, реализация в UI; состояния await-ят показ под
   ref-counted `IPlayerInputLock`; FIGHT! живёт в `BattleStartState`.** ✅ Принято.

## Принятое решение

- **Порт** `Code.Battle.Services.IStageAnnouncer`:
  `UniTask<bool> AnnounceAsync(StageBannerKind kind, CancellationToken ct)`.
  `true` — показ закончен или показывать нечего; `false` — прерван. На отмене **не бросает**.
  Внешний kill твина → `true` (игра продолжается, софт-лока нет).
- **Реализации**: `StageBannerAnnouncer` (`Code/Battle/UI/StageBanner/`) — заменил удалённый
  `StageBannerPresenter`, на сигналы больше не подписан; `SilentStageAnnouncer` — null-object,
  биндится, если в `BattleInstaller` нет конфига или префаба.
- **Блокировка ввода** `Code.Battle.Input.IPlayerInputLock` / `PlayerInputLock`: счётчик
  захватов, `Acquire()` → `PlayerInputLockHandle` (идемпотентный `IDisposable`), `IsLocked`,
  событие `LockChanged`, `WaitUntilUnlockedAsync(ct)`. Первый в проекте общий гейт drag/merge.
  Потребители:
  - `PreBattleHudPresenter` — ADD ANIMAL `SetInteractable(false)` (видимо), FIGHT
    `SetReady(false)`, пересчёт на `LockChanged`, ранний выход в `OnAddAnimalClicked`;
  - `AnimalMover` — в `Update` при блокировке `AbortGesture`: перетаскиваемое животное
    возвращается через общий `ReturnToOriginalPosition`, выбор сбрасывается;
  - `TutorialRunner` — ждёт разблокировки перед шагами, рука не перекрывает MERGE.
- **`PreBattleState.Enter`**: токены `stateToken` / `battleToken` захватываются в локальные
  переменные, поля после `await` не перечитываются. Под `using (_inputLock.Acquire())`: спавн
  врагов → проверка `stateToken` → `EnemiesSpawnedSignal` → подписка на `StartBattleRequested` →
  `PreBattlePhaseStartedSignal` → `await` MERGE → `_battleReadiness.Activate()` (отложенная
  активация переиспользует readiness-гейт и для кнопки FIGHT, и для клавиши P).
- **`PreBattleState.StartBattleAsync`**: `BattleStartedSignal` (зум камеры идёт параллельно с
  баннером) → `ChangeStateAsync<BattleStartState>`. Прежде мёртвое `BattleStartState` теперь
  используется: свой CTS, связанный с `BattleToken`; ждёт FIGHT! под блокировкой → в
  `PlayerTurnState` только если показ завершён. `BattleStateMachine.Initialize` получает
  `IStageAnnouncer` и `IPlayerInputLock`.
- **Порядок сигналов**. Подготовка: `EnemiesSpawned` → `PreBattlePhaseStarted` (камера, правила,
  HUD показан заблокированным, туториал ждёт) → MERGE → `readiness.Activate` → разблокировка.
  Бой: `BattleStarted` → `PreBattlePhaseEnded` (`Exit`) → FIGHT! под блокировкой →
  `PlayerTurnState`.
- **View** (заодно закрыты предложения ревью первой версии): ожидание DOTween через `OnKill` →
  `UniTaskCompletionSource` вместо покадрового `WaitUntil`; `HideImmediate` удалён; `null`-материал
  стиля → материал шрифта по умолчанию; `OnValidate` предупреждает о пустом материале заголовка /
  подзаголовка, виде без стиля и дубле вида; `_exitDirection` (int) → `StageBannerExitSide
  { Left, Right } _exitSide`; `TitleOffset` → `ContentOffset`
  (`[FormerlySerializedAs("_titleOffset")]`).

## Почему так

- Баннер стал частью темпа стадии, а не косметикой: порядок «объявили → можно играть» — это логика
  флоу, её место в состояниях. Порт держит состояния независимыми от UI и даёт null-object.
- Контракт `bool` без исключений на отмене упрощает состояния: одна ветка «продолжить / выйти», без
  `try/catch` вокруг каждого показа. Kill твина → `true` выбран сознательно: лучше пропустить
  баннер, чем зависнуть.
- Счётчик захватов с идемпотентным хендлом: поздний `Dispose` отменённого прогона снимает только
  **свой** захват и не разблокирует новый прогон; двойной `Dispose` безопасен.
- Локальные токены в `Enter` закрывают особенность рестарта (нет `Exit`, поля перезаписываются
  новым прогоном).
- FIGHT! в `BattleStartState` оживляет существующее состояние вместо ещё одного `await` в
  `PreBattleState` и чисто отделяет «подготовка кончилась» от «бой начался».

## Проверка

- EditMode: BattleSystem + `MergeUndoServiceTests` 67/67; PlayMode: тесты вьюх кнопок 7/7.
- Play Mode: тайминг блокировки/разблокировки, FIGHT! до первого хода, рестарт посреди MERGE и
  посреди FIGHT!.
- **Не проверено**: блокировка drag (нет симуляции указателя), скрытие руки туториала во время
  первого MERGE, MERGE на второй стадии.
- Побочные эффекты: drag заблокирован и во время спавна врагов в начале стадии; каждая стадия
  длиннее примерно на 2 × 2.35 с (настраивается в `StageBannerConfig.asset`).
- Открытые предложения ревью: `SilentStageAnnouncer` должен возвращать
  `!ct.IsCancellationRequested`; `StageBannerAnnouncer` должен ловить неотменные исключения из
  `PlayAsync` (например, `MissingReferenceException` на teardown) и возвращать `false`; в
  `PreBattleState.Enter/Exit` остались старые `//`-комментарии.

> [!warning] Найден предсуществующий баг (не чинился, отдельная задача)
> Рестарт во время хода врага: состояния старого прогона (`EnemyTurnState` → `CheckVictoryState`)
> продолжают менять состояние уже на новом прогоне и обрезают новый MERGE. Фикс — ходовые
> состояния должны проверять battle-токен перед `ChangeStateAsync`.

## Затронутые файлы

- `Assets/Code/Battle/Services/IStageAnnouncer.cs`, `SilentStageAnnouncer.cs` (новые)
- `Assets/Code/Battle/Input/IPlayerInputLock.cs`, `PlayerInputLock.cs`,
  `PlayerInputLockHandle.cs` (новые)
- `Assets/Code/Battle/UI/StageBanner/StageBannerAnnouncer.cs` (новый), `IStageBannerView.cs`,
  `StageBannerView.cs`; `StageBannerPresenter.cs` удалён
- `Assets/Code/Battle/Config/StageBannerConfig.cs`, `StageBannerStyle.cs`,
  `StageBannerExitSide.cs` (новый)
- `Assets/Code/Battle/States/PreBattleState.cs`, `BattleStartState.cs`
- `Assets/Code/Battle/StateMachine/BattleStateMachine.cs`
- `Assets/Code/Battle/UI/PreBattleHudPresenter.cs`
- `Assets/Code/Animals/AnimalMover.cs`
- `Assets/Code/Tutorial/TutorialRunner.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Settings/BattleConfigs/StageBannerConfig.asset`
