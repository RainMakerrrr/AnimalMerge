using Code.Infrastructure.Factories.Nodes;
using Code.Pathfinding;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;

namespace Code.Infrastructure.Installers
{
    public class CurrentGameInstaller : MonoInstaller
    {
        [SerializeField] private Grid _grid;

        public override void InstallBindings()
        {
            BindPathNodeFactory();
            BindPathfinder();
            BindGrid();
        }

        private void BindPathfinder() => Container.Bind<IPathfinder>().To<Pathfinder>().AsSingle();

        private void BindPathNodeFactory() => Container.Bind<IPathNodeFactory>().To<PathNodeFactory>().AsSingle();
        private void BindGrid() => Container.Bind<Grid>().FromInstance(_grid).AsSingle();
    }
}