using System;
using System.ComponentModel;

namespace HarmonyLib
{
	/// <summary>A getter delegate type</summary>
	/// <typeparam name="T">Type that getter gets field/property value from</typeparam>
	/// <typeparam name="S">Type of the value that getter gets</typeparam>
	/// <param name="source">The instance get getter uses</param>
	/// <returns>An delegate</returns>
	// Token: 0x02000005 RID: 5
	// (Invoke) Token: 0x06000007 RID: 7
	[Obsolete("Use AccessTools.FieldRefAccess<T, S> for fields and AccessTools.MethodDelegate<Func<T, S>> for property getters")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public delegate S GetterHandler<in T, out S>(T source);
}
