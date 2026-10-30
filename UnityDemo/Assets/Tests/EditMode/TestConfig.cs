using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;

namespace BagElimination.Tests
{
    internal static class TestConfig
    {
        internal static LevelConfig FromRows(params string[] rows)
        {
            return new LevelConfig(
                "test",
                rows[0].Length,
                rows.Length,
                AsciiLevelParser.Parse(rows),
                new PieceColor[0],
                1,
                LevelConfigValidator.SupportedConfigVersion);
        }

        internal static LevelConfig WithCell(LevelConfig source, CellSpec cell)
        {
            var cells = new List<CellSpec>(source.InitialCells) { cell };
            return new LevelConfig(
                source.LevelId,
                source.Width,
                source.Height,
                cells,
                source.RefillSequence,
                source.RandomSeed,
                source.ConfigVersion);
        }
    }
}
