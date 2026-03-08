using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000444 RID: 1092
	public class TownSecurityCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060045AC RID: 17836 RVA: 0x0015A6C8 File Offset: 0x001588C8
		public override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.MapEventEnded));
			CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.SiegeEventEnded));
			CampaignEvents.OnHideoutDeactivatedEvent.AddNonSerializedListener(this, new Action<Settlement>(this.OnHideoutDeactivated));
		}

		// Token: 0x060045AD RID: 17837 RVA: 0x0015A71C File Offset: 0x0015891C
		private void OnHideoutDeactivated(Settlement hideout)
		{
			SettlementSecurityModel model = Campaign.Current.Models.SettlementSecurityModel;
			foreach (Settlement settlement in (from t in Settlement.All
				where t.IsTown && t.Position.DistanceSquared(hideout.Position) < model.HideoutClearedSecurityEffectRadius * model.HideoutClearedSecurityEffectRadius
				select t).ToList<Settlement>())
			{
				settlement.Town.Security += (float)model.HideoutClearedSecurityGain;
			}
		}

		// Token: 0x060045AE RID: 17838 RVA: 0x0015A7BC File Offset: 0x001589BC
		private void MapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.IsFieldBattle && mapEvent.HasWinner)
			{
				SettlementSecurityModel model = Campaign.Current.Models.SettlementSecurityModel;
				using (List<Settlement>.Enumerator enumerator = (from t in Settlement.All
					where t.IsTown && t.Position.DistanceSquared(mapEvent.Position) < model.MapEventSecurityEffectRadius * model.MapEventSecurityEffectRadius
					select t).ToList<Settlement>().GetEnumerator())
				{
					Func<PartyBase, bool> <>9__3;
					while (enumerator.MoveNext())
					{
						Settlement town = enumerator.Current;
						if (mapEvent.Winner.Parties.Any((MapEventParty party) => party.Party.IsMobile && party.Party.MobileParty.IsBandit) && mapEvent.InvolvedParties.Any((PartyBase party) => this.ValidCivilianPartyCondition(party, mapEvent, town.MapFaction)))
						{
							float sumOfAttackedPartyStrengths = mapEvent.StrengthOfSide[(int)mapEvent.DefeatedSide];
							town.Town.Security += model.GetLootedNearbyPartySecurityEffect(town.Town, sumOfAttackedPartyStrengths);
						}
						else
						{
							IEnumerable<PartyBase> involvedParties = mapEvent.InvolvedParties;
							Func<PartyBase, bool> predicate;
							if ((predicate = <>9__3) == null)
							{
								predicate = (<>9__3 = (PartyBase party) => this.ValidBanditPartyCondition(party, mapEvent));
							}
							if (involvedParties.Any(predicate))
							{
								float sumOfAttackedPartyStrengths2 = mapEvent.StrengthOfSide[(int)mapEvent.DefeatedSide];
								town.Town.Security += model.GetNearbyBanditPartyDefeatedSecurityEffect(town.Town, sumOfAttackedPartyStrengths2);
							}
						}
					}
				}
			}
		}

		// Token: 0x060045AF RID: 17839 RVA: 0x0015AA24 File Offset: 0x00158C24
		private bool ValidCivilianPartyCondition(PartyBase party, MapEvent mapEvent, IFaction mapFaction)
		{
			return party.IsMobile && ((party.Side != mapEvent.WinningSide && party.MobileParty.IsVillager && DiplomacyHelper.IsSameFactionAndNotEliminated(party.MapFaction, mapFaction)) || (party.MobileParty.IsCaravan && !party.MapFaction.IsAtWarWith(mapFaction)));
		}

		// Token: 0x060045B0 RID: 17840 RVA: 0x0015AA84 File Offset: 0x00158C84
		private bool ValidBanditPartyCondition(PartyBase party, MapEvent mapEvent)
		{
			if (party.Side != mapEvent.WinningSide)
			{
				MobileParty mobileParty = party.MobileParty;
				return mobileParty != null && mobileParty.IsBandit;
			}
			return false;
		}

		// Token: 0x060045B1 RID: 17841 RVA: 0x0015AAA7 File Offset: 0x00158CA7
		private void SiegeEventEnded(SiegeEvent siegeEvent)
		{
		}

		// Token: 0x060045B2 RID: 17842 RVA: 0x0015AAA9 File Offset: 0x00158CA9
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
