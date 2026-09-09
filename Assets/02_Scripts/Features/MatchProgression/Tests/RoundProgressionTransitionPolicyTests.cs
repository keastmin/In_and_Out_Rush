using NUnit.Framework;

namespace ProjectIO.MatchProgression.Tests
{
    public sealed class RoundProgressionTransitionPolicyTests
    {
        [Test]
        public void Evaluate_MaintenanceBeforeDuration_ReturnsNone()
        {
            RoundProgressionTransition transition = RoundProgressionTransitionPolicy.Evaluate(
                RoundProgressionPhase.Maintenance,
                phaseElapsedTime: 29.99f,
                maintenanceDuration: 30f,
                roundDuration: 180f);

            Assert.That(transition, Is.EqualTo(RoundProgressionTransition.None));
        }

        [TestCase(30f)]
        [TestCase(31f)]
        public void Evaluate_MaintenanceAtOrAfterDuration_StartsRound(float phaseElapsedTime)
        {
            RoundProgressionTransition transition = RoundProgressionTransitionPolicy.Evaluate(
                RoundProgressionPhase.Maintenance,
                phaseElapsedTime,
                maintenanceDuration: 30f,
                roundDuration: 180f);

            Assert.That(transition, Is.EqualTo(RoundProgressionTransition.StartRound));
        }

        [Test]
        public void Evaluate_CombatBeforeDuration_ReturnsNone()
        {
            RoundProgressionTransition transition = RoundProgressionTransitionPolicy.Evaluate(
                RoundProgressionPhase.Combat,
                phaseElapsedTime: 179.99f,
                maintenanceDuration: 30f,
                roundDuration: 180f);

            Assert.That(transition, Is.EqualTo(RoundProgressionTransition.None));
        }

        [TestCase(180f)]
        [TestCase(181f)]
        public void Evaluate_CombatAtOrAfterDuration_EndsRound(float phaseElapsedTime)
        {
            RoundProgressionTransition transition = RoundProgressionTransitionPolicy.Evaluate(
                RoundProgressionPhase.Combat,
                phaseElapsedTime,
                maintenanceDuration: 30f,
                roundDuration: 180f);

            Assert.That(transition, Is.EqualTo(RoundProgressionTransition.EndRound));
        }

        [Test]
        public void Evaluate_UnknownPhase_ReturnsNone()
        {
            RoundProgressionTransition transition = RoundProgressionTransitionPolicy.Evaluate(
                (RoundProgressionPhase)99,
                phaseElapsedTime: 999f,
                maintenanceDuration: 30f,
                roundDuration: 180f);

            Assert.That(transition, Is.EqualTo(RoundProgressionTransition.None));
        }
    }
}
