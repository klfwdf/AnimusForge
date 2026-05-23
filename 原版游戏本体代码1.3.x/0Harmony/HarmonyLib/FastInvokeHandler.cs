using System;

namespace HarmonyLib
{
	/// <summary>A delegate to invoke a method</summary>
	/// <param name="target">The instance</param>
	/// <param name="parameters">The method parameters</param>
	/// <returns>The method result</returns>
	// Token: 0x02000009 RID: 9
	// (Invoke) Token: 0x0600001B RID: 27
	public delegate object FastInvokeHandler(object target, params object[] parameters);
}
