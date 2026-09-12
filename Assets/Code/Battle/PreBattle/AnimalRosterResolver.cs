using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Battle.Config;

namespace Code.Battle.PreBattle
{
    public static class AnimalRosterResolver
    {
        private const int FirstLevel = 1;

        public static IReadOnlyList<AnimalType> Resolve(PreBattleConfig config, int currentLevel)
        {
            var roster = new List<AnimalType>();
            var included = new HashSet<AnimalType>();

            if (config == null)
                return roster;

            AppendBaseRoster(config, roster, included);
            AppendUnlockedBefore(config, NormalizeLevel(currentLevel), roster, included);

            return roster;
        }

        public static IReadOnlyList<AnimalType> ResolveNewlyUnlocked(PreBattleConfig config, int currentLevel)
        {
            var newlyUnlocked = new List<AnimalType>();

            if (config == null || config.Unlocks == null)
                return newlyUnlocked;

            int level = NormalizeLevel(currentLevel);
            int lastCompletedLevel = level - 1;

            if (lastCompletedLevel < FirstLevel)
                return newlyUnlocked;

            var availableBefore = new HashSet<AnimalType>(Resolve(config, lastCompletedLevel));

            foreach (var entry in config.Unlocks)
            {
                if (entry.CompletedLevel != lastCompletedLevel)
                    continue;

                if (availableBefore.Add(entry.Animal))
                    newlyUnlocked.Add(entry.Animal);
            }

            return newlyUnlocked;
        }

        private static void AppendBaseRoster(PreBattleConfig config, List<AnimalType> roster, HashSet<AnimalType> included)
        {
            if (config.BaseRoster == null)
                return;

            foreach (var animal in config.BaseRoster)
            {
                if (included.Add(animal))
                    roster.Add(animal);
            }
        }

        private static void AppendUnlockedBefore(
            PreBattleConfig config,
            int currentLevel,
            List<AnimalType> roster,
            HashSet<AnimalType> included)
        {
            if (config.Unlocks == null)
                return;

            var unlocked = config.Unlocks
                .Where(entry => entry.CompletedLevel < currentLevel)
                .OrderBy(entry => entry.CompletedLevel);

            foreach (var entry in unlocked)
            {
                if (included.Add(entry.Animal))
                    roster.Add(entry.Animal);
            }
        }

        private static int NormalizeLevel(int level) => level < FirstLevel ? FirstLevel : level;
    }
}
