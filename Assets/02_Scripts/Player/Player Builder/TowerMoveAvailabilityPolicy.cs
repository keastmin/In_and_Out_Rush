using Dev.Network;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class TowerMoveAvailabilityPolicy
    {
        private TimeSystem _timeSystem;

        public bool IsMaintenanceActive()
        {
            if (!CanReadTimeSystem(_timeSystem))
                _timeSystem = Object.FindFirstObjectByType<TimeSystem>();

            return CanReadTimeSystem(_timeSystem) &&
                   _timeSystem.Phase == RoundPhase.Maintenance;
        }

        private static bool CanReadTimeSystem(TimeSystem timeSystem)
        {
            return timeSystem != null &&
                   timeSystem.Object != null &&
                   timeSystem.Object.IsValid &&
                   timeSystem.Object.IsInSimulation;
        }
    }
}
