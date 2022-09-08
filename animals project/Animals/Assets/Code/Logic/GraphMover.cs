using System.Collections;
using System.Collections.Generic;
using Code.Logic.Boards;
using Pathfinding;
using Pathfinding.Examples;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Logic
{
    public class GraphMover : MonoBehaviour
    {
        [SerializeField] private TurnBasedAI _selected;

        public float _movementSpeed;
        public GameObject _nodePrefab;
        public LayerMask _layerMask;

        private List<GameObject> _possibleMoves = new List<GameObject>();
        private EventSystem _eventSystem;

        public State _state = State.SelectUnit;

        public enum State
        {
            SelectUnit,
            SelectTarget,
            Move
        }

        private void Awake()
        {
            _eventSystem = FindObjectOfType<EventSystem>();
        }

        private void Update()
        {
            // var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //
            // // Ignore any input while the mouse is over a UI element
            // if (_eventSystem.IsPointerOverGameObject()) {
            // 	return;
            // }
            //
            // if (_state == State.SelectTarget) {
            // 	HandleButtonUnderRay(ray);
            // }
            //
            // if (_state == State.SelectUnit || _state == State.SelectTarget) {
            // 	if (Input.GetKeyDown(KeyCode.Mouse0)) {
            // 		var unitUnderMouse = GetByRay<TurnBasedAI>(ray);
            //
            // 		if (unitUnderMouse != null) {
            // 			Select(unitUnderMouse);
            // 			DestroyPossibleMoves();
            // 			GeneratePossibleMoves(_selected);
            // 			_state = State.SelectTarget;
            // 		}
            // 	}
            // }

            if (Input.GetKeyDown(KeyCode.G))
            {
                DestroyPossibleMoves();
                GeneratePossibleMoves(_selected);
                
                //Debug.Log(AstarPath.active.data.graphs[1].name);
                var dataGraph = AstarPath.active.graphs[0] as GridGraph;
                //= dataGraph?.Ge GridNode nexttNodeConnection(_selected.Current as GridNode, 3);
                GraphNode next=  dataGraph?.GetNearest(_selected.transform.position + Vector3.forward * 1, _selected.Constraint).node;
                
                _selected.Current = next;
                
                StartCoroutine(MoveToNode(_selected, next));
            }
        }

        // TODO: Move to separate class
        private void HandleButtonUnderRay(Ray ray)
        {
            var button = GetByRay<Astar3DButton>(ray);

            if (button != null && Input.GetKeyDown(KeyCode.Mouse0))
            {
                button.OnClick();

                DestroyPossibleMoves();
                _state = State.Move;
                StartCoroutine(MoveToNode(_selected, button.node));
            }
        }

        private T GetByRay<T>(Ray ray) where T : class
        {
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, float.PositiveInfinity, _layerMask))
            {
                return hit.transform.GetComponentInParent<T>();
            }

            return null;
        }

        private void Select(TurnBasedAI unit)
        {
            _selected = unit;
        }

        private IEnumerator MoveToNode(TurnBasedAI unit, GraphNode node)
        {
            var path = ABPath.Construct(unit.transform.position, (Vector3) node.position);
            
            path.traversalProvider = unit.traversalProvider;
            
            // Schedule the path for calculation
            unit.GetComponent<Seeker>().StartPath(path);

            // Wait for the path calculation to complete
            yield return StartCoroutine(path.WaitForPath());

            if (path.error)
            {
                // Not obvious what to do here, but show the possible moves again
                // and let the player choose another target node
                // Likely a node was blocked between the possible moves being
                // generated and the player choosing which node to move to
                Debug.LogError("Path failed:\n" + path.errorLog);
                _state = State.SelectTarget;
                GeneratePossibleMoves(_selected);
                yield break;
            }

            // Set the target node so other scripts know which
            // node is the end point in the path
            unit.targetNode = path.path[path.path.Count - 1];

            yield return StartCoroutine(MoveAlongPath(unit, path, _movementSpeed));

            unit.blocker.BlockAtCurrentPosition();

            // Select a new unit to move
            _state = State.SelectUnit;
        }

        /// <summary>Interpolates the unit along the path</summary>
        private static IEnumerator MoveAlongPath(TurnBasedAI unit, ABPath path, float speed)
        {
            if (path.error || path.vectorPath.Count == 0)
                throw new System.ArgumentException("Cannot follow an empty path");

            // Very simple movement, just interpolate using a catmull rom spline
            float distanceAlongSegment = 0;
            for (int i = 0; i < path.vectorPath.Count - 1; i++)
            {
                var p0 = path.vectorPath[Mathf.Max(i - 1, 0)];
                // Start of current segment
                var p1 = path.vectorPath[i];
                // End of current segment
                var p2 = path.vectorPath[i + 1];
                var p3 = path.vectorPath[Mathf.Min(i + 2, path.vectorPath.Count - 1)];

                var segmentLength = Vector3.Distance(p1, p2);

                while (distanceAlongSegment < segmentLength)
                {
                    var interpolatedPoint =
                        AstarSplines.CatmullRom(p0, p1, p2, p3, distanceAlongSegment / segmentLength);
                    unit.transform.position = interpolatedPoint;
                    yield return null;
                    distanceAlongSegment += Time.deltaTime * speed;
                }

                distanceAlongSegment -= segmentLength;
            }

            unit.transform.position = path.vectorPath[path.vectorPath.Count - 1];
        }

        private void DestroyPossibleMoves()
        {
            foreach (var go in _possibleMoves)
            {
                GameObject.Destroy(go);
            }

            _possibleMoves.Clear();
        }

        private void GeneratePossibleMoves(TurnBasedAI unit)
        {
            var path = ConstantPath.Construct(unit.transform.position, unit.movementPoints * 1000 + 1);
            
            path.traversalProvider = unit.traversalProvider;

            // Schedule the path for calculation
            unit.GetComponent<Seeker>().StartPath(path);
            // Force the path request to complete immediately
            // This assumes the graph is small enough that
            // this will not cause any lag
            path.BlockUntilCalculated();

            // foreach (var node in path.allNodes)
            // {
            //     if (node != path.startNode)
            //     {
            //         // Create a new node prefab to indicate a node that can be reached
            //         // NOTE: If you are going to use this in a real game, you might want to
            //         // use an object pool to avoid instantiating new GameObjects all the time
            //         var go =
            //             GameObject.Instantiate(_nodePrefab, (Vector3) node.position, Quaternion.identity) as GameObject;
            //         _possibleMoves.Add(go);
            //
            //         go.GetComponent<Tile>().Node = node;
            //     }
            // }
        }
    }
}