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
        private int _session;
        private float _nextDemo;
        public void Initialize(BoardState board,BoardView view)
        { _board=board; _view=view; _view.HudClicked -= OnHudButton; _view.HudClicked += OnHudButton; var config=DemoLevelConfig.Create(); _context=new SimulationContext(board,config.RandomSeed,config.RefillSequence); }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Restart();
                return;
            }

            _view.SetHudState(_busy, _victory, _demo);
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
            // Deterministic one-turn lookahead for demonstration; never changes the live random stream.
            SwapMove best=moves[0]; int bestScore=int.MinValue;
            foreach(var move in moves)
            {
                var config=DemoLevelConfig.Create();
                var ctx=new SimulationContext(_board,config.RandomSeed,config.RefillSequence)
                { NextId=_context.NextId,Cursor=_context.Cursor,RandomState=_context.RandomState,ShuffleState=_context.ShuffleState };
                var end=_board; int score=0;
                try { foreach(var step in TurnSolver.Resolve(_board,move.From,move.To,ctx)) { end=step.Board; if(step.Kind=="Match")score++; } }
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
            var iterator=TurnSolver.Resolve(_board,a,b,_context).GetEnumerator();
            SimulationStep pending = null;
            using(iterator)
            while(session==_session)
            {
                bool next=false; string error=null;
                try { next=pending != null || iterator.MoveNext(); } catch(Exception ex) { error=ex.Message; Debug.LogException(ex); }
                if(error!=null) { _demo=false; Debug.LogWarning("[BoardController] 结算异常，已停止: " + error); yield break; }
                if(!next) break;
                var step = pending ?? iterator.Current;
                pending = null;
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
                    _board = falls[falls.Count - 1].Board;
                    yield return _view.PlayFalls(falls);
                    if (session != _session) yield break;
                    if (gravityError != null)
                    { _demo = false; Debug.LogWarning("[BoardController] 下落异常，已停止: " + gravityError); yield break; }
                    continue;
                }
                _board=step.Board;
                yield return _view.PlayStep(step);
                if(session!=_session) yield break;
                if(step.Kind=="Victory") { _victory=true; _demo=false; yield return _view.Celebrate(); yield break; }
            }
            _busy=false; _nextDemo=Time.time+0.3f;
        }
        private void Restart()
        {
            _session++; StopAllCoroutines(); _busy=false; _victory=false; _selected=false; _demo=false;
            _view.ResetBoard(); Initialize(BoardFactory.Create(DemoLevelConfig.Create()),_view); _view.Render(_board);
        }
    }
}
