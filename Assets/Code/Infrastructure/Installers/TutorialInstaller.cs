using Code.Tutorial;
using Code.Tutorial.Config;
using Code.Tutorial.Progress;
using Code.Tutorial.Signals;
using Code.Tutorial.Steps;
using Code.Tutorial.UI;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class TutorialInstaller : MonoInstaller
    {
        [SerializeField] private MergeHintStepConfig _mergeHintConfig;
        [SerializeField] private TutorialHandView _handViewPrefab;

        public override void InstallBindings()
        {
            ValidateSceneReferences();

            DeclareSignals();
            BindProgress();
            BindView();
            BindSteps();
            BindRunner();
        }

        private void DeclareSignals()
        {
            Container.DeclareSignal<TutorialStepCompletedSignal>().OptionalSubscriber();
        }

        private void BindProgress()
        {
            Container.Bind<ITutorialProgressService>().To<PlayerPrefsTutorialProgressService>().AsSingle();
        }

        private void BindView()
        {
            Container.Bind<ITutorialHandView>()
                .To<TutorialHandView>()
                .FromComponentInNewPrefab(_handViewPrefab)
                .AsSingle();
        }

        private void BindSteps()
        {
            Container.Bind<MergeHintStepConfig>().FromInstance(_mergeHintConfig).AsSingle();

            Container.Bind<IMergeHintTargetResolver>().To<SpawnOrderTargetResolver>().AsSingle();
            Container.Bind<IMergeHintTargetResolver>().To<AnimalTypeTargetResolver>().AsSingle();

            Container.Bind<ITutorialStep>().To<MergeHintStep>().AsSingle();
        }

        private void BindRunner()
        {
            Container.BindInterfacesTo<TutorialRunner>().AsSingle().NonLazy();
        }

        private void ValidateSceneReferences()
        {
            if (_mergeHintConfig == null)
                Debug.LogError($"[TutorialInstaller] {nameof(_mergeHintConfig)} is not assigned - assign Assets/Settings/TutorialConfigs/MergeHintStepConfig.asset", this);

            if (_handViewPrefab == null)
                Debug.LogError($"[TutorialInstaller] {nameof(_handViewPrefab)} is not assigned - assign Assets/Prefabs/TutorialHandView.prefab", this);
        }
    }
}
