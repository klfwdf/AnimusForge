using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Provides information surrounding an error.
	/// </summary>
	// Token: 0x0200007B RID: 123
	[NullableContext(1)]
	[Nullable(0)]
	public class ErrorContext
	{
		// Token: 0x06000673 RID: 1651 RVA: 0x0001C064 File Offset: 0x0001A264
		internal ErrorContext([Nullable(2)] object originalObject, [Nullable(2)] object member, string path, Exception error)
		{
			this.OriginalObject = originalObject;
			this.Member = member;
			this.Error = error;
			this.Path = path;
		}

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x06000674 RID: 1652 RVA: 0x0001C089 File Offset: 0x0001A289
		// (set) Token: 0x06000675 RID: 1653 RVA: 0x0001C091 File Offset: 0x0001A291
		internal bool Traced { get; set; }

		/// <summary>
		/// Gets the error.
		/// </summary>
		/// <value>The error.</value>
		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x06000676 RID: 1654 RVA: 0x0001C09A File Offset: 0x0001A29A
		public Exception Error { get; }

		/// <summary>
		/// Gets the original object that caused the error.
		/// </summary>
		/// <value>The original object that caused the error.</value>
		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x06000677 RID: 1655 RVA: 0x0001C0A2 File Offset: 0x0001A2A2
		[Nullable(2)]
		public object OriginalObject
		{
			[NullableContext(2)]
			get;
		}

		/// <summary>
		/// Gets the member that caused the error.
		/// </summary>
		/// <value>The member that caused the error.</value>
		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x06000678 RID: 1656 RVA: 0x0001C0AA File Offset: 0x0001A2AA
		[Nullable(2)]
		public object Member
		{
			[NullableContext(2)]
			get;
		}

		/// <summary>
		/// Gets the path of the JSON location where the error occurred.
		/// </summary>
		/// <value>The path of the JSON location where the error occurred.</value>
		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x06000679 RID: 1657 RVA: 0x0001C0B2 File Offset: 0x0001A2B2
		public string Path { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Newtonsoft.Json.Serialization.ErrorContext" /> is handled.
		/// </summary>
		/// <value><c>true</c> if handled; otherwise, <c>false</c>.</value>
		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x0600067A RID: 1658 RVA: 0x0001C0BA File Offset: 0x0001A2BA
		// (set) Token: 0x0600067B RID: 1659 RVA: 0x0001C0C2 File Offset: 0x0001A2C2
		public bool Handled { get; set; }
	}
}
