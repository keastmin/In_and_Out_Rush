using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Construction.Tests
{
    public sealed class TowerConstructionUseCaseTests
    {
        private readonly TowerConstructionUseCase _useCase = new();

        [Test]
        public void Execute_CommitsAfterAllConstructionStepsSucceed()
        {
            var operation = new RecordingOperation();

            TowerConstructionResult result = _useCase.Execute(operation);

            Assert.That(result, Is.EqualTo(TowerConstructionResult.Success));
            CollectionAssert.AreEqual(
                new[] { "Spawn", "Placement", "Initialize", "Pay", "Commit" },
                operation.Calls);
        }

        [Test]
        public void Execute_RollsBackWithoutLaterStepsWhenSpawnFails()
        {
            var operation = new RecordingOperation { SpawnSucceeds = false };

            TowerConstructionResult result = _useCase.Execute(operation);

            Assert.That(result, Is.EqualTo(TowerConstructionResult.SpawnFailed));
            CollectionAssert.AreEqual(new[] { "Spawn", "Rollback" }, operation.Calls);
        }

        [Test]
        public void Execute_RollsBackWithoutPaymentWhenPlacementIsRejected()
        {
            var operation = new RecordingOperation { PlacementSucceeds = false };

            TowerConstructionResult result = _useCase.Execute(operation);

            Assert.That(result, Is.EqualTo(TowerConstructionResult.PlacementRejected));
            CollectionAssert.AreEqual(
                new[] { "Spawn", "Placement", "Rollback" },
                operation.Calls);
        }

        [Test]
        public void Execute_RollsBackWithoutPaymentWhenInitializationFails()
        {
            var operation = new RecordingOperation { InitializationSucceeds = false };

            TowerConstructionResult result = _useCase.Execute(operation);

            Assert.That(result, Is.EqualTo(TowerConstructionResult.InitializationFailed));
            CollectionAssert.AreEqual(
                new[] { "Spawn", "Placement", "Initialize", "Rollback" },
                operation.Calls);
        }

        [Test]
        public void Execute_RollsBackWithoutCommitWhenPaymentFails()
        {
            var operation = new RecordingOperation { PaymentSucceeds = false };

            TowerConstructionResult result = _useCase.Execute(operation);

            Assert.That(result, Is.EqualTo(TowerConstructionResult.PaymentFailed));
            CollectionAssert.AreEqual(
                new[] { "Spawn", "Placement", "Initialize", "Pay", "Rollback" },
                operation.Calls);
        }

        [Test]
        public void Execute_RejectsMissingOperationWithoutSideEffects()
        {
            TowerConstructionResult result = _useCase.Execute(null);

            Assert.That(result, Is.EqualTo(TowerConstructionResult.InvalidOperation));
        }

        private sealed class RecordingOperation : ITowerConstructionOperation
        {
            public bool SpawnSucceeds { get; set; } = true;
            public bool PlacementSucceeds { get; set; } = true;
            public bool InitializationSucceeds { get; set; } = true;
            public bool PaymentSucceeds { get; set; } = true;
            public List<string> Calls { get; } = new();

            public bool TrySpawn()
            {
                Calls.Add("Spawn");
                return SpawnSucceeds;
            }

            public bool ValidatePlacement()
            {
                Calls.Add("Placement");
                return PlacementSucceeds;
            }

            public bool TryInitialize()
            {
                Calls.Add("Initialize");
                return InitializationSucceeds;
            }

            public bool TryPay()
            {
                Calls.Add("Pay");
                return PaymentSucceeds;
            }

            public void Commit()
            {
                Calls.Add("Commit");
            }

            public void Rollback()
            {
                Calls.Add("Rollback");
            }
        }
    }
}
