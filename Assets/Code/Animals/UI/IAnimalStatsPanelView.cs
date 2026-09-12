using System.Collections.Generic;
using UnityEngine;

namespace Code.Animals.UI
{
    public interface IAnimalStatsPanelView
    {
        void Show(Transform anchor, Sprite icon, int attack, int health, string title,
            IReadOnlyList<string> abilityLines);

        void UpdateAbilityLines(IReadOnlyList<string> abilityLines);

        void UpdateStats(int attack, int health);

        void Hide();
    }
}
