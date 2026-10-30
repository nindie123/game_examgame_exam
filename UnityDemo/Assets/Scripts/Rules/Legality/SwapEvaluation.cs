namespace BagElimination.Rules.Legality
{
    public readonly struct SwapEvaluation
    {
        public SwapEvaluation(SwapOutcome outcome)
        {
            Outcome = outcome;
        }

        public SwapOutcome Outcome { get; }
        public bool IsLegal => Outcome.IsLegal();
    }
}
