#if UNITY_EDITOR
using System.Collections.Generic;
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class SerializedFieldWriter
    {
        public static bool SetInt(Object target, string fieldName, int value) =>
            Write(target, fieldName, SerializedPropertyType.Integer, p => p.intValue = value);

        public static bool SetFloat(Object target, string fieldName, float value) =>
            Write(target, fieldName, SerializedPropertyType.Float, p => p.floatValue = value);

        public static bool SetBool(Object target, string fieldName, bool value) =>
            Write(target, fieldName, SerializedPropertyType.Boolean, p => p.boolValue = value);

        public static bool SetEnum(Object target, string fieldName, int rawValue) =>
            Write(target, fieldName, SerializedPropertyType.Enum, p => p.intValue = rawValue);

        public static bool SetObject(Object target, string fieldName, Object value) =>
            Write(target, fieldName, SerializedPropertyType.ObjectReference, p => p.objectReferenceValue = value);

        public static bool SetVector3(Object target, string fieldName, Vector3 value) =>
            Write(target, fieldName, SerializedPropertyType.Vector3, p => p.vector3Value = value);

        public static bool SetColor(Object target, string fieldName, Color value) =>
            Write(target, fieldName, SerializedPropertyType.Color, p => p.colorValue = value);

        public static bool SetLayerMask(Object target, string fieldName, int bits) =>
            Write(target, fieldName, SerializedPropertyType.LayerMask, p => p.intValue = bits);

        public static bool SetUnitSize(Object target, string fieldName, UnitSize value)
        {
            if (target == null) return false;

            var serialized = new SerializedObject(target);
            var width = serialized.FindProperty($"{fieldName}.Width");
            var height = serialized.FindProperty($"{fieldName}.Height");

            if (width == null || height == null)
            {
                LogMissing(target, fieldName);
                return false;
            }

            width.intValue = value.Width;
            height.intValue = value.Height;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static bool SetObjectArray(Object target, string fieldName, IReadOnlyList<Object> values)
        {
            if (target == null) return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);

            if (property == null || !property.isArray)
            {
                LogMissing(target, fieldName);
                return false;
            }

            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static bool ClearArray(Object target, string fieldName)
        {
            if (target == null) return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);

            if (property == null || !property.isArray)
            {
                LogMissing(target, fieldName);
                return false;
            }

            property.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static bool Write(
            Object target,
            string fieldName,
            SerializedPropertyType expected,
            System.Action<SerializedProperty> assign)
        {
            if (target == null) return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                LogMissing(target, fieldName);
                return false;
            }

            if (property.propertyType != expected)
            {
                Debug.LogError(
                    $"[SerializedFieldWriter] {target.GetType().Name}.{fieldName} is {property.propertyType}, " +
                    $"expected {expected}. Nothing was written",
                    target);
                return false;
            }

            assign(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void LogMissing(Object target, string fieldName) =>
            Debug.LogError(
                $"[SerializedFieldWriter] {target.GetType().Name} has no serialized field '{fieldName}'. " +
                "It was probably renamed",
                target);
    }
}
#endif
