using System;
using System.Collections.Generic;

namespace Code.Levels
{
    public interface ILevelSetProvider
    {
        LevelSet ActiveSet { get; }
        IReadOnlyList<LevelSet> AvailableSets { get; }
        event Action<LevelSet> ActiveSetChanged;
        void SetActiveSet(LevelSet set);
    }
}
