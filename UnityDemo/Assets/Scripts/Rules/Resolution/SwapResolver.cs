using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Legality;
using BagElimination.Rules.Matching;

namespace BagElimination.Rules.Resolution
{
    /// <summary>
    /// Resolves a requested swap for the current (T03) scope: when it is a plain three-in-a-line
    /// it computes the committed board with the matched pieces and their four-neighbour backpacks
    /// cleared; every other legal-but-unsupported outcome is reported without committing. It never
    /// mutates the input board.
    /// </summary>
    public static class SwapResolver
    {
        public static SwapResolveResult Resolve(BoardState board, BoardPosition first, BoardPosition second)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            SwapOutcome outcome = SwapEvaluator.Evaluate(board, first, second).Outcome;
            if (outcome == SwapOutcome.PropTrigger)
            {
                return Reject(SwapResolveOutcome.SpecialUnsupported);
            }

            if (outcome != SwapOutcome.NormalMatch)
            {
                return Reject(SwapResolveOutcome.Rejected);
            }

            BoardState swapped = board.WithSwap(first, second);
            LinearMatchResult matches = LinearMatchScanner.FindMatches(swapped);
            if (!IsPlainThree(swapped, matches))
            {
                // A 2x2, a four-or-longer run, or a crossing shape resolves to special items in a
                // later task; do not delete it as a plain three-line now.
                return Reject(SwapResolveOutcome.SpecialUnsupported);
            }

            var toClear = new List<BoardPosition>(matches.MatchedCells.Count + matches.HitBackpacks.Count);
            toClear.AddRange(matches.MatchedCells);
            toClear.AddRange(matches.HitBackpacks);
            BoardState cleared = swapped.WithCellsCleared(toClear);

            return new SwapResolveResult(
                SwapResolveOutcome.NormalThree,
                cleared,
                matches.MatchedCells,
                matches.HitBackpacks);
        }

        private static bool IsPlainThree(BoardState swapped, LinearMatchResult matches)
        {
            IReadOnlyList<BoardPosition> cells = matches.MatchedCells;
            if (cells.Count != 3)
            {
                return false;
            }

            BoardPosition a = cells[0];
            BoardPosition b = cells[1];
            BoardPosition c = cells[2];
            bool straight = (a.Row == b.Row && b.Row == c.Row)
                || (a.Column == b.Column && b.Column == c.Column);
            if (!straight)
            {
                return false;
            }

            // A 3-run that is also part of a 2x2 square still resolves to a special item, so keep
            // it out of the plain clear.
            PieceColor color = swapped.GetCell(a).Color;
            for (int i = 0; i < cells.Count; i++)
            {
                if (MatchScanner.IsInMatchingBlock(swapped, cells[i], color))
                {
                    return false;
                }
            }

            return true;
        }

        private static SwapResolveResult Reject(SwapResolveOutcome outcome)
        {
            return new SwapResolveResult(
                outcome,
                null,
                Array.Empty<BoardPosition>(),
                Array.Empty<BoardPosition>());
        }
    }
}