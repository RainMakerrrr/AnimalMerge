using System;
using System.Collections.Generic;
using Code.Animals;
using UnityEngine;

namespace Code.Data.Animals
{
    [Serializable]
    public class AnimalConfig
    {
        public AnimalType Type;
        public AnimalStats Stats;
    }

    [CreateAssetMenu(fileName = "AnimalDatabase", menuName = "Data/Animal Database")]
    public class AnimalDatabase : ScriptableObject
    {
        [SerializeField] private List<AnimalConfig> _configs;

        public AnimalStats GetStats(AnimalType type)
        {
            foreach (var config in _configs)
            {
                if (config.Type == type)
                    return config.Stats;
            }

            Debug.LogWarning($"[AnimalDatabase] No stats found for {type}");
            return null;
        }
    }
}
