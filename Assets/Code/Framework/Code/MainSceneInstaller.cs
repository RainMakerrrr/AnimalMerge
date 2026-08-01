using Framework.Code.Data;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.GameRestart;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.Progression;
using Framework.Code.Infrastructure.Services.SaveSystem;
using Framework.Code.Infrastructure.Signals;
using Framework.Code.Infrastructure.States;
using Framework.Code.UI;
using Framework.Code.UI.Elements;
using Framework.Code.UI.Windows;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Framework.Code
{
	public class MainSceneInstaller : MonoInstaller
	{
		[FormerlySerializedAs("uiRoot")] [SerializeField] private UIRoot _uiRoot;
		[FormerlySerializedAs("windowHolder")] [SerializeField] private WindowHolder _windowHolder;
		[SerializeField] private DefeatWindowView _defeatWindow;
		[SerializeField] private CampaignVictoryWindowView _campaignVictoryWindow;

		public override void InstallBindings()
		{
			ValidateSceneReferences();

			RegisterSignalBus();
			BindAssetProvider();
			BindGameData();
			BindSaveLoadService();
			BindPersistentProgressService();
			BindAnalyticsService();
			BindFactories();
			BindProgression();
			BindUI();
			BindGameRestart();
			BindGameStateMachine();
		}

		private void RegisterSignalBus()
		{
			SignalBusInstaller.Install(Container);
			Container.DeclareSignal<StateChangedSignal>();
		}

		private void BindAssetProvider() => Container.Bind<IAssetProvider>().To<AssetProvider>().AsSingle();

		private void BindGameData() =>
			Container.Bind<GameData>()
				.FromMethod(context => context.Container.Resolve<IAssetProvider>().Load<GameData>(AssetPath.GAME_DATA))
				.AsSingle();

		private void BindSaveLoadService() => Container.Bind<ISaveLoadService>().To<SaveLoadService>().AsSingle();

		private void BindPersistentProgressService() =>
			Container.Bind<IPersistentProgressService>().To<PersistentProgressService>().AsSingle();

		private void BindAnalyticsService() => Container.Bind<IAnalyticsService>().To<AnalyticsService>().AsSingle();

		private void BindFactories() => Container.Bind<ILevelFactory>().To<LevelFactory>().AsSingle();

		private void BindProgression() =>
			Container.Bind<ICampaignProgressService>().To<CampaignProgressService>().AsSingle();

		private void BindUI()
		{
			Container.Bind<UIRoot>().FromInstance(_uiRoot).AsSingle();
			Container.Bind<WindowHolder>().FromInstance(_windowHolder).AsSingle();
			Container.Bind<WindowPool>().FromNew().AsSingle();

			Container.Bind<DefeatWindowView>().FromInstance(_defeatWindow).AsSingle();
			Container.Bind<CampaignVictoryWindowView>().FromInstance(_campaignVictoryWindow).AsSingle();
			Container.BindInterfacesAndSelfTo<GameResultPresenter>().AsSingle().NonLazy();
		}

		private void BindGameRestart() =>
			Container.Bind<IGameRestartService>().To<GameRestartService>().AsSingle();

		private void BindGameStateMachine() =>
			Container.Bind<GameStateMachine>().FromNew().AsSingle();

		private void ValidateSceneReferences()
		{
			if (_uiRoot == null)
				Debug.LogError($"[MainSceneInstaller] {nameof(_uiRoot)} is not assigned", this);

			if (_windowHolder == null)
				Debug.LogError($"[MainSceneInstaller] {nameof(_windowHolder)} is not assigned", this);

			if (_defeatWindow == null)
				Debug.LogError($"[MainSceneInstaller] {nameof(_defeatWindow)} is not assigned - assign the Lose panel from the Canvas", this);

			if (_campaignVictoryWindow == null)
				Debug.LogError($"[MainSceneInstaller] {nameof(_campaignVictoryWindow)} is not assigned - assign the Victory panel from the Canvas", this);
		}
	}
}
