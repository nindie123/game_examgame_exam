using System.Collections.Generic;
using BagElimination.Rules.Board;

namespace BagElimination.Rules.Resolution
{
    /// <summary>
    /// The outcome of resolving one swap. <see cref="Board"/> is the committed post-clear snapshot
    /// only for <see cref="SwapResolveOutcome.NormalThree"/>; otherwise it is null and the two
    /// lists are empty.
    /// </summary>
    public sealed class SwapResolveResult
    {
        public SwapResolveResult(
            SwapResolveOutcome outcome,
            BoardState board,
            IReadOnlyList<BoardPosition> matchedCells,
            IReadOnlyList<BoardPosition> hitBackpacks)
        {
            Outcome = outcome;
            Board = board;
            MatchedCells = matchedCells;
            HitBackpacks = hitBackpacks;
        }

        public SwapResolveOutcome Outcome { get; }

        public BoardState Board { get; }

        public IReadOnlyList<BoardPosition> MatchedCells { get; }

        public IReadOnlyList<BoardPosition> HitBackpacks { get; }
    }
}