using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Provides an interface to enable a class to return line and position information.
	/// </summary>
	// Token: 0x02000015 RID: 21
	public interface IJsonLineInfo
	{
		/// <summary>
		/// Gets a value indicating whether the class can return line information.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if <see cref="P:Newtonsoft.Json.IJsonLineInfo.LineNumber" /> and <see cref="P:Newtonsoft.Json.IJsonLineInfo.LinePosition" /> can be provided; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000016 RID: 22
		bool HasLineInfo();

		/// <summary>
		/// Gets the current line number.
		/// </summary>
		/// <value>The current line number or 0 if no line information is available (for example, when <see cref="M:Newtonsoft.Json.IJsonLineInfo.HasLineInfo" /> returns <c>false</c>).</value>
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000017 RID: 23
		int LineNumber { get; }

		/// <summary>
		/// Gets the current line position.
		/// </summary>
		/// <value>The current line position or 0 if no line information is available (for example, when <see cref="M:Newtonsoft.Json.IJsonLineInfo.HasLineInfo" /> returns <c>false</c>).</value>
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000018 RID: 24
		int LinePosition { get; }
	}
}
