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

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultAlleyModel : AlleyModel
{
	public enum AlleyMemberAvailabilityDetail
	{
		Available,
		AvailableWithDelay,
		NotEnoughRoguerySkill,
		NotEnoughMercyTrait,
		CanNotLeadParty,
		AlreadyAlleyLeader,
		Prisoner,
		SolvingIssue,
		Traveling,
		Busy,
		Fugutive,
		Governor,
		AlleyUnderAttack
	}

	private const int BaseResponseTimeInDays = 8;

	private const int MaxResponseTimeInDays = 12;

	public const int MinimumRoguerySkillNeededForLeadingAnAlley = 30;

	public const int MaximumMercyTraitNeededForLeadingAnAlley = 0;

	private CharacterObject _thug => MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");

	private CharacterObject _expertThug => MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");

	private CharacterObject _masterThug => MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");

	public override CampaignTime DestroyAlleyAfterDaysWhenLeaderIsDeath => CampaignTime.Days(4f);

	public override int MinimumTroopCountInPlayerOwnedAlley => 5;

	public override int MaximumTroopCountInPlayerOwnedAlley => 10;

	public override float GetDailyCrimeRatingOfAlley => 0.5f;

	public override float GetDailyXpGainForAssignedClanMember(Hero assignedHero)
	{
		return 200f;
	}

	public override float GetDailyXpGainForMainHero()
	{
		return 40f;
	}

	public override float GetInitialXpGainForMainHero()
	{
		return 1500f;
	}

	public override float GetXpGainAfterSuccessfulAlleyDefenseForMainHero()
	{
		return 6000f;
	}

	public override TroopRoster GetTroopsOfAIOwnedAlley(Alley alley)
	{
		return GetTroopsOfAlleyInternal(alley);
	}

	public override TroopRoster GetTroopsOfAlleyForBattleMission(Alley alley)
	{
		TroopRoster troopsOfAlleyInternal = GetTroopsOfAlleyInternal(alley);
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopRosterElement item in troopsOfAlleyInternal.GetTroopRoster())
		{
			troopRoster.AddToCounts(item.Character, item.Number * 2);
		}
		return troopRoster;
	}

	private TroopRoster GetTroopsOfAlleyInternal(Alley alley)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		Hero owner = alley.Owner;
		if (owner.Power <= 100f)
		{
			if ((float)owner.RandomValue > 0.5f)
			{
				troopRoster.AddToCounts(_thug, 3);
			}
			else
			{
				troopRoster.AddToCounts(_thug, 2);
				troopRoster.AddToCounts(_masterThug, 1);
			}
		}
		else if (owner.Power <= 200f)
		{
			if ((float)owner.RandomValue > 0.5f)
			{
				troopRoster.AddToCounts(_thug, 2);
				troopRoster.AddToCounts(_expertThug, 1);
				troopRoster.AddToCounts(_masterThug, 2);
			}
			else
			{
				troopRoster.AddToCounts(_thug, 1);
				troopRoster.AddToCounts(_expertThug, 2);
				troopRoster.AddToCounts(_masterThug, 2);
			}
		}
		else if (owner.Power <= 300f)
		{
			if ((float)owner.RandomValue > 0.5f)
			{
				troopRoster.AddToCounts(_thug, 3);
				troopRoster.AddToCounts(_expertThug, 2);
				troopRoster.AddToCounts(_masterThug, 2);
			}
			else
			{
				troopRoster.AddToCounts(_thug, 1);
				troopRoster.AddToCounts(_expertThug, 3);
				troopRoster.AddToCounts(_masterThug, 3);
			}
		}
		else if ((float)owner.RandomValue > 0.5f)
		{
			troopRoster.AddToCounts(_thug, 3);
			troopRoster.AddToCounts(_expertThug, 3);
			troopRoster.AddToCounts(_masterThug, 3);
		}
		else
		{
			troopRoster.AddToCounts(_thug, 1);
			troopRoster.AddToCounts(_expertThug, 4);
			troopRoster.AddToCounts(_masterThug, 4);
		}
		return troopRoster;
	}

	public override List<(Hero, AlleyMemberAvailabilityDetail)> GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(Alley alley)
	{
		List<(Hero, AlleyMemberAvailabilityDetail)> list = new List<(Hero, AlleyMemberAvailabilityDetail)>();
		foreach (Hero aliveLord in Clan.PlayerClan.AliveLords)
		{
			if (aliveLord != Hero.MainHero)
			{
				list.Add((aliveLord, GetAvailability(alley, aliveLord)));
			}
		}
		foreach (Hero companion in Clan.PlayerClan.Companions)
		{
			if (companion != Hero.MainHero && !companion.IsDead)
			{
				list.Add((companion, GetAvailability(alley, companion)));
			}
		}
		return list;
	}

	public override TroopRoster GetTroopsToRecruitFromAlleyDependingOnAlleyRandom(Alley alley, float random)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		if (random >= 0.5f)
		{
			return troopRoster;
		}
		Clan relatedBanditClanDependingOnAlleySettlementFaction = GetRelatedBanditClanDependingOnAlleySettlementFaction(alley);
		if (random > 0.3f)
		{
			troopRoster.AddToCounts(_thug, 1);
			troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 1);
		}
		else if (random > 0.15f)
		{
			troopRoster.AddToCounts(_thug, 2);
			troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 1);
			troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop.UpgradeTargets[0], 1);
		}
		else if (random > 0.05f)
		{
			troopRoster.AddToCounts(_thug, 3);
			troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 2);
			troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop.UpgradeTargets[0], 1);
		}
		else
		{
			troopRoster.AddToCounts(_thug, 2);
			troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop, 3);
			troopRoster.AddToCounts(relatedBanditClanDependingOnAlleySettlementFaction.BasicTroop.UpgradeTargets[0], 3);
		}
		return troopRoster;
	}

	public override TextObject GetDisabledReasonTextForHero(Hero hero, Alley alley, AlleyMemberAvailabilityDetail detail)
	{
		switch (detail)
		{
		case AlleyMemberAvailabilityDetail.Available:
			return TextObject.GetEmpty();
		case AlleyMemberAvailabilityDetail.AvailableWithDelay:
		{
			TextObject textObject3 = new TextObject("{=dgUF5awO}It will take {HOURS} {?HOURS > 1}hours{?}hour{\\?} for this clan member to arrive.");
			textObject3.SetTextVariable("HOURS", (int)Math.Ceiling(Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, alley.Settlement.Party).ResultNumber));
			return textObject3;
		}
		case AlleyMemberAvailabilityDetail.NotEnoughRoguerySkill:
		{
			TextObject textObject2 = GameTexts.FindText("str_character_role_disabled_tooltip");
			textObject2.SetTextVariable("SKILL_NAME", DefaultSkills.Roguery.Name.ToString());
			textObject2.SetTextVariable("MIN_SKILL_AMOUNT", 30);
			return textObject2;
		}
		case AlleyMemberAvailabilityDetail.NotEnoughMercyTrait:
		{
			TextObject textObject = GameTexts.FindText("str_hero_needs_trait_tooltip");
			textObject.SetTextVariable("TRAIT_NAME", DefaultTraits.Mercy.Name.ToString());
			textObject.SetTextVariable("MAX_TRAIT_AMOUNT", 0);
			return textObject;
		}
		case AlleyMemberAvailabilityDetail.AlreadyAlleyLeader:
			return GameTexts.FindText("str_hero_is_already_alley_leader");
		case AlleyMemberAvailabilityDetail.CanNotLeadParty:
			return new TextObject("{=qClVr2ka}This hero cannot lead a party.");
		case AlleyMemberAvailabilityDetail.Prisoner:
			return new TextObject("{=qhRC8XWU}This hero is currently prisoner.");
		case AlleyMemberAvailabilityDetail.SolvingIssue:
			return new TextObject("{=nT6EQGf9}This hero is currently solving an issue.");
		case AlleyMemberAvailabilityDetail.Traveling:
			return new TextObject("{=WECWpVSw}This hero is currently traveling.");
		case AlleyMemberAvailabilityDetail.Busy:
			return new TextObject("{=c9iu5lcc}This hero is currently busy.");
		case AlleyMemberAvailabilityDetail.Fugutive:
			return new TextObject("{=eZYtkDff}This hero is currently fugutive.");
		case AlleyMemberAvailabilityDetail.Governor:
			return new TextObject("{=8NI4wrqU}This hero is currently assigned as a governor.");
		case AlleyMemberAvailabilityDetail.AlleyUnderAttack:
			return new TextObject("{=pdqi2qz1}You can not do this action while your alley is under attack.");
		default:
			return TextObject.GetEmpty();
		}
	}

	public override float GetAlleyAttackResponseTimeInDays(TroopRoster troopRoster)
	{
		float num = 0f;
		foreach (TroopRosterElement item in troopRoster.GetTroopRoster())
		{
			num += (((float)item.Character.Tier > 4f) ? 4f : ((float)item.Character.Tier)) * (float)item.Number;
		}
		return Math.Min(12, 8 + (int)(num / 8f));
	}

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
		else
		{
			switch (stringId)
			{
			case "aserai":
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "desert_bandits");
				break;
			case "battania":
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "forest_bandits");
				break;
			case "sturgia":
			case "nord":
				result = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "sea_raiders");
				break;
			}
		}
		return result;
	}

	private AlleyMemberAvailabilityDetail GetAvailability(Alley alley, Hero hero)
	{
		IAlleyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>();
		if (alley.Owner == Hero.MainHero && campaignBehavior != null && campaignBehavior.GetIsPlayerAlleyUnderAttack(alley))
		{
			return AlleyMemberAvailabilityDetail.AlleyUnderAttack;
		}
		if (hero.GetSkillValue(DefaultSkills.Roguery) < 30)
		{
			return AlleyMemberAvailabilityDetail.NotEnoughRoguerySkill;
		}
		if (hero.GetTraitLevel(DefaultTraits.Mercy) > 0)
		{
			return AlleyMemberAvailabilityDetail.NotEnoughMercyTrait;
		}
		if (campaignBehavior != null && campaignBehavior.GetAllAssignedClanMembersForOwnedAlleys().Contains(hero))
		{
			return AlleyMemberAvailabilityDetail.AlreadyAlleyLeader;
		}
		if (hero.GovernorOf != null)
		{
			return AlleyMemberAvailabilityDetail.Governor;
		}
		if (!hero.CanLeadParty())
		{
			return AlleyMemberAvailabilityDetail.CanNotLeadParty;
		}
		if (Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
		{
			return AlleyMemberAvailabilityDetail.SolvingIssue;
		}
		if (hero.IsFugitive)
		{
			return AlleyMemberAvailabilityDetail.Fugutive;
		}
		if (hero.IsTraveling)
		{
			return AlleyMemberAvailabilityDetail.Traveling;
		}
		if (hero.IsPrisoner)
		{
			return AlleyMemberAvailabilityDetail.Prisoner;
		}
		if (!hero.IsActive)
		{
			return AlleyMemberAvailabilityDetail.Busy;
		}
		if (hero.IsPartyLeader)
		{
			return AlleyMemberAvailabilityDetail.Busy;
		}
		if (Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, alley.Settlement.Party).BaseNumber > 0f)
		{
			return AlleyMemberAvailabilityDetail.AvailableWithDelay;
		}
		return AlleyMemberAvailabilityDetail.Available;
	}

	public override int GetDailyIncomeOfAlley(Alley alley)
	{
		return (int)(alley.Settlement.Town.Prosperity / 50f);
	}
}
