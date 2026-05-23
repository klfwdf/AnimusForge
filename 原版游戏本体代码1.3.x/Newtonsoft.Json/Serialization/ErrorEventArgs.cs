using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Provides data for the Error event.
	/// </summary>
	// Token: 0x0200007C RID: 124
	[NullableContext(1)]
	[Nullable(0)]
	public class ErrorEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the current object the error event is being raised against.
		/// </summary>
		/// <value>The current object the error event is being raised against.</value>
		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x0600067C RID: 1660 RVA: 0x0001C0CB File Offset: 0x0001A2CB
		[Nullable(2)]
		public object CurrentObject
		{
			[NullableContext(2)]
			get;
		}

		/// <summary>
		/// Gets the error context.
		/// </summary>
		/// <value>The error context.</value>
		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x0600067D RID: 1661 RVA: 0x0001C0D3 File Offset: 0x0001A2D3
		public ErrorContext ErrorContext { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.ErrorEventArgs" /> class.
		/// </summary>
		/// <param name="currentObject">The current object.</param>
		/// <param name="errorContext">The error context.</param>
		// Token: 0x0600067E RID: 1662 RVA: 0x0001C0DB File Offset: 0x0001A2DB
		public ErrorEventArgs([Nullable(2)] object currentObject, ErrorContext errorContext)
		{
			this.CurrentObject = currentObject;
			this.ErrorContext = errorContext;
		}
	}
}
