using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Legality;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class SwapEvaluatorTests
    {
        // Layout used by most cases. Swapping (0,1) with (1,1) makes row 0 read R R R.
        private static BoardState MatchBoard()
        {
            return TestBoard.FromRows(
                "RGR",
                "BRB",
                "GB#");
        }

        [Test]
        public void Evaluate_NormalSwapThatCreatesMatch_IsLegal()
        {
            SwapEvaluation evaluation = SwapEvaluator.Evaluate(
                MatchBoard(),
                new BoardPosition(0, 1),
                new BoardPosition(1, 1));

            Assert.That(evaluation.Outcome, Is.EqualTo(SwapOutcome.NormalMatch));
            Assert.That(evaluation.IsLegal, Is.True);
        }

        [Test]
        public void Evaluate_NormalSwapThatCreatesNoMatch_IsRejected()
        {
            SwapEvaluation evaluation = SwapEvaluator.Evaluate(
                MatchBoard(),
                new BoardPosition(0, 0),
                new BoardPosition(0, 1));

            Assert.That(evaluation.Outcome, Is.EqualTo(SwapOutcome.NoMatch));
            Assert.That(evaluation.IsLegal, Is.False);
        }

        [Test]
        public void Evaluate_SwapInvolvingBackpack_IsBlocked()
        {
            SwapEvaluation evaluation = SwapEvaluator.Evaluate(
                MatchBoard(),
                new BoardPosition(1, 2),
                new BoardPosition(2, 2));

            Assert.That(evaluation.Outcome, Is.EqualTo(SwapOutcome.Blocked));
        }

        [Test]
        public void Evaluate_SwapInvolvingEmptyCell_IsBlocked()
        {
            BoardState board = TestBoard.FromRows(
                "RG.",
                "BRB",
                "GB#");

            SwapEvaluation evaluation = SwapEvaluator.Evaluate(
                board,
                new BoardPosition(0, 2),
                new BoardPosition(1, 2));

            Assert.That(evaluation.Outcome, Is.EqualTo(SwapOutcome.Blocked));
        }

        [Test]
        public void Evaluate_NonAdjacentCells_AreRejected()
        {
            SwapEvaluation evaluation = SwapEvaluator.Evaluate(
                MatchBoard(),
                new BoardPosition(0, 0),
                new BoardPosition(2, 2));

            Assert.That(evaluation.Outcome, Is.EqualTo(SwapOutcome.NotAdjacent));
        }

        [Test]
        public void Evaluate_PositionOutsideBoard_IsOutOfBounds()
        {
            SwapEvaluation evaluation = SwapEvaluator.Evaluate(
                MatchBoard(),
                new BoardPosition(0, 0),
                new BoardPosition(-1, 0));

            Assert.That(evaluation.Outcome, Is.EqualTo(SwapOutcome.OutOfBounds));
        }

        [Test]
        public void Evaluate_SwapInvolvingProp_TriggersProp()
        {
            BoardState board = TestBoard.FromRows(
                "GHG",
                "BRB",
                "GB#");

            SwapEvaluation evaluation = SwapEvaluator.Evaluate(
                board,
                new BoardPosition(0, 1),
                new BoardPosition(0, 2));

            Assert.That(evaluation.Outcome, Is.EqualTo(SwapOutcome.PropTrigger));
            Assert.That(evaluation.IsLegal, Is.True);
        }

        [Test]
        public void FindLegalSwaps_ReturnsFixedRowMajorOrder()
        {
            IReadOnlyList<SwapMove> moves = SwapEvaluator.FindLegalSwaps(MatchBoard());

            Assert.That(moves, Has.Count.EqualTo(2));
            Assert.That(moves[0].From, Is.EqualTo(new BoardPosition(0, 1)));
            Assert.That(moves[0].To, Is.EqualTo(new BoardPosition(1, 1)));
            Assert.That(moves[0].Outcome, Is.EqualTo(SwapOutcome.NormalMatch));
            Assert.That(moves[1].From, Is.EqualTo(new BoardPosition(1, 1)));
            Assert.That(moves[1].To, Is.EqualTo(new BoardPosition(2, 1)));
            Assert.That(moves[1].Outcome, Is.EqualTo(SwapOutcome.NormalMatch));
        }
    }
}
