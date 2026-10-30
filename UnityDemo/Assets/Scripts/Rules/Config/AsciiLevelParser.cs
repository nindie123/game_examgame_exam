using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// Pitch symbol mapping. Only layout and object kinds are described here; rule validity is
    /// checked by <see cref="LevelConfigValidator"/>.
    /// </summary>
    public static class AsciiLevelParser
    {
        public const char EmptySymbol = '.';
        public const char BackpackSymbol = '#';

        public static CellSpec[] Parse(string[] rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (rows.Length == 0)
            {
                throw new FormatException("The level layout has no rows.");
            }

            if (rows[0] == null || rows[0].Length == 0)
            {
                throw new FormatException("The first level row is empty.");
            }

            int width = rows[0].Length;
            for (int row = 0; row < rows.Length; row++)
            {
                if (rows[row] == null)
                {
                    throw new FormatException("Level row " + (row + 1) + " is null.");
                }

                if (rows[row].Length != width)
                {
                    throw new FormatException(
                        "Level row " + (row + 1) + " has length " + rows[row].Length + "; expected " + width + ".");
                }
            }

            var specs = new List<CellSpec>();
            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    char symbol = rows[row][column];
                    if (symbol == EmptySymbol)
                    {
                        continue;
                    }

                    var position = new BoardPosition(row, column);
                    specs.Add(ParseSymbol(symbol, position));
                }
            }

            return specs.ToArray();
        }

        private static CellSpec ParseSymbol(char symbol, BoardPosition position)
        {
            switch (symbol)
            {
                case BackpackSymbol:
                    return new CellSpec(position, ElementType.Backpack, PieceColor.None, RocketDirection.None);
                case 'G':
                    return Normal(position, PieceColor.Green);
                case 'B':
                    return Normal(position, PieceColor.Blue);
                case 'R':
                    return Normal(position, PieceColor.Red);
                case 'Y':
                    return Normal(position, PieceColor.Yellow);
                case 'H':
                    return new CellSpec(position, ElementType.Rocket, PieceColor.None, RocketDirection.Horizontal);
                case 'V':
                    return new CellSpec(position, ElementType.Rocket, PieceColor.None, RocketDirection.Vertical);
                case 'X':
                    return new CellSpec(position, ElementType.Bomb, PieceColor.None, RocketDirection.None);
                case 'P':
                    return new CellSpec(position, ElementType.Propeller, PieceColor.None, RocketDirection.None);
                default:
                    throw new FormatException(
                        "Unknown layout symbol '" + symbol + "' at row " + (position.Row + 1)
                        + ", column " + (position.Column + 1) + ".");
            }
        }

        private static CellSpec Normal(BoardPosition position, PieceColor color)
        {
            return new CellSpec(position, ElementType.NormalPiece, color, RocketDirection.None);
        }
    }
}
