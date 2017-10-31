using System.Linq;

namespace Godfrey.Collections
{
    public class LoopBackList<T>
    {
        private T[] items;

        public int CursorPosition { get; set; }

        public LoopBackList(int length)
        {
            items = new T[length];
        }

        public LoopBackList(params T[] items)
        {
            this.items = items;
        }

        public bool Contains(T value)
        {
            return items.Contains(value);
        }

        public void Add(T value)
        {
            if (items.Contains(value))
            {
                return;
            }

            items[CursorPosition] = value;
            CursorPosition++;
            if (CursorPosition >= items.Length)
            {
                CursorPosition = 0;
            }
        }
    }
}
