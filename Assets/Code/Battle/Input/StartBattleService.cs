using System;
using UnityEngine;

namespace Code.Battle.Input
{
    /// <summary>
    /// Central service for triggering battle start
    /// Provides event-based notification to PreBattleState
    /// </summary>
    public class StartBattleService
    {
        public event Action StartBattleRequested;

        public void RequestStart()
        {
            Debug.Log("[StartBattleService] Battle start requested");
            StartBattleRequested?.Invoke();
        }
    }
}
