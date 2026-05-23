using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000418 RID: 1048
	[Serializable]
	internal sealed class ContractException : Exception
	{
		// Token: 0x170007AF RID: 1967
		// (get) Token: 0x06003425 RID: 13349 RVA: 0x000C6CB3 File Offset: 0x000C4EB3
		public ContractFailureKind Kind
		{
			get
			{
				return this._Kind;
			}
		}

		// Token: 0x170007B0 RID: 1968
		// (get) Token: 0x06003426 RID: 13350 RVA: 0x000C6CBB File Offset: 0x000C4EBB
		public string Failure
		{
			get
			{
				return this.Message;
			}
		}

		// Token: 0x170007B1 RID: 1969
		// (get) Token: 0x06003427 RID: 13351 RVA: 0x000C6CC3 File Offset: 0x000C4EC3
		public string UserMessage
		{
			get
			{
				return this._UserMessage;
			}
		}

		// Token: 0x170007B2 RID: 1970
		// (get) Token: 0x06003428 RID: 13352 RVA: 0x000C6CCB File Offset: 0x000C4ECB
		public string Condition
		{
			get
			{
				return this._Condition;
			}
		}

		// Token: 0x06003429 RID: 13353 RVA: 0x000C6CD3 File Offset: 0x000C4ED3
		private ContractException()
		{
			base.HResult = -2146233022;
		}

		// Token: 0x0600342A RID: 13354 RVA: 0x000C6CE6 File Offset: 0x000C4EE6
		public ContractException(ContractFailureKind kind, string failure, string userMessage, string condition, Exception innerException)
			: base(failure, innerException)
		{
			base.HResult = -2146233022;
			this._Kind = kind;
			this._UserMessage = userMessage;
			this._Condition = condition;
		}

		// Token: 0x0600342B RID: 13355 RVA: 0x000C6D12 File Offset: 0x000C4F12
		private ContractException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this._Kind = (ContractFailureKind)info.GetInt32("Kind");
			this._UserMessage = info.GetString("UserMessage");
			this._Condition = info.GetString("Condition");
		}

		// Token: 0x0600342C RID: 13356 RVA: 0x000C6D50 File Offset: 0x000C4F50
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Kind", this._Kind);
			info.AddValue("UserMessage", this._UserMessage);
			info.AddValue("Condition", this._Condition);
		}

		// Token: 0x0400171F RID: 5919
		private readonly ContractFailureKind _Kind;

		// Token: 0x04001720 RID: 5920
		private readonly string _UserMessage;

		// Token: 0x04001721 RID: 5921
		private readonly string _Condition;
	}
}
