namespace BagElimination.Rules.Resolution
{
    /// <summary>
    /// What a requested swap resolves to in the current scope. Only <see cref="NormalThree"/>
    /// commits a new board; the other outcomes leave the authoritative board unchanged and the
    /// presentation plays a revert.
    /// </summary>
    public enum SwapResolveOutcome
    {
        /// <summary>The swap has no match; revert without committing.</summary>
        Rejected = 0,

        /// <summary>One plain three-in-a-line plus adjacent backpack clear; the board is committed.</summary>
        NormalThree = 1,

        /// <summary>Legal under the full rules (four+, crossing, 2x2, prop) but out of this round's scope.</summary>
        SpecialUnsupported = 2,
    }
}