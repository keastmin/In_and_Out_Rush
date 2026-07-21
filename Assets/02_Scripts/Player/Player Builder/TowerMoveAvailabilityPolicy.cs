using Dev.Network;

namespace KIM.Dev
{
    public sealed class TowerMoveAvailabilityPolicy
    {
        private TimeSystem _timeSystem;

        public void Initialize(TimeSystem timeSystem)
        {
            _timeSystem = timeSystem;
        }

        public bool IsMaintenanceActive()
        {
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
