# Решение: попап информации о юните по макету — value-object `AnimalCardStats` вместо пары `(attack, health)`, статические SDF-шрифты Calibri, `pixelsPerUnitMultiplier` под арт макетного масштаба

**Дата:** 2026-09-26
**Статус:** принято, реализовано (код + шрифты + префаб `AnimalStatsPanelView.prefab`). Не закоммичено
на момент записи. Play Mode не прогонялся (общий редактор с параллельной VFX-сессией). Часть арта
временная (бейдж портрета), ждём @2x-экспорты от художника.

## Контекст

Макет попапа юнита (`AnimalStatsPanelView`) показывает больше, чем умел View:

- атака + **бонус к HP в процентах** («+N% HP», зелёная обводка), здоровье, **дальность хода**
  («x{n}» с иконкой следа `distance_icon`);
- разделитель между статами и способностями;
- две строки способностей разного стиля (крупная белая со свечением + мелкая курсивная лаймовая);
- бейдж портрета на левом краю, панель — спрайт `info_bar`;
- шрифты Calibri (Regular / Bold / Bold Italic) вместо `riffic`.

Что определило решение:

- `IAnimalStatsPanelView.Show(anchor, icon, attack, health, title, abilityLines)` и
  `UpdateStats(attack, health)` — каждое новое поле карточки расширяло бы две сигнатуры в двух
  интерфейсах (`IAnimalStatsPanelView`, `IEnemyCardView`) и в двух презентерах.
- `EnemyCardView : AnimalStatsPanelView` делит View с союзной карточкой, но `EnemyCardView.prefab`
  не переделывался — новые лейблы обязаны быть опциональными.
- `SetTilesPerMove` при UNDO MERGE не фаерит событие — обновление «x{n}» нельзя вешать на событие
  изменения дальности.
- Экспорты художника в `UI_Sprites/Buttons etc/` — в масштабе макета (553 px ширины при канвасе
  1080, ≈ ×1.95).

## Рассмотренные варианты

1. **Добавить параметры `tilesPerMove`, `bonusPercent` в `Show`/`UpdateStats`.** ❌ Отклонено:
   шесть позиционных `int` подряд — легко перепутать местами без ошибки компиляции, и каждое
   следующее поле макета снова ломает четыре файла.
2. **View сам читает `AnimalFacade`.** ❌ Отклонено: View получает зависимость на доменный фасад
   и `AnimalDatabase`, нарушается слоистость Presentation.
3. **Readonly struct `AnimalCardStats { Attack, Health, TilesPerMove, HealthBonusPercent }`.**
   ✅ Принято.
4. **Шрифты: динамические TMP-ассеты из TTF.** ❌ Отклонено: на мобильных динамический атлас
   тянет TTF в билд и дорисовывает глифы в рантайме. **Статические SDFAA** с фиксированным
   набором (ASCII + кириллица, 161 глиф) — ✅ принято.
5. **Перерезать арт `info_bar` под канвас 1080.** ❌ Отложено до @2x-экспортов художника;
   временно — `Image.pixelsPerUnitMultiplier ≈ 0.5`.

## Принятое решение

- **`Code/Animals/UI/AnimalCardStats.cs`** — readonly struct, передаётся в
  `Show(anchor, icon, AnimalCardStats, title, abilityLines)` и `UpdateStats(AnimalCardStats)` обоих
  интерфейсов (`IAnimalStatsPanelView`, `IEnemyCardView`).
- **`AnimalStatsPanelView`**: опциональные поля с null-guard — `_movesLabel` («x{TilesPerMove}»),
  `_bonusLabel` («+{n}% HP», скрыт при `<= 0`), `_abilitiesDivider` (виден, если видна хотя бы
  одна строка способности). Логика позиционирования не тронута. `EnemyCardView.prefab` работает
  без этих полей.
- **`AnimalStatsPanelPresenter.BuildStats`**: бонус =
  `RoundToInt((GetMaxHealth() / AnimalDatabase.GetStats(type).Health - 1) * 100)`; обновление на
  выбор, `HealthChanged` и `IMergeUndoService.OnStackCountChanged` (последний покрывает UNDO, где
  `SetTilesPerMove` события не даёт). **`EnemyCardPresenter`**: бонус всегда 0.
- **`AbilityLinesProvider`** оборачивает имя способности в `<i>…</i>` (на карточке врага `riffic`
  рендерит его фейковым италиком).
- **Шрифты** — `Assets/Code/Framework/Fonts/`: `Calibri SDF` (60 pt, padding 8), `Calibri Bold SDF`,
  `Calibri Bold Italic SDF` (56 pt, padding 12), атлас 1024², 161 глиф. В weight table `Calibri Bold
  SDF` италик-слоты 400/700 → `Calibri Bold Italic SDF` (тег `<i>` берёт настоящий курсив).
  Пресеты материалов: `Calibri Bold SDF - Bonus Outline.mat` (outline `#23A600`, width 0.3,
  dilate 0.3), `Calibri Bold SDF - Ability Glow.mat` (underlay `#B4CC10`, dilate 0.5,
  softness 0.9).
- **Спрайты**: `info_bar.png` → Sprite, 9-slice L16 B40 R16 T38; `distance_icon.png` → Sprite;
  плейсхолдер `UI_Sprites/Placeholder/portrait_badge.png`.
- **Префаб** `AnimalStatsPanelView.prefab` пересобран: корень 430×291, `_screenOffset (-250, 60)`,
  панель `info_bar` Sliced 283×291 c `pixelsPerUnitMultiplier 0.512`; `AttackRow`
  (HorizontalLayoutGroup: лапа + атака Calibri Bold 48 `#E2F53F` + бонус 40 с пресетом Outline),
  `HealthRow`, `MovesRow`, `Divider #474E41`, `AbilityLine0` (Calibri Bold 38, белая, Glow),
  `AbilityLine1` (24, италик, лайм), `PortraitBadge` 120 на левом краю, иконка головы тинтована
  `#ACBA3C`. `riffic` из этого префаба убран.

## Почему так

- Value-object делает следующее поле макета изменением одного struct и одного `BuildStats`,
  а не четырёх сигнатур; именованные поля исключают перестановку `int`-аргументов.
- Опциональные поля с null-guard позволяют делить `AnimalStatsPanelView` с `EnemyCardView`
  без переделки вражеского префаба.
- Бонус считается от **базового** HP из `AnimalDatabase`, а не хранится отдельно — единственный
  источник истины, мердж и UNDO отражаются автоматически.
- Подписка на `OnStackCountChanged` уже была (фича скиллов в карточке 2026-09-12) — «x{n}» после
  UNDO обновляется без нового события.
- Статические SDF-атласы: предсказуемая память и рендер на мобильных, глифы только нужные.

## Проверка

- ⚠️ **Play Mode не прогонялся** — редактор делился с параллельной VFX-сессией.
- Тесты: TESTS PAUSED, новых нет.
- ⚠️ Известные ограничения:
  - корень расширен под бонус, поэтому клампинг к краю канваса учитывает бонус, даже когда он скрыт;
  - отрицательный бонус HP (после мерджа курицы) скрывается, а не показывается;
  - `head_deer.png` — силуэт без рогов, не как на макете;
  - хелперы `Read*` продублированы в `AnimalStatsPanelPresenter` и `EnemyCardPresenter` — кандидат
    на вынос.
- 🔴 **Открытые вопросы владельцу**: сырые TTF (семейство Calibri + Impact) лежат в
  `TextMesh Pro/Resources/Fonts & Materials` → ~9 МБ уезжает в билд и нарушает правило про
  `Resources/`; лицензии (Calibri © Microsoft, Impact © Monotype).
- Недостающий арт: бейдж портрета, @2x `info_bar`/`distance_icon`, голова оленя с рогами.

## Затронутые файлы

- `Assets/Code/Animals/UI/AnimalCardStats.cs` (новый), `AbilityLinesProvider.cs`,
  `AnimalStatsPanelPresenter.cs`, `AnimalStatsPanelView.cs`, `IAnimalStatsPanelView.cs`
- `Assets/Code/Battle/UI/EnemyCardPresenter.cs`, `IEnemyCardView.cs`
- `Assets/Prefabs/AnimalStatsPanelView.prefab`
- `Assets/Code/Framework/Fonts/Calibri SDF.asset`, `Calibri Bold SDF.asset`,
  `Calibri Bold Italic SDF.asset`, `Calibri Bold SDF - Bonus Outline.mat`,
  `Calibri Bold SDF - Ability Glow.mat`
- `Assets/UI_Sprites/Buttons etc/info_bar.png.meta`, `distance_icon.png.meta`
- `Assets/UI_Sprites/Placeholder/portrait_badge.png`
