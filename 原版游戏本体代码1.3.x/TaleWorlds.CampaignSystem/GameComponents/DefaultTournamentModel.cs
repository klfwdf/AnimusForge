using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200015B RID: 347
	public class DefaultTournamentModel : TournamentModel
	{
		// Token: 0x06001A93 RID: 6803 RVA: 0x00087B43 File Offset: 0x00085D43
		public override TournamentGame CreateTournament(Town town)
		{
			return new FightTournamentGame(town);
		}

		// Token: 0x06001A94 RID: 6804 RVA: 0x00087B4C File Offset: 0x00085D4C
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
			return 0.1f * (float)(town.Settlement.Parties.Count((MobileParty x) => x.IsLordParty) + town.Settlement.HeroesWithoutParty.Count((Hero x) => this.SuitableForTournament(x))) - 0.2f;
		}

		// Token: 0x06001A95 RID: 6805 RVA: 0x00087BEC File Offset: 0x00085DEC
		public override int GetNumLeaderboardVictoriesAtGameStart()
		{
			return 500;
		}

		// Token: 0x06001A96 RID: 6806 RVA: 0x00087BF4 File Offset: 0x00085DF4
		public override float GetTournamentEndChance(TournamentGame tournament)
		{
			float elapsedDaysUntilNow = tournament.CreationTime.ElapsedDaysUntilNow;
			return MathF.Max(0f, (elapsedDaysUntilNow - 10f) * 0.05f);
		}

		// Token: 0x06001A97 RID: 6807 RVA: 0x00087C27 File Offset: 0x00085E27
		private bool SuitableForTournament(Hero hero)
		{
			return hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && MathF.Max(hero.GetSkillValue(DefaultSkills.OneHanded), hero.GetSkillValue(DefaultSkills.TwoHanded)) > 100;
		}

		// Token: 0x06001A98 RID: 6808 RVA: 0x00087C68 File Offset: 0x00085E68
		public override float GetTournamentSimulationScore(CharacterObject character)
		{
			return (character.IsHero ? 1f : 0.4f) * (MathF.Max((float)character.GetSkillValue(DefaultSkills.OneHanded), (float)character.GetSkillValue(DefaultSkills.TwoHanded), (float)character.GetSkillValue(DefaultSkills.Polearm)) + (float)character.GetSkillValue(DefaultSkills.Athletics) + (float)character.GetSkillValue(DefaultSkills.Riding)) * 0.01f;
		}

		// Token: 0x06001A99 RID: 6809 RVA: 0x00087CD4 File Offset: 0x00085ED4
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
			return MathF.Round(num);
		}

		// Token: 0x06001A9A RID: 6810 RVA: 0x00087D21 File Offset: 0x00085F21
		public override int GetInfluenceReward(Hero winner, Town town)
		{
			return 0;
		}

		// Token: 0x06001A9B RID: 6811 RVA: 0x00087D24 File Offset: 0x00085F24
		[return: TupleElementNames(new string[] { "skill", "xp" })]
		public override ValueTuple<SkillObject, int> GetSkillXpGainFromTournament(Town town)
		{
			float randomFloat = MBRandom.RandomFloat;
			SkillObject item = ((randomFloat < 0.2f) ? DefaultSkills.OneHanded : ((randomFloat < 0.4f) ? DefaultSkills.TwoHanded : ((randomFloat < 0.6f) ? DefaultSkills.Polearm : ((randomFloat < 0.8f) ? DefaultSkills.Riding : DefaultSkills.Athletics))));
			int item2 = 500;
			return new ValueTuple<SkillObject, int>(item, item2);
		}

		// Token: 0x06001A9C RID: 6812 RVA: 0x00087D84 File Offset: 0x00085F84
		public override Equipment GetParticipantArmor(CharacterObject participant)
		{
			if (CampaignMission.Current != null && CampaignMission.Current.Mode != MissionMode.Tournament && Settlement.CurrentSettlement != null)
			{
				return (Game.Current.ObjectManager.GetObject<CharacterObject>("gear_practice_dummy_" + Settlement.CurrentSettlement.MapFaction.Culture.StringId) ?? Game.Current.ObjectManager.GetObject<CharacterObject>("gear_practice_dummy_empire")).RandomBattleEquipment;
			}
			return participant.RandomBattleEquipment;
		}

		// Token: 0x06001A9D RID: 6813 RVA: 0x00087E00 File Offset: 0x00086000
		public override MBList<ItemObject> GetRegularRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
		{
			MBList<ItemObject> mblist = new MBList<ItemObject>();
			MBList<ItemObject> mblist2 = new MBList<ItemObject>();
			foreach (ItemObject itemObject in Items.All)
			{
				if (itemObject.Value > regularRewardMinValue && itemObject.Value < regularRewardMaxValue && !itemObject.NotMerchandise && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer)
				{
					if (itemObject.Culture == town.Culture)
					{
						mblist.Add(itemObject);
					}
					else
					{
						mblist2.Add(itemObject);
					}
				}
			}
			foreach (ItemObject itemObject2 in Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems())
			{
				if (itemObject2.BannerComponent.BannerLevel == 1 || itemObject2.BannerComponent.BannerLevel == 2)
				{
					mblist.Add(itemObject2);
				}
			}
			if (mblist.IsEmpty<ItemObject>())
			{
				mblist.AddRange(mblist2);
			}
			return mblist;
		}

		// Token: 0x06001A9E RID: 6814 RVA: 0x00087F30 File Offset: 0x00086130
		public override MBList<ItemObject> GetEliteRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
		{
			MBList<ItemObject> mblist = new MBList<ItemObject>();
			foreach (string objectName in new string[]
			{
				"winds_fury_sword_t3", "bone_crusher_mace_t3", "tyrhung_sword_t3", "pernach_mace_t3", "early_retirement_2hsword_t3", "black_heart_2haxe_t3", "knights_fall_mace_t3", "the_scalpel_sword_t3", "judgement_mace_t3", "dawnbreaker_sword_t3",
				"ambassador_sword_t3", "heavy_nasalhelm_over_imperial_mail", "sturgian_helmet_closed", "full_helm_over_laced_coif", "desert_mail_coif", "heavy_nasalhelm_over_imperial_mail", "plumed_nomad_helmet", "ridged_northernhelm", "noble_horse_southern", "noble_horse_imperial",
				"noble_horse_western", "noble_horse_eastern", "noble_horse_battania", "noble_horse_northern", "special_camel", "western_crowned_helmet", "northern_warlord_helmet", "battania_warlord_pauldrons", "aserai_armor_02_b", "white_coat_over_mail",
				"spiked_helmet_with_facemask"
			})
			{
				ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>(objectName);
				if (@object != null)
				{
					mblist.Add(@object);
				}
			}
			return mblist;
		}
	}
}
