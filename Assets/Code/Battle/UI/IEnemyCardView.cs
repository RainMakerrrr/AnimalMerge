using System.Collections.Generic;
using UnityEngine;

namespace Code.Battle.UI
{
    public interface IEnemyCardView
    {
        void Show(Transform anchor, Sprite icon, int attack, int health, string title,
            IReadOnlyList<string> abilityLines);

        void UpdateStats(int attack, int health);

        void Hide();
    }
}
