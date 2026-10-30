using BagElimination.Rules.Board;

namespace BagElimination.Rules.Matching
{
    /// <summary>
    /// Recognizes the match shapes used by the prototype: horizontal or vertical runs of at
    /// least three pieces, and a 2x2 block of one color. Backpacks, props and empty cells break
    /// a run and never match.
    /// </summary>
    public static class MatchScanner
    {
        public const int MinimumRunLength = 3;
        public const int BlockSize = 2;

        public static bool HasAnyMatch(BoardState board)
        {
            return TryFindAnyMatch(board, out _);
        }

        /// <summary>
        /// Scans row-major and returns the first cell that participates in any match.
        /// The position is used only for diagnostics; the match shape is not implied.
        /// </summary>
        public static bool TryFindAnyMatch(BoardState board, out BoardPosition position)
        {
            if (board == null)
            {
                throw new System.ArgumentNullException(nameof(board));
            }

            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    var candidate = new BoardPosition(row, column);
                    if (HasMatchAt(board, candidate))
                    {
                        position = candidate;
                        return true;
                    }
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// True when the piece at this position belongs to at least one match shape.
        /// Used by swap evaluation to ensure the swap itself creates a match.
        /// </summary>
        public static bool HasMatchAt(BoardState board, BoardPosition position)
        {
            if (board == null)
            {
                throw new System.ArgumentNullException(nameof(board));
            }

            if (!board.IsInside(position) || !TryGetColor(board, position, out PieceColor color))
            {
                return false;
            }

            int horizontal = 1
                + CountRun(board, position, 0, -1, color)
                + CountRun(board, position, 0, 1, color);
            if (horizontal >= MinimumRunLength)
            {
                return true;
            }

            int vertical = 1
                + CountRun(board, position, -1, 0, color)
                + CountRun(board, position, 1, 0, color);
            if (vertical >= MinimumRunLength)
            {
                return true;
            }

            return IsInMatchingBlock(board, position, color);
        }

        private static int CountRun(BoardState board, BoardPosition origin, int rowStep, int columnStep, PieceColor color)
        {
            int count = 0;
            int row = origin.Row + rowStep;
            int column = origin.Column + columnStep;
            while (row >= 0 && row < board.Height && column >= 0 && column < board.Width)
            {
                var cell = board.GetCell(row, column);
                if (!cell.IsMatchable || cell.Color != color)
                {
                    break;
                }

                count++;
                row += rowStep;
                column += columnStep;
            }

            return count;
        }

        private static bool IsInMatchingBlock(BoardState board, BoardPosition position, PieceColor color)
        {
            for (int rowOffset = 0; rowOffset < BlockSize; rowOffset++)
            {
                for (int columnOffset = 0; columnOffset < BlockSize; columnOffset++)
                {
                    int top = position.Row - rowOffset;
                    int left = position.Column - columnOffset;
                    if (top < 0 || left < 0 || top + BlockSize > board.Height || left + BlockSize > board.Width)
                    {
                        continue;
                    }

                    bool allMatch = true;
                    for (int row = top; row < top + BlockSize && allMatch; row++)
                    {
                        for (int column = left; column < left + BlockSize; column++)
                        {
                            var cell = board.GetCell(row, column);
                            if (!cell.IsMatchable || cell.Color != color)
                            {
                                allMatch = false;
                                break;
                            }
                        }
                    }

                    if (allMatch)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetColor(BoardState board, BoardPosition position, out PieceColor color)
        {
            var cell = board.GetCell(position);
            if (!cell.IsMatchable || !PieceColorRules.IsPlayable(cell.Color))
            {
                color = PieceColor.None;
                return false;
            }

            color = cell.Color;
            return true;
        }
    }
}
