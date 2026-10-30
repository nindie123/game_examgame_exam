namespace BagElimination.Rules.Legality
{
    /// <summary>
    /// Result of asking whether a swap is legal. The outcome names the reason so callers do not
    /// need to duplicate legality rules.
    /// </summary>
    public enum SwapOutcome
    {
        OutOfBounds = 0,
        NotAdjacent = 1,
        Blocked = 2,
        NoMatch = 3,
        NormalMatch = 4,
        PropTrigger = 5,
    }

    public static class SwapOutcomeRules
    {
        public static bool IsLegal(this SwapOutcome outcome)
        {
            return outcome == SwapOutcome.NormalMatch || outcome == SwapOutcome.PropTrigger;
        }
    }
}
