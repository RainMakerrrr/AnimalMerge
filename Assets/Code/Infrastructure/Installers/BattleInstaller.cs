using Code.Animals;
using Code.Animals.Health;
using Code.Animals.Merge.Services;
using Code.Animals.UI;
using Code.Battle;
using Code.Battle.CameraControl;
using Code.Battle.Config;
using Code.Battle.Input;
using Code.Battle.PreBattle;
using Code.Battle.PreBattle.Rules;
using Code.Battle.Selection;
using Code.Battle.Services;
using Code.Battle.Signals;
using Code.Battle.StateMachine;
using Code.Battle.UI;
using Code.Battle.UI.StageBanner;
using Code.Battle.Vfx;
using Code.Levels;
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
        [SerializeField] private BattleCameraConfig _battleCameraConfig;
        [SerializeField] private HealthBarConfig _healthBarConfig;
        [SerializeField] private BattleVfxConfig _battleVfxConfig;
        [SerializeField] private AddAnimalButtonView _addAnimalButton;
        [SerializeField] private BattleButtonView _battleButton;
        [SerializeField] private AnimalStatsPanelView _statsPanelPrefab;
        [SerializeField] private EnemyCardView _enemyCardPrefab;
        [SerializeField] private TurnOrderView _turnOrderPrefab;
        [SerializeField] private StageBannerConfig _stageBannerConfig;
        [SerializeField] private StageBannerView _stageBannerPrefab;

        public override void InstallBindings()
        {
            ValidateSceneReferences();

            DeclareSignals();
            BindServices();
            BindPreBattle();
            BindBattleCamera();
            BindHealthBar();
            BindBattleVfx();
            BindAbilityLines();
            BindAnimalStatsPanel();
            BindEnemySelection();
            BindEnemyCard();
            BindTurnOrder();
            BindStageBanner();
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
            Container.DeclareSignal<AllyMergeUndoneSignal>().OptionalSubscriber();
            Container.DeclareSignal<TurnRoundStartedSignal>().OptionalSubscriber();
            Container.DeclareSignal<UnitTurnStartedSignal>().OptionalSubscriber();
            Container.DeclareSignal<UnitTurnCompletedSignal>().OptionalSubscriber();
            Container.DeclareSignal<BattleStartedSignal>().OptionalSubscriber();
            Container.DeclareSignal<BattleEndedSignal>().OptionalSubscriber();
            Container.DeclareSignal<EnemiesSpawnedSignal>().OptionalSubscriber();
            Container.DeclareSignal<AoeAttackLandedSignal>().OptionalSubscriber();
        }

        private void BindServices()
        {
            // Core battle services
            Container.BindInterfacesAndSelfTo<UnitTracker>().AsSingle();
            Container.Bind<IEnemySpawnService>().To<EnemySpawnService>().AsSingle();
            Container.Bind<ITurnExecutor>().To<TurnExecutor>().AsSingle();
            Container.BindInterfacesTo<TurnOrderProvider>().AsSingle();
            Container.Bind<IVictoryConditionChecker>().To<VictoryConditionChecker>().AsSingle();
            Container.Bind<IHealthRestorationService>().To<HealthRestorationService>().AsSingle();
            Container.Bind<IUnitRepositioningService>().To<UnitRepositioningService>().AsSingle();
            Container.Bind<IBattleResetService>().To<BattleResetService>().AsSingle();

            // Merge undo service
            Container.Bind<IMergeUndoService>().To<MergeUndoService>().AsSingle();

            // Battle start input system
            Container.Bind<StartBattleService>().AsSingle();
            Container.Bind<IPlayerInputLock>().To<PlayerInputLock>().AsSingle();

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
            Container.Bind<PreBattleConfig>().FromMethod(ResolvePreBattleConfig).AsSingle();

            Container.Bind<IAnimalRosterService>().To<AnimalRosterService>().AsSingle();

            Container.Bind<IAllySpawnPool>().To<AllySpawnPool>().AsSingle();
            Container.Bind<IAllySpawnService>().To<AllySpawnService>().AsSingle();

            Container.BindInterfacesTo<PoolExhaustedRule>().AsSingle().NonLazy();

            Container.BindInterfacesTo<MinAllyCountRule>().AsSingle().NonLazy();
            Container.BindInterfacesTo<BattleReadinessService>().AsSingle();

            Container.Bind<AddAnimalButtonView>().FromInstance(_addAnimalButton).AsSingle();
            Container.Bind<BattleButtonView>().FromInstance(_battleButton).AsSingle();
            Container.BindInterfacesAndSelfTo<PreBattleHudPresenter>().AsSingle().NonLazy();
        }

        private PreBattleConfig ResolvePreBattleConfig(InjectContext context)
        {
            LevelSet activeSet = context.Container.Resolve<ILevelSetProvider>().ActiveSet;
            PreBattleConfig perSetConfig = activeSet == null ? null : activeSet.PreBattleConfig;

            return perSetConfig != null ? perSetConfig : _preBattleConfig;
        }

        private void BindBattleCamera()
        {
            Container.Bind<BattleCameraConfig>().FromInstance(_battleCameraConfig).AsSingle();
            Container.BindInterfacesTo<BattleCameraService>().AsSingle();
            Container.BindInterfacesAndSelfTo<BattleCameraPresenter>().AsSingle().NonLazy();
        }

        private void BindHealthBar() =>
            Container.Bind<HealthBarConfig>().FromInstance(_healthBarConfig).AsSingle();

        private void BindBattleVfx()
        {
            if (_battleVfxConfig == null)
            {
                Debug.LogError($"[BattleInstaller] {nameof(_battleVfxConfig)} is not assigned - assign Assets/Settings/BattleConfigs/BattleVfxConfig.asset", this);
                return;
            }

            Container.Bind<BattleVfxConfig>().FromInstance(_battleVfxConfig).AsSingle();
            Container.Bind<IVfxSpawner>().To<VfxSpawner>().AsSingle();
            Container.BindInterfacesAndSelfTo<UnitSpawnVfxPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<AoeAttackVfxPresenter>().AsSingle().NonLazy();
        }

        private void BindAbilityLines()
        {
            Container.Bind<IAbilityLinesProvider>().To<AbilityLinesProvider>().AsSingle();
        }

        private void BindAnimalStatsPanel()
        {
            Container.Bind<IAnimalStatsPanelView>()
                .To<AnimalStatsPanelView>()
                .FromComponentInNewPrefab(_statsPanelPrefab)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<AnimalStatsPanelPresenter>().AsSingle().NonLazy();
        }

        private void BindEnemySelection()
        {
            Container.BindInterfacesAndSelfTo<EnemySelectionService>().AsSingle();
        }

        private void BindEnemyCard()
        {
            Container.Bind<IEnemyCardView>()
                .To<EnemyCardView>()
                .FromComponentInNewPrefab(_enemyCardPrefab)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<EnemyCardPresenter>().AsSingle().NonLazy();
        }

        private void BindTurnOrder()
        {
            Container.Bind<ITurnOrderView>()
                .To<TurnOrderView>()
                .FromComponentInNewPrefab(_turnOrderPrefab)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<TurnOrderPresenter>().AsSingle().NonLazy();
        }

        private void BindStageBanner()
        {
            if (_stageBannerConfig == null || _stageBannerPrefab == null)
            {
                Container.Bind<IStageAnnouncer>().To<SilentStageAnnouncer>().AsSingle();
                return;
            }

            Container.Bind<StageBannerConfig>().FromInstance(_stageBannerConfig).AsSingle();

            Container.Bind<IStageBannerView>()
                .To<StageBannerView>()
                .FromComponentInNewPrefab(_stageBannerPrefab)
                .AsSingle();

            Container.BindInterfacesTo<StageBannerAnnouncer>().AsSingle();
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

            if (_battleCameraConfig == null)
                Debug.LogError($"[BattleInstaller] {nameof(_battleCameraConfig)} is not assigned - assign Assets/Settings/BattleConfigs/BattleCameraConfig.asset", this);

            if (_healthBarConfig == null)
                Debug.LogError($"[BattleInstaller] {nameof(_healthBarConfig)} is not assigned - assign Assets/Settings/BattleConfigs/HealthBarConfig.asset", this);

            if (_addAnimalButton == null)
                Debug.LogError($"[BattleInstaller] {nameof(_addAnimalButton)} is not assigned - assign the Add Animal button from the scene", this);

            if (_battleButton == null)
                Debug.LogError($"[BattleInstaller] {nameof(_battleButton)} is not assigned - assign the Battle button from the scene", this);

            if (_statsPanelPrefab == null)
                Debug.LogError($"[BattleInstaller] {nameof(_statsPanelPrefab)} is not assigned - assign Assets/Prefabs/AnimalStatsPanelView.prefab", this);

            if (_enemyCardPrefab == null)
                Debug.LogError($"[BattleInstaller] {nameof(_enemyCardPrefab)} is not assigned - assign Assets/Prefabs/EnemyCardView.prefab", this);

            if (_turnOrderPrefab == null)
                Debug.LogError($"[BattleInstaller] {nameof(_turnOrderPrefab)} is not assigned - assign Assets/Prefabs/TurnOrderView.prefab", this);

            if (_stageBannerConfig == null)
                Debug.LogError($"[BattleInstaller] {nameof(_stageBannerConfig)} is not assigned - assign Assets/Settings/BattleConfigs/StageBannerConfig.asset", this);

            if (_stageBannerPrefab == null)
                Debug.LogError($"[BattleInstaller] {nameof(_stageBannerPrefab)} is not assigned - assign Assets/Prefabs/StageBannerView.prefab", this);
        }
    }
}
