using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies how object creation is handled by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000038 RID: 56
	public enum ObjectCreationHandling
	{
		/// <summary>
		/// Reuse existing objects, create new objects when needed.
		/// </summary>
		// Token: 0x04000125 RID: 293
		Auto,
		/// <summary>
		/// Only reuse existing objects.
		/// </summary>
		// Token: 0x04000126 RID: 294
		Reuse,
		/// <summary>
		/// Always create new objects.
		/// </summary>
		// Token: 0x04000127 RID: 295
		Replace
	}
}
