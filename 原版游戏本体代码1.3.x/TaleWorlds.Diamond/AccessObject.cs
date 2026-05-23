using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000002 RID: 2
	[JsonConverter(typeof(AccessObjectJsonConverter))]
	[Serializable]
	public abstract class AccessObject
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
		public string Type { get; set; }
	}
}
