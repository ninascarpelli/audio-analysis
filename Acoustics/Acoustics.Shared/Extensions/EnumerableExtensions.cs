﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableExtensions.cs" company="QutBioacoustics">
//   All code in this file and all associated files are the copyright of the QUT Bioacoustics Research Group (formally MQUTeR).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

// ReSharper disable once CheckNamespace
namespace System
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class EnumerableExtensions
    {
        #region Public Methods and Operators

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
        {
            return items == null || !items.Any();
        }

        public static Tuple<int, int> SumAndCount(this List<int> items, Func<int, bool> predicate)
        {
            var count = 0;
            var sum = 0;

            foreach (var item in items)
            {
                var filtered = predicate(item);

                if (filtered)
                {
                    count++;
                    sum = sum + item;
                }
            }

            return Tuple.Create(count, sum);
        }

        public static Dictionary<string, T[,]> ToTwoDimensionalArray<T, TBase>(
            this IList<TBase> items,
            Dictionary<string, Func<TBase, T[]>> selectors)
        {
            var itemCount = items.Count;
            int keyCount = selectors.Keys.Count;
            var result = new Dictionary<string, T[,]>(keyCount);
            foreach (var kvp in selectors)
            {
                var current = kvp.Value(items[0]);
                result.Add(kvp.Key, new T[itemCount,  current.Length]);
            }


            Parallel.ForEach(
                selectors,
                (kvp, state, index) =>
                    {
                        var key = kvp.Key;
                        var selctor = kvp.Value;

                        for (int i = 1; i < itemCount; i++)
                        {
                            T[] line = selctor(items[i]);
                            for (int j = 0; j < line.Length; j++)
                            {
                                result[key][i, j] = line[j];
                            }
                        }
                    });

            return result;
        }

        #endregion
    }
}