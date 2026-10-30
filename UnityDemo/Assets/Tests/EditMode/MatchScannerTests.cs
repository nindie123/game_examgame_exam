using BagElimination.Rules.Board;
using BagElimination.Rules.Matching;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class MatchScannerTests
    {
        [Test]
        public void ThreeInARow_IsAMatch()
        {
            BoardState board = TestBoard.FromRows("RRR");

            Assert.That(MatchScanner.HasAnyMatch(board), Is.True);
            Assert.That(MatchScanner.HasMatchAt(board, new BoardPosition(0, 0)), Is.True);
            Assert.That(MatchScanner.HasMatchAt(board, new BoardPosition(0, 1)), Is.True);
            Assert.That(MatchScanner.HasMatchAt(board, new BoardPosition(0, 2)), Is.True);
        }

        [Test]
        public void FourInARow_IsAMatch()
        {
            Assert.That(MatchScanner.HasAnyMatch(TestBoard.FromRows("RRRR")), Is.True);
        }

        [Test]
        public void TwoInARow_IsNotAMatch()
        {
            Assert.That(MatchScanner.HasAnyMatch(TestBoard.FromRows("RRG")), Is.False);
        }

        [Test]
        public void ThreeInAColumn_IsAMatch()
        {
            BoardState board = TestBoard.FromRows(
                "R",
                "R",
                "R");

            Assert.That(MatchScanner.HasAnyMatch(board), Is.True);
            Assert.That(MatchScanner.HasMatchAt(board, new BoardPosition(1, 0)), Is.True);
        }

        [Test]
        public void TwoByTwoSameColor_IsAMatch()
        {
            BoardState board = TestBoard.FromRows(
                "RR",
                "RR");

            Assert.That(MatchScanner.HasAnyMatch(board), Is.True);
            Assert.That(MatchScanner.HasMatchAt(board, new BoardPosition(1, 1)), Is.True);
        }

        [Test]
        public void TwoByTwoWithDifferentColor_IsNotAMatch()
        {
            BoardState board = TestBoard.FromRows(
                "RG",
                "RG");

            Assert.That(MatchScanner.HasAnyMatch(board), Is.False);
        }

        [Test]
        public void BackpackBreaksARun()
        {
            Assert.That(MatchScanner.HasAnyMatch(TestBoard.FromRows("RR#R")), Is.False);
        }

        [Test]
        public void PropBreaksARun()
        {
            Assert.That(MatchScanner.HasAnyMatch(TestBoard.FromRows("RRHR")), Is.False);
        }

        [Test]
        public void TryFindAnyMatch_ReturnsFirstMatchInRowMajorOrder()
        {
            BoardState board = TestBoard.FromRows(
                "GGB",
                "RRR",
                "BGG");

            bool found = MatchScanner.TryFindAnyMatch(board, out BoardPosition position);

            Assert.That(found, Is.True);
            Assert.That(position, Is.EqualTo(new BoardPosition(1, 0)));
        }
    }
}
