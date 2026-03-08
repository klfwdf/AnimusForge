using System;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x02000006 RID: 6
	public class GauntletMovieIdentifier
	{
		// Token: 0x0600000F RID: 15 RVA: 0x00002150 File Offset: 0x00000350
		internal GauntletMovieIdentifier(string movieName, ViewModel viewModel)
		{
			this.MovieName = movieName;
			this.DataSource = viewModel;
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000010 RID: 16 RVA: 0x00002166 File Offset: 0x00000366
		// (set) Token: 0x06000011 RID: 17 RVA: 0x0000216E File Offset: 0x0000036E
		public string MovieName { get; internal set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000012 RID: 18 RVA: 0x00002177 File Offset: 0x00000377
		// (set) Token: 0x06000013 RID: 19 RVA: 0x0000217F File Offset: 0x0000037F
		public IGauntletMovie Movie { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000014 RID: 20 RVA: 0x00002188 File Offset: 0x00000388
		// (set) Token: 0x06000015 RID: 21 RVA: 0x00002190 File Offset: 0x00000390
		public ViewModel DataSource { get; set; }
	}
}
