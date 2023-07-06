﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.Helpers {

    internal static class LinqHelpers {

        internal static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) {
            return DistinctBy(source, keySelector, null);
        }

        internal static IEnumerable<TSource> DistinctBy<TSource, TKey>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer) {

            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return _(); IEnumerable<TSource> _() {
                var knownKeys = new HashSet<TKey>(comparer);
                foreach (var element in source) {
                    if (knownKeys.Add(keySelector(element)))
                        yield return element;
                }
            }

        }

    }

}
