using System;
using Code.Animals;
using UnityEngine;

namespace Code.Battle.Config
{
    [Serializable]
    public class UnitBattleFeedbackEntry
    {
        [SerializeField] private AnimalType _type;
        [SerializeField] private bool _showBattleVignette;
        [SerializeField] private UnitHitMarkSettings _hitMark = new UnitHitMarkSettings();
        [SerializeField] private bool _shakeCameraOnStep;
        [SerializeField] private CameraShakeSettings _stepShake = new CameraShakeSettings();
        [SerializeField] private bool _shakeCameraOnAttack;
        [SerializeField] private CameraShakeSettings _attackShake = new CameraShakeSettings();

        public AnimalType Type => _type;
        public bool ShowBattleVignette => _showBattleVignette;
        public UnitHitMarkSettings HitMark => _hitMark;
        public bool ShakeCameraOnStep => _shakeCameraOnStep;
        public CameraShakeSettings StepShake => _stepShake;
        public bool ShakeCameraOnAttack => _shakeCameraOnAttack;
        public CameraShakeSettings AttackShake => _attackShake;
    }
}
