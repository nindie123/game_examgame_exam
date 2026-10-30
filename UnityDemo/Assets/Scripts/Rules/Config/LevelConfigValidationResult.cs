using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BagElimination.Rules.Config
{
    public sealed class LevelConfigValidationResult
    {
        private readonly ReadOnlyCollection<LevelConfigError> _errors;

        public LevelConfigValidationResult(IReadOnlyList<LevelConfigError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            var copy = new LevelConfigError[errors.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = errors[i];
            }

            _errors = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<LevelConfigError> Errors => _errors;
        public bool IsValid => _errors.Count == 0;
    }
}
