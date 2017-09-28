using System;
using System.Data;
using System.Runtime.Serialization;

namespace Godfrey.Exceptions
{
    public class UsageBlockedException : Exception
    {
        public UsageBlockedException(string message) : base(message) { }

        public UsageBlockedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
