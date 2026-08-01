using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using UnityEngine;
using Zenject;

namespace Framework.Code.Factories.Levels
{
	public class LevelFactory : ILevelFactory
	{
		private readonly DiContainer _diContainer;
		private readonly IPersistentProgressService _progressService;
		private readonly IAssetProvider _assetProvider;

		private string[] _tutorialLevels;
		private string[] _levels;
		private LevelDataBase _levelDataBase;

		public Level CurrentLevel { get; private set; }

		public int TotalLevelsCount
		{
			get
			{
				EnsureLoaded();

				return TutorialLevelsCount + RegularLevelsCount;
			}
		}

		private int TutorialLevelsCount => _tutorialLevels?.Length ?? 0;

		private int RegularLevelsCount => _levels?.Length ?? 0;

		public LevelFactory(DiContainer diContainer, IPersistentProgressService progressService,
			IAssetProvider assetProvider)
		{
			_diContainer = diContainer;
			_progressService = progressService;
			_assetProvider = assetProvider;
		}

		public void Load()
		{
			_levelDataBase = _assetProvider.Load<LevelDataBase>(AssetPath.LEVELS_DATABASE);
			_tutorialLevels = _levelDataBase.TutorialLevels;
			_levels = _levelDataBase.Levels;
		}

		public Level Create()
		{
			EnsureLoaded();

			if (CurrentLevel == null)
			{
				var existingLevel = Object.FindObjectOfType<Level>();
				if (existingLevel != null)
				{
					Debug.Log("Level already in scene");

					CurrentLevel = existingLevel;
					return CurrentLevel;
				}
			}

			if (TotalLevelsCount == 0)
			{
				Debug.LogError("No levels loaded");
				CurrentLevel = null;
				return null;
			}

			Level level = LoadCurrentLevel();

			if (level == null)
			{
				CurrentLevel = null;
				return null;
			}

			CurrentLevel =
				_diContainer.InstantiatePrefabForComponent<Level>(level, Vector3.zero, Quaternion.identity, null);
			return CurrentLevel;
		}

		private void EnsureLoaded()
		{
			if (_levelDataBase == null)
				Load();
		}

		private Level LoadCurrentLevel()
		{
			int requestedLevel = _progressService.Progress.Level;

			if (requestedLevel > TotalLevelsCount)
			{
				Debug.LogError($"Level {requestedLevel} is out of range, total levels: {TotalLevelsCount}");
				return null;
			}

			if (requestedLevel <= TutorialLevelsCount)
				return _assetProvider.Load<Level>(
					$"{AssetPath.TUTORIAL_LEVELS}/{_tutorialLevels[requestedLevel - 1]}");

			int levelIndex = requestedLevel - TutorialLevelsCount - 1;

			return _assetProvider.Load<Level>($"{AssetPath.LEVELS}/{_levels[levelIndex]}");
		}
	}
}
