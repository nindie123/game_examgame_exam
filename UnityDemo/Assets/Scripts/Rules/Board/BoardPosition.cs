using System;

namespace BagElimination.Rules.Board
{
    /// <summary>
    /// A zero-based board coordinate. Config files and diagnostics use one-based coordinates;
    /// this type keeps the rules layer aligned with the flat index formula row * width + column.
    /// </summary>
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public BoardPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        public bool IsInside(int width, int height)
        {
            return Row >= 0 && Row < height && Column >= 0 && Column < width;
        }

        /// <summary>
        /// Converts to the flat board index. Callers must check <see cref="IsInside"/> first;
        /// this method deliberately does not hide an out-of-bounds position.
        /// </summary>
        public int ToIndex(int width)
        {
            return Row * width + Column;
        }

        public bool Equals(BoardPosition other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Column;
            }
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !left.Equals(right);
        }

        /// <summary>One-based display form used in diagnostics and configuration errors.</summary>
        public override string ToString()
        {
            return "(" + (Row + 1) + ", " + (Column + 1) + ")";
        }
    }
}
