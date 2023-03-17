using System.Collections;
using Code.Animals;
using UnityEngine;

namespace Code
{
    public class GridMover : MonoBehaviour
    {
        [SerializeField] private AnimalSpawner _spawner;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                //Sequence sequence = DOTween.Sequence();

                StartCoroutine(MoveAllAnimals());
                // foreach (Animal animal in _spawner.Animals)
                // {
                //     //sequence.Append(animal.GetComponent<AnimalMovement>().Move());
                // }
            }
        }

        private IEnumerator MoveAllAnimals()
        {
            foreach (Animal animal in _spawner.Animals)
            {
                yield return StartCoroutine(animal.GetComponent<AnimalMovement>().Move());
            }
        }
    }
}