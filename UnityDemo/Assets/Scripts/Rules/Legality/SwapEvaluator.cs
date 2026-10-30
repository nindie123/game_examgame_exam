using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Matching;

namespace BagElimination.Rules.Legality
{
    /// <summary>
    /// Decides whether a player swap is legal. Only adjacent swaps are considered. Backpacks and
    /// empty cells block the swap; a swap that moves a prop triggers it; two normal pieces must
    /// create a match at one of the two swapped positions.
    /// </summary>
    public static class SwapEvaluator
    {
        public static SwapEvaluation Evaluate(BoardState board, BoardPosition first, BoardPosition second)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!board.IsInside(first) || !board.IsInside(second))
            {
                return new SwapEvaluation(SwapOutcome.OutOfBounds);
            }

            if (!IsAdjacent(first, second))
            {
                return new SwapEvaluation(SwapOutcome.NotAdjacent);
            }

            BoardCell firstCell = board.GetCell(first);
            BoardCell secondCell = board.GetCell(second);
            if (!firstCell.IsMovable || !secondCell.IsMovable)
            {
                return new SwapEvaluation(SwapOutcome.Blocked);
            }

            if (IsProp(firstCell.Type) || IsProp(secondCell.Type))
            {
                return new SwapEvaluation(SwapOutcome.PropTrigger);
            }

            BoardState swapped = board.WithSwap(first, second);
            bool createsMatch = MatchScanner.HasMatchAt(swapped, first)
                || MatchScanner.HasMatchAt(swapped, second);
            return new SwapEvaluation(createsMatch ? SwapOutcome.NormalMatch : SwapOutcome.NoMatch);
        }

        /// <summary>
        /// Enumerates legal swaps in a fixed order: row-major, and for each cell right before down.
        /// The order makes deadlock checks and test output reproducible.
        /// </summary>
        public static IReadOnlyList<SwapMove> FindLegalSwaps(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var moves = new List<SwapMove>();
            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    var from = new BoardPosition(row, column);
                    if (column + 1 < board.Width)
                    {
                        AddIfLegal(board, moves, from, new BoardPosition(row, column + 1));
                    }

                    if (row + 1 < board.Height)
                    {
                        AddIfLegal(board, moves, from, new BoardPosition(row + 1, column));
                    }
                }
            }

            return moves;
        }

        public static bool HasLegalSwap(BoardState board)
        {
            return FindLegalSwaps(board).Count > 0;
        }

        private static void AddIfLegal(BoardState board, List<SwapMove> moves, BoardPosition from, BoardPosition to)
        {
            SwapOutcome outcome = Evaluate(board, from, to).Outcome;
            if (outcome.IsLegal())
            {
                moves.Add(new SwapMove(from, to, outcome));
            }
        }

        private static bool IsAdjacent(BoardPosition first, BoardPosition second)
        {
            int rowDistance = Math.Abs(first.Row - second.Row);
            int columnDistance = Math.Abs(first.Column - second.Column);
            return rowDistance + columnDistance == 1;
        }

        private static bool IsProp(ElementType type)
        {
            return type == ElementType.Rocket || type == ElementType.Bomb || type == ElementType.Propeller;
        }
    }
}
