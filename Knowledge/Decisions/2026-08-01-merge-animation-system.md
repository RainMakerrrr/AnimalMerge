# Решение: общая система анимаций при мердже — арбитр скейла на цели + переиспользуемая appear-анимация

**Дата:** 2026-08-01
**Статус:** принято, реализовано

## Контекст
После glow-свипа гепарда ([[2026-08-01-merge-sweep-glow]]) визуальные merge-атрибуты остались разнородными: гепард получил анимированный свип, слон менял масштаб мгновенно, слот-детали (хвост/рога) просто включались через `SetActive(true)`, клон курицы появлялся мгновенно. Нужна общая система: слон растёт скейл-анимацией с настраиваемыми параметрами, слот-деталь появляется со scale-in параллельно с glow-пассом, клон курицы получает ту же appear-анимацию, окрас гепарда визуально не меняется.

Триггер миграции тюнинга в ScriptableObject, зафиксированный в [[2026-08-01-merge-sweep-glow]] («как только второй merge attribute захочет тот же свип»), сработал буквально — поэтому конфиг вынесен сразу.

Ограничения, найденные разведкой (не предположенные):
- **Владельца merge-атрибута деактивируют через кадр после `Apply`** (`MergeTarget.cs:113` → `:124`) — ловушка из прошлой фичи действует и здесь.
- **`MergeView.OnMerge` применяет несколько накопленных атрибутов в ОДНОМ кадре** (`AppliedVisualAttributes` в порядке `[attrB, attrA]`) — два `ElephantMergeAttribute` дерутся за один трансформ, два оверлея могут делить один `Renderer`.
- **`UNITASK_DOTWEEN_SUPPORT` в проекте нет** — ждать твин можно только поллингом ([[2026-08-01-tutorial-hand-merge-hint]]).
- Единственный логический потребитель `localScale` — `MergeStateSnapshot`. Занятость сетки берётся из `AnimalMovement._unitSize`, multi-size pathfinding — из `UnitSize`/`Direction`, радиус атаки — из статов.

## Рассмотренные варианты
1. **Хранить pre-merge скейл в самом атрибуте** (как было). Отпадает: цепочка Elephant A → B → C давала два атрибута, каждый писал `_scaleBeforeApply` из живого трансформа → undo возвращал `1.5S` вместо `S`. Плюс владелец живёт на деактивируемом источнике.
2. **DOTween-твины на скейл.** Отпадает: без `UNITASK_DOTWEEN_SUPPORT` ждать пришлось бы поллингом, а обе половины параллельной композиции (скейл и glow) должны тикать на одном тайминге — `MergeSweepGlow.PlayPassAsync` уже аккумуляторный цикл на `UniTask.Yield`.
3. **Общий базовый класс/фреймворк анимаций.** Отпадает по KISS — задача покрывается пятью маленькими независимыми кусками.
4. **Арбитр скейла как MonoBehaviour на ЦЕЛИ + композиция «glow ‖ scale-in» + тюнинг в ScriptableObject** (выбрано).

## Принятое решение

**Новые типы — `Assets/Code/Animals/Vfx/`** (пять маленьких кусков, без фреймворка):
- `Config/MergeAnimationConfig.cs` — ScriptableObject со всеми тюнингами (секции Elephant Growth / Appear / Glow), `[CreateAssetMenu]` → «Game/Merge Animation Config». `OnValidate` клампит `_glowBandsPerBody ≤ 1/(1+2·_glowBandMargin)` — это закрыло открытый SUGGESTION из ревью свипа.
- `MergeScaleTween.cs` — статик, один lerp скейла на `UniTask.Yield(PlayerLoopTiming.Update, ct)` + `AnimationCurve`, через `Mathf.LerpUnclamped` (чтобы кривая с перелётом работала).
- `RendererMaterialOverlay.cs` — материальная логика гепарда, поднятая дословно и обобщённая одним параметром `Material skinOverride` (`null` = слот 0 не трогаем). Только `sharedMaterials`, фильтр по идентичности glow-шейдера сохранён.
- `MergeAppearAnimation.cs` — композиция «glow ‖ scale-in». `Begin()` синхронно схлопывает скейл и вешает оверлей ДО первого `await`; `Dispose()` идемпотентен.
- `MergeScaleAnimator.cs` — MonoBehaviour-арбитр рутового скейла, добавляется в рантайме на ЦЕЛЬ через `GetComponent ?? AddComponent`. Держит `_logicalScale`, умножает/делит его.

**Ассет и биндинг.** `Assets/Resources/MergeAnimationConfig.asset`, биндится в `CurrentGameInstaller` через `FromScriptableObjectResource` (прецедент `AnimalDatabase`). Доставка — метод-инъекция в `VisualMergeAttribute`: `[Inject] ConstructVisual([InjectOptional] MergeAnimationConfig)` → `protected AnimationConfig`.

**Изменённые:** `ElephantMergeAttribute` (скейл через аниматор, БЕЗ glow), `SlotMergeAttribute` (appear-анимация + `StopAppearAnimation()` на Undo и `OnDestroy`), `MergeVisualSlot` (`AuthoredScale`), `CheetahMergeAttribute` (внутренности на `RendererMaterialOverlay`, тюнинг остался на компоненте и не переименован), `ChickenMergeSkill` + `ChickenFacade` (анимация клона, токен из `clone.GetCancellationTokenOnDestroy()`), `MergeSweepGlow` (`: IDisposable` — закрыт SUGGESTION прошлой фичи), `MergeStateSnapshot` (читает `animal.ScaleAnimator`), `AnimalFacade` (ленивое свойство `ScaleAnimator`), `CurrentGameInstaller` (+1 биндинг), `Deer.prefab` (`SlotMergeAttribute._type: 0 → 2`).

## Почему так

- **Арбитр скейла живёт на ЦЕЛИ, а не в атрибуте.** `MergeScaleAnimator` добавляется в рантайме на корень цели и владеет `_logicalScale`. Решает три проблемы одним ходом:
  1. владелец живёт на цели, которая остаётся активной, поэтому ловушка деактивации источника вообще не действует;
  2. два `ElephantMergeAttribute` в одном кадре не дерутся за трансформ — второй домножает логический скейл;
  3. undo имеет одно место отмены и становится порядко-независимым.
  Правок префабов не требует.
- **Побочно исправлен предсуществующий баг undo слона.** Цепочка Elephant A → B → C: `AppliedVisualAttributes = [attrB, attrA]`, каждый писал `_scaleBeforeApply` из живого трансформа, поэтому undo возвращал `1.5S` вместо `S`. Схема «умножение/деление логического скейла» чинит это структурно. Проверено побитово: `afterUndo == original`, включая случай двух атрибутов в одном кадре.
- **Логическое значение переключается синхронно, интерполируется только трансформ.** Проверено, что рост слона чисто косметический: занятость сетки берётся из `AnimalMovement._unitSize`, multi-size pathfinding — из `UnitSize`/`Direction`, радиус атаки — из статов (`AnimalAttack`, `ChickenAttack`, `TargetFinder`). Единственный логический потребитель `localScale` — `MergeStateSnapshot`. Undo — мгновенный снап, без анимации.
- **[WARNING из ревью, исправлено] Два `RendererMaterialOverlay` могут владеть одним `Renderer.sharedMaterials`.** Реальный сценарий: слот активируется первым в проходе `MergeView.OnMerge`, после чего `CollectPaintableRenderers` гепарда подхватывает уже активный рендерер слота — и `Dispose()` слотового оверлея затирал гепардовый скин своими «оригиналами». Исправлено так: `Apply` запоминает, что именно он записал (`RendererState.AppliedSharedMaterials`) и какой glow-инстанс внедрил; `RestoreOriginalMaterials` восстанавливает оригиналы ТОЛЬКО если рендерер всё ещё держит ровно то, что этот оверлей писал (по ссылке), иначе снимает лишь свой собственный glow по `ReferenceEquals`. Порядок в `Dispose()` перевёрнут на restore-then-`DisposeGlow`, чтобы glow-инстанс был жив и опознаваем во время восстановления.
  > **Правило на будущее:** оверлей, который может делить рендерер с другим писателем, обязан восстанавливать состояние **условно** — по сверке с тем, что он сам записал, а не безусловно из снапшота. Это развитие правила из [[2026-08-01-merge-sweep-glow]] («рантайм-оверлей нельзя снапшотить как оригинал»).
- **Захват `AuthoredScale` до активации.** `SlotMergeAttribute.Apply` читает `_slot.AuthoredScale` ДО `SetActive(true)`, поэтому ленивый геттер снимает авторское значение, пока объект ещё неактивен, а `Awake` затем ничего не делает. Цикл undo → повторный мердж переиспользует закешированное значение и не может увидеть схлопнутый масштаб.
- **DOTween сознательно не используется.** `UNITASK_DOTWEEN_SUPPORT` нет в дефайнах, ждать твин пришлось бы поллингом; обе половины параллельной композиции (скейл и glow) должны тикать на одном тайминге — `MergeSweepGlow.PlayPassAsync` уже аккумуляторный цикл на `UniTask.Yield`.
- **Все потребители null-гардят конфиг** → отсутствие биндинга деградирует к старому мгновенному поведению. Правок сцены и префабов для доставки конфига не нужно.
- **`[InjectOptional]` метод-инъекция достаёт до неактивных детей префаба**, потому что `AnimalFactory.Create` использует `_container.InstantiatePrefabForComponent`. Проверено: `configInjected = True` на Fox / Deer / Cheetah / Elephant.
- **Кривые с перелётом — это данные, а не код.** Первая версия ассета уехала с плоским smoothstep, и запрошенное «с перелётом» движение не воспроизводилось, хотя код (`LerpUnclamped` + `AnimationCurve`) его поддерживал. Итоговые кривые — 3-ключевой back-out `(0,0,out 2.2) → (0.65,1.12) → (1,1)`; пик 1.56× для слона, 1.114× для появляющейся детали.
  > **Урок:** при вынесении тюнинга в ScriptableObject дефолты ассета — часть фичи, а не мелочь; их надо проверять отдельно от кода.
- **`_appearStartScale` = 0.05, а не 0.** Нулевой стартовый скейл схлопывал коллайдеры клона курицы, и свежий клон был не-raycastable (то есть невыбираемым для следующего мерджа) все 0.35 с — регрессия относительно прежнего мгновенного появления. Шейдер к нулевому AABB устойчив (`max(_SweepAxisExtent, 1e-4)`), так что малый ненулевой старт ничего не стоит визуально.
- **Исправлен предсуществующий баг данных `Deer.prefab`:** у `SlotMergeAttribute` стоял `_type: 0 (Elephant)`, а слот «Antlers 02» ждёт `_sourceType: 2 (Deer)` — рога не появлялись никогда, мердж писал warning. Теперь `_type: 2`.

## Проверка
- Unity Console: новых ошибок нет, компиляция чистая.
- Тесты `Code.Tests.EditorTests.MergeSystem` + `BattleSystem` — 70/70 зелёные. Новых тестов не писали (**TESTS PAUSED** + требование пользователя).
- Undo слона проверен побитово: `afterUndo == original`, включая случай двух атрибутов в одном кадре.
- Фикс коллизии оверлеев перепроверен на РЕАЛЬНОМ пути в `EditorSceneManager.NewPreviewScene()` на настоящих `Cheetah.prefab` + `Deer.prefab` через настоящие `SlotMergeAttribute.Apply` / `CheetahMergeAttribute.Apply`: подтверждено, что гепард действительно подхватывает 3 рендерера включая только что активированные рога, и что после `Dispose()` слотовой анимации рога сохраняют `Cheetah Spots` (до фикса откатывались на `Antlers`).
- Гепард отдельно прогнан по полному циклу — записи и их порядок идентичны прежним, сериализованные тайминги (0.6 / 0.05 / 0.5 / 0.5) не тронуты.
- Доставка конфига проверена: `configInjected = True` на Fox / Deer / Cheetah / Elephant.

## Долг (SUGGESTION из ревью, НЕ применены)
- `MergeScaleAnimator._logicalScale` дрейфует и не ресинхронизируется с трансформом — нужен `Resync(Vector3)` или передача авторитетного pre-merge скейла в `UndoGrowBy`.
- `GrowScaleMultiplier` — балансная величина конкретно слона, но лежит в общем конфиге; по конвенции проекта ([[2026-06-27-fox-dodge-stats]]) ей место в `ElephantStats : AnimalStats`. Плюс `FallbackScaleMultiplier = 1.5f` в коде дублирует дефолт ассета и может тихо разъехаться.
- `SlotMergeAttribute.Apply` рано выходит ДО `StopAppearAnimation()`, если на новой цели нет подходящего слота.
- `MergeScaleAnimator._cancellation` не диспозится на happy path.
- `MergeAppearAnimation.Begin` разыменовывает `_config` без null-гарда (оба вызывающих проверяют снаружи).
- `OnValidate` клампит `_glowBandsPerBody` разрушительно — поднятие margin навсегда занижает bands.
- Три новых `GetComponent`/`AddComponent` вне `Awake` (`ElephantMergeAttribute.Apply`, `MergeStateSnapshot.Capture`) — частично закешированы, первый вызов на цель принципиально неустраним: цель неизвестна до `Apply`.

## Затронутые файлы
Новые:
- `Assets/Code/Animals/Vfx/Config/MergeAnimationConfig.cs` (+ `.meta`, `Config.meta`)
- `Assets/Code/Animals/Vfx/MergeScaleTween.cs` (+ `.meta`)
- `Assets/Code/Animals/Vfx/RendererMaterialOverlay.cs` (+ `.meta`)
- `Assets/Code/Animals/Vfx/MergeAppearAnimation.cs` (+ `.meta`)
- `Assets/Code/Animals/Vfx/MergeScaleAnimator.cs` (+ `.meta`)
- `Assets/Resources/MergeAnimationConfig.asset` (+ `.meta`) — **untracked**, значения кривых, `_appearStartScale` и ссылка `_glowMaterial` уедут только если его закоммитить

Изменены:
- `Assets/Code/Animals/Merge/MergeAttributes/VisualMergeAttribute.cs`
- `Assets/Code/Animals/Merge/MergeAttributes/ElephantMergeAttribute.cs`
- `Assets/Code/Animals/Merge/MergeAttributes/SlotMergeAttribute.cs`
- `Assets/Code/Animals/Merge/MergeAttributes/MergeVisualSlot.cs`
- `Assets/Code/Animals/Merge/MergeAttributes/CheetahMergeAttribute.cs`
- `Assets/Code/Animals/Merge/MergeSkills/IMergeSkill.cs`
- `Assets/Code/Animals/Merge/Commands/MergeStateSnapshot.cs`
- `Assets/Code/Animals/Facades/AnimalFacade.cs`
- `Assets/Code/Animals/Facades/ChickenFacade.cs`
- `Assets/Code/Animals/Vfx/MergeSweepGlow.cs`
- `Assets/Code/Infrastructure/Installers/CurrentGameInstaller.cs`
- `Assets/Resources/Prefabs/Animals/Deer.prefab`

НЕ тронуты: `MergeView`, `MergeCommand`, `BattleStateMachine`, `Assets/Scenes/Main Scene.unity`.

## Связанное
- [[2026-08-01-merge-sweep-glow]] — glow-свип гепарда, от которого отталкивается вся система; здесь закрыты два его SUGGESTION (`IDisposable`, `OnValidate` на bands)
- [[2026-07-31-iridescent-sweep-shader]] — базовый шейдер
- [[2026-08-01]] — сессия, в которой сделана система
