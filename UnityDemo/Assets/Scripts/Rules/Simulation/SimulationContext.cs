using System;
using System.Collections.Generic;
using BagElimination.Rules.Board;
namespace BagElimination.Rules.Simulation
{
    public sealed class SimulationContext
    {
        public int NextId;
        public int Cursor;
        public uint RandomState;
        public uint ShuffleState;
        public readonly IReadOnlyList<PieceColor> Sequence;
        public SimulationContext(BoardState board, int seed, IReadOnlyList<PieceColor> sequence)
        {
            foreach (var p in board.EnumeratePositions()) NextId = Math.Max(NextId, board.GetCell(p).ObjectId);
            NextId++;
            RandomState = (uint)seed | 1u; ShuffleState = RandomState ^ 0x9e3779b9u; Sequence = sequence;
        }
        public static int Next(ref uint state, int count)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (int)(state % (uint)count);
        }
        public BoardCell Spawn()
        {
            var color = Cursor < Sequence.Count ? Sequence[Cursor++] : (PieceColor)(1 + Next(ref RandomState, 4));
            return BoardCell.CreateNormalPiece(NextId++, color);
        }
    }
    public static class BoardData
    {
        public static BoardCell[] Copy(BoardState board)
        {
            var cells = new BoardCell[board.Width * board.Height];
            foreach (var p in board.EnumeratePositions()) cells[p.Row * board.Width + p.Column] = board.GetCell(p);
            return cells;
        }
        public static void Validate(BoardState board)
        {
            var ids = new HashSet<int>();
            foreach (var p in board.EnumeratePositions())
                if (board.GetCell(p).IsOccupied && !ids.Add(board.GetCell(p).ObjectId))
                    throw new InvalidOperationException("Duplicate object at " + p);
        }
    }
    public sealed class SimulationStep
    {
        public readonly BoardState Board;
        public readonly string Kind;
        public readonly int Origin;
        public SimulationStep(BoardState board, string kind, int origin = -1)
        { BoardData.Validate(board); Board = board; Kind = kind; Origin = origin; }
    }
}
