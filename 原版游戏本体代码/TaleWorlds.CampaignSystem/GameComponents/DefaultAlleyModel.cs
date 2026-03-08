using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000EE RID: 238
	public class DefaultAlleyModel : AlleyModel
	{
		// Token: 0x17000600 RID: 1536
		// (get) Token: 0x060015E9 RID: 5609 RVA: 0x00063236 File Offset: 0x00061436
		private CharacterObject _thug
		{
			get
			{
				return MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");
			}
		}

		// Token: 0x17000601 RID: 1537
		// (get) Token: 0x060015EA RID: 5610 RVA: 0x00063247 File Offset: 0x00061447
		private CharacterObject _expertThug
		{
			get
			{
				return MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
			}
		}

		// Token: 0x17000602 RID: 1538
		// (get) Token: 0x060015EB RID: 5611 RVA: 0x00063258 File Offset: 0x00061458
		private CharacterObject _masterThug
		{
			get
			{
				return MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");
			}
		}

		// Token: 0x17000603 RID: 1539
		// (get) Token: 0x060015EC RID: 5612 RVA: 0x00063269 File Offset: 0x00061469
		public override CampaignTime DestroyAlleyAfterDaysWhenLeaderIsDeath
		{
			get
			{
				return CampaignTime.Days(4f);
			}
		}

		// Token: 0x17000604 RID: 1540
		// (get) Token: 0x060015ED RID: 5613 RVA: 0x00063275 File Offset: 0x00061475
		public override int MinimumTroopCountInPlayerOwnedAlley
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x17000605 RID: 1541
		// (get) Token: 0x060015EE RID: 5614 RVA: 0x00063278 File Offset: 0x00061478
		public override int MaximumTroopCountInPlayerOwnedAlley
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x17000606 RID: 1542
		// (get) Token: 0x060015EF RID: 5615 RVA: 0x0006327C File Offset: 0x0006147C
		public override float GetDailyCrimeRatingOfAlley
		{
			get
			{
				return 0.5f;
			}
		}

		// Token: 0x060015F0 RID: 5616 RVA: 0x00063283 File Offset: 0x00061483
		public override float GetDailyXpGainForAssignedClanMember(Hero assignedHero)
		{
			return 200f;
		}

		// Token: 0x060015F1 RID: 5617 RVA: 0x0006328A File Offset: 0x0006148A
		public override float GetDailyXpGainForMainHero()
		{
			return 40f;
		}

		// Token: 0x060015F2 RID: 5618 RVA: 0x00063291 File Offset: 0x00061491
		public override float GetInitialXpGainForMainHero()
		{
			return 1500f;
		}

		// Token: 0x060015F3 RID: 5619 RVA: 0x00063298 File Offset: 0x00061498
		public override float GetXpGainAfterSuccessfulAlleyDefenseForMainHero()
		{
			return 6000f;
		}

		// Token: 0x060015F4 RID: 5620 RVA: 0x0006329F File Offset: 0x0006149F
		public override TroopRoster GetTroopsOfAIOwnedAlley(Alley alley)
		{
			return this.GetTroopsOfAlleyInternal(alley);
		}

		// Token: 0x060015F5 RID: 5621 RVA: 0x000632A8 File Offset: 0x000614A8
		public override TroopRoster GetTroopsOfAlleyForBattleMission(Alley alley)
		{
			TroopRoster troopsOfAlleyInternal = this.GetTroopsOfAlleyInternal(alley);
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			foreach (TroopRosterElement troopRosterElement in troopsOfAlleyInternal.GetTroopRoster())
			{
				troopRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number * 2, false, 0, 0, true, -1);
			}
			return troopRoster;
		}

		// Token: 0x060015F6 RID: 5622 RVA: 0x0006331C File Offset: 0x0006151C
		private TroopRoster GetTroopsOfAlleyInternal(Alley alley)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			Hero owner = alley.Owner;
			if (owner.Power <= 100f)
			{
				if ((float)owner.RandomValue > 0.5f)
				{
					troopRoster.AddToCounts(this._thug, 3, false, 0, 0, true, -1);
				}
				else
				{
					troopRoster.AddToCounts(this._thug, 2, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._masterThug, 1, false, 0, 0, true, -1);
				}
			}
			else if (owner.Power <= 200f)
			{
				if ((float)owner.RandomValue > 0.5f)
				{
					troopRoster.AddToCounts(this._thug, 2, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._expertThug, 1, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._masterThug, 2, false, 0, 0, true, -1);
				}
				else
				{
					troopRoster.AddToCounts(this._thug, 1, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._expertThug, 2, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._masterThug, 2, false, 0, 0, true, -1);
				}
			}
			else if (owner.Power <= 300f)
			{
				if ((float)owner.RandomValue > 0.5f)
				{
					troopRoster.AddToCounts(this._thug, 3, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._expertThug, 2, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._masterThug, 2, false, 0, 0, true, -1);
				}
				else
				{
					troopRoster.AddToCounts(this._thug, 1, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._expertThug, 3, false, 0, 0, true, -1);
					troopRoster.AddToCounts(this._masterThug, 3, false, 0, 0, true, -1);
				}
			}
			else if ((float)owner.RandomValue > 0.5f)
			{
				troopRoster.AddToCounts(this._thug, 3, false, 0, 0, true, -1);
				troopRoster.AddToCounts(this._expertThug, 3, false, 0, 0, true, -1);
				troopRoster.AddToCounts(this._masterThug, 3, false, 0, 0, true, -1);
			}
			else
			{
				troopRoster.AddToCounts(this._thug, 1, false, 0, 0, true, -1);
				troopRoster.AddToCounts(this._expertThug, 4, false, 0, 0, true, -1);
				troopRoster.AddToCounts(this._masterThug, 4, false, 0, 0, true, -1);
			}
			return troopRoster;
		}

		// Token: 0x060015F7 RID: 5623 RVA: 0x0006354C File Offset: 0x0006174C
		public override List<ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>> GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(Alley alley)
		{
			List<ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>> list = new List<ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>>();
			foreach (Hero hero in Clan.PlayerClan.AliveLords)
			{
				if (hero != Hero.MainHero)
				{
					list.Add(new ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>(hero, this.GetAvailability(alley, hero)));
				}
			}
			foreach (Hero hero2 in Clan.PlayerClan.Companions)
			{
				if (hero2 != Hero.MainHero && !hero2.IsDead)
				{
					list.Add(new ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>(hero2, this.GetAvailability(alley, hero2)));
				}
			}
			return list;
		}

		// Token: 0x060015F8 RID: 5624 RVA: 0x00063624 File Offset: 0x00061824
		public override TroopRoster GetTroopsToRecruitFromAlleyDependingOnAlleyRandom(Alley alley, float random)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			if (random >= 0.5f)
			{
				return troopRoster;
			}
			Clan relatedBanditClanDependingOnAlleySettlementFaction = this.GetRelatedBanditClanDependingOnAlleySettlementFaction(alley);
			if (random > 0.3f)
			{
				troopRoster.AddToCounts(this._thug, 1, false, 0, 0, true, -1);
				troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 1, false, 0, 0, true, -1);
			}
			else if (random > 0.15f)
			{
				troopRoster.AddToCounts(this._thug, 2, false, 0, 0, true, -1);
				troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 1, false, 0, 0, true, -1);
				troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop.UpgradeTargets[0], 1, false, 0, 0, true, -1);
			}
			else if (random > 0.05f)
			{
				troopRoster.AddToCounts(this._thug, 3, false, 0, 0, true, -1);
				troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 2, false, 0, 0, true, -1);
				troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop.UpgradeTargets[0], 1, false, 0, 0, true, -1);
			}
			else
			{
				troopRoster.AddToCounts(this._thug, 2, false, 0, 0, true, -1);
				troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 3, false, 0, 0, true, -1);
				troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop.UpgradeTargets[0], 3, false, 0, 0, true, -1);
			}
			return troopRoster;
		}

		// Token: 0x060015F9 RID: 5625 RVA: 0x00063754 File Offset: 0x00061954
		public override TextObject GetDisabledReasonTextForHero(Hero hero, Alley alley, DefaultAlleyModel.AlleyMemberAvailabilityDetail detail)
		{
			switch (detail)
			{
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available:
				return TextObject.GetEmpty();
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay:
			{
				TextObject textObject = new TextObject("{=dgUF5awO}It will take {HOURS} {?HOURS > 1}hours{?}hour{\\?} for this clan member to arrive.", null);
				textObject.SetTextVariable("HOURS", (int)Math.Ceiling((double)Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, alley.Settlement.Party).ResultNumber));
				return textObject;
			}
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.NotEnoughRoguerySkill:
			{
				TextObject textObject2 = GameTexts.FindText("str_character_role_disabled_tooltip", null);
				textObject2.SetTextVariable("SKILL_NAME", DefaultSkills.Roguery.Name.ToString());
				textObject2.SetTextVariable("MIN_SKILL_AMOUNT", 30);
				return textObject2;
			}
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.NotEnoughMercyTrait:
			{
				TextObject textObject3 = GameTexts.FindText("str_hero_needs_trait_tooltip", null);
				textObject3.SetTextVariable("TRAIT_NAME", DefaultTraits.Mercy.Name.ToString());
				textObject3.SetTextVariable("MAX_TRAIT_AMOUNT", 0);
				return textObject3;
			}
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.CanNotLeadParty:
				return new TextObject("{=qClVr2ka}This hero cannot lead a party.", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.AlreadyAlleyLeader:
				return GameTexts.FindText("str_hero_is_already_alley_leader", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.Prisoner:
				return new TextObject("{=qhRC8XWU}This hero is currently prisoner.", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.SolvingIssue:
				return new TextObject("{=nT6EQGf9}This hero is currently solving an issue.", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.Traveling:
				return new TextObject("{=WECWpVSw}This hero is currently traveling.", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.Busy:
				return new TextObject("{=c9iu5lcc}This hero is currently busy.", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.Fugutive:
				return new TextObject("{=eZYtkDff}This hero is currently fugutive.", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.Governor:
				return new TextObject("{=8NI4wrqU}This hero is currently assigned as a governor.", null);
			case DefaultAlleyModel.AlleyMemberAvailabilityDetail.AlleyUnderAttack:
				return new TextObject("{=pdqi2qz1}You can not do this action while your alley is under attack.", null);
			default:
				return TextObject.GetEmpty();
			}
		}

		// Token: 0x060015FA RID: 5626 RVA: 0x000638C8 File Offset: 0x00061AC8
		public override float GetAlleyAttackResponseTimeInDays(TroopRoster troopRoster)
		{
			float num = 0f;
			foreach (TroopRosterElement troopRosterElement in troopRoster.GetTroopRoster())
			{
				num += (((float)troopRosterElement.Character.Tier > 4f) ? 4f : ((float)troopRosterElement.Character.Tier)) * (float)troopRosterElement.Number;
			}
			return (float)Math.Min(12, 8 + (int)(num / 8f));
		}

		// Token: 0x060015FB RID: 5627 RVA: 0x00063960 File Offset: 0x00061B60
		private Clan GetRelatedBanditClanDependingOnAlleySettlementFaction(Alley alley)
		{
			string stringId = alley.Settlement.Culture.StringId;
			Clan result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits");
			if (stringId == "khuzait")
			{
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "steppe_bandits");
			}
			else if (stringId == "vlandia" || stringId.Contains("empire"))
			{
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits");
			}
			else if (stringId == "aserai")
			{
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "desert_bandits");
			}
			else if (stringId == "battania")
			{
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "forest_bandits");
			}
			else if (stringId == "sturgia" || stringId == "nord")
			{
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "sea_raiders");
			}
			return result;
		}

		// Token: 0x060015FC RID: 5628 RVA: 0x00063AE4 File Offset: 0x00061CE4
		private DefaultAlleyModel.AlleyMemberAvailabilityDetail GetAvailability(Alley alley, Hero hero)
		{
			IAlleyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>();
			if (alley.Owner == Hero.MainHero && campaignBehavior != null && campaignBehavior.GetIsPlayerAlleyUnderAttack(alley))
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.AlleyUnderAttack;
			}
			if (hero.GetSkillValue(DefaultSkills.Roguery) < 30)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.NotEnoughRoguerySkill;
			}
			if (hero.GetTraitLevel(DefaultTraits.Mercy) > 0)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.NotEnoughMercyTrait;
			}
			if (campaignBehavior != null && campaignBehavior.GetAllAssignedClanMembersForOwnedAlleys().Contains(hero))
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.AlreadyAlleyLeader;
			}
			if (hero.GovernorOf != null)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.Governor;
			}
			if (!hero.CanLeadParty())
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.CanNotLeadParty;
			}
			if (Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.SolvingIssue;
			}
			if (hero.IsFugitive)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.Fugutive;
			}
			if (hero.IsTraveling)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.Traveling;
			}
			if (hero.IsPrisoner)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.Prisoner;
			}
			if (!hero.IsActive)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.Busy;
			}
			if (hero.IsPartyLeader)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.Busy;
			}
			if (Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, alley.Settlement.Party).BaseNumber > 0f)
			{
				return DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay;
			}
			return DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available;
		}

		// Token: 0x060015FD RID: 5629 RVA: 0x00063BE1 File Offset: 0x00061DE1
		public override int GetDailyIncomeOfAlley(Alley alley)
		{
			return (int)(alley.Settlement.Town.Prosperity / 50f);
		}

		// Token: 0x04000742 RID: 1858
		private const int BaseResponseTimeInDays = 8;

		// Token: 0x04000743 RID: 1859
		private const int MaxResponseTimeInDays = 12;

		// Token: 0x04000744 RID: 1860
		public const int MinimumRoguerySkillNeededForLeadingAnAlley = 30;

		// Token: 0x04000745 RID: 1861
		public const int MaximumMercyTraitNeededForLeadingAnAlley = 0;

		// Token: 0x02000569 RID: 1385
		public enum AlleyMemberAvailabilityDetail
		{
			// Token: 0x040016FC RID: 5884
			Available,
			// Token: 0x040016FD RID: 5885
			AvailableWithDelay,
			// Token: 0x040016FE RID: 5886
			NotEnoughRoguerySkill,
			// Token: 0x040016FF RID: 5887
			NotEnoughMercyTrait,
			// Token: 0x04001700 RID: 5888
			CanNotLeadParty,
			// Token: 0x04001701 RID: 5889
			AlreadyAlleyLeader,
			// Token: 0x04001702 RID: 5890
			Prisoner,
			// Token: 0x04001703 RID: 5891
			SolvingIssue,
			// Token: 0x04001704 RID: 5892
			Traveling,
			// Token: 0x04001705 RID: 5893
			Busy,
			// Token: 0x04001706 RID: 5894
			Fugutive,
			// Token: 0x04001707 RID: 5895
			Governor,
			// Token: 0x04001708 RID: 5896
			AlleyUnderAttack
		}
	}
}
