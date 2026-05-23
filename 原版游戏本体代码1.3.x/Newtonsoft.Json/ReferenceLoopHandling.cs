using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies reference loop handling options for the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x0200003A RID: 58
	public enum ReferenceLoopHandling
	{
		/// <summary>
		/// Throw a <see cref="T:Newtonsoft.Json.JsonSerializationException" /> when a loop is encountered.
		/// </summary>
		// Token: 0x0400012E RID: 302
		Error,
		/// <summary>
		/// Ignore loop references and do not serialize.
		/// </summary>
		// Token: 0x0400012F RID: 303
		Ignore,
		/// <summary>
		/// Serialize loop references.
		/// </summary>
		// Token: 0x04000130 RID: 304
		Serialize
	}
}
