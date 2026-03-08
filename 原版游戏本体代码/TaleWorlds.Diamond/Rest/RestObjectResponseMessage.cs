using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000038 RID: 56
	[DataContract]
	[Serializable]
	public class RestObjectResponseMessage : RestResponseMessage
	{
		// Token: 0x0600012F RID: 303 RVA: 0x00003FBF File Offset: 0x000021BF
		public override Message GetMessage()
		{
			return this._message;
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00003FC7 File Offset: 0x000021C7
		public RestObjectResponseMessage()
		{
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00003FCF File Offset: 0x000021CF
		public RestObjectResponseMessage(Message message)
		{
			this._message = message;
		}

		// Token: 0x0400005C RID: 92
		[DataMember]
		private Message _message;
	}
}
