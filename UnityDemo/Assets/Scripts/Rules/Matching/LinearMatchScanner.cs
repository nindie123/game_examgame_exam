using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;

namespace BagElimination.Rules.Matching
{
    /// <summary>
    /// Finds every horizontal/vertical straight-line match (a run of three or more same-color
    /// normal pieces) on a board, together with the backpacks orthogonally adjacent to any
    /// matched cell. This is the T03 linear-elimination scan: it only computes members and does
    /// not mutate the board, commit a deletion, or resolve props/fall/chain reactions.
    /// </summary>
    public static class LinearMatchScanner
    {
        public static LinearMatchResult FindMatches(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var matched = new HashSet<BoardPosition>();
            var backpacks = new HashSet<BoardPosition>();

            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    var position = new BoardPosition(row, column);
                    if (!MatchScanner.TryGetColor(board, position, out PieceColor color))
                    {
                        continue; // Empty, backpack or prop: cannot start a run.
                    }

                    CollectLine(board, position, color, 0, 1, matched);
                    CollectLine(board, position, color, 1, 0, matched);
                }
            }

            CollectAdjacentBackpacks(board, matched, backpacks);

            return new LinearMatchResult(
                SortRowMajor(board, matched),
                SortRowMajor(board, backpacks));
        }

        private static void CollectLine(
            BoardState board,
            BoardPosition start,
            PieceColor color,
            int rowStep,
            int columnStep,
            HashSet<BoardPosition> matched)
        {
            // Only the leading cell of a run records the whole run, so every line is emitted once
            // even when it shares cells with a crossing line or a longer run.
            if (MatchScanner.CountRun(board, start, -rowStep, -columnStep, color) != 0)
            {
                return; // Inside a run already handled by its leading cell.
            }

            int length = 1 + MatchScanner.CountRun(board, start, rowStep, columnStep, color);
            if (length < MatchScanner.MinimumRunLength)
            {
                return;
            }

            int row = start.Row;
            int column = start.Column;
            for (int i = 0; i < length; i++)
            {
                matched.Add(new BoardPosition(row, column));
                row += rowStep;
                column += columnStep;
            }
        }

        private static void CollectAdjacentBackpacks(
            BoardState board,
            HashSet<BoardPosition> matched,
            HashSet<BoardPosition> backpacks)
        {
            foreach (BoardPosition position in matched)
            {
                CheckBackpack(board, position.Row - 1, position.Column, backpacks);
                CheckBackpack(board, position.Row + 1, position.Column, backpacks);
                CheckBackpack(board, position.Row, position.Column - 1, backpacks);
                CheckBackpack(board, position.Row, position.Column + 1, backpacks);
            }
        }

        private static void CheckBackpack(BoardState board, int row, int column, HashSet<BoardPosition> backpacks)
        {
            if (row < 0 || row >= board.Height || column < 0 || column >= board.Width)
            {
                return;
            }

            if (board.GetCell(row, column).Type == ElementType.Backpack)
            {
                backpacks.Add(new BoardPosition(row, column));
            }
        }

        private static List<BoardPosition> SortRowMajor(BoardState board, HashSet<BoardPosition> positions)
        {
            var ordered = new List<BoardPosition>(positions.Count);
            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    var position = new BoardPosition(row, column);
                    if (positions.Contains(position))
                    {
                        ordered.Add(position);
                    }
                }
            }

            return ordered;
        }
    }
}