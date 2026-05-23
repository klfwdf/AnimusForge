using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x0200002D RID: 45
	public class PlayerRequestUpgradeTroopEvent : EventBase
	{
		// Token: 0x17000147 RID: 327
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x0001B7AA File Offset: 0x000199AA
		// (set) Token: 0x0600049F RID: 1183 RVA: 0x0001B7B2 File Offset: 0x000199B2
		public CharacterObject SourceTroop { get; private set; }

		// Token: 0x17000148 RID: 328
		// (get) Token: 0x060004A0 RID: 1184 RVA: 0x0001B7BB File Offset: 0x000199BB
		// (set) Token: 0x060004A1 RID: 1185 RVA: 0x0001B7C3 File Offset: 0x000199C3
		public CharacterObject TargetTroop { get; private set; }

		// Token: 0x17000149 RID: 329
		// (get) Token: 0x060004A2 RID: 1186 RVA: 0x0001B7CC File Offset: 0x000199CC
		// (set) Token: 0x060004A3 RID: 1187 RVA: 0x0001B7D4 File Offset: 0x000199D4
		public int Number { get; private set; }

		// Token: 0x060004A4 RID: 1188 RVA: 0x0001B7DD File Offset: 0x000199DD
		public PlayerRequestUpgradeTroopEvent(CharacterObject sourceTroop, CharacterObject targetTroop, int num)
		{
			this.SourceTroop = sourceTroop;
			this.TargetTroop = targetTroop;
			this.Number = num;
		}
	}
}
