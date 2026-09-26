using System.Collections.Generic;
using Code.Animals.UI;
using UnityEngine;

namespace Code.Battle.UI
{
    public interface IEnemyCardView
    {
        void Show(Transform anchor, Sprite icon, AnimalCardStats stats, string title,
            IReadOnlyList<string> abilityLines);

        void UpdateAbilityLines(IReadOnlyList<string> abilityLines);

        void UpdateStats(AnimalCardStats stats);

        void Hide();
    }
}
