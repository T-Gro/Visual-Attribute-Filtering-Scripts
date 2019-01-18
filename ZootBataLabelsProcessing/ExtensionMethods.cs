using System;
using System.Collections.Generic;
using System.Linq;

namespace ZootBataLabelsProcessing
{
    public static class ExtensionMethods
    {
        public static IDictionary<K, V> Unique<K, V>(this ILookup<K, V> original)
        {
            return original.ToDictionary(x => x.Key, x => x.Single());
        }

      public static IDictionary<K, V> Reverse<K, V>(this IDictionary<V, K> original)
      {
        return original.ToDictionary(x => x.Value, x => x.Key);
      }

        public static ILookup<K, V> WithoutSingles<K, V>(this ILookup<K, V> original)
        {
            return
                (from lkp in original
                 where lkp.Count() > 1
                 from entry in lkp
                 select new { lkp.Key, entry }).ToLookup(x => x.Key, x => x.entry);
        }

        public static double PointBiserialCorrelation(this ICollection<Tuple<ZootLabel, double>> rankedList, Func<ZootLabel, bool> func)
        {
            var split = rankedList.ToLookup(x => func(x.Item1));
            var hitsAverage = split[true].Average(x => x.Item2);
            var nonHitsAverage = split[false].Average(x => x.Item2);
            var avg = rankedList.Average(x => x.Item2);
            var stddev = Math.Sqrt(rankedList.Average(x => (x.Item2 - avg) * (x.Item2 - avg)));

            var balanceFactor = (split[true].Count() * split[false].Count()) / (rankedList.Count * rankedList.Count * 1.0);
            var result = ((hitsAverage - nonHitsAverage) / stddev) * Math.Sqrt(balanceFactor);

            return result;
        }

        public static ILookup<string, T> CreateIndex<T>(this IEnumerable<T> records, Func<T, IEnumerable<string>> extraction)
        {
            return (
                from r in records
                from i in extraction(r)
                where !String.IsNullOrWhiteSpace(i)
                select new { r, i }).ToLookup(x => x.i.ToLower(), x => x.r);
        }
    }
}
