using System;

namespace HarmonyLib
{
	/// <summary>A constructor delegate type</summary>
	/// <typeparam name="T">Type that constructor creates</typeparam>
	/// <returns>An delegate</returns>
	// Token: 0x02000007 RID: 7
	// (Invoke) Token: 0x0600000F RID: 15
	public delegate T InstantiationHandler<out T>();
}
