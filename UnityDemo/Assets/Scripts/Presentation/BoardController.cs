using System;
using System.Collections;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using BagElimination.Rules.Legality;
using BagElimination.Rules.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BagElimination.Presentation
{
    public sealed class BoardController : MonoBehaviour
    {
        private BoardState _board; private BoardView _view; private SimulationContext _context;
        private bool _busy, _selected, _victory, _demo; private BoardPosition _selection;
        private readonly SessionGate _gate=new SessionGate();
        private int _session => _gate.Session;
        private LevelConfig _config;
        private ReplayLog _replay; private int _routeIndex;
        private bool NoAnimation => Array.IndexOf(Environment.GetCommandLineArgs(),"-no-animation")>=0;
        public SettlementState State { get; private set; } = SettlementState.Ready;
        private float _nextDemo;
        public void Initialize(BoardState board,BoardView view,LevelConfig config = null)
        { _board=board; _view=view; _view.HudClicked -= OnHudButton; _view.HudClicked += OnHudButton; _config=config ?? DemoLevelConfig.Create(); _context=new SimulationContext(board,_config.RandomSeed,_config.RefillSequence,_config.Rules); _replay=new ReplayLog(_config); State=SettlementState.Ready; _routeIndex=_config.LevelId==DemoLevelConfig.LevelId ? 0 : -1; _demo=Array.IndexOf(Environment.GetCommandLineArgs(),"-verify-route")>=0; }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Restart();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            { _config=_config.LevelId==DemoLevelConfig.LevelId ? LevelCatalog.Second() : DemoLevelConfig.Create(); Restart(); return; }
            _view.SetHudState(_busy, _victory, _demo); _view.SetHudLabel("auto",_demo ? "Stop" : "Auto");
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector3 world = ScreenToWorld(mouse.position.ReadValue());
            if (_busy || _victory)
            {
                return;
            }

            if (_demo && Time.time >= _nextDemo)
            {
                var moves = SwapEvaluator.FindLegalSwaps(_board);
                if (moves.Count > 0)
                {
                    var move = ChooseMove(moves);
                    Request(move.From, move.To);
                }
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame || _view.IsOverUi(mouse.position.ReadValue()))
            {
                return;
            }

            if (!_view.TryGetCell(world, out int r, out int c) || !_board.GetCell(r, c).IsMovable)
            {
                return;
            }

            var p = new BoardPosition(r, c);
            if (_selected && Math.Abs(r - _selection.Row) + Math.Abs(c - _selection.Column) == 1) { Request(_selection, p); return; }
            if (_selected && r == _selection.Row && c == _selection.Column) { _selected = false; _view.ClearSelection(); return; }
            _selected = true; _selection = p; _view.ShowSelection(r, c);
        }

        private static Vector3 ScreenToWorld(Vector2 position)
        {
            if (Camera.main == null)
            {
                return Vector3.zero;
            }

            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(position.x, position.y, 0f));
            world.z = 0f;
            return world;
        }

        private void OnHudButton(string id)
        {
            if (id == "restart") { Restart(); return; }
            if (_victory || (_busy && !(id == "auto" && _demo))) return;
            if (id == "auto") { _demo = !_demo; _nextDemo = Time.time; _view.SetHudLabel("auto", _demo ? "Stop" : "Auto"); }
            else if (id == "hint") Hint();
        }

        private void Hint()
        {
            var moves = SwapEvaluator.FindLegalSwaps(_board);
            if (moves.Count == 0) return;
            var move = ChooseMove(moves);
            _view.ShowSelection(move.From.Row, move.From.Column);
        }
        private SwapMove ChooseMove(IReadOnlyList<SwapMove> moves)
        {
            if(_routeIndex>=0 && _routeIndex<LevelCatalog.RecommendedRoute.Count)
            {
                var recommended=LevelCatalog.RecommendedRoute[_routeIndex];
                foreach(var move in moves)if(move.From.Equals(recommended.From)&&move.To.Equals(recommended.To))return move;
            }
            // Deterministic one-turn lookahead for demonstration; never changes the live random stream.
            SwapMove best=moves[0]; int bestScore=int.MinValue;
            foreach(var move in moves)
            {
                var config=_config;
                var ctx=new SimulationContext(_board,config.RandomSeed,config.RefillSequence,config.Rules)
                { NextId=_context.NextId,Cursor=_context.Cursor,RandomState=_context.RandomState,ShuffleState=_context.ShuffleState };
                var end=_board; int score=0;
                try { foreach(var step in TurnSolver.Resolve(_board,move.From,move.To,ctx)) { end=step.Board; if(step.Kind=="Match")score++; if(step.Type==StepKind.Error) {score=int.MinValue/2;break;} } }
                catch(InvalidOperationException) { continue; }
                score+=(_board.CountBackpacks()-end.CountBackpacks())*100;
                foreach(var p in end.EnumeratePositions()) if(PropEffects.IsProp(end.GetCell(p)))score+=15;
                if(score>bestScore) { bestScore=score; best=move; }
            }
            return best;
        }
        private void Request(BoardPosition a,BoardPosition b)
        { _selected=false; _view.ClearSelection(); _busy=true; StartCoroutine(Play(a,b,_session)); }
        private IEnumerator Play(BoardPosition a,BoardPosition b,int session)
        {
            if(!SwapEvaluator.Evaluate(_board,a,b).Outcome.IsLegal())
            { yield return _view.AnimateSwap(a.Row,a.Column,b.Row,b.Column,0.16f); yield return _view.AnimateSwap(a.Row,a.Column,b.Row,b.Column,0.16f); _busy=false; yield break; }
            if(_routeIndex>=0 && _routeIndex<LevelCatalog.RecommendedRoute.Count)
            {
                var expected=LevelCatalog.RecommendedRoute[_routeIndex];
                _routeIndex=expected.From.Equals(a)&&expected.To.Equals(b) ? _routeIndex+1 : -1;
            }
            _replay.Action(a,b);
            var iterator=TurnSolver.Resolve(_board,a,b,_context).GetEnumerator();
            SimulationStep pending = null;
            using(iterator)
            while(session==_session)
            {
                bool next=false; string error=null;
                try { next=pending != null || iterator.MoveNext(); } catch(Exception ex) { error=ex.Message; Debug.LogException(ex); }
                if(error!=null) { EnterError(error); yield break; }
                if(!next) break;
                var step = pending ?? iterator.Current;
                pending = null;
                if(step.Type==StepKind.Error) { _replay.Record(step); EnterError(step.Error); yield break; }
                State=step.State;
                if (step.Kind == "Fall" || step.Kind == "Refill")
                {
                    // Keep every rules snapshot for the view's actual paths, but animate the
                    // whole gravity phase together instead of pausing for each cell movement.
                    var falls = new List<SimulationStep> { step };
                    string gravityError = null;
                    try
                    {
                        while (iterator.MoveNext())
                        {
                            var following = iterator.Current;
                            if (following.Kind != "Fall" && following.Kind != "Refill")
                            { pending = following; break; }
                            falls.Add(following);
                        }
                    }
                    catch (Exception ex) { gravityError = ex.Message; Debug.LogException(ex); }
                    foreach(var fall in falls) { _replay.Record(fall); _gate.Commit(fall.Id); }
                    if(NoAnimation) _view.RenderSnapshot(falls[falls.Count-1].Board); else yield return _view.PlayFalls(falls);
                    _board = falls[falls.Count - 1].Board;
                    if (!_gate.Accepts(session, falls[falls.Count-1].Id)) yield break;
                    if (gravityError != null)
                    { EnterError(gravityError); yield break; }
                    continue;
                }
                _replay.Record(step); _gate.Commit(step.Id);
                if(step.Type!=StepKind.Checking && step.Type!=StepKind.Ready && step.Type!=StepKind.Victory)
                { if(NoAnimation) _view.RenderSnapshot(step.Board); else yield return _view.PlayStep(step); }
                _board=step.Board;
                if(!_gate.Accepts(session,step.Id)) yield break;
                if(step.Kind=="Victory") { _victory=true; _demo=false; _busy=false; SaveReplay();
                    if(_routeIndex==LevelCatalog.RecommendedRoute.Count)
                    {
                        var difference=ReplayLog.FirstDifference(ReplayLog.Run(_config,LevelCatalog.RecommendedRoute),_replay);
                        Debug.Log(difference==null ? "[Replay] PASS live presentation equals headless steps." : "[Replay] FAIL "+difference);
                    }
                    yield return _view.Celebrate(); yield break; }
            }
            _busy=false; _nextDemo=Time.time+0.3f;
        }
        private void EnterError(string error)
        {
            State=SettlementState.Error; _busy=true; _demo=false;
            _view.ShowSettlementError(error); _view.SetHudState(true,false,false);
            Debug.LogError("[Settlement] "+error); SaveReplay();
        }
        private void SaveReplay()
        {
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath,"last-replay.txt"),_replay.Export()); }
            catch(Exception ex) { Debug.LogWarning("Replay export failed: "+ex.Message); }
        }
        private void OnDestroy() { if(_view!=null)_view.HudClicked-=OnHudButton; }
        private void Restart()
        {
            _gate.Restart(); StopAllCoroutines(); _busy=false; _victory=false; _selected=false; _demo=false;
            _view.ResetBoard(); Initialize(BoardFactory.Create(_config),_view,_config); _view.Render(_board);
        }
    }
}
