using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Indicating whether a property is required.
	/// </summary>
	// Token: 0x0200003B RID: 59
	public enum Required
	{
		/// <summary>
		/// The property is not required. The default state.
		/// </summary>
		// Token: 0x04000132 RID: 306
		Default,
		/// <summary>
		/// The property must be defined in JSON but can be a null value.
		/// </summary>
		// Token: 0x04000133 RID: 307
		AllowNull,
		/// <summary>
		/// The property must be defined in JSON and cannot be a null value.
		/// </summary>
		// Token: 0x04000134 RID: 308
		Always,
		/// <summary>
		/// The property is not required but it cannot be a null value.
		/// </summary>
		// Token: 0x04000135 RID: 309
		DisallowNull
	}
}
