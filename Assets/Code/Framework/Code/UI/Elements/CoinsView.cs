using Framework.Code.Infrastructure.Services.PersistentProgress;
using TMPro;
using UnityEngine;
using Zenject;

namespace Framework.Code.UI.Elements
{
	public class CoinsView : MonoBehaviour, IViewUpdatable
	{
		[SerializeField] TextMeshProUGUI coinsText;

		IPersistentProgressService progressService;

		[Inject]
		void Construct(IPersistentProgressService progressService)
		{
			this.progressService = progressService;
		}

		void OnEnable()
		{
			progressService.Progress.Collectables.AmountChanged += OnCoinsChanged;
			coinsText.text = progressService.Progress.Collectables.Amount.ToString();
		}

		public void UpdateView() => OnCoinsChanged();

		void OnCoinsChanged() => coinsText.text = progressService.Progress.Collectables.Amount.ToString();

		void OnDisable() => progressService.Progress.Collectables.AmountChanged -= OnCoinsChanged;
	}
}