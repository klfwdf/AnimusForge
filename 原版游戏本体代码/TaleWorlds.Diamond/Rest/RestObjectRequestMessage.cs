using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x0200003F RID: 63
	[DataContract]
	[Serializable]
	public class RestObjectRequestMessage : RestRequestMessage
	{
		// Token: 0x17000047 RID: 71
		// (get) Token: 0x0600016A RID: 362 RVA: 0x00004CEF File Offset: 0x00002EEF
		// (set) Token: 0x0600016B RID: 363 RVA: 0x00004CF7 File Offset: 0x00002EF7
		[DataMember]
		public MessageType MessageType { get; private set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600016C RID: 364 RVA: 0x00004D00 File Offset: 0x00002F00
		// (set) Token: 0x0600016D RID: 365 RVA: 0x00004D08 File Offset: 0x00002F08
		[DataMember]
		public SessionCredentials SessionCredentials { get; private set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x0600016E RID: 366 RVA: 0x00004D11 File Offset: 0x00002F11
		// (set) Token: 0x0600016F RID: 367 RVA: 0x00004D19 File Offset: 0x00002F19
		[DataMember]
		public Message Message { get; private set; }

		// Token: 0x06000170 RID: 368 RVA: 0x00004D22 File Offset: 0x00002F22
		public RestObjectRequestMessage()
		{
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00004D2A File Offset: 0x00002F2A
		public RestObjectRequestMessage(SessionCredentials sessionCredentials, Message message, MessageType messageType)
		{
			this.Message = message;
			this.MessageType = messageType;
			this.SessionCredentials = sessionCredentials;
		}
	}
}
