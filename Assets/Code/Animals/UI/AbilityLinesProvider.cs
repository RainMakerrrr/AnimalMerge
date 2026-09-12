using System;
using System.Collections.Generic;
using Code.Abilities;
using Code.Animals.Facades;
using UnityEngine;

namespace Code.Animals.UI
{
    public class AbilityLinesProvider : IAbilityLinesProvider
    {
        private const int GuaranteedChancePercent = 100;

        public IReadOnlyList<string> Build(AnimalFacade animal)
        {
            if (animal == null || animal.AbilityManager == null)
                return Array.Empty<string>();

            var kindOrder = new List<AbilityKind>();
            var bestChances = new Dictionary<AbilityKind, int>();

            foreach (var ability in animal.AbilityManager.Abilities)
            {
                var describable = ability as IDescribableAbility;

                if (describable == null)
                {
                    Debug.LogWarning($"[{nameof(AbilityLinesProvider)}] {ability.GetType().Name} does not implement {nameof(IDescribableAbility)} - it is missing from the card of {animal.Type}.");
                    continue;
                }

                var description = describable.Describe();

                if (bestChances.TryGetValue(description.Kind, out var knownChance) == false)
                {
                    kindOrder.Add(description.Kind);
                    bestChances[description.Kind] = description.ChancePercent;
                    continue;
                }

                if (description.ChancePercent > knownChance)
                    bestChances[description.Kind] = description.ChancePercent;
            }

            var lines = new List<string>(kindOrder.Count);

            foreach (var kind in kindOrder)
                lines.Add(BuildLine(kind, bestChances[kind]));

            return lines;
        }

        private string BuildLine(AbilityKind kind, int chancePercent)
        {
            var name = GetDisplayName(kind);

            return chancePercent <= 0 || chancePercent >= GuaranteedChancePercent
                ? name
                : $"{chancePercent}% {name}";
        }

        private string GetDisplayName(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Dodge:
                    return "Dodge";
                case AbilityKind.CounterAttack:
                    return "Counter";
                case AbilityKind.Retreat:
                    return "Retreat";
                default:
                    return kind.ToString();
            }
        }
    }
}
