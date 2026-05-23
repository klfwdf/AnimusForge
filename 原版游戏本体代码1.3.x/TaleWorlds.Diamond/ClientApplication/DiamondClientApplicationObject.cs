using System;
using TaleWorlds.Library;

namespace TaleWorlds.Diamond.ClientApplication
{
	// Token: 0x02000045 RID: 69
	public abstract class DiamondClientApplicationObject
	{
		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060001A5 RID: 421 RVA: 0x0000563C File Offset: 0x0000383C
		public DiamondClientApplication Application
		{
			get
			{
				return this._application;
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x00005644 File Offset: 0x00003844
		public ApplicationVersion ApplicationVersion
		{
			get
			{
				return this.Application.ApplicationVersion;
			}
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00005651 File Offset: 0x00003851
		protected DiamondClientApplicationObject(DiamondClientApplication application)
		{
			this._application = application;
		}

		// Token: 0x04000096 RID: 150
		private DiamondClientApplication _application;
	}
}
