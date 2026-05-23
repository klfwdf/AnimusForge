using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies how date formatted strings, e.g. <c>"\/Date(1198908717056)\/"</c> and <c>"2012-03-21T05:40Z"</c>, are parsed when reading JSON text.
	/// </summary>
	// Token: 0x0200000D RID: 13
	public enum DateParseHandling
	{
		/// <summary>
		/// Date formatted strings are not parsed to a date type and are read as strings.
		/// </summary>
		// Token: 0x0400000C RID: 12
		None,
		/// <summary>
		/// Date formatted strings, e.g. <c>"\/Date(1198908717056)\/"</c> and <c>"2012-03-21T05:40Z"</c>, are parsed to <see cref="T:System.DateTime" />.
		/// </summary>
		// Token: 0x0400000D RID: 13
		DateTime,
		/// <summary>
		/// Date formatted strings, e.g. <c>"\/Date(1198908717056)\/"</c> and <c>"2012-03-21T05:40Z"</c>, are parsed to <see cref="T:System.DateTimeOffset" />.
		/// </summary>
		// Token: 0x0400000E RID: 14
		DateTimeOffset
	}
}
