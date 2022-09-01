using NaughtyAttributes;
using UnityEngine;

namespace Framework.Code.Factories.Levels
{
    [CreateAssetMenu(fileName = "New Level Database", menuName = "Data / Level Database")]
    public class LevelDataBase : ScriptableObject
    {
        [ReorderableList] [SerializeField] string[] tutorialLevels;
        [ReorderableList] [SerializeField] string[] levels;

        public string[] TutorialLevels => tutorialLevels;
        public string[] Levels => levels;
    }
}