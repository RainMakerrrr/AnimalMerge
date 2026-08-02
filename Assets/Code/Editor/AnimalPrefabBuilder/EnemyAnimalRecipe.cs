#if UNITY_EDITOR
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    [CreateAssetMenu(fileName = "EnemyAnimalRecipe", menuName = "Data/Enemy Animal Recipe")]
    public class EnemyAnimalRecipe : AnimalRecipe
    {
        public override AnimalSideProfile Side => AnimalSideProfile.Enemy;
    }
}
#endif
