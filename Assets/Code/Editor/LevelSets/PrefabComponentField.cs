#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Editor.LevelSets
{
    internal static class PrefabComponentField
    {
        public static void Draw(GUIContent label, SerializedProperty property, Type componentType)
        {
            var fieldRect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);
            var serialized = property.serializedObject;
            var path = property.propertyPath;
            var dropped = HandleDrop(fieldRect, componentType);

            if (dropped != null)
                Assign(serialized, path, dropped);

            var current = property.objectReferenceValue;
            var caption = current == null ? $"None ({componentType.Name})" : current.name;

            if (!EditorGUI.DropdownButton(fieldRect, new GUIContent(caption), FocusType.Keyboard))
                return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), current == null, () => Assign(serialized, path, null));
            menu.AddSeparator(string.Empty);

            foreach (var entry in PrefabComponentCatalog.Scan(componentType))
            {
                var captured = entry;

                menu.AddItem(
                    new GUIContent(PrefabComponentCatalog.MenuPathOf(entry)),
                    current == (Object)entry,
                    () => Assign(serialized, path, captured));
            }

            menu.DropDown(fieldRect);
        }

        public static void DrawAppend(GUIContent content, SerializedProperty arrayProperty, Type componentType)
        {
            var rect = EditorGUILayout.GetControlRect();
            var serialized = arrayProperty.serializedObject;
            var path = arrayProperty.propertyPath;
            var dropped = HandleDrop(rect, componentType);

            if (dropped != null)
                Append(serialized, path, dropped);

            if (!EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
                return;

            var menu = new GenericMenu();
            var entries = PrefabComponentCatalog.Scan(componentType);

            if (entries.Count == 0)
                menu.AddDisabledItem(new GUIContent($"No prefab with {componentType.Name} on its root"));

            foreach (var entry in entries)
            {
                var captured = entry;

                menu.AddItem(
                    new GUIContent(PrefabComponentCatalog.MenuPathOf(entry)),
                    false,
                    () => Append(serialized, path, captured));
            }

            menu.DropDown(rect);
        }

        private static Component HandleDrop(Rect rect, Type componentType)
        {
            var current = Event.current;

            if (!rect.Contains(current.mousePosition))
                return null;

            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
                return null;

            var candidate = PrefabComponentCatalog.Resolve(DragAndDrop.objectReferences, componentType);

            if (current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = candidate == null
                    ? DragAndDropVisualMode.Rejected
                    : DragAndDropVisualMode.Copy;

                current.Use();
                return null;
            }

            DragAndDrop.AcceptDrag();
            current.Use();

            return candidate;
        }

        private static void Assign(SerializedObject serialized, string path, Object value)
        {
            serialized.Update();

            var property = serialized.FindProperty(path);

            if (property == null)
                return;

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void Append(SerializedObject serialized, string path, Object value)
        {
            serialized.Update();

            var array = serialized.FindProperty(path);

            if (array == null)
                return;

            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
#endif
