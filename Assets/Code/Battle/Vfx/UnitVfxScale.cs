using Code.Animals.Facades;
using UnityEngine;

namespace Code.Battle.Vfx
{
    public static class UnitVfxScale
    {
        public static float Calculate(AnimalFacade unit, float scalePerCell)
        {
            if (unit == null || unit.Movement == null)
                return scalePerCell;

            var unitSize = unit.Movement.UnitSize;
            return Mathf.Max(unitSize.Width, unitSize.Height) * scalePerCell;
        }
    }
}
