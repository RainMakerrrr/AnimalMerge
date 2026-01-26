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
        [Header("Grid Manager Reference")]
        [SerializeField] private GridManager _gridManager;

        public override void InstallBindings()
        {
            // Validate GridManager reference
            if (_gridManager == null)
            {
                Debug.LogError("[PathfindingInstaller] GridManager reference is not set! Please assign it in the Inspector.");
                return;
            }

            // Bind GridManager instance
            Container.Bind<IGridManager>()
                .FromInstance(_gridManager)
                .AsSingle();

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
