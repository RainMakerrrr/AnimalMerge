using DG.Tweening;

namespace Code.Battle.UI.Vignette
{
    public interface IBattleVignetteView
    {
        void FadeIn(float delay, float duration, Ease ease, float alpha);
        void FadeOut(float duration, Ease ease);
        void HideImmediate();
    }
}
