using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultHeroCreationModel : HeroCreationModel
{
	private const int AverageSkillValueForHeroComesOfAge = 112;

	private const int NonCombatantSkillThresholdValue = 100;

	private const float FemaleCombatantChance = 0.6f;

	private const int NoiseValueToAddSkill = 5;

	public override (CampaignTime, CampaignTime) GetBirthAndDeathDay(CharacterObject character, bool createAlive, int age)
	{
		if (createAlive)
		{
			switch (age)
			{
			case -1:
			{
				CampaignTime randomBirthDayForAge3 = HeroHelper.GetRandomBirthDayForAge(Campaign.Current.Models.AgeModel.HeroComesOfAge + MBRandom.RandomInt(30));
				CampaignTime never4 = CampaignTime.Never;
				return (randomBirthDayForAge3, never4);
			}
			case 0:
			{
				CampaignTime now = CampaignTime.Now;
				CampaignTime never3 = CampaignTime.Never;
				return (now, never3);
			}
			default:
			{
				if (character.Occupation == Occupation.Wanderer)
				{
					age = (int)character.Age + MBRandom.RandomInt(5);
					if (age < 20)
					{
						foreach (TraitObject item in TraitObject.All)
						{
							int num = 12 + 4 * character.GetTraitLevel(item);
							if (age < num)
							{
								age = num;
							}
						}
					}
					CampaignTime randomBirthDayForAge = HeroHelper.GetRandomBirthDayForAge(age);
					CampaignTime never = CampaignTime.Never;
					return (randomBirthDayForAge, never);
				}
				CampaignTime randomBirthDayForAge2 = HeroHelper.GetRandomBirthDayForAge(age);
				CampaignTime never2 = CampaignTime.Never;
				return (randomBirthDayForAge2, never2);
			}
			}
		}
		HeroHelper.GetRandomDeathDayAndBirthDay((int)character.Age, out var birthday, out var deathday);
		return (birthday, deathday);
	}

	public override Settlement GetBornSettlement(Hero hero)
	{
		if (hero.Mother != null)
		{
			Settlement settlement;
			if (hero.Mother.CurrentSettlement != null && (hero.Mother.CurrentSettlement.IsTown || hero.Mother.CurrentSettlement.IsVillage))
			{
				settlement = hero.Mother.CurrentSettlement;
			}
			else if (hero.Mother.PartyBelongedTo != null || hero.Mother.PartyBelongedToAsPrisoner != null)
			{
				IMapPoint mapPoint2;
				if (hero.Mother.PartyBelongedToAsPrisoner != null)
				{
					IMapPoint mapPoint;
					if (!hero.Mother.PartyBelongedToAsPrisoner.IsMobile)
					{
						IMapPoint settlement2 = hero.Mother.PartyBelongedToAsPrisoner.Settlement;
						mapPoint = settlement2;
					}
					else
					{
						IMapPoint settlement2 = hero.Mother.PartyBelongedToAsPrisoner.MobileParty;
						mapPoint = settlement2;
					}
					mapPoint2 = mapPoint;
				}
				else
				{
					mapPoint2 = hero.Mother.PartyBelongedTo;
				}
				if (mapPoint2 is Settlement settlement3)
				{
					settlement = settlement3;
				}
				else if (mapPoint2 is MobileParty mobileParty)
				{
					Town town = SettlementHelper.FindNearestTownToMobileParty(mobileParty, MobileParty.NavigationType.All);
					settlement = ((town != null) ? town.Settlement : hero.Mother.HomeSettlement);
				}
				else
				{
					settlement = hero.Mother.HomeSettlement;
				}
			}
			else
			{
				settlement = hero.Mother.HomeSettlement;
			}
			if (settlement == null)
			{
				settlement = ((hero.Mother.Clan.Settlements.Count > 0) ? hero.Mother.Clan.Settlements.GetRandomElement() : Town.AllTowns.GetRandomElement().Settlement);
			}
			return settlement;
		}
		Settlement settlement4 = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown && (hero.Culture.StringId == "neutral_culture" || x.Culture == hero.Culture));
		if (settlement4 == null)
		{
			settlement4 = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown);
		}
		return settlement4;
	}

	public override StaticBodyProperties GetStaticBodyProperties(Hero hero, bool isOffspring, float variationAmount = 0.35f)
	{
		if (isOffspring)
		{
			string hairTags = hero.CharacterObject.BodyPropertyRange.HairTags;
			string beardTags = hero.CharacterObject.BodyPropertyRange.BeardTags;
			string tattooTags = hero.CharacterObject.BodyPropertyRange.TattooTags;
			bool flag = string.IsNullOrEmpty(hairTags);
			bool flag2 = string.IsNullOrEmpty(beardTags);
			bool flag3 = string.IsNullOrEmpty(tattooTags);
			if (!flag || !flag2 || !flag3)
			{
				Hero hero2 = ((!hero.IsFemale) ? hero.Father : hero.Mother);
				if (hero2 != null)
				{
					if (!flag)
					{
						hairTags = hero2.CharacterObject.BodyPropertyRange.HairTags;
					}
					if (!flag2)
					{
						beardTags = hero2.CharacterObject.BodyPropertyRange.BeardTags;
					}
					if (!flag3)
					{
						tattooTags = hero2.CharacterObject.BodyPropertyRange.TattooTags;
					}
				}
			}
			BodyProperties bodyProperties = hero.Mother.BodyProperties;
			BodyProperties bodyProperties2 = hero.Father.BodyProperties;
			int seed = MBRandom.RandomInt();
			BodyProperties bodyProperties3 = BodyProperties.GetRandomBodyProperties(hero.Mother.CharacterObject.Race, hero.IsFemale, bodyProperties, bodyProperties2, 1, seed, hairTags, beardTags, tattooTags, variationAmount);
			int hair = -1;
			int beard = -1;
			int tattoo = -1;
			if (string.IsNullOrEmpty(hairTags))
			{
				int[] hairIndicesForCulture = Campaign.Current.Models.BodyPropertiesModel.GetHairIndicesForCulture(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
				hair = ((hairIndicesForCulture.Length != 0) ? hairIndicesForCulture.GetRandomElement() : (-1));
			}
			if (string.IsNullOrEmpty(beardTags))
			{
				int[] beardIndicesForCulture = Campaign.Current.Models.BodyPropertiesModel.GetBeardIndicesForCulture(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
				beard = ((beardIndicesForCulture.Length != 0) ? beardIndicesForCulture.GetRandomElement() : (-1));
			}
			if (string.IsNullOrEmpty(tattooTags))
			{
				int[] tattooIndicesForCulture = Campaign.Current.Models.BodyPropertiesModel.GetTattooIndicesForCulture(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
				tattoo = ((tattooIndicesForCulture.Length != 0) ? tattooIndicesForCulture.GetRandomElement() : (-1));
				float tattooZeroProbability = FaceGen.GetTattooZeroProbability(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age);
				if (MBRandom.RandomFloat < tattooZeroProbability)
				{
					tattoo = 0;
				}
			}
			FaceGen.SetHair(ref bodyProperties3, hair, beard, tattoo);
			return bodyProperties3.StaticProperties;
		}
		if (hero.CharacterObject.IsOriginalCharacter)
		{
			return hero.CharacterObject.GetBodyPropertiesMin(returnBaseValue: true).StaticProperties;
		}
		CharacterObject originalCharacter = hero.CharacterObject.OriginalCharacter;
		return BodyProperties.GetRandomBodyProperties(originalCharacter.Race, originalCharacter.IsFemale, originalCharacter.GetBodyPropertiesMin(returnBaseValue: true), originalCharacter.GetBodyPropertiesMax(returnBaseValue: true), 0, MBRandom.RandomInt(), originalCharacter.BodyPropertyRange.HairTags, originalCharacter.BodyPropertyRange.BeardTags, originalCharacter.BodyPropertyRange.TattooTags).StaticProperties;
	}

	public override FormationClass GetPreferredUpgradeFormation(Hero hero)
	{
		int num = MBRandom.RandomInt(10);
		if (num < 4)
		{
			return (FormationClass)num;
		}
		return FormationClass.NumberOfAllFormations;
	}

	public override Clan GetClan(Hero hero)
	{
		if (hero.Mother != null)
		{
			if (hero.Father == Hero.MainHero || hero.Mother == Hero.MainHero)
			{
				return Clan.PlayerClan;
			}
			return hero.Father.Clan;
		}
		return null;
	}

	public override CultureObject GetCulture(Hero hero, Settlement bornSettlement, Clan clan)
	{
		if (hero.Mother != null)
		{
			if (hero.Father == Hero.MainHero || hero.Mother == Hero.MainHero)
			{
				return Hero.MainHero.Culture;
			}
			if (!(MBRandom.RandomFloat < 0.5f))
			{
				return hero.Mother.Culture;
			}
			return hero.Father.Culture;
		}
		if (!hero.CharacterObject.IsOriginalCharacter)
		{
			return hero.CharacterObject.OriginalCharacter.Culture;
		}
		return hero.CharacterObject.Culture;
	}

	public override CharacterObject GetRandomTemplateByOccupation(Occupation occupation, Settlement settlement = null)
	{
		Settlement settlement2 = settlement ?? SettlementHelper.GetRandomTown();
		List<CharacterObject> list = settlement2.Culture.NotableTemplates.Where((CharacterObject x) => x.Occupation == occupation).ToList();
		int num = 0;
		foreach (CharacterObject item in list)
		{
			int num2 = item.GetTraitLevel(DefaultTraits.Frequency) * 10;
			num += ((num2 > 0) ? num2 : 100);
		}
		if (!list.Any())
		{
			return null;
		}
		int num3 = settlement2.RandomIntWithSeed((uint)settlement2.Notables.Count, 1, num);
		foreach (CharacterObject item2 in list)
		{
			int num4 = item2.GetTraitLevel(DefaultTraits.Frequency) * 10;
			num3 -= ((num4 > 0) ? num4 : 100);
			if (num3 < 0)
			{
				return item2;
			}
		}
		Debug.FailedAssert("Couldn't find template for given occupation!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultHeroCreationModel.cs", "GetRandomTemplateByOccupation", 311);
		return null;
	}

	public override List<(TraitObject trait, int level)> GetTraitsForHero(Hero hero)
	{
		List<(TraitObject, int)> list = new List<(TraitObject, int)>();
		if (hero.Mother != null)
		{
			float randomFloat = MBRandom.RandomFloat;
			int val = ((!(randomFloat < 0.1f)) ? ((randomFloat < 0.5f) ? 1 : ((!(randomFloat < 0.9f)) ? 3 : 2)) : 0);
			List<TraitObject> list2 = DefaultTraits.Personality.ToList();
			list2.Shuffle();
			for (int i = 0; i < Math.Min(list2.Count, val); i++)
			{
				int item = ((MBRandom.RandomFloat < 0.5f) ? MBRandom.RandomInt(list2[i].MinValue, 0) : MBRandom.RandomInt(1, list2[i].MaxValue + 1));
				list.Add((list2[i], item));
			}
			foreach (TraitObject item2 in TraitObject.All.Except(DefaultTraits.Personality))
			{
				list.Add((item2, (MBRandom.RandomFloat < 0.5f) ? hero.Mother.GetTraitLevel(item2) : hero.Father.GetTraitLevel(item2)));
			}
		}
		if (hero.Occupation == Occupation.GangLeader || hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.RuralNotable || hero.Occupation == Occupation.Merchant || hero.Occupation == Occupation.Headman)
		{
			list.Add((DefaultTraits.Honor, CalculateTraitValueForHero(hero, DefaultTraits.Honor)));
			list.Add((DefaultTraits.Mercy, CalculateTraitValueForHero(hero, DefaultTraits.Mercy)));
			list.Add((DefaultTraits.Generosity, CalculateTraitValueForHero(hero, DefaultTraits.Generosity)));
			list.Add((DefaultTraits.Valor, CalculateTraitValueForHero(hero, DefaultTraits.Valor)));
			list.Add((DefaultTraits.Calculating, CalculateTraitValueForHero(hero, DefaultTraits.Calculating)));
		}
		return list;
	}

	private static int CalculateTraitValueForHero(Hero hero, TraitObject trait)
	{
		int num = hero.CharacterObject.GetTraitLevel(trait);
		float num2 = (((hero.IsPreacher && trait == DefaultTraits.Generosity) || (hero.IsPreacher && trait == DefaultTraits.Calculating)) ? 0.5f : MBRandom.RandomFloat);
		if (num2 < 0.25f)
		{
			num--;
		}
		else if (num2 > 0.75f)
		{
			num++;
		}
		if (hero.IsGangLeader && (trait == DefaultTraits.Mercy || trait == DefaultTraits.Honor) && num > 0)
		{
			num = 0;
		}
		return MBMath.ClampInt(num, trait.MinValue, trait.MaxValue);
	}

	public override Equipment GetCivilianEquipment(Hero hero)
	{
		if (hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			return Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentForDeliveredOffspring(hero);
		}
		return hero.CivilianEquipment;
	}

	public override Equipment GetBattleEquipment(Hero hero)
	{
		if (hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			Equipment equipment = new Equipment(Equipment.EquipmentType.Battle);
			equipment.FillFrom(hero.CivilianEquipment, useSourceEquipmentType: false);
			return equipment;
		}
		return hero.BattleEquipment;
	}

	public override CharacterObject GetCharacterTemplateForOffspring(Hero mother, Hero father, bool isOffspringFemale)
	{
		if (!isOffspringFemale)
		{
			return father.CharacterObject;
		}
		return mother.CharacterObject;
	}

	public override (TextObject firstName, TextObject name) GenerateFirstAndFullName(Hero hero)
	{
		NameGenerator.Current.GenerateHeroNameAndHeroFullName(hero, out var firstName, out var fullName, useDeterministicValues: false);
		return (firstName: firstName, name: fullName);
	}

	public override List<(SkillObject, int)> GetDefaultSkillsForHero(Hero hero)
	{
		List<(SkillObject, int)> list = new List<(SkillObject, int)>();
		if (hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			return list;
		}
		MBCharacterSkills defaultCharacterSkills = hero.CharacterObject.GetDefaultCharacterSkills();
		foreach (SkillObject item in Skills.All)
		{
			int num = defaultCharacterSkills.Skills.GetPropertyValue(item);
			if (num > 0)
			{
				num = AddNoiseToSkillValue(num);
			}
			list.Add((item, num));
		}
		return list;
	}

	private static int GetInheritedSkillValue(Hero hero, SkillObject skillObject)
	{
		int num = hero.Father?.GetSkillValue(skillObject) ?? 0;
		int num2 = hero.Mother?.GetSkillValue(skillObject) ?? 0;
		int maxValue = num + num2;
		return AddNoiseToSkillValue((MBRandom.RandomInt(0, maxValue) < num) ? num : num2);
	}

	public override List<(SkillObject, int)> GetInheritedSkillsForHero(Hero hero)
	{
		if (hero.Father == null && hero.Mother == null)
		{
			MBCharacterSkills defaultSkills = hero.CharacterObject.GetDefaultCharacterSkills();
			return Skills.All.Select((SkillObject skill) => (skill: skill, defaultSkills.Skills.GetPropertyValue(skill))).ToList();
		}
		List<(SkillObject, int)> list = new List<(SkillObject, int)>();
		SkillObject item = null;
		foreach (SkillObject item2 in Skills.All)
		{
			list.Add((item2, GetInheritedSkillValue(hero, item2)));
		}
		list = list.OrderByDescending(((SkillObject, int) x) => x.Item2).ToList();
		int num = (int)Math.Round((float)list.Count * (5f / 18f));
		int num2 = -1;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			(SkillObject, int) tuple = list[num3];
			if (IsSkillCombatant(tuple.Item1))
			{
				num2 = num3;
				(item, _) = tuple;
				break;
			}
		}
		list = list.Take(num).ToList();
		bool flag = !hero.IsFemale || hero.Mother == null || !hero.Mother.IsNoncombatant || MBRandom.RandomFloat < 0.6f;
		if (flag && num2 >= list.Count)
		{
			list[list.Count - 1] = (item, list[list.Count - 1].Item2);
			num2 = list.Count - 1;
		}
		int num4 = list.Sum(((SkillObject, int) x) => x.Item2);
		if (num4 == 0)
		{
			Debug.FailedAssert("Neither parent has any skills!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultHeroCreationModel.cs", "GetInheritedSkillsForHero", 512);
			return new List<(SkillObject, int)>();
		}
		float num5 = (float)(112 * num) / (float)num4;
		if (flag)
		{
			if (TaleWorlds.Library.MathF.Round((float)list[num2].Item2 * num5) < 100)
			{
				num4 -= list[num2].Item2;
				num5 = (float)(112 * (num - 1)) / (float)num4;
				list[num2] = (list[num2].Item1, 100);
			}
			else
			{
				list[num2] = (list[num2].Item1, TaleWorlds.Library.MathF.Round((float)list[num2].Item2 * num5));
			}
		}
		for (int num6 = 0; num6 < list.Count; num6++)
		{
			if (!flag || num6 != num2)
			{
				(SkillObject, int) tuple3 = list[num6];
				list[num6] = (tuple3.Item1, TaleWorlds.Library.MathF.Round((float)tuple3.Item2 * num5));
			}
		}
		return list;
	}

	private static bool IsSkillCombatant(SkillObject skillObject)
	{
		if (skillObject != DefaultSkills.OneHanded && skillObject != DefaultSkills.TwoHanded && skillObject != DefaultSkills.Polearm && skillObject != DefaultSkills.Throwing && skillObject != DefaultSkills.Crossbow)
		{
			return skillObject == DefaultSkills.Bow;
		}
		return true;
	}

	private static int AddNoiseToSkillValue(int skillValue)
	{
		skillValue += MBRandom.RandomInt(5, 10);
		return TaleWorlds.Library.MathF.Max(skillValue, 1);
	}

	public override bool IsHeroCombatant(Hero hero)
	{
		if (hero.GetSkillValue(DefaultSkills.OneHanded) < 100 && hero.GetSkillValue(DefaultSkills.TwoHanded) < 100 && hero.GetSkillValue(DefaultSkills.Polearm) < 100 && hero.GetSkillValue(DefaultSkills.Throwing) < 100 && hero.GetSkillValue(DefaultSkills.Crossbow) < 100)
		{
			return hero.GetSkillValue(DefaultSkills.Bow) >= 100;
		}
		return true;
	}
}
