using System.Linq;

namespace Godfrey.Collections
{
    public class LoopBackList<T>
    {
        private readonly T[] _items;

        public int CursorPosition { get; set; }

        public LoopBackList(int length)
        {
            _items = new T[length];
        }

        public LoopBackList(params T[] items)
        {
            _items = items;
        }

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
            CursorPosition++;
            if (CursorPosition >= _items.Length)
            {
                CursorPosition = 0;
            }
        }
    }
}
