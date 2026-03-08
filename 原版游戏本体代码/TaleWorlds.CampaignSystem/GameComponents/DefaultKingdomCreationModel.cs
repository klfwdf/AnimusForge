using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000122 RID: 290
	public class DefaultKingdomCreationModel : KingdomCreationModel
	{
		// Token: 0x17000669 RID: 1641
		// (get) Token: 0x06001829 RID: 6185 RVA: 0x00073F24 File Offset: 0x00072124
		public override int MinimumClanTierToCreateKingdom
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x1700066A RID: 1642
		// (get) Token: 0x0600182A RID: 6186 RVA: 0x00073F27 File Offset: 0x00072127
		public override int MinimumNumberOfSettlementsOwnedToCreateKingdom
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x1700066B RID: 1643
		// (get) Token: 0x0600182B RID: 6187 RVA: 0x00073F2A File Offset: 0x0007212A
		public override int MinimumTroopCountToCreateKingdom
		{
			get
			{
				return 100;
			}
		}

		// Token: 0x1700066C RID: 1644
		// (get) Token: 0x0600182C RID: 6188 RVA: 0x00073F2E File Offset: 0x0007212E
		public override int MaximumNumberOfInitialPolicies
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x0600182D RID: 6189 RVA: 0x00073F34 File Offset: 0x00072134
		public override bool IsPlayerKingdomCreationPossible(out List<TextObject> explanations)
		{
			bool result = true;
			explanations = new List<TextObject>();
			if (Hero.MainHero.MapFaction.IsKingdomFaction)
			{
				result = false;
				TextObject item = new TextObject("{=w5b79MmE}Player clan should be independent.", null);
				explanations.Add(item);
			}
			if (Clan.PlayerClan.Tier < this.MinimumClanTierToCreateKingdom)
			{
				result = false;
				TextObject textObject = new TextObject("{=j0UDi2AN}Clan tier should be at least {TIER}.", null);
				textObject.SetTextVariable("TIER", this.MinimumClanTierToCreateKingdom);
				explanations.Add(textObject);
			}
			if (Clan.PlayerClan.Settlements.Count((Settlement t) => t.IsTown || t.IsCastle) < this.MinimumNumberOfSettlementsOwnedToCreateKingdom)
			{
				result = false;
				TextObject textObject2 = new TextObject("{=YsGSgaba}Number of towns or castles you own should be at least {SETTLEMENT_COUNT}.", null);
				textObject2.SetTextVariable("SETTLEMENT_COUNT", this.MinimumNumberOfSettlementsOwnedToCreateKingdom);
				explanations.Add(textObject2);
			}
			if (Clan.PlayerClan.Fiefs.Sum(delegate(Town t)
			{
				MobileParty garrisonParty = t.GarrisonParty;
				int? num;
				if (garrisonParty == null)
				{
					num = null;
				}
				else
				{
					TroopRoster memberRoster = garrisonParty.MemberRoster;
					num = ((memberRoster != null) ? new int?(memberRoster.TotalHealthyCount) : null);
				}
				int? num2 = num;
				if (num2 == null)
				{
					return 0;
				}
				return num2.GetValueOrDefault();
			}) + Clan.PlayerClan.WarPartyComponents.Sum((WarPartyComponent t) => t.MobileParty.MemberRoster.TotalHealthyCount) < this.MinimumTroopCountToCreateKingdom)
			{
				result = false;
				TextObject textObject3 = new TextObject("{=K2txLdOS}You should have at least {TROOP_COUNT} men ready to fight.", null);
				textObject3.SetTextVariable("TROOP_COUNT", this.MinimumTroopCountToCreateKingdom);
				explanations.Add(textObject3);
			}
			return result;
		}

		// Token: 0x0600182E RID: 6190 RVA: 0x0007409C File Offset: 0x0007229C
		public override bool IsPlayerKingdomAbdicationPossible(out List<TextObject> explanations)
		{
			explanations = new List<TextObject>();
			object obj = Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan;
			bool flag = MobileParty.MainParty.MapEvent != null || MobileParty.MainParty.SiegeEvent != null;
			object obj2 = obj;
			bool flag2 = obj2 != null && !Clan.PlayerClan.Kingdom.UnresolvedDecisions.IsEmpty<KingdomDecision>();
			if (obj2 == null)
			{
				explanations.Add(new TextObject("{=s1ERZ4ZR}You must be the king", null));
			}
			if (flag)
			{
				explanations.Add(new TextObject("{=uaMmmhRV}You must conclude your current encounter", null));
			}
			if (flag2)
			{
				explanations.Add(new TextObject("{=etKrpcHe}You must resolve pending decisions", null));
			}
			return obj2 != null && !flag && !flag2;
		}

		// Token: 0x0600182F RID: 6191 RVA: 0x0007415A File Offset: 0x0007235A
		public override IEnumerable<CultureObject> GetAvailablePlayerKingdomCultures()
		{
			List<CultureObject> list = new List<CultureObject>();
			list.Add(Clan.PlayerClan.Culture);
			foreach (Settlement settlement in from t in Clan.PlayerClan.Settlements
				where t.IsTown || t.IsCastle
				select t)
			{
				if (!list.Contains(settlement.Culture))
				{
					list.Add(settlement.Culture);
				}
			}
			foreach (CultureObject cultureObject in list)
			{
				yield return cultureObject;
			}
			List<CultureObject>.Enumerator enumerator2 = default(List<CultureObject>.Enumerator);
			yield break;
			yield break;
		}
	}
}
