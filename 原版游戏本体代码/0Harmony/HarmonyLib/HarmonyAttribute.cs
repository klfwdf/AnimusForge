using System;

namespace HarmonyLib
{
	/// <summary>The base class for all Harmony annotations (not meant to be used directly)</summary>
	// Token: 0x02000063 RID: 99
	public class HarmonyAttribute : Attribute
	{
		/// <summary>The common information for all attributes</summary>
		// Token: 0x0400017A RID: 378
		public HarmonyMethod info = new HarmonyMethod();
	}
}
