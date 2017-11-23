namespace Postulate.Orm.Merge
{
    public class LineRange
    {
        private readonly int _start;
        private readonly int _end;

        public LineRange(int start, int end)
        {
            _start = start;
            _end = end;
        }

        public int Start { get { return _start; } }
        public int End { get { return _end; } }
    }
}