using System;
using System.Collections;
using System.Collections.Generic;
using Code.Infrastructure.Services.Path;
using Pathfinding;
using UnityEngine;
using Zenject;

namespace Code.Logic.MovementModel
{
    public class GridTransformable : VersionedMonoBehaviour
    {
        [SerializeField] private Seeker _seeker;
        [SerializeField] private SingleNodeBlocker _blocker;
        [SerializeField] private int _movementPoint;
        [SerializeField] private int _graphIndex;
        [SerializeField] private int _distanceToNextNode;
        [SerializeField] private float _moveSpeed;

        public Seeker Seeker => _seeker;
        public Vector3 Position => transform.position;
        public int MovementPoints => _movementPoint;


        public ITraversalProvider TraversalProvider { get; private set; }

        private BlockManager _blockManager;
        private IPathProvider _pathProvider;
        private GraphNode _next;
        private GraphNode _current;

        [Inject]
        private void Construct(IPathProvider pathProvider)
        {
            _pathProvider = pathProvider;
        }

        public void Construct(BlockManager blockManager)
        {
            _blockManager = blockManager;
        }
        
        private void Start()
        {
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

        private IEnumerator GeneratePath()
        {
            ABPath path = _pathProvider.GeneratePath(this, _current);

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

            float distanceAlongSegment = 0;
            for (int i = 0; i < path.vectorPath.Count - 1; i++)
            {
                Vector3 p0 = path.vectorPath[Mathf.Max(i - 1, 0)];
                Vector3 p1 = path.vectorPath[i];
                
                Vector3 p2 = path.vectorPath[i + 1];
                Vector3 p3 = path.vectorPath[Mathf.Min(i + 2, path.vectorPath.Count - 1)];

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

            transform.position = path.vectorPath[path.vectorPath.Count - 1];
        }

        public void InitCurrentGraphNode() => _current =
            AstarPath.active.graphs[_graphIndex].GetNearest(transform.position, NNConstraint.None).node;

        private void InitTraversalProvider() =>
            TraversalProvider = new BlockManager.TraversalProvider(_blockManager, 
                BlockManager.BlockMode.AllExceptSelector, new List<SingleNodeBlocker> {_blocker});

        private void FindNextNode()
        {
            GraphNode next = AstarPath.active.graphs[_graphIndex]
                .GetNearest(transform.position + Vector3.forward * _distanceToNextNode, NNConstraint.None).node;
            
            if (next == _current)
            {
                next = AstarPath.active.graphs[_graphIndex]
                    .GetNearest(transform.position + Vector3.right * _distanceToNextNode, NNConstraint.None).node;
            }
            
            _current = next;
        }
    }
}