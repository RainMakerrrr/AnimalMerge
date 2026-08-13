#if UNITY_EDITOR
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class EditorGridSpawner
    {
        public static GridCell[,] Spawn(GridManager grid, Transform parent, BattleSceneBuildReport report)
        {
            if (!GridManagerEditorAccess.TryGetCellPrefab(grid, out var cellPrefab))
            {
                report.AddError($"Cell prefab is not assigned on '{grid.name}'");
                return null;
            }

            var width = grid.Width;
            var height = grid.Height;
            var cellSize = grid.CellSize;
            var origin = grid.transform.position;
            var cells = new GridCell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cellObject = PrefabUtility.InstantiatePrefab(cellPrefab.gameObject, parent) as GameObject;

                    if (cellObject == null)
                    {
                        report.AddError($"Failed to instantiate the cell prefab of '{grid.name}'");
                        return null;
                    }

                    cellObject.transform.position = origin + new Vector3(x * cellSize, 0f, y * cellSize);
                    cellObject.transform.rotation = Quaternion.identity;

                    var cell = cellObject.GetComponent<GridCell>();

                    if (cell == null)
                    {
                        report.AddError($"Cell prefab of '{grid.name}' has no GridCell component");
                        return null;
                    }

                    cell.SetSize(cellSize);
                    cell.Initialize(x, y, grid, true);
                    cells[x, y] = cell;

                    Undo.RegisterCreatedObjectUndo(cellObject, "Spawn Grid Cell");
                }
            }

            report.GridCells += width * height;

            return cells;
        }
    }
}
#endif
