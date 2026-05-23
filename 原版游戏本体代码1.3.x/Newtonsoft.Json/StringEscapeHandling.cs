using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies how strings are escaped when writing JSON text.
	/// </summary>
	// Token: 0x0200003C RID: 60
	public enum StringEscapeHandling
	{
		/// <summary>
		/// Only control characters (e.g. newline) are escaped.
		/// </summary>
		// Token: 0x04000137 RID: 311
		Default,
		/// <summary>
		/// All non-ASCII and control characters (e.g. newline) are escaped.
		/// </summary>
		// Token: 0x04000138 RID: 312
		EscapeNonAscii,
		/// <summary>
		/// HTML (&lt;, &gt;, &amp;, ', ") and control characters (e.g. newline) are escaped.
		/// </summary>
		// Token: 0x04000139 RID: 313
		EscapeHtml
	}
}
