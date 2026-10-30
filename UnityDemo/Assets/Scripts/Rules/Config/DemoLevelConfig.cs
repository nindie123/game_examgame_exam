using System.Collections.Generic;
using BagElimination.Rules.Board;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// The fixed opening layout for the architecture prototype. It is written as symbols so the
    /// layout can be read in one place, and it is validated before a board is built from it.
    /// The refill sequence is intentionally empty in T01; refill behaviour belongs to a later task.
    /// </summary>
    public static class DemoLevelConfig
    {
        public const string LevelId = "demo-9x8";
        public const int Width = 9;
        public const int Height = 8;
        public const int BackpackCount = 33;
        public const int NormalPieceCount = 39;
        public const int RandomSeed = 20261030;
        public const int ConfigVersion = 1;

        private static readonly string[] Layout =
        {
            "RBYGYRYBB",
            "BGRYGRYRG",
            "YRRGRBRYB",
            "YRBRBGRRB",
            "###RRG###",
            "#########",
            "#########",
            "#########",
        };

        public static LevelConfig Create()
        {
            CellSpec[] cells = AsciiLevelParser.Parse(Layout);
            return new LevelConfig(
                LevelId,
                Width,
                Height,
                cells,
                new PieceColor[0],
                RandomSeed,
                ConfigVersion);
        }

        /// <summary>Read-only copy of the layout rows, useful for diagnostics and tests.</summary>
        public static IReadOnlyList<string> GetLayoutRows()
        {
            return (string[])Layout.Clone();
        }
    }
}
