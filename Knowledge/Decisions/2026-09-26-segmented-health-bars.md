# Решение: HP-бары по макету — сегментированная раскладка через Strategy, плавная анимация отображаемого значения, низкое HP по отображаемому значению

**Дата:** 2026-09-26
**Статус:** принято, реализовано (код + префабы + конфиг + сцена + editor-тулзы). Не закоммичено на
момент записи. Часть арта временная, ждёт художника.

## Контекст

Макет требует два разных HP-бара:

- **союзник** — вертикальный сегментированный бар слева от юнита, рамка со скосом;
- **враг** — горизонтальный сегментированный бар с черепом.

Плюс плавное изменение HP вместо мгновенного прыжка и мигание/покраснение при низком HP.

Что определило решение:

- Старый `HealthBarView` висел на всех ~12 префабах животных и врагов через вложенный
  `HealthBar.prefab`; смена скрипта или его GUID означала бы перепривязку всех префабов.
- Старая штриховка держалась на `Mask` + `Filled` + Tiled-оверлее — в сегментированный бар она
  не переносится, а два бара различаются только геометрией раскладки, не поведением.
- Все фиксированные размеры в баре обязаны делиться на скейл владельца (животные от 0.35 до 1.0),
  иначе мировой размер бара «плывёт».
- `HealthBarSetupTool` и `AnimalPrefabAssembler` независимо считали позицию бара — два источника
  истины для одного числа.

## Рассмотренные варианты

1. **Два отдельных View-скрипта (`AllyHealthBarView` / `EnemyHealthBarView`).** ❌ Отклонено:
   дублирование логики анимации/мигания, новый GUID — перепривязка всех префабов.
2. **Один `HealthBarView` с `if (orientation)` внутри.** ❌ Отклонено: геометрия двух баров
   (скос рамки у союзника, иконка у врага) разрастается в ветвления.
3. **Один `HealthBarView` (имя и GUID сохранены) + раскладка через Strategy
   `HealthBarLayout` → `HorizontalHealthBarLayout` / `VerticalHealthBarLayout`.** ✅ Принято.

По анимации:

- **awaited DOTween (`AsyncWaitForCompletion`/UniTask)** ❌ — никому не нужно ждать окончания
  твина, лишняя отмена при уничтожении юнита.
- **fire-and-forget твин с `SetLink(gameObject)` + `Kill` и рестарт от текущего значения** ✅.

По порогу низкого HP:

- **по целевому значению** ❌ — бар краснеет и мигает до того, как заливка доехала до порога.
- **по отображаемому (анимируемому) значению** ✅ — визуально согласовано с заливкой.

## Принятое решение

- `HealthBarView` твинит отображаемое значение через `DOTween.To` (Kill + рестарт от текущего);
  `OnEnable` ставит значение мгновенно (нет «заполнения» при спавне); на `Died` — твин до 0,
  затем `SetActive(false)`. Зависимость от `IUnitTracker` убрана — череп статичен в
  `EnemyHealthBar.prefab`.
- Низкое HP: `displayed > 0 && displayed <= threshold` → красные сегменты + свечение рамки +
  мигание (`HealthBarBlinker`, `CanvasGroup.DOFade` Yoyo-луп).
- `SegmentedHealthBar` клонирует шаблон сегмента, раскладывает анкорами, для рамки союзника
  интерполирует скос `bottomSpan → topSpan`; заливка сегмента `i` = `Clamp01(v·N − i)`.
- Раскладка — `HealthBarLayout` (Strategy): делит размер на скейл владельца и возвращает число
  сегментов.
- Параметры — `HealthBarConfig` (SO, `Assets/Settings/BattleConfigs/HealthBarConfig.asset`:
  0.35 с, `OutCubic`, порог 0.3, полупериод мигания 0.3, мин. альфа 0.35), биндинг
  `BattleInstaller.BindHealthBar()`.
- `HealthBar.prefab` → `EnemyHealthBar.prefab` (GUID сохранён) + новый `AllyHealthBar.prefab`,
  корень в обоих — `HealthBar`. Бар союзника смещается вбок `BillboardRotator._lateralOffset`
  по `camera.right`.
- Позиция бара союзника — `DerivedGeometry.HealthBarLateralOffset` из bounds меша, общая для
  `HealthBarSetupTool` и `AnimalPrefabAssembler`.

> [!warning] Временный арт
> `boss_hp` — сегмент союзника при низком HP; `UIGrayscale.mat` — пустые сегменты врага;
> `UI_Sprites/UISilhouette.mat` — красное свечение рамки союзника (рамка почти чёрная, обычный
> тинт цвета её не красит). Заменить, когда придёт арт от художника.

## Почему так

- Сохранённые имя и GUID `HealthBarView` — ноль перепривязок в ~12 префабах животных/врагов.
- Strategy изолирует единственное реальное различие двух баров — геометрию; анимация, мигание и
  подписка на здоровье живут в одном месте.
- Порог по отображаемому значению держит цвет и заливку синхронными при любой длительности твина.
- Единый `DerivedGeometry.HealthBarLateralOffset` убирает расхождение между тулзой донастройки и
  генератором префабов.

## Проверка

- EditMode `HealthBarViewTests` — **5/5** зелёные (фикстура адаптирована; прежние 4 падения
  закрыты).
- 🔴 Ловушка: в EditMode после `AddComponent` у обычного `MonoBehaviour` **не вызываются**
  `Awake`/`OnEnable`/`OnDisable` — фикстура зовёт их рефлексией.
- Известное: бары врагов перекрываются, когда враги стоят вплотную; T-Rex и Deer в Play Mode не
  отсмотрены.

## Затронутые файлы

- `Assets/Code/Animals/Health/HealthBarView.cs`, `HealthBarBlinker.cs`, `HealthBarConfig.cs`,
  `HealthBarLayout.cs`, `HealthBarOrientation.cs`, `HealthBarSegment.cs`,
  `HorizontalHealthBarLayout.cs`, `SegmentedHealthBar.cs`, `VerticalHealthBarLayout.cs`
- `Assets/Code/Animals/UI/BillboardRotator.cs`
- `Assets/Code/Infrastructure/Installers/BattleInstaller.cs`
- `Assets/Code/Editor/HealthBarSetupTool.cs`
- `Assets/Code/Editor/AnimalPrefabBuilder/AnimalPrefabAssembler.cs`, `AnimalPrefabConstants.cs`,
  `AnimalPrefabPaths.cs`, `AnimalSideProfile.cs`, `DerivedGeometry.cs`
- `Assets/Code/Tests/EditorTests/UI/HealthBarViewTests.cs`
- `Assets/Prefabs/AllyHealthBar.prefab`, `Assets/Prefabs/EnemyHealthBar.prefab`
- `Assets/Resources/Prefabs/Animals/*.prefab`, `Assets/Resources/Prefabs/Enemies/*.prefab`
- `Assets/Scenes/Main Scene.unity`
- `Assets/Settings/BattleConfigs/HealthBarConfig.asset`
- `Assets/UI_Sprites/UISilhouette.mat`, `Assets/UI_Sprites/Buttons etc/*.png.meta` (6 HP-спрайтов)
