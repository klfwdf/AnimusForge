using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies how duplicate property names are handled when loading JSON.
	/// </summary>
	// Token: 0x020000B7 RID: 183
	public enum DuplicatePropertyNameHandling
	{
		/// <summary>
		/// Replace the existing value when there is a duplicate property. The value of the last property in the JSON object will be used.
		/// </summary>
		// Token: 0x04000376 RID: 886
		Replace,
		/// <summary>
		/// Ignore the new value when there is a duplicate property. The value of the first property in the JSON object will be used.
		/// </summary>
		// Token: 0x04000377 RID: 887
		Ignore,
		/// <summary>
		/// Throw a <see cref="T:Newtonsoft.Json.JsonReaderException" /> when a duplicate property is encountered.
		/// </summary>
		// Token: 0x04000378 RID: 888
		Error
	}
}
