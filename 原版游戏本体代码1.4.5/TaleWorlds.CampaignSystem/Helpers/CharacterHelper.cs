using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Helpers;

public static class CharacterHelper
{
	public static TextObject GetDeathNotification(Hero victimHero, Hero killer, KillCharacterAction.KillCharacterActionDetail detail)
	{
		TextObject empty = TextObject.GetEmpty();
		if (detail == KillCharacterAction.KillCharacterActionDetail.DiedInLabor || detail == KillCharacterAction.KillCharacterActionDetail.Murdered || detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle || detail == KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge)
		{
			empty = GameTexts.FindText("str_on_hero_killed", detail.ToString());
		}
		else if ((detail == KillCharacterAction.KillCharacterActionDetail.Executed || detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent) && killer != null)
		{
			empty = GameTexts.FindText("str_on_hero_killed", detail.ToString());
			StringHelpers.SetCharacterProperties("KILLER", killer.CharacterObject, empty);
		}
		else if (detail == KillCharacterAction.KillCharacterActionDetail.Lost)
		{
			empty = GameTexts.FindText("str_on_hero_killed", detail.ToString());
			StringHelpers.SetCharacterProperties("VICTIM", victimHero.CharacterObject, empty);
		}
		else
		{
			empty = GameTexts.FindText("str_on_hero_killed", "Default");
		}
		StringHelpers.SetCharacterProperties("HERO", victimHero.CharacterObject, empty);
		return empty;
	}

	public static DynamicBodyProperties GetDynamicBodyPropertiesBetweenMinMaxRange(CharacterObject character)
	{
		BodyProperties bodyPropertyMin = character.BodyPropertyRange.BodyPropertyMin;
		BodyProperties bodyPropertyMax = character.BodyPropertyRange.BodyPropertyMax;
		float minVal = ((bodyPropertyMin.Age < bodyPropertyMax.Age) ? bodyPropertyMin.Age : bodyPropertyMax.Age);
		float maxVal = ((bodyPropertyMin.Age > bodyPropertyMax.Age) ? bodyPropertyMin.Age : bodyPropertyMax.Age);
		float minVal2 = ((bodyPropertyMin.Weight < bodyPropertyMax.Weight) ? bodyPropertyMin.Weight : bodyPropertyMax.Weight);
		float maxVal2 = ((bodyPropertyMin.Weight > bodyPropertyMax.Weight) ? bodyPropertyMin.Weight : bodyPropertyMax.Weight);
		float minVal3 = ((bodyPropertyMin.Build < bodyPropertyMax.Build) ? bodyPropertyMin.Build : bodyPropertyMax.Build);
		float maxVal3 = ((bodyPropertyMin.Build > bodyPropertyMax.Build) ? bodyPropertyMin.Build : bodyPropertyMax.Build);
		float age = MBRandom.RandomFloatRanged(minVal, maxVal);
		float weight = MBRandom.RandomFloatRanged(minVal2, maxVal2);
		float build = MBRandom.RandomFloatRanged(minVal3, maxVal3);
		return new DynamicBodyProperties(age, weight, build);
	}

	public static TextObject GetReputationDescription(CharacterObject character)
	{
		TextObject textObject = new TextObject("{=!}{REPUTATION_SUMMARY}");
		TextObject textObject2 = Campaign.Current.ConversationManager.FindMatchingTextOrNull("reputation", character);
		StringHelpers.SetCharacterProperties("NOTABLE", character, textObject2);
		textObject.SetTextVariable("REPUTATION_SUMMARY", textObject2);
		return textObject;
	}

	public static (uint color1, uint color2) GetDeterministicColorsForCharacter(CharacterObject character, PartyBase partyBelongsTo)
	{
		if (character.IsHero)
		{
			if (character.Occupation == Occupation.Lord)
			{
				return (color1: (uint)(((int?)character.HeroObject.MapFaction?.Color) ?? (-3357781)), color2: (uint)(((int?)character.HeroObject.MapFaction?.Color2) ?? (-3357781)));
			}
			if (character.HeroObject.MapFaction == null)
			{
				return (color1: 4291609515u, color2: 4291609515u);
			}
			uint[] colors = character.HeroObject.MapFaction.Culture.StringId switch
			{
				"empire" => CampaignData.EmpireHeroClothColors, 
				"sturgia" => CampaignData.SturgiaHeroClothColors, 
				"aserai" => CampaignData.AseraiHeroClothColors, 
				"vlandia" => CampaignData.VlandiaHeroClothColors, 
				"battania" => CampaignData.BattaniaHeroClothColors, 
				"khuzait" => CampaignData.KhuzaitHeroClothColors, 
				_ => CampaignData.EmpireHeroClothColors, 
			};
			return (color1: character.HeroObject.MapFaction.Color, color2: GetDeterministicColorFromListForHero(character.HeroObject, colors));
		}
		int item = ((int?)partyBelongsTo?.MapFaction?.Color) ?? (-3357781);
		uint item2 = (uint)(((int?)partyBelongsTo?.MapFaction?.Color2) ?? (-3357781));
		return (color1: (uint)item, color2: item2);
	}

	private static uint GetDeterministicColorFromListForHero(Hero hero, uint[] colors)
	{
		return colors.ElementAt(hero.RandomIntWithSeed(39u) % colors.Length);
	}

	public static IFaceGeneratorCustomFilter GetFaceGeneratorFilter()
	{
		return Campaign.Current.GetCampaignBehavior<IFacegenCampaignBehavior>()?.GetFaceGenFilter();
	}

	public static string GetNonconversationPose(CharacterObject character)
	{
		if (character.HeroObject.IsGangLeader)
		{
			return "aggressive";
		}
		if (!character.HeroObject.IsNoncombatant && character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) <= 0 && character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) < 0)
		{
			return "aggressive2";
		}
		if (!character.HeroObject.IsNoncombatant && character.HeroObject.IsLord && character.GetPersona() == DefaultTraits.PersonaCurt && character.HeroObject.GetTraitLevel(DefaultTraits.Honor) > 0)
		{
			return "warrior2";
		}
		if (character.HeroObject.Clan != null && character.HeroObject.Clan.IsNoble && character.GetPersona() == DefaultTraits.PersonaEarnest && character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) >= 0 && character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) >= 0)
		{
			return "hip2";
		}
		if (character.IsFemale && character.GetPersona() == DefaultTraits.PersonaSoftspoken)
		{
			return "demure";
		}
		if (character.IsFemale && character.GetPersona() == DefaultTraits.PersonaIronic)
		{
			return "confident3";
		}
		if (character.GetPersona() == DefaultTraits.PersonaCurt)
		{
			return "closed2";
		}
		if (character.GetPersona() == DefaultTraits.PersonaSoftspoken)
		{
			return "demure2";
		}
		if (character.GetPersona() == DefaultTraits.PersonaIronic)
		{
			return "confident";
		}
		if (character.GetPersona() == DefaultTraits.PersonaEarnest)
		{
			return "normal2";
		}
		return "normal";
	}

	public static string GetNonconversationFacialIdle(CharacterObject character)
	{
		string result = "convo_normal";
		string result2 = "convo_bemused";
		string result3 = "convo_mocking_teasing";
		string result4 = "convo_mocking_revenge";
		string result5 = "convo_delighted";
		string result6 = "convo_approving";
		string result7 = "convo_thinking";
		string result8 = "convo_focused_happy";
		string result9 = "convo_calm_friendly";
		string result10 = "convo_annoyed";
		string result11 = "convo_undecided_closed";
		string result12 = "convo_bored";
		string result13 = "convo_grave";
		string result14 = "convo_predatory";
		string result15 = "convo_confused_annoyed";
		if (character.HeroObject.IsGangLeader)
		{
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) <= 0 && character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) < 0)
			{
				return result14;
			}
			return result15;
		}
		if (character.GetPersona() == DefaultTraits.PersonaCurt)
		{
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) < 0)
			{
				return result12;
			}
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Honor) > 0)
			{
				return result11;
			}
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) < 0)
			{
				return result10;
			}
			return result13;
		}
		if (character.GetPersona() == DefaultTraits.PersonaEarnest)
		{
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) > 0)
			{
				return result8;
			}
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) < 0)
			{
				return result12;
			}
			return result5;
		}
		if (character.IsFemale && character.GetPersona() == DefaultTraits.PersonaSoftspoken)
		{
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) > 0)
			{
				return result9;
			}
			if (!character.HeroObject.IsNoncombatant)
			{
				return result7;
			}
			return result6;
		}
		if (character.GetPersona() == DefaultTraits.PersonaIronic)
		{
			if (!character.HeroObject.IsNoncombatant && character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) < 0)
			{
				return result4;
			}
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) < 0)
			{
				return result3;
			}
			return result2;
		}
		return result;
	}

	public static string GetStandingBodyIdle(CharacterObject character, PartyBase party)
	{
		HeroHelper.WillLordAttack();
		string result = "normal";
		int num = 0;
		TraitObject persona = character.GetPersona();
		int num2 = -1;
		bool flag = Settlement.CurrentSettlement != null;
		if (character.IsHero)
		{
			if (character.HeroObject.IsWounded)
			{
				return (MBRandom.RandomFloat <= 0.7f) ? "weary" : "weary2";
			}
			bool num3 = !character.HeroObject.IsHumanPlayerCharacter;
			num2 = GetSuperiorityState(character);
			if (num3)
			{
				num = Hero.MainHero.GetRelation(character.HeroObject);
				bool flag2 = MorePowerThanPlayer(character);
				if (character.IsFemale && character.HeroObject.IsNoncombatant)
				{
					if (num < 0)
					{
						result = "closed";
					}
					else if (persona == DefaultTraits.PersonaIronic)
					{
						result = ((MBRandom.RandomFloat <= 0.5f) ? "confident" : "confident2");
					}
					else if (persona == DefaultTraits.PersonaCurt)
					{
						result = ((MBRandom.RandomFloat <= 0.5f) ? "closed" : "confident");
					}
					else if (persona == DefaultTraits.PersonaEarnest || persona == DefaultTraits.PersonaSoftspoken)
					{
						result = ((MBRandom.RandomFloat <= 0.7f) ? "demure" : "confident");
					}
				}
				else if (num <= -20)
				{
					if (num2 >= 0)
					{
						result = ((persona == DefaultTraits.PersonaSoftspoken) ? (character.IsFemale ? "closed" : "warrior2") : ((persona != DefaultTraits.PersonaIronic) ? (character.IsFemale ? "confident2" : "warrior") : (character.IsFemale ? "confident2" : "aggressive")));
					}
					else if (num2 == -1)
					{
						result = ((persona == DefaultTraits.PersonaSoftspoken) ? ((!flag2) ? (character.IsFemale ? "closed" : "normal") : "closed") : ((persona != DefaultTraits.PersonaIronic) ? (character.IsFemale ? "closed" : "warrior2") : ((!flag2) ? "closed" : ((MBRandom.RandomFloat <= 0.5f) ? "closed" : "warrior"))));
					}
				}
				else if (num2 >= 0)
				{
					if (persona == DefaultTraits.PersonaIronic)
					{
						result = ((!flag) ? "confident2" : ((!flag2) ? ((MBRandom.RandomFloat <= 0.5f) ? "hip" : "normal") : ((MBRandom.RandomFloat <= 0.7f) ? "confident2" : "normal")));
					}
					else if (persona == DefaultTraits.PersonaSoftspoken)
					{
						result = ((!flag) ? "normal" : ((character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) + character.HeroObject.GetTraitLevel(DefaultTraits.Honor) > 0) ? ((MBRandom.RandomFloat <= 0.5f) ? "normal2" : "demure2") : ((!flag2) ? ((MBRandom.RandomFloat <= 0.5f) ? "normal" : "demure") : ((MBRandom.RandomFloat <= 0.5f) ? "normal" : "closed"))));
					}
					else if (persona == DefaultTraits.PersonaCurt)
					{
						result = ((!flag) ? "normal" : ((character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) + character.HeroObject.GetTraitLevel(DefaultTraits.Honor) > 0) ? "demure2" : ((!flag2) ? ((MBRandom.RandomFloat <= 0.4f) ? "warrior2" : "closed") : ((MBRandom.RandomFloat <= 0.6f) ? "normal" : "closed2"))));
					}
					else if (persona == DefaultTraits.PersonaEarnest)
					{
						result = ((!flag) ? "normal" : ((!flag2) ? ((MBRandom.RandomFloat <= 0.2f) ? "normal" : "confident") : ((MBRandom.RandomFloat <= 0.6f) ? "normal" : "confident")));
					}
				}
			}
		}
		if (party != null)
		{
			MobileParty mobileParty = party.MobileParty;
			if (mobileParty != null && mobileParty.IsCurrentlyAtSea && party != PartyBase.MainParty)
			{
				return "naval";
			}
		}
		if (character.Occupation == Occupation.Bandit || character.Occupation == Occupation.Gangster)
		{
			result = ((MBRandom.RandomFloat <= 0.7f) ? "aggressive" : "hip");
		}
		if (character.Occupation == Occupation.Guard || character.Occupation == Occupation.PrisonGuard || character.Occupation == Occupation.Soldier)
		{
			result = "normal";
		}
		return result;
	}

	public static string GetDefaultFaceIdle(CharacterObject character)
	{
		string result = "convo_normal";
		string result2 = "convo_bemused";
		string result3 = "convo_mocking_aristocratic";
		string result4 = "convo_mocking_teasing";
		string result5 = "convo_mocking_revenge";
		string result6 = "convo_contemptuous";
		string result7 = "convo_delighted";
		string result8 = "convo_approving";
		string result9 = "convo_relaxed_happy";
		string result10 = "convo_nonchalant";
		string result11 = "convo_thinking";
		string result12 = "convo_undecided_closed";
		string result13 = "convo_bored";
		string result14 = "convo_bored2";
		string result15 = "convo_grave";
		string result16 = "convo_stern";
		string result17 = "convo_very_stern";
		string result18 = "convo_beaten";
		string result19 = "convo_predatory";
		string result20 = "convo_confused_annoyed";
		bool flag = false;
		bool flag2 = false;
		if (character.IsHero)
		{
			flag = character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) + character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) > 0;
			flag2 = character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) + character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) < 0;
		}
		bool flag3 = Hero.MainHero.Clan.Renown < 0f;
		bool flag4 = false;
		if (PlayerEncounter.Current != null && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender && (PlayerEncounter.EncounteredMobileParty == null || PlayerEncounter.EncounteredMobileParty.Ai.DoNotAttackMainPartyUntil.IsPast) && PlayerEncounter.EncounteredParty.Owner != null && FactionManager.IsAtWarAgainstFaction(PlayerEncounter.EncounteredParty.MapFaction, Hero.MainHero.MapFaction))
		{
			flag4 = true;
		}
		if (Campaign.Current.CurrentConversationContext == ConversationContext.CapturedLord && character.IsHero && character.HeroObject.MapFaction == PlayerEncounter.EncounteredParty.MapFaction)
		{
			return result16;
		}
		int num = 0;
		if (character.HeroObject != null)
		{
			num = character.HeroObject.GetRelation(Hero.MainHero);
			if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaIronic)
			{
				if (num > 4)
				{
					return result4;
				}
				if (num < -10)
				{
					return result5;
				}
				if (character.Occupation == Occupation.GangLeader && character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) < 0)
				{
					return result10;
				}
				if (character.Occupation == Occupation.GangLeader && flag3)
				{
					return result10;
				}
				Clan clan = character.HeroObject.Clan;
				if (clan == null || !clan.IsNoble)
				{
					return result3;
				}
				if (character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) + character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) < 0)
				{
					return result13;
				}
				return result2;
			}
			if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaCurt)
			{
				if (num > 4)
				{
					return result7;
				}
				if (num < -20)
				{
					return result4;
				}
				if (character.Occupation == Occupation.GangLeader && flag3)
				{
					return result19;
				}
				if (flag2)
				{
					return result15;
				}
				return result14;
			}
			if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaSoftspoken)
			{
				if (num > 4)
				{
					return result7;
				}
				if (num < -20)
				{
					return result20;
				}
				Clan clan2 = character.HeroObject.Clan;
				if ((clan2 == null || !clan2.IsNoble) && flag3 && !character.IsFemale && flag2)
				{
					return result6;
				}
				if ((character.HeroObject.Clan?.IsNoble ?? false) && flag3 && !character.IsFemale && flag2)
				{
					return result12;
				}
				if (flag)
				{
					return result8;
				}
				return result11;
			}
			if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaEarnest)
			{
				if (num > 4)
				{
					return result7;
				}
				if (num < -40)
				{
					return result17;
				}
				if (num < -20)
				{
					return result16;
				}
				if ((character.HeroObject.Clan?.IsNoble ?? false) && flag2)
				{
					return result10;
				}
				if (flag)
				{
					return result8;
				}
				return result;
			}
		}
		else if (character.Occupation == Occupation.Villager || character.Occupation == Occupation.Townsfolk)
		{
			int deterministicHashCode = character.StringId.GetDeterministicHashCode();
			if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.Town != null && Settlement.CurrentSettlement.Town.Prosperity < (float)(200 * ((!Settlement.CurrentSettlement.IsTown) ? 1 : 5)) && deterministicHashCode % 2 == 0)
			{
				return result18;
			}
			if (deterministicHashCode % 2 == 1)
			{
				return result9;
			}
		}
		else if (flag4 && character.Occupation == Occupation.Bandit)
		{
			return result16;
		}
		return result;
	}

	private static int GetSuperiorityState(CharacterObject character)
	{
		if (Hero.MainHero.MapFaction != null && Hero.MainHero.MapFaction.Leader == Hero.MainHero && character.HeroObject.MapFaction == Hero.MainHero.MapFaction)
		{
			return -1;
		}
		if (character.IsHero && character.HeroObject.MapFaction != null && character.HeroObject.MapFaction.IsKingdomFaction)
		{
			Clan clan = character.HeroObject.Clan;
			if (clan != null && clan.IsNoble)
			{
				return 1;
			}
		}
		if (character.Occupation == Occupation.Villager || character.Occupation == Occupation.Townsfolk || character.Occupation == Occupation.Bandit || character.Occupation == Occupation.Gangster || character.Occupation == Occupation.Wanderer)
		{
			return -1;
		}
		return 0;
	}

	private static bool MorePowerThanPlayer(CharacterObject otherCharacter)
	{
		float num = 0f;
		num = ((otherCharacter.HeroObject.PartyBelongedTo == null) ? otherCharacter.HeroObject.Power : otherCharacter.HeroObject.PartyBelongedTo.Party.CalculateCurrentStrength());
		float num2 = MobileParty.MainParty.Party.CalculateCurrentStrength();
		return num > num2;
	}

	public static CharacterObject FindUpgradeRootOf(CharacterObject character)
	{
		foreach (CharacterObject item in CharacterObject.All)
		{
			if (item.IsBasicTroop && UpgradeTreeContains(item, item, character))
			{
				return item;
			}
		}
		return character;
	}

	private static bool UpgradeTreeContains(CharacterObject rootTroop, CharacterObject baseTroop, CharacterObject character)
	{
		if (baseTroop == character)
		{
			return true;
		}
		for (int i = 0; i < baseTroop.UpgradeTargets.Length; i++)
		{
			if (baseTroop.UpgradeTargets[i] == rootTroop)
			{
				return false;
			}
			if (UpgradeTreeContains(rootTroop, baseTroop.UpgradeTargets[i], character))
			{
				return true;
			}
		}
		return false;
	}

	public static ItemObject GetDefaultWeapon(CharacterObject affectorCharacter)
	{
		for (int i = 0; i <= 4; i++)
		{
			EquipmentElement equipmentFromSlot = affectorCharacter.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item?.PrimaryWeapon != null && equipmentFromSlot.Item.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.WeaponMask))
			{
				return equipmentFromSlot.Item;
			}
		}
		return null;
	}

	public static bool CanUseItem(BasicCharacterObject currentCharacter, EquipmentElement itemRosterElement)
	{
		TextObject reason;
		return CanUseItem(currentCharacter, itemRosterElement, out reason);
	}

	public static bool CanUseItem(BasicCharacterObject currentCharacter, EquipmentElement itemRosterElement, out TextObject reason)
	{
		bool result = true;
		reason = null;
		ItemObject item = itemRosterElement.Item;
		SkillObject relevantSkill = item.RelevantSkill;
		if (relevantSkill != null && currentCharacter.GetSkillValue(relevantSkill) < item.Difficulty)
		{
			reason = new TextObject("{=rgqA29b8}You don't have enough {SKILL_NAME} skill to equip this item");
			reason.SetTextVariable("SKILL_NAME", itemRosterElement.Item.RelevantSkill.Name);
			result = false;
		}
		bool num = (!currentCharacter.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale)) && (currentCharacter.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale));
		bool flag = item.StringId.Equals("dragon_banner_center") || item.StringId.Equals("dragon_banner_dragonhead") || item.StringId.Equals("dragon_banner_handle");
		if (!num || flag || (itemRosterElement.Item.HasHorseComponent && !itemRosterElement.Item.HorseComponent.IsRideable))
		{
			reason = new TextObject("{=ITKb4cKv}{ITEM_NAME} is not equippable.");
			reason.SetTextVariable("ITEM_NAME", itemRosterElement.GetModifiedItemName());
			result = false;
		}
		return result;
	}

	public static int GetPartyMemberFaceSeed(PartyBase party, BasicCharacterObject character, int rank)
	{
		int num = party.Index * 171 + character.StringId.GetDeterministicHashCode() * 6791 + rank * 197;
		return ((num >= 0) ? num : (-num)) % 2000;
	}

	public static int GetDefaultFaceSeed(BasicCharacterObject character, int rank)
	{
		return character.GetDefaultFaceSeed(rank);
	}

	public static bool SearchForFormationInTroopTree(CharacterObject baseTroop, FormationClass formation)
	{
		if (baseTroop.UpgradeTargets.Length == 0 && baseTroop.DefaultFormationClass == formation)
		{
			return true;
		}
		CharacterObject[] upgradeTargets = baseTroop.UpgradeTargets;
		foreach (CharacterObject characterObject in upgradeTargets)
		{
			if (characterObject.Level > baseTroop.Level && SearchForFormationInTroopTree(characterObject, formation))
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<CharacterObject> GetTroopTree(CharacterObject baseTroop, float minTier = -1f, float maxTier = float.MaxValue)
	{
		MBQueue<CharacterObject> queue = new MBQueue<CharacterObject>();
		queue.Enqueue(baseTroop);
		while (queue.Count > 0)
		{
			CharacterObject character = queue.Dequeue();
			if ((float)character.Tier >= minTier && (float)character.Tier <= maxTier)
			{
				yield return character;
			}
			CharacterObject[] upgradeTargets = character.UpgradeTargets;
			foreach (CharacterObject item in upgradeTargets)
			{
				queue.Enqueue(item);
			}
		}
	}

	public static void DeleteQuestCharacter(CharacterObject character, Settlement questSettlement)
	{
		if (questSettlement != null)
		{
			IList<LocationCharacter> listOfCharacters = questSettlement.LocationComplex.GetListOfCharacters();
			if (listOfCharacters.Any((LocationCharacter x) => x.Character == character))
			{
				LocationCharacter locationCharacter = listOfCharacters.First((LocationCharacter x) => x.Character == character);
				questSettlement.LocationComplex.RemoveCharacterIfExists(locationCharacter);
			}
		}
		Game.Current.ObjectManager.UnregisterObject(character);
	}

	public static CharacterObject GetRandomCompanionTemplateWithPredicate(Func<CharacterObject, bool> predicate = null)
	{
		if (predicate == null)
		{
			return MBObjectManager.Instance.GetObjectTypeList<CharacterObject>().GetRandomElementWithPredicate((CharacterObject x) => x.IsTemplate && x.Occupation == Occupation.Wanderer);
		}
		return MBObjectManager.Instance.GetObjectTypeList<CharacterObject>().GetRandomElementWithPredicate((CharacterObject x) => x.IsTemplate && x.Occupation == Occupation.Wanderer && predicate(x));
	}
}
