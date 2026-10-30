using System;
using System.Collections.Generic;
using System.Text;

namespace BagElimination.Rules.Config
{
    /// <summary>
    /// Thrown when an invalid config is used to build a board. All validation errors are kept
    /// so the caller can report the complete problem instead of only the first one.
    /// </summary>
    public sealed class LevelConfigException : Exception
    {
        public LevelConfigException(IReadOnlyList<LevelConfigError> errors)
            : base(BuildMessage(errors))
        {
            Errors = errors;
        }

        public IReadOnlyList<LevelConfigError> Errors { get; }

        private static string BuildMessage(IReadOnlyList<LevelConfigError> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return "The level config is invalid.";
            }

            var builder = new StringBuilder();
            builder.Append("The level config is invalid (");
            builder.Append(errors.Count);
            builder.Append(" error(s)): ");
            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                builder.Append(errors[i]);
            }

            return builder.ToString();
        }
    }
}
