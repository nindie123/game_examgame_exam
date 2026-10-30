namespace BagElimination.Rules.Board
{
    /// <summary>
    /// Colors used by normal pieces. Backpacks and props always carry <see cref="None"/>.
    /// </summary>
    public enum PieceColor
    {
        None = 0,
        Green = 1,
        Blue = 2,
        Red = 3,
        Yellow = 4,
    }

    public static class PieceColorRules
    {
        /// <summary>
        /// Only these four colors may appear on a normal piece. Keeping the check in one place
        /// prevents config validation, board construction and match scanning from drifting apart.
        /// </summary>
        public static bool IsPlayable(PieceColor color)
        {
            return color == PieceColor.Green
                || color == PieceColor.Blue
                || color == PieceColor.Red
                || color == PieceColor.Yellow;
        }
    }
}
