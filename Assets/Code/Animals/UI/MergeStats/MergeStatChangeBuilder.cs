using System.Collections.Generic;
using Code.Animals.Merge;
using UnityEngine;

namespace Code.Animals.UI.MergeStats
{
    public static class MergeStatChangeBuilder
    {
        public static IReadOnlyList<MergeStatChange> Build(MergeStatValues before, MergeStatValues after)
        {
            var changes = new List<MergeStatChange>(3);

            TryAdd(changes, MergeStatKind.Health, before.MaxHealth, after.MaxHealth,
                before.CurrentHealth, after.CurrentHealth);
            TryAdd(changes, MergeStatKind.Damage, before.Damage, after.Damage,
                before.Damage, after.Damage);
            TryAdd(changes, MergeStatKind.Moves, before.TilesPerMove, after.TilesPerMove,
                before.TilesPerMove, after.TilesPerMove);

            return changes;
        }

        private static void TryAdd(List<MergeStatChange> changes, MergeStatKind kind,
            float percentBase, float percentTarget, float displayedFrom, float displayedTo)
        {
            if (percentBase <= 0f)
                return;

            var percent = Mathf.RoundToInt((percentTarget / percentBase - 1f) * 100f);

            if (percent == 0)
                return;

            changes.Add(new MergeStatChange(kind, ToStatValue(displayedFrom), ToStatValue(displayedTo), percent));
        }

        private static int ToStatValue(float value) => Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
