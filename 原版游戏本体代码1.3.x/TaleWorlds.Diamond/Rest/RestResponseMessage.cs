using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000039 RID: 57
	[DataContract]
	[Serializable]
	public abstract class RestResponseMessage : RestData
	{
		// Token: 0x06000132 RID: 306
		public abstract Message GetMessage();
	}
}
