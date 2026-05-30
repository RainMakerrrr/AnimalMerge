using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class CheetahMergeAttribute : VisualMergeAttribute
    {
        [SerializeField] private Material _cheetahMaterial;

        private struct RendererState
        {
            public Renderer Renderer;
            public Material OriginalMaterial;
        }

        private RendererState[] _savedState;

        public override void Apply(Transform target)
        {
            base.Apply(target);
            var renderers = Target.GetComponentsInChildren<Renderer>();
            _savedState = new RendererState[renderers.Length];

            for (var i = 0; i < renderers.Length; i++)
            {
                Debug.Log($"[CheetahMergeAttribute] apply material for {renderers[i]}");

                _savedState[i] = new RendererState
                {
                    Renderer = renderers[i],
                    OriginalMaterial = renderers[i].material
                };
                renderers[i].material = _cheetahMaterial;
            }

            Debug.Log(
                $"[CheetahMergeAttribute] Applied cheetah material to {renderers.Length} renderers on {Target.name}");
        }

        public override void Undo()
        {
            if (_savedState != null)
            {
                foreach (var state in _savedState)
                {
                    if (state.Renderer != null)
                        state.Renderer.material = state.OriginalMaterial;
                }

                _savedState = null;
            }

            Debug.Log($"[CheetahMergeAttribute] Restored original materials");
            base.Undo();
        }
    }
}
