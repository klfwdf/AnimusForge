using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TaleWorlds.Library.NewsManager
{
	// Token: 0x020000AB RID: 171
	public struct NewsItem
	{
		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x06000679 RID: 1657 RVA: 0x00016AB5 File Offset: 0x00014CB5
		// (set) Token: 0x0600067A RID: 1658 RVA: 0x00016ABD File Offset: 0x00014CBD
		public string Title { get; set; }

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x0600067B RID: 1659 RVA: 0x00016AC6 File Offset: 0x00014CC6
		// (set) Token: 0x0600067C RID: 1660 RVA: 0x00016ACE File Offset: 0x00014CCE
		public string Description { get; set; }

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x0600067D RID: 1661 RVA: 0x00016AD7 File Offset: 0x00014CD7
		// (set) Token: 0x0600067E RID: 1662 RVA: 0x00016ADF File Offset: 0x00014CDF
		public string ImageSourcePath { get; set; }

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x0600067F RID: 1663 RVA: 0x00016AE8 File Offset: 0x00014CE8
		// (set) Token: 0x06000680 RID: 1664 RVA: 0x00016AF0 File Offset: 0x00014CF0
		public List<NewsType> Feeds { get; set; }

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x06000681 RID: 1665 RVA: 0x00016AF9 File Offset: 0x00014CF9
		// (set) Token: 0x06000682 RID: 1666 RVA: 0x00016B01 File Offset: 0x00014D01
		public string NewsLink { get; set; }

		// Token: 0x020000F4 RID: 244
		[JsonConverter(typeof(StringEnumConverter))]
		public enum NewsTypes
		{
			// Token: 0x04000311 RID: 785
			LauncherSingleplayer,
			// Token: 0x04000312 RID: 786
			LauncherMultiplayer,
			// Token: 0x04000313 RID: 787
			MultiplayerLobby
		}
	}
}
