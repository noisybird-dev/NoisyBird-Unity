using System.Collections.Generic;

namespace NoisyBird.EditorExtension.Editor
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                hashSet.Add(item);
            }
        }
    }
}
