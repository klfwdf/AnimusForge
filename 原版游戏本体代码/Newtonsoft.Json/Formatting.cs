using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies formatting options for the <see cref="T:Newtonsoft.Json.JsonTextWriter" />.
	/// </summary>
	// Token: 0x02000013 RID: 19
	public enum Formatting
	{
		/// <summary>
		/// No special formatting is applied. This is the default.
		/// </summary>
		// Token: 0x04000025 RID: 37
		None,
		/// <summary>
		/// Causes child objects to be indented according to the <see cref="P:Newtonsoft.Json.JsonTextWriter.Indentation" /> and <see cref="P:Newtonsoft.Json.JsonTextWriter.IndentChar" /> settings.
		/// </summary>
		// Token: 0x04000026 RID: 38
		Indented
	}
}
