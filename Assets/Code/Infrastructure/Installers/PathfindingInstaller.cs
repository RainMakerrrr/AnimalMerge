using Code.Animals.Movement;
using Code.GridPathfinding;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Installers
{
    /// <summary>
    /// Zenject installer for pathfinding system
    /// Binds GridManager and PathfindingService to the DI container
    /// </summary>
    public class PathfindingInstaller : MonoInstaller
    {
        [Header("Grid Manager References")]
        [SerializeField] private GridManager _mergeGridManager;
        [SerializeField] private GridManager _gameGridManager;

        public override void InstallBindings()
        {
            // Validate GridManager references
            if (_mergeGridManager == null)
            {
                Debug.LogError("[PathfindingInstaller] MergeGridManager reference is not set! Please assign it in the Inspector.");
                return;
            }

            if (_gameGridManager == null)
            {
                Debug.LogError("[PathfindingInstaller] GameGridManager reference is not set! Please assign it in the Inspector.");
                return;
            }

            // Bind GridManager instances with IDs
            Container.Bind<IGridManager>()
                .WithId(GridIdentifier.MergeGrid)
                .FromInstance(_mergeGridManager)
                .AsTransient();

            Container.Bind<IGridManager>()
                .WithId(GridIdentifier.GameGrid)
                .FromInstance(_gameGridManager)
                .AsTransient();

            // Bind PathfindingService
            Container.Bind<IPathfindingService>()
                .To<PathfindingService>()
                .AsSingle();

            // Bind TargetDetector for movement positioning logic
            Container.Bind<ITargetDetector>()
                .To<TargetPositionCalculator>()
                .AsSingle();

            Debug.Log("[PathfindingInstaller] Pathfinding system installed successfully");
        }
    }
}
