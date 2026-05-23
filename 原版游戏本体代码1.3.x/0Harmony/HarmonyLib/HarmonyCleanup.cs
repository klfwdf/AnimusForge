using System;

namespace HarmonyLib
{
	/// <summary>Specifies the Cleanup function in a patch class</summary>
	// Token: 0x0200006E RID: 110
	[AttributeUsage(AttributeTargets.Method)]
	public class HarmonyCleanup : Attribute
	{
	}
}
