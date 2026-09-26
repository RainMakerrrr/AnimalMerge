# Решение: боевые VFX (дым при спавне, волна пыли при AoE) — центральный сервис на сигналах с пулом, а не спавнеры на префабах

**Дата:** 2026-09-26
**Статус:** принято, реализовано (код + конфиг + сцена + плейсхолдер-префабы). Не закоммичено на
момент записи. Префабы эффектов — плейсхолдеры, будут заменены дизайнером.

## Контекст

Нужно было два эффекта:

- **клубок дыма**, когда животное появляется на поле — и союзник (включая всю стаю куриц одной
  операцией спавна), и враг;
- **расходящаяся волна пыли** при AoE-атаке (сейчас `_isAoE` только у Elephant).

Что определило решение:

- В проекте уже есть образец эффекта «на префабе» — `DeathSpiritSpawner` висит на самом животном.
  Повторить его означало бы добавить компонент на ~12 префабов (6 союзников + 6 врагов) и держать
  их в паритете.
- Факты «юниты появились» и «AoE-удар приземлился» уже частично существовали как сигналы
  (`AllySpawnedSignal`), но нёс он только один `Unit` — стаю куриц он не описывал, а врагов не
  описывало ничего.
- Префабы будут заменены дизайнером: сервис не может полагаться на то, что дизайнер правильно
  выставит Scaling Mode, `stopAction` и `playOnAwake`.

## Рассмотренные варианты

1. **Спавнер на каждом префабе животного** (как `DeathSpiritSpawner`). ❌ Отклонено: правка ~12
   префабов, риск рассинхронизации паритета, эффект размазан по ассетам животных.
2. **`Instantiate`/`Destroy` на каждый эффект без пула.** ❌ Отклонено: мобильная платформа,
   эффекты массовые (спавн партии, стая из 4 куриц) — лишний GC и аллокации.
3. **Центральный `IVfxSpawner` с пулом по префабу + презентеры, подписанные на `SignalBus`.**
   ✅ Принято.

## Принятое решение

- `Code/Battle/Vfx/`: `IVfxSpawner` / `VfxSpawner` — пул `Dictionary<ParticleSystem, Stack<PooledVfx>>`,
  инстансы под scene-root `VfxPool`. При первом создании инстанса сервис **принудительно** ставит
  `scalingMode = Hierarchy` всем дочерним системам и `stopAction = Callback` руту.
- `PooledVfx` ловит `OnParticleSystemStopped` → `UniTask.WaitWhile(IsAlive(true))` (дожидается
  дочерних систем) → деактивирует и возвращает себя в пул.
- `UnitSpawnVfxPresenter` и `AoeAttackVfxPresenter` — `IInitializable`/`IDisposable`, подписка
  через `SignalBus`.
- Сигналы: `AllySpawnedSignal` получил `Units` (все юниты операции; `Unit` оставлен для
  совместимости), новый `EnemiesSpawnedSignal` из `PreBattleState.Enter` после регистрации врагов,
  новый `AoeAttackLandedSignal {Center, Radius, Forward}` — `TryFire` из AoE-ветки
  `AnimalAttack.AttackAnimationHandlerAsync` **до** overlap-запроса (волна играет и при промахе).
  `SignalBus` приходит в `AnimalAttack` отдельным `[Inject] ConstructSignalBus([InjectOptional] SignalBus)`
  с null-guard — тесты создают `AnimalAttack` без контейнера.
- Параметры — `BattleVfxConfig` (SO, `Assets/Settings/BattleConfigs/BattleVfxConfig.asset`,
  поле `BattleInstaller._battleVfxConfig`): префаб клубка, scale на клетку
  (`max(UnitSize.W, H) × value`), высота, тумблер для врагов; префаб волны, `AuthoredRadius` (1),
  множитель радиуса, высота. `null`-конфиг → `LogError`, эффекты выключены, бой не ломается.
- Радиус волны = `AnimalAttack._radius` + половина длины капсулы (слон ≈ 2.375).

> [!warning] Контракт для дизайнера при замене префабов
> - подменять префабы только в `BattleVfxConfig.asset`;
> - волну авторить под мировой радиус **1** (= `AuthoredRadius`), сервис масштабирует её сам;
> - Scaling Mode будет переписан на `Hierarchy`, `stopAction` рута — на `Callback`;
> - **рут не должен лупиться** — зацикленный префаб никогда не остановится и не вернётся в пул;
> - `playOnAwake` игнорируется — запуск делает сервис.

## Почему так

- Один модуль вместо правки ~12 префабов; новый эффект на новое событие = сигнал + презентер +
  поле конфига.
- Сигналы и так являются «фактами наружу» в архитектуре пре-батл фазы (см.
  [[2026-07-31-pre-battle-phase-architecture|решение о пре-батл фазе]]), VFX — чистый подписчик и не
  влияет на логику боя.
- Принудительная нормализация настроек частиц убирает класс тихих багов при замене плейсхолдеров
  (эффект не масштабируется, не возвращается в пул).
- Волна стартует до overlap-запроса, чтобы визуал не зависел от того, попал ли удар.

## Проверка

- Что проверять в Play Mode: клубок появляется на каждом союзнике (включая всех кур стаи) и на врагах при входе в
  пре-батл; волна слона расходится на радиус атаки, в том числе при промахе.
- Эффекты возвращаются в пул (`VfxPool` в иерархии, повторное использование инстансов).
- Замечено, но не чинилось (пред-существующее): ранние `return` в
  `AnimalAttack.AttackAnimationHandlerAsync` (`count <= 0`, `_attackPoint == null`) не выставляют
  `_isAttackDone` → `Attack()` может зависнуть; `AnimalAttackPostAbilityTests` — 5 падений из-за
  сигнатур `async Task`.
- Уточнено по ходу: радиус атаки — сериализованное поле префаба `AnimalAttack._radius`, а не стат
  (`ApplyStats` его не трогает); запись в `Index.md` исправлена.

## Затронутые файлы

- `Assets/Code/Battle/Vfx/IVfxSpawner.cs`, `VfxSpawner.cs`, `PooledVfx.cs`,
  `UnitSpawnVfxPresenter.cs`, `AoeAttackVfxPresenter.cs` (новые)
- `Assets/Code/Battle/Config/BattleVfxConfig.cs` (новый)
- `Assets/Code/Battle/Signals/EnemiesSpawnedSignal.cs`, `AoeAttackLandedSignal.cs` (новые)
- `Assets/Code/Battle/Signals/AllySpawnedSignal.cs`
- `Assets/Code/Battle/PreBattle/AllySpawnService.cs`
- `Assets/Code/Battle/States/PreBattleState.cs`
- `Assets/Code/Animals/AnimalAttack.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Scenes/Main Scene.unity`
- `Assets/Prefabs/Vfx/SpawnSmokePuff.prefab`, `AoeDustWave.prefab` (новые)
- `Assets/Settings/BattleConfigs/BattleVfxConfig.asset` (новый)
