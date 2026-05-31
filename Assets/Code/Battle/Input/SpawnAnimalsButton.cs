using Code.Animals;
using Code.Battle.Services;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Battle.Input
{
    [RequireComponent(typeof(Button))]
    public class SpawnAnimalsButton : MonoBehaviour
    {
        private AnimalSpawner _animalSpawner;
        private IUnitTracker _unitTracker;
        private Button _button;

        [Inject]
        private void Construct(AnimalSpawner animalSpawner, IUnitTracker unitTracker)
        {
            _animalSpawner = animalSpawner;
            _unitTracker = unitTracker;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy() => _button.onClick.RemoveListener(OnButtonClick);

        public void Show() => gameObject.SetActive(true);

        private void OnButtonClick()
        {
            gameObject.SetActive(false);
            _animalSpawner.SpawnAnimals();
            foreach (var unit in _animalSpawner.Animals)
                _unitTracker.RegisterPlayerUnit(unit);
            Debug.Log($"[SpawnAnimalsButton] Spawned and registered {_animalSpawner.Animals.Count} units");
        }
    }
}
