using System;

namespace HarmonyLib
{
	/// <summary>A Harmony annotation</summary>
	// Token: 0x0200006A RID: 106
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
	public class HarmonyBefore : HarmonyAttribute
	{
		/// <summary>A Harmony annotation to define that a patch comes before another patch</summary>
		/// <param name="before">The array of harmony IDs of the other patches</param>
		// Token: 0x06000202 RID: 514 RVA: 0x0000D784 File Offset: 0x0000B984
		public HarmonyBefore(params string[] before)
		{
			this.info.before = before;
		}
	}
}
