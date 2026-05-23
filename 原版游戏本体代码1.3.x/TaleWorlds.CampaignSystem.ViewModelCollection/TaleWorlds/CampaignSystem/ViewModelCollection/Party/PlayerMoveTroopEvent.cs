using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x0200002F RID: 47
	public class PlayerMoveTroopEvent : EventBase
	{
		// Token: 0x1700014B RID: 331
		// (get) Token: 0x060004A8 RID: 1192 RVA: 0x0001B81A File Offset: 0x00019A1A
		// (set) Token: 0x060004A9 RID: 1193 RVA: 0x0001B822 File Offset: 0x00019A22
		public CharacterObject Troop { get; private set; }

		// Token: 0x1700014C RID: 332
		// (get) Token: 0x060004AA RID: 1194 RVA: 0x0001B82B File Offset: 0x00019A2B
		// (set) Token: 0x060004AB RID: 1195 RVA: 0x0001B833 File Offset: 0x00019A33
		public int Amount { get; private set; }

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x060004AC RID: 1196 RVA: 0x0001B83C File Offset: 0x00019A3C
		// (set) Token: 0x060004AD RID: 1197 RVA: 0x0001B844 File Offset: 0x00019A44
		public bool IsPrisoner { get; private set; }

		// Token: 0x1700014E RID: 334
		// (get) Token: 0x060004AE RID: 1198 RVA: 0x0001B84D File Offset: 0x00019A4D
		// (set) Token: 0x060004AF RID: 1199 RVA: 0x0001B855 File Offset: 0x00019A55
		public PartyScreenLogic.PartyRosterSide FromSide { get; private set; }

		// Token: 0x1700014F RID: 335
		// (get) Token: 0x060004B0 RID: 1200 RVA: 0x0001B85E File Offset: 0x00019A5E
		// (set) Token: 0x060004B1 RID: 1201 RVA: 0x0001B866 File Offset: 0x00019A66
		public PartyScreenLogic.PartyRosterSide ToSide { get; private set; }

		// Token: 0x060004B2 RID: 1202 RVA: 0x0001B86F File Offset: 0x00019A6F
		public PlayerMoveTroopEvent(CharacterObject troop, PartyScreenLogic.PartyRosterSide fromSide, PartyScreenLogic.PartyRosterSide toSide, int amount, bool isPrisoner)
		{
			this.Troop = troop;
			this.FromSide = fromSide;
			this.ToSide = toSide;
			this.IsPrisoner = isPrisoner;
			this.Amount = amount;
		}
	}
}
