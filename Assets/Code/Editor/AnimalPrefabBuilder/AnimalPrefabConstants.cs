#if UNITY_EDITOR
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AnimalPrefabConstants
    {
        public const string AnimalLayerName = "Animal";
        public const string EnemyLayerName = "Enemy";
        public const string HealthBarLayerName = "HealthBar";

        public const string AttackEventFunctionName = "AttackAnimationHandler";
        public const string AttackPointChildName = "AttackPoint";
        public const string LegacyAttackPointChildName = "Attack Point";
        public const string HealthBarChildName = "HealthBar";

        public const string MoveSpeedParameter = "MoveSpeed";
        public const string AttackParameter = "Attack";
        public const string TakeDamageParameter = "TakeDamage";
        public const string IsDeadParameter = "IsDead";
        public const string JumpParameter = "Jump";
        public const string CounterAttackParameter = "CounterAttack";
        public const string TurnDirectionParameter = "TurnDirection";

        public const float AnimatorSpeed = 1.5f;
        public const float DeathDestroyDelaySeconds = 3f;
        public const float RandomXRange = 0.3f;
        public const float ForwardReach = 0.25f;
        public const float DefaultRaycastOffset = 0.4f;
        public const float LargestRaycastOffsetInProject = 0.4f;
        public const float RotationSpeed = 10f;
        public const float TurnAnimationDuration = 0.5f;
        public const float TurnDirectionSmoothSpeed = 4f;
        public const float TurnAmplification = 1.3f;
        public const float TurnAngleThreshold = 10f;
        public const int TilesPerMoveBaseline = 2;
        public const float BaseMoveSpeed = 3f;

        public static int AnimalLayer => LayerMask.NameToLayer(AnimalLayerName);
        public static int EnemyLayer => LayerMask.NameToLayer(EnemyLayerName);
        public static int HealthBarLayer => LayerMask.NameToLayer(HealthBarLayerName);
    }
}
#endif
