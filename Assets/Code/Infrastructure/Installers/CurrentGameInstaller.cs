using Code.Infrastructure.Factories.Animals;
using Code.Infrastructure.Factories.Nodes;
using Code.Infrastructure.Services.Input;
using Code.Pathfinding;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;

namespace Code.Infrastructure.Installers
{
    public class CurrentGameInstaller : MonoInstaller
    {
        private const string GameGridId = "Game Grid";
        private const string MergeGridId = "Merge Grid";

        [SerializeField] private Grid _grid;
        [SerializeField] private Grid _mergeGrid;

        public override void InstallBindings()
        {
            BindPathNodeFactory();
            BindPathfinder();
            BindGrid();
            BindAnimalFactory();
            BindCamera();
            BindInputService();
        }


        private void BindPathfinder() => Container.Bind<IPathfinder>().To<Pathfinder>().AsSingle();

        private void BindPathNodeFactory() => Container.Bind<IPathNodeFactory>().To<PathNodeFactory>().AsSingle();
        private void BindGrid()
        {
            Container.Bind<Grid>().FromInstance(_grid).AsSingle();
        }

        private void BindAnimalFactory() => Container.Bind<IAnimalFactory>().To<AnimalFactory>().AsSingle();
        private void BindCamera() => Container.Bind<Camera>().FromInstance(Camera.main).AsSingle();
        private void BindInputService() => Container.Bind<IInputService>().To<InputService>().AsSingle();
    }
}