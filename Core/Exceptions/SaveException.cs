using System;

namespace Postulate.Orm.Exceptions
{
    public class SaveException : Exception
    {
        private readonly string _command;
        private readonly object _record;

        public SaveException(string message, string command, object record) : base(message)
        {
            _command = command;
            _record = record;
        }

        public string CommandText { get { return _command; } }
        public object Record { get { return _record; } }
    }
}