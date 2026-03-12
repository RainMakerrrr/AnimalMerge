#if UNITY_EDITOR
using Code.Animals.Merge.Services;
using UnityEngine;
using Zenject;

namespace Code.Battle.Input
{
    /// <summary>
    /// Debug helper for testing merge undo functionality via keyboard shortcuts
    /// Only available in Unity Editor builds
    /// </summary>
    public class KeyboardMergeUndoHelper : MonoBehaviour
    {
        private IMergeUndoService _mergeUndoService;

        [Inject]
        private void Construct(IMergeUndoService mergeUndoService)
        {
            _mergeUndoService = mergeUndoService;
        }

        private void Update()
        {
            // Ctrl+Z: Undo last merge
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKeyDown(KeyCode.Z))
            {
                if (_mergeUndoService.IsEnabled)
                {
                    Debug.Log("[KeyboardMergeUndoHelper] Ctrl+Z pressed - undoing last merge");
                    var success = _mergeUndoService.UndoLastMerge();

                    if (success)
                    {
                        Debug.Log($"[KeyboardMergeUndoHelper] Undo successful. Remaining commands: {_mergeUndoService.StackCount}");
                    }
                    else
                    {
                        Debug.LogWarning("[KeyboardMergeUndoHelper] Undo failed or stack was empty");
                    }
                }
                else
                {
                    Debug.LogWarning("[KeyboardMergeUndoHelper] Cannot undo - merge undo service is disabled (not in pre-battle state?)");
                }
            }
            // Ctrl+Shift+Z: Show stack debug info
            else if (UnityEngine.Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log($"[KeyboardMergeUndoHelper] === Merge Undo Stack Info ===");
                Debug.Log($"[KeyboardMergeUndoHelper] Enabled: {_mergeUndoService.IsEnabled}");
                Debug.Log($"[KeyboardMergeUndoHelper] Stack Count: {_mergeUndoService.StackCount}");
                Debug.Log($"[KeyboardMergeUndoHelper] ==============================");
            }
        }
    }
}
#endif
