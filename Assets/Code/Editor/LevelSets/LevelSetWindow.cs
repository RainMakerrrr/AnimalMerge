#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Battle.Config;
using Code.Editor.AnimalPrefabBuilder;
using Code.Levels;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.LevelSets
{
    public class LevelSetWindow : EditorWindow
    {
        private const float ListWidth = 230f;

        [SerializeField] private LevelSet _selectedSet;
        [SerializeField] private string _newSetName = "NewSet";
        [SerializeField] private int _previewLevel = 1;
        [SerializeField] private Vector2 _listScroll;
        [SerializeField] private Vector2 _bodyScroll;
        [SerializeField] private Vector2 _reportScroll;
        [SerializeField] private List<string> _reportLines = new List<string>();

        private List<LevelSet> _sets;
        private List<ValidationFinding> _findings;
        private LevelSetEditorDrawer _drawer;

        [MenuItem("Tools/AnimalMerge/Level Sets")]
        public static void Open()
        {
            var window = GetWindow<LevelSetWindow>("Level Sets");
            window.minSize = new Vector2(760f, 420f);
        }

        [MenuItem("Tools/AnimalMerge/Validate Level Sets")]
        public static void ValidateAllSets()
        {
            var findings = LevelSetValidator.ValidateAll();
            var errors = 0;

            foreach (var finding in findings)
            {
                if (finding.Severity == FindingSeverity.Error)
                {
                    errors++;
                    Debug.LogError($"[LevelSets] {finding}", finding.Context);
                }
                else
                {
                    Debug.LogWarning($"[LevelSets] {finding}", finding.Context);
                }
            }

            Debug.Log($"[LevelSets] Validated {LevelSetCatalog.FindAll().Count} sets: " +
                      $"{errors} errors, {findings.Count - errors} warnings");
        }

        private void OnEnable()
        {
            if (_reportLines == null)
                _reportLines = new List<string>();

            _drawer = new LevelSetEditorDrawer();
            RefreshCatalog();
        }

        private void OnProjectChange()
        {
            RefreshCatalog();
            Repaint();
        }

        private void OnGUI()
        {
            if (_drawer == null)
                _drawer = new LevelSetEditorDrawer();

            if (_sets == null)
                RefreshCatalog();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSetList();
                DrawSelectedSet();
            }
        }

        private void DrawSetList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth)))
            {
                EditorGUILayout.LabelField("Level Sets", EditorStyles.boldLabel);

                if (LevelSetLibraryAccess.Load() == null)
                    EditorGUILayout.HelpBox(
                        $"{LevelSetPaths.LibraryAsset} does not exist yet. It is created with the first set.",
                        MessageType.Warning);

                var defaultSet = LevelSetLibraryAccess.DefaultSet();

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

                for (var index = 0; index < _sets.Count; index++)
                    DrawSetRow(_sets[index], defaultSet);

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                _newSetName = EditorGUILayout.TextField("New Set Name", _newSetName);

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newSetName)))
                {
                    if (GUILayout.Button("Create Set"))
                        CreateSet();

                    using (new EditorGUI.DisabledScope(_selectedSet == null))
                    {
                        if (GUILayout.Button("Duplicate Selected"))
                            DuplicateSet();
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(_selectedSet == null))
                {
                    if (GUILayout.Button("Set As Default"))
                        SetAsDefault();

                    if (GUILayout.Button("Register In Library"))
                        RegisterSelected();

                    if (GUILayout.Button("Remove From Library"))
                        UnregisterSelected();

                    if (GUILayout.Button("Reveal In Project"))
                        EditorGUIUtility.PingObject(_selectedSet);

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Delete Set"))
                        DeleteSelected();
                }
            }
        }

        private void DrawSetRow(LevelSet set, LevelSet defaultSet)
        {
            var isDefault = set == defaultSet;
            var isRegistered = LevelSetLibraryAccess.IsRegistered(set);

            var style = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = isDefault ? FontStyle.Bold : FontStyle.Normal
            };

            if (!isRegistered)
                style.normal.textColor = Color.gray;

            var label = isDefault ? $"{set.DisplayName}  (default)" : set.DisplayName;

            if (set == _selectedSet)
                label = $"> {label}";

            if (GUILayout.Button(label, style))
                SelectSet(set);
        }

        private void DrawSelectedSet()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                _bodyScroll = EditorGUILayout.BeginScrollView(_bodyScroll);

                _drawer.SetTarget(_selectedSet);
                var previewStage = _drawer.Draw();

                EditorGUILayout.EndScrollView();

                if (previewStage != null)
                    PreviewStage(previewStage);

                DrawPreviewControls();
                DrawRuntimeControls();
                DrawValidation();
                DrawReport();
            }
        }

        private void DrawPreviewControls()
        {
            if (_selectedSet == null)
                return;

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                _previewLevel = Mathf.Max(1, EditorGUILayout.IntField("Preview Level", _previewLevel));

                using (new EditorGUI.DisabledScope(!LevelSetPreviewBridge.IsAvailable))
                {
                    if (GUILayout.Button("Clear Preview", GUILayout.Width(120f)))
                        ClearPreview();
                }
            }
        }

        private void DrawRuntimeControls()
        {
            if (!EditorApplication.isPlaying)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Running Game", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Applying a set restarts the campaign: progress is reset to level 1 and saved to PlayerPrefs. " +
                "A per-set Pre Battle Config is only picked up on the next play mode entry.",
                MessageType.Warning);

            using (new EditorGUI.DisabledScope(_selectedSet == null))
            {
                if (GUILayout.Button("Apply To Running Game", GUILayout.Height(26f)))
                    ApplyToRunningGame();
            }
        }

        private void DrawValidation()
        {
            if (_selectedSet == null)
                return;

            EditorGUILayout.Space();

            if (GUILayout.Button("Validate Set"))
                _findings = LevelSetValidator.Validate(_selectedSet);

            if (_findings == null)
                return;

            if (_findings.Count == 0)
            {
                EditorGUILayout.HelpBox("No problems found.", MessageType.Info);
                return;
            }

            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, GUILayout.MinHeight(120f));

            for (var index = 0; index < _findings.Count; index++)
            {
                var finding = _findings[index];
                var icon = finding.Severity == FindingSeverity.Error ? MessageType.Error : MessageType.Warning;

                EditorGUILayout.HelpBox(finding.ToString(), icon);

                var row = GUILayoutUtility.GetLastRect();

                if (finding.Context != null && Event.current.type == EventType.MouseDown &&
                    row.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.PingObject(finding.Context);
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawReport()
        {
            if (_reportLines.Count == 0)
                return;

            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);

            foreach (var line in _reportLines)
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
        }

        private void CreateSet()
        {
            var created = LevelSetFactory.CreateSet(_newSetName.Trim());

            RefreshCatalog();
            SelectSet(created);

            _reportLines.Clear();
            _reportLines.Add($"[Info] Created {AssetDatabase.GetAssetPath(created)}");
        }

        private void DuplicateSet()
        {
            _reportLines.Clear();

            var copy = LevelSetFactory.DuplicateSet(_selectedSet, _newSetName.Trim(), _reportLines);

            RefreshCatalog();

            if (copy == null)
                return;

            SelectSet(copy);
            _reportLines.Add($"[Info] Duplicated into {AssetDatabase.GetAssetPath(copy)}");
        }

        private void SetAsDefault()
        {
            LevelSetLibraryAccess.SetDefault(_selectedSet);

            _reportLines.Clear();
            _reportLines.Add($"[Info] {_selectedSet.DisplayName} is now the default set");
        }

        private void DeleteSelected()
        {
            var message =
                $"Delete '{_selectedSet.DisplayName}' together with the level prefabs and stage " +
                "configs inside its folder?";

            if (LevelSetLibraryAccess.DefaultSet() == _selectedSet)
                message += "\n\nThis is the DEFAULT set. The game will fall back to the first set left in the library.";

            message += "\n\nThis cannot be undone.";

            if (!EditorUtility.DisplayDialog("Delete Level Set", message, "Delete", "Cancel"))
                return;

            _reportLines.Clear();

            if (LevelSetFactory.DeleteSet(_selectedSet, _reportLines))
                SelectSet(null);

            RefreshCatalog();
        }

        private void RegisterSelected()
        {
            LevelSetLibraryAccess.Register(_selectedSet);

            _reportLines.Clear();
            _reportLines.Add($"[Info] {_selectedSet.DisplayName} is registered in {LevelSetPaths.LibraryAsset}");
        }

        private void UnregisterSelected()
        {
            LevelSetLibraryAccess.Unregister(_selectedSet);

            _reportLines.Clear();
            _reportLines.Add(
                $"[Info] {_selectedSet.DisplayName} is no longer listed in {LevelSetPaths.LibraryAsset}, " +
                "its assets are untouched");
        }

        private void PreviewStage(LevelStageConfig stage)
        {
            _reportLines.Clear();
            _reportLines.AddRange(
                LevelSetPreviewBridge.PreviewStage(stage, _selectedSet.PreBattleConfig, _previewLevel));
        }

        private void ClearPreview()
        {
            LevelSetPreviewBridge.ClearPreview();

            _reportLines.Clear();
            _reportLines.Add("[Info] Preview objects removed");
        }

        private void ApplyToRunningGame()
        {
            _reportLines.Clear();

            if (!LevelSetRuntimeBridge.TryResolveSwitchService(out var switchService, out var problem))
            {
                _reportLines.Add($"[Error] {problem}");
                return;
            }

            switchService.SwitchTo(_selectedSet);
            _reportLines.Add($"[Info] Switched the running game to {_selectedSet.DisplayName}");
        }

        private void SelectSet(LevelSet set)
        {
            _selectedSet = set;
            _findings = null;
        }

        private void RefreshCatalog()
        {
            _sets = LevelSetCatalog.FindAll();

            if (_selectedSet == null && _sets.Count > 0)
                SelectSet(LevelSetLibraryAccess.DefaultSet() ?? _sets[0]);
        }
    }
}
#endif
