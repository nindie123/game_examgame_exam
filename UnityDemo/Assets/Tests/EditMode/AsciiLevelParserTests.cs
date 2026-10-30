using System;
using System.Linq;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class AsciiLevelParserTests
    {
        [Test]
        public void Parse_MapsEverySymbolAndOmitsEmptyCells()
        {
            CellSpec[] cells = AsciiLevelParser.Parse(new[]
            {
                "GHX",
                "R.B",
                "YVP",
            });

            Assert.That(cells, Has.Length.EqualTo(8));
            AssertSpec(cells, 0, 0, ElementType.NormalPiece, PieceColor.Green, RocketDirection.None);
            AssertSpec(cells, 0, 1, ElementType.Rocket, PieceColor.None, RocketDirection.Horizontal);
            AssertSpec(cells, 0, 2, ElementType.Bomb, PieceColor.None, RocketDirection.None);
            AssertSpec(cells, 1, 0, ElementType.NormalPiece, PieceColor.Red, RocketDirection.None);
            AssertSpec(cells, 1, 2, ElementType.NormalPiece, PieceColor.Blue, RocketDirection.None);
            AssertSpec(cells, 2, 0, ElementType.NormalPiece, PieceColor.Yellow, RocketDirection.None);
            AssertSpec(cells, 2, 1, ElementType.Rocket, PieceColor.None, RocketDirection.Vertical);
            AssertSpec(cells, 2, 2, ElementType.Propeller, PieceColor.None, RocketDirection.None);
            Assert.That(cells.Any(cell => cell.Position == new BoardPosition(1, 1)), Is.False);
        }

        [Test]
        public void Parse_RejectsRowsWithDifferentLengths()
        {
            FormatException exception = Assert.Throws<FormatException>(() => AsciiLevelParser.Parse(new[]
            {
                "RGB",
                "RB",
            }));

            StringAssert.Contains("row 2", exception.Message);
            StringAssert.Contains("expected 3", exception.Message);
        }

        [Test]
        public void Parse_RejectsUnknownSymbolWithOneBasedPosition()
        {
            FormatException exception = Assert.Throws<FormatException>(() => AsciiLevelParser.Parse(new[]
            {
                "RG?",
            }));

            StringAssert.Contains("'?'", exception.Message);
            StringAssert.Contains("row 1", exception.Message);
            StringAssert.Contains("column 3", exception.Message);
        }

        private static void AssertSpec(
            CellSpec[] cells,
            int row,
            int column,
            ElementType type,
            PieceColor color,
            RocketDirection direction)
        {
            var position = new BoardPosition(row, column);
            CellSpec spec = cells.Single(cell => cell.Position == position);
            Assert.That(spec.Type, Is.EqualTo(type), position.ToString());
            Assert.That(spec.Color, Is.EqualTo(color), position.ToString());
            Assert.That(spec.Direction, Is.EqualTo(direction), position.ToString());
        }
    }
}
