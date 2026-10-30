using BagElimination.Rules.Board;

namespace BagElimination.Rules.Legality
{
    public readonly struct SwapMove
    {
        public SwapMove(BoardPosition from, BoardPosition to, SwapOutcome outcome)
        {
            From = from;
            To = to;
            Outcome = outcome;
        }

        public BoardPosition From { get; }
        public BoardPosition To { get; }
        public SwapOutcome Outcome { get; }
        public bool IsLegal => Outcome.IsLegal();
    }
}
