using System;
using System.ComponentModel;

namespace HarmonyLib
{
	/// <summary>A setter delegate type</summary>
	/// <typeparam name="T">Type that setter sets field/property value for</typeparam>
	/// <typeparam name="S">Type of the value that setter sets</typeparam>
	/// <param name="source">The instance the setter uses</param>
	/// <param name="value">The value the setter uses</param>
	/// <returns>An delegate</returns>
	// Token: 0x02000006 RID: 6
	// (Invoke) Token: 0x0600000B RID: 11
	[Obsolete("Use AccessTools.FieldRefAccess<T, S> for fields and AccessTools.MethodDelegate<Action<T, S>> for property setters")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public delegate void SetterHandler<in T, in S>(T source, S value);
}
