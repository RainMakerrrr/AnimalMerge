# Решение: glow-свип при мердже Cheetah — world-режим шейдера + async-фасад поверх синхронного Apply/Undo

**Дата:** 2026-08-01
**Статус:** принято, реализовано

## Контекст
Смена скина при мердже Cheetah была мгновенной подменой материала (`renderer.material = _cheetahMaterial`). Нужно: glow-волна проезжает по **всему** животному → под ней накладывается материал с текстурой гепарда → волна уезжает обратно → эффект полностью убирается, в слоте материала не остаётся мусора.

Ограничения, найденные разведкой (не предположенные):
- `MergeTarget.ExecuteMergeDirectly` дёргает `Merge?.Invoke(...)` (`MergeTarget.cs:113`) и **строкой ниже** гасит исходное животное `animal.gameObject.SetActive(false)` (`:124`). То есть компонент, ВЛАДЕЮЩИЙ `CheetahMergeAttribute`, выключается через миллисекунды после `Apply`.
- `VisualMergeAttribute.Apply/Undo` синхронные `void`, а `MergeCommand.Execute()` возвращает `bool` — асинхронность в контракт не пролезает без правки 7 файлов.
- Существующий `IridescentSweep.shader` общий с loot-shine на хвосте гепарда — ломать его нельзя.

## Рассмотренные варианты
1. **Coroutine на владельце.** Отпадает сразу: владельца деактивируют через кадр → корутина умирает с исключением. Плюс запрет проекта на корутины в новом коде.
2. **`ApplyAsync` в базовом `VisualMergeAttribute` + await по цепочке** (`MergeView` → `MergeCommand` → `MergeStateSnapshot` → `AnimalFacade` → `BattleStateMachine`). Честно, но blast radius 7 файлов и слом синхронного контракта `MergeCommand.Execute() : bool` ради одного визуального атрибута.
3. **Object-space развёртка свипа** (существующий `_WorldSweepBlend = 0`). Непригодна для целого животного: у каждого рендерера свой пивот/поворот/масштаб, полоса легла бы в разных местах на разных частях тела.
4. **Sync-фасад + `.Forget()` + явный world-режим в шейдере** (выбрано).

## Принятое решение
**Шейдер.** В `Assets/Shaders/IridescentSweep.shader` добавлен явный world-режим: свойства `_SweepWorldAxis` (компонента `w` = флаг включения) и `_SweepWorldOrigin`, `_PhaseOffset` расширен с `Range(0,1)` до `Float` (C# должен уметь вывести полосу за пределы тела). В `vert`:
```
explicitSweep = dot(worldPos - _SweepWorldOrigin.xyz, normalize(_SweepWorldAxis.xyz)) / max(_SweepAxisExtent, 1e-4)
sweepCoord    = lerp(<легаси>, explicitSweep, step(0.5, _SweepWorldAxis.w))
```
Обратная совместимость: при `w = 0` математика бит-в-бит прежняя; существующий пресет `IridescentSweepOverlay.mat` (loot-shine) `_SweepWorldAxis` не сериализует → берёт дефолт `(0,1,0,0)` → легаси-ветка.

**Драйвер.** `Assets/Code/Animals/Vfx/MergeSweepGlow.cs` — обычный C#-класс (НЕ MonoBehaviour), namespace `Code.Animals.Vfx`, рядом с `DeathSpiritView`. Владеет одним рантайм-инстансом материала, общим для всех рендереров цели. Каждый кадр пересчитывает объединённый world-AABB рендереров → `extent` и `origin`, поэтому полоса покрывает произвольное животное без захардкоженного размера. Фазу гонит аккумуляторным циклом на `UniTask.Yield(PlayerLoopTiming.Update, ct)`. Property-id кэшируются через `Shader.PropertyToID` (прецедент `DeathSpiritView`, `GridCell`). Границы фазы `pStart = 0.5 + T*m`, `pEnd = 0.5 - T*(1+m)` при `T = _bandsPerBody = 0.5`, `m = _bandMargin = 0.5` → `0.75` и `-0.25`; реверс = обмен концов местами.

**Материал.** `Assets/Code/Animals/Materials/MergeSweepGlow.mat` (GUID `5e87d2afac6f4407a3d961d3326b6044`): `_SweepSpeed 0` (анимация больше не самоходная — фазу гонит C#), `_SweepTiling 0.5`, `_SweepWorldAxis (0,1,0,1)`, `_SweepAxisExtent 1` (перезаписывается в рантайме). Вид унаследован от loot-shine пресета: `_SweepIntensity 5`, `_SurfaceWeight 1.3`, `_RimWeight 2.5`, `_GlowSoftClip 0.5`, `_SweepWidth 0.22`.

**Атрибут.** `CheetahMergeAttribute` переписан, но `Apply`/`Undo` сохранили синхронные `void`-сигнатуры — последовательность запускается через `.Forget()`. Новые `[SerializeField]`: `_glowMaterial` (null ⇒ мгновенный legacy-своп), `_sweepDuration 0.4`, `_swapPause 0.05`, `_sweepAxis` (Vector3.up, локальное пространство цели), `_bandsPerBody 0.5`, `_bandMargin 0.5`, `_sweepEase`. Имя поля `_cheetahMaterial` намеренно НЕ переименовано — Unity сериализует по имени, ссылка в префабе сломалась бы.

## Почему так
- **`UniTask.Yield(PlayerLoopTiming.Update, ct)` переживает деактивацию владельца** — крутится на player loop, а не на MonoBehaviour. Это единственное, что вообще работает в этой точке жизненного цикла. Следствия для любого будущего visual merge attribute: (а) корутина бросит исключение на неактивном GameObject; (б) очистку нельзя вешать на `OnDisable` — она убьёт эффект мгновенно, только на `OnDestroy`; (в) `UniTask.Delay` тоже безопасен.
- **Sync-фасад + `.Forget()`** — прецедент `DeathSpiritSpawner.OnDied`. `MergeView`, `MergeCommand`, `MergeStateSnapshot`, `AnimalFacade`, `BattleStateMachine` не тронуты вообще.
- **[КРИТИЧНОЕ, найдено ревью и исправлено] Overlay-материал нельзя снапшотить как «оригинал».** Репро из обычной игры: мердж Cheetah A → Cheetah B (одинаковый тип тоже накапливает атрибут в `AccumulatedVisualAttributes`), затем B → C. `MergeView.OnMerge` применяет к C оба атрибута `[attrB, attrA]` в одном кадре: второй захватывает `[M0, glowB]` как свои «оригиналы», его финальный `Compose` записывает в рендерер уже уничтоженный материал → `[cheetah, null]` навсегда, `Undo` восстанавливает null, третий мердж распространяет его дальше. Починено `CaptureOriginalMaterials(Renderer)`, отфильтровывающим любую запись, чей шейдер == `_glowMaterial.shader`. Побочный бонус: одновременное двойное аддитивное свечение исчезло само, без флага подавления.
  > **Правило на будущее:** любой рантайм-оверлей в слоте материала должен отфильтровываться при захвате исходного состояния, иначе повторное применение атрибута к той же цели ломает массив материалов.
- **[WARNING, исправлено] `OnDestroy` должен доигрывать, а не просто отменять.** Зовёт `FinishRunningSequenceImmediately()`, чтобы уничтожение владельца в середине свипа оставило цель со скином гепарда и без висящего overlay-слота, а не потеряло визуальный атрибут молча.
- **Undo в середине последовательности синхронен и авторитетен:** отмена CTS → destroy инстанса glow → дословное восстановление сохранённых `sharedMaterials`. Сознательно БЕЗ флага «я уже свопнул» — переприсваивание сохранённого массива корректно во всех трёх точках (до свопа, между проходами, после завершения). Undo достижим из `UndoMergeButton` и `KeyboardMergeUndoHelper` в пре-батле.
- **Zenject не задействован** — ни биндингов, ни установщиков, ни сигналов, только сериализованные ссылки; `new MergeSweepGlow(...)` создаётся владельцем. Прецедент — `DeathSpiritSpawner`/`DeathSpiritView` и решение [[2026-07-31-iridescent-sweep-shader]].
- **Тайминги на `[SerializeField]` компонента, не в ScriptableObject** — это авторские значения одного эффекта с единственным потребителем, а не геймплейный баланс. Триггер миграции зафиксирован явно: как только второй merge attribute (например `ElephantMergeAttribute`) захочет тот же свип — выносить `MergeSweepGlowConfig : ScriptableObject`.
- **Попутно исправлена утечка:** старый код использовал `renderer.material` (инстанцирует каждый базовый материал, и `Undo` восстанавливал инстанс вместо shared-ассета) — теперь всё идёт только через `sharedMaterials`.

## Известное ограничение (принято, не баг)
Когда длина массива материалов рендерера больше его `subMeshCount`, Unity перерисовывает лишним (glow) материалом только **последний** сабмеш. На многосабмешевых мешах свип покроет этот сабмеш, а не весь рендерер.

## Проверка
- Unity Console: новых ошибок нет (остались предсуществующие `UnityEditor.Graphs.Edge.WakeUp` NRE и 5 ошибок параметров `ChickentwoLayerController` из `EnemySpawnService.cs:86`).
- Тесты системы Merge: `MergeUndoServiceTests` + `AbilityUndoTests` — 19/19 зелёные. Новые тесты не писались: **TESTS PAUSED** + явное требование пользователя.
- Критичное репро перепроверено в play mode на пробном объекте: после двух атрибутов в одном кадре массив остаётся 2 слота, после завершения обоих — 1 слот `[Cheetah Spots]`, 0 null-слотов (до фикса было `[Cheetah Spots, NULL]`).
- Обратная совместимость шейдера: `IridescentSweepOverlay.mat` не сериализует `_SweepWorldAxis` → дефолт `(0,1,0,0)` → легаси-ветка, loot-shine не изменился.

## Долг (SUGGESTION из ревью, НЕ применены)
- `_bandMargin = 0.5` уводит полосу полностью за силуэт до свопа материала (центр полосы на `s = 1.5`, видимый экстент `[1.28, 1.72]` при теле `[0, 1]`) — своп происходит при нулевом свечении на экране. Значение ~0.15–0.2 оставило бы полосу на дальней кромке в момент смены скина.
- Ничто не гарантирует `_bandsPerBody * (1 + 2 * _bandMargin) <= 1`; текущие значения сидят ровно на границе (`0.5 * 2 = 1`), любое увеличение через Inspector выведет соседнюю полосу на тело. Нужен `OnValidate` или производный `Mathf.Min`.
- `_cts` не диспозится на happy path — по одному `CancellationTokenSource` на мердж живёт до следующего `Apply`/`Undo`/`OnDestroy`.
- `MergeSweepGlow` имеет `Dispose()`, но не реализует `IDisposable`.

## Затронутые файлы
Новые: `Assets/Code/Animals/Vfx/MergeSweepGlow.cs` (+`.meta`), `Assets/Code/Animals/Materials/MergeSweepGlow.mat` (+`.meta`).
Изменены: `Assets/Shaders/IridescentSweep.shader`, `Assets/Code/Animals/Merge/MergeAttributes/CheetahMergeAttribute.cs` (переписан), `Assets/Resources/Prefabs/Animals/Cheetah.prefab` (удалён тестовый rig прошлой фичи — 2 override-записи `m_Materials` на `SkinnedMeshRenderer` `Fox Tail`; назначен `_glowMaterial`).
НЕ тронуты: `MergeView`, `MergeCommand`, `MergeStateSnapshot`, `AnimalFacade`, `BattleStateMachine`, `IridescentSweepOverlay.mat`.

## Связанное
- [[2026-07-31-iridescent-sweep-shader]] — базовый шейдер и решение «без C#», от которого этот эффект отталкивается
- [[2026-08-01]] — сессия, в которой сделан эффект
