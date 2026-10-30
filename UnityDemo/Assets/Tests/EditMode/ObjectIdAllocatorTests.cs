using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class ObjectIdAllocatorTests
    {
        [Test]
        public void Allocate_ReturnsUniquePositiveIdsInOrder()
        {
            var allocator = new ObjectIdAllocator();

            Assert.That(allocator.Allocate(), Is.EqualTo(1));
            Assert.That(allocator.Allocate(), Is.EqualTo(2));
            Assert.That(allocator.Allocate(), Is.EqualTo(3));
            Assert.That(allocator.PeekNextId, Is.EqualTo(4));
        }

        [Test]
        public void Reset_RestartsAtOne()
        {
            var allocator = new ObjectIdAllocator();
            allocator.Allocate();
            allocator.Allocate();

            allocator.Reset();

            Assert.That(allocator.PeekNextId, Is.EqualTo(1));
        }

        [Test]
        public void BoardFactory_UsesProvidedAllocatorAndLeavesItAfterTheBoard()
        {
            var allocator = new ObjectIdAllocator();
            BoardState board = BoardFactory.Create(DemoLevelConfig.Create(), allocator);

            Assert.That(board.CountObjects(), Is.EqualTo(72));
            Assert.That(allocator.PeekNextId, Is.EqualTo(73));
        }

        [Test]
        public void DemoBoard_ObjectIdsAreUniqueAndRowMajor()
        {
            BoardState board = BoardFactory.Create(DemoLevelConfig.Create());
            var ids = new HashSet<int>();

            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    BoardCell cell = board.GetCell(row, column);
                    Assert.That(cell.IsOccupied, Is.True, "Cell " + new BoardPosition(row, column) + " should be occupied.");
                    Assert.That(ids.Add(cell.ObjectId), Is.True, "Duplicate object id " + cell.ObjectId + ".");
                }
            }

            Assert.That(ids.Count, Is.EqualTo(72));
            Assert.That(board.GetCell(0, 0).ObjectId, Is.EqualTo(1));
            Assert.That(board.GetCell(0, 8).ObjectId, Is.EqualTo(9));
            Assert.That(board.GetCell(1, 0).ObjectId, Is.EqualTo(10));
            Assert.That(board.GetCell(7, 8).ObjectId, Is.EqualTo(72));
        }
    }
}
