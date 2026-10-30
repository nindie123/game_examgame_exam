using BagElimination.Rules.Board;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// One configuration problem. The position is optional because some errors concern the
    /// level as a whole (for example a missing backpack).
    /// </summary>
    public sealed class LevelConfigError
    {
        public LevelConfigError(LevelConfigErrorCode code, string message, BoardPosition? position = null)
        {
            Code = code;
            Message = message;
            Position = position;
        }

        public LevelConfigErrorCode Code { get; }
        public string Message { get; }
        public BoardPosition? Position { get; }

        public override string ToString()
        {
            return Position.HasValue ? Code + " " + Position.Value + ": " + Message : Code + ": " + Message;
        }
    }
}
