using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class VisualMergeAttribute : MonoBehaviour
    {
        [SerializeField] private AnimalType _type;
        public AnimalType Type => _type;

        public virtual void Apply()
        {
            Debug.Log($"[VisualMergeAttribute] Applying visual for {_type} on {gameObject.name}");
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Undoes the visual effect applied by this attribute
        /// </summary>
        public virtual void Undo()
        {
            Debug.Log($"[VisualMergeAttribute] Undoing visual for {_type} on {gameObject.name}, was active: {gameObject.activeSelf}");
            gameObject.SetActive(false);
            Debug.Log($"[VisualMergeAttribute] Visual {gameObject.name} is now active: {gameObject.activeSelf}");
        }
    }
}