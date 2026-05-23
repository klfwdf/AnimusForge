using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000013 RID: 19
	public struct CampaignOptionDisableStatus
	{
		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000D3 RID: 211 RVA: 0x000045F2 File Offset: 0x000027F2
		// (set) Token: 0x060000D4 RID: 212 RVA: 0x000045FA File Offset: 0x000027FA
		public bool IsDisabled { get; private set; }

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000D5 RID: 213 RVA: 0x00004603 File Offset: 0x00002803
		// (set) Token: 0x060000D6 RID: 214 RVA: 0x0000460B File Offset: 0x0000280B
		public string DisabledReason { get; private set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000D7 RID: 215 RVA: 0x00004614 File Offset: 0x00002814
		// (set) Token: 0x060000D8 RID: 216 RVA: 0x0000461C File Offset: 0x0000281C
		public float ValueIfDisabled { get; private set; }

		// Token: 0x060000D9 RID: 217 RVA: 0x00004625 File Offset: 0x00002825
		public CampaignOptionDisableStatus(bool isDisabled, string disabledReason, float valueIfDisabled = -1f)
		{
			this.IsDisabled = isDisabled;
			this.DisabledReason = disabledReason;
			this.ValueIfDisabled = valueIfDisabled;
		}
	}
}
