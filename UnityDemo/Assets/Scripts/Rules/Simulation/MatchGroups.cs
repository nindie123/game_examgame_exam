using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;
namespace BagElimination.Rules.Simulation
{
    public static class MatchGroups
    {
        private sealed class Shape
        {
            public List<int> Cells = new List<int>();
            public int Rank; public RocketDirection Direction; public int Anchor;
        }
        public static BoardState Clear(BoardState board, SimulationContext ctx, int destination = -1, int source = -1)
        {
            int w = board.Width, h = board.Height;
            var a = BoardData.Copy(board); var shapes = new List<Shape>();
            for (int r=0;r<h;r++) for(int c=0;c<w;c++)
            {
                int i=r*w+c; if(!a[i].IsMatchable) continue;
                for(int axis=0;axis<2;axis++)
                {
                    int dr=axis, dc=1-axis;
                    if(r-dr>=0 && c-dc>=0 && Same(a[i],a[(r-dr)*w+c-dc])) continue;
                    var s=new Shape { Anchor=i, Direction=axis==0?RocketDirection.Horizontal:RocketDirection.Vertical };
                    for(int rr=r,cc=c;rr<h && cc<w && Same(a[i],a[rr*w+cc]);rr+=dr,cc+=dc) s.Cells.Add(rr*w+cc);
                    if(s.Cells.Count>=3) { s.Rank=s.Cells.Count>=4?2:0; shapes.Add(s); }
                }
                if(r+1<h && c+1<w && Same(a[i],a[i+1]) && Same(a[i],a[i+w]) && Same(a[i],a[i+w+1]))
                    shapes.Add(new Shape { Anchor=i, Rank=1, Cells=new List<int>{i,i+1,i+w,i+w+1} });
            }
            // Crossing line candidates carry their actual intersecting structure.
            int initial=shapes.Count;
            for(int i=0;i<initial;i++) for(int j=i+1;j<initial;j++)
            {
                var x=shapes[i]; var y=shapes[j];
                if(x.Direction==RocketDirection.None || y.Direction==RocketDirection.None || x.Direction==y.Direction) continue;
                foreach(int cell in x.Cells) if(y.Cells.Contains(cell))
                { var s=new Shape { Rank=3, Anchor=cell }; s.Cells.AddRange(x.Cells); foreach(int k in y.Cells) if(!s.Cells.Contains(k)) s.Cells.Add(k); shapes.Add(s); break; }
            }
            if(shapes.Count==0) return null;
            var consumed=new bool[shapes.Count]; var clear=new HashSet<int>(); var creations=new List<KeyValuePair<int,BoardCell>>();
            for(int seed=0;seed<shapes.Count;seed++)
            {
                if(consumed[seed]) continue;
                var members=new HashSet<int>(shapes[seed].Cells); var group=new List<Shape>{shapes[seed]}; consumed[seed]=true;
                bool changed=true;
                while(changed)
                { changed=false; for(int j=0;j<shapes.Count;j++) if(!consumed[j] && shapes[j].Cells.Exists(k=>members.Contains(k)))
                    { consumed[j]=true; changed=true; group.Add(shapes[j]); foreach(int k in shapes[j].Cells) members.Add(k); } }
                foreach(int k in members)
                {
                    clear.Add(k); int r=k/w,c=k%w;
                    if(r>0 && a[k-w].Type==ElementType.Backpack) clear.Add(k-w);
                    if(r+1<h && a[k+w].Type==ElementType.Backpack) clear.Add(k+w);
                    if(c>0 && a[k-1].Type==ElementType.Backpack) clear.Add(k-1);
                    if(c+1<w && a[k+1].Type==ElementType.Backpack) clear.Add(k+1);
                }
                group.Sort((x,y)=> { int rank=y.Rank.CompareTo(x.Rank); if(rank!=0)return rank;
                    int xp=x.Cells.Contains(destination)?0:x.Cells.Contains(source)?1:2;
                    int yp=y.Cells.Contains(destination)?0:y.Cells.Contains(source)?1:2;
                    return xp!=yp?xp.CompareTo(yp):x.Anchor.CompareTo(y.Anchor); });
                var best=group[0]; if(best.Rank==0) continue;
                int at=best.Cells.Contains(destination)?destination:best.Cells.Contains(source)?source:-1;
                if(at<0) foreach(int k in best.Cells) if(at<0 || k/w>at/w || (k/w==at/w && k%w<at%w)) at=k;
                int id=ctx.NextId++;
                var prop=best.Rank==3?BoardCell.CreateBomb(id):best.Rank==2?BoardCell.CreateRocket(id,best.Direction):BoardCell.CreatePropeller(id);
                creations.Add(new KeyValuePair<int,BoardCell>(at,prop));
            }
            foreach(int k in clear) a[k]=BoardCell.Empty;
            foreach(var pair in creations) a[pair.Key]=pair.Value;
            return new BoardState(w,h,a);
        }
        private static bool Same(BoardCell x,BoardCell y) => x.IsMatchable && y.IsMatchable && x.Color==y.Color;
    }
}
