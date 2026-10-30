using BagElimination.Rules.Board;
using BagElimination.Rules.Config;

namespace BagElimination.Tests
{
    /// <summary>
    /// Builds a board directly from symbols for fixed-layout rule tests. This bypasses config
    /// validation on purpose: tests need boards that are valid inputs to one rule even when they
    /// would not be a playable level opening.
    /// </summary>
    internal static class TestBoard
    {
        internal static BoardState FromRows(params string[] rows)
        {
            CellSpec[] specs = AsciiLevelParser.Parse(rows);
            int width = rows[0].Length;
            var cells = new BoardCell[width * rows.Length];
            int nextId = 1;
            for (int i = 0; i < specs.Length; i++)
            {
                CellSpec spec = specs[i];
                cells[spec.Position.ToIndex(width)] = Create(spec, nextId);
                nextId++;
            }

            return new BoardState(width, rows.Length, cells);
        }

        private static BoardCell Create(CellSpec spec, int objectId)
        {
            switch (spec.Type)
            {
                case ElementType.NormalPiece:
                    return BoardCell.CreateNormalPiece(objectId, spec.Color);
                case ElementType.Backpack:
                    return BoardCell.CreateBackpack(objectId);
                case ElementType.Rocket:
                    return BoardCell.CreateRocket(objectId, spec.Direction);
                case ElementType.Bomb:
                    return BoardCell.CreateBomb(objectId);
                case ElementType.Propeller:
                    return BoardCell.CreatePropeller(objectId);
                default:
                    return BoardCell.Empty;
            }
        }
    }
}
