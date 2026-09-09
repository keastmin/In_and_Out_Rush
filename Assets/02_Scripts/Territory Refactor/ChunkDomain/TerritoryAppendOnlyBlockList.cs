using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProjectIO.Territory
{
    public sealed class TerritoryAppendOnlyBlockList<T> : IReadOnlyList<T>
    {
        public const int DefaultBlockCapacity = 256;

        private readonly List<T[]> _blocks = new();
        private readonly int _blockCapacity;
        private int _count;

        public TerritoryAppendOnlyBlockList(int blockCapacity = DefaultBlockCapacity)
        {
            if (blockCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockCapacity));

            _blockCapacity = blockCapacity;
        }

        public int Count => _count;
        public int BlockCapacity => _blockCapacity;
        public int AllocatedBlockCount => _blocks.Count;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _blocks[index / _blockCapacity][index % _blockCapacity];
            }
        }

        public void Add(T item)
        {
            int blockIndex = _count / _blockCapacity;
            if (blockIndex == _blocks.Count)
                _blocks.Add(new T[_blockCapacity]);

            _blocks[blockIndex][_count % _blockCapacity] = item;
            _count++;
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                int remaining = _count;
                for (int blockIndex = 0; blockIndex < _blocks.Count && remaining > 0; blockIndex++)
                {
                    int clearCount = Math.Min(_blockCapacity, remaining);
                    Array.Clear(_blocks[blockIndex], 0, clearCount);
                    remaining -= clearCount;
                }
            }

            _count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int index = 0; index < _count; index++)
                yield return this[index];
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
