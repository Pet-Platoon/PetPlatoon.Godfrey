using System;

namespace PetPlatoon.Godfrey
{
    internal static class Program
    {
        private static void Main()
        {
            var startup = new Startup();
            startup.Run().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
