using System.Collections.Generic;
using Code.Animals.Facades;
using UnityEngine;

namespace Code.Battle.Services
{
    public class BattleResetService : IBattleResetService
    {
        private readonly IUnitTracker _unitTracker;
        private readonly BattleFlowController _flowController;

        public BattleResetService(IUnitTracker unitTracker, BattleFlowController flowController)
        {
            _unitTracker = unitTracker;
            _flowController = flowController;
        }

        public void ResetForNewRun()
        {
            foreach (AnimalFacade unit in CollectLeftoverUnits())
            {
                unit.NotifyRemoved();
                Object.Destroy(unit.gameObject);
            }

            _flowController.Cleanup();
        }

        private HashSet<AnimalFacade> CollectLeftoverUnits()
        {
            var leftoverUnits = new HashSet<AnimalFacade>(Object.FindObjectsOfType<AnimalFacade>());

            foreach (AnimalFacade survivingUnit in _unitTracker.GetAlivePlayerUnits())
            {
                if (survivingUnit != null)
                    leftoverUnits.Add(survivingUnit);
            }

            return leftoverUnits;
        }
    }
}
