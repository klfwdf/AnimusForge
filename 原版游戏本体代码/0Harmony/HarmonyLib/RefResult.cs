using System;

namespace HarmonyLib
{
	/// <summary>Delegate type for "ref return" injections</summary>
	/// <typeparam name="T">Return type of the original method, without ref modifier</typeparam>
	// Token: 0x0200000B RID: 11
	// (Invoke) Token: 0x06000026 RID: 38
	public unsafe delegate T* RefResult<T>();
}
