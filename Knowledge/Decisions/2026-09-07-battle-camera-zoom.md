# Решение: приближение камеры при старте боя — долли по сигналам фазы, а не анимация FOV по клику

**Дата:** 2026-09-07
**Статус:** принято, реализовано (код + конфиг + сцена). Не закоммичено и **не импортировано Unity**
на момент записи

## Контекст

Нужно было «приближать камеру, когда начинается бой»: наезд по нажатию Fight, отъезд при
возврате к подготовке и в конце боя, параметры анимации — в руках геймдизайнера.

Три вещи в проекте определили решение сильнее, чем сама формулировка задачи:

- **У `Main Camera` есть дочерняя `HealthBarCamera`** — clearFlags Depth only, culling mask = слой 8
  `HealthBar`, собственный FOV 60. Хелсбары рисуются второй камерой поверх мира и совпадают
  с юнитами ровно потому, что обе камеры смотрят одинаково.
- **Клик по Fight ≠ старт боя.** `BattleReadinessService` может отклонить запрос (правила
  минимума союзников, пул), и кнопка в этом случае ничего не запускает.
- **Сцена не перезагружается** ни между стадиями уровня, ни при рестарте — камера живёт дольше
  одного боя, и «вернуть как было» обязано быть явным шагом, а не побочным эффектом загрузки.

## Рассмотренные варианты

1. **Анимировать `Camera.main.fieldOfView`.** Самый очевидный «зум». ❌ Отклонено: дочерняя
   `HealthBarCamera` свой FOV не меняет, поэтому наезд рассинхронизировал бы **все** хелсбары
   с юнитами — полоски поехали бы относительно голов на всё время анимации. Чинится только
   прогоном FOV по `GetComponentsInChildren<Camera>()`, то есть знанием о чужой камере внутри
   сервиса.
2. **Долли: сдвиг Transform родительской камеры вдоль `transform.forward`.** ✅ Принято.
   Дочерняя камера — ребёнок трансформа, едет следом по построению, знать о ней никому не нужно.
3. **Триггер — обработчик клика по кнопке Fight.** ❌ Отклонено: readiness-правила могут отклонить
   старт, и камера уехала бы в бой, которого не будет.
4. **Твин на DOTween** (в проекте DOTween есть). ❌ Отклонено: `UNITASK_DOTWEEN_SUPPORT` не
   определён, а `DOTWEEN_ASYNC` определён **только для Android** — await'абельный DOTween-твин
   непортируем на iOS. В проекте уже есть образец ручного UniTask-твина:
   `Assets/Code/Animals/Vfx/MergeScaleTween.cs`.
5. **Хранить параметры в полях компонента на сцене.** ❌ Отклонено в пользу ScriptableObject —
   по конвенции проекта настраиваемые дизайнером величины живут в ассетах `Settings/BattleConfigs/`.

## Принятое решение

### Модуль `Assets/Code/Battle/CameraControl/`

- `IBattleCameraService` — `ZoomInAsync(CancellationToken)` / `ZoomOutAsync(CancellationToken)`.
- `BattleCameraService` — в конструкторе один раз снимает `_basePosition` с камеры и считает
  `_zoomedInPosition = _basePosition + transform.forward * DollyDistance`. Твин — цикл на
  `UniTask.Yield(PlayerLoopTiming.Update)` с `Vector3.LerpUnclamped` и `AnimationCurve`.
- `BattleCameraPresenter` (`IInitializable` + `IDisposable`) — подписки и `_lifetimeCts`,
  вызовы через `.SuppressCancellationThrow().Forget()`.

> [!info] Namespace — `Code.Battle.CameraControl`, а не `Code.Battle.Camera`
> Во втором случае идентификатор `Camera` внутри модуля резолвится в **namespace**, а не в
> `UnityEngine.Camera`, и всё падает на CS0118. Правило общее для любого будущего модуля,
> названного именем типа Unity.

### Сигналы: наезд — по факту старта, отъезд — по двум входам

- `BattleStartedSignal` фаерится в `PreBattleState.StartBattleAsync()` **перед**
  `ChangeStateAsync<PlayerTurnState>()` — это единственная точка, где бой уже прошёл все проверки
  готовности и точно начинается.
- `BattleEndedSignal` фаерится в `BattleEndState.Enter()` **после** гварда отмены death-animation
  и **до** `CleanupLevel()` — то есть только на реальном завершении боя и до того, как флоу
  начнёт разбирать уровень.
- Зум-аут слушает **и** `BattleEndedSignal`, **и** существующий `PreBattlePhaseStartedSignal`.
  Второй нужен потому, что сцена не перезагружается: переход к следующей стадии и рестарт уровня
  возвращают игрока в подготовку, не проходя через `BattleEndState`, и камера осталась бы в наезде.

### Прерывание твина

Каждый твин стартует от **текущей** позиции камеры, а не от номинальной базы/цели, и первым делом
отменяет предыдущий через внутренний CTS (`CreateLinkedTokenSource` с внешним токеном). Поэтому
зум-аут, прилетевший посреди зум-ина, даёт плавный доворот, а не скачок; повторный зум-аут
идемпотентен (base → base, завершается за кадр).

### Конфиг

`Assets/Code/Battle/Config/BattleCameraConfig.cs` + ассет
`Assets/Settings/BattleConfigs/BattleCameraConfig.asset`:
`_dollyDistance` `[Range(0, 4.5)]` = 3.5 · `_zoomInDuration` = 0.6 · `_zoomInCurve` (EaseInOut) ·
`_zoomInDelay` = 0 · `_zoomOutDuration` = 0.45 · `_zoomOutCurve` (EaseInOut).

> [!warning] Верхняя граница 4.5 привязана к позе камеры в сцене
> Диапазон выведен из авторской позы `Main Camera` (pos `(3.06, 16.09, −8.76)`, pitch 45°, FOV 60):
> при d ≈ 4.73 из кадра уходит задний ряд зоны деплоя (z = −2.5). **Если камеру в сцене подвинут,
> `Range` надо пересчитать** — иначе слайдер позволит наехать так, что часть зоны деплоя окажется
> за кадром.

### DI

`BattleInstaller`: `[SerializeField] _battleCameraConfig`, `DeclareSignal<BattleStartedSignal>()`
и `DeclareSignal<BattleEndedSignal>()` (оба `OptionalSubscriber`), метод `BindBattleCamera()`
(`FromInstance` конфига + `BindInterfacesTo<BattleCameraService>()` +
`BindInterfacesAndSelfTo<BattleCameraPresenter>().NonLazy()`), проверка в
`ValidateSceneReferences()`. `Camera` брать было откуда — `CurrentGameInstaller.BindCamera()`
уже держит `Bind<Camera>().FromInstance(Camera.main).AsSingle()`.
`BattleStateMachine` прокидывает `signalBus` в конструктор `BattleEndState`.

## Проверка

- **Компиляция — в обход редактора**: Unity MCP всю сессию отдавал ConnectionRefused, поэтому
  собиралась сгенерированная копия `CodeBase.csproj` через msbuild. Чисто, только два
  преэкзистующих варнинга.
- Все `.meta` и `.asset` YAML написаны **вручную** на диске по той же причине.
- 🔴 **В Unity изменения не импортированы, Play Mode не прогонялся, консоль не отсматривалась.**
  До прогона не подтверждены: резолв GUID конфига, непустая ссылка на `BattleInstaller`,
  визуальный результат наезда и поведение при прерывании.
- Тесты не писались — действует **TESTS PAUSED**.

## Затронутые файлы

**Новые**
- `Assets/Code/Battle/CameraControl/IBattleCameraService.cs`
- `Assets/Code/Battle/CameraControl/BattleCameraService.cs`
- `Assets/Code/Battle/CameraControl/BattleCameraPresenter.cs`
- `Assets/Code/Battle/Config/BattleCameraConfig.cs`
- `Assets/Code/Battle/Signals/BattleStartedSignal.cs`
- `Assets/Code/Battle/Signals/BattleEndedSignal.cs`
- `Assets/Settings/BattleConfigs/BattleCameraConfig.asset`

**Изменённые**
- `Assets/Code/Battle/States/PreBattleState.cs`
- `Assets/Code/Battle/States/BattleEndState.cs`
- `Assets/Code/Battle/StateMachine/BattleStateMachine.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Scenes/Main Scene.unity`

## Связанное

- [[2026-07-31-pre-battle-phase-architecture]] — откуда взялся `PreBattlePhaseStartedSignal`
  и почему факты идут наружу через `SignalBus`
- [[2026-08-01-merge-animation-system]] — соседний ручной UniTask-твин (`MergeScaleTween`)
