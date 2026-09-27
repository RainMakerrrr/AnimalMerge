# Решение: эффекты босс-файта с T-Rex — конфиг по `AnimalType` в battle-слое, тряска камеры поворотом, когти через пул

**Дата:** 2026-09-26
**Статус:** принято, реализовано (код + конфиг + префабы + сцена). Не закоммичено на момент записи.
Текстура когтей — плейсхолдер.

## Контекст

Бой с T-Rex должен ощущаться как босс-файт:

- **виньетка** по краям экрана при старте боя, если на поле есть T-Rex;
- **следы когтей** на T-Rex юните в момент, когда его атакуют;
- **тряска камеры** на каждый шаг T-Rex.

Что определило решение:

- Флаг `IsBoss` в данных животных выключен везде — опираться на него нельзя.
- Не хотелось `if (type == TRex)` в коде: следующий «тяжёлый» юнит должен получать эффекты правкой
  ассета.
- Факта «юнит получил урон» наружу не было (только локальное событие `TakenDamage` на
  `AnimalHealth`), факта «юнит сделал шаг» — тоже.
- `BattleCameraService` (долли-зум) каждый кадр пишет **позицию** камеры — тряска позицией с ним
  конфликтует.
- В сцене уже есть `DamagePopupController`, который завязан на существующий Canvas — отдельный
  root Canvas для виньетки ломал бы порядок отрисовки.

## Рассмотренные варианты

1. **Поля эффектов в `AnimalDatabase`.** ❌ Отклонено: данные животного (Domain) смешались бы с
   презентационными настройками боя.
2. **Триггер по `IsBoss`.** ❌ Отклонено: флаг выключен везде, включение затронуло бы другую логику.
3. **Жёсткая проверка на `AnimalType.TRex` в презентерах.** ❌ Отклонено: не расширяется.
4. **Battle-слойный SO `UnitBattleFeedbackConfig` с записями по `AnimalType`.** ✅ Принято.

Для тряски: DOTween `DOShakePosition` ❌ (дерётся с долли) → `DOShakeRotation` ✅.
Для когтей: эффект-ребёнок цели ❌ (жизненный цикл цели, смерть) → world-space ParticleSystem
через существующий `IVfxSpawner` ✅.

## Принятое решение

- **Конфиг** `UnitBattleFeedbackConfig` (`Assets/Settings/BattleConfigs/UnitBattleFeedbackConfig.asset`):
  записи `UnitBattleFeedbackEntry` по `AnimalType` — `ShowBattleVignette`, `HitMark`
  (`UnitHitMarkSettings`: prefab, scalePerCell, heightOffset, towardCameraOffset),
  `ShakeCameraOnStep` + `CameraShakeSettings`; плюс тайминги виньетки. Сейчас одна запись — TRex.
- **Сигнал** `UnitDamagedSignal { Attacker, Target, TargetUnit, Damage }` — `TryFire` из
  `AnimalHealth.TakeDamageAsync` сразу после `TakenDamage`: только реальный урон (не Dodge), до
  `Die` — то есть и на смертельном ударе. `SignalBus` — `[InjectOptional]`. Фасад владельца
  передаётся один раз через `AnimalHealth.BindOwner(AnimalFacade)` в `AnimalFacade.Start`, чтобы
  не делать `GetComponentInParent` на каждый удар. `AnimalAttack` получил геттер `AnimalType`.
- **Событие шага** `AnimalMovement.StepTaken` — из `OnWaypointChange` в `ExecuteMovement` для
  `i > 0` (DOTween вызывает колбэк на всех точках, включая последнюю; `0` — стартовая клетка).
  Dodge/Retreat шаги не шлют.
- **Камера** `ICameraShakeService` / `CameraShakeService` (`Code.Battle.CameraControl`) —
  `DOShakeRotation`, базовый поворот захватывается в конструкторе и восстанавливается после тряски.
- **Презентеры**:
  - `UnitHitMarkVfxPresenter` — на `UnitDamagedSignal` спавнит `ClawScratch` через `IVfxSpawner` в
    центре bounds коллайдеров цели (со сдвигом к камере); к цели не прикрепляется;
  - `UnitStepShakePresenter` — между `UnitTurnStarted`/`Completed` подписан на `StepTaken`
    ходящего юнита;
  - `BattleVignettePresenter` + `BattleVignetteView` — ребёнок инжектированного Canvas,
    `SetAsFirstSibling`, фейд через `CanvasGroup`; по умолчанию аддитивный материал
    (`UIAdditive.shader` / `UIAdditive.mat`).
- `UnitVfxScale` — общий хелпер масштаба эффекта по футпринту, `UnitSpawnVfxPresenter` переведён на него.
- `BattleInstaller`: `BindUnitBattleFeedback()`, `DeclareSignal<UnitDamagedSignal>().OptionalSubscriber()`,
  `CameraShakeService` в `BindBattleCamera`.

## Почему так

- Эффекты включаются данными: новый «босс» = запись в ассете, без кода.
- Конфиг лежит в battle-слое рядом с `BattleVfxConfig`, Domain (`AnimalDatabase`) не знает про VFX.
- Сигнал урона — «факт наружу» по той же схеме, что `AoeAttackLandedSignal`; презентеры — чистые
  подписчики, логику боя не трогают; `OptionalSubscriber` не требует подписчиков.
- Поворотная тряска не конфликтует с долли `BattleCameraService`, который владеет позицией.
- Пул `IVfxSpawner` уже нормализует настройки частиц и переживает смерть цели.
- Виньетка внутри существующего Canvas не ломает порядок отрисовки попапов урона.

## Проверка

- Play Mode на уровне с T-Rex: виньетка появляется при FIGHT, когти — на каждом реальном ударе
  по T-Rex (включая смертельный, не на Dodge), камера трясётся на каждой клетке шага T-Rex, но не
  на старте пути и не на Dodge/Retreat.
- Известное (по анализу не связано с изменениями, на чистой ветке не проверено): 12 EditMode
  тестов падают с «Method has non-void return value» (`async Task` `[Test]` в
  `AnimalAttackPostAbilityTests`, `RetreatAbilityTests`, `AnimalMovementRetreatTests`) и 2 PlayMode
  `DamagePopupControllerIntegrationTests` — NRE.

## Затронутые файлы

- `Assets/Code/Animals/AnimalAttack.cs`, `Health/AnimalHealth.cs`, `Facades/AnimalFacade.cs`,
  `Movement/AnimalMovement.cs`
- `Assets/Code/Battle/CameraControl/ICameraShakeService.cs`, `CameraShakeService.cs` (новые)
- `Assets/Code/Battle/Config/UnitBattleFeedbackConfig.cs`, `UnitBattleFeedbackEntry.cs`,
  `UnitHitMarkSettings.cs`, `CameraShakeSettings.cs` (новые)
- `Assets/Code/Battle/Signals/UnitDamagedSignal.cs` (новый)
- `Assets/Code/Battle/UI/Vignette/BattleVignettePresenter.cs`, `BattleVignetteView.cs`,
  `IBattleVignetteView.cs` (новые)
- `Assets/Code/Battle/Vfx/UnitHitMarkVfxPresenter.cs`, `UnitStepShakePresenter.cs`,
  `UnitVfxScale.cs` (новые), `UnitSpawnVfxPresenter.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`, `Assets/Scenes/Main Scene.unity`
- `Assets/Prefabs/BattleVignetteView.prefab`, `Assets/Prefabs/Vfx/ClawScratch.prefab`,
  `Prefabs/Vfx/Materials/ClawScratch.mat`, `Prefabs/Vfx/Textures/ClawScratch.png` (новые)
- `Assets/Settings/BattleConfigs/UnitBattleFeedbackConfig.asset` (новый)
- `Assets/Shaders/UIAdditive.shader`, `Assets/UI_Sprites/UIAdditive.mat` (новые)
- `Assets/UI_Sprites/Buttons etc/vingette.png.meta` (Sprite, без мипов, clamp, ASTC 4x4)
