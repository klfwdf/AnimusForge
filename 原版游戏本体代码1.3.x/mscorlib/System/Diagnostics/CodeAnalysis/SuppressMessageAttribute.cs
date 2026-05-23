using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200041A RID: 1050
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
	[Conditional("CODE_ANALYSIS")]
	[__DynamicallyInvokable]
	public sealed class SuppressMessageAttribute : Attribute
	{
		// Token: 0x0600342F RID: 13359 RVA: 0x000C6DB5 File Offset: 0x000C4FB5
		[__DynamicallyInvokable]
		public SuppressMessageAttribute(string category, string checkId)
		{
			this.category = category;
			this.checkId = checkId;
		}

		// Token: 0x170007B3 RID: 1971
		// (get) Token: 0x06003430 RID: 13360 RVA: 0x000C6DCB File Offset: 0x000C4FCB
		[__DynamicallyInvokable]
		public string Category
		{
			[__DynamicallyInvokable]
			get
			{
				return this.category;
			}
		}

		// Token: 0x170007B4 RID: 1972
		// (get) Token: 0x06003431 RID: 13361 RVA: 0x000C6DD3 File Offset: 0x000C4FD3
		[__DynamicallyInvokable]
		public string CheckId
		{
			[__DynamicallyInvokable]
			get
			{
				return this.checkId;
			}
		}

		// Token: 0x170007B5 RID: 1973
		// (get) Token: 0x06003432 RID: 13362 RVA: 0x000C6DDB File Offset: 0x000C4FDB
		// (set) Token: 0x06003433 RID: 13363 RVA: 0x000C6DE3 File Offset: 0x000C4FE3
		[__DynamicallyInvokable]
		public string Scope
		{
			[__DynamicallyInvokable]
			get
			{
				return this.scope;
			}
			[__DynamicallyInvokable]
			set
			{
				this.scope = value;
			}
		}

		// Token: 0x170007B6 RID: 1974
		// (get) Token: 0x06003434 RID: 13364 RVA: 0x000C6DEC File Offset: 0x000C4FEC
		// (set) Token: 0x06003435 RID: 13365 RVA: 0x000C6DF4 File Offset: 0x000C4FF4
		[__DynamicallyInvokable]
		public string Target
		{
			[__DynamicallyInvokable]
			get
			{
				return this.target;
			}
			[__DynamicallyInvokable]
			set
			{
				this.target = value;
			}
		}

		// Token: 0x170007B7 RID: 1975
		// (get) Token: 0x06003436 RID: 13366 RVA: 0x000C6DFD File Offset: 0x000C4FFD
		// (set) Token: 0x06003437 RID: 13367 RVA: 0x000C6E05 File Offset: 0x000C5005
		[__DynamicallyInvokable]
		public string MessageId
		{
			[__DynamicallyInvokable]
			get
			{
				return this.messageId;
			}
			[__DynamicallyInvokable]
			set
			{
				this.messageId = value;
			}
		}

		// Token: 0x170007B8 RID: 1976
		// (get) Token: 0x06003438 RID: 13368 RVA: 0x000C6E0E File Offset: 0x000C500E
		// (set) Token: 0x06003439 RID: 13369 RVA: 0x000C6E16 File Offset: 0x000C5016
		[__DynamicallyInvokable]
		public string Justification
		{
			[__DynamicallyInvokable]
			get
			{
				return this.justification;
			}
			[__DynamicallyInvokable]
			set
			{
				this.justification = value;
			}
		}

		// Token: 0x04001722 RID: 5922
		private string category;

		// Token: 0x04001723 RID: 5923
		private string justification;

		// Token: 0x04001724 RID: 5924
		private string checkId;

		// Token: 0x04001725 RID: 5925
		private string scope;

		// Token: 0x04001726 RID: 5926
		private string target;

		// Token: 0x04001727 RID: 5927
		private string messageId;
	}
}
