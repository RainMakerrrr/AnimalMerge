using UnityEngine;

namespace Code.Animals
{
    public class Animal : MonoBehaviour
    {
        [SerializeField] private AnimalType _type;

        public AnimalType Type => _type;
    }
}