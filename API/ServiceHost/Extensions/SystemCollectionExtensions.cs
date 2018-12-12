using System.Linq;

namespace System.Collections.Generic
{
    public static class SystemCollectionExtensions
    {
        public static bool SafeAny<T>(this IEnumerable<T> collection)
        {
            return collection?.Any() ?? false;
        }
    }
}
