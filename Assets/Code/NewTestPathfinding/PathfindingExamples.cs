using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Примеры использования всех трех реализаций PathfindingSystem
    /// </summary>
    public class PathfindingExamples : MonoBehaviour
    {
        [Header("Example Settings")]
        [SerializeField] private Unit testUnit;
        [SerializeField] private Vector2Int startPos = new Vector2Int(0, 0);
        [SerializeField] private Vector2Int goalPos = new Vector2Int(9, 9);

        // Для Pure C# версии
        private PathfindingSystemPure purePathfinder;

        private void Start()
        {
            // Инициализация Pure C# версии
            purePathfinder = new PathfindingSystemPure();
            
            Debug.Log("Готово к тестированию всех трех версий PathfindingSystem!");
        }

        /// <summary>
        /// Пример 1: MonoBehaviour версия (оригинальная)
        /// </summary>
        [ContextMenu("Example 1: Use MonoBehaviour Version")]
        public void Example1_MonoBehaviour()
        {
            Debug.Log("=== Example 1: MonoBehaviour Version ===");

            if (PathfindingSystem.Instance == null)
            {
                Debug.LogWarning("PathfindingSystem (MonoBehaviour) не найден в сцене!");
                return;
            }

            if (testUnit == null)
            {
                Debug.LogWarning("Test Unit не назначен!");
                return;
            }

            // Простой вызов через Singleton
            List<GridNode> path = PathfindingSystem.Instance.FindPath(
                testUnit,
                startPos,
                goalPos
            );

            if (path != null)
            {
                Debug.Log($"✅ MonoBehaviour: Путь найден! Длина: {path.Count}");
                // Путь автоматически визуализируется через Gizmos
            }
            else
            {
                Debug.Log("❌ MonoBehaviour: Путь не найден");
            }
        }

        /// <summary>
        /// Пример 2: Static версия
        /// </summary>
        [ContextMenu("Example 2: Use Static Version")]
        public void Example2_Static()
        {
            Debug.Log("=== Example 2: Static Version ===");

            if (testUnit == null)
            {
                Debug.LogWarning("Test Unit не назначен!");
                return;
            }

            // Настройка перед использованием
            PathfindingSystemStatic.MaxIterations = 1000;
            PathfindingSystemStatic.UseDiagonalMovement = true;

            // Простой статический вызов
            List<GridNode> path = PathfindingSystemStatic.FindPath(
                testUnit,
                startPos,
                goalPos
            );

            if (path != null)
            {
                Debug.Log($"✅ Static: Путь найден! Длина: {path.Count}");
                VisualizePathInConsole(path);
            }
            else
            {
                Debug.Log("❌ Static: Путь не найден");
            }
        }

        /// <summary>
        /// Пример 3: Pure C# версия
        /// </summary>
        [ContextMenu("Example 3: Use Pure C# Version")]
        public void Example3_PureCSharp()
        {
            Debug.Log("=== Example 3: Pure C# Version ===");

            if (testUnit == null)
            {
                Debug.LogWarning("Test Unit не назначен!");
                return;
            }

            if (GridManager.Instance == null)
            {
                Debug.LogWarning("GridManager не найден!");
                return;
            }

            // Используем экземпляр (передаем GridManager явно)
            List<GridNode> path = purePathfinder.FindPath(
                testUnit,
                startPos,
                goalPos,
                GridManager.Instance
            );

            if (path != null)
            {
                Debug.Log($"✅ Pure C#: Путь найден! Длина: {path.Count}");
                Debug.Log($"Path cost: {purePathfinder.GetPathCost(path)}");
                VisualizePathInConsole(path);
            }
            else
            {
                Debug.Log("❌ Pure C#: Путь не найден");
            }
        }

        /// <summary>
        /// Пример 4: Несколько Pure C# pathfinder'ов с разными настройками
        /// </summary>
        [ContextMenu("Example 4: Multiple Pure Pathfinders")]
        public void Example4_MultiplePathfinders()
        {
            Debug.Log("=== Example 4: Multiple Pathfinders ===");

            if (testUnit == null || GridManager.Instance == null)
            {
                Debug.LogWarning("Test Unit или GridManager не найдены!");
                return;
            }

            // Быстрый pathfinder (меньше итераций)
            var fastPathfinder = new PathfindingSystemPure(PathfindingConfig.Fast);
            
            // Точный pathfinder (больше итераций)
            var precisePathfinder = new PathfindingSystemPure(PathfindingConfig.Precise);

            // Кастомный pathfinder без диагоналей
            var noDiagonalPathfinder = new PathfindingSystemPure()
            {
                UseDiagonalMovement = false,
                MaxIterations = 1500
            };

            Debug.Log("--- Fast Pathfinder ---");
            var path1 = fastPathfinder.FindPath(testUnit, startPos, goalPos, GridManager.Instance);
            if (path1 != null) Debug.Log($"Fast: {path1.Count} nodes, cost: {fastPathfinder.GetPathCost(path1)}");

            Debug.Log("--- Precise Pathfinder ---");
            var path2 = precisePathfinder.FindPath(testUnit, startPos, goalPos, GridManager.Instance);
            if (path2 != null) Debug.Log($"Precise: {path2.Count} nodes, cost: {precisePathfinder.GetPathCost(path2)}");

            Debug.Log("--- No Diagonal Pathfinder ---");
            var path3 = noDiagonalPathfinder.FindPath(testUnit, startPos, goalPos, GridManager.Instance);
            if (path3 != null) Debug.Log($"No Diagonal: {path3.Count} nodes, cost: {noDiagonalPathfinder.GetPathCost(path3)}");
        }

        /// <summary>
        /// Пример 5: Сравнение производительности
        /// </summary>
        [ContextMenu("Example 5: Performance Comparison")]
        public void Example5_PerformanceComparison()
        {
            Debug.Log("=== Example 5: Performance Comparison ===");

            if (testUnit == null)
            {
                Debug.LogWarning("Test Unit не назначен!");
                return;
            }

            int iterations = 100;

            // MonoBehaviour версия
            if (PathfindingSystem.Instance != null)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    PathfindingSystem.Instance.FindPath(testUnit, startPos, goalPos);
                }
                sw.Stop();
                Debug.Log($"MonoBehaviour: {sw.ElapsedMilliseconds}ms для {iterations} поисков");
            }

            // Static версия
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    PathfindingSystemStatic.FindPath(testUnit, startPos, goalPos);
                }
                sw.Stop();
                Debug.Log($"Static: {sw.ElapsedMilliseconds}ms для {iterations} поисков");
            }

            // Pure C# версия
            if (GridManager.Instance != null)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    purePathfinder.FindPath(testUnit, startPos, goalPos, GridManager.Instance);
                }
                sw.Stop();
                Debug.Log($"Pure C#: {sw.ElapsedMilliseconds}ms для {iterations} поисков");
            }
        }

        /// <summary>
        /// Визуализация пути в консоли
        /// </summary>
        private void VisualizePathInConsole(List<GridNode> path)
        {
            if (path == null || path.Count == 0) return;

            string pathStr = "Path: ";
            for (int i = 0; i < Mathf.Min(path.Count, 5); i++)
            {
                pathStr += $"({path[i].GridX},{path[i].GridY})";
                if (i < path.Count - 1) pathStr += " → ";
            }
            if (path.Count > 5)
                pathStr += $" ... → ({path[path.Count - 1].GridX},{path[path.Count - 1].GridY})";

            Debug.Log(pathStr);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 250, 300, 400));
            GUILayout.Label("Pathfinding Examples", new GUIStyle(GUI.skin.label) 
                { fontSize = 14, fontStyle = FontStyle.Bold });

            GUILayout.Space(10);

            if (GUILayout.Button("1. MonoBehaviour Version", GUILayout.Height(35)))
            {
                Example1_MonoBehaviour();
            }

            if (GUILayout.Button("2. Static Version", GUILayout.Height(35)))
            {
                Example2_Static();
            }

            if (GUILayout.Button("3. Pure C# Version", GUILayout.Height(35)))
            {
                Example3_PureCSharp();
            }

            if (GUILayout.Button("4. Multiple Pathfinders", GUILayout.Height(35)))
            {
                Example4_MultiplePathfinders();
            }

            if (GUILayout.Button("5. Performance Test", GUILayout.Height(35)))
            {
                Example5_PerformanceComparison();
            }

            GUILayout.Space(10);

            // Информация
            GUILayout.Label($"Test Unit: {(testUnit != null ? testUnit.name : "Not assigned")}");
            GUILayout.Label($"Start: ({startPos.x}, {startPos.y})");
            GUILayout.Label($"Goal: ({goalPos.x}, {goalPos.y})");

            GUILayout.EndArea();
        }
    }
}

