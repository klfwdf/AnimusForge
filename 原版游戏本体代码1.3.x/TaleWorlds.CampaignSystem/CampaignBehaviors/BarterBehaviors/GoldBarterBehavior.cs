using System;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors
{
	// Token: 0x02000466 RID: 1126
	public class GoldBarterBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600479A RID: 18330 RVA: 0x00166B28 File Offset: 0x00164D28
		public override void RegisterEvents()
		{
			CampaignEvents.BarterablesRequested.AddNonSerializedListener(this, new Action<BarterData>(this.CheckForBarters));
		}

		// Token: 0x0600479B RID: 18331 RVA: 0x00166B41 File Offset: 0x00164D41
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600479C RID: 18332 RVA: 0x00166B44 File Offset: 0x00164D44
		public void CheckForBarters(BarterData args)
		{
			if ((args.OffererHero != null && args.OtherHero != null && args.OffererHero.Clan != args.OtherHero.Clan) || (args.OffererHero == null && args.OffererParty != null) || (args.OtherHero == null && args.OtherParty != null))
			{
				int val = ((args.OffererHero != null) ? args.OffererHero.Gold : args.OffererParty.MobileParty.PartyTradeGold);
				int val2 = ((args.OtherHero != null) ? args.OtherHero.Gold : args.OtherParty.MobileParty.PartyTradeGold);
				Barterable barterable = new GoldBarterable(args.OffererHero, args.OtherHero, args.OffererParty, args.OtherParty, val);
				args.AddBarterable<GoldBarterGroup>(barterable, false);
				Barterable barterable2 = new GoldBarterable(args.OtherHero, args.OffererHero, args.OtherParty, args.OffererParty, val2);
				args.AddBarterable<GoldBarterGroup>(barterable2, false);
			}
		}
	}
}
