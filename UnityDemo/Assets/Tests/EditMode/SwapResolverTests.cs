using BagElimination.Rules.Board;
using BagElimination.Rules.Resolution;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class SwapResolverTests
    {
        [Test]
        public void NormalThree_CommitsClearedBoard_AndPreservesSurvivors()
        {
            // Swapping (2,0)Y with (2,1)R makes column 0 hold R R R; the backpack below-right of
            // the middle cell ((1,1)) is the only four-neighbour hit.
            BoardState board = TestBoard.FromRows(
                "R..",
                "R#.",
                "YR.");

            SwapResolveResult result = SwapResolver.Resolve(board, new BoardPosition(2, 0), new BoardPosition(2, 1));

            Assert.That(result.Outcome, Is.EqualTo(SwapResolveOutcome.NormalThree));
            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(0, 0), new BoardPosition(1, 0), new BoardPosition(2, 0) }));
            Assert.That(result.HitBackpacks, Is.EqualTo(new[] { new BoardPosition(1, 1) }));

            BoardState committed = result.Board;
            Assert.That(committed, Is.Not.Null);
            Assert.That(committed.CountBackpacks(), Is.EqualTo(0));
            Assert.That(committed.GetCell(0, 0).Type, Is.EqualTo(ElementType.Empty));
            Assert.That(committed.GetCell(1, 0).Type, Is.EqualTo(ElementType.Empty));
            Assert.That(committed.GetCell(2, 0).Type, Is.EqualTo(ElementType.Empty));
            Assert.That(committed.GetCell(1, 1).Type, Is.EqualTo(ElementType.Empty));

            // The yellow piece that moved from (2,0) to (2,1) survives with its original id (4).
            Assert.That(committed.GetCell(2, 1).Type, Is.EqualTo(ElementType.NormalPiece));
            Assert.That(committed.GetCell(2, 1).Color, Is.EqualTo(PieceColor.Yellow));
            Assert.That(committed.GetCell(2, 1).ObjectId, Is.EqualTo(4));

            // The input board is untouched.
            Assert.That(board.GetCell(2, 0).Color, Is.EqualTo(PieceColor.Yellow));
            Assert.That(board.GetCell(2, 0).Type, Is.EqualTo(ElementType.NormalPiece));
            Assert.That(board.CountBackpacks(), Is.EqualTo(1));
        }

        [Test]
        public void NormalThree_Horizontal_ClearsTwoAdjacentBackpacks()
        {
            // Swapping (0,1)G with (1,1)R makes row 0 hold R R R; both backpacks below the run ends
            // are hit and removed.
            BoardState board = TestBoard.FromRows(
                "RGR",
                "#R#");

            SwapResolveResult result = SwapResolver.Resolve(board, new BoardPosition(0, 1), new BoardPosition(1, 1));

            Assert.That(result.Outcome, Is.EqualTo(SwapResolveOutcome.NormalThree));
            Assert.That(result.MatchedCells, Is.EqualTo(new[] {
                new BoardPosition(0, 0), new BoardPosition(0, 1), new BoardPosition(0, 2) }));
            Assert.That(result.HitBackpacks, Is.EqualTo(new[] {
                new BoardPosition(1, 0), new BoardPosition(1, 2) }));
            Assert.That(result.Board.CountBackpacks(), Is.EqualTo(0));
        }

        [Test]
        public void NoMatch_IsRejected_WithoutBoard()
        {
            BoardState board = TestBoard.FromRows("RGR");

            SwapResolveResult result = SwapResolver.Resolve(board, new BoardPosition(0, 0), new BoardPosition(0, 1));

            Assert.That(result.Outcome, Is.EqualTo(SwapResolveOutcome.Rejected));
            Assert.That(result.Board, Is.Null);
            Assert.That(result.MatchedCells, Is.Empty);
            Assert.That(result.HitBackpacks, Is.Empty);
        }

        [Test]
        public void FourInARow_IsSpecialUnsupported()
        {
            // Swapping the B at (0,3) with the R below produces a four-run, which resolves to a
            // special item later and must not be cleared as a plain three now.
            BoardState board = TestBoard.FromRows(
                "RRRB",
                "...R");

            SwapResolveResult result = SwapResolver.Resolve(board, new BoardPosition(0, 3), new BoardPosition(1, 3));

            Assert.That(result.Outcome, Is.EqualTo(SwapResolveOutcome.SpecialUnsupported));
            Assert.That(result.Board, Is.Null);
        }

        [Test]
        public void CrossingLines_AreSpecialUnsupported()
        {
            BoardState board = TestBoard.FromRows(
                ".R.",
                "RRB",
                ".RR");

            SwapResolveResult result = SwapResolver.Resolve(board, new BoardPosition(1, 2), new BoardPosition(2, 2));

            Assert.That(result.Outcome, Is.EqualTo(SwapResolveOutcome.SpecialUnsupported));
            Assert.That(result.Board, Is.Null);
        }

        [Test]
        public void TwoByTwo_IsSpecialUnsupported()
        {
            // The swap fills a 2x2 square of R without any three-run; the square is a special shape.
            BoardState board = TestBoard.FromRows(
                "RR.",
                "RB.",
                ".R.");

            SwapResolveResult result = SwapResolver.Resolve(board, new BoardPosition(1, 1), new BoardPosition(2, 1));

            Assert.That(result.Outcome, Is.EqualTo(SwapResolveOutcome.SpecialUnsupported));
            Assert.That(result.Board, Is.Null);
        }
    }
}