using System.Collections;
using BagElimination.Rules.Board;
using BagElimination.Rules.Resolution;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BagElimination.Presentation
{
    /// <summary>
    /// Minimal control layer for the T02 click/swap flow. It owns the Ready/Busy state, the current
    /// selection, and the swap request, and drives <see cref="BoardView"/>'s feedback. It never writes
    /// to the authoritative <see cref="BoardState"/>: the swap is only evaluated, and the board revert
    /// below is purely visual, so the rule data is unchanged at the end of any feedback.
    /// </summary>
    public sealed class BoardController : MonoBehaviour
    {
        private enum State
        {
            Ready, // Accepts input.
            Busy,  // Animating; new clicks are ignored, not cached.
        }

        private BoardState _board;
        private BoardView _view;
        private State _state = State.Ready;

        private bool _hasSelection;
        private int _selectedRow;
        private int _selectedCol;

        public void Initialize(BoardState board, BoardView view)
        {
            _board = board;
            _view = view;
        }

        private void Update()
        {
            if (_state != State.Ready)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            HandleClick(ScreenToWorld(mouse.position.ReadValue()));
        }

        private void HandleClick(Vector3 worldPosition)
        {
            if (_board == null || _view == null)
            {
                return;
            }

            if (!_view.TryGetCell(worldPosition, out int row, out int col))
            {
                return; // Clicked outside the board: no selection change, no swap.
            }

            BoardCell clicked = _board.GetCell(row, col);
            if (clicked.Type != ElementType.NormalPiece)
            {
                // Only normal pieces start a swap; a backpack is never selectable or a swap target.
                return;
            }

            if (!_hasSelection)
            {
                Select(row, col);
                return;
            }

            if (_selectedRow == row && _selectedCol == col)
            {
                ClearSelection();
                return;
            }

            if (IsAdjacent(_selectedRow, _selectedCol, row, col))
            {
                RequestSwap(_selectedRow, _selectedCol, row, col);
                return;
            }

            // A non-adjacent normal piece becomes the new selection.
            Select(row, col);
        }

        private void Select(int row, int col)
        {
            _hasSelection = true;
            _selectedRow = row;
            _selectedCol = col;
            _view.ShowSelection(row, col);
        }

        private void ClearSelection()
        {
            _hasSelection = false;
            _view.ClearSelection();
        }

        private void RequestSwap(int firstRow, int firstCol, int secondRow, int secondCol)
        {
            _state = State.Busy;
            ClearSelection();

            // The resolver carries the whole decision (reject / plain three / special); the
            // controller only plays the matching feedback and never re-implements match rules.
            SwapResolveResult result = SwapResolver.Resolve(
                _board,
                new BoardPosition(firstRow, firstCol),
                new BoardPosition(secondRow, secondCol));

            switch (result.Outcome)
            {
                case SwapResolveOutcome.NormalThree:
                    StartCoroutine(PlayCommittedSwap(firstRow, firstCol, secondRow, secondCol, result));
                    break;
                case SwapResolveOutcome.SpecialUnsupported:
                    StartCoroutine(PlayPreviewSwap(firstRow, firstCol, secondRow, secondCol, BoardView.SpecialSwapHint));
                    break;
                default:
                    StartCoroutine(PlayPreviewSwap(firstRow, firstCol, secondRow, secondCol, null));
                    break;
            }
        }

        private IEnumerator PlayPreviewSwap(int firstRow, int firstCol, int secondRow, int secondCol, string hint)
        {
            // Rejected swaps animate and roll back with no hint; special (four+/cross/2x2) swaps
            // roll back with a hint. Neither mutates the authoritative board.
            yield return StartCoroutine(_view.AnimateSwap(firstRow, firstCol, secondRow, secondCol, BoardView.SwapDuration));
            yield return null;

            if (hint != null)
            {
                _view.ShowHint(hint);
                yield return new WaitForSeconds(BoardView.HintHoldSeconds);
                _view.HideHint();
            }

            yield return StartCoroutine(_view.AnimateSwap(firstRow, firstCol, secondRow, secondCol, BoardView.SwapDuration));
            _state = State.Ready;
        }

        private IEnumerator PlayCommittedSwap(int firstRow, int firstCol, int secondRow, int secondCol, SwapResolveResult result)
        {
            yield return StartCoroutine(_view.AnimateSwap(firstRow, firstCol, secondRow, secondCol, BoardView.SwapDuration));

            // Commit: adopt the cleared snapshot, re-key the piece look-up to the swapped ownership,
            // hide the cleared pieces and backpacks, and refresh the count from the committed board.
            _board = result.Board;
            _view.FinalizeSwap(firstRow, firstCol, secondRow, secondCol);
            _view.ApplyCommittedBoard(result.Board);
            _view.RemoveOccupants(result.MatchedCells);
            _view.RemoveOccupants(result.HitBackpacks);
            _view.ShowHint(BoardView.RoundCompleteHint);

            // No fall/refill this round: input stays locked (State.Busy) until the scene is re-entered.
        }

        private static Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            if (Camera.main == null)
            {
                return Vector3.zero;
            }

            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            world.z = 0f;
            return world;
        }

        private static bool IsAdjacent(int firstRow, int firstCol, int secondRow, int secondCol)
        {
            return Mathf.Abs(firstRow - secondRow) + Mathf.Abs(firstCol - secondCol) == 1;
        }
    }
}