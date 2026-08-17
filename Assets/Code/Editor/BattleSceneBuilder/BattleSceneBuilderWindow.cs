#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Battle.Config;
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    public class BattleSceneBuilderWindow : EditorWindow
    {
        [SerializeField] private LevelStageConfig _stageConfig;
        [SerializeField] private PreBattleConfig _preBattleConfig;
        [SerializeField] private GridManager _gameGrid;
        [SerializeField] private bool _buildEnemies = true;
        [SerializeField] private bool _buildAllies = true;
        [SerializeField] private bool _showSceneReferences;
        [SerializeField] private Vector2 _reportScroll;
        [SerializeField] private List<string> _reportLines = new List<string>();

        [MenuItem("Tools/AnimalMerge/Scene Builder")]
        public static void Open()
        {
            GetWindow<BattleSceneBuilderWindow>("Scene Builder");
        }

        private void OnEnable()
        {
            if (_reportLines == null)
                _reportLines = new List<string>();

            ResolveMissingReferences();
        }

        private void OnHierarchyChange()
        {
            Repaint();
        }

        private void OnGUI()
        {
            var isPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;

            EditorGUILayout.HelpBox(
                "Edit mode preview. Spawned objects get no Zenject dependencies and do not run any game logic. Press Clear before saving or committing the scene.",
                MessageType.Info);

            if (isPlayMode)
                EditorGUILayout.HelpBox("Scene Builder is disabled in play mode.", MessageType.Warning);

            EditorGUILayout.Space();

            _stageConfig = (LevelStageConfig)EditorGUILayout.ObjectField(
                "Level Stage", _stageConfig, typeof(LevelStageConfig), false);

            _preBattleConfig = (PreBattleConfig)EditorGUILayout.ObjectField(
                "Pre Battle Config", _preBattleConfig, typeof(PreBattleConfig), false);

            DrawSceneReferences();

            EditorGUILayout.Space();

            _buildEnemies = EditorGUILayout.Toggle("Build Enemies", _buildEnemies);
            _buildAllies = EditorGUILayout.Toggle("Build Allies", _buildAllies);

            EditorGUILayout.Space();

            DrawRequirements();

            using (new EditorGUI.DisabledScope(!CanBuild(isPlayMode)))
            {
                if (GUILayout.Button("Build", GUILayout.Height(30f)))
                    Build();
            }

            using (new EditorGUI.DisabledScope(isPlayMode || BattleSceneRoot.Find() == null))
            {
                if (GUILayout.Button("Clear", GUILayout.Height(24f)))
                    ClearScene();
            }

            DrawReport();
        }

        private void DrawSceneReferences()
        {
            _showSceneReferences = EditorGUILayout.Foldout(_showSceneReferences, "Scene References", true);

            if (!_showSceneReferences)
                return;

            EditorGUI.indentLevel++;

            _gameGrid = (GridManager)EditorGUILayout.ObjectField("Game Grid", _gameGrid, typeof(GridManager), true);

            if (GUILayout.Button("Resolve From Scene"))
                ResolveReferences();

            EditorGUI.indentLevel--;
        }

        private void DrawRequirements()
        {
            if (_stageConfig == null)
                EditorGUILayout.HelpBox("Assign a LevelStageConfig asset to build.", MessageType.Warning);

            if (_gameGrid == null)
                EditorGUILayout.HelpBox("Game grid is not resolved. Open Scene References and assign it.", MessageType.Warning);

            if (_buildAllies && _preBattleConfig == null)
                EditorGUILayout.HelpBox("PreBattleConfig is not assigned, allies cannot be built.", MessageType.Warning);
        }

        private void DrawReport()
        {
            if (_reportLines.Count == 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);

            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, GUILayout.MinHeight(80f));

            foreach (var line in _reportLines)
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndScrollView();
        }

        private bool CanBuild(bool isPlayMode)
        {
            return !isPlayMode && _stageConfig != null && _gameGrid != null;
        }

        private void Build()
        {
            var request = new BattleSceneBuildRequest
            {
                StageConfig = _stageConfig,
                PreBattleConfig = _preBattleConfig,
                GameGrid = _gameGrid,
                BuildAllies = _buildAllies,
                BuildEnemies = _buildEnemies
            };

            var report = BattleSceneBuilder.Build(request);

            _reportLines.Clear();
            _reportLines.AddRange(report.Messages);
        }

        private void ClearScene()
        {
            BattleSceneBuilder.Clear();

            _reportLines.Clear();
            _reportLines.Add("[Info] Spawned objects removed");
        }

        private void ResolveMissingReferences()
        {
            if (_gameGrid != null && _preBattleConfig != null)
                return;

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (BattleSceneReferenceResolver.TryResolveGameGrid(out var gameGrid))
                _gameGrid = gameGrid;

            if (_preBattleConfig == null)
                _preBattleConfig = BattleSceneReferenceResolver.LoadPreBattleConfig();
        }
    }
}
#endif
