using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x0200038A RID: 906
	public class ClanState : GameState
	{
		// Token: 0x17000C60 RID: 3168
		// (get) Token: 0x0600344C RID: 13388 RVA: 0x000D5F20 File Offset: 0x000D4120
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C61 RID: 3169
		// (get) Token: 0x0600344D RID: 13389 RVA: 0x000D5F23 File Offset: 0x000D4123
		// (set) Token: 0x0600344E RID: 13390 RVA: 0x000D5F2B File Offset: 0x000D412B
		public Hero InitialSelectedHero { get; private set; }

		// Token: 0x17000C62 RID: 3170
		// (get) Token: 0x0600344F RID: 13391 RVA: 0x000D5F34 File Offset: 0x000D4134
		// (set) Token: 0x06003450 RID: 13392 RVA: 0x000D5F3C File Offset: 0x000D413C
		public PartyBase InitialSelectedParty { get; private set; }

		// Token: 0x17000C63 RID: 3171
		// (get) Token: 0x06003451 RID: 13393 RVA: 0x000D5F45 File Offset: 0x000D4145
		// (set) Token: 0x06003452 RID: 13394 RVA: 0x000D5F4D File Offset: 0x000D414D
		public Settlement InitialSelectedSettlement { get; private set; }

		// Token: 0x17000C64 RID: 3172
		// (get) Token: 0x06003453 RID: 13395 RVA: 0x000D5F56 File Offset: 0x000D4156
		// (set) Token: 0x06003454 RID: 13396 RVA: 0x000D5F5E File Offset: 0x000D415E
		public Workshop InitialSelectedWorkshop { get; private set; }

		// Token: 0x17000C65 RID: 3173
		// (get) Token: 0x06003455 RID: 13397 RVA: 0x000D5F67 File Offset: 0x000D4167
		// (set) Token: 0x06003456 RID: 13398 RVA: 0x000D5F6F File Offset: 0x000D416F
		public Alley InitialSelectedAlley { get; private set; }

		// Token: 0x17000C66 RID: 3174
		// (get) Token: 0x06003457 RID: 13399 RVA: 0x000D5F78 File Offset: 0x000D4178
		// (set) Token: 0x06003458 RID: 13400 RVA: 0x000D5F80 File Offset: 0x000D4180
		public IClanStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		// Token: 0x06003459 RID: 13401 RVA: 0x000D5F89 File Offset: 0x000D4189
		public ClanState()
		{
		}

		// Token: 0x0600345A RID: 13402 RVA: 0x000D5F91 File Offset: 0x000D4191
		public ClanState(Hero initialSelectedHero)
		{
			this.InitialSelectedHero = initialSelectedHero;
		}

		// Token: 0x0600345B RID: 13403 RVA: 0x000D5FA0 File Offset: 0x000D41A0
		public ClanState(PartyBase initialSelectedParty)
		{
			this.InitialSelectedParty = initialSelectedParty;
		}

		// Token: 0x0600345C RID: 13404 RVA: 0x000D5FAF File Offset: 0x000D41AF
		public ClanState(Settlement initialSelectedSettlement)
		{
			this.InitialSelectedSettlement = initialSelectedSettlement;
		}

		// Token: 0x0600345D RID: 13405 RVA: 0x000D5FBE File Offset: 0x000D41BE
		public ClanState(Workshop initialSelectedWorkshop)
		{
			this.InitialSelectedWorkshop = initialSelectedWorkshop;
		}

		// Token: 0x0600345E RID: 13406 RVA: 0x000D5FCD File Offset: 0x000D41CD
		public ClanState(Alley initialSelectedAlley)
		{
			this.InitialSelectedAlley = initialSelectedAlley;
		}

		// Token: 0x04000EED RID: 3821
		private IClanStateHandler _handler;
	}
}
