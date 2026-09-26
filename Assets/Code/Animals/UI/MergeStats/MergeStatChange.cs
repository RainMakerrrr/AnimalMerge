namespace Code.Animals.UI.MergeStats
{
    public readonly struct MergeStatChange
    {
        public MergeStatChange(MergeStatKind kind, int from, int to, int percent)
        {
            Kind = kind;
            From = from;
            To = to;
            Percent = percent;
        }

        public MergeStatKind Kind { get; }
        public int From { get; }
        public int To { get; }
        public int Percent { get; }
        public bool IsGain => Percent > 0;
    }
}
