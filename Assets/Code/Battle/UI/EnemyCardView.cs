using Code.Animals.UI;

namespace Code.Battle.UI
{
    public class EnemyCardView : AnimalStatsPanelView, IEnemyCardView
    {
        protected override bool ShouldAvoidCoveringAnchor => false;
    }
}
