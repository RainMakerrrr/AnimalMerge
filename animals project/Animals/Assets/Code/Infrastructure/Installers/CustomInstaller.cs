using Code.Infrastructure.Factories.Animals;
using Code.Infrastructure.Factories.Tiles;
using Code.Infrastructure.Services.Input;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class CustomInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindInputService();
            BindTileFactory();
            BindAnimalFactory();
        }

        private void BindInputService() => Container.Bind<IInputService>().To<InputService>().AsSingle();
        private void BindTileFactory() => Container.Bind<ITileFactory>().To<TileFactory>().AsSingle();
        private void BindAnimalFactory() => Container.Bind<IAnimalFactory>().To<AnimalFactory>().AsSingle();
    }
}