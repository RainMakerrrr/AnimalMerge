using System;
using Code.Animals;
using UnityEngine;

namespace Code.Battle.Config
{
    [Serializable]
    public class AnimalUnlockEntry
    {
        [SerializeField, Min(1)] private int _completedLevel;
        [SerializeField] private AnimalType _animal;

        public AnimalUnlockEntry()
        {
            _completedLevel = 1;
        }

        public AnimalUnlockEntry(int completedLevel, AnimalType animal)
        {
            _completedLevel = completedLevel;
            _animal = animal;
        }

        public int CompletedLevel => _completedLevel;
        public AnimalType Animal => _animal;
    }
}
