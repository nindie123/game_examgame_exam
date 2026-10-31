using System.Linq;
using NUnit.Framework;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using BagElimination.Rules.Simulation;
namespace BagElimination.Tests
{
 public class SimulationTests
 {
  private static BoardState Board(int w,params BoardCell[] cells)=>new BoardState(w,cells.Length/w,cells);
  private static SimulationContext Context(BoardState b)=>new SimulationContext(b,123,new PieceColor[0]);
  private static BoardCell R(int id)=>BoardCell.CreateNormalPiece(id,PieceColor.Red);
  [Test] public void FourCreatesHorizontalRocketAndClearsAdjacentBackpack()
  { var b=Board(4,R(1),R(2),R(3),R(4),BoardCell.CreateBackpack(5),BoardCell.Empty,BoardCell.Empty,BoardCell.Empty); var end=MatchGroups.Clear(b,Context(b),2,1);Assert.AreEqual(ElementType.Rocket,end.GetCell(0,2).Type);Assert.AreEqual(RocketDirection.Horizontal,end.GetCell(0,2).Direction);Assert.AreEqual(0,end.CountBackpacks());Assert.AreEqual(1,end.CountObjects()); }
  [Test] public void SquareCreatesPropeller()
  { var b=Board(2,R(1),R(2),R(3),R(4));var end=MatchGroups.Clear(b,Context(b));Assert.AreEqual(ElementType.Propeller,end.GetCell(1,0).Type);Assert.AreEqual(1,end.CountObjects()); }
  [Test] public void CrossingCreatesOneBomb()
  { var b=Board(3,BoardCell.Empty,R(1),BoardCell.Empty,R(2),R(3),R(4),BoardCell.Empty,R(5),BoardCell.Empty);var end=MatchGroups.Clear(b,Context(b));Assert.AreEqual(1,end.CountObjects());Assert.AreEqual(ElementType.Bomb,end.GetCell(2,1).Type); }
  [Test] public void SealedAreaIsNotFilled()
  {var b=Board(2,BoardCell.CreateBackpack(1),BoardCell.CreateBackpack(2),BoardCell.Empty,BoardCell.Empty);Assert.AreEqual(0,GravitySolver.Settle(b,Context(b)).Count());}
  [Test] public void DiagonalGapBetweenTwoBackpacksIsBlocked()
  {var b=Board(2,R(1),BoardCell.CreateBackpack(2),BoardCell.CreateBackpack(3),BoardCell.Empty);Assert.AreEqual(0,GravitySolver.Settle(b,Context(b)).Count());}
  [Test] public void DiagonalContentMovesWithoutDuplicatingId()
  {var b=Board(2,R(1),BoardCell.Empty,BoardCell.CreateBackpack(2),BoardCell.Empty);var first=GravitySolver.Settle(b,Context(b)).First().Board;Assert.AreEqual(1,first.GetCell(1,1).ObjectId);Assert.IsFalse(first.GetCell(0,0).IsOccupied);}
  [Test] public void ChainedPropsExecuteOnceEach()
  {var b=Board(3,BoardCell.CreateRocket(1,RocketDirection.Horizontal),BoardCell.CreateBomb(2),BoardCell.CreateBackpack(3));var steps=PropEffects.Execute(b,0,1).ToList();Assert.AreEqual(2,steps.Count);Assert.AreEqual(0,steps.Last().Board.CountObjects());}
  [Test] public void TwoPropellersChooseDistinctTargets()
  {var b=Board(5,BoardCell.CreatePropeller(1),BoardCell.CreatePropeller(2),BoardCell.Empty,BoardCell.Empty,BoardCell.Empty,BoardCell.Empty,BoardCell.Empty,BoardCell.Empty,BoardCell.CreateBackpack(3),BoardCell.CreateBackpack(4));var steps=PropEffects.Execute(b,0,1).ToList();Assert.AreEqual(1,steps[0].Board.CountBackpacks());Assert.AreEqual(0,steps[1].Board.CountBackpacks());}
  [Test] public void DemoRouteWinsDeterministically()
  {var config=DemoLevelConfig.Create();var b=BoardFactory.Create(config);var ctx=Context(b);ctx.RandomState=(uint)config.RandomSeed|1;ctx.ShuffleState=ctx.RandomState^0x9e3779b9u;int[,] moves={{1,5,1,6},{6,5,6,6},{7,1,7,2}};for(int i=0;i<3;i++)foreach(var s in TurnSolver.Resolve(b,new BoardPosition(moves[i,0],moves[i,1]),new BoardPosition(moves[i,2],moves[i,3]),ctx)){BoardData.Validate(s.Board);b=s.Board;}Assert.AreEqual(0,b.CountBackpacks());}
 }
}
