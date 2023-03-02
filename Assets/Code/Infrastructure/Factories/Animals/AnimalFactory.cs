using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using Zenject;

namespace Code.Infrastructure.Factories.Animals
{
    public class AnimalFactory : IAnimalFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly DiContainer _container;
        private Dictionary<AnimalType, Animal> _animalPrefabs;

        [Inject]
        public AnimalFactory(IAssetProvider assetProvider, DiContainer container)
        {
            _assetProvider = assetProvider;
            _container = container;
        }

        public void Load()
        {
            _animalPrefabs = _assetProvider.LoadCollection<Animal>(AssetPath.Animals)
                .ToDictionary(animal => animal.Type);
        }

        public Animal Create(AnimalType type)
        {
            return _container.InstantiatePrefabForComponent<Animal>(_animalPrefabs[type]);
        }
    }
}