using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using UnityEngine;
using Zenject;

namespace Framework.Code.Factories.Levels
{
	public class LevelFactory : ILevelFactory
	{
		readonly DiContainer diContainer;
		readonly IPersistentProgressService progressService;
		readonly IAssetProvider assetProvider;

		string[] tutorialLevels;
		string[] levels;
		LevelDataBase levelDataBase;

		public Level CurrentLevel { get; private set; }

		public LevelFactory(DiContainer diContainer, IPersistentProgressService progressService,
			IAssetProvider assetProvider)
		{
			this.diContainer = diContainer;
			this.progressService = progressService;
			this.assetProvider = assetProvider;
		}

		public void Load()
		{
			levelDataBase = assetProvider.Load<LevelDataBase>(AssetPath.LEVELS_DATABASE);
			tutorialLevels = levelDataBase.TutorialLevels;
			levels = levelDataBase.Levels;
		}

		public Level Create()
		{
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
			
			if (levels.Length == 0 && tutorialLevels.Length == 0)
			{
				Debug.Log("No levels loaded");
				return null;
			}
			

			Level level = LoadCurrentLevel();

			CurrentLevel =
				diContainer.InstantiatePrefabForComponent<Level>(level, Vector3.zero, Quaternion.identity, null);
			return CurrentLevel;
		}

		Level LoadCurrentLevel()
		{
			Level level;
			if (tutorialLevels != null && progressService.Progress.Level <= tutorialLevels.Length)
			{
				level = assetProvider.Load<Level>(
					$"{AssetPath.TUTORIAL_LEVELS}/{tutorialLevels[progressService.Progress.Level - 1]}");
			}
			else
			{
				int index =
					(progressService.Progress.Level - (tutorialLevels != null ? tutorialLevels.Length + 1 : 1)) %
					levels.Length;

				level = assetProvider.Load<Level>($"{AssetPath.LEVELS}/{levels[index]}");
			}

			return level;
		}
	}
}