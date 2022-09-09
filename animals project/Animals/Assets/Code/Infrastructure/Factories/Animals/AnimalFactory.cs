using System;
using Code.Logic.Animals;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Factories.Animals
{
    public class AnimalFactory : IAnimalFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly DiContainer _container;
        
        private Animal _foxPrefab;
        private Animal _elephantPrefab;
        private Animal _cheetahPrefab;
        
        public AnimalFactory(IAssetProvider assetProvider,DiContainer container)
        {
            _assetProvider = assetProvider;
            _container = container;
        }

        public void Load()
        {
            _foxPrefab = _assetProvider.Load<Animal>(AssetPath.Fox);
            _elephantPrefab = _assetProvider.Load<Animal>(AssetPath.Elephant);
            _cheetahPrefab = _assetProvider.Load<Animal>(AssetPath.Cheetah);
        }

        public Animal Create(AnimalType type)
        {
            Debug.Log(_elephantPrefab);
            
            switch (type)
            {
                case AnimalType.Fox:
                    return _container.InstantiatePrefabForComponent<Animal>(_foxPrefab);
                case AnimalType.Elephant:
                    return _container.InstantiatePrefabForComponent<Animal>(_elephantPrefab);
                case AnimalType.Cheetah:
                    return _container.InstantiatePrefabForComponent<Animal>(_cheetahPrefab);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}