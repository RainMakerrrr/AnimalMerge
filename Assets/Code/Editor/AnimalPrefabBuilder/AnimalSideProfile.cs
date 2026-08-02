#if UNITY_EDITOR
using System;
using Code.Animals.Facades;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public sealed class AnimalSideProfile
    {
        public static readonly AnimalSideProfile Ally = new AnimalSideProfile(
            "Ally",
            AnimalPrefabConstants.AnimalLayerName,
            AnimalPrefabConstants.EnemyLayerName,
            Direction.North,
            new Color(0.96f, 0.8f, 0.47f, 1f),
            AnimalPrefabPaths.AnimalsFolder,
            "Merge",
            true,
            typeof(PlayerAnimalFacade));

        public static readonly AnimalSideProfile Enemy = new AnimalSideProfile(
            "Enemy",
            AnimalPrefabConstants.EnemyLayerName,
            AnimalPrefabConstants.AnimalLayerName,
            Direction.South,
            new Color(0.2f, 0.5f, 1f, 1f),
            AnimalPrefabPaths.EnemiesFolder,
            "Collider",
            false,
            typeof(EnemyAnimalFacade));

        private AnimalSideProfile(
            string name,
            string ownLayerName,
            string targetLayerName,
            Direction direction,
            Color moveRangeColor,
            string outputFolder,
            string colliderChildName,
            bool colliderIsTrigger,
            Type facadeBaseType)
        {
            Name = name;
            OwnLayerName = ownLayerName;
            TargetLayerName = targetLayerName;
            Direction = direction;
            MoveRangeColor = moveRangeColor;
            OutputFolder = outputFolder;
            ColliderChildName = colliderChildName;
            ColliderIsTrigger = colliderIsTrigger;
            FacadeBaseType = facadeBaseType;
        }

        public string Name { get; }
        public string OwnLayerName { get; }
        public string TargetLayerName { get; }
        public Direction Direction { get; }
        public Color MoveRangeColor { get; }
        public string OutputFolder { get; }
        public string ColliderChildName { get; }
        public bool ColliderIsTrigger { get; }
        public Type FacadeBaseType { get; }

        public int Layer => LayerMask.NameToLayer(OwnLayerName);
        public int AttackMask => 1 << LayerMask.NameToLayer(TargetLayerName);

        public static AnimalSideProfile FromAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (assetPath.StartsWith(AnimalPrefabPaths.AnimalsFolder, StringComparison.Ordinal)) return Ally;
            if (assetPath.StartsWith(AnimalPrefabPaths.EnemiesFolder, StringComparison.Ordinal)) return Enemy;
            return null;
        }
    }
}
#endif
