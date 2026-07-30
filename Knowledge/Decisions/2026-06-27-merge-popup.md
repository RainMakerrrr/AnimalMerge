# Решение — Merge popup показывается только при cross-type мердже

Дата: 2026-06-27

## Контекст
Нужна текстовая анимация (всплывающий текст плавающий вверх с затуханием), аналогичная `DamagePopupView`, информирующая игрока о полученном балансовом улучшении/скилле при мердже животных (например при мердже лисы — "Dodge Skill", слона — "Health Up"). Строки должны настраиваться per-animal.

## Решение
- **Ключевание по SOURCE**: popup ключуется по типу ПОТРЕБЛЁННОГО (source) животного, т.к. именно его `MergeSkill` применяется к target → "при мердже лисы → Dodge Skill". Событие `MergeTarget.Merged` передаёт source-facade.
- **Только cross-type мердж**: событие `Merged` гейтится флагом `grantedNewSkill` и фаерится ТОЛЬКО на cross-type ветке `ExecuteMergeDirectly` (где реально вызывается `animal.MergeSkill.Merge` и выдаётся новый скилл/апгрейд). При same-type мердже (одинаковые типы — только комбинируются HP/damage, новый скилл не выдаётся) popup НЕ показывается, чтобы не дезинформировать игрока (Fox+Fox больше не показывает "Dodge Skill"). Гарантии: ровно один раз на квалифицирующий мердж, никогда на failed/rejected мердж, никогда на undo (undo идёт через `MergeCommand`/`IMergeUndoService`, минуя `ExecuteMergeDirectly`).
- **Переиспользование Zenject**: `MergePopupController` инжектит существующий биндинг `AnimalDatabase` (`[Inject] Construct(AnimalDatabase)`) — без Singleton, новых биндингов/installer/сервисов не вводилось.
- **Per-prefab controller pattern**: зеркало `DamagePopupController` — подписка/отписка на `MergeTarget.Merged` в `OnEnable`/`OnDisable`, спавн popup над выжившим target цветом `GainColor` (зелёный) поверх overlay-canvas; пустая строка `MergeInfo` → popup не показывается.
- **Данные**: в `AnimalConfig` добавлено поле `[TextArea] string MergeInfo`; в `AnimalDatabase` — метод `string GetMergeInfo(AnimalType)` (зеркало `GetStats`; возвращает `string.Empty` + warning если тип не найден). `GetStats` не тронут.

## Затронутые файлы
- `Assets/Code/Animals/UI/MergePopupView.cs` (новый)
- `Assets/Code/Animals/UI/MergePopupController.cs` (новый)
- `Assets/Code/Animals/Merge/MergeTarget.cs` (добавлено событие `Merged`)
- `Assets/Code/Data/Animals/AnimalDatabase.cs` (поле `MergeInfo` + метод `GetMergeInfo`)

## Последствия
- Существующее событие `MergeTarget.Merge` (для `MergeView`) не тронуто — добавлено отдельное `Merged`.
- Ручная настройка в Unity Editor (Editor был офлайн): создать `MergePopupView.prefab` (дубль `DamagePopupView`), повесить `MergePopupController` на 5 player-префабов с MergeTarget (Cheetah, Elephant, Fox, Hedgehog, Chicken — НЕ Deer), назначить `_target`/`_popupPrefab`, заполнить `MergeInfo` per-animal в `AnimalDatabase.asset`.
- Live-проверка Unity Console на ошибки компиляции не выполнялась — нужна при следующем запуске Editor.

## Связанное
- [[2026-06-27]] — лог сессии
- [[Index]] — карта проекта
