using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BagElimination.Rules.Board;
namespace BagElimination.Rules.Simulation
{
    // Ordered atomic From/To changes form the full movement path.
    public readonly struct ObjectChange
    {
        public int ObjectId { get; }
        public int From { get; }
        public int To { get; }
        public ObjectChange(int id, int from, int to) { ObjectId=id; From=from; To=to; }
    }
    public sealed class SimulationStep
    {
        public BoardState Board { get; }
        public StepKind Type { get; }
        public string Kind => Type.ToString();
        public int Origin { get; }
        public IReadOnlyList<int> HitCells { get; }
        public int EffectObjectId { get; private set; }
        public int Id { get; private set; }
        public int TargetDelta { get; private set; }
        public IReadOnlyList<ObjectChange> Changes { get; private set; } = Array.AsReadOnly(new ObjectChange[0]);
        public string Digest { get; private set; }
        public string Error { get; }
        public SettlementState State => Type == StepKind.Error ? SettlementState.Error :
            Type == StepKind.Swap ? SettlementState.Swapping :
            Type == StepKind.Fall || Type == StepKind.Refill ? SettlementState.Falling :
            Type == StepKind.Shuffle ? SettlementState.Recovering :
            Type == StepKind.Checking ? SettlementState.Checking : Type == StepKind.Ready ? SettlementState.Ready :
            Type == StepKind.Victory ? SettlementState.Victory : SettlementState.Resolving;
        public SimulationStep(BoardState board, string kind, int origin=-1, string error=null, IEnumerable<int> hits=null)
        { Type=(StepKind)Enum.Parse(typeof(StepKind),kind); if(Type!=StepKind.Error)BoardData.Validate(board); Board=board; Origin=origin; Error=error; HitCells=new List<int>(hits ?? new int[0]).AsReadOnly(); }
        internal void Complete(BoardState before, SimulationContext ctx)
        {
            if(Origin>=0)EffectObjectId=before.GetCell(Origin/before.Width,Origin%before.Width).ObjectId; Id=++ctx.StepId; TargetDelta=Board.CountBackpacks()-before.CountBackpacks();
            var old=new SortedDictionary<int,int>(); var next=new SortedDictionary<int,int>();
            foreach(var p in before.EnumeratePositions()) { var c=before.GetCell(p); if(c.IsOccupied)old[c.ObjectId]=p.Row*before.Width+p.Column; }
            foreach(var p in Board.EnumeratePositions()) { var c=Board.GetCell(p); if(c.IsOccupied)next[c.ObjectId]=p.Row*Board.Width+p.Column; }
            var changes=new List<ObjectChange>();
            foreach(var pair in old) if(!next.TryGetValue(pair.Key,out int to) || to!=pair.Value)
                changes.Add(new ObjectChange(pair.Key,pair.Value,next.ContainsKey(pair.Key)?to:-1));
            foreach(var pair in next) if(!old.ContainsKey(pair.Key))changes.Add(new ObjectChange(pair.Key,-1,pair.Value));
            Changes=changes.AsReadOnly();
            Digest=Snapshot(Board,ctx)+"|"+Id+"|"+Kind+"|"+Origin+"|"+TargetDelta+"|"+EffectObjectId+"|"+string.Join(",",HitCells)+"|"+Error;
        }
        public static string Snapshot(BoardState board, SimulationContext ctx)
        {
            var text=new StringBuilder().Append(board.Width).Append('x').Append(board.Height);
            foreach(var p in board.EnumeratePositions()) {var c=board.GetCell(p);text.Append('|').Append(c.ObjectId).Append(',').Append((int)c.Type).Append(',').Append((int)c.Color).Append(',').Append((int)c.Direction);}
            text.Append('|').Append(ctx.NextId).Append('|').Append(ctx.Cursor).Append('|').Append(ctx.RandomState).Append('|').Append(ctx.ShuffleState);
            using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
        }
    }
}

