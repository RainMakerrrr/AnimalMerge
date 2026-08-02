#if UNITY_EDITOR
using Code.GridPathfinding;

namespace Code.Editor.AnimalPrefabBuilder
{
    public enum AnimalFootprint
    {
        Small1x1,
        Medium1x2,
        Large2x2
    }

    public static class AnimalFootprintExtensions
    {
        public static UnitSize ToUnitSize(this AnimalFootprint footprint)
        {
            switch (footprint)
            {
                case AnimalFootprint.Medium1x2:
                    return UnitSize.Medium;
                case AnimalFootprint.Large2x2:
                    return UnitSize.Large;
                default:
                    return UnitSize.Small;
            }
        }

        public static bool IsSupportedByGridStack(UnitSize size)
        {
            return size == UnitSize.Small || size == UnitSize.Medium || size == UnitSize.Large;
        }
    }
}
#endif
