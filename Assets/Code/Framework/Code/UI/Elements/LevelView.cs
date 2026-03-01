using Framework.Code.Infrastructure.Services.PersistentProgress;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Framework.Code.UI.Elements
{
	public class LevelView : MonoBehaviour, IViewUpdatable
	{
		[SerializeField] TextMeshProUGUI levelText;
		
		IPersistentProgressService progressService;

		[Inject]
		void Construct(IPersistentProgressService progressService)
		{
			this.progressService = progressService;
		}

		void OnEnable() => levelText.text = $"Level {progressService.Progress.Level.ToString()}";

		public void UpdateView() => levelText.text = $"Level {progressService.Progress.Level.ToString()}";
	}
}