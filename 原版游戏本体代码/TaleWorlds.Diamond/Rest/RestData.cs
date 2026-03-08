using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000034 RID: 52
	[DataContract]
	[Serializable]
	public abstract class RestData
	{
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x0600011F RID: 287 RVA: 0x00003E84 File Offset: 0x00002084
		// (set) Token: 0x06000120 RID: 288 RVA: 0x00003E8C File Offset: 0x0000208C
		[DataMember]
		public string TypeName { get; set; }

		// Token: 0x06000121 RID: 289 RVA: 0x00003E95 File Offset: 0x00002095
		protected RestData()
		{
			this.TypeName = base.GetType().FullName;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00003EAE File Offset: 0x000020AE
		public string SerializeAsJson()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
