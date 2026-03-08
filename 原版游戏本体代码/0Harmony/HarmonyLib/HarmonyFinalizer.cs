using System;

namespace HarmonyLib
{
	/// <summary>Specifies the Finalizer function in a patch class</summary>
	// Token: 0x02000074 RID: 116
	[AttributeUsage(AttributeTargets.Method)]
	public class HarmonyFinalizer : Attribute
	{
	}
}
