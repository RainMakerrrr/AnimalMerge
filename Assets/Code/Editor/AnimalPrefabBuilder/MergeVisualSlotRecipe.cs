#if UNITY_EDITOR
using System;
using Code.Animals;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    [Serializable]
    public class MergeVisualSlotRecipe
    {
        [SerializeField] private AnimalType _sourceType;
        [SerializeField] private GameObject _visualPrefab;
        [SerializeField] private string _boneName;
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private Vector3 _localEulerAngles;
        [SerializeField] private Vector3 _localScale = Vector3.one;

        public AnimalType SourceType => _sourceType;
        public GameObject VisualPrefab => _visualPrefab;
        public string BoneName => _boneName;
        public Vector3 LocalPosition => _localPosition;
        public Vector3 LocalEulerAngles => _localEulerAngles;
        public Vector3 LocalScale => _localScale;
    }
}
#endif
