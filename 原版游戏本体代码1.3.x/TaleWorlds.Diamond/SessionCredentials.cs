using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000021 RID: 33
	[DataContract]
	[Serializable]
	public sealed class SessionCredentials
	{
		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000AC RID: 172 RVA: 0x000032B6 File Offset: 0x000014B6
		// (set) Token: 0x060000AD RID: 173 RVA: 0x000032BE File Offset: 0x000014BE
		[DataMember]
		public PeerId PeerId { get; private set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000AE RID: 174 RVA: 0x000032C7 File Offset: 0x000014C7
		// (set) Token: 0x060000AF RID: 175 RVA: 0x000032CF File Offset: 0x000014CF
		[DataMember]
		public SessionKey SessionKey { get; private set; }

		// Token: 0x060000B0 RID: 176 RVA: 0x000032D8 File Offset: 0x000014D8
		[JsonConstructor]
		public SessionCredentials(PeerId peerId, SessionKey sessionKey)
		{
			this.PeerId = peerId;
			this.SessionKey = sessionKey;
		}
	}
}
