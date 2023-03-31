using Code.Animals.Health;
using Code.Pathfinding;

namespace Code.Animals
{
    public interface ITarget
    {
        IDamageable Damageable { get; }
        ITransformable Transformable { get; }
    }
}