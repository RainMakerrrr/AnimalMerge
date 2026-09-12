#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Animals.Facades;
using Code.Battle.Config;
using Code.Infrastructure;
using Code.Levels;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.LevelSets
{
    internal class LevelSetEditorDrawer
    {
        private const string DisplayNameField = "_displayName";
        private const string DescriptionField = "_description";
        private const string PreBattleConfigField = "_preBattleConfig";
        private const string TutorialLevelsField = "_tutorialLevels";
        private const string LevelsField = "_levels";
        private const string StageNumberField = "_stageNumber";
        private const string IsBossStageField = "_isBossStage";
        private const string EnemiesField = "_enemies";
        private const string EnemyPrefabField = "_prefab";
        private const string EnemyPositionField = "_gridPosition";
        private const string EnemyIsBossField = "_isBoss";

        private readonly Dictionary<Object, SerializedObject> _serializedTargets =
            new Dictionary<Object, SerializedObject>();

        private readonly HashSet<Object> _expandedTargets = new HashSet<Object>();

        private LevelSet _set;
        private bool _showTutorialLevels;

        public void SetTarget(LevelSet set)
        {
            if (_set == set)
                return;

            _set = set;
            _serializedTargets.Clear();
            _expandedTargets.Clear();
        }

        public LevelStageConfig Draw()
        {
            if (_set == null)
            {
                EditorGUILayout.HelpBox("Select a level set on the left, or create one.", MessageType.Info);
                return null;
            }

            var setSerialized = Serialized(_set);
            setSerialized.Update();

            EditorGUILayout.PropertyField(setSerialized.FindProperty(DisplayNameField));
            EditorGUILayout.PropertyField(setSerialized.FindProperty(DescriptionField));
            EditorGUILayout.PropertyField(
                setSerialized.FindProperty(PreBattleConfigField),
                new GUIContent("Pre Battle Config", "Optional per-set roster. Only picked up on entering play mode."));

            EditorGUILayout.Space();

            var previewRequest = DrawLevels(setSerialized.FindProperty(LevelsField), "Levels");

            EditorGUILayout.Space();

            _showTutorialLevels = EditorGUILayout.Foldout(_showTutorialLevels, "Tutorial Levels", true);

            if (_showTutorialLevels)
            {
                var tutorialRequest = DrawLevels(setSerialized.FindProperty(TutorialLevelsField), "Tutorial Levels");

                if (previewRequest == null)
                    previewRequest = tutorialRequest;
            }

            setSerialized.ApplyModifiedProperties();

            return previewRequest;
        }

        private LevelStageConfig DrawLevels(SerializedProperty levels, string label)
        {
            LevelStageConfig previewRequest = null;

            EditorGUILayout.LabelField($"{label} ({levels.arraySize})", EditorStyles.boldLabel);

            var removeIndex = -1;
            var moveIndex = -1;
            var moveOffset = 0;

            for (var index = 0; index < levels.arraySize; index++)
            {
                var element = levels.GetArrayElementAtIndex(index);
                var level = element.objectReferenceValue as ExtendedLevel;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (level == null)
                        {
                            EditorGUILayout.PropertyField(element, new GUIContent($"{index + 1}."));
                        }
                        else
                        {
                            var isExpanded = EditorGUILayout.Foldout(
                                _expandedTargets.Contains(level), $"{index + 1}. {level.name}", true);

                            SetExpanded(level, isExpanded);
                        }

                        using (new EditorGUI.DisabledScope(index == 0))
                        {
                            if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(32f)))
                            {
                                moveIndex = index;
                                moveOffset = -1;
                            }
                        }

                        using (new EditorGUI.DisabledScope(index == levels.arraySize - 1))
                        {
                            if (GUILayout.Button("Down", EditorStyles.miniButtonMid, GUILayout.Width(46f)))
                            {
                                moveIndex = index;
                                moveOffset = 1;
                            }
                        }

                        if (GUILayout.Button("Remove", EditorStyles.miniButtonRight, GUILayout.Width(60f)))
                            removeIndex = index;
                    }

                    if (level != null && _expandedTargets.Contains(level))
                    {
                        var stageRequest = DrawLevelBody(level);

                        if (previewRequest == null)
                            previewRequest = stageRequest;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Level"))
                    CreateLevel(levels);

                PrefabComponentField.DrawAppend(
                    new GUIContent(
                        "Attach Existing Level",
                        "Adds a level prefab that already exists. Drag and drop works here too."),
                    levels,
                    typeof(ExtendedLevel));
            }

            if (moveIndex >= 0)
                levels.MoveArrayElement(moveIndex, moveIndex + moveOffset);

            if (removeIndex >= 0)
            {
                levels.GetArrayElementAtIndex(removeIndex).objectReferenceValue = null;
                levels.DeleteArrayElementAtIndex(removeIndex);
            }

            return previewRequest;
        }

        private LevelStageConfig DrawLevelBody(ExtendedLevel level)
        {
            LevelStageConfig previewRequest = null;

            EditorGUI.indentLevel++;

            var levelSerialized = Serialized(level);
            levelSerialized.Update();

            EditorGUILayout.PropertyField(
                levelSerialized.FindProperty(LevelPrefabWriter.IdField), new GUIContent("Analytics Id"));

            var stages = levelSerialized.FindProperty(LevelPrefabWriter.StagesField);

            EditorGUILayout.LabelField($"Stages ({stages.arraySize})", EditorStyles.boldLabel);

            var removeIndex = -1;
            var moveIndex = -1;
            var moveOffset = 0;

            for (var index = 0; index < stages.arraySize; index++)
            {
                var element = stages.GetArrayElementAtIndex(index);
                var stage = element.objectReferenceValue as LevelStageConfig;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(element, new GUIContent($"Stage {index + 1}"));

                    using (new EditorGUI.DisabledScope(index == 0))
                    {
                        if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(32f)))
                        {
                            moveIndex = index;
                            moveOffset = -1;
                        }
                    }

                    using (new EditorGUI.DisabledScope(index == stages.arraySize - 1))
                    {
                        if (GUILayout.Button("Down", EditorStyles.miniButtonMid, GUILayout.Width(46f)))
                        {
                            moveIndex = index;
                            moveOffset = 1;
                        }
                    }

                    if (GUILayout.Button("Remove", EditorStyles.miniButtonRight, GUILayout.Width(60f)))
                        removeIndex = index;
                }

                if (stage == null)
                    continue;

                if (DrawStage(stage))
                    previewRequest = stage;
            }

            var isStageRequested = GUILayout.Button("Create Stage");

            if (moveIndex >= 0)
                stages.MoveArrayElement(moveIndex, moveIndex + moveOffset);

            if (removeIndex >= 0)
            {
                stages.GetArrayElementAtIndex(removeIndex).objectReferenceValue = null;
                stages.DeleteArrayElementAtIndex(removeIndex);
            }

            if (levelSerialized.ApplyModifiedProperties())
                LevelPrefabWriter.Save(level);

            if (isStageRequested)
            {
                LevelSetFactory.AddStage(_set, level);
                levelSerialized.Update();
            }

            EditorGUI.indentLevel--;

            return previewRequest;
        }

        private bool DrawStage(LevelStageConfig stage)
        {
            var isPreviewRequested = false;

            EditorGUI.indentLevel++;

            var stageSerialized = Serialized(stage);
            stageSerialized.Update();

            EditorGUILayout.PropertyField(stageSerialized.FindProperty(StageNumberField));
            EditorGUILayout.PropertyField(
                stageSerialized.FindProperty(IsBossStageField),
                new GUIContent("Is Boss Stage", "Battle logs only. Boss behaviour comes from the per-enemy flag."));

            DrawEnemies(stageSerialized.FindProperty(EnemiesField));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!LevelSetPreviewBridge.IsAvailable))
                {
                    if (GUILayout.Button("Preview Stage In Scene"))
                        isPreviewRequested = true;
                }

                if (GUILayout.Button("Select Asset", GUILayout.Width(100f)))
                    Selection.activeObject = stage;
            }

            stageSerialized.ApplyModifiedProperties();

            EditorGUI.indentLevel--;

            return isPreviewRequested;
        }

        private void DrawEnemies(SerializedProperty enemies)
        {
            EditorGUILayout.LabelField($"Enemies ({enemies.arraySize})");

            var removeIndex = -1;

            for (var index = 0; index < enemies.arraySize; index++)
            {
                var enemy = enemies.GetArrayElementAtIndex(index);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        PrefabComponentField.Draw(
                            new GUIContent($"#{index}"),
                            enemy.FindPropertyRelative(EnemyPrefabField),
                            typeof(AnimalFacade));

                        if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60f)))
                            removeIndex = index;
                    }

                    EditorGUILayout.PropertyField(
                        enemy.FindPropertyRelative(EnemyPositionField), new GUIContent("Grid Position"));
                    EditorGUILayout.PropertyField(
                        enemy.FindPropertyRelative(EnemyIsBossField), new GUIContent("Is Boss"));
                }
            }

            if (GUILayout.Button("Add Enemy"))
            {
                enemies.arraySize++;

                var added = enemies.GetArrayElementAtIndex(enemies.arraySize - 1);
                added.FindPropertyRelative(EnemyPrefabField).objectReferenceValue = null;
                added.FindPropertyRelative(EnemyPositionField).vector2IntValue = Vector2Int.zero;
                added.FindPropertyRelative(EnemyIsBossField).boolValue = false;
            }

            if (removeIndex >= 0)
                enemies.DeleteArrayElementAtIndex(removeIndex);
        }

        private void CreateLevel(SerializedProperty levels)
        {
            var level = LevelPrefabWriter.CreatePrefab(
                LevelSetFactory.LevelsFolderOf(_set), $"Level_{levels.arraySize + 1}");

            if (level == null)
                return;

            AppendElement(levels, level);
            SetExpanded(level, true);
        }

        private void AppendElement(SerializedProperty array, Object value)
        {
            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
        }

        private void SetExpanded(Object target, bool isExpanded)
        {
            if (isExpanded)
                _expandedTargets.Add(target);
            else
                _expandedTargets.Remove(target);
        }

        private SerializedObject Serialized(Object target)
        {
            if (_serializedTargets.TryGetValue(target, out var cached))
                return cached;

            var created = new SerializedObject(target);
            _serializedTargets[target] = created;

            return created;
        }
    }
}
#endif
