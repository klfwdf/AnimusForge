using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies the settings used when loading JSON.
	/// </summary>
	// Token: 0x020000C4 RID: 196
	public class JsonLoadSettings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> class.
		/// </summary>
		// Token: 0x06000AC3 RID: 2755 RVA: 0x0002B2BC File Offset: 0x000294BC
		public JsonLoadSettings()
		{
			this._lineInfoHandling = LineInfoHandling.Load;
			this._commentHandling = CommentHandling.Ignore;
			this._duplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace;
		}

		/// <summary>
		/// Gets or sets how JSON comments are handled when loading JSON.
		/// The default value is <see cref="F:Newtonsoft.Json.Linq.CommentHandling.Ignore" />.
		/// </summary>
		/// <value>The JSON comment handling.</value>
		// Token: 0x170001ED RID: 493
		// (get) Token: 0x06000AC4 RID: 2756 RVA: 0x0002B2D9 File Offset: 0x000294D9
		// (set) Token: 0x06000AC5 RID: 2757 RVA: 0x0002B2E1 File Offset: 0x000294E1
		public CommentHandling CommentHandling
		{
			get
			{
				return this._commentHandling;
			}
			set
			{
				if (value < CommentHandling.Ignore || value > CommentHandling.Load)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._commentHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how JSON line info is handled when loading JSON.
		/// The default value is <see cref="F:Newtonsoft.Json.Linq.LineInfoHandling.Load" />.
		/// </summary>
		/// <value>The JSON line info handling.</value>
		// Token: 0x170001EE RID: 494
		// (get) Token: 0x06000AC6 RID: 2758 RVA: 0x0002B2FD File Offset: 0x000294FD
		// (set) Token: 0x06000AC7 RID: 2759 RVA: 0x0002B305 File Offset: 0x00029505
		public LineInfoHandling LineInfoHandling
		{
			get
			{
				return this._lineInfoHandling;
			}
			set
			{
				if (value < LineInfoHandling.Ignore || value > LineInfoHandling.Load)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._lineInfoHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how duplicate property names in JSON objects are handled when loading JSON.
		/// The default value is <see cref="F:Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Replace" />.
		/// </summary>
		/// <value>The JSON duplicate property name handling.</value>
		// Token: 0x170001EF RID: 495
		// (get) Token: 0x06000AC8 RID: 2760 RVA: 0x0002B321 File Offset: 0x00029521
		// (set) Token: 0x06000AC9 RID: 2761 RVA: 0x0002B329 File Offset: 0x00029529
		public DuplicatePropertyNameHandling DuplicatePropertyNameHandling
		{
			get
			{
				return this._duplicatePropertyNameHandling;
			}
			set
			{
				if (value < DuplicatePropertyNameHandling.Replace || value > DuplicatePropertyNameHandling.Error)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._duplicatePropertyNameHandling = value;
			}
		}

		// Token: 0x0400038C RID: 908
		private CommentHandling _commentHandling;

		// Token: 0x0400038D RID: 909
		private LineInfoHandling _lineInfoHandling;

		// Token: 0x0400038E RID: 910
		private DuplicatePropertyNameHandling _duplicatePropertyNameHandling;
	}
}
