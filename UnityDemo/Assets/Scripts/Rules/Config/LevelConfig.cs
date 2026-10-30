using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BagElimination.Rules.Board;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// Immutable level definition. The initial cells describe the opening board; the refill
    /// sequence is separate so gameplay randomness never changes the configured opening.
    /// </summary>
    public sealed class LevelConfig
    {
        private readonly ReadOnlyCollection<CellSpec> _initialCells;
        private readonly ReadOnlyCollection<PieceColor> _refillSequence;

        public LevelConfig(
            string levelId,
            int width,
            int height,
            IReadOnlyList<CellSpec> initialCells,
            IReadOnlyList<PieceColor> refillSequence,
            int randomSeed,
            int configVersion)
        {
            if (initialCells == null)
            {
                throw new ArgumentNullException(nameof(initialCells));
            }

            if (refillSequence == null)
            {
                throw new ArgumentNullException(nameof(refillSequence));
            }

            LevelId = levelId;
            Width = width;
            Height = height;
            RandomSeed = randomSeed;
            ConfigVersion = configVersion;

            var cells = new CellSpec[initialCells.Count];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = initialCells[i];
            }

            var colors = new PieceColor[refillSequence.Count];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = refillSequence[i];
            }

            _initialCells = Array.AsReadOnly(cells);
            _refillSequence = Array.AsReadOnly(colors);
        }

        public string LevelId { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<CellSpec> InitialCells => _initialCells;
        public IReadOnlyList<PieceColor> RefillSequence => _refillSequence;
        public int RandomSeed { get; }
        public int ConfigVersion { get; }
    }
}
