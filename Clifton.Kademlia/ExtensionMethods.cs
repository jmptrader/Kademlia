using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
	public static class ExtensionMethods
	{
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
			{
				action(item);
			}
		}

		/// <summary>
		/// ForEach with an index.
		/// </summary>
		public static void ForEachWithIndex<T>(this IEnumerable<T> collection, Action<T, int> action)
		{
			int n = 0;

			foreach (var item in collection)
			{
				action(item, n++);
			}
		}

		/// <summary>
		/// Implements ForEach for non-generic enumerators.
		/// </summary>
		// Usage: Controls.ForEach<Control>(t=>t.DoSomething());
		public static void ForEach<T>(this IEnumerable collection, Action<T> action)
		{
			foreach (T item in collection)
			{
				action(item);
			}
		}

		public static void ForEach(this int n, Action action)
		{
			for (int i = 0; i < n; i++)
			{
				action();
			}
		}

		public static void ForEach(this int n, Action<int> action)
		{
			for (int i = 0; i < n; i++)
			{
				action(i);
			}
		}

		public static void MoveToTail<T>(this List<T> list, T item, Predicate<T> pred)
		{
			int idx = list.FindIndex(pred);
			list.RemoveAt(idx);
			list.Add(item);
		}

		public static void AddMaximum<T>(this List<T> list, T item, int max)
		{
			list.Add(item);

			if (list.Count > max)
			{
				list.RemoveAt(0);
			}
		}

		public static void AddDistinct<T>(this List<T> list, T item)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}

		// TODO: Change the equalityComparer to a KeySelector for the these extension methods:

		public static void AddDistinct<T>(this List<T> list, T item, Func<T, bool> equalityComparer)
		{
			// no items in the list must match the item.
			if (list.None(q => equalityComparer(q)))
			{
				list.Add(item);
			}
		}

		public static void AddRangeDistinct<T>(this List<T> target, List<T> src, Func<T, T, bool> equalityComparer)
		{
			src.ForEach(item =>
			{
				// no items in the list must match the item.
				if (target.None(q => equalityComparer(q, item)))
				{
					target.Add(item);
				}
			});
		}

		public static bool None<TSource>(this IEnumerable<TSource> source)
		{
			return !source.Any();
		}

		public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			return !source.Any(predicate);
		}

		public static void RemoveRange<T>(this List<T> target, List<T> src)
		{
			src.ForEach(s => target.Remove(s));
		}

		public static void RemoveRange<T>(this List<T> target, List<T> src, Func<T, T, bool> equalityComparer)
		{
			src.ForEach(s =>
			{
				int idx = target.FindIndex(t => equalityComparer(t, s));

				if (idx != -1)
				{
					target.RemoveAt(idx);
				}
			});
		}
	}
}
