namespace ProjectIO.MatchProgression
{
    public static class RoundProgressionTransitionPolicy
    {
        public static RoundProgressionTransition Evaluate(
            RoundProgressionPhase phase,
            float phaseElapsedTime,
            float maintenanceDuration,
            float roundDuration)
        {
            switch (phase)
            {
                case RoundProgressionPhase.Maintenance:
                    return phaseElapsedTime >= maintenanceDuration
                        ? RoundProgressionTransition.StartRound
                        : RoundProgressionTransition.None;
                case RoundProgressionPhase.Combat:
                    return phaseElapsedTime >= roundDuration
                        ? RoundProgressionTransition.EndRound
                        : RoundProgressionTransition.None;
                default:
                    return RoundProgressionTransition.None;
            }
        }
    }
}
