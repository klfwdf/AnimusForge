using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies missing member handling options for the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000036 RID: 54
	public enum MissingMemberHandling
	{
		/// <summary>
		/// Ignore a missing member and do not attempt to deserialize it.
		/// </summary>
		// Token: 0x0400011F RID: 287
		Ignore,
		/// <summary>
		/// Throw a <see cref="T:Newtonsoft.Json.JsonSerializationException" /> when a missing member is encountered during deserialization.
		/// </summary>
		// Token: 0x04000120 RID: 288
		Error
	}
}
