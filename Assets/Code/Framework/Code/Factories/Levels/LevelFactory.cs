using Code.Levels;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using UnityEngine;
using Zenject;

namespace Framework.Code.Factories.Levels
{
	public class LevelFactory : ILevelFactory
	{
		private readonly DiContainer _diContainer;
		private readonly IPersistentProgressService _progressService;
		private readonly ILevelSetProvider _levelSetProvider;

		private LevelSet _activeSet;

		public Level CurrentLevel { get; private set; }

		public int TotalLevelsCount
		{
			get
			{
				EnsureLoaded();

				return TutorialLevelsCount + RegularLevelsCount;
			}
		}

		private int TutorialLevelsCount => _activeSet == null ? 0 : _activeSet.TutorialLevels.Count;

		private int RegularLevelsCount => _activeSet == null ? 0 : _activeSet.Levels.Count;

		public LevelFactory(DiContainer diContainer, IPersistentProgressService progressService,
			ILevelSetProvider levelSetProvider)
		{
			_diContainer = diContainer;
			_progressService = progressService;
			_levelSetProvider = levelSetProvider;
		}

		public void Load()
		{
			_activeSet = _levelSetProvider.ActiveSet;

			if (_activeSet == null)
				Debug.LogError(
					$"No active level set. Create Assets/Resources/{AssetPath.LEVEL_SET_LIBRARY}.asset " +
					"and pick a default set in Tools/AnimalMerge/Level Sets");
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
			if (_activeSet != _levelSetProvider.ActiveSet)
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
				return _activeSet.TutorialLevels[requestedLevel - 1];

			int levelIndex = requestedLevel - TutorialLevelsCount - 1;

			return _activeSet.Levels[levelIndex];
		}
	}
}
