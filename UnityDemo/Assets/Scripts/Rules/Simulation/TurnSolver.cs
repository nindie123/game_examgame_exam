using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Legality;
using BagElimination.Rules.Matching;
namespace BagElimination.Rules.Simulation
{
    public static class PropEffects
    {
        public static bool IsProp(BoardCell cell) => cell.IsMovable && !cell.IsMatchable;
        public static IEnumerable<SimulationStep> Execute(BoardState board, int first, int second)
        {
            int w=board.Width,h=board.Height; var a=BoardData.Copy(board);
            var queue=new Queue<int>(); var seen=new HashSet<int>();
            Action<int> enqueue=k=> { if(IsProp(a[k]) && seen.Add(a[k].ObjectId)) queue.Enqueue(k); };
            enqueue(Math.Min(first,second)); enqueue(Math.Max(first,second));
            while(queue.Count>0)
            {
                int origin=queue.Dequeue(); var prop=a[origin]; var hit=new SortedSet<int>();
                int r=origin/w,c=origin%w;
                if(prop.Type==ElementType.Rocket)
                { for(int k=0;k<a.Length;k++) if(prop.Direction==RocketDirection.Horizontal?k/w==r:k%w==c) hit.Add(k); }
                else if(prop.Type==ElementType.Bomb)
                { for(int rr=Math.Max(0,r-2);rr<=Math.Min(h-1,r+2);rr++) for(int cc=Math.Max(0,c-2);cc<=Math.Min(w-1,c+2);cc++) hit.Add(rr*w+cc); }
                else
                { hit.Add(origin); if(r>0)hit.Add(origin-w); if(r+1<h)hit.Add(origin+w); if(c>0)hit.Add(origin-1); if(c+1<w)hit.Add(origin+1); }
                a[origin]=BoardCell.Empty;
                foreach(int k in hit) { if(IsProp(a[k])) enqueue(k); else a[k]=BoardCell.Empty; }
                if(prop.Type==ElementType.Propeller)
                    for(int rr=0;rr<h;rr++) { bool found=false; for(int cc=w-1;cc>=0;cc--)
                        if(a[rr*w+cc].Type==ElementType.Backpack) { a[rr*w+cc]=BoardCell.Empty; found=true; break; } if(found)break; }
                yield return new SimulationStep(new BoardState(w,h,a),prop.Type.ToString(),origin);
            }
        }
    }
    public static class TurnSolver
    {
        public static IEnumerable<SimulationStep> Resolve(BoardState board, BoardPosition first, BoardPosition second, SimulationContext ctx)
        {
            var outcome=SwapEvaluator.Evaluate(board,first,second).Outcome;
            if(!outcome.IsLegal()) yield break;
            board=board.WithSwap(first,second); yield return new SimulationStep(board,"Swap");
            int source=first.Row*board.Width+first.Column, target=second.Row*board.Width+second.Column;
            if(outcome==SwapOutcome.PropTrigger)
                foreach(var step in PropEffects.Execute(board,source,target)) { board=step.Board; yield return step; }
            else
            { board=MatchGroups.Clear(board,ctx,target,source); yield return new SimulationStep(board,"Match"); }
            for(int round=0;round<100;round++)
            {
                if(board.CountBackpacks()==0) { yield return new SimulationStep(board,"Victory"); yield break; }
                foreach(var step in GravitySolver.Settle(board,ctx)) { board=step.Board; yield return step; }
                var cleared=MatchGroups.Clear(board,ctx);
                if(cleared==null)
                {
                    if(!SwapEvaluator.HasLegalSwap(board))
                    {
                        board=Shuffle(board,ctx);
                        if(board==null) throw new InvalidOperationException("No legal shuffle after 100 attempts. Restart required.");
                        yield return new SimulationStep(board,"Shuffle");
                    }
                    yield return new SimulationStep(board,"Ready"); yield break;
                }
                board=cleared; yield return new SimulationStep(board,"Match");
            }
            throw new InvalidOperationException("Cascade limit 100 exceeded. Restart required.");
        }
        public static BoardState Shuffle(BoardState board,SimulationContext ctx)
        {
            var a=BoardData.Copy(board); var positions=new List<int>();
            for(int i=0;i<a.Length;i++) if(a[i].IsMatchable) positions.Add(i);
            for(int attempt=0;attempt<100;attempt++)
            {
                for(int i=positions.Count-1;i>0;i--)
                { int j=SimulationContext.Next(ref ctx.ShuffleState,i+1); var t=a[positions[i]]; a[positions[i]]=a[positions[j]]; a[positions[j]]=t; }
                var candidate=new BoardState(board.Width,board.Height,a);
                if(!MatchScanner.HasAnyMatch(candidate) && SwapEvaluator.HasLegalSwap(candidate)) return candidate;
            }
            return null;
        }
    }
}
