using NUnit.Framework;

namespace ProjectIO.ResourceEconomy.Tests
{
    public sealed class ResourcePaymentPolicyTests
    {
        [Test]
        public void TryCalculateRemaining_SubtractsBothResourcesWhenAffordable()
        {
            bool success = ResourcePaymentPolicy.TryCalculateRemaining(
                new ResourceAmount(100, 50),
                new ResourceAmount(30, 20),
                out ResourceAmount remaining);

            Assert.That(success, Is.True);
            Assert.That(remaining.Mineral, Is.EqualTo(70));
            Assert.That(remaining.Gas, Is.EqualTo(30));
        }

        [TestCase(29, 20)]
        [TestCase(30, 19)]
        public void TryCalculateRemaining_RejectsWhenEitherResourceIsInsufficient(
            int mineral,
            int gas)
        {
            var balance = new ResourceAmount(mineral, gas);

            bool success = ResourcePaymentPolicy.TryCalculateRemaining(
                balance,
                new ResourceAmount(30, 20),
                out ResourceAmount remaining);

            Assert.That(success, Is.False);
            Assert.That(remaining.Mineral, Is.EqualTo(balance.Mineral));
            Assert.That(remaining.Gas, Is.EqualTo(balance.Gas));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void TryCalculateRemaining_RejectsNegativeCost(int mineral, int gas)
        {
            bool success = ResourcePaymentPolicy.TryCalculateRemaining(
                new ResourceAmount(100, 50),
                new ResourceAmount(mineral, gas),
                out ResourceAmount remaining);

            Assert.That(success, Is.False);
            Assert.That(remaining.Mineral, Is.EqualTo(100));
            Assert.That(remaining.Gas, Is.EqualTo(50));
        }

        [Test]
        public void CanAfford_AcceptsExactBalance()
        {
            bool canAfford = ResourcePaymentPolicy.CanAfford(
                new ResourceAmount(30, 20),
                new ResourceAmount(30, 20));

            Assert.That(canAfford, Is.True);
        }
    }
}
