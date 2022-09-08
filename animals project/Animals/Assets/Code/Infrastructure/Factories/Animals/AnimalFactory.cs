using System;
using Code.Logic.Animals;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using Object = UnityEngine.Object;

namespace Code.Infrastructure.Factories.Animals
{
    public class AnimalFactory : IAnimalFactory
    {
        private readonly IAssetProvider _assetProvider;

        private Animal _foxPrefab;
        private Animal _elephantPrefab;
        private Animal _cheetahPrefab;
        
        public AnimalFactory(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public void Load()
        {
            _foxPrefab = _assetProvider.Load<Animal>(AssetPath.Fox);
            _elephantPrefab = _assetProvider.Load<Animal>(AssetPath.Elephant);
            _cheetahPrefab = _assetProvider.Load<Animal>(AssetPath.Cheetah);
        }

        public Animal Create(AnimalType type)
        {
            switch (type)
            {
                case AnimalType.Fox:
                    return Object.Instantiate(_foxPrefab);
                case AnimalType.Elephant:
                    return Object.Instantiate(_elephantPrefab);
                case AnimalType.Cheetah:
                    return Object.Instantiate(_cheetahPrefab);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}