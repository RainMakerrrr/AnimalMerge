using System.Linq;
using UnityEngine;

namespace Code.Logic.MovementModel
{
    public class FoxTransformable : GridTransformable
    {
        protected override Vector3 GetTargetPosition() => CurrentTiles.FirstOrDefault()!.transform.position;
    }
}