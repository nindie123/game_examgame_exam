namespace BagElimination.Rules.Board
{
    /// <summary>
    /// Kinds of object that can occupy a board cell.
    /// <see cref="Empty"/> means no board object. Backpacks are immovable and do not match;
    /// special props move and fall but never participate in color matching.
    /// </summary>
    public enum ElementType
    {
        Empty = 0,
        NormalPiece = 1,
        Backpack = 2,
        Rocket = 3,
        Bomb = 4,
        Propeller = 5,
    }
}
