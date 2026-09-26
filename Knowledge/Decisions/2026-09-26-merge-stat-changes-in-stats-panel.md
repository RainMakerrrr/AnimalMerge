# Решение: анимация изменения статов при мердже — внутри карточки статов (`AnimalStatsPanelView`), а не world-space надписями

**Дата:** 2026-09-26
**Статус:** принято, реализовано (код + префаб + материал). Не закоммичено на момент записи.
Заменяет [[2026-09-26-merge-stat-callouts-queue]] (world-space надписи и очередь `MergeCalloutQueue`
удалены). Событие `MergeTarget.Merged(MergeOutcome)` на любой мердж и `MergeStatValues` из того
решения сохранены.

## Контекст

Первая итерация показывала изменения статов world-space надписями над животным
(`StatChangePopupView` + per-animal `MergeCalloutQueue` в `MergePopupController`). Переделка по
запросу: изменения должны проигрываться **внутри инфо-окошка статов** — там, где игрок и так читает
HP / урон / ходы.

Ограничения:

- Мердж синхронный внутри `AnimalMover.OnRelease`, и сразу после него `OnRelease` сбрасывает
  выделение — открыть карточку в том же кадре нельзя, она тут же закроется.
- Карточку открывает только `IAnimalSelectionService`, и он отказывает вне пре-батл фазы.
- `EnemyCardView` наследует `AnimalStatsPanelView` — новые поля не должны ломать вражеский префаб.
- Во время анимации живые обновления (`HealthChanged`, `IMergeUndoService.OnStackCountChanged`)
  перетёрли бы досчитывающееся значение.
- Undo, выбор другого животного, FIGHT и новый мердж должны прерывать анимацию и оставлять
  карточку с живыми значениями.

## Рассмотренные варианты

1. **Оставить world-space надписи** ([[2026-09-26-merge-stat-callouts-queue]]). ❌ Отклонено
   пользователем: информация дублирует карточку и читается хуже.
2. **Отдельная мини-карточка/тост для результата мерджа.** ❌ Отклонено: второй UI с теми же
   тремя статами, отдельная раскладка и жизненный цикл.
3. **Открывать обычную карточку через `IAnimalSelectionService.Select(target)` на кадр позже и
   проигрывать анимацию во View; триггер — `AllyMergedSignal` со снимком `StatsBefore`/`StatsAfter`.**
   ✅ Принято.

## Принятое решение

- **Сигналы** (`Code/Battle/Signals/`): `AllyMergedSignal` получил `StatsBefore`/`StatsAfter`
  (`MergeStatValues`, теперь с `CurrentHealth`). Новый `AllyMergeUndoneSignal` — фаерится из
  `MergeTarget.NotifyMergeUndone` через `_signalBus?.Fire`, объявлен в `BattleInstaller`.
- **Презентер** `AnimalStatsPanelPresenter`: на `AllyMergedSignal` ждёт `UniTask.NextFrame`, затем
  `Select(target)` — карточка открывается как при обычном тапе (подсветка дальности хода тоже,
  подтверждено пользователем). Отказ `Select` (вне пре-батла) → анимация отменяется. Состояния
  pending / playing, владелец проверяется по экземпляру CTS. Отмена: выбор другого животного (в т.ч.
  в pending-кадре — выбор игрока важнее), `Clear` во время проигрывания, FIGHT
  (`PreBattlePhaseEnded` сбрасывает выбор), новый мердж, undo (`AllyMergeUndoneSignal`). После
  отмены карточка показывает живые значения. На время анимации подавлены `HealthChanged` и
  `OnStackCountChanged`.
- **View** `AnimalStatsPanelView.PlayStatChangesAsync(changes, token)` (+ `IAnimalStatsPanelView`):
  по каждому изменившемуся стату в порядке HP → урон → ходы — лейбл «+N%» (Calibri Bold, материал
  «Bonus Outline», зелёный; потеря — новый `Calibri Bold SDF - Loss Outline.mat`, красное «-N%»)
  справа от значения → подъём / удержание / фейд → досчёт старое → новое (вниз при снижении) →
  `DOPunchScale` строки. HP анимирует `CurrentHealth`, процент — от `MaxHealth`. Отдельная
  `_statSequence`; `StopStatChanges` сбрасывает лейблы и масштабы. Тайминги сериализованы на View:
  стартовая задержка 0.2, процент 0.12 / 0.35 / 0.15 и подъём 12 px, досчёт 0.45 `OutQuad`,
  punch 0.25 / 0.25 / 6 / 0.5, пауза 0.1, отступ лейбла 8. Поля null-safe — `EnemyCardView` не
  затронут.
- **Префаб** `AnimalStatsPanelView.prefab`: неактивный дочерний `ChangeLabel` в
  `AttackRow` / `HealthRow` / `MovesRow` (`LayoutElement.ignoreLayout`), позиционируется во время
  проигрывания после более широкого из текстов старого/нового значения.
- **Модели** `MergeStatKind` / `MergeStatChange` / `MergeStatChangeBuilder.Build(before, after)`
  переехали в `Code/Animals/UI/MergeStats/`.
- **Удалено**: `Code/Animals/UI/MergeCallouts/` (`StatChangePopupView`, `MergeCalloutConfig`,
  `MergeStatStyle`, `MergeCalloutQueue`), `Prefabs/StatChangePopupView.prefab`,
  `Settings/BattleConfigs/MergeCalloutConfig.asset`, биндинг в `BattleInstaller`,
  `MergeTarget.Facade`. `MergePopupController` снова только попап скилла (отменяется на undo),
  4 сериализованных поля сохранены по имени (их ищет `AllyPrefabAssembler`).

## Почему так

- Карточка уже показывает эти три стата — анимация прямо в строках не дублирует UI и не требует
  следить за позицией животного на экране.
- Переиспользование `Select` даёт ровно то же поведение, что и тап (гейт пре-батла, подсветка
  хода, закрытие), без второго пути открытия карточки.
- Кадр задержки — единственный способ пережить синхронный сброс выделения в
  `AnimalMover.OnRelease`, не трогая `AnimalMover`.
- Сигналы на `SignalBus` вместо подписки на каждый `MergeTarget`: презентер карточки один на сцену,
  а мерджи происходят на любом животном.
- Отмена с показом живых значений — карточка никогда не застревает на промежуточном числе.

## Проверка

- `dotnet build CodeBase.csproj` — 0 ошибок / 0 предупреждений.
- ⚠️ **Не выполнено**: Unity Console, EditMode-тесты (`AbilityUndoTests`, `MergeUndoServiceTests`),
  Play Mode — редактор был заблокирован модальным диалогом перезагрузки сцены (восстановление
  открытой `Main Scene.unity` через git).
- Ревью: CRITICAL нет. Неприменённые предложения: `MergeOutcome.Target`/`Before`/`After` и
  `AllyMergeUndoneSignal.Target` не используются; твин досчёта перестраивает текст каждый кадр.

> [!warning] Грабли
> - Мердж синхронный внутри `AnimalMover.OnRelease` — любой авто-выбор после мерджа надо
>   откладывать на кадр.
> - Не восстанавливать через git сцену, открытую в Unity: редактор встаёт на модальном диалоге
>   перезагрузки, MCP-пинги остаются без ответа. Править через редактор.

## Затронутые файлы

- `Assets/Code/Animals/Merge/{MergeStatValues,MergeOutcome,MergeTarget}.cs`,
  `Assets/Code/Animals/Merge/Commands/MergeCommand.cs`
- `Assets/Code/Battle/Signals/{AllyMergedSignal,AllyMergeUndoneSignal}.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Code/Animals/UI/{IAnimalStatsPanelView,AnimalStatsPanelView,AnimalStatsPanelPresenter,AnimalCardStats,MergePopupController,MergePopupView}.cs`
- `Assets/Code/Animals/UI/MergeStats/{MergeStatKind,MergeStatChange,MergeStatChangeBuilder}.cs`
- `Assets/Code/Battle/UI/EnemyCardPresenter.cs`
- `Assets/Prefabs/AnimalStatsPanelView.prefab`
- `Assets/Code/Framework/Fonts/Calibri Bold SDF - Loss Outline.mat` (новый)
- Удалены: `Assets/Code/Animals/UI/MergeCallouts/`, `Assets/Prefabs/StatChangePopupView.prefab`,
  `Assets/Settings/BattleConfigs/MergeCalloutConfig.asset`

## Связанное

- [[2026-09-26-merge-stat-callouts-queue]] — заменённое решение
- [[2026-08-01-animal-selection-and-stats-panel]] — сервис выбора и карточка
- [[2026-09-26-animal-stats-popup-mockup]] — макет карточки
- [[2026-06-27-merge-popup]] — попап скилла
- [[2026-09-26]] — лог сессии
