using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000042 RID: 66
	[DataContract]
	[Serializable]
	public sealed class RestResponse : RestData
	{
		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000175 RID: 373 RVA: 0x00004D60 File Offset: 0x00002F60
		// (set) Token: 0x06000176 RID: 374 RVA: 0x00004D68 File Offset: 0x00002F68
		[DataMember]
		public bool Successful { get; private set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000177 RID: 375 RVA: 0x00004D71 File Offset: 0x00002F71
		// (set) Token: 0x06000178 RID: 376 RVA: 0x00004D79 File Offset: 0x00002F79
		[DataMember]
		public string SuccessfulReason { get; private set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000179 RID: 377 RVA: 0x00004D82 File Offset: 0x00002F82
		// (set) Token: 0x0600017A RID: 378 RVA: 0x00004D8A File Offset: 0x00002F8A
		[DataMember]
		public RestFunctionResult FunctionResult { get; set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600017B RID: 379 RVA: 0x00004D93 File Offset: 0x00002F93
		// (set) Token: 0x0600017C RID: 380 RVA: 0x00004D9B File Offset: 0x00002F9B
		[DataMember]
		public byte[] UserCertificate { get; set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600017D RID: 381 RVA: 0x00004DA4 File Offset: 0x00002FA4
		public int RemainingMessageCount
		{
			get
			{
				if (this._responseMessages != null)
				{
					return this._responseMessages.Count;
				}
				return 0;
			}
		}

		// Token: 0x0600017E RID: 382 RVA: 0x00004DBB File Offset: 0x00002FBB
		public RestResponse()
		{
			this._responseMessages = new List<RestResponseMessage>();
		}

		// Token: 0x0600017F RID: 383 RVA: 0x00004DCE File Offset: 0x00002FCE
		public void SetSuccessful(bool successful, string successfulReason)
		{
			this.Successful = successful;
			this.SuccessfulReason = successfulReason;
		}

		// Token: 0x06000180 RID: 384 RVA: 0x00004DDE File Offset: 0x00002FDE
		public static RestResponse Create(bool successful, string successfulReason)
		{
			RestResponse restResponse = new RestResponse();
			restResponse.SetSuccessful(successful, successfulReason);
			return restResponse;
		}

		// Token: 0x06000181 RID: 385 RVA: 0x00004DED File Offset: 0x00002FED
		public RestResponseMessage TryDequeueMessage()
		{
			if (this._responseMessages != null && this._responseMessages.Count > 0)
			{
				RestResponseMessage result = this._responseMessages[0];
				this._responseMessages.RemoveAt(0);
				return result;
			}
			return null;
		}

		// Token: 0x06000182 RID: 386 RVA: 0x00004E1F File Offset: 0x0000301F
		public void ClearMessageQueue()
		{
			this._responseMessages.Clear();
		}

		// Token: 0x06000183 RID: 387 RVA: 0x00004E2C File Offset: 0x0000302C
		public void EnqueueMessage(RestResponseMessage message)
		{
			this._responseMessages.Add(message);
		}

		// Token: 0x04000088 RID: 136
		[DataMember]
		private List<RestResponseMessage> _responseMessages;
	}
}
