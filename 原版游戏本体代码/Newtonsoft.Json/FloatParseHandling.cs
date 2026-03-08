using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies how floating point numbers, e.g. 1.0 and 9.9, are parsed when reading JSON text.
	/// </summary>
	// Token: 0x02000012 RID: 18
	public enum FloatParseHandling
	{
		/// <summary>
		/// Floating point numbers are parsed to <see cref="F:Newtonsoft.Json.FloatParseHandling.Double" />.
		/// </summary>
		// Token: 0x04000022 RID: 34
		Double,
		/// <summary>
		/// Floating point numbers are parsed to <see cref="F:Newtonsoft.Json.FloatParseHandling.Decimal" />.
		/// </summary>
		// Token: 0x04000023 RID: 35
		Decimal
	}
}
