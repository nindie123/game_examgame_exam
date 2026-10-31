using System.Collections.Generic;
using BagElimination.Rules.Board;
namespace BagElimination.Rules.Simulation
{
    public static class GravitySolver
    {
        // One atomic movement per step preserves the real path around obstacles for playback.
        public static IEnumerable<SimulationStep> Settle(BoardState board, SimulationContext context)
        {
            int w = board.Width, h = board.Height;
            var a = BoardData.Copy(board);
            while (true)
            {
                int from = -1, to = -1;
                for (int r = h - 2; r >= 0 && from < 0; r--)
                    for (int c = 0; c < w; c++)
                        if (a[r*w+c].IsMovable && !a[(r+1)*w+c].IsOccupied)
                        { from = r*w+c; to = from+w; break; }
                if (from < 0)
                    for (int r = h-1; r > 0 && from < 0; r--)
                        for (int c = 0; c < w && from < 0; c++)
                        {
                            to = r*w+c;
                            if (a[to].IsOccupied) continue;
                            for (int side = -1; side <= 1; side += 2)
                            {
                                int sc = c+side;
                                if (sc < 0 || sc >= w) continue;
                                int source = (r-1)*w+sc;
                                if (!a[source].IsMovable || !a[source+w].IsOccupied) continue;
                                if (a[source+w].Type == ElementType.Backpack && a[to-w].Type == ElementType.Backpack) continue;
                                from = source; break;
                            }
                        }
                if (from >= 0)
                {
                    a[to] = a[from]; a[from] = BoardCell.Empty;
                    yield return new SimulationStep(new BoardState(w,h,a), "Fall");
                    continue;
                }
                bool spawned = false;
                for (int c = 0; c < w; c++)
                    if (!a[c].IsOccupied) { a[c] = context.Spawn(); spawned = true; }
                if (!spawned) yield break;
                yield return new SimulationStep(new BoardState(w,h,a), "Refill");
            }
        }
    }
}
