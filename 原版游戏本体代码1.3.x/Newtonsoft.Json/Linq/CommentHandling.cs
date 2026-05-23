using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies how JSON comments are handled when loading JSON.
	/// </summary>
	// Token: 0x020000B6 RID: 182
	public enum CommentHandling
	{
		/// <summary>
		/// Ignore comments.
		/// </summary>
		// Token: 0x04000373 RID: 883
		Ignore,
		/// <summary>
		/// Load comments as a <see cref="T:Newtonsoft.Json.Linq.JValue" /> with type <see cref="F:Newtonsoft.Json.Linq.JTokenType.Comment" />.
		/// </summary>
		// Token: 0x04000374 RID: 884
		Load
	}
}
