using System;

namespace HarmonyLib
{
	/// <summary>A Harmony annotation to define that all methods in a class are to be patched</summary>
	// Token: 0x02000068 RID: 104
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class HarmonyPatchAll : HarmonyAttribute
	{
	}
}
