using System;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors
{
	// Token: 0x02000465 RID: 1125
	public class FiefBarterBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004796 RID: 18326 RVA: 0x001669C7 File Offset: 0x00164BC7
		public override void RegisterEvents()
		{
			CampaignEvents.BarterablesRequested.AddNonSerializedListener(this, new Action<BarterData>(this.CheckForBarters));
		}

		// Token: 0x06004797 RID: 18327 RVA: 0x001669E0 File Offset: 0x00164BE0
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004798 RID: 18328 RVA: 0x001669E4 File Offset: 0x00164BE4
		public void CheckForBarters(BarterData args)
		{
			if (args.OffererHero != null && args.OtherHero != null && args.OffererHero.GetPerkValue(DefaultPerks.Trade.EverythingHasAPrice) && (!args.OtherHero.Clan.IsMinorFaction || args.OtherHero.Clan == Clan.PlayerClan) && !args.OtherHero.Clan.IsUnderMercenaryService && !args.OffererHero.Clan.IsUnderMercenaryService)
			{
				foreach (Town town in Town.AllFiefs)
				{
					Clan ownerClan = town.OwnerClan;
					if (((ownerClan != null) ? ownerClan.Leader : null) == args.OffererHero)
					{
						Barterable barterable = new FiefBarterable(town.Settlement, args.OffererHero, args.OtherHero);
						args.AddBarterable<FiefBarterGroup>(barterable, false);
					}
					else
					{
						Clan ownerClan2 = town.OwnerClan;
						if (((ownerClan2 != null) ? ownerClan2.Leader : null) == args.OtherHero)
						{
							Barterable barterable2 = new FiefBarterable(town.Settlement, args.OtherHero, args.OffererHero);
							args.AddBarterable<FiefBarterGroup>(barterable2, false);
						}
					}
				}
			}
		}
	}
}
