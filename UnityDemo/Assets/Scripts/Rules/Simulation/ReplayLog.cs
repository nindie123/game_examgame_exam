using System;
using System.Collections.Generic;
using System.Text;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using BagElimination.Rules.Legality;
namespace BagElimination.Rules.Simulation
{
    public sealed class ReplayLog
    {
        public string Header { get; }
        private readonly List<string> _entries=new List<string>();
        public IReadOnlyList<string> Entries => _entries.AsReadOnly();
        public ReplayLog(LevelConfig config)
        {
            var b=BoardFactory.Create(config); var ctx=new SimulationContext(b,config.RandomSeed,config.RefillSequence,config.Rules);
            Header="Replay=1|Level="+config.LevelId+"|Config="+config.ConfigVersion+"|Rules="+RuleOptions.Version+
                "|RNG=xorshift32-v1|Seed="+config.RandomSeed+"|Radius="+config.Rules.BombRadius+
                "|Cascades="+config.Rules.MaxCascades+"|Shuffle="+config.Rules.MaxShuffleAttempts+
                "|Refill="+string.Join(",",config.RefillSequence)+"|Initial="+SimulationStep.Snapshot(b,ctx);
        }
        public void Action(BoardPosition from, BoardPosition to) => _entries.Add("ACTION|"+from.Row+","+from.Column+"|"+to.Row+","+to.Column);
        public void Record(SimulationStep step) => _entries.Add("STEP|"+step.Digest);
        public string Export() => Header+Environment.NewLine+string.Join(Environment.NewLine,_entries);
        public static string FirstDifference(ReplayLog expected, ReplayLog actual)
        {
            if(expected.Header!=actual.Header)return "Replay header differs";
            int n=Math.Min(expected._entries.Count,actual._entries.Count);
            for(int i=0;i<n;i++)if(expected._entries[i]!=actual._entries[i])
                return "Entry "+i+": expected "+expected._entries[i]+"; actual "+actual._entries[i];
            return expected._entries.Count==actual._entries.Count ? null : "Entry count differs at "+n;
        }
        public static string Verify(LevelConfig config, string exported)
        {
            var lines=exported.Replace("\r","").Split('\n');
            var expected=new ReplayLog(config);
            if(lines.Length==0 || lines[0]!=expected.Header)return "Replay header differs";
            var moves=new List<SwapMove>();
            for(int i=1;i<lines.Length;i++)
            {
                if(string.IsNullOrWhiteSpace(lines[i]))continue;
                expected._entries.Add(lines[i]);
                if(!lines[i].StartsWith("ACTION|",StringComparison.Ordinal))continue;
                var fields=lines[i].Split('|');var from=fields[1].Split(',');var to=fields[2].Split(',');
                moves.Add(new SwapMove(new BoardPosition(int.Parse(from[0]),int.Parse(from[1])),
                    new BoardPosition(int.Parse(to[0]),int.Parse(to[1])),SwapOutcome.NormalMatch));
            }
            return FirstDifference(expected,Run(config,moves));
        }
        public static ReplayLog Run(LevelConfig config, IReadOnlyList<SwapMove> moves, Action<SimulationStep> observer=null)
        {
            var log=new ReplayLog(config); var board=BoardFactory.Create(config);
            var ctx=new SimulationContext(board,config.RandomSeed,config.RefillSequence,config.Rules);
            foreach(var move in moves)
            {
                if(!SwapEvaluator.Evaluate(board,move.From,move.To).Outcome.IsLegal())throw new InvalidOperationException("Illegal replay action "+move.From+" -> "+move.To);
                log.Action(move.From,move.To);
                foreach(var step in TurnSolver.Resolve(board,move.From,move.To,ctx))
                {log.Record(step); board=step.Board; observer?.Invoke(step); if(step.Type==StepKind.Error)throw new InvalidOperationException(step.Error);}
            }
            return log;
        }
    }
    // An animation completion may acknowledge only the active session's most recently committed step.
    public sealed class SessionGate
    {
        public int Session { get; private set; }
        public int Step { get; private set; }
        public void Restart() {Session++;Step=0;}
        public void Commit(int step) {if(step<=Step)throw new InvalidOperationException("Out-of-order step "+step);Step=step;}
        public bool Accepts(int session,int step)=>session==Session && step==Step;
    }
}

