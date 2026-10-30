using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class DemoLevelInitializationTests
    {
        [Test]
        public void Create_HasExpectedShapeAndObjectCounts()
        {
            BoardState board = BoardFactory.Create(DemoLevelConfig.Create());

            Assert.That(board.Width, Is.EqualTo(9));
            Assert.That(board.Height, Is.EqualTo(8));
            Assert.That(board.CountBackpacks(), Is.EqualTo(33));
            Assert.That(board.CountNormalPieces(), Is.EqualTo(39));
            Assert.That(board.CountObjects(), Is.EqualTo(72));
        }

        [Test]
        public void Create_MatchesConfiguredLayoutAtRepresentativeCells()
        {
            BoardState board = BoardFactory.Create(DemoLevelConfig.Create());

            BoardCell topLeft = board.GetCell(0, 0);
            Assert.That(topLeft.Type, Is.EqualTo(ElementType.NormalPiece));
            Assert.That(topLeft.Color, Is.EqualTo(PieceColor.Red));
            Assert.That(topLeft.ObjectId, Is.EqualTo(1));

            Assert.That(board.GetCell(4, 0).Type, Is.EqualTo(ElementType.Backpack));
            Assert.That(board.GetCell(4, 3).Color, Is.EqualTo(PieceColor.Red));
            Assert.That(board.GetCell(4, 5).Color, Is.EqualTo(PieceColor.Green));
            Assert.That(board.GetCell(4, 6).Type, Is.EqualTo(ElementType.Backpack));
            Assert.That(board.GetCell(7, 8).Type, Is.EqualTo(ElementType.Backpack));
            Assert.That(board.GetCell(7, 8).ObjectId, Is.EqualTo(72));
        }

        [Test]
        public void Create_EveryCellMatchesConfiguredLayout()
        {
            BoardState board = BoardFactory.Create(DemoLevelConfig.Create());
            IReadOnlyList<string> rows = DemoLevelConfig.GetLayoutRows();

            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    char symbol = rows[row][column];
                    BoardCell cell = board.GetCell(row, column);
                    string where = new BoardPosition(row, column).ToString();
                    if (symbol == '#')
                    {
                        Assert.That(cell.Type, Is.EqualTo(ElementType.Backpack), where);
                        Assert.That(cell.Color, Is.EqualTo(PieceColor.None), where);
                    }
                    else
                    {
                        Assert.That(cell.Type, Is.EqualTo(ElementType.NormalPiece), where);
                        Assert.That(cell.Color, Is.EqualTo(ExpectedColor(symbol)), where);
                    }
                }
            }
        }

        [Test]
        public void Create_Twice_ProducesIdenticalBoards()
        {
            BoardState first = BoardFactory.Create(DemoLevelConfig.Create());
            BoardState second = BoardFactory.Create(DemoLevelConfig.Create());

            for (int row = 0; row < first.Height; row++)
            {
                for (int column = 0; column < first.Width; column++)
                {
                    BoardCell left = first.GetCell(row, column);
                    BoardCell right = second.GetCell(row, column);
                    string where = new BoardPosition(row, column).ToString();
                    Assert.That(right.ObjectId, Is.EqualTo(left.ObjectId), where);
                    Assert.That(right.Type, Is.EqualTo(left.Type), where);
                    Assert.That(right.Color, Is.EqualTo(left.Color), where);
                    Assert.That(right.Direction, Is.EqualTo(left.Direction), where);
                }
            }
        }

        private static PieceColor ExpectedColor(char symbol)
        {
            switch (symbol)
            {
                case 'G': return PieceColor.Green;
                case 'B': return PieceColor.Blue;
                case 'R': return PieceColor.Red;
                case 'Y': return PieceColor.Yellow;
                default: throw new AssertionException("Unexpected demo layout symbol '" + symbol + "'.");
            }
        }

        [Test]
        public void Create_RefillSequenceIsEmptyInT01()
        {
            LevelConfig config = DemoLevelConfig.Create();

            Assert.That(config.RefillSequence, Is.Empty);
            Assert.That(config.Width, Is.EqualTo(9));
            Assert.That(config.Height, Is.EqualTo(8));
            Assert.That(config.RandomSeed, Is.EqualTo(20261030));
        }
    }
}
