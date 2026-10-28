using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTournamentModel : TournamentModel
{
	public override TournamentGame CreateTournament(Town town)
	{
		return new FightTournamentGame(town);
	}

	public override float GetTournamentStartChance(Town town)
	{
		if (town.Settlement.SiegeEvent != null)
		{
			return 0f;
		}
		if (Math.Abs(town.StringId.GetHashCode() % 3) != CampaignTime.Now.GetWeekOfSeason)
		{
			return 0f;
		}
		return 0.1f * (float)(town.Settlement.Parties.Count((MobileParty x) => x.IsLordParty) + town.Settlement.HeroesWithoutParty.Count((Hero x) => SuitableForTournament(x))) - 0.2f;
	}

	public override int GetNumLeaderboardVictoriesAtGameStart()
	{
		return 500;
	}

	public override float GetTournamentEndChance(TournamentGame tournament)
	{
		float elapsedDaysUntilNow = tournament.CreationTime.ElapsedDaysUntilNow;
		return TaleWorlds.Library.MathF.Max(0f, (elapsedDaysUntilNow - 10f) * 0.05f);
	}

	private bool SuitableForTournament(Hero hero)
	{
		if (hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			return TaleWorlds.Library.MathF.Max(hero.GetSkillValue(DefaultSkills.OneHanded), hero.GetSkillValue(DefaultSkills.TwoHanded)) > 100;
		}
		return false;
	}

	public override float GetTournamentSimulationScore(CharacterObject character)
	{
		return (character.IsHero ? 1f : 0.4f) * (TaleWorlds.Library.MathF.Max(character.GetSkillValue(DefaultSkills.OneHanded), character.GetSkillValue(DefaultSkills.TwoHanded), character.GetSkillValue(DefaultSkills.Polearm)) + (float)character.GetSkillValue(DefaultSkills.Athletics) + (float)character.GetSkillValue(DefaultSkills.Riding)) * 0.01f;
	}

	public override int GetRenownReward(Hero winner, Town town)
	{
		float num = 3f;
		if (winner.GetPerkValue(DefaultPerks.OneHanded.Duelist))
		{
			num *= DefaultPerks.OneHanded.Duelist.SecondaryBonus;
		}
		if (winner.GetPerkValue(DefaultPerks.Charm.SelfPromoter))
		{
			num += DefaultPerks.Charm.SelfPromoter.PrimaryBonus;
		}
		return TaleWorlds.Library.MathF.Round(num);
	}

	public override int GetInfluenceReward(Hero winner, Town town)
	{
		return 0;
	}

	public override (SkillObject skill, int xp) GetSkillXpGainFromTournament(Town town)
	{
		float randomFloat = MBRandom.RandomFloat;
		SkillObject item = ((randomFloat < 0.2f) ? DefaultSkills.OneHanded : ((randomFloat < 0.4f) ? DefaultSkills.TwoHanded : ((randomFloat < 0.6f) ? DefaultSkills.Polearm : ((randomFloat < 0.8f) ? DefaultSkills.Riding : DefaultSkills.Athletics))));
		int item2 = 500;
		return (skill: item, xp: item2);
	}

	public override Equipment GetParticipantArmor(CharacterObject participant)
	{
		if (CampaignMission.Current != null && CampaignMission.Current.Mode != MissionMode.Tournament && Settlement.CurrentSettlement != null)
		{
			return (Game.Current.ObjectManager.GetObject<CharacterObject>("gear_practice_dummy_" + Settlement.CurrentSettlement.MapFaction.Culture.StringId) ?? Game.Current.ObjectManager.GetObject<CharacterObject>("gear_practice_dummy_empire")).RandomBattleEquipment;
		}
		return participant.RandomBattleEquipment;
	}

	public override MBList<ItemObject> GetRegularRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
	{
		MBList<ItemObject> mBList = new MBList<ItemObject>();
		MBList<ItemObject> mBList2 = new MBList<ItemObject>();
		foreach (ItemObject item in Items.All)
		{
			if (item.Value > regularRewardMinValue && item.Value < regularRewardMaxValue && !item.NotMerchandise && (item.IsCraftedWeapon || item.IsMountable || item.ArmorComponent != null) && !item.IsCraftedByPlayer)
			{
				if (item.Culture == town.Culture)
				{
					mBList.Add(item);
				}
				else
				{
					mBList2.Add(item);
				}
			}
		}
		foreach (ItemObject possibleRewardBannerItem in Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems())
		{
			if (possibleRewardBannerItem.BannerComponent.BannerLevel == 1 || possibleRewardBannerItem.BannerComponent.BannerLevel == 2)
			{
				mBList.Add(possibleRewardBannerItem);
			}
		}
		if (mBList.IsEmpty())
		{
			mBList.AddRange(mBList2);
		}
		return mBList;
	}

	public override MBList<ItemObject> GetEliteRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
	{
		MBList<ItemObject> mBList = new MBList<ItemObject>();
		string[] array = new string[31]
		{
			"winds_fury_sword_t3", "bone_crusher_mace_t3", "tyrhung_sword_t3", "pernach_mace_t3", "early_retirement_2hsword_t3", "black_heart_2haxe_t3", "knights_fall_mace_t3", "the_scalpel_sword_t3", "judgement_mace_t3", "dawnbreaker_sword_t3",
			"ambassador_sword_t3", "heavy_nasalhelm_over_imperial_mail", "sturgian_helmet_closed", "full_helm_over_laced_coif", "desert_mail_coif", "heavy_nasalhelm_over_imperial_mail", "plumed_nomad_helmet", "ridged_northernhelm", "noble_horse_southern", "noble_horse_imperial",
			"noble_horse_western", "noble_horse_eastern", "noble_horse_battania", "noble_horse_northern", "special_camel", "western_crowned_helmet", "northern_warlord_helmet", "battania_warlord_pauldrons", "aserai_armor_02_b", "white_coat_over_mail",
			"spiked_helmet_with_facemask"
		};
		foreach (string objectName in array)
		{
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(objectName);
			if (itemObject != null)
			{
				mBList.Add(itemObject);
			}
		}
		return mBList;
	}
}
