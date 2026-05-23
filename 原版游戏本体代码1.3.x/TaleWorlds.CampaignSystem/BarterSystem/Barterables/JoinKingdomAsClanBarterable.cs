using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables
{
	// Token: 0x02000482 RID: 1154
	public class JoinKingdomAsClanBarterable : Barterable
	{
		// Token: 0x17000E64 RID: 3684
		// (get) Token: 0x0600488E RID: 18574 RVA: 0x0016DA0A File Offset: 0x0016BC0A
		public override string StringID
		{
			get
			{
				return "join_faction_barterable";
			}
		}

		// Token: 0x0600488F RID: 18575 RVA: 0x0016DA11 File Offset: 0x0016BC11
		public JoinKingdomAsClanBarterable(Hero owner, Kingdom targetKingdom, bool isDefecting = false)
			: base(owner, null)
		{
			this.TargetKingdom = targetKingdom;
			this.IsDefecting = isDefecting;
		}

		// Token: 0x17000E65 RID: 3685
		// (get) Token: 0x06004890 RID: 18576 RVA: 0x0016DA29 File Offset: 0x0016BC29
		public override TextObject Name
		{
			get
			{
				TextObject textObject = new TextObject("{=8Az4q2wp}Join {FACTION}", null);
				textObject.SetTextVariable("FACTION", this.TargetKingdom.Name);
				return textObject;
			}
		}

		// Token: 0x06004891 RID: 18577 RVA: 0x0016DA50 File Offset: 0x0016BC50
		public override int GetUnitValueForFaction(IFaction factionForEvaluation)
		{
			float num = -1000000f;
			if (factionForEvaluation == base.OriginalOwner.Clan)
			{
				num = Campaign.Current.Models.DiplomacyModel.GetScoreOfClanToJoinKingdom(base.OriginalOwner.Clan, this.TargetKingdom);
				if (base.OriginalOwner.Clan.Kingdom != null)
				{
					int valueForFaction = new LeaveKingdomAsClanBarterable(base.OriginalOwner, base.OriginalParty).GetValueForFaction(factionForEvaluation);
					if (!this.TargetKingdom.IsAtWarWith(base.OriginalOwner.Clan.Kingdom))
					{
						float num2 = base.OriginalOwner.Clan.CalculateTotalSettlementValueForFaction(base.OriginalOwner.Clan.Kingdom);
						num -= num2 * ((this.TargetKingdom.Leader == Hero.MainHero) ? 0.5f : 1f);
					}
					num += (float)valueForFaction;
				}
			}
			else if (factionForEvaluation.MapFaction == this.TargetKingdom)
			{
				num = Campaign.Current.Models.DiplomacyModel.GetScoreOfKingdomToGetClan(this.TargetKingdom, base.OriginalOwner.Clan);
			}
			if (this.TargetKingdom == Clan.PlayerClan.Kingdom && Hero.MainHero.GetPerkValue(DefaultPerks.Trade.SilverTongue))
			{
				num += num * DefaultPerks.Trade.SilverTongue.PrimaryBonus;
			}
			return (int)num;
		}

		// Token: 0x06004892 RID: 18578 RVA: 0x0016DB9D File Offset: 0x0016BD9D
		public override void CheckBarterLink(Barterable linkedBarterable)
		{
		}

		// Token: 0x06004893 RID: 18579 RVA: 0x0016DBA0 File Offset: 0x0016BDA0
		public override bool IsCompatible(Barterable barterable)
		{
			LeaveKingdomAsClanBarterable leaveKingdomAsClanBarterable = barterable as LeaveKingdomAsClanBarterable;
			return leaveKingdomAsClanBarterable == null || leaveKingdomAsClanBarterable.OriginalOwner.MapFaction != this.TargetKingdom;
		}

		// Token: 0x06004894 RID: 18580 RVA: 0x0016DBCF File Offset: 0x0016BDCF
		public override ImageIdentifier GetVisualIdentifier()
		{
			return new BannerImageIdentifier(this.TargetKingdom.Banner, false);
		}

		// Token: 0x06004895 RID: 18581 RVA: 0x0016DBE2 File Offset: 0x0016BDE2
		public override string GetEncyclopediaLink()
		{
			return this.TargetKingdom.EncyclopediaLink;
		}

		// Token: 0x06004896 RID: 18582 RVA: 0x0016DBF0 File Offset: 0x0016BDF0
		public override void Apply()
		{
			if (this.TargetKingdom != null && this.TargetKingdom != null && this.TargetKingdom.Leader == Hero.MainHero)
			{
				int valueForFaction = base.GetValueForFaction(base.OriginalOwner.Clan);
				int relation = ((valueForFaction < 0) ? (20 - valueForFaction / 20000) : 20);
				ChangeRelationAction.ApplyPlayerRelation(base.OriginalOwner.Clan.Leader, relation, true, true);
				if (base.OriginalOwner.Clan.MapFaction != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(base.OriginalOwner.Clan.Leader, base.OriginalOwner.Clan.MapFaction.Leader, -100, true);
				}
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender && PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Attacker)
			{
				PlayerEncounter.Current.SetPlayerSiegeInterruptedByEnemyDefection();
			}
			bool flag = base.OriginalOwner.Clan.IsMinorFaction && base.OriginalOwner.Clan != Clan.PlayerClan;
			Kingdom kingdom = base.OriginalOwner.Clan.Kingdom;
			if (base.OriginalOwner.Clan.Kingdom != null)
			{
				if (flag)
				{
					ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(base.OriginalOwner.Clan, true);
				}
				else if (base.OriginalOwner.Clan.Kingdom != null && this.TargetKingdom != null && base.OriginalOwner.Clan.Kingdom.IsAtWarWith(this.TargetKingdom))
				{
					ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(base.OriginalOwner.Clan, true);
				}
				else
				{
					ChangeKingdomAction.ApplyByLeaveKingdom(base.OriginalOwner.Clan, true);
				}
			}
			if (flag)
			{
				ChangeKingdomAction.ApplyByJoinFactionAsMercenary(base.OriginalOwner.Clan, this.TargetKingdom, default(CampaignTime), Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(base.OriginalOwner.Clan, this.TargetKingdom, false), true);
				return;
			}
			if (this.IsDefecting)
			{
				ChangeKingdomAction.ApplyByJoinToKingdomByDefection(base.OriginalOwner.Clan, kingdom, this.TargetKingdom, default(CampaignTime), true);
				return;
			}
			ChangeKingdomAction.ApplyByJoinToKingdom(base.OriginalOwner.Clan, this.TargetKingdom, default(CampaignTime), true);
		}

		// Token: 0x06004897 RID: 18583 RVA: 0x0016DE23 File Offset: 0x0016C023
		internal static void AutoGeneratedStaticCollectObjectsJoinKingdomAsClanBarterable(object o, List<object> collectedObjects)
		{
			((JoinKingdomAsClanBarterable)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x06004898 RID: 18584 RVA: 0x0016DE31 File Offset: 0x0016C031
		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x04001405 RID: 5125
		public readonly Kingdom TargetKingdom;

		// Token: 0x04001406 RID: 5126
		public readonly bool IsDefecting;
	}
}
