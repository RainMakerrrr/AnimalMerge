using Code.Abilities;
using Code.Pathfinding;
using UnityEngine;
using Grid = UnityEngine.Grid;

namespace Code.Animals.Movement
{
    public class ChickenMovement : MonoBehaviour
    {
        private IAbility _ability;
        private Grid _mergeGrid;
        
        public void Construct(IAbility ability)
        {
            _ability = ability;
        }
        
    }
}