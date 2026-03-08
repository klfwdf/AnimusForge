using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TaleWorlds.Library.NewsManager
{
	// Token: 0x020000AC RID: 172
	public struct NewsType
	{
		// Token: 0x170000BB RID: 187
		// (get) Token: 0x06000683 RID: 1667 RVA: 0x00016B0A File Offset: 0x00014D0A
		// (set) Token: 0x06000684 RID: 1668 RVA: 0x00016B12 File Offset: 0x00014D12
		[JsonConverter(typeof(StringEnumConverter))]
		public NewsItem.NewsTypes Type { get; set; }

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x06000685 RID: 1669 RVA: 0x00016B1B File Offset: 0x00014D1B
		// (set) Token: 0x06000686 RID: 1670 RVA: 0x00016B23 File Offset: 0x00014D23
		public int Index { get; set; }
	}
}
