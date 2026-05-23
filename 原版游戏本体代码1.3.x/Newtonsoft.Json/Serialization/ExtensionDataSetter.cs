using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Sets extension data for an object during deserialization.
	/// </summary>
	/// <param name="o">The object to set extension data on.</param>
	/// <param name="key">The extension data key.</param>
	/// <param name="value">The extension data value.</param>
	// Token: 0x02000089 RID: 137
	// (Invoke) Token: 0x060006B4 RID: 1716
	public delegate void ExtensionDataSetter(object o, string key, [Nullable(2)] object value);
}
