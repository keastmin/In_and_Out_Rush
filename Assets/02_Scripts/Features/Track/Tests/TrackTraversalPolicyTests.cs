using NUnit.Framework;

namespace ProjectIO.Tracks.Tests
{
    public sealed class TrackTraversalPolicyTests
    {
        [Test]
        public void IntermediatePath_TransfersToNextPath()
        {
            Assert.That(
                TrackTraversalPolicy.Resolve(hasNextPath: true, shouldLoopAtFinalPath: false),
                Is.EqualTo(TrackTraversalAction.TransferToNextPath));
        }

        [Test]
        public void FinalPath_NormalMonsterCompletes()
        {
            Assert.That(
                TrackTraversalPolicy.Resolve(hasNextPath: false, shouldLoopAtFinalPath: false),
                Is.EqualTo(TrackTraversalAction.Complete));
        }

        [Test]
        public void FinalPath_EliteMonsterLoopsToFirstPath()
        {
            Assert.That(
                TrackTraversalPolicy.Resolve(hasNextPath: false, shouldLoopAtFinalPath: true),
                Is.EqualTo(TrackTraversalAction.LoopToFirstPath));
        }
    }
}
