# Решение: эффект «loot shine» — аддитивный оверлей-шейдер во втором слоте материала, без C#

**Дата:** 2026-07-31
**Статус:** принято, реализовано

## Контекст
Нужна переливающаяся волна света, бегущая по всему объекту в цикле, со сдвигом оттенка (iridescence) и свечением по силуэту/периметру. Тестовая цель — хвост лисы на `Assets/Resources/Prefabs/Animals/Cheetah.prefab`.

Ограничения проекта: Built-in RP, Gamma color space, Forward path, пост-обработки в `Main Scene.unity` нет вообще, мобильные платформы. Базовый материал хвоста (`Fox Skin Mane Wolf`) менять нельзя — он общий.

## Рассмотренные варианты
1. **Форк Standard в кастомный surface-шейдер.** Плюс: 1 draw call вместо 2. Минусы: нужно переписать все свойства Standard, меняется базовый вид материала, риск разъехаться с остальными животными.
2. **Полупрозрачный оверлей (`Blend SrcAlpha OneMinusSrcAlpha`).** Минус: сортировка альфы на скиннед-меше, чёрный оверлей затемняет базу.
3. **Аддитивный оверлей вторым слотом материала** (выбрано).

## Принятое решение
Новый шейдер `Assets/Shaders/IridescentSweep.shader` (`Voodoo_LaunchOps/FX/IridescentSweep`) — Built-in RP, ShaderLab + CG, unlit аддитивный оверлей, вешается **вторым слотом материала**, базовый lit-материал не трогается вообще.

Ключевые настройки: `Blend One One`, `ZWrite Off`, `ZTest LEqual`, `Offset -1,-1`, `Fallback Off`, `ForceNoShadowCasting`, `Queue Transparent+1`, `DisableBatching True`, `#pragma target 3.0`, один безусловный вариант шейдера. 18 свойств. Без комментариев — по правилу проекта.

Математика волны:
```
phase = sweepCoord * _SweepTiling - _Time.y * _SweepSpeed + _PhaseOffset
band  = 1 - smoothstep(0, _SweepWidth, abs(frac(phase) - 0.5) * 2)
rim   = pow(1 - saturate(dot(normal, viewDir)), _RimPower)
```
Полоса симметрична относительно wrap-а `frac` → шов бесшовен по построению. Fresnel-терм `rim` — то, что заставляет свет читаться по всему силуэту/периметру. Iridescence — один `cos` между двумя HDR-цветами, оттенок сдвигается тем же Fresnel-термом, поэтому переливается от угла обзора, как масляная плёнка.

**C#-контроллера нет намеренно** — циклическая волна полностью описывается `_Time.y`. Ноль DI-поверхности: нет Zenject-биндинга, нет сигнала, нет MonoBehaviour.

Материал `Assets/Code/Animals/Materials/IridescentSweepOverlay.mat` лежит рядом с владеющим модулем — по прецеденту `Assets/Code/GridPathfinding/Materials/`.

В `Cheetah.prefab` — ровно 2 override-записи на вложенном инстансе `Fox tail Variant`: у `SkinnedMeshRenderer` `Fox Tail` (путь `CG/Pelvis/Tail/Fox tail Variant/Fox Tail`) массив материалов 1 → 2, элемент 0 не тронут (`Fox Skin Mane Wolf`), элемент 1 = оверлей. `m_IsActive` намеренно оставлен `0`.

## Почему так
- **Аддитив вместо форка Standard.** Оверлей выводит только свечение (чёрный = невидим), поэтому не нужно ни воспроизводить освещение Standard, ни решать сортировку альфы. Цена — один лишний draw того же меша; при GPU skinning скиннед-вершины считаются один раз, оба draw читают один буфер.
- **`_SweepTiling` = «полос на объект», а не «полос на мировую единицу».** Это КРИТИЧНАЯ находка ревью. Первая реализация мерила координату в сырых object-space единицах: при `_SweepTiling = 0.4` на хвосте с Y-размахом 0.5502 полоса выходила **шире самого объекта** — вместо бегущей волны получалось равномерное мигание с периодом ~1.67 с. Починено добавлением `_SweepAxisExtent` («Object Size Along Sweep Axis») и делением координаты на него. Для Fox Tail bounds = (0.2047, 0.5502, 0.3502) → `_SweepAxisExtent = 0.5502`, `_SweepTiling = 1`, `_SweepAxis = (0,1,0)`.
- **Мягкий rolloff `glow / (1 + glow)`** по скалярному glow до умножения на тинт. Проект в Gamma, пост-обработки нет — несмотря на `m_HDR: 1` на камерах, ничего не тонмапится, значения > 1 просто клампятся при resolve. Без rolloff на риме `glow ≈ 2.03` выбивал все каналы в белый ровно там, где эффект сильнее всего, убивая переливы. Rolloff сохраняет соотношение каналов на любой интенсивности, поэтому ползунок `_SweepIntensity` безопасен и его не приходится «детюнить».
- **Без GPU instancing.** Unity не поддерживает instancing для `SkinnedMeshRenderer`, поэтому `#pragma multi_compile_instancing` и `UNITY_INSTANCING_BUFFER` убраны; `_PhaseOffset` — обычная uniform-переменная (по-прежнему выставляется через `MaterialPropertyBlock`). Один безусловный вариант — то, что нужно на мобилках.
- **Безопасность для системы мерджа — проверено, а не предположено.** `CheetahMergeAttribute.cs:32,46` — единственное место во всём `Assets/Code`, где пишется материал рендерера животного, и нигде не присваиваются массивные формы (`.materials` / `.sharedMaterials`). Одиночный сеттер `Renderer.material` заменяет элемент 0 и сохраняет длину массива → оверлей в слоте 1 переживает мердж Cheetah. Плюс `GetComponentsInChildren<Renderer>()` там идёт с дефолтным `includeInactive: false`, а хвост неактивен до активации слота Fox, так что в типовом пути этот рендерер даже не посещается.

## Компромисс, который надо помнить
Отсутствие C# оплачено тем, что **переиспользование материала на другом животном требует выставить `_SweepAxisExtent` под размах конкретного меша вдоль `_SweepAxis`**. Пока целей одна — цена приемлема.

Контроллер был в задании, но осознанно отложен. Строить его стоит, только если шиммер должен будет срабатывать по событию (мердж / выбор / награда), а не крутиться в цикле. Тогда: `Assets/Code/Animals/Visuals/SweepGlowView.cs`, слой Presentation, тупая view по образцу `MergePopupView` / `UiPulseAnimator`, параметры на рендерер через `MaterialPropertyBlock` + `Shader.PropertyToID` с перегрузкой `SetPropertyBlock(block, materialIndex)`, чтобы слот 0 остался нетронутым (форма — как в `Code/GridPathfinding/GridCell.cs:136-143`), `UniTask` + DOTween, без биндинга в инсталлере.

## Проверка
- `ShaderUtil.ShaderHasError = False`, 0 shader messages, `isSupported = True`. Unity Console: 0 ошибок шейдера (остаётся только предсуществующий `UnityEditor.Graphs.Edge.WakeUp` NRE от аниматор-контроллеров).
- Волна проверена эмпирически: реальный меш Fox Tail отрисован офскрин через `PreviewRenderUtility` в приватной preview-сцене (открытая сцена не трогалась), с шагами по `_PhaseOffset`. Носитель полосы — 0.220 объекта (было 0.798), центроид движется монотонно на 0.819 объекта за цикл (раньше стоял на месте).
- Цвет: 1 161 600 пикселей, 0 пикселей с каналом ≥ 0.999.
- NaN-guard: `_SweepWidth = 0` → 0 NaN-пикселей.
- Тесты не писались и не гонялись — политика **TESTS PAUSED**, изменение чисто шейдерное/материальное, существующего покрытия нет.

## Затронутые файлы
Новые: `Assets/Shaders/IridescentSweep.shader` (+`.meta`), `Assets/Code/Animals/Materials/IridescentSweepOverlay.mat` (+`.meta`), `Assets/Code/Animals/Materials.meta`.
Изменён: `Assets/Resources/Prefabs/Animals/Cheetah.prefab` (+10 строк, 2 override-записи).
НЕ трогать: `Assets/Animals compilation/Prefabs/Fox tail Variant.prefab` — общий для 10 префабов (Cheetah, Deer, EnemyCheetah_Temp, Elephant, Archive/Elephant, Porcupine Variant, Leopard Variant, Chicken, Elk, Elephant_PA Grey).

## Связанное
- [[2026-07-31]] — сессия, в которой сделан эффект
