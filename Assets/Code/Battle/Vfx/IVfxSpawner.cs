using UnityEngine;

namespace Code.Battle.Vfx
{
    public interface IVfxSpawner
    {
        void Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation, float uniformScale);
    }
}
