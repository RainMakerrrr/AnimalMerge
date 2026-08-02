# Решение: очередь ходов — снапшот исполнения, а не живой пересчёт; колонка аватарок как отдельный префаб поверх Canvas

**Дата:** 2026-08-02 (доработка — 2026-08-02, тот же день)
**Статус:** принято, реализовано (код + шейдер + два префаба + сцена + 9 запечённых портретов); доработано — плавный транзишн, `UnitTurnCompletedSignal`, `TurnOrderUnit`

## Контекст

Задача из макета: вертикальная колонка аватарок в левом верхнем углу под кнопкой вибрации, порядок сверху вниз = порядок ходов, ходящий — цветной, увеличенный и в рамке, остальные серые.

Исходное состояние: **данных об очереди ходов в проекте не существовало.** `TurnExecutor` вычислял порядок инлайн двумя почти одинаковыми LINQ-цепочками (`ExecutePlayerTurnsAsync` / `ExecuteEnemyTurnsAsync`), итерировал результат и выбрасывал. Наружу не публиковалось ничего: ни «начался раунд», ни «пошёл вот этот юнит».

Никакой инициативы, скорости хода или очков действия в проекте нет — раунд это просто «все живые союзники, затем все живые враги», внутри стороны сортировка по grid-позиции.

**Доработка (тот же день).** Первая версия колонки перестраивала список на каждый ход (аватарки телепортировались), убитый враг залипал в колонке до следующего сигнала, а ревью нашло два CRITICAL — пул отдавал живую вью и сторона юнита определялась по типу фасада. Ниже — что изменилось и почему.

## Рассмотренные варианты

**1. Откуда UI берёт порядок.**
- *Пересчитывать в презентере из `IUnitTracker`* — не требует правок `TurnExecutor`, но это второй экземпляр правила сортировки: разъедутся при первой же правке.
- *Пересчитывать общей функцией по запросу* — правило одно, но 🔴 **список всё равно врёт**: юниты ДВИГАЮТСЯ во время своего хода, ключи сортировки (`Movement.CurrentPathNode.GridPosition`) меняются посреди раунда, и уже походивший юнит может пересортироваться выше ещё не походивших.
- ✅ *Отдавать тот самый список, который executor реально итерирует* — `TurnExecutor` записывает снапшот раунда через `ITurnOrderRecorder`, `ITurnOrderProvider` его отдаёт, фильтруя мёртвых, но **не пересортировывая**.

**2. Форма связи «бой → UI».**
- *Презентер тянет состояние в `Tick()`* — polling ради события, которое и так известно точно.
- ✅ *Новые сигналы* — `UnitTurnStartedSignal { AnimalFacade Unit }`, `TurnRoundStartedSignal { bool IsPlayerSide }` и (доработка) `UnitTurnCompletedSignal { AnimalFacade Unit }`, все `.OptionalSubscriber()`. Бой публикует факты, UI подписывается; удаление подписчика ничего не ломает.

**3. Спрайты аватарок.**
- *Переиспользовать `Assets/UI_Sprites/AnimalHeads/*.png`* (иконки карточки статов от 2026-08-01) — 🔴 **непригодны**: все 9 — чисто белые силуэты (каждый непрозрачный пиксель ровно `255,255,255`), форма живёт только в альфе. Grayscale от белого = белый, активный и неактивный неотличимы.
- *Нарисовать процедурно* — цвет пришлось бы выдумывать, к моделям он бы не относился.
- ✅ *Запечь портреты из реальных 3D-префабов* editor-тулой `AnimalPortraitBaker` (`Tools/Animals/Bake Turn Order Portraits`) в `Assets/UI_Sprites/AnimalPortraits/`.

**4. Куда вешать колонку.**
- *Отредактировать `Canvas.prefab`* — общий префаб UI, правка задевает параллельную сессию и весь остальной UI.
- ✅ *Свой префаб + `FromComponentInNewPrefab` + репарент под `Canvas` в `Awake`* — ровно паттерн `AnimalStatsPanelView.AttachToCanvas`. `Canvas.prefab` не тронут вообще.

**5. Обесцвечивание неактивных.**
- *Держать два набора спрайтов (цветной + серый)* — 18 текстур вместо 9.
- *`Image.color` в серый* — тонирует, а не обесцвечивает: цветной портрет остаётся цветным, просто тусклым.
- ✅ *Шейдер `UIGrayscale` + один общий материал* на неактивных.

**6. [доработка] Как довести смерть врага до колонки.**
- *Расширить `IUnitTracker`* — подписать трекер на `Health.Died` врагов и фаерить событие. Правка ядра ради UI, blast radius за пределы фичи.
- *Тикать презентер* — polling ради факта, который бою известен точно.
- ✅ *`UnitTurnCompletedSignal` + фильтр `Health.IsDead` в провайдере.* Сигнал фаерится в `TurnExecutor.ExecuteUnitTurnsAsync` сразу после `await ExecuteSingleUnitTurnAsync`, внутри того же `foreach` и после того же guard'а, что и `UnitTurnStartedSignal`, — Started/Completed структурно парные. `IUnitTracker` не тронут.

**7. [доработка] Плавная ротация колонки.**
- *Оставить `VerticalLayoutGroup` и анимировать внутри слотов* — лэйаут-группа сама расставляет детей и перестраивается мгновенно: анимировать перестановку под ней нечем.
- ✅ *Снять `VerticalLayoutGroup` и `ContentSizeFitter` с префаба и считать слоты в коде* — `_entryHeight` / `_entrySpacing` сериализованы, энтри едут твином `anchoredPosition`, высоту колонки выставляет сам `Render`.

**8. [доработка] Кроссфейд серый ↔ цветной.**
- *Твинить `_Saturation` общего материала* — 🔴 материал ОБЩИЙ: правка перекрашивает сразу все энтри.
- *Per-instance материал на энтри* — аллоцируется на каждый энтри и в этом проекте никем не уничтожается (тот же класс утечки, что ловили в `MergeSweepGlow`).
- ✅ *Два наложенных `Image`* — нижний `Portrait` с общим `UIGrayscale.mat`, верхний `PortraitColor` без материала; твинится альфа верхнего.

**9. [доработка] Как определять сторону юнита.**
- *`unit is EnemyAnimalFacade`* — 🔴 **лжёт**: от `EnemyAnimalFacade` наследуются только `PterodactylFacade`, `TRexFacade`, `VelociraptorFacade`, а три «temp»-врага (`EnemyChicken_Temp` → `ChickenFacade`, `EnemyCheetah_Temp` → `CheetahFacade`, `EnemyElephant_Temp` → `ElephantFacade`) наследуются от `PlayerAnimalFacade`.
- *Форкнуть префабы врагов под отдельные фасады* — дублирование префабов ради флага в UI.
- ✅ *`TurnOrderUnit { AnimalFacade Unit; bool IsEnemy }`* — сторона едет вместе с юнитом от того, кто её знает достоверно.

## Принятое решение

### Данные — `Assets/Code/Battle/Services/`

`TurnOrderCalculator` — статический класс с `OrderPlayerUnits` / `OrderEnemyUnits`. LINQ-цепочки перенесены из `TurnExecutor` **вербатим** (союзники — `OrderBy(x).ThenByDescending(y)`, враги — `OrderByDescending(x).ThenByDescending(y)`), чтобы правка не меняла порядок исполнения боя.

`ITurnOrderProvider` (`GetRoundOrder()`) и `ITurnOrderRecorder` (`RecordPlayerTurns` / `RecordEnemyTurns` / `Clear`) — **read и write разделены**: у UI физически нет способа испортить снапшот, у боя — нет причины знать про чтение. Обе реализует один `TurnOrderProvider`, биндинг `Container.BindInterfacesTo<TurnOrderProvider>().AsSingle()` (один инстанс на оба интерфейса).

Провайдер хранит ally-хвост и enemy-хвост раздельно и конкатенирует их при чтении. `RecordPlayerTurns` заодно снапшотит текущий порядок врагов — иначе во время фазы игрока хвост колонки был бы пуст. `AppendAlive` отбрасывает `null`, fake-null и `Health.IsDead`, но порядок оставшихся не трогает.

> [!important] Фоллбэк на живой расчёт — только пока раунд не записан
> `_hasRecordedRound == false` (пре-батл, до первого `fight!`) → провайдер считает порядок через `TurnOrderCalculator` от `IUnitTracker`. Это корректно ровно потому, что вне боя никто не движется. `Clear()` в конце вражеской фазы возвращает провайдер в это состояние.

**[доработка] `GetRoundOrder()` отдаёт `IReadOnlyList<TurnOrderUnit>`, а не `IReadOnlyList<AnimalFacade>`.** Сторона проставляется там, где она известна достоверно: в записанной ветке — из `_playerTurns` / `_enemyTurns`, в пре-батл фоллбэке — из `GetAlivePlayerUnits()` / `GetAliveEnemyUnits()`. Тип фасада не спрашивается нигде.

### Сигналы — `Assets/Code/Battle/Signals/`

`TurnRoundStartedSignal` фаерится после записи снапшота, до `ExecuteUnitTurnsAsync`. `UnitTurnStartedSignal` — внутри цикла, **после** guard на `null`/`IsDead` и **до** выполнения хода. **[доработка] `UnitTurnCompletedSignal`** — там же, сразу после `await ExecuteSingleUnitTurnAsync`. Исполнение боя байт-в-байт прежнее: добавлено только публикование фактов.

> [!warning] Смерть ВРАГА не проходит через `IUnitTracker`
> `UnitTracker.OnUnitRemoved` инвокает `PlayerUnitsChanged` только при `removedFromPlayers`, а на `Health.Died` трекер подписан лишь для союзников — поэтому смерть врага не вызывала НИ ОДНОГО обновления UI, и убитый враг залипал в колонке. `UnitTurnCompletedSignal` + фильтр `Health.IsDead` в провайдере закрывают это без правки трекера и работают даже пока юнит ещё числится живым в трекере и проигрывает анимацию смерти.

### UI — `Assets/Code/Battle/UI/`

`TurnOrderView` (свой пул энтри, `_maxEntries = 6`, `CanvasGroup`-fade, sibling index 0) · `TurnOrderEntryView` · `TurnOrderPresenter : IInitializable, IDisposable`.

Презентер ротирует список так, что активный оказывается наверху (`order[(activeIndex + i) % order.Count]`), и отдаёт вью список `TurnOrderEntryData { int Id, Sprite Portrait, bool IsActive, bool IsEnemy }` — **вью не знает про `AnimalFacade`**. Подписки строго симметричны `Initialize`/`Dispose`: `PreBattlePhaseStartedSignal`, `AllySpawnedSignal`, `AllyMergedSignal`, `IUnitTracker.PlayerUnitsChanged`, три сигнала очереди и `StateChangedSignal` (Hide на `WinState`/`LoseState`/`CampaignVictoryState`).

> [!warning] `_hasActiveTurn` отделён от `_activeUnit`
> `UnitTurnCompletedSignal` гасит подсветку, но **не** сбрасывает `_activeUnit`: иначе `ResolveActiveIndex` откатился бы на index 0 и между ходами колонка прыгала бы к первому юниту раунда.

#### [доработка] Плавный транзишн

> [!warning] `VerticalLayoutGroup` и `ContentSizeFitter` УДАЛЕНЫ из `TurnOrderView.prefab`
> Прежняя запись этого решения («твин на внутреннем `_content`, потому что `VerticalLayoutGroup` игнорирует `localScale`») **недействительна**. Лэйаут-группа расставляет детей сама и перестраивается мгновенно — анимировать перестановку под ней нечем. Слоты считает код (`_entryHeight`, `_entrySpacing` сериализованы), энтри позиционируются твином `anchoredPosition`, высоту колонки (`sizeDelta.y`) выставляет сам `Render`. Якоря: корень (0,1)-(0,1) пивот (0,1); энтри анкор (0.5,1) пивот (0.5,1).

**Идентичность энтри — `TurnOrderEntryData.Id` = `unit.GetInstanceID()`.** `Render` матчит существующие вью по `Id`, добирает недостающие из пула и ретайрит лишние. Именно это превращает ротацию в движение вместо перестройки списка.

> [!danger] Пул никогда не должен отдавать вью, которая ещё числится активной
> Второй fallback-проход `TakeAvailableEntry` исключал только `_next`, но не `_active`, и возвращал вью, всё ещё представляющую другого юнита. `Render` считал такую вью `animated: true`, пропускал `Restore()` и анимировал чужую аватарку: портрет щёлкал, по колонке ехал не тот аватар. Воспроизводилось КАЖДЫЙ ход, когда юнитов больше `_maxEntries` (первый уровень — 9 юнитов при 6 слотах), и при мердже с полной колонкой. Починено удалением fallback-прохода: при исчерпанном пуле инстанцируется свежая вью, пул стабилизируется на `_maxEntries + 1`. **Признак живости обязан быть identity-aware, а не «не в списке следующих».**

Кроссфейд серый ↔ цветной — два наложенных `Image`; рамка теперь всегда enabled и фейдится по альфе вместо `SetActive`; масштаб активного — прежний `DOScale`/`OutBack`. Реэнтерабельность обязательна: `Refresh` прилетает пачками (Completed(N) и Started(N+1) фаятся в одном синхронном блоке), поэтому у каждого анимируемого свойства свой твин-филд, который киллится перед новым, и всё киллится в `OnDisable`/`OnDestroy`. Тайминги сериализованы: `_moveDuration` 0.28 / OutCubic, `_colorFadeDuration` 0.25, `_activateDuration` 0.25 / OutBack, `_deactivateDuration` 0.2 / OutQuad, `_retireDuration` 0.15, `_appearDuration` 0.15 (возврат из пула теперь симметричен выходу), `_activeScale` 1.15.

> [!warning] Колонка стоит там, где `AnimalMover` читает тапы
> Все `Image` — `raycastTarget = false`, `CanvasGroup.blocksRaycasts = false` и `interactable = false`. Иначе левый верхний угол поля стал бы мёртвой зоной для выбора животного.

### Портреты — `AnimalPortraitBaker` + `AnimalConfig.Portrait`

Тула резолвит префаб под `AnimalType`, **читая `AnimalFacade.Type` с корня префаба**, а не по хардкод-таблице имён файлов — переименование префаба тулу не ломает.

`Portrait` — **отдельное поле `AnimalConfig`, а не переиспользование `Icon`**: `Icon` потребляет `AnimalStatsPanelPresenter`, и подмена спрайта поменяла бы карточку статов. `AnimalDatabase.GetPortrait()` возвращает `Portrait ?? Icon` — новый тип животного без портрета деградирует к иконке, а не к пустоте.

Портреты запекаются непрозрачными с тёмным фоном (opaque-шейдеры моделей не пишут альфу надёжно), поэтому `Background`-image из entry-префаба убран.

Попутно починена дыра в данных: у `Pterodactyl` в `AnimalDatabase.asset` `Icon` был `{fileID: 0}` — это чинит заодно карточку статов и merge-popup.

### Grayscale — `Assets/Shaders/UIGrayscale.shader`

Написан на базе UI-Default с сохранением stencil-пасса, `_ClipRect`, `UNITY_UI_ALPHACLIP` и `ZTest [unity_GUIZTestMode]` — иначе Image ломается внутри масок и Scroll Rect. Один общий `UIGrayscale.mat` на нижнем `Image` всех энтри: **никаких per-instance материалов и, значит, никаких утечек.**

## Почему так

- **Отображать надо тот список, который executor реально итерирует.** Это не деталь этой фичи, а общий принцип для любого будущего UI поверх порядка ходов (предсказание, «кто следующий», перетасовка инициативы): пересчёт по требованию корректен только там, где ключи сортировки неизменны в течение раунда — здесь они меняются, потому что ход юнита это и есть его перемещение.
- **Разделение `Provider`/`Recorder` вместо одного интерфейса** держит направление зависимости честным: `TurnExecutor` не получает способа читать очередь и обрастать логикой поверх неё.
- **Сторона юнита — runtime-принадлежность, а не compile-time тип.** Один и тот же префаб с `ChickenFacade` используется и как союзник, и как враг, поэтому никакая иерархия фасадов этого не выразит без форка префаба. Определять сторону надо по тому, из какого списка трекера юнит пришёл — и это общее правило для любого UI или логики, которой нужно «свой/чужой», а не частность колонки.
- **Отдельный префаб вместо правки `Canvas.prefab`** — блокирующее соображение было не архитектурное, а операционное: `Canvas.prefab` общий, а редактор Unity делят параллельные сессии (см. [[2026-08-01-victory-defeat-flow]], где обвязка Canvas уже делалась руками).
- **Смерть врага закрыта сигналом, а не правкой `IUnitTracker`** — трекер это ядро боя, а требование пришло от UI; сигнал уже существовал как форма связи, и добавление парного Completed к Started ничего не усложняет.
- **Мокап показывает врага наверху колонки, но реальный флоу — `PlayerTurnState` первым**, значит наверху союзник. Подделывать порядок под картинку не стали: колонка обязана быть правдой о ходе боя.

## Проверка

- Компиляция: 0 ошибок. В консоли остаётся только пре-существующий шум — NRE `UnityEditor.Graphs/Edge.cs` и ошибки animator-параметров `ChickentwoLayerController` через `EnemySpawnService.cs:86`. Не регрессии.
- `TurnExecutorTests` + `BattleEdgeCasesTests` — **16/16 passed** (правился только `BattleTestHelper` — конструктор `TurnExecutor` получил новые параметры).
- Play Mode, первая версия: колонка populated в пре-батле; после `fight!` ходящий слон ротировался наверх цветным и увеличенным, остальные — серые.
- Play Mode после доработки, 11 юнитов в 6-слотовой колонке: **7 вью использовано, 0 несвязанных слотов, 0 нарушений идентичности за 11 ходов, 0 расхождений стороны против `IUnitTracker`.**
- **TESTS PAUSED** — новых тестов не писали.

## Затронутые файлы

**Изменены:** `Assets/Code/Battle/Services/TurnExecutor.cs` · `Assets/Code/Data/Animals/AnimalDatabase.cs` · `Assets/Code/Infrastructure/Installers/BattleInstaller.cs` · `Assets/Code/Tests/EditorTests/BattleSystem/Helpers/BattleTestHelper.cs` · `Assets/Resources/AnimalDatabase.asset` · `Assets/Scenes/Main Scene.unity` (+2 строки: `_turnOrderPrefab` на `BattleInstaller`)

**Новые:** `Assets/Code/Battle/Services/{ITurnOrderProvider,ITurnOrderRecorder,TurnOrderProvider,TurnOrderCalculator,TurnOrderUnit}.cs` · `Assets/Code/Battle/Signals/{UnitTurnStartedSignal,UnitTurnCompletedSignal,TurnRoundStartedSignal}.cs` · `Assets/Code/Battle/UI/{ITurnOrderView,TurnOrderEntryData,TurnOrderEntryView,TurnOrderPresenter,TurnOrderView}.cs` · `Assets/Code/Editor/AnimalPortraits/AnimalPortraitBaker.cs` · `Assets/Prefabs/{TurnOrderView,TurnOrderEntryView}.prefab` · `Assets/Shaders/UIGrayscale.shader` · `Assets/UI_Sprites/UIGrayscale.mat` · `Assets/UI_Sprites/AnimalPortraits/portrait_*.png` (9 шт.)

## Открытые пункты

- `ITurnOrderProvider.GetRoundOrder()` аллоцирует список на каждый `Refresh` — оставлено осознанно, путь не горячий (несколько раз за ход, не каждый кадр).
- `TurnOrderView.Render` продолжает считать слоты и гонять твины, пока колонка скрыта (`alpha = 0` после Win/Lose).
- `unit is EnemyAnimalFacade` остаётся неверным для трёх temp-врагов **везде за пределами колонки** — фундаментальное решение (сторона как поле рантайм-состояния либо форк префабов) за владельцем.

## Связанное

- [[2026-08-01-animal-selection-and-stats-panel]] — паттерн `AttachToCanvas`, `AnimalConfig.Icon` и белые силуэты `AnimalHeads`
- [[2026-08-01-victory-defeat-flow]] — `StateChangedSignal`, `WinState`/`LoseState`/`CampaignVictoryState`
- [[2026-07-31-pre-battle-phase-architecture]] — «команды внутрь через интерфейсы, факты наружу через `SignalBus`»
- [[2026-08-01-merge-sweep-glow]] — тот же класс утечки: рантайм-материал, который никто не уничтожает
