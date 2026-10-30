using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Legality;
using BagElimination.Rules.Matching;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// Validates a level before the board is built. Structure checks catch malformed data;
    /// playability checks catch openings that already contain a match or have no legal move.
    /// </summary>
    public static class LevelConfigValidator
    {
        public const int SupportedConfigVersion = 1;

        public static LevelConfigValidationResult Validate(LevelConfig config)
        {
            LevelConfigValidationResult structure = ValidateStructure(config);
            if (!structure.IsValid)
            {
                return structure;
            }

            var errors = new List<LevelConfigError>();
            BoardState board = BoardFactory.CreateUnchecked(config, new ObjectIdAllocator());

            if (MatchScanner.TryFindAnyMatch(board, out BoardPosition matchPosition))
            {
                // An opening match is already a rule violation; a deadlock report would only add noise.
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.ImmediateMatch,
                    "The opening board already contains a match.",
                    matchPosition));
                return new LevelConfigValidationResult(errors);
            }

            if (!SwapEvaluator.HasLegalSwap(board))
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.NoLegalMove,
                    "The opening board has no legal swap."));
            }

            return new LevelConfigValidationResult(errors);
        }

        public static LevelConfigValidationResult ValidateStructure(LevelConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var errors = new List<LevelConfigError>();

            if (config.ConfigVersion != SupportedConfigVersion)
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.InvalidConfigVersion,
                    "Config version " + config.ConfigVersion + " is not supported; expected " + SupportedConfigVersion + "."));
            }

            if (string.IsNullOrWhiteSpace(config.LevelId))
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.InvalidLevelId,
                    "The level id must not be empty."));
            }

            if (config.Width <= 0 || config.Height <= 0)
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.InvalidDimensions,
                    "Board dimensions must be positive; got " + config.Width + "x" + config.Height + "."));
                return new LevelConfigValidationResult(errors);
            }

            if ((long)config.Width * config.Height > int.MaxValue)
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.InvalidDimensions,
                    "Board dimensions exceed the supported array length."));
                return new LevelConfigValidationResult(errors);
            }

            var seenIndices = new HashSet<int>();
            int backpackCount = 0;
            for (int i = 0; i < config.InitialCells.Count; i++)
            {
                CellSpec spec = config.InitialCells[i];
                if (!spec.Position.IsInside(config.Width, config.Height))
                {
                    errors.Add(new LevelConfigError(
                        LevelConfigErrorCode.OutOfBoundsCell,
                        "Cell position is outside the " + config.Width + "x" + config.Height + " board.",
                        spec.Position));
                    continue;
                }

                if (!seenIndices.Add(spec.Position.ToIndex(config.Width)))
                {
                    errors.Add(new LevelConfigError(
                        LevelConfigErrorCode.DuplicateCell,
                        "More than one cell is configured at this position.",
                        spec.Position));
                    continue;
                }

                ValidateSpec(spec, errors);
                if (spec.Type == ElementType.Backpack)
                {
                    backpackCount++;
                }
            }

            if (backpackCount == 0)
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.NoBackpacks,
                    "The level must contain at least one backpack."));
            }

            for (int i = 0; i < config.RefillSequence.Count; i++)
            {
                if (!PieceColorRules.IsPlayable(config.RefillSequence[i]))
                {
                    errors.Add(new LevelConfigError(
                        LevelConfigErrorCode.InvalidRefillColor,
                        "Refill entry " + (i + 1) + " is not a playable color."));
                }
            }

            return new LevelConfigValidationResult(errors);
        }

        private static void ValidateSpec(CellSpec spec, List<LevelConfigError> errors)
        {
            switch (spec.Type)
            {
                case ElementType.Empty:
                    errors.Add(new LevelConfigError(
                        LevelConfigErrorCode.EmptyCellSpec,
                        "An explicit cell spec must not describe an empty cell; omit it instead.",
                        spec.Position));
                    break;

                case ElementType.NormalPiece:
                    if (!PieceColorRules.IsPlayable(spec.Color))
                    {
                        errors.Add(new LevelConfigError(
                            LevelConfigErrorCode.InvalidColor,
                            "A normal piece needs Green, Blue, Red or Yellow.",
                            spec.Position));
                    }

                    if (spec.Direction != RocketDirection.None)
                    {
                        errors.Add(new LevelConfigError(
                            LevelConfigErrorCode.InvalidRocketDirection,
                            "Only a rocket may carry a direction.",
                            spec.Position));
                    }

                    break;

                case ElementType.Backpack:
                    ValidateNoColor(spec, errors);
                    ValidateNoDirection(spec, errors);
                    break;

                case ElementType.Rocket:
                    ValidateNoColor(spec, errors);
                    if (spec.Direction != RocketDirection.Horizontal && spec.Direction != RocketDirection.Vertical)
                    {
                        errors.Add(new LevelConfigError(
                            LevelConfigErrorCode.InvalidRocketDirection,
                            "A rocket needs a horizontal or vertical direction.",
                            spec.Position));
                    }

                    break;

                case ElementType.Bomb:
                case ElementType.Propeller:
                    ValidateNoColor(spec, errors);
                    ValidateNoDirection(spec, errors);
                    break;

                default:
                    errors.Add(new LevelConfigError(
                        LevelConfigErrorCode.InvalidElementType,
                        "Unknown element type value " + (int)spec.Type + ".",
                        spec.Position));
                    break;
            }
        }

        private static void ValidateNoColor(CellSpec spec, List<LevelConfigError> errors)
        {
            if (spec.Color != PieceColor.None)
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.InvalidColor,
                    "Only a normal piece may carry a color.",
                    spec.Position));
            }
        }

        private static void ValidateNoDirection(CellSpec spec, List<LevelConfigError> errors)
        {
            if (spec.Direction != RocketDirection.None)
            {
                errors.Add(new LevelConfigError(
                    LevelConfigErrorCode.InvalidRocketDirection,
                    "Only a rocket may carry a direction.",
                    spec.Position));
            }
        }
    }
}
