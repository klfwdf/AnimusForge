using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000395 RID: 917
	public class KingdomState : GameState
	{
		// Token: 0x17000C75 RID: 3189
		// (get) Token: 0x06003491 RID: 13457 RVA: 0x000D6183 File Offset: 0x000D4383
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C76 RID: 3190
		// (get) Token: 0x06003492 RID: 13458 RVA: 0x000D6186 File Offset: 0x000D4386
		// (set) Token: 0x06003493 RID: 13459 RVA: 0x000D618E File Offset: 0x000D438E
		public Army InitialSelectedArmy { get; private set; }

		// Token: 0x17000C77 RID: 3191
		// (get) Token: 0x06003494 RID: 13460 RVA: 0x000D6197 File Offset: 0x000D4397
		// (set) Token: 0x06003495 RID: 13461 RVA: 0x000D619F File Offset: 0x000D439F
		public Settlement InitialSelectedSettlement { get; private set; }

		// Token: 0x17000C78 RID: 3192
		// (get) Token: 0x06003496 RID: 13462 RVA: 0x000D61A8 File Offset: 0x000D43A8
		// (set) Token: 0x06003497 RID: 13463 RVA: 0x000D61B0 File Offset: 0x000D43B0
		public Clan InitialSelectedClan { get; private set; }

		// Token: 0x17000C79 RID: 3193
		// (get) Token: 0x06003498 RID: 13464 RVA: 0x000D61B9 File Offset: 0x000D43B9
		// (set) Token: 0x06003499 RID: 13465 RVA: 0x000D61C1 File Offset: 0x000D43C1
		public PolicyObject InitialSelectedPolicy { get; private set; }

		// Token: 0x17000C7A RID: 3194
		// (get) Token: 0x0600349A RID: 13466 RVA: 0x000D61CA File Offset: 0x000D43CA
		// (set) Token: 0x0600349B RID: 13467 RVA: 0x000D61D2 File Offset: 0x000D43D2
		public Kingdom InitialSelectedKingdom { get; private set; }

		// Token: 0x17000C7B RID: 3195
		// (get) Token: 0x0600349C RID: 13468 RVA: 0x000D61DB File Offset: 0x000D43DB
		// (set) Token: 0x0600349D RID: 13469 RVA: 0x000D61E3 File Offset: 0x000D43E3
		public KingdomDecision InitialSelectedDecision { get; private set; }

		// Token: 0x17000C7C RID: 3196
		// (get) Token: 0x0600349E RID: 13470 RVA: 0x000D61EC File Offset: 0x000D43EC
		// (set) Token: 0x0600349F RID: 13471 RVA: 0x000D61F4 File Offset: 0x000D43F4
		public IKingdomStateHandler Handler
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

		// Token: 0x060034A0 RID: 13472 RVA: 0x000D61FD File Offset: 0x000D43FD
		public KingdomState()
		{
		}

		// Token: 0x060034A1 RID: 13473 RVA: 0x000D6205 File Offset: 0x000D4405
		public KingdomState(KingdomDecision initialSelectedDecision)
		{
			this.InitialSelectedDecision = initialSelectedDecision;
		}

		// Token: 0x060034A2 RID: 13474 RVA: 0x000D6214 File Offset: 0x000D4414
		public KingdomState(Army initialSelectedArmy)
		{
			this.InitialSelectedArmy = initialSelectedArmy;
		}

		// Token: 0x060034A3 RID: 13475 RVA: 0x000D6223 File Offset: 0x000D4423
		public KingdomState(Settlement initialSelectedSettlement)
		{
			this.InitialSelectedSettlement = initialSelectedSettlement;
		}

		// Token: 0x060034A4 RID: 13476 RVA: 0x000D6234 File Offset: 0x000D4434
		public KingdomState(IFaction initialSelectedFaction)
		{
			Clan initialSelectedClan;
			if ((initialSelectedClan = initialSelectedFaction as Clan) != null)
			{
				this.InitialSelectedClan = initialSelectedClan;
				return;
			}
			Kingdom initialSelectedKingdom;
			if ((initialSelectedKingdom = initialSelectedFaction as Kingdom) != null)
			{
				this.InitialSelectedKingdom = initialSelectedKingdom;
			}
		}

		// Token: 0x060034A5 RID: 13477 RVA: 0x000D626A File Offset: 0x000D446A
		public KingdomState(PolicyObject initialSelectedPolicy)
		{
			this.InitialSelectedPolicy = initialSelectedPolicy;
		}

		// Token: 0x04000EFE RID: 3838
		private IKingdomStateHandler _handler;
	}
}
