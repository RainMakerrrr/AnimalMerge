#if UNITY_EDITOR
using Code.Animals;
using Code.Data.Animals;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public abstract class AnimalRecipe : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private AnimalType _type;
        [SerializeField] private string _prefabName;
        [SerializeField] private GameObject _modelPrefab;
        [SerializeField] private MonoScript _facadeScript;
        [SerializeField] private MonoScript _attackScript;

        [Header("Animation")]
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private Avatar _avatar;
        [SerializeField] private AnimationClip _attackClip;
        [SerializeField] private AnimationClip _deathClip;

        [Header("Transform")]
        [SerializeField] private Vector3 _rootScale = Vector3.one;

        [Header("Grid and movement")]
        [SerializeField] private AnimalFootprint _footprint = AnimalFootprint.Small1x1;
        [SerializeField] private float _zOffset;
        [SerializeField, Min(0f)] private float _raycastOffset = AnimalPrefabConstants.DefaultRaycastOffset;
        [SerializeField, Min(0)] private int _sizeEffectY;
        [SerializeField, Min(0.01f)] private float _minAnimSpeed = 0.5f;
        [SerializeField, Min(0.01f)] private float _maxAnimSpeed = 3f;

        [Header("Attack")]
        [SerializeField, Min(0.01f)] private float _attackRadius = 0.5f;
        [SerializeField, Min(1)] private int _maxTargets = 1;
        [SerializeField] private bool _isAoE;
        [SerializeField, Min(0f)] private float _forwardReach = AnimalPrefabConstants.ForwardReach;

        [Header("Popups")]
        [SerializeField, Min(0f)] private float _damagePopupOffsetY = 1.5f;

        [Header("Geometry overrides")]
        [SerializeField] private bool _overrideColliderShape;
        [SerializeField] private float _colliderRadius = 0.5f;
        [SerializeField] private Vector3 _colliderCenter;
        [SerializeField] private bool _overrideAttackPointPosition;
        [SerializeField] private Vector3 _attackPointLocalPosition;
        [SerializeField] private bool _overrideHealthBarHeight;
        [SerializeField] private float _healthBarHeightOffset = 1.5f;

        [Header("Stats and database")]
        [SerializeField] private AnimalStats _stats;
        [SerializeField] private MonoScript _statsScript;
        [SerializeField] private Sprite _icon;

        public abstract AnimalSideProfile Side { get; }

        public AnimalType Type => _type;
        public string PrefabName => string.IsNullOrEmpty(_prefabName) ? _type.ToString() : _prefabName;
        public GameObject ModelPrefab => _modelPrefab;
        public MonoScript FacadeScript => _facadeScript;
        public MonoScript AttackScript => _attackScript;

        public RuntimeAnimatorController AnimatorController => _animatorController;
        public Avatar Avatar => _avatar;
        public AnimationClip AttackClip => _attackClip;
        public AnimationClip DeathClip => _deathClip;

        public Vector3 RootScale => _rootScale;

        public AnimalFootprint Footprint => _footprint;
        public float ZOffset => _zOffset;
        public float RaycastOffset => _raycastOffset;
        public int SizeEffectY => _sizeEffectY;
        public float MinAnimSpeed => _minAnimSpeed;
        public float MaxAnimSpeed => _maxAnimSpeed;

        public float AttackRadius => _attackRadius;
        public int MaxTargets => _maxTargets;
        public bool IsAoE => _isAoE;
        public float ForwardReach => _forwardReach;

        public float DamagePopupOffsetY => _damagePopupOffsetY;

        public bool OverrideColliderShape => _overrideColliderShape;
        public float ColliderRadius => _colliderRadius;
        public Vector3 ColliderCenter => _colliderCenter;
        public bool OverrideAttackPointPosition => _overrideAttackPointPosition;
        public Vector3 AttackPointLocalPosition => _attackPointLocalPosition;
        public bool OverrideHealthBarHeight => _overrideHealthBarHeight;
        public float HealthBarHeightOffset => _healthBarHeightOffset;

        public AnimalStats Stats => _stats;
        public MonoScript StatsScript => _statsScript;
        public Sprite Icon => _icon;

        public string OutputPath => $"{Side.OutputFolder}/{PrefabName}.prefab";
    }
}
#endif
