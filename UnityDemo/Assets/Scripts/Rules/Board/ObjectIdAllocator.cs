using System;

namespace BagElimination.Rules.Board
{
    /// <summary>
    /// Allocates the unique object ids used by the board. Zero is reserved for an empty cell,
    /// so the first object id is 1. A session keeps one allocator for its lifetime.
    /// </summary>
    public sealed class ObjectIdAllocator
    {
        private int _nextId = 1;

        public int PeekNextId => _nextId;

        public int Allocate()
        {
            if (_nextId <= 0)
            {
                throw new InvalidOperationException("The object id space is exhausted.");
            }

            return _nextId++;
        }

        public void Reset()
        {
            _nextId = 1;
        }
    }
}
