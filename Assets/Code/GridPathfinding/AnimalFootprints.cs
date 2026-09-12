using Code.Animals;

namespace Code.GridPathfinding
{
    public static class AnimalFootprints
    {
        public static UnitSize For(AnimalType animalType)
        {
            return animalType switch
            {
                AnimalType.Elephant => new UnitSize(2, 2),
                AnimalType.Chicken => ChickenFlock.Footprint,
                AnimalType.Cheetah => new UnitSize(1, 2),
                AnimalType.Deer => new UnitSize(1, 2),
                AnimalType.Fox => new UnitSize(1, 2),
                AnimalType.Hedgehog => new UnitSize(1, 2),
                _ => new UnitSize(1, 1)
            };
        }

        public static int CellCount(AnimalType animalType)
        {
            var footprint = For(animalType);

            return footprint.Width * footprint.Height;
        }
    }
}
