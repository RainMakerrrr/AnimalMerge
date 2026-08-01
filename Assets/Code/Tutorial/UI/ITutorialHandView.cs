using UnityEngine;

namespace Code.Tutorial.UI
{
    public interface ITutorialHandView
    {
        void Show(Transform source, Transform target);

        void Hide();
    }
}
