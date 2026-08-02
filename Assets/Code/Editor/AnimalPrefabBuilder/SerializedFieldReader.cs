#if UNITY_EDITOR
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class SerializedFieldReader
    {
        public static bool TryGetInt(Object target, string fieldName, out int value)
        {
            value = 0;
            var property = FindProperty(target, fieldName, SerializedPropertyType.Integer);
            if (property == null) return false;
            value = property.intValue;
            return true;
        }

        public static bool TryGetFloat(Object target, string fieldName, out float value)
        {
            value = 0f;
            var property = FindProperty(target, fieldName, SerializedPropertyType.Float);
            if (property == null) return false;
            value = property.floatValue;
            return true;
        }

        public static bool TryGetBool(Object target, string fieldName, out bool value)
        {
            value = false;
            var property = FindProperty(target, fieldName, SerializedPropertyType.Boolean);
            if (property == null) return false;
            value = property.boolValue;
            return true;
        }

        public static bool TryGetObject(Object target, string fieldName, out Object value)
        {
            value = null;
            var property = FindProperty(target, fieldName, SerializedPropertyType.ObjectReference);
            if (property == null) return false;
            value = property.objectReferenceValue;
            return true;
        }

        public static bool TryGetLayerMask(Object target, string fieldName, out int bits)
        {
            bits = 0;
            var property = FindProperty(target, fieldName, SerializedPropertyType.LayerMask);
            if (property == null) return false;
            bits = property.intValue;
            return true;
        }

        public static bool TryGetUnitSize(Object target, string fieldName, out UnitSize value)
        {
            value = UnitSize.Small;
            if (target == null) return false;

            var serialized = new SerializedObject(target);
            var width = serialized.FindProperty($"{fieldName}.Width");
            var height = serialized.FindProperty($"{fieldName}.Height");
            if (width == null || height == null) return false;

            value = new UnitSize(width.intValue, height.intValue);
            return true;
        }

        public static bool TryGetObjectArray(Object target, string fieldName, out Object[] values)
        {
            values = null;
            if (target == null) return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null || !property.isArray) return false;

            values = new Object[property.arraySize];
            for (var i = 0; i < property.arraySize; i++)
                values[i] = property.GetArrayElementAtIndex(i).objectReferenceValue;

            return true;
        }

        private static SerializedProperty FindProperty(Object target, string fieldName, SerializedPropertyType expected)
        {
            if (target == null) return null;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null) return null;
            if (property.propertyType != expected) return null;

            return property;
        }
    }
}
#endif
