using Code.Animals.Merge.Services;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Framework.Code.UI.Elements
{
    /// <summary>
    /// UI button for undoing the last merge operation.
    /// Only active during PreBattleState when merge undo is enabled.
    /// </summary>
    public class UndoMergeButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private IMergeUndoService _mergeUndoService;

        [Inject]
        private void Construct(IMergeUndoService mergeUndoService)
        {
            _mergeUndoService = mergeUndoService;
        }

        private void Start()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            _button.onClick.AddListener(OnUndoButtonClicked);

            // Subscribe to stack count changes to update button state
            _mergeUndoService.OnStackCountChanged += UpdateButtonState;
            _mergeUndoService.OnEnabledStateChanged += UpdateButtonVisibility;

            // Initialize button state
            UpdateButtonVisibility(_mergeUndoService.IsEnabled);
            UpdateButtonState(_mergeUndoService.StackCount);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnUndoButtonClicked);
            }

            if (_mergeUndoService != null)
            {
                _mergeUndoService.OnStackCountChanged -= UpdateButtonState;
                _mergeUndoService.OnEnabledStateChanged -= UpdateButtonVisibility;
            }
        }

        private void OnUndoButtonClicked()
        {
            if (_mergeUndoService.IsEnabled && _mergeUndoService.StackCount > 0)
            {
                _mergeUndoService.UndoLastMerge();
            }
        }

        private void UpdateButtonState(int stackCount)
        {
            if (_button != null)
            {
                _button.interactable = stackCount > 0;
            }
        }

        private void UpdateButtonVisibility(bool isEnabled)
        {
            if (_button != null)
            {
                _button.gameObject.SetActive(isEnabled);
            }
        }
    }
}
