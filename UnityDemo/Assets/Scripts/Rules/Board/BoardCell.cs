using System;

namespace BagElimination.Rules.Board
{
    /// <summary>
    /// Immutable content of one board cell. Object ids are unique across all occupied cells;
    /// an id travels with its object when the object moves.
    /// </summary>
    public readonly struct BoardCell
    {
        private BoardCell(int objectId, ElementType type, PieceColor color, RocketDirection direction)
        {
            ObjectId = objectId;
            Type = type;
            Color = color;
            Direction = direction;
        }

        public int ObjectId { get; }
        public ElementType Type { get; }
        public PieceColor Color { get; }
        public RocketDirection Direction { get; }

        public bool IsOccupied => Type != ElementType.Empty;
        public bool IsMatchable => Type == ElementType.NormalPiece;

        public bool IsMovable =>
            Type == ElementType.NormalPiece
            || Type == ElementType.Rocket
            || Type == ElementType.Bomb
            || Type == ElementType.Propeller;

        public static BoardCell Empty => default;

        public static BoardCell CreateNormalPiece(int objectId, PieceColor color)
        {
            ValidateObjectId(objectId);
            if (!PieceColorRules.IsPlayable(color))
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "A normal piece needs a playable color.");
            }

            return new BoardCell(objectId, ElementType.NormalPiece, color, RocketDirection.None);
        }

        public static BoardCell CreateBackpack(int objectId)
        {
            ValidateObjectId(objectId);
            return new BoardCell(objectId, ElementType.Backpack, PieceColor.None, RocketDirection.None);
        }

        public static BoardCell CreateRocket(int objectId, RocketDirection direction)
        {
            ValidateObjectId(objectId);
            if (direction != RocketDirection.Horizontal && direction != RocketDirection.Vertical)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "A rocket needs a horizontal or vertical direction.");
            }

            return new BoardCell(objectId, ElementType.Rocket, PieceColor.None, direction);
        }

        public static BoardCell CreateBomb(int objectId)
        {
            ValidateObjectId(objectId);
            return new BoardCell(objectId, ElementType.Bomb, PieceColor.None, RocketDirection.None);
        }

        public static BoardCell CreatePropeller(int objectId)
        {
            ValidateObjectId(objectId);
            return new BoardCell(objectId, ElementType.Propeller, PieceColor.None, RocketDirection.None);
        }

        private static void ValidateObjectId(int objectId)
        {
            if (objectId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(objectId), objectId, "Object ids must be positive.");
            }
        }

        public override string ToString()
        {
            if (Type == ElementType.Empty)
            {
                return "Empty";
            }

            string detail = Type == ElementType.NormalPiece
                ? Color.ToString()
                : Direction != RocketDirection.None ? Direction.ToString() : string.Empty;
            return Type + "#" + ObjectId + (detail.Length == 0 ? string.Empty : "(" + detail + ")");
        }
    }
}
