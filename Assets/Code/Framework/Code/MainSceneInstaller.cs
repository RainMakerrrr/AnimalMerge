using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.SaveSystem;
using Framework.Code.Infrastructure.Signals;
using Framework.Code.Infrastructure.States;
using Framework.Code.UI;
using Framework.Code.UI.Elements;
using UnityEngine;
using Zenject;

namespace Framework.Code
{
	public class MainSceneInstaller : MonoInstaller
	{
		[SerializeField] UIRoot uiRoot;
		[SerializeField] WindowHolder windowHolder;

		public override void InstallBindings()
		{
			RegisterSignalBus();
			BindAssetProvider();
			BindSaveLoadService();
			BindPersistentProgressService();
			BindAnalyticsService();
			BindFactories();
			BindUI();
			BindGameStateMachine();
		}


		void RegisterSignalBus()
		{
			SignalBusInstaller.Install(Container);
			Container.DeclareSignal<StateChangedSignal>();
		}
		
		void BindAssetProvider() => Container.Bind<IAssetProvider>().To<AssetProvider>().AsSingle();

		void BindSaveLoadService() => Container.Bind<ISaveLoadService>().To<SaveLoadService>().AsSingle();

		void BindPersistentProgressService() =>
			Container.Bind<IPersistentProgressService>().To<PersistentProgressService>().AsSingle();


		void BindAnalyticsService() => Container.Bind<IAnalyticsService>().To<AnalyticsService>().AsSingle();

		void BindFactories() => Container.Bind<ILevelFactory>().To<LevelFactory>().AsSingle();

		void BindUI()
		{
			Container.Bind<UIRoot>().FromInstance(uiRoot).AsSingle();
			Container.Bind<WindowHolder>().FromInstance(windowHolder).AsSingle();
			Container.Bind<WindowPool>().FromNew().AsSingle();
		}

		void BindGameStateMachine() =>
			Container.Bind<GameStateMachine>().FromNew().AsSingle();
	}
}