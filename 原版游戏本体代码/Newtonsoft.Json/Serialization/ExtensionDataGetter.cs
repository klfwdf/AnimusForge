using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Gets extension data for an object during serialization.
	/// </summary>
	/// <param name="o">The object to set extension data on.</param>
	// Token: 0x0200008A RID: 138
	// (Invoke) Token: 0x060006B8 RID: 1720
	[return: Nullable(new byte[] { 2, 0, 1, 1 })]
	public delegate IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o);
}
