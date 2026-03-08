using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x02000074 RID: 116
	public class ObjectInstanceTracker
	{
		// Token: 0x06000420 RID: 1056 RVA: 0x0000E8B7 File Offset: 0x0000CAB7
		public static void RegisterTrackedInstance(string name, WeakReference instance)
		{
		}

		// Token: 0x06000421 RID: 1057 RVA: 0x0000E8BC File Offset: 0x0000CABC
		public static bool CheckBlacklistedTypeCounts(Dictionary<string, int> typeNameCounts, ref string outputLog)
		{
			bool result = false;
			foreach (string text in typeNameCounts.Keys)
			{
				int num = 0;
				int num2 = typeNameCounts[text];
				List<WeakReference> list;
				if (ObjectInstanceTracker.TrackedInstances.TryGetValue(text, out list))
				{
					using (List<WeakReference>.Enumerator enumerator2 = list.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							if (enumerator2.Current.Target != null)
							{
								num++;
							}
						}
					}
				}
				if (num != num2)
				{
					result = true;
					outputLog = string.Concat(new object[] { outputLog, "Type(", text, ") has ", num, " alive instance, but its should be ", num2, "\n" });
				}
			}
			return result;
		}

		// Token: 0x04000149 RID: 329
		private static Dictionary<string, List<WeakReference>> TrackedInstances = new Dictionary<string, List<WeakReference>>();
	}
}
