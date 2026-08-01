# Решение: туториал вынесен в отдельный модуль-надстройку над Battle, шаг = один класс `ITutorialStep`

**Дата:** 2026-08-01
**Статус:** принято, реализовано (обвязка сцены — на пользователе)

## Контекст
Задача: показать новому игроку на первом уровне туториальную руку (`Assets/Code/Framework/Sprites/hand 1.png`), которая слайдит с одного животного на другое и объясняет, что их надо смерджить. Пара животных настраивается, подсказка показывается один раз.

Ограничения, определившие форму решения:
- Туториал — не разовый костыль: за мерджем последуют подсказки по атаке, перемещению, старту боя. Значит нужна не «одна рука», а каркас, в который шаг добавляется без правки существующего кода.
- `Framework/` по `CLAUDE.md` — стабильная база, трогать нельзя.
- Сцена `Main Scene.unity` общая для параллельных сессий Claude Code — редактировать её агентом нельзя.

## Рассмотренные варианты
1. **`TutorialController` с `switch` по шагам / цепочкой `if`.** Минус: каждый новый шаг = правка центрального класса, растущий god-object, невозможно отключить шаг данными.
2. **Туториал внутри геймплейного кода** (`MergeTarget` сам знает, что идёт обучение, и дёргает руку). Минус: обучение протекает в домен, `MergeTarget` получает ответственность, к мерджу не относящуюся; выключить туториал = править геймплей.
3. **Отдельный прикладной модуль-надстройка + шаги как стратегии** (выбрано).

## Принятое решение

### Модуль `Assets/Code/Tutorial/` — читает состояние, командует только своим view
Слои:

```
Runner       TutorialRunner (IInitializable/IDisposable)  — идёт по List<ITutorialStep>
Step         ITutorialStep: Id · CanRun · UniTask<bool> RunAsync(CancellationToken)
             MergeHintStep — ждёт свой триггер внутри RunAsync
Resolver     IMergeHintTargetResolver (SpawnOrder | AnimalType) → MergeHintPair
View         ITutorialHandView / TutorialHandView — Image + DOTween-цикл, мир→экран
Progress     ITutorialProgressService → PlayerPrefsTutorialProgressService, ключ Tutorial.{stepId}
Config       MergeHintStepConfig : ScriptableObject
```

Внутрь модуль ходит только через существующие интерфейсы (`IUnitTracker`, `IPersistentProgressService`) и `SignalBus`. Геймплейный код о туториале не знает — **единственное исключение** — новый сигнал `AllyMergedSignal`, который `MergeTarget` фаерит наружу.

**Новый шаг = один класс + одна строка биндинга в `TutorialInstaller`.** `TutorialRunner` править не надо: он идёт по `List<ITutorialStep>` в порядке биндингов, пропускает пройденные (`ITutorialProgressService.IsCompleted`) и `CanRun == false`, а `RunAsync` возвращает `bool` — засчитывать шаг или нет.

### Старт последовательности — по `PreBattlePhaseStartedSignal` с `IsLevelStart == true`, а не в `Initialize()`
Zenject зовёт `IInitializable.Initialize` из `MonoKernel.Start`, когда `GameBootstrapper.Awake` только вошёл в `BootstrapState` — `PlayerProgress` там может быть ещё `null`, и `CanRun` (сверка `Progress.Level == _config.LevelNumber`) отработал бы по пустоте. К моменту `PreBattlePhaseStartedSignal` прогресс гарантированно загружен: `LoadProgressState` → `LoadLevelState` → `BattleLoopState` → `PreBattleState`. `Initialize()` поэтому только подписывается.

### Выбор пары — по индексу спавна (default), не по координатам сетки
`IUnitTracker._playerUnits` — обычный `List` в порядке регистрации, то есть в порядке спавна, и `GetAlivePlayerUnits()` порядок сохраняет. Конфиг задаёт индексы `0 → 1`.

Координаты сетки отвергнуты: `GridManager.PlaceOnGrid` ищет первую свободную клетку рядов `y = 0..1`, поэтому позиция животного зависит от `UnitSize` ранее поставленных юнитов (Elephant 2×2 vs Cheetah 1×2) и от `GridConfig` — «животное в клетке (0,0)» не является стабильным адресом.

Режим выбирается стратегией `IMergeHintTargetResolver` с полем `Mode`; обе реализации (`SpawnOrderTargetResolver`, `AnimalTypeTargetResolver`) биндятся, шаг в конструкторе берёт ту, чей `Mode` совпал с конфигом. Третий режим = один класс + один член enum.

### Новый сигнал `AllyMergedSignal` вместо подписки на события `MergeTarget`
`MergeTarget.Merged` гейтится флагом `grantedNewSkill` (фаерится только на cross-type мердже — см. [[2026-06-27-merge-popup]]), а подписка на `Merge` конкретных инстансов заставила бы туториал знать про Presentation-компонент и не засчитала бы мердж любой другой пары. `MergeTarget.ExecuteMergeDirectly` теперь фаерит `AllyMergedSignal` под null-guard перед `return true`; сигнал объявлен в `BattleInstaller` как `.OptionalSubscriber()`.

### Прогресс туториала — отдельный сервис на PlayerPrefs
`ITutorialProgressService` с ключом `Tutorial.{stepId}`, а не поле в `PlayerProgress`: добавление поля в сериализуемый JSON затронуло бы `SaveLoadService`, `WinState`, `LoseState` внутри неприкосновенного `Framework/`.

## Почему так
- **Шаг сам ждёт свой триггер.** `RunAsync` — не «покажи и верни управление», а вся жизнь шага: дождаться появления пары → показать руку → дождаться мерджа → вернуть `true`. Runner остаётся тупым итератором, а шаги могут иметь произвольные условия завершения (таймер, тап, сигнал), не расширяя контракт.
- **Резолв пары — событийный, не пофреймовый.** `IUnitTracker.GetAlivePlayerUnits()` это `Where(...).ToList()` — поллинг каждый кадр всё пре-батл окно давал бы аллокацию на кадр на мобиле. Используется `UniTaskCompletionSource<MergeHintPair>` поверх `IUnitTracker.PlayerUnitsChanged` + немедленная первая попытка резолва (животные могут уже стоять).
- **Отписка от `SignalBus` должна быть синхронной относительно teardown контейнера.** `UniTask.WaitUntil` poll-based и не возобновляется синхронно на `Cancel()` — к следующему тику PlayerLoop Zenject уже отработал `SignalBus.LateDispose()`, и `Unsubscribe` кинул бы (`throwIfMissing: true`). Решено обёрткой `CancelAwareSignalSubscription<T>`: она отписывается ровно один раз — из `Dispose()` или из `CancellationTokenRegistration`, что раньше. `CancellationTokenSource.Cancel()` выполняет регистрации синхронно, а `SignalBus` — `ILateDisposable` (сносится после всех `IDisposable`), поэтому `TutorialRunner.Dispose()` успевает размотать подписки до исчезновения шины.
- **Ввод не блокируется**: `raycastTarget = false` у руки, `CanvasGroup.blocksRaycasts = false`, затемняющего оверлея нет — подсказка как раз требует, чтобы игрок сделал мердж сам.
- **Конвертация мир→экран через `null`-камеру.** Canvas из `Assets/Resources/Canvas.prefab` — Screen Space Overlay, поэтому камера-аргумент `null` в `RectTransformUtility.ScreenPointToLocalPointInRectangle` корректен (прецедент — `MergePopupController`).
- **Пивот руки (0.32, 0.95) — на кончике пальца**, не в центре спрайта: анкер тогда указывает ровно на животное, а `DOScale` press-анимация «нажимает» в правильной точке.
- **Сцена не редактировалась осознанно** — Unity Editor общий для параллельных сессий. Вместо этого `TutorialInstaller.ValidateSceneReferences()` логирует внятную ошибку на каждый незаполненный слот (прецедент — [[2026-07-31-pre-battle-phase-architecture]]).

> [!warning] UniTask-адаптер DOTween (`ToUniTask`) в этом проекте НЕДОСТУПЕН
> `UniTask.DOTween.asmdef` гейтит всё за `UNITASK_DOTWEEN_SUPPORT`, а `versionDefines` эмитит этот define только при установленном UPM-пакете `com.demigiant.dotween`. Здесь DOTween поставлен из Asset Store (`Assets/Plugins/Demigiant/DOTween/DOTween.dll`), define не выставляется, `DOTweenAsyncExtensions` не компилируется (проверено: `unity_reflect` находит 0 типов).
> Вместо `AsyncWaitForCompletion().AsUniTask()` (Task-based, игнорирует токен, ломается при выходе из PlayMode) используется
> `UniTask.WaitUntil(() => !seq.IsActive() || seq.IsComplete(), cancellationToken: token).SuppressCancellationThrow()`.
> Чтобы включить нативный `ToUniTask`, надо добавить `UNITASK_DOTWEEN_SUPPORT` в `scriptingDefineSymbols` для Android/Standalone/iPhone — **не делали**.

## Проверка
- Unity Console: 0 ошибок компиляции по новому коду (остались предсуществующие NRE `UnityEditor.Graphs.Edge.WakeUp` и ошибки параметров `ChickentwoLayerController` из `EnemySpawnService.cs:86`).
- Новые тесты не писались: действует **TESTS PAUSED**.
- ⚠️ **Фича ещё не проверена в рантайме** — до обвязки сцены (см. ниже) `TutorialInstaller` не установлен и модуль не активен.

> [!todo] Осталось сделать пользователю — обвязка сцены
> На `SceneContext` в `Assets/Scenes/Main Scene.unity`: добавить компонент `TutorialInstaller`, дописать его в `Mono Installers`, назначить `_mergeHintConfig` = `Assets/Settings/TutorialConfigs/MergeHintStepConfig.asset`, `_handViewPrefab` = `Assets/Prefabs/TutorialHandView.prefab`.
> Сброс прохождения для повторного теста — удалить ключ PlayerPrefs `Tutorial.MergeHint`.

## Известные компромиссы (осознанные)
- `MergeCommand.Undo()` не фаерит контр-сигнал → отменённый мердж всё равно засчитывает туториал.
- Шаг завершается по **любому** `AllyMergedSignal`, не обязательно по подсвеченной паре.
- Биндится ровно один `MergeHintStepConfig` `AsSingle` — второй merge-hint (другой уровень / другая пара) потребует ID-биндингов или списка конфигов. Шаги *другого типа* добавляются свободно.
- Если игрок начал бой, не смерджив, ключ PlayerPrefs не пишется и подсказка появится снова (флаг `MarkCompletedWhenPhaseEnds` в конфиге это меняет).

## Затронутые файлы
Новые: `Code/Tutorial/{ITutorialStep, TutorialRunner, CancelAwareSignalSubscription}.cs`; `Code/Tutorial/Progress/{ITutorialProgressService, PlayerPrefsTutorialProgressService}.cs`; `Code/Tutorial/Signals/TutorialStepCompletedSignal.cs`; `Code/Tutorial/Config/MergeHintStepConfig.cs`; `Code/Tutorial/Steps/{MergeHintStep, IMergeHintTargetResolver, MergeHintTargetMode, MergeHintPair, SpawnOrderTargetResolver, AnimalTypeTargetResolver}.cs`; `Code/Tutorial/UI/{ITutorialHandView, TutorialHandView}.cs`; `Code/Battle/Signals/AllyMergedSignal.cs`; `Code/Infrastructure/Installers/TutorialInstaller.cs`; `Assets/Prefabs/TutorialHandView.prefab`; `Assets/Settings/TutorialConfigs/MergeHintStepConfig.asset`.
Изменены: `Code/Animals/Merge/MergeTarget.cs` (`SignalBus` в `[Inject] Construct`, фаер `AllyMergedSignal` в `ExecuteMergeDirectly`), `Code/Infrastructure/Installers/BattleInstaller.cs` (`DeclareSignal<AllyMergedSignal>().OptionalSubscriber()`).
