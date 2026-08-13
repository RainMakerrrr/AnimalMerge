#if UNITY_EDITOR
using System.Reflection;
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class GridManagerEditorAccess
    {
        private const string CellPrefabFieldName = "_cellPrefab";
        private const string CellsFieldName = "_cells";

        public static bool TryGetCellPrefab(GridManager grid, out GridCell cellPrefab)
        {
            cellPrefab = null;

            if (grid == null)
                return false;

            var serializedGrid = new SerializedObject(grid);
            var cellPrefabProperty = serializedGrid.FindProperty(CellPrefabFieldName);

            if (cellPrefabProperty == null)
            {
                Debug.LogError($"[SceneBuilder] GridManager field '{CellPrefabFieldName}' was renamed, the tool needs updating");
                return false;
            }

            cellPrefab = cellPrefabProperty.objectReferenceValue as GridCell;

            return cellPrefab != null;
        }

        public static bool TrySeedCells(GridManager grid, GridCell[,] cells)
        {
            if (grid == null || cells == null)
                return false;

            var cellsField = GetCellsField();

            if (cellsField == null)
                return false;

            cellsField.SetValue(grid, cells);

            return true;
        }

        public static bool ResetCells(GridManager grid)
        {
            if (grid == null)
                return false;

            var cellsField = GetCellsField();

            if (cellsField == null)
                return false;

            cellsField.SetValue(grid, null);

            return true;
        }

        private static FieldInfo GetCellsField()
        {
            var cellsField = typeof(GridManager).GetField(CellsFieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (cellsField == null)
                Debug.LogError($"[SceneBuilder] GridManager field '{CellsFieldName}' was renamed, the tool needs updating");

            return cellsField;
        }
    }
}
#endif
