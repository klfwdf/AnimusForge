using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000041 RID: 65
	[DataContract]
	[Serializable]
	public abstract class RestRequestMessage : RestData
	{
		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000172 RID: 370 RVA: 0x00004D47 File Offset: 0x00002F47
		// (set) Token: 0x06000173 RID: 371 RVA: 0x00004D4F File Offset: 0x00002F4F
		[DataMember]
		public byte[] UserCertificate { get; set; }
	}
}
