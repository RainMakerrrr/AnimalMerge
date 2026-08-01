using Framework.Code.UI;

namespace Framework.Code.Infrastructure.States
{
    public class CampaignVictoryState : IState
    {
        private readonly WindowPool _windowPool;

        public CampaignVictoryState(WindowPool windowPool)
        {
            _windowPool = windowPool;
        }

        public void Enter() => _windowPool.EnableWindows(WindowType.CampaignVictory);

        public void Exit()
        {
        }
    }
}
