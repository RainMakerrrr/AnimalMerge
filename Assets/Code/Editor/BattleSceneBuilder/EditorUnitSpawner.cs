#if UNITY_EDITOR
using Code.Animals.Facades;
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class EditorUnitSpawner
    {
        public static AnimalFacade Instantiate(AnimalFacade prefab, Transform parent)
        {
            if (prefab == null)
                return null;

            var prefabPath = AssetDatabase.GetAssetPath(prefab);

            if (string.IsNullOrEmpty(prefabPath))
                return null;

            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabRoot == null)
                return null;

            var instance = PrefabUtility.InstantiatePrefab(prefabRoot, parent) as GameObject;

            if (instance == null)
                return null;

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Animal");

            return instance.GetComponent<AnimalFacade>();
        }

        public static void PlaceAt(AnimalFacade unit, GridCell cell)
        {
            if (unit == null || cell == null)
                return;

            var movement = unit.Movement;

            if (movement == null)
                return;

            movement.Place(cell.WorldPosition);
            movement.RotateToTarget(DirectionVector(movement.Direction));
        }

        public static Vector3 DirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Vector3.forward;
                case Direction.South:
                    return Vector3.back;
                case Direction.East:
                    return Vector3.right;
                case Direction.West:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }
    }
}
#endif
