using UnityEngine;

namespace Code.Animals.UI
{
    public interface IAnimalStatsPanelView
    {
        void Show(Transform anchor, Sprite icon, int attack, int health);

        void UpdateStats(int attack, int health);

        void Hide();
    }
}
