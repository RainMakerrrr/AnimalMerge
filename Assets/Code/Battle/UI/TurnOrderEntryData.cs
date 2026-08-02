using UnityEngine;

namespace Code.Battle.UI
{
    public readonly struct TurnOrderEntryData
    {
        public readonly int Id;
        public readonly Sprite Portrait;
        public readonly bool IsActive;
        public readonly bool IsEnemy;

        public TurnOrderEntryData(int id, Sprite portrait, bool isActive, bool isEnemy)
        {
            Id = id;
            Portrait = portrait;
            IsActive = isActive;
            IsEnemy = isEnemy;
        }
    }
}
