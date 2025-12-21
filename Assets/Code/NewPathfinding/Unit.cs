using System.Collections;
using UnityEngine;

namespace Code.NewPathfinding
{
    public class Unit : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private Vector2Int _size = new Vector2Int(1, 1);
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _attackCooldown = 1.0f;

        // Свойства для доступа извне
        public Vector2Int Size => _size;
        public bool IsPlayerTeam { get; private set; }

        // Текущие координаты в сетке
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        private bool _isMoving;
        private float _lastAttackTime;

        // Метод инициализации вместо Start
        public void Initialize(int startX, int startY, bool isPlayerTeam)
        {
            GridX = startX;
            GridY = startY;
            IsPlayerTeam = isPlayerTeam;

            // Ставим модель ровно по сетке и занимаем тайлы
            SnapToGrid();
        }

        private void Update()
        {
            if (_isMoving) return;

            // Запрашиваем врага у Менеджера
            Unit target = UnitManager.Instance.GetNearestEnemy(this);

            if (target != null)
            {
                if (CanAttack(target))
                {
                    PerformAttack(target);
                }
                else
                {
                    MoveTowards(target);
                }
            }
        }

        private void OnDestroy()
        {
            // Важно: сообщаем менеджеру, что мы уничтожены
            if (UnitManager.Instance != null)
            {
                UnitManager.Instance.UnregisterUnit(this);
            }

            // Освобождаем занимаемые клетки перед удалением
            SetWalkableState(GridX, GridY, true);
        }

        private bool CanAttack(Unit target)
        {
            // Проверка дистанции (включая диагонали и размеры)
            for (int x1 = 0; x1 < _size.x; x1++)
            {
                for (int y1 = 0; y1 < _size.y; y1++)
                {
                    int myX = GridX + x1;
                    int myY = GridY + y1;

                    for (int x2 = 0; x2 < target.Size.x; x2++)
                    {
                        for (int y2 = 0; y2 < target.Size.y; y2++)
                        {
                            int targetX = target.GridX + x2;
                            int targetY = target.GridY + y2;

                            int dx = Mathf.Abs(myX - targetX);
                            int dy = Mathf.Abs(myY - targetY);

                            if (Mathf.Max(dx, dy) <= 1)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void PerformAttack(Unit target)
        {
            if (Time.time - _lastAttackTime < _attackCooldown) return;

            Debug.Log($"{name} (Team: {IsPlayerTeam}) attacks {target.name}!");
            _lastAttackTime = Time.time;
            // Тут можно вызвать target.TakeDamage();
        }

        private void MoveTowards(Unit target)
        {
            GridNode bestSpot = FindBestAttackSpot(target);

            if (bestSpot != null)
            {
                var path = Pathfinding.FindPath(
                    new Vector2Int(GridX, GridY),
                    new Vector2Int(bestSpot.X, bestSpot.Y),
                    _size);

                if (path != null && path.Count > 1)
                {
                    GridNode nextNode = path[1];
                    StartCoroutine(MoveRoutine(nextNode));
                }
            }
        }

        // Ищет ближайшую свободную клетку рядом с врагом, куда мы влезем
        private GridNode FindBestAttackSpot(Unit target)
        {
            int searchRadius = 3; // Радиус поиска точки атаки
            GridNode bestNode = null;
            float minDist = float.MaxValue;

            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    int checkX = target.GridX + x;
                    int checkY = target.GridY + y;

                    GridNode node = GridManager.Instance.GetNode(checkX, checkY);

                    // Клетка должна быть валидной, проходимой и мы должны туда поместиться
                    if (node != null && node.IsWalkable)
                    {
                        // Доп. проверка: может ли Pathfinder в принципе найти туда путь (помещаемся ли мы)
                        if (Pathfinding.FindPath(new Vector2Int(GridX, GridY), new Vector2Int(checkX, checkY), _size) !=
                            null)
                        {
                            float d = Vector2.Distance(new Vector2(GridX, GridY), new Vector2(checkX, checkY));
                            if (d < minDist)
                            {
                                minDist = d;
                                bestNode = node;
                            }
                        }
                    }
                }
            }

            return bestNode;
        }

        private IEnumerator MoveRoutine(GridNode targetNode)
        {
            _isMoving = true;

            // 1. Освобождаем старые клетки
            SetWalkableState(GridX, GridY, true);

            Vector3 startPos = transform.position;
            Vector3 endPos = CalculateWorldPosition(targetNode.X, targetNode.Y);

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * _moveSpeed;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // 2. Обновляем координаты
            GridX = targetNode.X;
            GridY = targetNode.Y;

            // 3. Занимаем новые клетки
            SetWalkableState(GridX, GridY, false);

            _isMoving = false;
        }

        private void SnapToGrid()
        {
            transform.position = CalculateWorldPosition(GridX, GridY);
            SetWalkableState(GridX, GridY, false);
        }

        private void SetWalkableState(int startX, int startY, bool state)
        {
            for (int x = 0; x < _size.x; x++)
            {
                for (int y = 0; y < _size.y; y++)
                {
                    GridNode node = GridManager.Instance.GetNode(startX + x, startY + y);
                    if (node != null) node.IsWalkable = state;
                }
            }
        }

        private Vector3 CalculateWorldPosition(int gridX, int gridY)
        {
            Vector3 basePos = GridManager.Instance.GetWorldPosition(gridX, gridY);
            float cellSize = GridManager.Instance.CellSize;

            // Центрирование модели относительно занимаемых тайлов
            float offsetX = (_size.x - 1) * cellSize * 0.5f;
            float offsetZ = (_size.y - 1) * cellSize * 0.5f;

            return basePos + new Vector3(offsetX, 0, offsetZ);
        }
    }
}