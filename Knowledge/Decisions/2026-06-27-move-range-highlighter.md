# Решение — Подсветка радиуса хода вынесена в отдельную сущность

Дата: 2026-06-27

## Контекст
Нужна подсветка клеток, достижимых животным в пределах его лимита шагов (`TilesPerMove`), при удержании клика/тапа на животном. При начале драга подсветка убирается, работает обычный drag-n-drop размещения.

## Решение
- **SRP**: новая логика вынесена в отдельную сущность `MoveRangeHighlighter` (`Assets/Code/Animals/Movement/`), а не добавлена в `AnimalMovement`, который уже отвечает за движение/dodge/retreat/occupancy/поворот. `AnimalMovement` не менялся — из него только читаются public-данные (`CurrentPathNode`, `UnitSize`, `Direction`, `TilesPerMove`).
- **Zenject**: `MoveRangeHighlighter` — plain C#-класс, биндится `IMoveRangeHighlighter → MoveRangeHighlighter AsSingle` в `CurrentGameInstaller`.
- **Расчёт**: 8-направленный BFS flood-fill глубиной `TilesPerMove` через `IGridManager.CanPlaceUnit(..., excludeFootprint)` + дополнительная проверка `GetCell != null`. Двойная проверка нужна из-за молчаливого клампа `AdjustPositionToFitBounds` в `GridManager`.
- **Скоуп подсветки**: радиус хода по ВСЕЙ сетке (без ограничения зоной деплоя Y<=1) — это превью боевого перемещения, а не зона размещения.
- **Drag vs hold**: различаются порогом смещения в пикселях (с поправкой на DPI), а не изменением `IInputService` — интерфейс не трогали. `AnimalMover.Update` переписан в gesture-машину (OnPress → OnHeld → BeginDrag → OnRelease + ResetGesture). `SaveState()/ClearNodes()` перенесены из `TryPickAnimal` в `BeginDrag`, чтобы удержание без драга не трогало occupancy.

## Затронутые файлы
- `Assets/Code/Animals/Movement/MoveRangeHighlighter.cs` (новый)
- `Assets/Code/Animals/Movement/IMoveRangeHighlighter.cs` (новый)
- `Assets/Code/Animals/AnimalMover.cs`
- `Assets/Code/GridPathfinding/GridCell.cs`
- `Assets/Code/Infrastructure/Installers/CurrentGameInstaller.cs`

## Последствия
- Подсветка визуально настраивается через `_moveRangeColor` на префабе GridCell; чувствительность жеста — `_dragThresholdPixels`/`_holdTimeToShow` на AnimalMover.
- Pre-existing WARNING: `GetComponentInParent` в `OnPress` (не введён этой фичей).

## Связанное
- [[2026-06-27]] — лог сессии
- [[Index]] — карта проекта
