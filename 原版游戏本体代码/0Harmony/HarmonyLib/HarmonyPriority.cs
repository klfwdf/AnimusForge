using System;

namespace HarmonyLib
{
	/// <summary>A Harmony annotation</summary>
	// Token: 0x02000069 RID: 105
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
	public class HarmonyPriority : HarmonyAttribute
	{
		/// <summary>A Harmony annotation to define patch priority</summary>
		/// <param name="priority">The priority</param>
		// Token: 0x06000201 RID: 513 RVA: 0x0000D770 File Offset: 0x0000B970
		public HarmonyPriority(int priority)
		{
			this.info.priority = priority;
		}
	}
}
