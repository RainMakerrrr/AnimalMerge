using System.Threading;
using Code.Animals.Facades;
using Code.Animals.Merge;
using Code.Data.Animals;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Code.Animals.UI
{
    public class MergePopupController : MonoBehaviour
    {
        private static readonly Color GainColor = new Color(0.431f, 0.949f, 0.078f);

        [SerializeField] private MergeTarget _target;
        [SerializeField] private MergePopupView _popupPrefab;
        [SerializeField] private float _spawnOffsetY = 1.5f;
        [SerializeField] private float _randomXRange = 0.3f;

        private AnimalDatabase _database;
        private Canvas _canvas;
        private Camera _camera;
        private CancellationTokenSource _popupCts;

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

            RecreatePopupCts();

            _target.Merged += OnMerged;
            _target.MergeUndone += OnMergeUndone;
        }

        private void OnDisable()
        {
            if (_target == null) return;

            _target.Merged -= OnMerged;
            _target.MergeUndone -= OnMergeUndone;

            DisposePopupCts();
        }

        private void OnMerged(MergeOutcome outcome)
        {
            if (outcome.GrantedSkill == false || _popupCts == null) return;

            PlaySkillCalloutAsync(outcome.Source, _popupCts.Token).Forget();
        }

        private void OnMergeUndone() => RecreatePopupCts();

        private UniTask PlaySkillCalloutAsync(PlayerAnimalFacade source, CancellationToken cancellationToken)
        {
            if (_database == null || source == null || _popupPrefab == null) return UniTask.CompletedTask;
            if (_canvas == null || _camera == null) return UniTask.CompletedTask;

            var text = _database.GetMergeInfo(source.Type);
            if (string.IsNullOrEmpty(text)) return UniTask.CompletedTask;

            var worldPos = transform.position + new Vector3(
                Random.Range(-_randomXRange, _randomXRange), _spawnOffsetY, 0f);

            var screenPos = _camera.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) return UniTask.CompletedTask;

            var popup = Instantiate(_popupPrefab, _canvas.transform);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPos,
                null,
                out var localPos);

            popup.GetComponent<RectTransform>().anchoredPosition = localPos;
            return popup.PlayAsync(text, GainColor, cancellationToken);
        }

        private void RecreatePopupCts()
        {
            DisposePopupCts();
            _popupCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        }

        private void DisposePopupCts()
        {
            if (_popupCts == null) return;

            _popupCts.Cancel();
            _popupCts.Dispose();
            _popupCts = null;
        }
    }
}
