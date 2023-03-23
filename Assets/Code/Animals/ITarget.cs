using Code.Animals.Health;

namespace Code.Animals
{
    public interface ITarget
    {
        IDamageable Damageable { get; }
        ITransformable Transformable { get; }
    }
}