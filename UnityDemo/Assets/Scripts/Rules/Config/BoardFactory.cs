using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// Builds the opening <see cref="BoardState"/> from a level config. Object ids are allocated
    /// row-major so the same config always produces the same ids.
    /// </summary>
    public static class BoardFactory
    {
        public static BoardState Create(LevelConfig config, ObjectIdAllocator allocator = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            LevelConfigValidationResult result = LevelConfigValidator.ValidateStructure(config);
            if (!result.IsValid)
            {
                throw new LevelConfigException(result.Errors);
            }

            return CreateUnchecked(config, allocator ?? new ObjectIdAllocator());
        }

        /// <summary>
        /// Builds a board without validation. Only the validator and tests that deliberately
        /// exercise invalid configs should use this path.
        /// </summary>
        internal static BoardState CreateUnchecked(LevelConfig config, ObjectIdAllocator allocator)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (allocator == null)
            {
                throw new ArgumentNullException(nameof(allocator));
            }

            int expectedLength = config.Width * config.Height;
            var cells = new BoardCell[expectedLength];

            var ordered = new List<CellSpec>(config.InitialCells);
            ordered.Sort(CompareByBoardOrder);
            for (int i = 0; i < ordered.Count; i++)
            {
                CellSpec spec = ordered[i];
                cells[spec.Position.ToIndex(config.Width)] = CreateCell(spec, allocator.Allocate());
            }

            return new BoardState(config.Width, config.Height, cells);
        }

        private static BoardCell CreateCell(CellSpec spec, int objectId)
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
                    throw new InvalidOperationException(
                        "Cannot build a board cell for element type " + spec.Type + " at " + spec.Position + ".");
            }
        }

        private static int CompareByBoardOrder(CellSpec left, CellSpec right)
        {
            if (left.Position.Row != right.Position.Row)
            {
                return left.Position.Row.CompareTo(right.Position.Row);
            }

            return left.Position.Column.CompareTo(right.Position.Column);
        }
    }
}
