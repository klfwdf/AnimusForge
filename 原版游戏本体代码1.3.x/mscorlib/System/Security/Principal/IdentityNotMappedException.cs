using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Security.Principal
{
	// Token: 0x0200033E RID: 830
	[ComVisible(false)]
	[Serializable]
	public sealed class IdentityNotMappedException : SystemException
	{
		// Token: 0x06002957 RID: 10583 RVA: 0x00098ECC File Offset: 0x000970CC
		public IdentityNotMappedException()
			: base(Environment.GetResourceString("IdentityReference_IdentityNotMapped"))
		{
		}

		// Token: 0x06002958 RID: 10584 RVA: 0x00098EDE File Offset: 0x000970DE
		public IdentityNotMappedException(string message)
			: base(message)
		{
		}

		// Token: 0x06002959 RID: 10585 RVA: 0x00098EE7 File Offset: 0x000970E7
		public IdentityNotMappedException(string message, Exception inner)
			: base(message, inner)
		{
		}

		// Token: 0x0600295A RID: 10586 RVA: 0x00098EF1 File Offset: 0x000970F1
		internal IdentityNotMappedException(string message, IdentityReferenceCollection unmappedIdentities)
			: this(message)
		{
			this.unmappedIdentities = unmappedIdentities;
		}

		// Token: 0x0600295B RID: 10587 RVA: 0x00098F01 File Offset: 0x00097101
		internal IdentityNotMappedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x0600295C RID: 10588 RVA: 0x00098F0B File Offset: 0x0009710B
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			base.GetObjectData(serializationInfo, streamingContext);
		}

		// Token: 0x17000566 RID: 1382
		// (get) Token: 0x0600295D RID: 10589 RVA: 0x00098F15 File Offset: 0x00097115
		public IdentityReferenceCollection UnmappedIdentities
		{
			get
			{
				if (this.unmappedIdentities == null)
				{
					this.unmappedIdentities = new IdentityReferenceCollection();
				}
				return this.unmappedIdentities;
			}
		}

		// Token: 0x04001107 RID: 4359
		private IdentityReferenceCollection unmappedIdentities;
	}
}
