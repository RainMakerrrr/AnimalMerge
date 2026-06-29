using Code.Animals.Facades;
using Code.Animals.Merge;
using Code.Data.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals.UI
{
    public class MergePopupController : MonoBehaviour
    {
        [SerializeField] private MergeTarget _target;
        [SerializeField] private MergePopupView _popupPrefab;
        [SerializeField] private float _spawnOffsetY = 1.5f;
        [SerializeField] private float _randomXRange = 0.3f;

        private static readonly Color GainColor = new Color(0.3f, 1f, 0.4f);

        private AnimalDatabase _database;
        private Canvas _canvas;
        private Camera _camera;

        [Inject]
        private void Construct(AnimalDatabase database, Canvas canvas, Camera camera)
        {
            _database = database;
            _canvas = canvas;
            _camera = camera;
        }

        private void OnEnable()
        {
            if (_target == null) return;
            _target.Merged += OnMerged;
        }

        private void OnDisable()
        {
            if (_target == null) return;
            _target.Merged -= OnMerged;
        }

        private void OnMerged(PlayerAnimalFacade source)
        {
            if (_database == null || source == null) return;

            var text = _database.GetMergeInfo(source.Type);
            if (string.IsNullOrEmpty(text)) return;

            SpawnPopup(text, GainColor);
        }

        private void SpawnPopup(string text, Color color)
        {
            if (_canvas == null || _camera == null) return;

            var worldPos = transform.position + new Vector3(
                Random.Range(-_randomXRange, _randomXRange), _spawnOffsetY, 0f);

            var screenPos = _camera.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) return;

            var popup = Instantiate(_popupPrefab, _canvas.transform);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPos,
                null,
                out var localPos);

            popup.GetComponent<RectTransform>().anchoredPosition = localPos;
            popup.Play(text, color);
        }
    }
}
