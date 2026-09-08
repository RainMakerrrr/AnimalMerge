# Решение: боевой HUD по макету — итерация 1, часть 2 (редакторская), фича завершена

**Дата:** 2026-09-08
**Статус:** ✅ принято и реализовано полностью. Закрывает часть 1 от 2026-09-07
([[2026-09-07-battle-hud-layout-iteration-1]]) — фича «боевой HUD по макету, итерация 1»
**завершена целиком**, Play Mode рабочий, всё проверено в живом редакторе

---

## Контекст

Часть 1 (2026-09-07) дала C#-слой и плейсхолдер-арт, но редакторская работа была заблокирована
отключённым Unity MCP: `Assets/Prefabs/EnemyCardView.prefab` не существовал, а `NonLazy`-биндинг
`BindEnemyCard()` роняет контейнер на резолве — **Play Mode не запускался**.

Сегодня доступ к редактору восстановлен (в обход упавшего MCP-клиента, см. ниже), и часть 2
сделана целиком: собран HUD в `Assets/Resources/Canvas.prefab` и `Assets/Scenes/Main Scene.unity`
(контейнер `HUD` с `SafeArea`, плашка LEVEL слева вверху, RESTART справа вверху, UNDO MERGE слева
внизу, ADD ANIMAL внизу по центру, FIGHT справа внизу), доведены карточки
`AnimalStatsPanelView.prefab` и новый `Assets/Prefabs/EnemyCardView.prefab`, доведён
`Assets/Prefabs/HealthBar.prefab` (штриховка + иконка врага), скрыты отсутствующие на макете
`Taptic Button`, `Coins Count`, `Heal All Units` и дубль `Restart`.

Вёрстка по макету оказалась не «расставить прямоугольники»: **девять решений ниже — это не выбор
оформления, а обход поведения Unity, которое не даёт ни ошибки, ни предупреждения**. Они и
составляют ценность записи.

## Рассмотренные варианты

### 1. `CanvasScaler.match` при reference 1080×1920

- **`match = 1` (Height, было в проекте)** — на любом экране выше 16:9 канвас в reference-единицах
  оказывается **уже** 1080, нижний ряд HUD наезжает сам на себя.
- **`match = 0.5` (промежуточная неверная попытка)** — та же болезнь, слабее:
  `scaleFactor = (sw/1080)^(1-match) · (sh/1920)^match`, поэтому на 1080×2340 / 1170×2532 /
  1284×2778 ширина канваса выходит **978 юнитов вместо 1080**, и нижний ряд перекрывался на ~41 px
  с мёртвой зоной рейкаста.
- **`match = 0` (Width) — принято.** Канвас всегда ровно 1080 в ширину, меняется только высота.

### 2. Штриховка поверх Filled-заливки HP-бара

- **Спрайт прямо на `Fill`** — не годится: `Fill` обязан остаться `m_Type: 3` (Filled), иначе
  перестаёт работать `fillAmount`, а Filled **не тайлит** — тайл 32×32 растягивается на ~100×26,
  диагональ 45° сплющивается до ~8°.
- **Простой дочерний оверлей** — не годится: ребёнок Filled-картинки **не обрезается** по
  `fillAmount` и красит пустую часть бара.
- **`Mask` на `Fill` + `Fill/FillHatch` как Tiled-оверлей внутри — принято.** Filled генерирует меш
  только на заполненную долю, стенсил режет детей ровно по краю заливки.

### 3. Как карточка врага узнаёт сторону юнита

- **По типу фасада** — отвергнуто и **подтверждено на живом Play Mode**: все шесть врагов стадии 2
  репортят `ChickenFacade`, который наследуется от `PlayerAnimalFacade`.
- **Через `IUnitTracker` — принято.** Событие `EnemyUnitRegistered` добавлено в
  `IUnitTracker`/`UnitTracker`: реактивного аналога для вражеской стороны не существовало,
  `PlayerUnitsChanged` был только для союзников.

### 4. Спрайт `button_2.png` (оранжевая hex-кнопка FIGHT)

- **9-slice** — геометрически невозможен: измеренные скосы требуют вертикальных border `top ≥ 65` и
  `bottom ≥ 68`, суммарно 133 > 130 px высоты спрайта. Плюс спрайт содержит два отдельных
  декоративных штриха, не связанных с телом восьмиугольника (вверху слева и внизу справа) — при
  растяжении они выглядят мусором.
- **Стереть штрихи в PNG** — попробовано и **откачено**: они есть и на макете, править чужой арт из
  коммита `afaefa1e` неправильно. PNG сейчас байт-в-байт как в коммите.
- **`Image.type = Simple`, `spriteBorder {0,0,0,0}` — принято.** Штрихи остаются как задумано
  художником.

## Принятое решение

### Раскладка нижнего ряда — асимметричная, и это вынужденно

Ряд интринсически тесный: `110 + 462 + 352 + поля = 964` из 1080, на два зазора остаётся
**116 юнитов**. **Центрировать ADD ANIMAL невозможно** — итог `anchoredPosition.x = -121`, зазоры по
58.

🔴 **Несущая деталь**: `UiPulseAnimator` на FIGHT растит кнопку **на 8% влево от пивота (1,0)** в
активном состоянии. При `x = -73` это давало реальное перекрытие 18 юнитов даже на идеальном
1080×1920 — то есть баг проявлялся бы **только когда кнопка загорается**, и на статичном скриншоте
его не видно.

### `CanvasScaler.match = 0`

Reference 1080×1920, `m_MatchWidthOrHeight: 0` (Width). Прежнее значение `1` и промежуточное `0.5`
разобраны выше.

### Материальные эффекты и TMP

`BattleButtonView` фильтрует получателей материала:
`AcceptsMaterial(graphic) => (graphic is TMP_Text) == false`. Лейблы гасятся **цветом**, не
материалом.

### `Button.transition = None` там, где цвет красит код

Если код сам красит графику в disabled-цвет, у `Button` обязан стоять `transition = None`. Иначе
`Selectable` накладывает `ColorTint` сверху и цвета **перемножаются**. Наступили дважды — на FIGHT и
на ADD ANIMAL: получалось `~{0.16, 0.22, 0.09}`, почти чёрный вместо приглушённого лайма.

### `_notReadyColor = 0.72` — посчитан, а не подобран

Шейдер `UIGrayscale` при `_Saturation = 0` даёт `lerp(luminance, rgb, _Saturation)`; светимость
оранжевого тела `button_2` = **0.57**, поэтому «на глаз» взятые 0.55 сделали бы надпись невидимой.

### Компенсация масштаба владельца на HP-баре

`HealthBarView.ApplyFootprintSize()` делит `sizeDelta` на `movement.transform.localScale`, чтобы бар
держал постоянный **мировой** размер. Любой добавленный в бар элемент фиксированного размера обязан
получить ту же компенсацию `/ ownerScale` — иначе он отличается кратно масштабу животного (в проекте
от 0.35 у ежа до 1.0 у курицы). Отсюда сериализованный `_enemyIconSize`.

### Карточка врага не уворачивается от якоря

`AnimalStatsPanelView` — **screen-space** (единственный экземпляр в корне overlay-`Canvas`, проекция
`Camera.WorldToScreenPoint` в `LateUpdate`); единственный настоящий world-space UI в префабах
животных — `HealthBar.prefab`. Логика `ResolvePanelPosition` / `CoversAnchor` уводит панель от юнита;
карточке врага это не нужно — добавлен `protected virtual bool ShouldAvoidCoveringAnchor` с
оверрайдом `false` в `EnemyCardView`. `ClampInsideCanvas` работает в обеих ветках.

### `SafeArea` двигает не весь UI

`Level Counter` лежит внутри `Canvas/HUD` с `SafeArea`, а `TurnOrderView` — **прямой ребёнок
`Canvas`**. При вырезе плашка едет вниз, колонка стоит — отсюда наезд на 1170×2532, которого **не
видно без симуляции safe area**. `TurnOrderView.prefab` сдвинут на `m_AnchoredPosition (36, -320)`.

### Скрытие, а не удаление

`Taptic Button`, `Coins Count`, `Heal All Units` и дубль `Restart` — `SetActive(false)`, объекты
остались в префабе/сцене. На макете их нет, но это не повод терять функциональность.

## Почему так

- **Молчаливое поведение дороже неверного.** Девять ловушек из этой части не дают ни ошибки, ни
  варнинга: TMP игнорирует `Graphic.material`; `ColorTint` домножается поверх кода; ребёнок Filled
  не обрезается по `fillAmount`; `match = 0.5` даёт 978 юнитов вместо 1080; перекрытие FIGHT видно
  только в активном состоянии; наезд `TurnOrderView` — только с симуляцией выреза; масштаб
  владельца ломает размер иконки только на мелких животных; 9-slice с невозможными border'ами
  просто рисует мусор; `unit is EnemyAnimalFacade` врёт на всех шести врагах стадии 2.
  **Каждая из них проходит код-ревью и статичный скриншот.**
- **Числа считаются, а не подбираются.** `_notReadyColor` выведен из формулы шейдера и измеренной
  светимости спрайта; `spriteBorder` отвергнут измерением скосов против высоты текстуры; зазоры
  нижнего ряда — из суммы ширин; `_enemyIconSize` — из деления на `ownerScale`.
- **Чужой арт не правим.** Первая попытка стереть декоративные штрихи в `button_2.png` была ошибкой
  и откачена; вместо этого `Image.type = Simple`.
- **Сторона юнита — runtime-принадлежность.** Продолжение решения от 2026-08-02, теперь
  **подтверждённое на живом Play Mode** и подкреплённое новым событием `EnemyUnitRegistered`.

## Проверка

- ✅ **Живой Play Mode** — фича отсмотрена целиком, ранее падавший `NonLazy`-биндинг
  `BindEnemyCard()` резолвится (`Assets/Prefabs/EnemyCardView.prefab` создан и назначен в
  `Main Scene`). Прежняя запись «Play Mode сломан» **недействительна**.
- ✅ Целевые наборы: EditMode `AllySpawnPoolTests` / `AllySpawnServiceTests` /
  `BattleReadinessServiceTests` — **20/20**; PlayMode `BattleButtonViewTests` /
  `AddAnimalButtonViewTests` — **7/7**.
- 🔴 **Предсуществующие падающие тесты, к задаче не относятся, ничего не отключалось и не
  чинилось**: 4 в `HealthBarViewTests` (проверено откатом `HealthBarView.cs` на HEAD — те же 4
  падения), плюс падения в `AnimalAttackPostAbilityTests`, `RetreatAbilityTests`,
  `TargetPositionCalculatorTests`.
- ✅ `button_1.png` / `button_2.png` — байт-в-байт как в коммите `afaefa1e`, изменены только `.meta`.
- Действует **TESTS PAUSED** — новых тестов не писалось.

### Инструментальное: мост к Unity в обход упавшего MCP-клиента

Сервер `mcp-for-unity-server` живёт на `http://127.0.0.1:8080/mcp` и говорит по HTTP JSON-RPC
(`initialize` → `Mcp-Session-Id` → `notifications/initialized` → `tools/call`) — все 48 инструментов
доступны через `curl`. Оговорки, стоившие времени:

- `execute_code` требует `"action":"execute"` и **обязательный `return` в конце** (иначе «not all
  code paths return a value»); компилируется как **C# 6**.
- у `run_tests` фильтрация через `group_names`, а **не** `test_filter`.
- `manage_script` в этой сборке **без** `validate`.
- 🔴 **Прогон PlayMode-тестов через MCP выставляет
  `ProjectSettings/EditorSettings.asset → m_EnterPlayModeOptionsEnabled: 1`.** Возвращать надо через
  `UnityEditor.EditorSettings.enterPlayModeOptionsEnabled = false` + `AssetDatabase.SaveAssets()`, а
  **не правкой файла** — иначе Unity перезапишет из памяти.

## Открытые вопросы

- **«Kickback» в коде отсутствует**; по числам похоже на `CounterAttack` (у ежа 50%). Строки перков
  пока **статические в префабе**, презентеры шлют `null` (контракт: `null` = не трогать лейблы,
  пустой список = погасить). Реальные проценты требуют расширения `IAbility` — вторая итерация.
- **Иконка на HP-баре показывается всем врагам, а не только боссам.** Переход на boss-only — правка
  одного условия, `AnimalFacade.IsBoss` и `StageEnemyConfig.IsBoss` существуют.
- **Из UI пропал тумблер вибрации** — `Taptic Button` скрыт как отсутствующий на макете.
- **Колонка очередности ходов (`TurnOrderView`) на макете отсутствует**, но оставлена как живая
  функциональность — только сдвинута.
- **Весь арт в `Assets/UI_Sprites/Placeholder/` временный**: `icon_lightning`, `icon_boss_skull`,
  `icon_undo`, `avatar_ring`, `hpbar_hatch`, `icon_attack_paw_white`, `icon_health_heart_white`.

## Затронутые файлы

**Изменён код**
- `Assets/Code/Animals/Health/HealthBarView.cs`
- `Assets/Code/Animals/UI/AnimalStatsPanelView.cs` · `AnimalStatsPanelPresenter.cs` ·
  `IAnimalStatsPanelView.cs`
- `Assets/Code/Battle/PreBattle/IAllySpawnPool.cs` · `AllySpawnPool.cs` · `IAllySpawnService.cs` ·
  `AllySpawnService.cs`
- `Assets/Code/Battle/Services/IUnitTracker.cs` · `UnitTracker.cs` (новое событие
  `EnemyUnitRegistered`)
- `Assets/Code/Battle/UI/AddAnimalButtonView.cs` · `BattleButtonView.cs` · `PreBattleHudPresenter.cs`
- `Assets/Code/Framework/Code/UI/Elements/LevelView.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`

**Создан код**
- `Assets/Code/Battle/UI/IEnemyCardView.cs` · `EnemyCardView.cs` · `EnemyCardPresenter.cs`
- `Assets/Code/Framework/Code/UI/Elements/SafeArea.cs`

**Ассеты**
- `Assets/Resources/Canvas.prefab` · `Assets/Scenes/Main Scene.unity`
- `Assets/Prefabs/AnimalStatsPanelView.prefab` · `HealthBar.prefab` · `TurnOrderView.prefab`
- `Assets/Prefabs/EnemyCardView.prefab` (новый)
- `Assets/Code/Framework/Sprites/button_1.png.meta` · `button_2.png.meta` (только `.meta`)
- `Assets/UI_Sprites/Placeholder/*` (7 png)

> [!warning] Зависимость при коммите
> `EnemyCardPresenter` подписан на `BattleEndedSignal` из параллельной задачи про боевую камеру, а
> `BattleInstaller.cs` несёт правки обеих задач. **По отдельности не компилируются — коммитить
> только вместе.**

## Связанное

- [[2026-09-07-battle-hud-layout-iteration-1]] — часть 1, C#-слой и контракты
- [[2026-09-07-battle-camera-zoom]] — параллельная задача, откуда пришёл `BattleEndedSignal`
- [[2026-08-02-turn-order-column]] — исходное решение «сторона юнита определяется трекером»
- [[2026-08-01-animal-selection-and-stats-panel]] — исходная карточка статов
