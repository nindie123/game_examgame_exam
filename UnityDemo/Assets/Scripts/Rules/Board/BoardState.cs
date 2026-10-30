using System;
using System.Collections.Generic;

namespace BagElimination.Rules.Board
{
    /// <summary>
    /// Immutable authoritative board snapshot. The flat array uses index row * Width + Column.
    /// Rules calculate a new snapshot and the resolver commits it; presentation never mutates a board.
    /// </summary>
    public sealed class BoardState
    {
        private readonly BoardCell[] _cells;

        /// <summary>
        /// Low-level constructor for board construction and fixed test layouts.
        /// Use <c>BoardFactory</c> when loading a level config.
        /// </summary>
        public BoardState(int width, int height, BoardCell[] cells)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            }

            if ((long)width * height > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Board size exceeds the supported array length.");
            }

            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            int expectedLength = width * height;
            if (cells.Length != expectedLength)
            {
                throw new ArgumentException(
                    "Cell array length " + cells.Length + " does not match " + width + "x" + height + ".",
                    nameof(cells));
            }

            Width = width;
            Height = height;
            _cells = (BoardCell[])cells.Clone();
        }

        public int Width { get; }
        public int Height { get; }

        public BoardCell GetCell(int row, int column)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row is outside the board.");
            }

            if (column < 0 || column >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(column), column, "Column is outside the board.");
            }

            return _cells[row * Width + column];
        }

        public BoardCell GetCell(BoardPosition position)
        {
            return GetCell(position.Row, position.Column);
        }

        public bool IsInside(BoardPosition position)
        {
            return position.IsInside(Width, Height);
        }

        /// <summary>
        /// Mechanical swap used by legality checks. Rule validity (adjacency, backpacks, match)
        /// belongs to <c>SwapEvaluator</c>; this method only moves the two cells and their object ids.
        /// </summary>
        public BoardState WithSwap(BoardPosition first, BoardPosition second)
        {
            if (!IsInside(first))
            {
                throw new ArgumentOutOfRangeException(nameof(first), first, "First position is outside the board.");
            }

            if (!IsInside(second))
            {
                throw new ArgumentOutOfRangeException(nameof(second), second, "Second position is outside the board.");
            }

            var copy = (BoardCell[])_cells.Clone();
            int firstIndex = first.ToIndex(Width);
            int secondIndex = second.ToIndex(Width);
            BoardCell temporary = copy[firstIndex];
            copy[firstIndex] = copy[secondIndex];
            copy[secondIndex] = temporary;
            return new BoardState(Width, Height, copy);
        }

        public int CountBackpacks()
        {
            int count = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i].Type == ElementType.Backpack)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountNormalPieces()
        {
            int count = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i].Type == ElementType.NormalPiece)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountObjects()
        {
            int count = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i].IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }

        public IEnumerable<BoardPosition> EnumeratePositions()
        {
            for (int row = 0; row < Height; row++)
            {
                for (int column = 0; column < Width; column++)
                {
                    yield return new BoardPosition(row, column);
                }
            }
        }
    }
}
