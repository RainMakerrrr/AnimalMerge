# Решение: выбор животного вынесен в `IAnimalSelectionService`; карточка статов — отдельная пара View/Presenter

**Дата:** 2026-08-01
**Статус:** принято, реализовано (код + префаб + сцена + спрайты)

## Контекст

Задача из макета: по тапу на животном показывать кремовую карточку с иконкой животного, уроном и здоровьем; одновременно подсвечивать радиус хода; плюс перекрасить кнопки «добавить животное» и «в бой» в цвета макета.

Исходное состояние:

- Подсветкой владел `AnimalMover` — он держал `IMoveRangeHighlighter` напрямую (наследие решения [[2026-06-27-move-range-highlighter]]) и **всегда прятал её на релизе**. Понятия «выбранное животное» в проекте не существовало вообще: жест начинался и заканчивался внутри одного кадра-цепочки.
- Значит, карточку было некуда вешать: если бы презентер подписался на `AnimalMover`, у подсветки и у карточки оказались бы два независимых источника истины, и любой новый путь очистки (смерть юнита, конец фазы, драг) пришлось бы дублировать в обоих.
- Статы животного мутируются в рантайме (`MergeTarget.ExecuteMergeDirectly`), поэтому показывать `AnimalStats` из конфига нельзя — после мерджа карточка лгала бы.

## Рассмотренные варианты

**1. Где живёт состояние «кто выбран».**
- *В `AnimalMover`* — минимум новых типов, но мувер получает знание про Canvas/TMP и становится вторым владельцем подсветки; десинк карточки и подсветки — вопрос времени.
- *В презентере карточки* — презентер начинает командовать `IMoveRangeHighlighter`, то есть UI-слой владеет геймплейным визуалом.
- ✅ *Отдельный `IAnimalSelectionService`* — единственный источник истины, владеет `IMoveRangeHighlighter` (переехал из `AnimalMover`), наружу отдаёт `event Action<AnimalFacade> SelectionChanged` (`null` = сброшено). `AnimalMover` остаётся переводчиком жеста в намерение и про UI не знает.

**2. Где стоит гейт «только в пре-батл фазе».**
- *В презентере* — он подписывается на `PreBattlePhaseEndedSignal` и зовёт `_selectionService.Clear()`. Инверсия зависимости: view-слой командует сервисом; к тому же подсветка гасилась бы «через» UI.
- ✅ *В самом сервисе* — он подписан на `PreBattlePhaseStartedSignal` / `PreBattlePhaseEndedSignal`, флаг `_selectionAllowed` делает `Select()` no-op вне фазы. У презентера **нет** зависимости от `SignalBus` вообще.

**3. Выбор врагов.**
- *Разрешить* — начальное решение, под него был перекрашен `_moveRangeColor` на вражеских префабах.
- ✅ *Оставить только союзников* и **откатить** правку вражеских префабов (`git checkout HEAD --`) — она была чистым no-op: враги лежат на слое 23 (`Enemy`), а `AnimalMover.CastRay` маскирует слой 20 (`Animal`), то есть враг физически не выбирается.

**4. Фон карточки.**
- *Нарисовать 9-slice арт* — лишний ассет ради прямоугольника с рамкой.
- ✅ *Два built-in `UISprite`-Image стопкой* — рамка `#8A7A55`, кремовая заливка `#E8DCC0` с инсетом 4 px. Макет воспроизводится точно.

**5. Иконки животных.**
- *Сгенерировать нейросетью* — `mcp__UnityMCP__generate_image` вернул `configured: false` и для fal, и для openrouter; провайдер не настроен.
- ✅ *Нарисовать процедурно через `execute_code`* — лапа и сердце из имплицитных форм; головы животных — растеризация скинного меша каждого префаба в профиль, кадрирование по взвешенным вершинам кости `Head`.

## Принятое решение

### Слой выбора — `Assets/Code/Animals/Selection/`

`AnimalSelectionService : IAnimalSelectionService, IInitializable, ITickable, IDisposable`, биндинг `BindInterfacesAndSelfTo<AnimalSelectionService>().AsSingle()` в `CurrentGameInstaller` — жизненным циклом управляет Zenject-кернел.

Три независимых пути самозалечивания, потому что «выбранное» переживает кадры:

1. подписка на `AnimalFacade.OnRemoved`;
2. `Tick()` ловит **fake-null** `_selected` (`ReferenceEquals(_selected, null) == false && _selected == null`);
3. `Dispose()` снимает подписку и гасит подсветку.

> [!warning] Путь (2) обязателен — `NotifyRemoved()` зовут не все
> `EnemySpawnService:170`, `AnimalFactory:190` и `ChickenFacade:90/98` уничтожают объект напрямую, не вызывая `NotifyRemoved()`. Без тика по fake-null сервис держал бы ссылку на уничтоженный `AnimalFacade`, а подсветка висела бы на пустой сетке.

> [!important] `_selectionAllowed` инициализируется в `true`, а не в `false`
> `GameBootstrapper.Awake()` входит в стейт-машину раньше, чем Zenject-кернел вызовет `Initialize()` — первый `PreBattlePhaseStartedSignal` сервис не услышит. Рестарт при этом работает: `PreBattleState.Enter()` фаерит Started при **каждом** входе, включая внутрипроцессный рестарт.

### Жест — `AnimalMover`

Зависимость `IMoveRangeHighlighter` заменена на `IAnimalSelectionService`. Правила: тап — выбрать · тап по тому же — снять · тап по пустому месту — снять · драг — снять и карточку не показывать · **выбор переживает отпускание пальца** (раньше релиз всегда гасил подсветку).

`Clear()` вызывается только на ветке промаха рейкаста и внутри `BeginDrag`, а **не на каждом нажатии** — иначе при переключении между двумя животными карточка мигала, а подсветка пропадала на ~150 мс.

> [!danger] `EventSystem.IsPointerOverGameObject()` без аргумента — мёртвый код на мобилках
> Безаргументная перегрузка резолвит id мыши `-1`, а `StandaloneInputModule` кладёт тач-данные под `fingerId`. `IsPointerOverUi()` теперь берёт `Input.GetTouch(0).fingerId` при `Input.touchCount > 0` и падает на безаргументный вызов только как editor/standalone-фолбэк. Проверить остальные точки ввода в проекте на ту же ошибку.

`TryPickAnimal()` первым делом обнуляет `_current` — иначе устаревшее значение позволяло «выбрать» животное, которого игрок не касался. Имена сериализованных полей `_holdTimeToShow` / `_dragThresholdPixels` намеренно не менялись, чтобы значения из сцены пережили правку.

### Карточка — `Assets/Code/Animals/UI/`

`IAnimalStatsPanelView` / `AnimalStatsPanelView` / `AnimalStatsPanelPresenter` + `Assets/Prefabs/AnimalStatsPanelView.prefab`. Screen-space-overlay, следует за мировой позицией животного (`WorldToScreenPoint` → `ScreenPointToLocalPointInRectangle` относительно инжектнутого `Canvas` — та же математика, что у `DamagePopupController` / `MergePopupController` / `TutorialHandView`), в `Awake` перепривязывается под этот Canvas.

`ResolvePanelPosition` + `CoversAnchor` выбирают сторону по **склампленному** результату, а не по сырому офсету: на сыром офсете в полосе x ∈ [−393, −123] карточка накрывала животное, которое описывает. `LateUpdate` дополнительно прячет панель, когда ранее выданный анкер стал fake-null.

Презентер читает `AnimalFacade.GetDamage()` / `GetCurrentHealth()` — **никогда `AnimalStats`** — и живо обновляется по `Health.HealthChanged`. Биндинг в `BattleInstaller`: `FromComponentInNewPrefab` для view (сериализованное поле `_statsPanelPrefab`, назначено в `Main Scene.unity`) + `BindInterfacesAndSelfTo<AnimalStatsPanelPresenter>().AsSingle().NonLazy()`.

### Данные и арт

`AnimalDatabase.AnimalConfig` получил `public Sprite Icon` и `GetIcon(AnimalType)` (молча возвращает `null`); иконки разведены для всех 9 типов в `Assets/Resources/AnimalDatabase.asset`. Файлы лежат в `Assets/UI_Sprites/` — **не в `Resources/`**, в билд их тянет ссылка из `AnimalDatabase.asset`.

У `Hedgehog` и `Velociraptor` в риге нет кости головы (меш ежа скинен на единственную кость `body` плюс посторонний сабмеш «Fox Tail» с целым лисьим скелетом), поэтому эти двое — силуэты в полный рост.

### Цвета макета

`AddAnimalButtonView._normalColor` `#7CB342` / `_disabledColor` `#5A7F33`; `BattleButtonView._readyColor` `#F5A623` / `_notReadyColor` `#B0762A`; подписи белые, `riffic-bold SDF`. Радиус хода перекрашен в янтарный `{0.96, 0.80, 0.47, 1}` на шести союзных префабах в `Assets/Resources/Prefabs/Animals/`.

> [!warning] Кнопки нельзя перекрашивать через `Image.m_Color`
> Источник истины — **сериализованные поля** цвета: `ApplyState()` / `SetInteractable()` затирают `_targetGraphic.color` на каждом `Refresh()`.

## Почему так

- **Выбор — это состояние домена, а не деталь ввода и не деталь UI.** Пока его не было, подсветка и карточка неизбежно разъезжались бы на каждом новом пути очистки. Один сервис = одно место, где перечислены все причины сброса (смерть, драг, конец фазы, тап по пустому, уничтожение в обход `NotifyRemoved`). Это прямое развитие [[2026-06-27-move-range-highlighter]]: тогда подсветку вынесли из `AnimalMovement`, теперь владельца подсветки вынесли из `AnimalMover`.
- **Гейт фазы принадлежит владельцу состояния.** Если гейт стоит в презентере, то каждый будущий потребитель выбора обязан продублировать подписку на сигналы фазы. Плюс презентер остался без `SignalBus` — его зависимости чисто отображающие.
- **Иконка легла на `AnimalConfig`, а не на `AnimalStats`.** Иерархия подклассов `AnimalStats` зарезервирована под балансные данные — консистентно с [[2026-06-27-fox-dodge-stats]]. Иконка — презентационный ассет, ей место в общей базе.
- **Ally-only оставлено сознательно.** Слоевая маска и так делает врагов невыбираемыми; побочный полезный эффект — нет утечки информации о радиусе хода врага. Расширение маски эту утечку создаст, и это отдельное геймдизайнерское решение.
- **Читать рантайм-значения, а не конфиг** — единственный способ пережить мердж: `MergeTarget.ExecuteMergeDirectly` мутирует урон и здоровье, конфиг после этого описывает уже несуществующее животное.

## Проверка

- Unity Console — ноль ошибок компиляции. Единственная запись об ошибке — предсуществующий NRE Unity `UnityEditor.Graphs/Edge.cs:114` от открытого окна Animator, к правке отношения не имеет.
- Play Mode: гейт фазы включается/выключается по сигналам; «осиротевший» выбор самозалечивается после `DestroyImmediate`, минующего `NotifyRemoved()`; проход по размещению панели от `x = −520` до `x = +520` — `CoversAnchor` ложно во всех точках.
- **Тестов не писали — TESTS PAUSED.** Ни один существующий тест не ссылается на `AnimalMover`, `AnimalSelectionService`, `AnimalStatsPanel*`, `MoveRangeHighlighter`, `AnimalDatabase` или оба инсталлера — релевантного набора для прогона не существует. Полный EditMode-прогон на ревью: 634 теста / 25 падений, все в `AttackAndDamageSystem` (NUnit: async-сигнатуры не `void`) и `GridPathfinding.TargetPositionCalculatorTests` — вне этого диффа. Чистый baseline «до» снять не удалось: рабочее дерево делит параллельная сессия.

## Затронутые файлы

**Новые:**
`Assets/Code/Animals/Selection/{IAnimalSelectionService, AnimalSelectionService}.cs`;
`Assets/Code/Animals/UI/{IAnimalStatsPanelView, AnimalStatsPanelView, AnimalStatsPanelPresenter}.cs`;
`Assets/Prefabs/AnimalStatsPanelView.prefab`;
`Assets/UI_Sprites/{icon_attack_paw, icon_health_heart}.png`;
`Assets/UI_Sprites/AnimalHeads/head_{cheetah, chicken, deer, elephant, fox, hedgehog, pterodactyl, trex, velociraptor}.png`.

**Изменены:**
`Assets/Code/Animals/AnimalMover.cs`;
`Assets/Code/Data/Animals/AnimalDatabase.cs`;
`Assets/Code/Infrastructure/Installers/{BattleInstaller, CurrentGameInstaller}.cs`;
`Assets/Resources/AnimalDatabase.asset`;
`Assets/Resources/Prefabs/Animals/{Cheetah, Chicken, Deer, Elephant, Fox, Hedgehog}.prefab`;
`Assets/Scenes/Main Scene.unity`.

## Связанное
- [[2026-08-01]] — лог сессии
- [[2026-06-27-move-range-highlighter]] — предыдущий шаг: подсветка вынесена из `AnimalMovement`
- [[2026-06-27-fox-dodge-stats]] — граница между `AnimalStats` и `AnimalConfig`
- [[Index]] — карта проекта
