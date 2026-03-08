using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003EE RID: 1006
	public class FindingItemOnMapBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003EB9 RID: 16057 RVA: 0x0011941B File Offset: 0x0011761B
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.DailyTickParty));
		}

		// Token: 0x06003EBA RID: 16058 RVA: 0x00119434 File Offset: 0x00117634
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003EBB RID: 16059 RVA: 0x00119438 File Offset: 0x00117638
		public void DailyTickParty(MobileParty party)
		{
			if (MBRandom.RandomFloat < DefaultPerks.Scouting.BeastWhisperer.PrimaryBonus && party.HasPerk(DefaultPerks.Scouting.BeastWhisperer, false))
			{
				TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
				if (faceTerrainType == TerrainType.Steppe || faceTerrainType == TerrainType.Plain)
				{
					ItemObject randomElementWithPredicate = Items.All.GetRandomElementWithPredicate((ItemObject x) => x.IsMountable && !x.NotMerchandise);
					if (randomElementWithPredicate != null)
					{
						party.ItemRoster.AddToCounts(randomElementWithPredicate, 1);
						if (party.IsMainParty)
						{
							TextObject textObject = new TextObject("{=vl9bawa7}{COUNT} {?(COUNT > 1)}{PLURAL(ANIMAL_NAME)} are{?}{ANIMAL_NAME} is{\\?} added to your party.", null);
							textObject.SetTextVariable("COUNT", 1);
							textObject.SetTextVariable("ANIMAL_NAME", randomElementWithPredicate.Name);
							InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
						}
					}
				}
			}
		}
	}
}
