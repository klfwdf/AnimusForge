using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies metadata property handling options for the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000035 RID: 53
	public enum MetadataPropertyHandling
	{
		/// <summary>
		/// Read metadata properties located at the start of a JSON object.
		/// </summary>
		// Token: 0x0400011B RID: 283
		Default,
		/// <summary>
		/// Read metadata properties located anywhere in a JSON object. Note that this setting will impact performance.
		/// </summary>
		// Token: 0x0400011C RID: 284
		ReadAhead,
		/// <summary>
		/// Do not try to read metadata properties.
		/// </summary>
		// Token: 0x0400011D RID: 285
		Ignore
	}
}
