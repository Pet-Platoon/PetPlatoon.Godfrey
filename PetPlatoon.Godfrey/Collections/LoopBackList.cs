using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PetPlatoon.Godfrey.Collections
{
    public class LoopBackList<T> : IEnumerable<T>
    {
        public T this[int key] => _items[key];

        private readonly T[] _items;

        public LoopBackList(int length)
        {
            _items = new T[length];
        }

        public LoopBackList(params T[] items)
        {
            _items = items;
        }

        public int CursorPosition { get; set; }

        public bool Contains(T value)
        {
            return _items.Contains(value);
        }

        public void Add(T value)
        {
            if (_items.Length == 0)
            {
                return;
            }

            if (_items.Contains(value))
            {
                return;
            }

            _items[CursorPosition] = value;
            CursorPosition += 1;
            CursorPosition %= _items.Length;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)_items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
