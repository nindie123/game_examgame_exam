using System;
using System.Linq;
using NUnit.Framework;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using BagElimination.Rules.Legality;
using BagElimination.Rules.Matching;
using BagElimination.Rules.Simulation;
namespace BagElimination.Tests
{
 public class SettlementBoundaryTests
 {
  static BoardState B(int w,params BoardCell[] c)=>new BoardState(w,c.Length/w,c);
  static BoardCell R(int id)=>BoardCell.CreateNormalPiece(id,PieceColor.Red);
  static BoardCell G(int id)=>BoardCell.CreateNormalPiece(id,PieceColor.Green);
  static BoardCell Bag(int id)=>BoardCell.CreateBackpack(id);
  static SimulationContext C(BoardState b,RuleOptions rules=null)=>new SimulationContext(b,123,new PieceColor[0],rules);
  [Test] public void A06_LeftSourceWinsContestedEmptyCell()
  {
   var b=B(3,R(1),BoardCell.Empty,G(2),Bag(3),BoardCell.Empty,Bag(4));
   var first=GravitySolver.Settle(b,C(b)).First().Board;
   Assert.AreEqual(1,first.GetCell(1,1).ObjectId);Assert.AreEqual(2,first.GetCell(0,2).ObjectId);
  }
  [Test] public void A06_MultipleTargetsCannotDuplicateSource()
  {
   var b=B(3,BoardCell.Empty,R(1),BoardCell.Empty,BoardCell.Empty,Bag(2),BoardCell.Empty);
   foreach(var s in GravitySolver.Settle(b,C(b))) {
    Assert.AreEqual(1,s.Board.EnumeratePositions().Count(p=>s.Board.GetCell(p).ObjectId==1));
    BoardData.Validate(s.Board);
   }
  }
  [Test] public void A07_LShapeCreatesBombAtLowestLeftmostCell()
  {var b=B(3,R(1),BoardCell.Empty,BoardCell.Empty,R(2),BoardCell.Empty,BoardCell.Empty,R(3),R(4),R(5));var e=MatchGroups.Clear(b,C(b));Assert.AreEqual(1,e.CountObjects());Assert.AreEqual(ElementType.Bomb,e.GetCell(2,0).Type);}
  [Test] public void A07_TShapeCreatesBombAtDestination()
  {var b=B(3,R(1),R(2),R(3),BoardCell.Empty,R(4),BoardCell.Empty,BoardCell.Empty,R(5),BoardCell.Empty);var e=MatchGroups.Clear(b,C(b),1,4);Assert.AreEqual(1,e.CountObjects());Assert.AreEqual(ElementType.Bomb,e.GetCell(0,1).Type);}
  [Test] public void A07_RocketOutranksOverlappingSquare()
  {var b=B(4,R(1),R(2),R(3),R(4),R(5),R(6),BoardCell.Empty,BoardCell.Empty);var e=MatchGroups.Clear(b,C(b));Assert.AreEqual(1,e.CountObjects());Assert.AreEqual(ElementType.Rocket,e.GetCell(0,0).Type);}
  [Test] public void A07_AdjacentIndependentGroupsCreateTwoProps()
  {var b=B(4,R(1),R(2),R(3),R(4),G(5),G(6),G(7),G(8));var e=MatchGroups.Clear(b,C(b));Assert.AreEqual(2,e.CountObjects());Assert.AreEqual(ElementType.Rocket,e.GetCell(0,0).Type);Assert.AreEqual(ElementType.Rocket,e.GetCell(1,0).Type);}
  [Test] public void A07_SourceWinsWhenDestinationOutsideStructure()
  {var b=B(4,R(1),R(2),R(3),R(4),BoardCell.Empty,BoardCell.Empty,BoardCell.Empty,BoardCell.Empty);var e=MatchGroups.Clear(b,C(b),7,2);Assert.AreEqual(ElementType.Rocket,e.GetCell(0,2).Type);}
  [Test] public void A07_VerticalFourCreatesVerticalRocket()
  {var b=B(1,R(1),R(2),R(3),R(4));var e=MatchGroups.Clear(b,C(b));Assert.AreEqual(RocketDirection.Vertical,e.GetCell(3,0).Direction);}
  [Test] public void A08_VerticalRocketClearsWholeColumnOnly()
  {var b=B(2,BoardCell.CreateRocket(1,RocketDirection.Vertical),Bag(2),Bag(3),Bag(4),Bag(5),Bag(6));var e=PropEffects.Execute(b,0,0).Last().Board;Assert.AreEqual(3,e.CountBackpacks());for(int r=0;r<3;r++)Assert.IsFalse(e.GetCell(r,0).IsOccupied);}
  [Test] public void A08_BombClipsAtCornerAndHonorsRadius()
  {
   var cells=Enumerable.Range(1,36).Select(Bag).ToArray();cells[0]=BoardCell.CreateBomb(1);var b=B(6,cells);
   Assert.AreEqual(27,PropEffects.Execute(b,0,0).Last().Board.CountBackpacks());
   Assert.AreEqual(32,PropEffects.Execute(b,0,0,new RuleOptions(bombRadius:1)).Last().Board.CountBackpacks());
  }
  [Test] public void A08_PropellerClearsLocalBeforeChoosingTopRightRemote()
  {
   var b=B(4,Bag(1),Bag(2),Bag(3),Bag(4),Bag(5),BoardCell.CreatePropeller(6),Bag(7),Bag(8),Bag(9),Bag(10),Bag(11),Bag(12));
   var e=PropEffects.Execute(b,5,5).Last().Board;
   foreach(int i in new[]{1,4,5,6,9,3})Assert.IsFalse(e.GetCell(i/4,i%4).IsOccupied);
   Assert.AreEqual(6,e.CountBackpacks());Assert.IsTrue(e.GetCell(0,0).IsOccupied);
  }
  [Test] public void A08_MultiplyHitPropExecutesOnceInStableOrder()
  {
   var b=B(3,BoardCell.CreateBomb(1),BoardCell.CreateRocket(2,RocketDirection.Horizontal),BoardCell.CreatePropeller(3));
   var s=PropEffects.Execute(b,1,0).ToArray();
   CollectionAssert.AreEqual(new[]{"Bomb","Rocket","Propeller"},s.Select(x=>x.Kind).ToArray());
   Assert.AreEqual(0,s.Last().Board.CountObjects());
  }
  [Test] public void A09_ShufflePreservesBackpacksPropsAndPieceIdentity()
  {
   var cells=new[]{R(1),R(2),G(3),R(4),G(5),R(6),G(7),R(8),G(9),Bag(10),BoardCell.CreateBomb(11),Bag(12)};
   var b=B(3,cells);var e=TurnSolver.Shuffle(b,C(b));
   Assert.NotNull(e);Assert.AreEqual(10,e.GetCell(3,0).ObjectId);Assert.AreEqual(11,e.GetCell(3,1).ObjectId);Assert.AreEqual(12,e.GetCell(3,2).ObjectId);
   CollectionAssert.AreEquivalent(cells.Select(x=>x.ObjectId),BoardData.Copy(e).Select(x=>x.ObjectId));
   Assert.IsFalse(MatchScanner.HasAnyMatch(e));Assert.IsTrue(SwapEvaluator.HasLegalSwap(e));
  }
  [Test] public void A09_ActualDeadlockBecomesPlayableWithoutImmediateMatch()
  {
   var cells=AsciiLevelParser.Parse(new[]{"RGB","GBR","BRG","###"});
   var config=new LevelConfig("deadlock",3,4,cells,new PieceColor[0],123,1);
   var b=BoardFactory.Create(config);
   Assert.IsFalse(MatchScanner.HasAnyMatch(b));Assert.IsFalse(SwapEvaluator.HasLegalSwap(b));
   var e=TurnSolver.Shuffle(b,C(b));Assert.NotNull(e);
   Assert.IsFalse(MatchScanner.HasAnyMatch(e));Assert.IsTrue(SwapEvaluator.HasLegalSwap(e));
   for(int c=0;c<3;c++)Assert.AreEqual(b.GetCell(3,c).ObjectId,e.GetCell(3,c).ObjectId);
  }
  [Test] public void A09_ImpossibleShuffleReturnsAtConfiguredLimit()
  {var b=B(2,Bag(1),Bag(2),R(3),G(4));Assert.IsNull(TurnSolver.Shuffle(b,C(b,new RuleOptions(maxShuffleAttempts:1))));}
  [Test] public void A10_LastBackpackDoesNotSkipQueuedProp()
  {
   var b=B(4,BoardCell.CreateRocket(1,RocketDirection.Horizontal),BoardCell.CreateBomb(2),Bag(3),R(4));
   var s=TurnSolver.Resolve(b,new BoardPosition(0,0),new BoardPosition(0,1),C(b)).ToArray();
   CollectionAssert.AreEqual(new[]{"Swap","Bomb","Rocket","Victory"},s.Select(x=>x.Kind).ToArray());
   Assert.AreEqual(0,s.Last().Board.CountBackpacks());Assert.IsFalse(s.Any(x=>x.Type==StepKind.Refill));
  }
  [Test] public void A10_VictoryKeepsUnrelatedNormalPieces()
  {
   var b=B(3,BoardCell.CreateRocket(1,RocketDirection.Horizontal),R(2),Bag(3),G(4),G(5),R(6));
   var s=TurnSolver.Resolve(b,new BoardPosition(0,0),new BoardPosition(0,1),C(b)).Last();
   Assert.AreEqual(StepKind.Victory,s.Type);Assert.AreEqual(3,s.Board.CountNormalPieces());
  }
  [Test] public void A10_WinOnFinalAllowedCascadeTakesPriorityOverLimit()
  {
   var b=B(3,BoardCell.CreateRocket(1,RocketDirection.Horizontal),R(2),G(3),R(4),R(5),Bag(6));
   var ctx=new SimulationContext(b,123,new[]{PieceColor.Green,PieceColor.Green,PieceColor.Green},new RuleOptions(maxCascades:1));
   var steps=TurnSolver.Resolve(b,new BoardPosition(0,0),new BoardPosition(0,1),ctx).ToArray();
   Assert.AreEqual(StepKind.Victory,steps.Last().Type);Assert.AreEqual(0,steps.Last().Board.CountBackpacks());
  }
  [Test] public void A09_ExhaustedShuffleBecomesRestartableErrorStep()
  {
   var b=B(3,BoardCell.CreateRocket(1,RocketDirection.Horizontal),R(2),G(3),Bag(4),Bag(5),Bag(6));
   var ctx=new SimulationContext(b,123,new[]{PieceColor.Red,PieceColor.Green,PieceColor.Blue},new RuleOptions(maxShuffleAttempts:2));
   var steps=TurnSolver.Resolve(b,new BoardPosition(0,0),new BoardPosition(0,1),ctx).ToArray();
   Assert.AreEqual(StepKind.Error,steps.Last().Type);StringAssert.Contains("2 attempts",steps.Last().Error);
   Assert.AreEqual(3,steps.Last().Board.CountBackpacks());Assert.AreEqual(steps[steps.Length-2].Board,steps.Last().Board);
  }
  [Test] public void A11_CascadeLimitProducesDiagnosticError()
  {
   var config=DemoLevelConfig.Create();var b=BoardFactory.Create(config);var ctx=new SimulationContext(b,config.RandomSeed,config.RefillSequence,new RuleOptions(maxCascades:1));
   var s=TurnSolver.Resolve(b,new BoardPosition(1,5),new BoardPosition(1,6),ctx).ToArray();
   Assert.AreEqual(StepKind.Error,s.Last().Type);StringAssert.Contains("Cascade limit 1",s.Last().Error);StringAssert.Contains("Step",s.Last().Error);
  }
  [Test] public void A11_RestartRejectsOldSessionAndOldStep()
  {
   var gate=new SessionGate();gate.Commit(1);int old=gate.Session;Assert.IsTrue(gate.Accepts(old,1));
   gate.Commit(2);Assert.IsFalse(gate.Accepts(old,1));gate.Restart();gate.Commit(1);Assert.IsFalse(gate.Accepts(old,1));Assert.IsTrue(gate.Accepts(gate.Session,1));
  }
  [Test] public void A11_RestartResetsSeedCursorIdsAndBoard()
  {
   var config=DemoLevelConfig.Create();var b=BoardFactory.Create(config);var ctx=new SimulationContext(b,config.RandomSeed,config.RefillSequence);string initial=SimulationStep.Snapshot(b,ctx);
   foreach(var step in TurnSolver.Resolve(b,new BoardPosition(1,5),new BoardPosition(1,6),ctx))b=step.Board;
   b=BoardFactory.Create(config);ctx=new SimulationContext(b,config.RandomSeed,config.RefillSequence);
   Assert.AreEqual(initial,SimulationStep.Snapshot(b,ctx));Assert.AreEqual(0,ctx.StepId);
  }
  [Test] public void A11_DuplicateIdentityReportsLocation()
  {var b=B(2,R(1),G(1));var ex=Assert.Throws<InvalidOperationException>(()=>BoardData.Validate(b));StringAssert.Contains("Duplicate object at",ex.Message);}
  [Test] public void A11_InvalidSnapshotStopsAtFirstStepWithDiagnostic()
  {
   var b=B(3,BoardCell.CreateRocket(1,RocketDirection.Horizontal),R(1),Bag(2));
   var steps=TurnSolver.Resolve(b,new BoardPosition(0,0),new BoardPosition(0,1),C(b)).ToArray();
   Assert.AreEqual(1,steps.Length);Assert.AreEqual(StepKind.Error,steps[0].Type);StringAssert.Contains("Step 1",steps[0].Error);
   StringAssert.Contains("Duplicate object",steps[0].Error);
  }
  [Test] public void A11_StepDeltasAndPathsAgreeWithAuthoritativeBoards()
  {
   var config=DemoLevelConfig.Create();var before=BoardFactory.Create(config);var ctx=new SimulationContext(before,config.RandomSeed,config.RefillSequence);int id=0;
   foreach(var s in TurnSolver.Resolve(before,new BoardPosition(1,5),new BoardPosition(1,6),ctx)){
    Assert.AreEqual(++id,s.Id);Assert.AreEqual(s.Board.CountBackpacks()-before.CountBackpacks(),s.TargetDelta);
    foreach(var change in s.Changes){
     if(change.From>=0)Assert.AreEqual(change.ObjectId,before.GetCell(change.From/before.Width,change.From%before.Width).ObjectId);
     if(change.To>=0)Assert.AreEqual(change.ObjectId,s.Board.GetCell(change.To/before.Width,change.To%before.Width).ObjectId);
    }before=s.Board;
   }
  }
 }
}



