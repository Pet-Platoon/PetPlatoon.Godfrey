using System;

namespace Godfrey.Exceptions
{
    public class MissingQuotesException : Exception
    {
        public MissingQuotesException(string message) : base(message) { }

        public MissingQuotesException(string message, Exception innerException) : base(message, innerException) { }
    }
}
