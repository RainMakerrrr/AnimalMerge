using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Вспомогательный класс для автоматической настройки сцены
    /// Используйте для быстрого тестирования системы
    /// </summary>
    public class SceneSetupHelper : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;

        [Header("Test Units")]
        [SerializeField] private int playerUnitsCount = 3;
        [SerializeField] private int enemyUnitsCount = 3;
        [SerializeField] private GameObject unitPrefab;

        [Header("Unit Distribution")]
        [SerializeField] private bool randomizeUnitSizes = true;

        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;

        private void Start()
        {
            if (setupOnStart)
            {
                SetupScene();
            }
        }

        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            Debug.Log("Setting up test scene...");

            // 1. Создаем Grid Manager
            SetupGridManager();

            // 2. Создаем системы
            SetupSystems();

            // 3. Создаем тестовых юнитов
            SetupTestUnits();

            Debug.Log("Scene setup complete!");
        }

        private void SetupGridManager()
        {
            GridManager gridManager = FindObjectOfType<GridManager>();
            
            if (gridManager == null)
            {
                GameObject gridObj = new GameObject("GridManager");
                gridManager = gridObj.AddComponent<GridManager>();
                Debug.Log("✓ GridManager created");
            }
            else
            {
                Debug.Log("✓ GridManager already exists");
            }
        }

        private void SetupSystems()
        {
            // PathfindingSystem
            if (FindObjectOfType<PathfindingSystem>() == null)
            {
                GameObject pathfindingObj = new GameObject("PathfindingSystem");
                pathfindingObj.AddComponent<PathfindingSystem>();
                Debug.Log("✓ PathfindingSystem created");
            }

            // TargetFindingSystem
            if (FindObjectOfType<TargetFindingSystem>() == null)
            {
                GameObject targetFindingObj = new GameObject("TargetFindingSystem");
                targetFindingObj.AddComponent<TargetFindingSystem>();
                Debug.Log("✓ TargetFindingSystem created");
            }

            // CombatSystem
            if (FindObjectOfType<CombatSystem>() == null)
            {
                GameObject combatObj = new GameObject("CombatSystem");
                combatObj.AddComponent<CombatSystem>();
                Debug.Log("✓ CombatSystem created");
            }
        }

        private void SetupTestUnits()
        {
            if (GridManager.Instance == null)
            {
                Debug.LogError("GridManager not found! Cannot create units.");
                return;
            }

            // Создаем игроков слева
            for (int i = 0; i < playerUnitsCount; i++)
            {
                CreateTestUnit(Team.Player, new Vector2Int(1, 2 + i * 2));
            }

            // Создаем врагов справа
            for (int i = 0; i < enemyUnitsCount; i++)
            {
                CreateTestUnit(Team.Enemy, new Vector2Int(gridWidth - 3, 2 + i * 2));
            }

            Debug.Log($"✓ Created {playerUnitsCount} player units and {enemyUnitsCount} enemy units");
        }

        private void CreateTestUnit(Team team, Vector2Int gridPosition)
        {
            GameObject unitObj;

            if (unitPrefab != null)
            {
                unitObj = Instantiate(unitPrefab);
            }
            else
            {
                // Создаем базовый визуальный объект
                unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unitObj.transform.localScale = Vector3.one * 0.8f;
            }

            unitObj.name = $"{team}_Unit_{Random.Range(1000, 9999)}";

            // Добавляем компоненты
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit == null)
                unit = unitObj.AddComponent<Unit>();

            UnitMovementController controller = unitObj.GetComponent<UnitMovementController>();
            if (controller == null)
                controller = unitObj.AddComponent<UnitMovementController>();

            // Настраиваем юнита
            SetUnitProperties(unit, team);

            // Устанавливаем позицию
            Vector3 worldPos = GridManager.Instance.GetWorldPosition(gridPosition.x, gridPosition.y);
            unitObj.transform.position = worldPos;

            // Цвет по команде
            var renderer = unitObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = team == Team.Player ? Color.blue : Color.red;
            }
        }

        private void SetUnitProperties(Unit unit, Team team)
        {
            // Используем reflection для установки приватных полей
            var unitType = typeof(Unit);

            // Team
            var teamField = unitType.GetField("team", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            teamField?.SetValue(unit, team);

            // Size (рандомный или 1x1)
            UnitSize size = UnitSize.Size1X1;
            if (randomizeUnitSizes)
            {
                int randomSize = Random.Range(0, 3);
                size = randomSize switch
                {
                    0 => UnitSize.Size1X1,
                    1 => UnitSize.Size2X1,
                    2 => UnitSize.Size2X2,
                    _ => UnitSize.Size1X1
                };
            }

            var sizeField = unitType.GetField("unitSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sizeField?.SetValue(unit, size);

            // Movement speed
            var speedField = unitType.GetField("movementSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            speedField?.SetValue(unit, Random.Range(2f, 4f));

            // Damage
            var damageField = unitType.GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            damageField?.SetValue(unit, Random.Range(8, 15));

            // Health
            var healthField = unitType.GetField("maxHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            healthField?.SetValue(unit, Random.Range(80, 120));
        }

        [ContextMenu("Clear All Units")]
        public void ClearAllUnits()
        {
            Unit[] units = FindObjectsOfType<Unit>();
            foreach (var unit in units)
            {
                DestroyImmediate(unit.gameObject);
            }
            Debug.Log($"Cleared {units.Length} units");
        }

        [ContextMenu("Clear Systems")]
        public void ClearSystems()
        {
            DestroyIfExists<GridManager>();
            DestroyIfExists<PathfindingSystem>();
            DestroyIfExists<TargetFindingSystem>();
            DestroyIfExists<CombatSystem>();
            Debug.Log("All systems cleared");
        }

        private void DestroyIfExists<T>() where T : Component
        {
            T component = FindObjectOfType<T>();
            if (component != null)
            {
                DestroyImmediate(component.gameObject);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Scene Setup Helper", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            
            if (GUILayout.Button("Setup Scene", GUILayout.Height(40)))
            {
                SetupScene();
            }

            if (GUILayout.Button("Clear All Units", GUILayout.Height(30)))
            {
                ClearAllUnits();
            }

            if (GUILayout.Button("Clear Systems", GUILayout.Height(30)))
            {
                ClearSystems();
            }

            // Статистика
            int playerCount = 0;
            int enemyCount = 0;
            Unit[] allUnits = FindObjectsOfType<Unit>();
            foreach (var unit in allUnits)
            {
                if (unit.Team == Team.Player) playerCount++;
                else if (unit.Team == Team.Enemy) enemyCount++;
            }

            GUILayout.Space(10);
            GUILayout.Label($"Player Units: {playerCount}");
            GUILayout.Label($"Enemy Units: {enemyCount}");

            GUILayout.EndArea();
        }
    }
}

