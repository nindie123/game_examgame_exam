using BagElimination.Rules.Board;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// One configured cell of a level. A config describes objects explicitly;
    /// cells without a spec are empty.
    /// </summary>
    public readonly struct CellSpec
    {
        public CellSpec(BoardPosition position, ElementType type, PieceColor color, RocketDirection direction)
        {
            Position = position;
            Type = type;
            Color = color;
            Direction = direction;
        }

        public BoardPosition Position { get; }
        public ElementType Type { get; }
        public PieceColor Color { get; }
        public RocketDirection Direction { get; }

        public override string ToString()
        {
            return Position + " " + Type + " " + Color + " " + Direction;
        }
    }
}
