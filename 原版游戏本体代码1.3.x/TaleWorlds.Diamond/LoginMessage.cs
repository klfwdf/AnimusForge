using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000014 RID: 20
	[DataContract]
	[Serializable]
	public abstract class LoginMessage : Message
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000064 RID: 100 RVA: 0x0000299C File Offset: 0x00000B9C
		// (set) Token: 0x06000065 RID: 101 RVA: 0x000029A4 File Offset: 0x00000BA4
		[DataMember]
		public PeerId PeerId { get; set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000066 RID: 102 RVA: 0x000029AD File Offset: 0x00000BAD
		// (set) Token: 0x06000067 RID: 103 RVA: 0x000029B5 File Offset: 0x00000BB5
		[JsonProperty]
		public AccessObject AccessObject { get; private set; }

		// Token: 0x06000068 RID: 104 RVA: 0x000029BE File Offset: 0x00000BBE
		public LoginMessage()
		{
		}

		// Token: 0x06000069 RID: 105 RVA: 0x000029C6 File Offset: 0x00000BC6
		protected LoginMessage(PeerId peerId, AccessObject accessObject)
		{
			this.PeerId = peerId;
			this.AccessObject = accessObject;
		}
	}
}
