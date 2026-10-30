using System.Collections.Generic;
using BagElimination.Rules.Board;

namespace BagElimination.Rules.Matching
{
    /// <summary>
    /// The full set of linear matches on a board, split into the matched normal-piece cells and
    /// the backpacks those matches hit. Both lists are ordered top-to-bottom then left-to-right
    /// and contain no duplicates. This result is read-only; it never carries a board snapshot
    /// because scanning does not mutate or consume the board.
    /// </summary>
    public sealed class LinearMatchResult
    {
        public LinearMatchResult(
            IReadOnlyList<BoardPosition> matchedCells,
            IReadOnlyList<BoardPosition> hitBackpacks)
        {
            MatchedCells = matchedCells;
            HitBackpacks = hitBackpacks;
        }

        /// <summary>
        /// Every normal-piece cell that belongs to at least one horizontal/vertical run of three
        /// or more same-color pieces. A cell in both a run and a crossing run appears once.
        /// </summary>
        public IReadOnlyList<BoardPosition> MatchedCells { get; }

        /// <summary>
        /// Backpacks orthogonally adjacent (up/down/left/right) to at least one matched cell.
        /// A backpack touched by several matched cells appears once.
        /// </summary>
        public IReadOnlyList<BoardPosition> HitBackpacks { get; }

        public bool HasMatch => MatchedCells.Count > 0;
    }
}