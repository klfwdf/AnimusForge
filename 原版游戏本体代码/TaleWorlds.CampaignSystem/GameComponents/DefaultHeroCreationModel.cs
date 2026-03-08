using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200011A RID: 282
	public class DefaultHeroCreationModel : HeroCreationModel
	{
		// Token: 0x060017EC RID: 6124 RVA: 0x000723E0 File Offset: 0x000705E0
		public override ValueTuple<CampaignTime, CampaignTime> GetBirthAndDeathDay(CharacterObject character, bool createAlive, int age)
		{
			if (!createAlive)
			{
				CampaignTime item;
				CampaignTime item2;
				HeroHelper.GetRandomDeathDayAndBirthDay((int)character.Age, out item, out item2);
				return new ValueTuple<CampaignTime, CampaignTime>(item, item2);
			}
			if (age == -1)
			{
				CampaignTime randomBirthDayForAge = HeroHelper.GetRandomBirthDayForAge((float)(Campaign.Current.Models.AgeModel.HeroComesOfAge + MBRandom.RandomInt(30)));
				CampaignTime never = CampaignTime.Never;
				return new ValueTuple<CampaignTime, CampaignTime>(randomBirthDayForAge, never);
			}
			if (age == 0)
			{
				CampaignTime now = CampaignTime.Now;
				CampaignTime never2 = CampaignTime.Never;
				return new ValueTuple<CampaignTime, CampaignTime>(now, never2);
			}
			if (character.Occupation == Occupation.Wanderer)
			{
				age = (int)character.Age + MBRandom.RandomInt(5);
				if (age < 20)
				{
					foreach (TraitObject trait in TraitObject.All)
					{
						int num = 12 + 4 * character.GetTraitLevel(trait);
						if (age < num)
						{
							age = num;
						}
					}
				}
				CampaignTime randomBirthDayForAge2 = HeroHelper.GetRandomBirthDayForAge((float)age);
				CampaignTime never3 = CampaignTime.Never;
				return new ValueTuple<CampaignTime, CampaignTime>(randomBirthDayForAge2, never3);
			}
			CampaignTime randomBirthDayForAge3 = HeroHelper.GetRandomBirthDayForAge((float)age);
			CampaignTime never4 = CampaignTime.Never;
			return new ValueTuple<CampaignTime, CampaignTime>(randomBirthDayForAge3, never4);
		}

		// Token: 0x060017ED RID: 6125 RVA: 0x000724F4 File Offset: 0x000706F4
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
					IMapPoint mapPoint3;
					if (hero.Mother.PartyBelongedToAsPrisoner != null)
					{
						IMapPoint mapPoint2;
						if (!hero.Mother.PartyBelongedToAsPrisoner.IsMobile)
						{
							IMapPoint mapPoint = hero.Mother.PartyBelongedToAsPrisoner.Settlement;
							mapPoint2 = mapPoint;
						}
						else
						{
							IMapPoint mapPoint = hero.Mother.PartyBelongedToAsPrisoner.MobileParty;
							mapPoint2 = mapPoint;
						}
						mapPoint3 = mapPoint2;
					}
					else
					{
						mapPoint3 = hero.Mother.PartyBelongedTo;
					}
					Settlement settlement2;
					MobileParty mobileParty;
					if ((settlement2 = mapPoint3 as Settlement) != null)
					{
						settlement = settlement2;
					}
					else if ((mobileParty = mapPoint3 as MobileParty) != null)
					{
						Town town = SettlementHelper.FindNearestTownToMobileParty(mobileParty, MobileParty.NavigationType.All, null);
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
					settlement = ((hero.Mother.Clan.Settlements.Count > 0) ? hero.Mother.Clan.Settlements.GetRandomElement<Settlement>() : Town.AllTowns.GetRandomElement<Town>().Settlement);
				}
				return settlement;
			}
			Settlement settlement3 = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown && (hero.Culture.StringId == "neutral_culture" || x.Culture == hero.Culture));
			if (settlement3 == null)
			{
				settlement3 = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown);
			}
			return settlement3;
		}

		// Token: 0x060017EE RID: 6126 RVA: 0x00072700 File Offset: 0x00070900
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
					Hero hero2;
					if (hero.IsFemale)
					{
						hero2 = hero.Mother;
					}
					else
					{
						hero2 = hero.Father;
					}
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
				BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(hero.Mother.CharacterObject.Race, hero.IsFemale, bodyProperties, bodyProperties2, 1, seed, hairTags, beardTags, tattooTags, variationAmount);
				int hair = -1;
				int beard = -1;
				int tattoo = -1;
				if (string.IsNullOrEmpty(hairTags))
				{
					int[] hairIndicesForCulture = Campaign.Current.Models.BodyPropertiesModel.GetHairIndicesForCulture(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
					hair = ((hairIndicesForCulture.Length != 0) ? hairIndicesForCulture.GetRandomElement<int>() : (-1));
				}
				if (string.IsNullOrEmpty(beardTags))
				{
					int[] beardIndicesForCulture = Campaign.Current.Models.BodyPropertiesModel.GetBeardIndicesForCulture(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
					beard = ((beardIndicesForCulture.Length != 0) ? beardIndicesForCulture.GetRandomElement<int>() : (-1));
				}
				if (string.IsNullOrEmpty(tattooTags))
				{
					int[] tattooIndicesForCulture = Campaign.Current.Models.BodyPropertiesModel.GetTattooIndicesForCulture(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
					tattoo = ((tattooIndicesForCulture.Length != 0) ? tattooIndicesForCulture.GetRandomElement<int>() : (-1));
					float tattooZeroProbability = FaceGen.GetTattooZeroProbability(hero.CharacterObject.Race, hero.IsFemale ? 1 : 0, hero.Age);
					if (MBRandom.RandomFloat < tattooZeroProbability)
					{
						tattoo = 0;
					}
				}
				FaceGen.SetHair(ref randomBodyProperties, hair, beard, tattoo);
				return randomBodyProperties.StaticProperties;
			}
			if (hero.CharacterObject.IsOriginalCharacter)
			{
				return hero.CharacterObject.GetBodyPropertiesMin(true).StaticProperties;
			}
			CharacterObject originalCharacter = hero.CharacterObject.OriginalCharacter;
			return BodyProperties.GetRandomBodyProperties(originalCharacter.Race, originalCharacter.IsFemale, originalCharacter.GetBodyPropertiesMin(true), originalCharacter.GetBodyPropertiesMax(true), 0, MBRandom.RandomInt(), originalCharacter.BodyPropertyRange.HairTags, originalCharacter.BodyPropertyRange.BeardTags, originalCharacter.BodyPropertyRange.TattooTags, 0f).StaticProperties;
		}

		// Token: 0x060017EF RID: 6127 RVA: 0x000729DC File Offset: 0x00070BDC
		public override FormationClass GetPreferredUpgradeFormation(Hero hero)
		{
			int num = MBRandom.RandomInt(10);
			if (num < 4)
			{
				return (FormationClass)num;
			}
			return FormationClass.NumberOfAllFormations;
		}

		// Token: 0x060017F0 RID: 6128 RVA: 0x000729F9 File Offset: 0x00070BF9
		public override Clan GetClan(Hero hero)
		{
			if (hero.Mother == null)
			{
				return null;
			}
			if (hero.Father == Hero.MainHero || hero.Mother == Hero.MainHero)
			{
				return Clan.PlayerClan;
			}
			return hero.Father.Clan;
		}

		// Token: 0x060017F1 RID: 6129 RVA: 0x00072A30 File Offset: 0x00070C30
		public override CultureObject GetCulture(Hero hero, Settlement bornSettlement, Clan clan)
		{
			if (hero.Mother != null)
			{
				if (hero.Father == Hero.MainHero || hero.Mother == Hero.MainHero)
				{
					return Hero.MainHero.Culture;
				}
				if (MBRandom.RandomFloat >= 0.5f)
				{
					return hero.Mother.Culture;
				}
				return hero.Father.Culture;
			}
			else
			{
				if (!hero.CharacterObject.IsOriginalCharacter)
				{
					return hero.CharacterObject.OriginalCharacter.Culture;
				}
				return hero.CharacterObject.Culture;
			}
		}

		// Token: 0x060017F2 RID: 6130 RVA: 0x00072AB8 File Offset: 0x00070CB8
		public override CharacterObject GetRandomTemplateByOccupation(Occupation occupation, Settlement settlement = null)
		{
			Settlement settlement2 = settlement ?? SettlementHelper.GetRandomTown(null);
			List<CharacterObject> list = (from x in settlement2.Culture.NotableTemplates
				where x.Occupation == occupation
				select x).ToList<CharacterObject>();
			int num = 0;
			foreach (CharacterObject characterObject in list)
			{
				int num2 = characterObject.GetTraitLevel(DefaultTraits.Frequency) * 10;
				num += ((num2 > 0) ? num2 : 100);
			}
			if (!list.Any<CharacterObject>())
			{
				return null;
			}
			int num3 = settlement2.RandomIntWithSeed((uint)settlement2.Notables.Count, 1, num);
			foreach (CharacterObject characterObject2 in list)
			{
				int num4 = characterObject2.GetTraitLevel(DefaultTraits.Frequency) * 10;
				num3 -= ((num4 > 0) ? num4 : 100);
				if (num3 < 0)
				{
					return characterObject2;
				}
			}
			Debug.FailedAssert("Couldn't find template for given occupation!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultHeroCreationModel.cs", "GetRandomTemplateByOccupation", 311);
			return null;
		}

		// Token: 0x060017F3 RID: 6131 RVA: 0x00072BFC File Offset: 0x00070DFC
		[return: TupleElementNames(new string[] { "trait", "level" })]
		public override List<ValueTuple<TraitObject, int>> GetTraitsForHero(Hero hero)
		{
			List<ValueTuple<TraitObject, int>> list = new List<ValueTuple<TraitObject, int>>();
			if (hero.Mother != null)
			{
				float randomFloat = MBRandom.RandomFloat;
				int val;
				if (randomFloat < 0.1f)
				{
					val = 0;
				}
				else if (randomFloat < 0.5f)
				{
					val = 1;
				}
				else if (randomFloat < 0.9f)
				{
					val = 2;
				}
				else
				{
					val = 3;
				}
				List<TraitObject> list2 = DefaultTraits.Personality.ToList<TraitObject>();
				list2.Shuffle<TraitObject>();
				for (int i = 0; i < Math.Min(list2.Count, val); i++)
				{
					int item = ((MBRandom.RandomFloat < 0.5f) ? MBRandom.RandomInt(list2[i].MinValue, 0) : MBRandom.RandomInt(1, list2[i].MaxValue + 1));
					list.Add(new ValueTuple<TraitObject, int>(list2[i], item));
				}
				foreach (TraitObject traitObject in TraitObject.All.Except(DefaultTraits.Personality))
				{
					list.Add(new ValueTuple<TraitObject, int>(traitObject, (MBRandom.RandomFloat < 0.5f) ? hero.Mother.GetTraitLevel(traitObject) : hero.Father.GetTraitLevel(traitObject)));
				}
			}
			if (hero.Occupation == Occupation.GangLeader || hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.RuralNotable || hero.Occupation == Occupation.Merchant || hero.Occupation == Occupation.Headman)
			{
				list.Add(new ValueTuple<TraitObject, int>(DefaultTraits.Honor, DefaultHeroCreationModel.CalculateTraitValueForHero(hero, DefaultTraits.Honor)));
				list.Add(new ValueTuple<TraitObject, int>(DefaultTraits.Mercy, DefaultHeroCreationModel.CalculateTraitValueForHero(hero, DefaultTraits.Mercy)));
				list.Add(new ValueTuple<TraitObject, int>(DefaultTraits.Generosity, DefaultHeroCreationModel.CalculateTraitValueForHero(hero, DefaultTraits.Generosity)));
				list.Add(new ValueTuple<TraitObject, int>(DefaultTraits.Valor, DefaultHeroCreationModel.CalculateTraitValueForHero(hero, DefaultTraits.Valor)));
				list.Add(new ValueTuple<TraitObject, int>(DefaultTraits.Calculating, DefaultHeroCreationModel.CalculateTraitValueForHero(hero, DefaultTraits.Calculating)));
			}
			return list;
		}

		// Token: 0x060017F4 RID: 6132 RVA: 0x00072DFC File Offset: 0x00070FFC
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

		// Token: 0x060017F5 RID: 6133 RVA: 0x00072E8F File Offset: 0x0007108F
		public override Equipment GetCivilianEquipment(Hero hero)
		{
			if (hero.Mother != null)
			{
				return Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForDeliveredOffspring(hero).GetRandomElementInefficiently<MBEquipmentRoster>().GetCivilianEquipments()
					.GetRandomElementInefficiently<Equipment>();
			}
			return hero.CivilianEquipment;
		}

		// Token: 0x060017F6 RID: 6134 RVA: 0x00072EC4 File Offset: 0x000710C4
		public override Equipment GetBattleEquipment(Hero hero)
		{
			if (hero.Mother != null)
			{
				Equipment equipment = new Equipment(Equipment.EquipmentType.Battle);
				equipment.FillFrom(hero.CivilianEquipment, false);
				return equipment;
			}
			return hero.BattleEquipment;
		}

		// Token: 0x060017F7 RID: 6135 RVA: 0x00072EE8 File Offset: 0x000710E8
		public override CharacterObject GetCharacterTemplateForOffspring(Hero mother, Hero father, bool isOffspringFemale)
		{
			if (!isOffspringFemale)
			{
				return father.CharacterObject;
			}
			return mother.CharacterObject;
		}

		// Token: 0x060017F8 RID: 6136 RVA: 0x00072EFC File Offset: 0x000710FC
		[return: TupleElementNames(new string[] { "firstName", "name" })]
		public override ValueTuple<TextObject, TextObject> GenerateFirstAndFullName(Hero hero)
		{
			TextObject item;
			TextObject item2;
			NameGenerator.Current.GenerateHeroNameAndHeroFullName(hero, out item, out item2, false);
			return new ValueTuple<TextObject, TextObject>(item, item2);
		}

		// Token: 0x060017F9 RID: 6137 RVA: 0x00072F20 File Offset: 0x00071120
		public override List<ValueTuple<SkillObject, int>> GetDefaultSkillsForHero(Hero hero)
		{
			List<ValueTuple<SkillObject, int>> list = new List<ValueTuple<SkillObject, int>>();
			if (hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
			{
				return list;
			}
			MBCharacterSkills defaultCharacterSkills = hero.CharacterObject.GetDefaultCharacterSkills();
			foreach (SkillObject skillObject in Skills.All)
			{
				int num = defaultCharacterSkills.Skills.GetPropertyValue(skillObject);
				if (num > 0)
				{
					num = DefaultHeroCreationModel.AddNoiseToSkillValue(num);
				}
				list.Add(new ValueTuple<SkillObject, int>(skillObject, num));
			}
			return list;
		}

		// Token: 0x060017FA RID: 6138 RVA: 0x00072FC8 File Offset: 0x000711C8
		private static int GetInheritedSkillValue(Hero hero, SkillObject skillObject)
		{
			Hero father = hero.Father;
			int num = ((father != null) ? father.GetSkillValue(skillObject) : 0);
			Hero mother = hero.Mother;
			int num2 = ((mother != null) ? mother.GetSkillValue(skillObject) : 0);
			int maxValue = num + num2;
			return DefaultHeroCreationModel.AddNoiseToSkillValue((MBRandom.RandomInt(0, maxValue) < num) ? num : num2);
		}

		// Token: 0x060017FB RID: 6139 RVA: 0x00073014 File Offset: 0x00071214
		public override List<ValueTuple<SkillObject, int>> GetInheritedSkillsForHero(Hero hero)
		{
			if (hero.Father == null && hero.Mother == null)
			{
				MBCharacterSkills defaultSkills = hero.CharacterObject.GetDefaultCharacterSkills();
				return (from skill in Skills.All
					select new ValueTuple<SkillObject, int>(skill, defaultSkills.Skills.GetPropertyValue(skill))).ToList<ValueTuple<SkillObject, int>>();
			}
			List<ValueTuple<SkillObject, int>> list = new List<ValueTuple<SkillObject, int>>();
			SkillObject item = null;
			foreach (SkillObject skillObject in Skills.All)
			{
				list.Add(new ValueTuple<SkillObject, int>(skillObject, DefaultHeroCreationModel.GetInheritedSkillValue(hero, skillObject)));
			}
			list = (from x in list
				orderby x.Item2 descending
				select x).ToList<ValueTuple<SkillObject, int>>();
			int num = (int)Math.Round((double)((float)list.Count * 0.2777778f));
			int num2 = -1;
			for (int i = 0; i < list.Count; i++)
			{
				ValueTuple<SkillObject, int> valueTuple = list[i];
				if (DefaultHeroCreationModel.IsSkillCombatant(valueTuple.Item1))
				{
					num2 = i;
					item = valueTuple.Item1;
					break;
				}
			}
			list = list.Take(num).ToList<ValueTuple<SkillObject, int>>();
			bool flag = !hero.IsFemale || hero.Mother == null || !hero.Mother.IsNoncombatant || MBRandom.RandomFloat < 0.6f;
			if (flag && num2 >= list.Count)
			{
				list[list.Count - 1] = new ValueTuple<SkillObject, int>(item, list[list.Count - 1].Item2);
				num2 = list.Count - 1;
			}
			int num3 = list.Sum((ValueTuple<SkillObject, int> x) => x.Item2);
			if (num3 == 0)
			{
				Debug.FailedAssert("Neither parent has any skills!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultHeroCreationModel.cs", "GetInheritedSkillsForHero", 513);
				return new List<ValueTuple<SkillObject, int>>();
			}
			float num4 = (float)(112 * num) / (float)num3;
			if (flag)
			{
				if (MathF.Round((float)list[num2].Item2 * num4) < 100)
				{
					num3 -= list[num2].Item2;
					num4 = (float)(112 * (num - 1)) / (float)num3;
					list[num2] = new ValueTuple<SkillObject, int>(list[num2].Item1, 100);
				}
				else
				{
					list[num2] = new ValueTuple<SkillObject, int>(list[num2].Item1, MathF.Round((float)list[num2].Item2 * num4));
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (!flag || j != num2)
				{
					ValueTuple<SkillObject, int> valueTuple2 = list[j];
					list[j] = new ValueTuple<SkillObject, int>(valueTuple2.Item1, MathF.Round((float)valueTuple2.Item2 * num4));
				}
			}
			return list;
		}

		// Token: 0x060017FC RID: 6140 RVA: 0x000732E4 File Offset: 0x000714E4
		private static bool IsSkillCombatant(SkillObject skillObject)
		{
			return skillObject == DefaultSkills.OneHanded || skillObject == DefaultSkills.TwoHanded || skillObject == DefaultSkills.Polearm || skillObject == DefaultSkills.Throwing || skillObject == DefaultSkills.Crossbow || skillObject == DefaultSkills.Bow;
		}

		// Token: 0x060017FD RID: 6141 RVA: 0x00073318 File Offset: 0x00071518
		private static int AddNoiseToSkillValue(int skillValue)
		{
			skillValue += MBRandom.RandomInt(5, 10);
			return MathF.Max(skillValue, 1);
		}

		// Token: 0x060017FE RID: 6142 RVA: 0x00073330 File Offset: 0x00071530
		public override bool IsHeroCombatant(Hero hero)
		{
			return hero.GetSkillValue(DefaultSkills.OneHanded) >= 100 || hero.GetSkillValue(DefaultSkills.TwoHanded) >= 100 || hero.GetSkillValue(DefaultSkills.Polearm) >= 100 || hero.GetSkillValue(DefaultSkills.Throwing) >= 100 || hero.GetSkillValue(DefaultSkills.Crossbow) >= 100 || hero.GetSkillValue(DefaultSkills.Bow) >= 100;
		}

		// Token: 0x040007CE RID: 1998
		private const int AverageSkillValueForHeroComesOfAge = 112;

		// Token: 0x040007CF RID: 1999
		private const int NonCombatantSkillThresholdValue = 100;

		// Token: 0x040007D0 RID: 2000
		private const float FemaleCombatantChance = 0.6f;

		// Token: 0x040007D1 RID: 2001
		private const int NoiseValueToAddSkill = 5;
	}
}
