using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Legality;
namespace BagElimination.Rules.Config
{
    public static class LevelCatalog
    {
        public static LevelConfig Second()
        {
            // Same mechanics, different obstacle orientation, seed, and bomb range.
            var rows=new[]{"BBYRYGYBR","GRYRGYRGB","BYRBRGRRY","BRRGBRBRY","###GRR###","#########","#########","#########"};
            return new LevelConfig("second-9x8",9,8,AsciiLevelParser.Parse(rows),new PieceColor[0],20261101,1,
                new Simulation.RuleOptions(bombRadius:1));
        }
        public static IReadOnlyList<SwapMove> RecommendedRoute { get; } = System.Array.AsReadOnly(new[]
        {
            Move(2,4,2,5), Move(1,4,2,4), Move(2,5,3,5),
            Move(2,6,3,6), Move(6,3,7,3), Move(5,3,5,4)
        });
        private static SwapMove Move(int r,int c,int rr,int cc)=>new SwapMove(new BoardPosition(r,c),new BoardPosition(rr,cc),SwapOutcome.NormalMatch);
    }
}
