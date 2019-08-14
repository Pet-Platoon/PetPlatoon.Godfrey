using System;
using System.Linq;

namespace PetPlatoon.Godfrey.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<TSource> RandomOrder<TSource>(this IQueryable<TSource> source, Random random = null)
        {
            if (random == null)
            {
                random = new Random(Environment.TickCount);
            }

            var shuffled = source.OrderBy(x => random.Next());
            return shuffled;
        }
    }
}
