namespace Code.Animals.Merge.MergeAttributes
{
    public class ElephantMergeAttribute : VisualMergeAttribute
    {
        private const float ScaleFactor = 1.5f;

        public override void Apply()
        {
            transform.root.localScale *= ScaleFactor;
        }
    }
}