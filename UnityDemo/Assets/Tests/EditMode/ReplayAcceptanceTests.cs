using System;
using System.Linq;
using NUnit.Framework;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using BagElimination.Rules.Legality;
using BagElimination.Rules.Simulation;
namespace BagElimination.Tests
{
 public class ReplayAcceptanceTests
 {
  [Test] public void A12_RecommendedRouteCoversEveryRequiredMechanicAndWins()
  {
   int generated=0,used=0,matches=0;bool ordinary=false,cascade=false;BoardState end=null;
   ReplayLog.Run(DemoLevelConfig.Create(),LevelCatalog.RecommendedRoute,step=>{
    if(step.Kind=="Swap"){matches=0;}
    if(step.Kind=="Match"){
     matches++;cascade|=matches>=2;bool creation=false;
     foreach(var change in step.Changes)if(change.From<0){
      var type=step.Board.GetCell(change.To/step.Board.Width,change.To%step.Board.Width).Type;
      int bit=type==ElementType.Rocket?1:type==ElementType.Bomb?2:type==ElementType.Propeller?4:0;
      generated|=bit;creation|=bit!=0;
     }ordinary|=!creation;
    }
    used|=step.Kind=="Rocket"?1:step.Kind=="Bomb"?2:step.Kind=="Propeller"?4:0;end=step.Board;
   });
   Assert.AreEqual(7,generated);Assert.AreEqual(7,used);Assert.IsTrue(ordinary);Assert.IsTrue(cascade);Assert.AreEqual(0,end.CountBackpacks());
  }
  [Test] public void A12_RepeatReplayComparesEveryStepAndRandomState()
  {
   var config=DemoLevelConfig.Create();var first=ReplayLog.Run(config,LevelCatalog.RecommendedRoute);
   var second=ReplayLog.Run(config,LevelCatalog.RecommendedRoute);
   Assert.IsNull(ReplayLog.FirstDifference(first,second));Assert.Greater(first.Entries.Count,100);
  }
  [Test] public void A12_ConsumingSnapshotsDoesNotChangeReplay()
  {
   var config=DemoLevelConfig.Create();var fast=ReplayLog.Run(config,LevelCatalog.RecommendedRoute);
   var observed=ReplayLog.Run(config,LevelCatalog.RecommendedRoute,s=>{
    // Consumer reads each path and board; actual Unity animation is separately checked in the player.
    foreach(var c in s.Changes)Assert.Greater(c.ObjectId,0);
    BoardData.Validate(s.Board);
   });
   Assert.IsNull(ReplayLog.FirstDifference(fast,observed));
  }
  [Test] public void A12_ExportedLogReplaysAndDetectsTampering()
  {
   var config=DemoLevelConfig.Create();var log=ReplayLog.Run(config,LevelCatalog.RecommendedRoute);
   Assert.IsNull(ReplayLog.Verify(config,log.Export()));
   StringAssert.StartsWith("Entry 1:",ReplayLog.Verify(config,log.Export().Replace("STEP|","STEP|tampered")));
  }
  [Test] public void A12_ReplayDetectsFirstChangedInput()
  {
   var config=DemoLevelConfig.Create();var expected=ReplayLog.Run(config,LevelCatalog.RecommendedRoute);
   var different=new ReplayLog(config);different.Action(new BoardPosition(0,0),new BoardPosition(0,1));
   StringAssert.StartsWith("Entry 0:",ReplayLog.FirstDifference(expected,different));
  }
  [Test] public void A13_SecondConfigLoadsWithoutChangingInputOrMatchModules()
  {
   var config=LevelCatalog.Second();Assert.IsTrue(LevelConfigValidator.Validate(config).IsValid);
   var b=BoardFactory.Create(config);Assert.AreEqual(33,b.CountBackpacks());Assert.AreEqual(1,config.Rules.BombRadius);
   Assert.AreNotEqual(DemoLevelConfig.Create().RandomSeed,config.RandomSeed);
  }
  [Test] public void A13_SecondConfigCanCompleteWithDeterministicLookahead()
  {
   var config=LevelCatalog.Second();var b=BoardFactory.Create(config);var ctx=new SimulationContext(b,config.RandomSeed,config.RefillSequence,config.Rules);
   for(int turn=0;turn<50&&b.CountBackpacks()>0;turn++){
    BoardState best=null;SimulationContext selected=null;int bestScore=int.MinValue;
    foreach(var move in SwapEvaluator.FindLegalSwaps(b)){
     var copy=new SimulationContext(b,config.RandomSeed,config.RefillSequence,config.Rules){NextId=ctx.NextId,Cursor=ctx.Cursor,RandomState=ctx.RandomState,ShuffleState=ctx.ShuffleState};
     var steps=TurnSolver.Resolve(b,move.From,move.To,copy).ToArray();if(steps.Last().Type==StepKind.Error)continue;
     var end=steps.Last().Board;int score=(b.CountBackpacks()-end.CountBackpacks())*100+steps.Count(s=>s.Kind=="Match");
     foreach(var p in end.EnumeratePositions())if(PropEffects.IsProp(end.GetCell(p)))score+=15;
     if(score>bestScore){bestScore=score;best=end;selected=copy;}
    }
    Assert.NotNull(best,"No progressing candidate");b=best;ctx=selected;
   }Assert.AreEqual(0,b.CountBackpacks());
  }
 }
}

