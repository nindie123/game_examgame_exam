namespace BagElimination.Rules.Config
{
    public enum LevelConfigErrorCode
    {
        InvalidDimensions = 0,
        InvalidConfigVersion = 1,
        InvalidLevelId = 2,
        OutOfBoundsCell = 3,
        DuplicateCell = 4,
        EmptyCellSpec = 5,
        InvalidElementType = 6,
        InvalidColor = 7,
        InvalidRocketDirection = 8,
        NoBackpacks = 9,
        InvalidRefillColor = 10,
        ImmediateMatch = 11,
        NoLegalMove = 12,
    }
}
