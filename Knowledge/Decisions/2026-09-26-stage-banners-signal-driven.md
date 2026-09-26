# Решение: баннеры стадий «MERGE» / «FIGHT!» — неблокирующий презентер на сигналах, шрифт riffic через отдельные пресеты материалов

**Дата:** 2026-09-26
**Статус:** ⛔️ заменено [2026-09-26] решением [[2026-09-26-stage-banners-blocking-state|блокирующий шаг состояний]] — отклонённый здесь вариант 1 «шаг `BattleStateMachine`» теперь выбран. Шрифтовая часть (пресеты материалов, градиенты, фейды через `CanvasGroup`) остаётся в силе.

> [!warning] Заменено
> Презентер на сигналах удалён; баннеры теперь ждут состояния через `IStageAnnouncer` под
> `IPlayerInputLock`. См. [[2026-09-26-stage-banners-blocking-state]].

## Контекст

Нужны всплывающие объявления стадий: «MERGE stage» в начале пре-батл фазы и «FIGHT!» при старте
боя, с анимацией «Band + Slam» (полоса раскрывается, заголовок падает с увеличения со
squash/stretch, подзаголовок выскакивает с покачиванием, всё уезжает вбок).

Что определило решение:

- Факты «началась пре-батл фаза», «начался бой», «бой закончился» уже существуют как сигналы
  (`PreBattlePhaseStartedSignal`, `BattleStartedSignal`, `BattleEndedSignal`).
- Баннер — чистая косметика; задерживать первый ход ради него никто не просил.
- Шрифт — riffic-bold SDF, уже используется в HUD; атлас общий, ломать HUD нельзя.
- Заголовки красятся вертикальным `TMP_ColorGradient`, а вертексный градиент игнорирует альфу
  TMP — фейд через `TMP_Text.alpha` не работает.
- Показы могут перекрываться (MERGE ещё играет, а игрок уже нажал «в бой»).

## Рассмотренные варианты

1. **Шаг `BattleStateMachine`** (состояние/await в `PreBattleState` перед первым ходом).
   ❌ Отклонено: блокирует геймплей ради косметики, связывает UI с логикой боя. Оставлен как хук на
   будущее — см. ниже.
2. **Новый атлас / отдельный шрифт под баннеры.** ❌ Отклонено: лишний ассет и память; проверено,
   что на масштабе 2.5× у общего атласа riffic артефактов нет.
3. **Неблокирующий `StageBannerPresenter` на `SignalBus` + `IStageBannerView` + SO-конфиг,
   riffic через отдельные пресеты материалов.** ✅ Принято.

## Принятое решение

- `StageBannerPresenter` (`IInitializable`/`IDisposable`, `NonLazy`):
  `PreBattlePhaseStartedSignal` → MERGE, `BattleStartedSignal` → FIGHT!,
  `BattleEndedSignal` → `HideImmediate`. Показ fire-and-forget, на каждый — свой linked CTS;
  новый показ отменяет предыдущий.
- `StageBannerView : IStageBannerView` — префаб через `FromComponentInNewPrefab`, переподвешивается
  под инжектированный `Canvas`, `SetAsLastSibling` при показе. DOTween `Sequence` собирается через
  `Insert` по абсолютным временам из конфига; ожидание — `UniTask.WaitUntil(!IsActive || IsComplete)`
  (интеграция `ToUniTask` для DOTween в проекте недоступна).
- Отменённый показ сбрасывает View в скрытое состояние **только если `_sequence == sequence`** —
  иначе отменённый MERGE спрятал бы уже стартовавший FIGHT!.
- `StageBannerConfig` (SO, `Assets/Settings/BattleConfigs/StageBannerConfig.asset`) — все тайминги,
  изи, масштабы + `StageBannerStyle[]` по `StageBannerKind` (текст, TMP-материал, градиент,
  смещения, наклон подзаголовка). `OnValidate` предупреждает о дублях `Kind`. Биндинг —
  `BattleInstaller.BindStageBanner()`; `null` конфиг/префаб → `LogError`, баннеры выключены.
- Шрифт: пресеты `riffic-bold SDF - Stage Fight/Merge/Subtitle.mat` (Mobile Distance Field,
  outline + underlay, белый face) + вертикальные градиенты в `Code/Framework/Fonts/Gradients/`.
- Фейды — через `CanvasGroup`, а не альфу TMP.
- Неблокирующий: `CanvasGroup.blocksRaycasts = false`, `raycastTarget = false` везде.

> [!tip] Если баннер понадобится сделать блокирующим
> Хук — `PreBattleState.StartBattleAsync` между `BattleStartedSignal` и
> `ChangeStateAsync<PlayerTurnState>`: там можно дождаться показа FIGHT!.

## Почему так

- Сигналы — принятые в проекте «факты наружу» (см.
  [[2026-07-31-pre-battle-phase-architecture|решение о пре-батл фазе]] и
  [[2026-09-26-battle-vfx-signal-driven-pool|боевые VFX]]); баннер — чистый подписчик и не влияет на
  логику боя и тайминги ходов.
- Отдельные пресеты материалов дают свой outline/underlay без копии атласа и без риска для HUD.
- `CanvasGroup` — единственный рабочий фейд при вертексном градиенте.
- Проверка идентичности `Sequence` при отмене закрывает гонку перекрывающихся показов без
  дополнительного состояния.

## Проверка

- Play Mode: MERGE при входе в пре-батл, FIGHT! при старте боя, мгновенное скрытие при конце боя;
  быстрый переход в бой во время MERGE — FIGHT! не пропадает; клики сквозь баннер проходят.
- Открытые предложения ревью (не чинились):
  - `null`-материал в стиле оставляет предыдущий материал;
  - `_exitDirection` — `int`, где 0 означает +1 (лучше bool/enum);
  - `TitleOffset` фактически двигает весь контент (переименовать в `ContentOffset`);
  - баннер перекрывает руку туториала на старте уровня (~2.35 с);
  - лишний `SuppressCancellationThrow` в презентере.

## Затронутые файлы

- `Assets/Code/Battle/UI/StageBanner/IStageBannerView.cs`, `StageBannerView.cs`,
  `StageBannerPresenter.cs` (новые)
- `Assets/Code/Battle/Config/StageBannerConfig.cs`, `StageBannerKind.cs`, `StageBannerStyle.cs` (новые)
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Scenes/Main Scene.unity`
- `Assets/Prefabs/StageBannerView.prefab` (новый)
- `Assets/Settings/BattleConfigs/StageBannerConfig.asset` (новый)
- `Assets/Code/Framework/Fonts/riffic-bold SDF - Stage Fight/Merge/Subtitle.mat` (новые)
- `Assets/Code/Framework/Fonts/Gradients/Stage Fight/Merge/Subtitle Gradient.asset` (новые)
