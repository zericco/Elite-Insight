#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 16.05.2015
// ///
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using RegulatedNoise.Core.Algorithms;

namespace RegulatedNoise.Core.Helpers
{
	public static class EnumerableExtensions
	{
		public static TItem[] CloneN<TItem>(this IEnumerable<TItem> array)
		{
			if (array == null)
				return null;
			else
				return array.ToArray();
		}

		public static List<T> LevenFilter<T>(this IEnumerable<T> source, string text, Func<T, string> levenGetter, int count = 8)
		{
			return source.Select(s => new KeyValuePair<T, int>(s, Levenshtein.Compute(text, levenGetter(s))))
				.OrderBy(kvp => kvp.Value)
				.Take(count)
				.Select(kvp => kvp.Key)
				.ToList();
		}

		public static List<string> LevenFilter(this IEnumerable<string> source, string text, int count = 8)
		{
			return LevenFilter(source, text, s => s, count);
		}
	}
}