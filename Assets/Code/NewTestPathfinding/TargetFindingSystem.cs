using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Система поиска целей (ближайших врагов)
    /// </summary>
    public class TargetFindingSystem : MonoBehaviour
    {
        public static TargetFindingSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float searchRadius = 50f;
        [SerializeField] private bool useGridDistance = true;

        [Header("Optimization")]
        [SerializeField] private float cacheUpdateInterval = 0.5f;

        // Кэш всех юнитов по командам
        private Dictionary<Team, List<Unit>> unitsByTeam = new Dictionary<Team, List<Unit>>();
        private float lastCacheUpdateTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            UpdateUnitsCache();
        }

        private void Update()
        {
            // Периодическое обновление кэша
            if (Time.time - lastCacheUpdateTime > cacheUpdateInterval)
            {
                UpdateUnitsCache();
            }
        }

        /// <summary>
        /// Обновить кэш всех юнитов
        /// </summary>
        private void UpdateUnitsCache()
        {
            unitsByTeam.Clear();

            Unit[] allUnits = FindObjectsOfType<Unit>();
            
            foreach (Unit unit in allUnits)
            {
                if (unit.IsDead)
                    continue;

                if (!unitsByTeam.ContainsKey(unit.Team))
                {
                    unitsByTeam[unit.Team] = new List<Unit>();
                }

                unitsByTeam[unit.Team].Add(unit);
            }

            lastCacheUpdateTime = Time.time;
        }

        /// <summary>
        /// Найти ближайшего врага для юнита
        /// </summary>
        public Unit FindNearestEnemy(Unit seeker)
        {
            if (seeker == null)
                return null;

            List<Unit> enemies = GetEnemies(seeker.Team);
            
            if (enemies.Count == 0)
                return null;

            Unit nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (Unit enemy in enemies)
            {
                if (enemy.IsDead)
                    continue;

                float distance = useGridDistance 
                    ? GetGridDistance(seeker, enemy)
                    : GetEuclideanDistance(seeker, enemy);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            return nearestEnemy;
        }

        /// <summary>
        /// Найти всех врагов в радиусе
        /// </summary>
        public List<Unit> FindEnemiesInRadius(Unit seeker, float radius)
        {
            if (seeker == null)
                return new List<Unit>();

            List<Unit> enemies = GetEnemies(seeker.Team);
            List<Unit> enemiesInRadius = new List<Unit>();

            foreach (Unit enemy in enemies)
            {
                if (enemy.IsDead)
                    continue;

                float distance = GetEuclideanDistance(seeker, enemy);
                
                if (distance <= radius)
                {
                    enemiesInRadius.Add(enemy);
                }
            }

            return enemiesInRadius;
        }

        /// <summary>
        /// Получить список врагов для команды
        /// </summary>
        private List<Unit> GetEnemies(Team team)
        {
            List<Unit> enemies = new List<Unit>();

            foreach (var kvp in unitsByTeam)
            {
                if (kvp.Key != team && kvp.Key != Team.Neutral)
                {
                    enemies.AddRange(kvp.Value);
                }
            }

            return enemies;
        }

        /// <summary>
        /// Получить расстояние по сетке между юнитами (grid distance)
        /// </summary>
        private float GetGridDistance(Unit a, Unit b)
        {
            if (GridManager.Instance == null)
                return GetEuclideanDistance(a, b);

            // Берем ближайшие клетки между юнитами
            var aCells = a.GetOccupiedCells();
            var bCells = b.GetOccupiedCells();

            float minDistance = float.MaxValue;

            foreach (var aCell in aCells)
            {
                foreach (var bCell in bCells)
                {
                    // Manhattan distance
                    float distance = Mathf.Abs(aCell.x - bCell.x) + Mathf.Abs(aCell.y - bCell.y);
                    
                    if (distance < minDistance)
                        minDistance = distance;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// Получить евклидово расстояние между юнитами
        /// </summary>
        private float GetEuclideanDistance(Unit a, Unit b)
        {
            return Vector3.Distance(a.transform.position, b.transform.position);
        }

        /// <summary>
        /// Проверить, находится ли цель в радиусе атаки
        /// </summary>
        public bool IsInAttackRange(Unit attacker, Unit target)
        {
            if (attacker == null || target == null)
                return false;

            return attacker.IsInAttackRange(target);
        }

        /// <summary>
        /// Получить ближайшую позицию для атаки цели
        /// </summary>
        public Vector2Int GetAttackPosition(Unit attacker, Unit target)
        {
            if (GridManager.Instance == null || target == null)
                return attacker.CurrentGridPosition;

            var targetCells = target.GetOccupiedCells();
            Vector2Int attackerPos = attacker.CurrentGridPosition;
            
            Vector2Int nearestCell = targetCells[0];
            float minDistance = float.MaxValue;

            // Находим ближайшую клетку цели
            foreach (var cell in targetCells)
            {
                float distance = Vector2Int.Distance(attackerPos, cell);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestCell = cell;
                }
            }

            // Находим свободную позицию рядом с целью для атаки
            List<Vector2Int> directions = new List<Vector2Int>
            {
                new Vector2Int(1, 0),   // Право
                new Vector2Int(-1, 0),  // Лево
                new Vector2Int(0, 1),   // Верх
                new Vector2Int(0, -1),  // Низ
                new Vector2Int(1, 1),   // Диагонали
                new Vector2Int(-1, 1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                Vector2Int checkPos = nearestCell + dir;
                
                if (GridManager.Instance.IsAreaClear(
                    checkPos.x, checkPos.y, 
                    attacker.Size.Width, attacker.Size.Height, 
                    attacker))
                {
                    return checkPos;
                }
            }

            // Если не нашли позицию рядом - используем pathfinding
            return PathfindingSystem.Instance?.FindNearestWalkablePosition(attacker, nearestCell) 
                ?? attackerPos;
        }

        /// <summary>
        /// Зарегистрировать нового юнита
        /// </summary>
        public void RegisterUnit(Unit unit)
        {
            if (unit == null)
                return;

            if (!unitsByTeam.ContainsKey(unit.Team))
            {
                unitsByTeam[unit.Team] = new List<Unit>();
            }

            if (!unitsByTeam[unit.Team].Contains(unit))
            {
                unitsByTeam[unit.Team].Add(unit);
            }
        }

        /// <summary>
        /// Удалить юнита из системы
        /// </summary>
        public void UnregisterUnit(Unit unit)
        {
            if (unit == null)
                return;

            if (unitsByTeam.ContainsKey(unit.Team))
            {
                unitsByTeam[unit.Team].Remove(unit);
            }
        }

        /// <summary>
        /// Получить всех юнитов команды
        /// </summary>
        public List<Unit> GetUnitsOfTeam(Team team)
        {
            if (unitsByTeam.ContainsKey(team))
            {
                return new List<Unit>(unitsByTeam[team]);
            }

            return new List<Unit>();
        }

        /// <summary>
        /// Получить количество юнитов команды
        /// </summary>
        public int GetTeamUnitCount(Team team)
        {
            if (unitsByTeam.ContainsKey(team))
            {
                return unitsByTeam[team].Count(u => !u.IsDead);
            }

            return 0;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

