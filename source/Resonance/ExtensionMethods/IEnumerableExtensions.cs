using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Resonance.ExtensionMethods
{
    /// <summary>
    /// Contains <see cref="IEnumerable{T}"/> extension methods.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns a distinct collection by the specified property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> property)
        {
            return source.GroupBy(property).Select(g => g.First());
        }
    }
}
