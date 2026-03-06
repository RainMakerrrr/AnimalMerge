using Code.Animals;
using Code.Battle;
using Code.Battle.Input;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Installers
{
    /// <summary>
    /// Installer for Battle State System services and states
    /// </summary>
    public class BattleInstaller : MonoInstaller
    {
        [SerializeField] private TargetFinder _targetFinder;
        [SerializeField] private AnimalSpawner _animalSpawner;

        public override void InstallBindings()
        {
            BindServices();
            BindBattleStateMachine();
            BindBattleFlowController();
        }

        private void BindServices()
        {
            // Core battle services
            Container.BindInterfacesAndSelfTo<UnitTracker>().AsSingle();
            Container.Bind<IEnemySpawnService>().To<EnemySpawnService>().AsSingle();
            Container.Bind<ITurnExecutor>().To<TurnExecutor>().AsSingle();
            Container.Bind<IVictoryConditionChecker>().To<VictoryConditionChecker>().AsSingle();
            Container.Bind<IHealthRestorationService>().To<HealthRestorationService>().AsSingle();
            Container.Bind<IUnitRepositioningService>().To<UnitRepositioningService>().AsSingle();

            // Battle start input system
            Container.Bind<StartBattleService>().AsSingle();

            // Debug helper - only in Unity Editor
#if UNITY_EDITOR
            Container.Bind<KeyboardStartBattleHelper>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("KeyboardStartBattleHelper (Debug)")
                .AsSingle()
                .NonLazy();
#endif

            // MonoBehaviour dependencies from scene
            Container.Bind<TargetFinder>().FromInstance(_targetFinder).AsSingle();
            Container.Bind<AnimalSpawner>().FromInstance(_animalSpawner).AsSingle();
        }

        private void BindBattleStateMachine()
        {
            // BattleStateMachine will initialize itself via [Inject] method
            Container.Bind<BattleStateMachine>().AsSingle().NonLazy();
        }

        private void BindBattleFlowController()
        {
            Container.Bind<BattleFlowController>().AsSingle().NonLazy();
        }
    }
}
