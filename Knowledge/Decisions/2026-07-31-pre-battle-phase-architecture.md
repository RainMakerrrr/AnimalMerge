# Решение: пре-батл фаза — команды внутрь через интерфейсы, факты наружу через SignalBus

**Дата:** 2026-07-31
**Статус:** принято, реализовано (обвязка сцены — на пользователе)

## Контекст
Пре-батл фаза выглядела так: `SpawnAnimalsButton` (MonoBehaviour на кнопке) в обработчике клика напрямую звал `animalSpawner.SpawnAnimals()` и разом высыпал на поле всех союзников; `StartBattleButton` так же напрямую дёргал старт боя. UI знал о доменной логике, логика спавна была неотделима от кнопки, тестировать было нечего, а условие «когда можно начинать бой» просто отсутствовало.

Задача: заменить массовый спавн на «одно животное за клик» (кнопка **AddAnimal**) + кнопку **Battle/fight** с пульсацией, а логику спавна поднять в прикладной слой.

## Рассмотренные варианты
1. **Оставить логику в MonoBehaviour-кнопке, добавить счётчик.** Минус: закрепляет исходную проблему — UI остаётся владельцем правил игры, каждое новое условие старта боя = ещё один `if` внутри кнопки.
2. **Гонять всё через `SignalBus` в обе стороны** (клик → сигнал `SpawnRequestedSignal`). Минус: команда без адресата и без результата; отладка «кто это обработал» превращается в поиск подписчиков, а `RequestSpawn()` обязан уметь отказать (нет клетки / пул пуст).
3. **View → Presenter → сервисы прикладного слоя, наружу — сигналы** (выбрано).

## Принятое решение
Слои:

```
View (тупые)   AddAnimalButtonView · BattleButtonView · UiPulseAnimator  → event Clicked, Set*(...)
Presenter      PreBattleHudPresenter (IInitializable/IDisposable)         → единственный клей
Application    IAllySpawnService · IAllySpawnPool · IBattleReadinessService · IBattleReadinessRule[]
Domain/Infra   IAnimalSpawner (AnimalSpawner) · IUnitTracker · IAnimalFactory
```

- **Команды идут внутрь через интерфейсы**: `view.Clicked → PreBattleHudPresenter → IAllySpawnService.RequestSpawn()` / `StartBattleService.RequestStart()`.
- **Факты идут наружу через `SignalBus`**: `PreBattlePhaseStartedSignal`, `PreBattlePhaseEndedSignal`, `AllySpawnedSignal`, `BattleReadinessChangedSignal` — все объявлены `.OptionalSubscriber()`.
- **Новое условие старта боя = один класс `IBattleReadinessRule` + одна строка биндинга.** `BattleReadinessService` агрегирует `IBattleReadinessRule[]` по И. Сейчас правил два: `PoolExhaustedRule` (пул стартовых союзников израсходован) и `MinAllyCountRule` (см. отдельное решение).
- **Данные вынесены в ScriptableObject** `PreBattleConfig` (`Code/Battle/Config/`, меню `Game/Pre Battle Config`), ассет `Assets/Settings/BattleConfigs/PreBattleConfig.asset`: `StartingPool = [Cheetah, Fox, Elephant]` (идентично прежнему `AnimalSpawner._animalTypes` на сцене), `MinAlliesToStart = 2`.
- `IAnimalSpawner` — новый интерфейс над `AnimalSpawner`, чтобы прикладной слой не зависел от MonoBehaviour.
- Старые `Code/Battle/Input/SpawnAnimalsButton.cs` и `StartBattleButton.cs` удалены.

Поведение: клик по AddAnimal берёт один тип из `IAllySpawnPool` → `IAnimalSpawner.Spawn(type)` → регистрирует **все** созданные фасады в `IUnitTracker` (курица порождает 3 дополнительных) → фаерит `AllySpawnedSignal`. Пул исчерпан → AddAnimal гаснет, Battle загорается и пульсирует (DOTween, `UiPulseAnimator`).

## Почему так
- **Команда — не сигнал.** `RequestSpawn()` должен уметь отказать (пул пуст, нет свободной клетки) и делает это синхронно у известного адресата. Сигналы оставлены для фактов, у которых заранее неизвестное число слушателей — их спокойно слушают presenter и правила readiness одновременно.
- **`.OptionalSubscriber()` на всех сигналах** — фаза может стартовать до того, как HUD собран, и сцена без кнопок не должна падать.
- **Presenter — единственная точка стыка UI и логики.** View не знают ни про `SignalBus`, ни про сервисы: у них `event Clicked` и сеттеры визуального состояния, поэтому их можно покрыть PlayMode-тестами без контейнера (`AddAnimalButtonViewTests`, `BattleButtonViewTests`).
- **Presenter слушает `IUnitTracker.PlayerUnitsChanged` дополнительно к сигналам**: мердж освобождает клетку на забитой сетке, не меняя readiness, — на «только сигнальном» HUD кнопка AddAnimal осталась бы серой.
- **Правила отдельными классами, а не флагами в сервисе** — условия старта боя разные по уровням и будут расти; агрегация по И даёт добавление условия без правки существующего кода.
- **Пул в данных, а не в сцене** — стартовый набор можно версионировать и подменять отдельно от `Main Scene.unity`, которая в проекте является точкой конфликтов между параллельными сессиями. Дублирование `PreBattleConfig.StartingPool` и `AnimalSpawner._animalTypes` (пул подкреплений для `SpawnRandom()`) оставлено намеренно — объединение вне скоупа.
- **Сцена не редактировалась осознанно**: Unity Editor общий для параллельных сессий, в дереве висел неподтверждённый чужой дифф `Main Scene.unity`. Вместо этого `BattleInstaller.ValidateSceneReferences()` логирует внятную ошибку на каждый незаполненный слот.

## Проверка
- Unity Console: 0 ошибок компиляции, 0 варнингов.
- EditMode `BattleSystem`: 51/51 зелёные. PlayMode `UI`: 9/12 (3 падения — известные `DamagePopup*`, не регрессия).
- Новые тесты (созданы до появления политики TESTS PAUSED): `AllySpawnPoolTests`, `AllySpawnServiceTests`, `BattleReadinessServiceTests`, `AddAnimalButtonViewTests`, `BattleButtonViewTests`.

> [!warning] Не сделано — обвязка сцены на пользователе
> `Spawn Animals Button` → снять missing script, повесить `AddAnimalButtonView`; `Start Battle button` → `UiPulseAnimator` (Scale 1.08, Duration 0.5) + `BattleButtonView`; `SceneContext → BattleInstaller` → заполнить `Pre Battle Config`, `Add Animal Button`, `Battle Button`. Подробный чек-лист в [[2026-07-31]].

## Затронутые файлы
Новые: `Code/Animals/IAnimalSpawner.cs`; `Code/Battle/Config/PreBattleConfig.cs`; `Code/Battle/PreBattle/{IAllySpawnPool, AllySpawnPool, IAllySpawnService, AllySpawnService, IBattleReadinessRule, IBattleReadinessService, BattleReadinessService}.cs`; `Code/Battle/PreBattle/Rules/{PoolExhaustedRule, MinAllyCountRule}.cs`; `Code/Battle/Signals/{PreBattlePhaseStartedSignal, PreBattlePhaseEndedSignal, AllySpawnedSignal, BattleReadinessChangedSignal}.cs`; `Code/Battle/UI/{AddAnimalButtonView, BattleButtonView, UiPulseAnimator, PreBattleHudPresenter}.cs`; `Settings/BattleConfigs/PreBattleConfig.asset`; тесты `EditorTests/BattleSystem/UnitTests/{AllySpawnPoolTests, AllySpawnServiceTests, BattleReadinessServiceTests}.cs`, `PlayModeTests/UI/{AddAnimalButtonViewTests, BattleButtonViewTests}.cs`.
Изменены: `Code/Animals/AnimalSpawner.cs`, `Code/Battle/States/PreBattleState.cs`, `Code/Battle/StateMachine/BattleStateMachine.cs`, `Code/Infrastructure/Installers/BattleInstaller.cs`, `Code/Tests/EditorTests/BattleSystem/Helpers/BattleTestHelper.cs`, `Code/Tests/EditorTests/Code.Editor.Tests.asmdef`.
Удалены: `Code/Battle/Input/SpawnAnimalsButton.cs`, `Code/Battle/Input/StartBattleButton.cs` (+ `.meta`).
