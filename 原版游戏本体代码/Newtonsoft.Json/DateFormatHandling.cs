using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies how dates are formatted when writing JSON text.
	/// </summary>
	// Token: 0x0200000C RID: 12
	public enum DateFormatHandling
	{
		/// <summary>
		/// Dates are written in the ISO 8601 format, e.g. <c>"2012-03-21T05:40Z"</c>.
		/// </summary>
		// Token: 0x04000009 RID: 9
		IsoDateFormat,
		/// <summary>
		/// Dates are written in the Microsoft JSON format, e.g. <c>"\/Date(1198908717056)\/"</c>.
		/// </summary>
		// Token: 0x0400000A RID: 10
		MicrosoftDateFormat
	}
}
