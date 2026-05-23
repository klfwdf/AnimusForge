using System;
using System.Collections.Generic;

namespace TaleWorlds.Library;

public class ObjectInstanceTracker
{
	private static Dictionary<string, List<WeakReference>> TrackedInstances = new Dictionary<string, List<WeakReference>>();

	public static void RegisterTrackedInstance(string name, WeakReference instance)
	{
	}

	public static bool CheckBlacklistedTypeCounts(Dictionary<string, int> typeNameCounts, ref string outputLog)
	{
		bool result = false;
		foreach (string key in typeNameCounts.Keys)
		{
			int num = 0;
			int num2 = typeNameCounts[key];
			if (TrackedInstances.TryGetValue(key, out var value))
			{
				foreach (WeakReference item in value)
				{
					if (item.Target != null)
					{
						num++;
					}
				}
			}
			if (num != num2)
			{
				result = true;
				outputLog = outputLog + "Type(" + key + ") has " + num + " alive instance, but its should be " + num2 + "\n";
			}
		}
		return result;
	}
}
