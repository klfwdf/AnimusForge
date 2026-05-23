using System;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors
{
	// Token: 0x0200046A RID: 1130
	public class TransferPrisonerBarterBehavior : CampaignBehaviorBase
	{
		// Token: 0x060047AA RID: 18346 RVA: 0x0016708C File Offset: 0x0016528C
		public override void RegisterEvents()
		{
			CampaignEvents.BarterablesRequested.AddNonSerializedListener(this, new Action<BarterData>(this.CheckForBarters));
		}

		// Token: 0x060047AB RID: 18347 RVA: 0x001670A5 File Offset: 0x001652A5
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060047AC RID: 18348 RVA: 0x001670A8 File Offset: 0x001652A8
		public void CheckForBarters(BarterData args)
		{
			PartyBase offererParty = args.OffererParty;
			PartyBase otherParty = args.OtherParty;
			if (offererParty != null && otherParty != null)
			{
				foreach (CharacterObject characterObject in offererParty.PrisonerHeroes)
				{
					if (characterObject.IsHero && FactionManager.IsAtWarAgainstFaction(characterObject.HeroObject.MapFaction, otherParty.MapFaction))
					{
						Barterable barterable = new TransferPrisonerBarterable(characterObject.HeroObject, args.OffererHero, args.OffererParty, args.OtherHero, otherParty);
						args.AddBarterable<PrisonerBarterGroup>(barterable, false);
					}
				}
				foreach (CharacterObject characterObject2 in otherParty.PrisonerHeroes)
				{
					if (characterObject2.IsHero && FactionManager.IsAtWarAgainstFaction(characterObject2.HeroObject.MapFaction, offererParty.MapFaction))
					{
						Barterable barterable2 = new TransferPrisonerBarterable(characterObject2.HeroObject, args.OtherHero, args.OtherParty, args.OffererHero, offererParty);
						args.AddBarterable<PrisonerBarterGroup>(barterable2, false);
					}
				}
			}
		}
	}
}
