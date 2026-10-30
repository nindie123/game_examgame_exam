using System.Linq;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using NUnit.Framework;

namespace BagElimination.Tests
{
    public sealed class LevelConfigValidationTests
    {
        // Structurally valid test layout with one backpack and no legal swap.
        private static LevelConfig BaseConfig()
        {
            return TestConfig.FromRows(
                "GRB",
                "BRG",
                "GB#");
        }

        [Test]
        public void ValidateStructure_ValidLayout_HasNoErrors()
        {
            LevelConfigValidationResult result = LevelConfigValidator.ValidateStructure(BaseConfig());

            Assert.That(result.IsValid, Is.True, Describe(result));
        }

        [Test]
        public void Validate_DemoLevel_HasNoErrors()
        {
            LevelConfigValidationResult result = LevelConfigValidator.Validate(DemoLevelConfig.Create());

            Assert.That(result.IsValid, Is.True, Describe(result));
        }

        [Test]
        public void Validate_DemoLevel_HasNoImmediateMatchAndAtLeastOneLegalSwap()
        {
            LevelConfigValidationResult result = LevelConfigValidator.Validate(DemoLevelConfig.Create());

            Assert.That(result.Errors.Any(error => error.Code == LevelConfigErrorCode.ImmediateMatch), Is.False);
            Assert.That(result.Errors.Any(error => error.Code == LevelConfigErrorCode.NoLegalMove), Is.False);
        }

        [Test]
        public void Validate_OpeningWithImmediateMatch_ReportsImmediateMatch()
        {
            LevelConfig config = TestConfig.FromRows(
                "RRR",
                "GBG",
                "B#B");

            LevelConfigValidationResult result = LevelConfigValidator.Validate(config);

            LevelConfigError error = result.Errors.Single(item => item.Code == LevelConfigErrorCode.ImmediateMatch);
            Assert.That(error.Position, Is.EqualTo(new BoardPosition(0, 0)));
        }

        [Test]
        public void Validate_OpeningWithoutLegalSwap_ReportsNoLegalMove()
        {
            LevelConfigValidationResult result = LevelConfigValidator.Validate(BaseConfig());

            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Code, Is.EqualTo(LevelConfigErrorCode.NoLegalMove));
        }

        [Test]
        public void ValidateStructure_NonPositiveDimensions_ReportsInvalidDimensions()
        {
            var config = new LevelConfig(
                "test",
                0,
                3,
                new CellSpec[0],
                new PieceColor[0],
                1,
                LevelConfigValidator.SupportedConfigVersion);

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.InvalidDimensions);
        }

        [Test]
        public void ValidateStructure_UnsupportedVersion_ReportsInvalidConfigVersion()
        {
            LevelConfig source = BaseConfig();
            var config = new LevelConfig(
                source.LevelId,
                source.Width,
                source.Height,
                source.InitialCells,
                source.RefillSequence,
                source.RandomSeed,
                LevelConfigValidator.SupportedConfigVersion + 1);

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.InvalidConfigVersion);
        }

        [Test]
        public void ValidateStructure_BlankLevelId_ReportsInvalidLevelId()
        {
            LevelConfig source = BaseConfig();
            var config = new LevelConfig(
                "  ",
                source.Width,
                source.Height,
                source.InitialCells,
                source.RefillSequence,
                source.RandomSeed,
                source.ConfigVersion);

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.InvalidLevelId);
        }

        [Test]
        public void ValidateStructure_CellOutsideBoard_ReportsOutOfBoundsCell()
        {
            LevelConfig config = TestConfig.WithCell(
                BaseConfig(),
                new CellSpec(new BoardPosition(3, 0), ElementType.NormalPiece, PieceColor.Green, RocketDirection.None));

            LevelConfigValidationResult result = LevelConfigValidator.ValidateStructure(config);
            AssertHasCode(result, LevelConfigErrorCode.OutOfBoundsCell);
            Assert.That(
                FindError(result, LevelConfigErrorCode.OutOfBoundsCell).Position,
                Is.EqualTo(new BoardPosition(3, 0)));
        }

        [Test]
        public void ValidateStructure_TwoSpecsAtOnePosition_ReportsDuplicateCell()
        {
            LevelConfig config = TestConfig.WithCell(
                BaseConfig(),
                new CellSpec(new BoardPosition(0, 0), ElementType.NormalPiece, PieceColor.Red, RocketDirection.None));

            LevelConfigValidationResult result = LevelConfigValidator.ValidateStructure(config);
            AssertHasCode(result, LevelConfigErrorCode.DuplicateCell);
            Assert.That(
                FindError(result, LevelConfigErrorCode.DuplicateCell).Position,
                Is.EqualTo(new BoardPosition(0, 0)));
        }

        [Test]
        public void ValidateStructure_ExplicitEmptySpec_ReportsEmptyCellSpec()
        {
            LevelConfig config = ConfigWithSingleCell(
                new CellSpec(new BoardPosition(1, 1), ElementType.Empty, PieceColor.None, RocketDirection.None));

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.EmptyCellSpec);
        }

        [Test]
        public void ValidateStructure_NormalPieceWithoutPlayableColor_ReportsInvalidColor()
        {
            LevelConfig config = ConfigWithSingleCell(
                new CellSpec(new BoardPosition(1, 1), ElementType.NormalPiece, PieceColor.None, RocketDirection.None));

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.InvalidColor);
        }

        [Test]
        public void ValidateStructure_UnknownElementType_ReportsInvalidElementType()
        {
            LevelConfig config = ConfigWithSingleCell(
                new CellSpec(new BoardPosition(1, 1), (ElementType)99, PieceColor.None, RocketDirection.None));

            LevelConfigValidationResult result = LevelConfigValidator.ValidateStructure(config);
            AssertHasCode(result, LevelConfigErrorCode.InvalidElementType);
            Assert.That(
                FindError(result, LevelConfigErrorCode.InvalidElementType).Position,
                Is.EqualTo(new BoardPosition(1, 1)));
        }

        [Test]
        public void ValidateStructure_RocketWithoutDirection_ReportsInvalidRocketDirection()
        {
            LevelConfig config = ConfigWithSingleCell(
                new CellSpec(new BoardPosition(1, 1), ElementType.Rocket, PieceColor.None, RocketDirection.None));

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.InvalidRocketDirection);
        }

        [Test]
        public void ValidateStructure_NoBackpacks_ReportsNoBackpacks()
        {
            LevelConfig config = TestConfig.FromRows(
                "GRB",
                "BRG",
                "GBG");

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.NoBackpacks);
        }

        [Test]
        public void ValidateStructure_RefillColorNone_ReportsInvalidRefillColor()
        {
            LevelConfig source = BaseConfig();
            var config = new LevelConfig(
                source.LevelId,
                source.Width,
                source.Height,
                source.InitialCells,
                new[] { PieceColor.None },
                source.RandomSeed,
                source.ConfigVersion);

            AssertHasCode(LevelConfigValidator.ValidateStructure(config), LevelConfigErrorCode.InvalidRefillColor);
        }

        private static LevelConfig ConfigWithSingleCell(CellSpec cell)
        {
            return new LevelConfig(
                "test",
                3,
                3,
                new[]
                {
                    new CellSpec(new BoardPosition(0, 0), ElementType.Backpack, PieceColor.None, RocketDirection.None),
                    cell,
                },
                new PieceColor[0],
                1,
                LevelConfigValidator.SupportedConfigVersion);
        }

        private static LevelConfigError FindError(LevelConfigValidationResult result, LevelConfigErrorCode code)
        {
            return result.Errors.Single(error => error.Code == code);
        }

        private static void AssertHasCode(LevelConfigValidationResult result, LevelConfigErrorCode code)
        {
            Assert.That(
                result.Errors.Any(error => error.Code == code),
                Is.True,
                "Expected " + code + ", got: " + Describe(result));
        }

        private static string Describe(LevelConfigValidationResult result)
        {
            if (result.IsValid)
            {
                return "no errors";
            }

            return string.Join("; ", result.Errors.Select(error => error.ToString()));
        }
    }
}
