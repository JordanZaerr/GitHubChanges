using System;
using System.Collections.Generic;

namespace GitHubChanges;

public static class Extensions
{
    public static void Each<T>(this IEnumerable<T> list, Action<T> action)
	{
		if (list == null) return;

		foreach (var t in list)
		{
			action(t);
		}
	}
}