using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x0200003A RID: 58
	[DataContract]
	[Serializable]
	public class AliveMessage : RestRequestMessage
	{
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x06000134 RID: 308 RVA: 0x00003FE6 File Offset: 0x000021E6
		// (set) Token: 0x06000135 RID: 309 RVA: 0x00003FEE File Offset: 0x000021EE
		[DataMember]
		public SessionCredentials SessionCredentials { get; private set; }

		// Token: 0x06000136 RID: 310 RVA: 0x00003FF7 File Offset: 0x000021F7
		public AliveMessage()
		{
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00003FFF File Offset: 0x000021FF
		[JsonConstructor]
		public AliveMessage(SessionCredentials sessionCredentials)
		{
			this.SessionCredentials = sessionCredentials;
		}
	}
}
