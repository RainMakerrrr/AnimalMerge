using Code.Animals;
using Code.Animals.Merge.Services;
using Code.Animals.UI;
using Code.Battle;
using Code.Battle.Config;
using Code.Battle.Input;
using Code.Battle.PreBattle;
using Code.Battle.PreBattle.Rules;
using Code.Battle.Services;
using Code.Battle.Signals;
using Code.Battle.StateMachine;
using Code.Battle.UI;
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
        [SerializeField] private PreBattleConfig _preBattleConfig;
        [SerializeField] private AddAnimalButtonView _addAnimalButton;
        [SerializeField] private BattleButtonView _battleButton;
        [SerializeField] private AnimalStatsPanelView _statsPanelPrefab;

        public override void InstallBindings()
        {
            ValidateSceneReferences();

            DeclareSignals();
            BindServices();
            BindPreBattle();
            BindAnimalStatsPanel();
            BindBattleStateMachine();
            BindBattleFlowController();
        }

        private void DeclareSignals()
        {
            Container.DeclareSignal<PreBattlePhaseStartedSignal>().OptionalSubscriber();
            Container.DeclareSignal<PreBattlePhaseEndedSignal>().OptionalSubscriber();
            Container.DeclareSignal<AllySpawnedSignal>().OptionalSubscriber();
            Container.DeclareSignal<BattleReadinessChangedSignal>().OptionalSubscriber();
            Container.DeclareSignal<AllyMergedSignal>().OptionalSubscriber();
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
            Container.Bind<IBattleResetService>().To<BattleResetService>().AsSingle();

            // Merge undo service
            Container.Bind<IMergeUndoService>().To<MergeUndoService>().AsSingle();

            // Battle start input system
            Container.Bind<StartBattleService>().AsSingle();

            // Debug helpers - only in Unity Editor
#if UNITY_EDITOR
            Container.Bind<KeyboardStartBattleHelper>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("KeyboardStartBattleHelper (Debug)")
                .AsSingle()
                .NonLazy();

            Container.Bind<KeyboardMergeUndoHelper>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("KeyboardMergeUndoHelper (Debug)")
                .AsSingle()
                .NonLazy();

            // Battle debug commands
            Container.Bind<BattleDebugCommands>()
                .AsSingle();

            Container.Bind<KeyboardBattleDebugHelper>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("KeyboardBattleDebugHelper (Debug)")
                .AsSingle()
                .NonLazy();
#endif

            // MonoBehaviour dependencies from scene
            Container.Bind<TargetFinder>().FromInstance(_targetFinder).AsSingle();
            Container.BindInterfacesAndSelfTo<AnimalSpawner>().FromInstance(_animalSpawner).AsSingle();
        }

        private void BindPreBattle()
        {
            Container.Bind<PreBattleConfig>().FromInstance(_preBattleConfig).AsSingle();

            Container.Bind<IAllySpawnPool>().To<AllySpawnPool>().AsSingle();
            Container.Bind<IAllySpawnService>().To<AllySpawnService>().AsSingle();

            Container.BindInterfacesTo<PoolExhaustedRule>().AsSingle().NonLazy();

            Container.BindInterfacesTo<MinAllyCountRule>().AsSingle().NonLazy();
            Container.BindInterfacesTo<BattleReadinessService>().AsSingle();

            Container.Bind<AddAnimalButtonView>().FromInstance(_addAnimalButton).AsSingle();
            Container.Bind<BattleButtonView>().FromInstance(_battleButton).AsSingle();
            Container.BindInterfacesAndSelfTo<PreBattleHudPresenter>().AsSingle().NonLazy();
        }

        private void BindAnimalStatsPanel()
        {
            Container.Bind<IAnimalStatsPanelView>()
                .To<AnimalStatsPanelView>()
                .FromComponentInNewPrefab(_statsPanelPrefab)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<AnimalStatsPanelPresenter>().AsSingle().NonLazy();
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

        private void ValidateSceneReferences()
        {
            if (_preBattleConfig == null)
                Debug.LogError($"[BattleInstaller] {nameof(_preBattleConfig)} is not assigned - assign Assets/Settings/BattleConfigs/PreBattleConfig.asset", this);

            if (_addAnimalButton == null)
                Debug.LogError($"[BattleInstaller] {nameof(_addAnimalButton)} is not assigned - assign the Add Animal button from the scene", this);

            if (_battleButton == null)
                Debug.LogError($"[BattleInstaller] {nameof(_battleButton)} is not assigned - assign the Battle button from the scene", this);

            if (_statsPanelPrefab == null)
                Debug.LogError($"[BattleInstaller] {nameof(_statsPanelPrefab)} is not assigned - assign Assets/Prefabs/AnimalStatsPanelView.prefab", this);
        }
    }
}
