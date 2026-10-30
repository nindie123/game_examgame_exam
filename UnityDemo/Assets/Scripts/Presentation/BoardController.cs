using System.Collections;
using BagElimination.Rules.Board;
using BagElimination.Rules.Legality;
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

            // Evaluate the requested move; the evaluator already contains adjacency / blocked / match
            // rules, so the controller does not duplicate any of that logic.
            SwapOutcome outcome = SwapEvaluator.Evaluate(
                _board,
                new BoardPosition(firstRow, firstCol),
                new BoardPosition(secondRow, secondCol)).Outcome;

            bool valid = outcome.IsLegal();
            StartCoroutine(PlaySwapFeedback(firstRow, firstCol, secondRow, secondCol, valid));
        }

        private IEnumerator PlaySwapFeedback(int firstRow, int firstCol, int secondRow, int secondCol, bool valid)
        {
            // Swap the two pieces on screen (authoritative data untouched)...
            yield return StartCoroutine(_view.AnimateSwap(firstRow, firstCol, secondRow, secondCol, BoardView.SwapDuration));
            yield return null; // Hold one frame so the swapped result is visible.

            if (valid)
            {
                // Temp preview for T02: show why the swap would be accepted, then roll back. The real
                // elimination branch will replace exactly this block in a later round.
                _view.ShowHint();
                yield return new WaitForSeconds(BoardView.HintHoldSeconds);
                _view.HideHint();
            }

            // ...then swap the same two pieces back, returning the screen to the pre-swap layout.
            yield return StartCoroutine(_view.AnimateSwap(firstRow, firstCol, secondRow, secondCol, BoardView.SwapDuration));

            // Visual layout and logical selection are consistent again; accept input once more.
            _state = State.Ready;
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