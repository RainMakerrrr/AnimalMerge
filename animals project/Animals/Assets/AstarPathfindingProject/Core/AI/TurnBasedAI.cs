using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding.Examples
{
    /// <summary>Helper script in the example scene 'Turn Based'</summary>
    [HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_examples_1_1_turn_based_a_i.php")]
    public class TurnBasedAI : VersionedMonoBehaviour
    {
        [SerializeField] private Seeker _seeker;

        public int movementPoints = 2;
        public BlockManager blockManager;
        public SingleNodeBlocker blocker;
        public GraphNode targetNode;
        public BlockManager.TraversalProvider traversalProvider;

        public GraphNode Current { get; set; }

        public NNConstraint Constraint { get; private set; }

        void Start()
        {
            GraphMask seekerGraphMask = _seeker.graphMask;
            Constraint = NNConstraint.None;
            Constraint.graphMask = seekerGraphMask;

            NavGraph activeGraph = AstarPath.active.graphs[0];

            Current = AstarPath.active.graphs[0].GetNearest(transform.position, Constraint).node;
            blocker.BlockAtCurrentPosition();
        }
        
        protected override void Awake()
        {
            base.Awake();
            // Set the traversal provider to block all nodes that are blocked by a SingleNodeBlocker
            // except the SingleNodeBlocker owned by this AI (we don't want to be blocked by ourself)
            traversalProvider = new BlockManager.TraversalProvider(blockManager,
                BlockManager.BlockMode.AllExceptSelector, new List<SingleNodeBlocker>() {blocker});
        }
    }
}