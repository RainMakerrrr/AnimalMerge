# Решение: надписи изменения статов при мердже — per-animal очередь `MergeCalloutQueue` в `MergePopupController` + событие `MergeTarget.Merged(MergeOutcome)` на любой мердж

**Дата:** 2026-09-26
**Статус:** ⛔️ заменено [2026-09-26] решением [[2026-09-26-merge-stat-changes-in-stats-panel|анимация статов внутри карточки]] — world-space надписи, `MergeCalloutQueue`, `StatChangePopupView`, `MergeCalloutConfig` удалены. Событие `MergeTarget.Merged(MergeOutcome)` на любой мердж и `MergeStatValues` остаются в силе.

> [!warning] Заменено
> Изменения статов теперь проигрываются внутри `AnimalStatsPanelView`: презентер карточки ловит
> `AllyMergedSignal` (`StatsBefore`/`StatsAfter`) и через кадр открывает карточку цели.
> См. [[2026-09-26-merge-stat-changes-in-stats-panel]].

Частично заменяет [[2026-06-27-merge-popup]]: событие `Merged` больше **не** гейтится выдачей
скилла — оно поднимается на любой успешный мердж; текст скилла по-прежнему показывается только
при cross-type мердже.

## Контекст

После мерджа игрок не видел, что именно изменилось у выжившего животного: был только текст скилла
(cross-type) и статичный лейбл «+N% HP» в карточке. Макет требует на каждый изменившийся стат:
«+N%» (красное «-N%» при снижении) → фейд → иконка стата со старым значением, которое досчитывается
до нового → punch-scale → фейд.

Ограничения:

- `MergeTarget.Merged` раньше был `Action<PlayerAnimalFacade>` и фаерился **только** когда выдан
  скилл — same-type мерджи (основной источник +50%) событий не давали.
- Цепочки `MergeSkill` (Fox, несущий Chicken, и т. п.) меняют статы несколько раз за один мердж —
  разница должна считаться по всему мерджу, а не по отдельному скиллу.
- Несколько мерджей подряд в одно животное не должны накладывать надписи друг на друга.
- `MergePopupController` висит на корнях ally-префабов, и `AllyPrefabAssembler` ищет его класс и
  4 сериализованных поля по имени — переименовывать нельзя.
- Undo мерджа (`MergeCommand.Undo`) должно гасить недоигранные надписи.

## Рассмотренные варианты

1. **Глобальный сервис надписей на `SignalBus`** (по образцу `IVfxSpawner`). ❌ Отклонено: очередь
   всё равно нужна per-animal, а контроллер уже стоит на каждом префабе и знает свой `MergeTarget`;
   сервис дублировал бы маршрутизацию «какое животное».
2. **Считать разницу статов в UI из `AnimalDatabase` (базовые статы vs текущие).** ❌ Отклонено: не
   даёт «было → стало» для повторных мерджей и не видит чужих модификаторов.
3. **Параллельный показ всех надписей со смещением по Y.** ❌ Отклонено: макет — последовательная
   анимация, на мобильном экране стек надписей читается плохо.
4. **Снимок `MergeStatValues` до/после всего мерджа в `MergeTarget`, событие
   `Merged(MergeOutcome)` на любой мердж, per-animal FIFO-очередь в существующем
   `MergePopupController`.** ✅ Принято.

## Принятое решение

- **Домен/событие** (`Code/Animals/Merge/`):
  - `MergeStatValues` (readonly struct: `MaxHealth`, `Damage`, `TilesPerMove`, `From(AnimalFacade)`).
  - `MergeOutcome` (readonly struct: `Source`, `Target`, `GrantedSkill`, `Before`, `After`) —
    снимок вокруг всего `ExecuteMergeDirectly`, включая цепочки `MergeSkill`.
  - `MergeTarget.Merged : Action<MergeOutcome>` — для **обоих** типов мерджа. Новые
    `MergeTarget.Facade`, событие `MergeUndone`, `NotifyMergeUndone()` — вызывается из
    `MergeCommand.Undo` после удаления клона курицы.
- **Очередь** `MergeCalloutQueue` (plain C#, `IDisposable`, UniTask): FIFO фабрик
  `Func<CancellationToken, UniTask>`, стартовая задержка + пауза между элементами, linked CTS на
  активный элемент от lifetime-токена, `CancelAll()` — очищает очередь и отменяет текущий показ.
- **Контроллер** `MergePopupController` (класс и 4 поля сохранены): владеет очередью; инжектит
  `IAnimalSelectionService` и `[InjectOptional] MergeCalloutConfig`. На `Merged`: текст скилла
  (только `GrantedSkill`) → надписи статов в порядке Max HP → damage → TilesPerMove
  (`MergeStatChangeBuilder` → `MergeStatChange`). Надписи статов **пропускаются**, если в момент
  мерджа открыта карточка цели (`Selected == target`). Открытие карточки посреди очереди её
  **не** отменяет (решение пользователя). `CancelAll` — на `MergeUndone`, `OnDisable`, destroy.
- **View**: `MergePopupView.Play` → `PlayAsync(string, Color, CancellationToken)` по образцу
  `StageBannerView` (`UniTaskCompletionSource` в `OnKill`, `AttachExternalCancellation` +
  `SuppressCancellationThrow`). Новый `StatChangePopupView` — следует за якорем в `LateUpdate`,
  `raycastTarget` выключен; префаб `Prefabs/StatChangePopupView.prefab` (Impact SDF, пресет
  `Popup Merge`, наклон 8°).
- **Конфиг** `MergeCalloutConfig` (SO, `Settings/BattleConfigs/MergeCalloutConfig.asset`): цвета
  gain/skill `#6EF214`, loss `#FF4D4D`, `ShowDecreases = true`, offset (0,2,0), gap 0.1, фаза
  процента 0.12/0.35/0.15 + подъём 40 px, фаза значения 0.12 / count 0.45 / punch 0.25 за 0.25 /
  hold 0.2 / fade 0.25 (≈ 1.89 с на стат), формат `{0:+0;-0}%`, стили `MergeStatStyle` по
  `MergeStatKind`. Биндинг — `BattleInstaller.BindMergeCallouts`, назначен в `Main Scene`.
- **Карточка**: старый лейбл «+N% HP» удалён (`AnimalStatsPanelView`, `AnimalCardStats` теперь
  3 аргумента, оба презентера, префаб) — информацию о бонусе теперь несёт надпись.

## Почему так

- Снимок вокруг всего мерджа — единственный способ корректно показать цепочки скиллов и повторные
  мерджи: «было → стало» берётся с живого фасада, без знания формул.
- Очередь на животное, а не глобальная: надписи разных животных не ждут друг друга, а одного —
  не наслаиваются.
- `UniTask<void>` с отменой вместо fire-and-forget твинов: undo и уничтожение префаба должны
  прерывать показ детерминированно.
- Пропуск при открытой карточке — не дублировать информацию, которую игрок уже видит; отмена при
  открытии посреди очереди отклонена пользователем.
- Реальные проценты мерджа: +50% (same-type, HP слона, урон оленя), +100% (ходы гепарда), -25%
  (HP и урон курицы). «+75%» из макета в игре не возникает.

## Проверка

- `MergeUndoServiceTests` + `AbilityUndoTests` — 19/19. Новых тестов нет (тесты на паузе).
- Не проверено вживую: настоящий drag-merge, надпись гепарда, Fox с Chicken, следование надписи
  во время drag. Drag-merge сбрасывает выбор (`AnimalMover.BeginDrag` / `OnRelease`), так что
  «карточка открыта в момент мерджа» — редкий случай.
- Неприменённые предложения ревью: пул попапов; префаб `StatChangePopupView` с null-ссылками
  бросит после `Instantiate` и оставит зависший попап / застопорит очередь до следующего
  `Enqueue`; пропускать надпись, если округлённые From == To; `MergeOutcome.Target` не
  используется.

## Затронутые файлы

- `Assets/Code/Animals/Merge/{MergeOutcome,MergeStatValues}.cs` (новые), `MergeTarget.cs`,
  `Commands/MergeCommand.cs`
- `Assets/Code/Animals/UI/MergeCallouts/{MergeStatKind,MergeStatChange,MergeStatChangeBuilder,MergeStatStyle,MergeCalloutConfig,MergeCalloutQueue,StatChangePopupView}.cs` (новые)
- `Assets/Code/Animals/UI/{MergePopupController,MergePopupView,AnimalCardStats,AnimalStatsPanelPresenter,AnimalStatsPanelView}.cs`
- `Assets/Code/Battle/UI/EnemyCardPresenter.cs`, `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Prefabs/{StatChangePopupView,AnimalStatsPanelView}.prefab`,
  `Assets/Settings/BattleConfigs/MergeCalloutConfig.asset`, `Assets/Scenes/Main Scene.unity`

## Связанное

- [[2026-06-27-merge-popup]] — исходное решение по попапу мерджа
- [[2026-09-26-stage-banners-blocking-state]] — образец `PlayAsync` с `OnKill`
- [[2026-09-26]] — лог сессии
