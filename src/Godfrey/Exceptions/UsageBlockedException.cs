using System;

namespace Godfrey.Exceptions
{
    public class UsageBlockedException : Exception
    {
        public UsageBlockedException(string message) : base(message) { }

        public UsageBlockedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
