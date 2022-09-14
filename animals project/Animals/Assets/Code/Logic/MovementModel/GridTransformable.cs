using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Infrastructure.Services.Path;
using Code.Logic.Animals;
using Code.Logic.Boards;
using Pathfinding;
using UnityEngine;
using Zenject;

namespace Code.Logic.MovementModel
{
    public abstract class GridTransformable : VersionedMonoBehaviour
    {
        [SerializeField] private Seeker _seeker;
        [SerializeField] private SingleNodeBlocker _blocker;
        [SerializeField] private int _movementPoint;
        [SerializeField] private int _graphIndex;
        [SerializeField] private int _distanceToNextNode;
        [SerializeField] private float _moveSpeed;

        private GameBoard _gameBoard;

        public Seeker Seeker => _seeker;
        public Vector3 Position => transform.position;
        public int MovementPoints => _movementPoint;

        public ITraversalProvider TraversalProvider { get; private set; }

        private BlockManager _blockManager;
        private IPathProvider _pathProvider;
        private GraphNode _next;
        private GraphNode _current;

        protected List<Tile> CurrentTiles = new();

        private Transform _currentTarget;
        
        [Inject]
        private void Construct(IPathProvider pathProvider)
        {
            _pathProvider = pathProvider;
        }

        public void Construct(BlockManager blockManager, Transform testTarget)
        {
            _blockManager = blockManager;
            _currentTarget = testTarget;
        }

        private void Start()
        {
            _gameBoard = FindObjectOfType<GameBoard>();

            InitTraversalProvider();
            InitCurrentGraphNode();

            _blocker.BlockAtCurrentPosition();
        }

        [ContextMenu("Try move")]
        public void MoveToNextNode()
        {
            _pathProvider.GeneratePossibleMoves(this);

            FindNextNode();

            StartCoroutine(GeneratePath());
        }

        protected abstract Vector3 GetTargetPosition();

        private Vector3 GetTopNeighbour()
        {
            List<Tile> tiles = CurrentTiles.OrderBy(tile => tile.Position.y).ToList();

            Debug.Log(tiles.FirstOrDefault()!.name);

            return (Vector3) tiles.FirstOrDefault()!.Node.position;
        }

        private Vector3 GetBottomNeighbour()
        {
            List<Tile> tiles = CurrentTiles.OrderByDescending(tile => tile.Position.x).ToList();

            Debug.Log(tiles.FirstOrDefault()!.name);

            return (Vector3) tiles.FirstOrDefault()!.Node.position;
        }

        private IEnumerator GeneratePath()
        {
            ABPath path = _pathProvider.GeneratePath(this, GetTargetPosition());

            yield return StartCoroutine(path.WaitForPath());

            if (path.error)
            {
                Debug.LogError("Path failed:\n" + path.errorLog);

                _pathProvider.GeneratePossibleMoves(this);
                yield break;
            }

            _next = path.path[path.path.Count - 1];

            yield return StartCoroutine(MoveAlongPath(path));

            _blocker.BlockAtCurrentPosition();
        }

        private IEnumerator MoveAlongPath(ABPath path)
        {
            if (path.error || path.vectorPath.Count == 0)
                throw new ArgumentException("Cannot follow an empty path");

            int end = _distanceToNextNode > path.vectorPath.Count ? path.vectorPath.Count - 1 : _distanceToNextNode;
            
            float distanceAlongSegment = 0;
            for (int i = 0; i < end; i++)
            {
                Vector3 p0 = path.vectorPath[Mathf.Max(i - 1, 0)];
                Vector3 p1 = path.vectorPath[i];

                Vector3 p2 = path.vectorPath[i + 1];
                Vector3 p3 = path.vectorPath[Mathf.Min(i + 2, end)];

                float segmentLength = Vector3.Distance(p1, p2);

                while (distanceAlongSegment < segmentLength)
                {
                    Vector3 interpolatedPoint =
                        AstarSplines.CatmullRom(p0, p1, p2, p3, distanceAlongSegment / segmentLength);

                    transform.position = interpolatedPoint;
                    yield return null;
                    distanceAlongSegment += Time.deltaTime * _moveSpeed;
                }

                distanceAlongSegment -= segmentLength;
            }

            transform.position = path.vectorPath[end];
        }

        public void InitCurrentGraphNode() => _current =
            AstarPath.active.graphs[_graphIndex].GetNearest(transform.position, NNConstraint.None).node;

        private void InitTraversalProvider() =>
            TraversalProvider = new BlockManager.TraversalProvider(_blockManager,
                BlockManager.BlockMode.AllExceptSelector, new List<SingleNodeBlocker> {_blocker});

        private void FindNextNode()
        {
            Tile targetTile =_gameBoard.GetNearest(_currentTarget.position, GetComponent<Animal>().TilesCount, out List<Tile> neighbours);
            
            //(transform.position + Vector3.forward * _distanceToNextNode / 2) + Vector3.forward
            
            //Tile tile = _gameBoard.GetNearest((Vector3)targetTile.position / _distanceToNextNode, GetComponent<Animal>().TilesCount, out List<Tile> neighbours);

            //Debug.Log(tile.name);

            GraphNode next = targetTile.Node;

            CurrentTiles = neighbours;

            foreach (Tile tile1 in GetComponent<Animal>().Tiles)
            {
                tile1.ReleaseAnimal();
            }

            GetComponent<Animal>().Tiles = CurrentTiles.ToArray();

            foreach (Tile tile1 in GetComponent<Animal>().Tiles)
            {
                tile1.AssignAnimal(GetComponent<Animal>());
            }

            if (next == _current)
            {
                next = AstarPath.active.graphs[_graphIndex]
                    .GetNearest(transform.position + Vector3.right * _distanceToNextNode, NNConstraint.None).node;
            }

            _current = next;
        }
    }
}