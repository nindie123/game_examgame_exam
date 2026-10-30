using BagElimination.Rules.Board;
using BagElimination.Rules.Matching;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class LinearMatchScannerTests
    {
        [Test]
        public void HorizontalThree_CollectsAllThreeCells()
        {
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows("RRR"));

            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(0, 0), new BoardPosition(0, 1), new BoardPosition(0, 2) }));
            Assert.That(result.HitBackpacks, Is.Empty);
            Assert.That(result.HasMatch, Is.True);
        }

        [Test]
        public void VerticalThree_CollectsAllThreeCells()
        {
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows("R", "R", "R"));

            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(0, 0), new BoardPosition(1, 0), new BoardPosition(2, 0) }));
            Assert.That(result.HitBackpacks, Is.Empty);
        }

        [Test]
        public void FourInARow_IncludesEveryCell()
        {
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows("RRRR"));

            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(0, 0), new BoardPosition(0, 1),
                new BoardPosition(0, 2), new BoardPosition(0, 3) }));
        }

        [Test]
        public void TwoIndependentMatches_AreBothReportedInRowMajorOrder()
        {
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows("RRR..BBB"));

            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(0, 0), new BoardPosition(0, 1), new BoardPosition(0, 2),
                new BoardPosition(0, 5), new BoardPosition(0, 6), new BoardPosition(0, 7) }));
        }

        [Test]
        public void CrossingLines_RecordSharedCellOnce()
        {
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows(
                ".R.",
                "RRR",
                ".R."));

            // Five cells, not six: the center cell belongs to both runs but appears once,
            // and the result is top-to-bottom then left-to-right.
            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(0, 1),
                new BoardPosition(1, 0), new BoardPosition(1, 1), new BoardPosition(1, 2),
                new BoardPosition(2, 1) }));
        }

        [Test]
        public void AdjacentBackpack_HitOrthogonally_DiagonalKept()
        {
            // (0,2) is directly above matched (1,2): hit. (0,0) is only diagonal to (1,1): kept.
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows(
                "#.#.",
                ".RRR",
                "...."));

            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(1, 1), new BoardPosition(1, 2), new BoardPosition(1, 3) }));
            Assert.That(result.HitBackpacks, Is.EqualTo(new[] { new BoardPosition(0, 2) }));
        }

        [Test]
        public void BackpackAdjacentToTwoMatchedCells_IsRecordedOnce()
        {
            // (0,0) is orthogonal to matched (0,1) and (1,0); it must be listed exactly once.
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows(
                "#R.",
                "RRR",
                ".R."));

            Assert.That(result.HitBackpacks, Is.EqualTo(new[] { new BoardPosition(0, 0) }));
        }

        [Test]
        public void Backpack_Prop_Empty_EachBreakARun()
        {
            Assert.That(LinearMatchScanner.FindMatches(TestBoard.FromRows("RR#RR")).HasMatch, Is.False);
            Assert.That(LinearMatchScanner.FindMatches(TestBoard.FromRows("RRHRR")).HasMatch, Is.False);
            Assert.That(LinearMatchScanner.FindMatches(TestBoard.FromRows("RR.RR")).HasMatch, Is.False);
        }

        [Test]
        public void NoMatch_ReturnsEmptyResult()
        {
            LinearMatchResult result = LinearMatchScanner.FindMatches(TestBoard.FromRows("RGB"));

            Assert.That(result.MatchedCells, Is.Empty);
            Assert.That(result.HitBackpacks, Is.Empty);
            Assert.That(result.HasMatch, Is.False);
        }

        [Test]
        public void ScanDoesNotMutateBoardOrObjectIds()
        {
            BoardState board = TestBoard.FromRows(
                "RRR",
                "#B#",
                ".R.");

            string[] before = Snapshot(board);

            LinearMatchScanner.FindMatches(board);

            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        private static string[] Snapshot(BoardState board)
        {
            var snapshot = new string[board.Height * board.Width];
            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    snapshot[row * board.Width + column] = board.GetCell(row, column).ToString();
                }
            }

            return snapshot;
        }
    }
}