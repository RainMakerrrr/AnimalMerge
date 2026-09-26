using System.Collections.Generic;
using System.Threading;
using Code.Animals.UI.MergeStats;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.UI
{
    public interface IAnimalStatsPanelView
    {
        void Show(Transform anchor, Sprite icon, AnimalCardStats stats, string title,
            IReadOnlyList<string> abilityLines);

        void UpdateAbilityLines(IReadOnlyList<string> abilityLines);

        void UpdateStats(AnimalCardStats stats);

        UniTask PlayStatChangesAsync(IReadOnlyList<MergeStatChange> changes, CancellationToken cancellationToken);

        void StopStatChanges();

        void Hide();
    }
}
