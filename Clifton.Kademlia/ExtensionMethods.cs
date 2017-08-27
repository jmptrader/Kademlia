using System;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
	public static class ExtensionMethods
	{
		public static void ForEach(this int n, Action action)
		{
			for (int i = 0; i < n; i++)
			{
				action();
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
	}
}
