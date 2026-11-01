using System;
namespace BagElimination.Rules.Simulation
{
    public sealed class RuleOptions
    {
        public const string Version = "2";
        public int BombRadius { get; }
        public int MaxCascades { get; }
        public int MaxShuffleAttempts { get; }
        public RuleOptions(int bombRadius = 2, int maxCascades = 100, int maxShuffleAttempts = 100)
        {
            if (bombRadius < 0 || maxCascades < 1 || maxShuffleAttempts < 1)
                throw new ArgumentOutOfRangeException("Rule limits must be positive; radius may be zero.");
            BombRadius = bombRadius; MaxCascades = maxCascades; MaxShuffleAttempts = maxShuffleAttempts;
        }
    }
    public enum SettlementState { Ready, Swapping, Resolving, Falling, Checking, Recovering, Victory, Error }
    public enum StepKind { Swap, Match, Fall, Refill, Rocket, Bomb, Propeller, Shuffle, Checking, Ready, Victory, Error }
}

