using NUnit.Framework;

namespace ProjectIO.GridPlacement.Tests
{
    public sealed class TowerPlacementPolicyTests
    {
        [Test]
        public void CanPlace_AcceptsEmptyCellInsideBuildArea()
        {
            var cellState = new TowerPlacementCellState(
                isInBuildArea: true,
                isBlockedByTrack: false,
                isOccupied: false);

            Assert.That(TowerPlacementPolicy.CanPlace(cellState), Is.True);
        }

        [Test]
        public void CanPlace_RejectsCellOutsideBuildArea()
        {
            var cellState = new TowerPlacementCellState(
                isInBuildArea: false,
                isBlockedByTrack: false,
                isOccupied: false);

            Assert.That(TowerPlacementPolicy.CanPlace(cellState), Is.False);
        }

        [Test]
        public void CanPlace_RejectsCellBlockedByTrack()
        {
            var cellState = new TowerPlacementCellState(
                isInBuildArea: true,
                isBlockedByTrack: true,
                isOccupied: false);

            Assert.That(TowerPlacementPolicy.CanPlace(cellState), Is.False);
        }

        [Test]
        public void CanPlace_RejectsOccupiedCell()
        {
            var cellState = new TowerPlacementCellState(
                isInBuildArea: true,
                isBlockedByTrack: false,
                isOccupied: true);

            Assert.That(TowerPlacementPolicy.CanPlace(cellState), Is.False);
        }
    }
}
