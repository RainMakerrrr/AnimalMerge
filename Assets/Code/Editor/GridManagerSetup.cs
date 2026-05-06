#if UNITY_EDITOR
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor
{
    /// <summary>
    /// Editor utility to create and setup GridManager objects
    /// </summary>
    public static class GridManagerSetup
    {
        [MenuItem("Tools/Pathfinding/Create Game Grid")]
        public static void CreateGameGrid()
        {
            CreateGridManager("Game Grid", new Vector3(0, 0, 0), 8, 10, 1f, "GameGrid");
        }

        [MenuItem("Tools/Pathfinding/Create Merge Grid")]
        public static void CreateMergeGrid()
        {
            CreateGridManager("Merge Grid", new Vector3(0, 0, 0), 8, 2, 1f, "MergeGrid");
        }

        [MenuItem("Tools/Pathfinding/Create Both Grids")]
        public static void CreateBothGrids()
        {
            CreateGameGrid();
            CreateMergeGrid();
        }

        private static void CreateGridManager(string name, Vector3 position, int width, int height, float cellSize, string tag)
        {
            // Check if object already exists
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                if (EditorUtility.DisplayDialog("Grid Already Exists",
                    $"A GameObject named '{name}' already exists. Replace it?",
                    "Replace", "Cancel"))
                {
                    Undo.DestroyObjectImmediate(existing);
                }
                else
                {
                    return;
                }
            }

            // Create GameObject
            GameObject gridObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gridObject, $"Create {name}");
            gridObject.transform.position = position;

            // Add GridManager component
            GridManager gridManager = gridObject.AddComponent<GridManager>();

            // Set private fields via SerializedObject
            SerializedObject serializedObject = new SerializedObject(gridManager);
            serializedObject.FindProperty("_width").intValue = width;
            serializedObject.FindProperty("_height").intValue = height;
            serializedObject.FindProperty("_cellSize").floatValue = cellSize;
            serializedObject.FindProperty("_showDebugGizmos").boolValue = true;
            serializedObject.ApplyModifiedProperties();

            // Create cell prefab if it doesn't exist
            CreateCellPrefabIfNeeded(serializedObject);

            // Tag the object
            try
            {
                gridObject.tag = tag;
            }
            catch
            {
                Debug.LogWarning($"Tag '{tag}' doesn't exist. Please create it in Tag Manager.");
            }

            // Select the created object
            Selection.activeGameObject = gridObject;

            Debug.Log($"[GridManagerSetup] Created {name} at {position} with size {width}x{height}");
        }

        private static void CreateCellPrefabIfNeeded(SerializedObject gridManagerSerializedObject)
        {
            // Check if prefab already exists
            string prefabPath = "Assets/Prefabs/GridCell.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                // Use existing prefab
                gridManagerSerializedObject.FindProperty("_cellPrefab").objectReferenceValue =
                    existingPrefab.GetComponent<GridCell>();
                gridManagerSerializedObject.ApplyModifiedProperties();
                Debug.Log("[GridManagerSetup] Using existing GridCell prefab");
                return;
            }

            // Create new prefab
            Debug.Log("[GridManagerSetup] Creating new GridCell prefab...");

            // Ensure Prefabs folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Create cell GameObject
            GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cellObject.name = "GridCell";
            cellObject.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f);

            // Add GridCell component
            GridCell cellView = cellObject.AddComponent<GridCell>();

            // Setup material
            Material cellMaterial = new Material(Shader.Find("Standard"));
            cellMaterial.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            cellObject.GetComponent<Renderer>().sharedMaterial = cellMaterial;

            // Save material
            string materialPath = "Assets/Prefabs/GridCellMaterial.mat";
            AssetDatabase.CreateAsset(cellMaterial, materialPath);

            // Create prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cellObject, prefabPath);
            Object.DestroyImmediate(cellObject);

            // Assign prefab to GridManager
            gridManagerSerializedObject.FindProperty("_cellPrefab").objectReferenceValue =
                prefab.GetComponent<GridCell>();
            gridManagerSerializedObject.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            Debug.Log($"[GridManagerSetup] Created GridCell prefab at {prefabPath}");
        }

        [MenuItem("Tools/Pathfinding/Setup GridCell Layer")]
        public static void SetupGridCellLayer()
        {
            // This opens the Tags and Layers settings
            EditorApplication.ExecuteMenuItem("Edit/Project Settings/Tags and Layers");

            Debug.Log("[GridManagerSetup] Please add 'PathNode' layer in the Inspector if it doesn't exist.");
            Debug.Log("[GridManagerSetup] Then assign this layer to GridCell prefab.");
        }
    }
}

#endif
