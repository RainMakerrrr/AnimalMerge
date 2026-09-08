# Решение: боевой HUD по макету — итерация 1, часть 1 (только C# и плейсхолдер-арт)

**Дата:** 2026-09-07
**Статус:** ✅ **[обновлено 2026-09-08] принято и реализовано полностью.** На момент записи была
готова только часть 1 (C#-слой), а редакторская часть блокировалась отключённым Unity MCP —
**2026-09-08 часть 2 сделана целиком, фича завершена, Play Mode рабочий**. Все пометки «не
сделано» и «Play Mode сломан» ниже сохранены как история и **недействительны**; редакторская
часть и добытые в ней знания — [[2026-09-08-battle-hud-layout-editor-part]]

---

## Контекст

Задача — привести боевой HUD к макету: счётчик «03 / 05» на кнопке ADD ANIMAL, состояния
disabled/enabled у FIGHT и UNDO MERGE, расширенная карточка статов (заголовок + строки
способностей), отдельная карточка врага, safe area, надпись LEVEL.

Работа сознательно разделена на **две части**:

- **Часть 1 (эта запись)** — всё, что выражается в коде и в файлах на диске: контракты, вью,
  презентеры, биндинги, импорт-настройки текстур, плейсхолдер-арт.
- **Часть 2 (не сделана)** — всё, что делается только руками в редакторе: раскладка в
  `Canvas.prefab` и `Main Scene`, создание `EnemyCardView.prefab`, назначение сериализованных
  ссылок, назначение спрайтов, 9-slice-бордеры, `CanvasScaler.match`, `Button.transition`.

Разделение вынужденное: Unity MCP всю сессию отдавал `ConnectionRefused`, редактор недоступен.

Перед вёрсткой разведано фактическое устройство UI проекта, и три вещи оказались не тем, чем
выглядели, — они и определили архитектуру решения (ниже).

## Рассмотренные варианты

### 1. Знаменатель счётчика «03 / 05»

| Вариант | Оценка |
|---|---|
| Тотал = `MinAlliesToStart` из `PreBattleConfig` | Отклонён — это порог готовности, а не размер ростера; числа расходятся |
| Тотал = длина массива пула в конфиге | Отклонён — пул уровня 2+ рандомный, конфиг не знает, сколько реально положили в очередь |
| **Тотал = сколько всего положили в `AllySpawnPool` за фазу** | ✅ Принят (решение владельца) — знаменатель всегда равен тому, что игрок реально получил |

### 2. Серый (disabled) вид кнопки FIGHT

| Вариант | Оценка |
|---|---|
| Нарисовать второй, серый спрайт кнопки | Отклонён — лишний ассет, который придётся сопровождать парой к оранжевому |
| `ColorTint` поверх оранжевого спрайта | Отклонён — умножение на серый даёт **коричневый**, а не металлик; выглядит как грязь, а не как «недоступно» |
| **Grayscale-материал (`UI_Sprites/UIGrayscale.mat`) поверх существующего спрайта** | ✅ Принят — честное обесцвечивание, материал в проекте уже есть (заведён для колонки очереди ходов 2026-08-02) |

### 3. Откуда карточка врага берёт цель

| Вариант | Оценка |
|---|---|
| Через `IAnimalSelectionService`, как карточка союзника | ❌ **Невозможно**: `AnimalMover.CastRay()` кастует `LayerMask.GetMask("Animal")`, а враги живут на слое `Enemy` (23) — врага физически нельзя выбрать тапом |
| Расширить маску райкаста до `Enemy` | Отклонён — тап по врагу сейчас часть жеста движения/атаки; расширение маски меняет игровой ввод ради UI |
| **Свой презентер поверх `IUnitTracker` + сигналов боя** | ✅ Принят — карточка врага не зависит от выбора вообще |

### 4. Строки способностей в карточке

| Вариант | Оценка |
|---|---|
| Расширить `IAbility` полями Name/Chance и рисовать реальные строки | Отклонён на этой итерации — `IAbility` не отдаёт ни имени, ни шанса; расширение задевает все способности и подклассы `AnimalStats`. Вынесено во вторую итерацию |
| Захардкодить строки в презентере | Отклонён — данные о балансе уехали бы в презентационный слой |
| **Трёхзначный контракт `abilityLines`** (см. ниже) | ✅ Принят — префаб может держать статический текст, а код готов к реальным данным без смены сигнатуры |

## Принятое решение

### Счётчик ADD ANIMAL «03 / 05»

- `IAllySpawnPool.Total` / `AllySpawnPool._total` — инкремент в `Enqueue`, сброс в `Clear`.
- `IAllySpawnService.PoolTotal` пробрасывает его наружу.
- `AddAnimalButtonView.SetRemaining(int remaining, int total)` — **два раздельных лейбла**
  (`_remainingLabel` / `_totalLabel`), формат `D2`, знаменатель пишется как `/ 05`.

Раздельные лейблы, а не одна строка, потому что в макете у числителя и знаменателя разные
кегль и цвет.

### FIGHT — disabled/enabled без нового арта

`BattleButtonView` получил `Graphic[] _stateGraphics` и `Material _notReadyMaterial`. Три
несущих детали:

1. **Авторские материалы кэшируются лениво в `Dictionary<Graphic, Material>` и
   восстанавливаются**, а не затираются в `null`. Присвоение `graphic.material = null`
   вернуло бы дефолтный UI-материал и потеряло бы всё, что художник назначил в префабе.
2. 🔴 **TMP-лейблы отфильтрованы из назначения материала** (`AcceptsMaterial(graphic) =>
   (graphic is TMP_Text) == false`). `TextMeshProUGUI` наследует `Graphic`, но **не роутит**
   `Graphic.material` в свой рендер — присваивание проходит молча, без ошибки и без эффекта.
   Материальные эффекты на TMP ставятся через `TMP_Text.fontMaterial`. Здесь лейблы гасятся
   только цветом.
3. **Когда grayscale-материал реально применён, тинт `_notReadyColor` НЕ домножается** —
   иначе двойное затемнение: обесцвеченный спрайт ещё и уходит в темноту.

### UNDO MERGE — кода не потребовалось

`UndoMergeButton` уже выставляет `interactable = stackCount > 0`. Disabled-вид даётся
`Button.transition = ColorTint` — это настройка в редакторе, часть 2. Ни строки C# не написано
сознательно: писать вью ради того, что уже умеет штатный `Button`, — лишняя сущность.

### Карточка статов — контракт `abilityLines`

`IAnimalStatsPanelView.Show(Transform anchor, Sprite icon, int attack, int health,
string title, IReadOnlyList<string> abilityLines)`.

> [!important] Трёхзначный контракт `abilityLines`
> - `null` — **«не трогать лейблы»**: в префабе остаётся статический текст («30% Dodge»,
>   «50% Kickback»). В коде выражено именованной константой
>   `private const IReadOnlyList<string> KeepPrefabAbilityLines = null;` — чтобы на месте
>   вызова было видно намерение, а не голый `null`.
> - **пустой список** — «способностей явно нет», все строки гасятся.
> - **непустой список** — строки берутся из него, лишние лейблы гасятся.

На итерации 1 **оба** презентера передают `KeepPrefabAbilityLines`, потому что `IAbility` не
отдаёт ни имени, ни шанса. Реальные проценты — вторая итерация.

### Карточка врага

- `EnemyCardView : AnimalStatsPanelView, IEnemyCardView` — **пустой сабкласс**. Нужен ровно
  затем, чтобы не заводить обратную зависимость `Code.Animals.UI` → `Code.Battle.UI`:
  интерфейс живёт в боевом модуле, реализация переиспользует уже готовую вью животного.
- `EnemyCardPresenter` (`IInitializable` + `IDisposable`) слушает
  `PreBattlePhaseStartedSignal` (показать), `UnitTurnStartedSignal` (перенацелить, если ходит
  живой враг) и `BattleEndedSignal` (спрятать). Цель по умолчанию — босс
  (`AnimalFacade.IsBoss`), иначе первый живой враг.
- 🔴 **Сторона юнита определяется ТОЛЬКО через трекер** (`IUnitTracker.GetAliveEnemyUnits()`),
  а не по типу фасада — продолжение решения от 2026-08-02: три temp-врага наследуются от
  `PlayerAnimalFacade`, и `unit is EnemyAnimalFacade` врёт.
- 🔴 **Карточка показывается один раз за бой и перенацеливается**, а не прячется на каждом
  ходу игрока. Прятать/показывать заново — значит пересоздавать DOTween-сиквенс появления,
  и карточка мигала бы N раз за раунд.
- DI: `BindEnemyCard()` в `BattleInstaller` —
  `Bind<IEnemyCardView>().To<EnemyCardView>().FromComponentInNewPrefab(_enemyCardPrefab).AsSingle()`
  + `BindInterfacesAndSelfTo<EnemyCardPresenter>().AsSingle().NonLazy()`, плюс проверка в
  `ValidateSceneReferences()`.

### SafeArea

Новый `Assets/Code/Framework/Code/UI/Elements/SafeArea.cs` — гоняет `Screen.safeArea` в
`anchorMin`/`anchorMax` своего `RectTransform`, пересчитывает при смене размера экрана или
ориентации. **В проекте safe area не обрабатывалась НИГДЕ** — на устройствах с вырезом HUD
уезжал под чёлку.

### Импорт-настройки плашек

`Assets/Code/Framework/Sprites/button_1.png` и `button_2.png` были импортированы как
`textureType: Default` + `spriteMode: None` — **такую текстуру нельзя назначить в `Image`**.
Переведены в `Sprite (2D and UI)` / `Single` / `Full Rect` / 9-slice border / mipmaps off.
Без Full Rect 9-slice режется по tight-меши и рвёт растяжку.

### Плейсхолдер-арт

`Assets/UI_Sprites/Placeholder/`: `icon_lightning`, `icon_boss_skull`, `icon_undo`,
`avatar_ring`, `hpbar_hatch`. Временный — художник заменит.

## Почему так

Три факта об устройстве UI проекта, выясненные разведкой, — они дороже самой вёрстки, потому
что каждый из них ломает «очевидное» решение:

1. 🔴 **`AnimalStatsPanelView` — screen-space, а НЕ world-space.** Она живёт в единственном
   экземпляре в корне overlay-`Canvas` и проецирует мировую позицию юнита через
   `Camera.WorldToScreenPoint` в `LateUpdate`. Визуально выглядит как world-space — отсюда
   стойкое заблуждение. **Единственный настоящий world-space UI** в префабах животных —
   `HealthBar.prefab` (слой `HealthBar` 8, рисуется отдельной `HealthBarCamera`).
   Практическое следствие: карточку нельзя «положить рядом с юнитом» в префабе, а
   `HealthBarCamera` — та самая камера, из-за которой зум боя сделан долли, а не FOV
   (`Decisions/2026-09-07-battle-camera-zoom.md`).
2. 🔴 **Врага нельзя выбрать тапом** — `AnimalMover.CastRay()` кастует
   `LayerMask.GetMask("Animal")`, враги на слое `Enemy` (23). Любая попытка провести карточку
   врага через `IAnimalSelectionService` была бы мёртвым кодом.
3. 🔴 **TMP молча игнорирует `Graphic.material`.** Ни ошибки, ни варнинга — эффект просто не
   применяется. Это ловушка на будущее: любой материальный эффект (grayscale, outline, маска)
   на `TextMeshProUGUI` надо ставить через `TMP_Text.fontMaterial`.

Ещё три факта об окружении, найденные попутно и относящиеся к части 2:

4. **`Canvas` в `Main Scene` — это инстанс префаба `Assets/Resources/Canvas.prefab`**, а не
   объекты сцены. Правки HUD идут в префаб, иначе теряются.
   `CanvasScaler` 1080×1920, `match = 1` (Height): на 20:9 нижний ряд HUD **наезжает сам на
   себя**. Решено менять на `0.5` в части 2.
5. **В проекте ДВА рестарта**: объект `Restart` в сцене и `Restart Button` в `Canvas.prefab`
   (со скриптом `RestartButton`). Решено оставить второй, первый скрыть — часть 2.
6. **Годного для UI арта из `33c9080e` всего 7 файлов**: `button_1.png` (тёмная beveled-плашка,
   фон почти всего HUD), `button_2.png` (оранжевая hex-кнопка FIGHT), `reload 1.png` (RESTART),
   `tapticON/OFF 1.png`, `icon_attack_paw.png`, `icon_health_heart.png` — плюс головы животных
   в `UI_Sprites/AnimalHeads/`. Остальное в коммите — 3D-текстуры и фон сцены.

## Проверка

> [!success] Обновление 2026-09-08 — часть 2 сделана, всё проверено вживую
> Play Mode **рабочий**: `Assets/Prefabs/EnemyCardView.prefab` создан и назначен в `Main Scene`,
> `NonLazy`-биндинг `BindEnemyCard()` резолвится. Целевые наборы: EditMode `AllySpawnPoolTests` /
> `AllySpawnServiceTests` / `BattleReadinessServiceTests` — **20/20**, PlayMode
> `BattleButtonViewTests` / `AddAnimalButtonViewTests` — **7/7**. Предсуществующие падения
> (`HealthBarViewTests` ×4, `AnimalAttackPostAbilityTests`, `RetreatAbilityTests`,
> `TargetPositionCalculatorTests`) к задаче не относятся и не чинились.
> Пункты ниже — состояние на 2026-09-07, **недействительны**.

- 🔴 **Unity MCP недоступен весь сеанс (`ConnectionRefused`) — консоль Unity и тесты НЕ
  проверены.**
- ✅ Компиляция проверена локальным .NET SDK против сгенерированных Unity `csproj`:
  `CodeBase`, `Code.Editor.Tests`, `Code.PlayMode.Tests` пересобираются с **0 ошибок**;
  5 предупреждений — все дозадачные.
- ✅ GUID'ы пяти рукописных `.meta` плейсхолдеров проверены на уникальность.
- ⛔️ **Play Mode сейчас сломан, и это ожидаемо.** `BindEnemyCard()` — `NonLazy` и делает
  `FromComponentInNewPrefab(_enemyCardPrefab)`, а `Assets/Prefabs/EnemyCardView.prefab` **ещё
  не создан** и в `Main Scene` не назначен (в YAML сцены поля `_enemyCardPrefab` нет). До
  выполнения части 2 контейнер упадёт на резолве. `ValidateSceneReferences()` заранее пишет
  об этом внятную ошибку.
- ⛔️ **Ничего из вёрстки не видно**, пока не сделана часть 2: новые сериализованные поля
  (`_titleLabel`, `_abilityLabels`, `_totalLabel`, `_stateGraphics`, `_notReadyMaterial`) в
  префабах пусты, и код по ним корректно ничего не делает.
- TESTS PAUSED — новых тестов нет.

## Что осталось (часть 2, редакторская) — ✅ ВЫПОЛНЕНО 2026-09-08

> [!done] Все девять пунктов закрыты
> Сверх списка сделано: `HealthBar.prefab` (штриховка `Mask` + Tiled-оверлей, иконка врага),
> `EnemyCardView.prefab`, сдвиг `TurnOrderView.prefab` из-под safe area, скрытие `Taptic Button` /
> `Coins Count` / `Heal All Units`. Пункт 6 выполнен **иначе, чем планировалось**:
> `CanvasScaler.match` = **0 (Width)**, а не 0.5 — при 0.5 канвас на экранах выше 16:9 сужается до
> 978 юнитов вместо 1080. Пункт 5 тоже иначе: 9-slice для `button_2.png` геометрически невозможен,
> итог — `Image.type = Simple`. Выкладки — [[2026-09-08-battle-hud-layout-editor-part]].

1. ✅ Создать `Assets/Prefabs/EnemyCardView.prefab` и назначить в `BattleInstaller` — **снимает
   падение Play Mode**.
2. Назначить `_titleLabel` / `_abilityLabels` в `AnimalStatsPanelView.prefab`, вписать
   статический текст строк способностей.
3. Назначить `_totalLabel` на ADD ANIMAL, `_stateGraphics` + `UIGrayscale.mat` на FIGHT.
4. `UndoMergeButton` → `Button.transition = ColorTint`.
5. Раскладка HUD в `Canvas.prefab` по макету, спрайты `button_1`/`button_2`, 9-slice.
6. `CanvasScaler.match` 1 → 0.5.
7. Повесить `SafeArea` на корневой контейнер HUD.
8. Слово `LEVEL` — статическим TMP в префабе (`LevelView` теперь пишет только число).
9. Скрыть дублирующий объект `Restart` в сцене.

## Открытые вопросы

- **«Kickback» в коде отсутствует.** По числам похоже на `CounterAttack` (у ежа 50%) — нужно
  подтверждение владельца, прежде чем связывать строку макета с реальной способностью.
- **Череп на HP-баре** — украшение или маркер босса? ⚠️ **[2026-09-08] на итерации 1 иконка
  показывается ВСЕМ врагам**; переход на boss-only — правка одного условия, данные уже есть
  (`StageEnemyConfig.IsBoss` / `AnimalFacade.IsBoss`). Вопрос остаётся открытым.
- **Иконки лапы и сердца салатовые** — красный тинт даст бордовый вместо алого. ✅ **[2026-09-08]
  закрыто**: заведены белые плейсхолдеры `icon_attack_paw_white` / `icon_health_heart_white`,
  тинтуются нормально.
- **Расширение `IAbility`** ради реальных процентов способностей — вторая итерация.
  ⚠️ **[2026-09-08] актуально**: строки перков в префабе остались статическими, оба презентера
  по-прежнему шлют `null`.

## Затронутые файлы

**Изменены**
- `Assets/Code/Animals/UI/AnimalStatsPanelPresenter.cs`
- `Assets/Code/Animals/UI/AnimalStatsPanelView.cs`
- `Assets/Code/Animals/UI/IAnimalStatsPanelView.cs`
- `Assets/Code/Battle/PreBattle/AllySpawnPool.cs`
- `Assets/Code/Battle/PreBattle/AllySpawnService.cs`
- `Assets/Code/Battle/PreBattle/IAllySpawnPool.cs`
- `Assets/Code/Battle/PreBattle/IAllySpawnService.cs`
- `Assets/Code/Battle/UI/AddAnimalButtonView.cs`
- `Assets/Code/Battle/UI/BattleButtonView.cs`
- `Assets/Code/Battle/UI/PreBattleHudPresenter.cs`
- `Assets/Code/Framework/Code/UI/Elements/LevelView.cs`
- `Assets/Code/Framework/Sprites/button_1.png.meta`
- `Assets/Code/Framework/Sprites/button_2.png.meta`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`

**Созданы**
- `Assets/Code/Battle/UI/EnemyCardPresenter.cs`
- `Assets/Code/Battle/UI/EnemyCardView.cs`
- `Assets/Code/Battle/UI/IEnemyCardView.cs`
- `Assets/Code/Framework/Code/UI/Elements/SafeArea.cs`
- `Assets/UI_Sprites/Placeholder/{icon_lightning, icon_boss_skull, icon_undo, avatar_ring, hpbar_hatch}.png` (+ рукописные `.meta`)

## Связанное

- `Decisions/2026-08-01-animal-selection-and-stats-panel.md` — исходная карточка статов и
  `IAnimalSelectionService`
- `Decisions/2026-08-02-turn-order-column.md` — откуда взялся `UIGrayscale.mat` и почему
  сторона юнита определяется трекером, а не типом фасада
- `Decisions/2026-09-07-battle-camera-zoom.md` — `HealthBarCamera`, из-за которой зум сделан
  долли; там же общая причина «Unity MCP недоступен»
- `Decisions/2026-09-08-battle-hud-layout-editor-part.md` — **часть 2, редакторская**: закрывает
  эту запись, там же все ловушки Unity, вскрытые вёрсткой
