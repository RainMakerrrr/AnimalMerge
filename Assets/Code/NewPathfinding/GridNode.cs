using UnityEngine;
using UnityEngine.Serialization;

namespace Code.NewPathfinding
{
    public class GridNode : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool IsWalkable { get; set; } = true;

        // Стоимость движения (G cost)
        public int GCost { get; set; }

        // Эвристика (H cost)
        public int HCost { get; set; }

        // Ссылка на родителя для построения пути
        public GridNode ParentNode { get; set; }

        public int FCost => GCost + HCost;

        public void Initialize(int x, int y)
        {
            X = x;
            Y = y;
        }

        // Для визуальной отладки (опционально)
        public void SetColor(Color color)
        {
            if (TryGetComponent(out Renderer rend))
            {
                rend.material.color = color;
            }
        }
    }
}