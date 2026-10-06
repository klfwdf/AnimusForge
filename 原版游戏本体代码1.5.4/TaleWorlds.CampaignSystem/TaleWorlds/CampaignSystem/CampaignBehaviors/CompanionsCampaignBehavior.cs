using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CompanionsCampaignBehavior : CampaignBehaviorBase
{
	private enum CompanionTemplateType
	{
		Engineering,
		Tactics,
		Leadership,
		Steward,
		Trade,
		Roguery,
		Medicine,
		Smithing,
		Scouting,
		Combat,
		Sailor
	}

	private const int CompanionMoveRandomIndex = 2;

	private const float DesiredCompanionPerTown = 0.6f;

	private const float KillChance = 0.1f;

	private const int SkillThresholdValue = 20;

	private const int RemoveWandererAfterDays = 40;

	private IReadOnlyDictionary<CompanionTemplateType, List<CharacterObject>> _companionsOfTemplates = new Dictionary<CompanionTemplateType, List<CharacterObject>>
	{
		{
			CompanionTemplateType.Engineering,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Tactics,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Leadership,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Steward,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Trade,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Roguery,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Medicine,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Smithing,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Scouting,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Combat,
			new List<CharacterObject>()
		},
		{
			CompanionTemplateType.Sailor,
			new List<CharacterObject>()
		}
	};

	private HashSet<CharacterObject> _aliveCompanionTemplates = new HashSet<CharacterObject>();

	private const float EngineerScore = 2f;

	private const float TacticsScore = 4f;

	private const float LeadershipScore = 3f;

	private const float StewardScore = 3f;

	private const float TradeScore = 3f;

	private const float RogueryScore = 4f;

	private const float MedicineScore = 3f;

	private const float SmithingScore = 2f;

	private const float ScoutingScore = 5f;

	private const float CombatScore = 5f;

	private const float SailorScore = 4f;

	private const float AllScore = 38f;

	private float _desiredTotalCompanionCount => (float)Town.AllTowns.Count * 0.6f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.HeroOccupationChangedEvent.AddNonSerializedListener(this, OnHeroOccupationChanged);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
	}

	private void OnGameLoadFinished()
	{
		InitializeCompanionTemplateList();
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero.IsWanderer)
			{
				AddToAliveCompanions(allAliveHero);
			}
		}
		foreach (Hero deadOrDisabledHero in Hero.DeadOrDisabledHeroes)
		{
			if (deadOrDisabledHero.IsAlive && deadOrDisabledHero.IsWanderer)
			{
				AddToAliveCompanions(deadOrDisabledHero);
			}
		}
	}

	private void DailyTick()
	{
		TryKillCompanion();
		SwapCompanions();
		TrySpawnNewCompanion();
	}

	private void WeeklyTick()
	{
		foreach (Hero item in Hero.DeadOrDisabledHeroes.ToList())
		{
			if (item.IsWanderer && item.DeathDay.ElapsedDaysUntilNow >= 40f)
			{
				Campaign.Current.CampaignObjectManager.UnregisterDeadHero(item);
			}
		}
	}

	private void RemoveFromAliveCompanions(Hero companion)
	{
		CharacterObject template = companion.Template;
		if (_aliveCompanionTemplates.Contains(template))
		{
			_aliveCompanionTemplates.Remove(template);
		}
	}

	private void AddToAliveCompanions(Hero companion, bool isTemplateControlled = false)
	{
		CharacterObject template = companion.Template;
		bool flag = true;
		if (!isTemplateControlled)
		{
			flag = IsTemplateKnown(template);
		}
		if (flag && !_aliveCompanionTemplates.Contains(template))
		{
			_aliveCompanionTemplates.Add(template);
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		RemoveFromAliveCompanions(victim);
		if (victim.IsWanderer && !victim.HasMet)
		{
			Campaign.Current.CampaignObjectManager.UnregisterDeadHero(victim);
		}
	}

	private void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
	{
		if (oldOccupation == Occupation.Wanderer)
		{
			RemoveFromAliveCompanions(hero);
		}
		else if (hero.Occupation == Occupation.Wanderer)
		{
			AddToAliveCompanions(hero);
		}
	}

	private void OnHeroCreated(Hero hero, bool showNotification = true)
	{
		if (hero.IsAlive && hero.IsWanderer)
		{
			AddToAliveCompanions(hero, isTemplateControlled: true);
		}
	}

	private void TryKillCompanion()
	{
		if (!(MBRandom.RandomFloat <= 0.1f) || _aliveCompanionTemplates.Count <= 0)
		{
			return;
		}
		CharacterObject randomElementInefficiently = _aliveCompanionTemplates.GetRandomElementInefficiently();
		Hero hero = null;
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero.Template == randomElementInefficiently && allAliveHero.IsWanderer)
			{
				hero = allAliveHero;
				break;
			}
		}
		if (hero != null && hero.CompanionOf == null && (hero.CurrentSettlement == null || hero.CurrentSettlement != Hero.MainHero.CurrentSettlement))
		{
			KillCharacterAction.ApplyByRemove(hero);
		}
	}

	private void TrySpawnNewCompanion()
	{
		if (!((float)_aliveCompanionTemplates.Count < _desiredTotalCompanionCount))
		{
			return;
		}
		Settlement settlement = Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Settlement != Hero.MainHero.CurrentSettlement && x.Settlement.SiegeEvent == null && x.Settlement.HeroesWithoutParty.AllQ((Hero y) => !y.IsWanderer || y.CompanionOf != null))?.Settlement;
		if (settlement != null)
		{
			CreateCompanionAndAddToSettlement(settlement);
		}
	}

	private void SwapCompanions()
	{
		int num = Town.AllTowns.Count / 2;
		int num2 = MBRandom.RandomInt(Town.AllTowns.Count % 2);
		Town town = Town.AllTowns[num2 + MBRandom.RandomInt(num)];
		Hero hero = town.Settlement.HeroesWithoutParty.Where((Hero x) => x.IsWanderer && x.CompanionOf == null).GetRandomElementInefficiently();
		for (int num3 = 1; num3 < 2; num3++)
		{
			Town town2 = Town.AllTowns[num3 * num + num2 + MBRandom.RandomInt(num)];
			IEnumerable<Hero> enumerable = town2.Settlement.HeroesWithoutParty.Where((Hero x) => x.IsWanderer && x.CompanionOf == null);
			Hero hero2 = null;
			if (enumerable.Any())
			{
				hero2 = enumerable.GetRandomElementInefficiently();
				LeaveSettlementAction.ApplyForCharacterOnly(hero2);
			}
			if (hero != null)
			{
				EnterSettlementAction.ApplyForCharacterOnly(hero, town2.Settlement);
			}
			hero = hero2;
		}
		if (hero != null)
		{
			EnterSettlementAction.ApplyForCharacterOnly(hero, town.Settlement);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		InitializeCompanionTemplateList();
		List<Town> list = Town.AllTowns.ToListQ();
		list.Shuffle();
		for (int i = 0; (float)i < _desiredTotalCompanionCount; i++)
		{
			CreateCompanionAndAddToSettlement(list[i].Settlement);
		}
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		InitializeCompanionTemplateList();
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero.IsWanderer)
			{
				AddToAliveCompanions(allAliveHero);
			}
		}
		foreach (Hero deadOrDisabledHero in Hero.DeadOrDisabledHeroes)
		{
			if (deadOrDisabledHero.IsAlive && deadOrDisabledHero.IsWanderer)
			{
				AddToAliveCompanions(deadOrDisabledHero);
			}
		}
	}

	private void AdjustEquipments(Hero hero)
	{
		AdjustEquipmentModifiers(hero.BattleEquipment);
		AdjustEquipmentModifiers(hero.CivilianEquipment);
	}

	private void AdjustEquipmentModifiers(Equipment equipment)
	{
		ItemModifier itemModifier = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
		ItemModifier itemModifier2 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
		ItemModifier itemModifier3 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			EquipmentElement equipmentElement = equipment[equipmentIndex];
			if (equipmentElement.Item != null)
			{
				if (equipmentElement.Item.ArmorComponent != null)
				{
					equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, itemModifier);
				}
				else if (equipmentElement.Item.HorseComponent != null)
				{
					equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, itemModifier3);
				}
				else if (equipmentElement.Item.WeaponComponent != null)
				{
					equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, itemModifier2);
				}
			}
		}
	}

	private void InitializeCompanionTemplateList()
	{
		foreach (CharacterObject objectType in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
		{
			if (objectType.IsTemplate && objectType.Occupation == Occupation.Wanderer)
			{
				_companionsOfTemplates[GetTemplateTypeOfCompanion(objectType)].Add(objectType);
			}
		}
	}

	private CompanionTemplateType GetTemplateTypeOfCompanion(CharacterObject character)
	{
		if (character.IsMariner)
		{
			return CompanionTemplateType.Sailor;
		}
		CompanionTemplateType result = CompanionTemplateType.Combat;
		int num = 20;
		foreach (SkillObject item in Skills.All)
		{
			int skillValue = character.GetSkillValue(item);
			if (skillValue > num)
			{
				CompanionTemplateType templateTypeForSkill = GetTemplateTypeForSkill(item);
				if (templateTypeForSkill != CompanionTemplateType.Combat)
				{
					num = skillValue;
					result = templateTypeForSkill;
				}
			}
		}
		return result;
	}

	private void CreateCompanionAndAddToSettlement(Settlement settlement)
	{
		CharacterObject companionTemplate = GetCompanionTemplateToSpawn();
		if (companionTemplate != null)
		{
			Settlement settlement2 = Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Culture == companionTemplate.Culture)?.Settlement;
			if (settlement2 == null)
			{
				settlement2 = Town.AllTowns.GetRandomElement().Settlement;
			}
			Hero hero = HeroCreator.CreateSpecialHero(companionTemplate, settlement2, null, null, Campaign.Current.Models.AgeModel.HeroComesOfAge + 5 + MBRandom.RandomInt(12));
			AdjustEquipments(hero);
			hero.ChangeState(Hero.CharacterStates.Active);
			EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
		}
	}

	private CompanionTemplateType GetCompanionTemplateTypeToSpawn()
	{
		CompanionTemplateType result = CompanionTemplateType.Combat;
		float num = -1f;
		foreach (KeyValuePair<CompanionTemplateType, List<CharacterObject>> companionsOfTemplate in _companionsOfTemplates)
		{
			float templateTypeScore = GetTemplateTypeScore(companionsOfTemplate.Key);
			if (!(templateTypeScore > 0f))
			{
				continue;
			}
			int num2 = 0;
			foreach (CharacterObject item in companionsOfTemplate.Value)
			{
				if (_aliveCompanionTemplates.Contains(item))
				{
					num2++;
				}
			}
			float num3 = (float)num2 / _desiredTotalCompanionCount;
			float num4 = (templateTypeScore - num3) / templateTypeScore;
			if (num2 < companionsOfTemplate.Value.Count && num4 > num)
			{
				num = num4;
				result = companionsOfTemplate.Key;
			}
		}
		return result;
	}

	private bool IsTemplateKnown(CharacterObject companionTemplate)
	{
		foreach (KeyValuePair<CompanionTemplateType, List<CharacterObject>> companionsOfTemplate in _companionsOfTemplates)
		{
			for (int i = 0; i < companionsOfTemplate.Value.Count; i++)
			{
				if (companionTemplate == companionsOfTemplate.Value[i])
				{
					return true;
				}
			}
		}
		return false;
	}

	private CharacterObject GetCompanionTemplateToSpawn()
	{
		List<CharacterObject> list = _companionsOfTemplates[GetCompanionTemplateTypeToSpawn()];
		list.Shuffle();
		CharacterObject result = null;
		foreach (CharacterObject item in list)
		{
			if (!_aliveCompanionTemplates.Contains(item))
			{
				result = item;
				break;
			}
		}
		return result;
	}

	private float GetTemplateTypeScore(CompanionTemplateType templateType)
	{
		return templateType switch
		{
			CompanionTemplateType.Engineering => 1f / 19f, 
			CompanionTemplateType.Tactics => 0.10526316f, 
			CompanionTemplateType.Leadership => 3f / 38f, 
			CompanionTemplateType.Steward => 3f / 38f, 
			CompanionTemplateType.Trade => 3f / 38f, 
			CompanionTemplateType.Roguery => 0.10526316f, 
			CompanionTemplateType.Medicine => 3f / 38f, 
			CompanionTemplateType.Smithing => 1f / 19f, 
			CompanionTemplateType.Scouting => 5f / 38f, 
			CompanionTemplateType.Combat => 5f / 38f, 
			CompanionTemplateType.Sailor => 0.10526316f, 
			_ => 0f, 
		};
	}

	private CompanionTemplateType GetTemplateTypeForSkill(SkillObject skill)
	{
		CompanionTemplateType result = CompanionTemplateType.Combat;
		if (skill == DefaultSkills.Engineering)
		{
			result = CompanionTemplateType.Engineering;
		}
		else if (skill == DefaultSkills.Tactics)
		{
			result = CompanionTemplateType.Tactics;
		}
		else if (skill == DefaultSkills.Leadership)
		{
			result = CompanionTemplateType.Leadership;
		}
		else if (skill == DefaultSkills.Steward)
		{
			result = CompanionTemplateType.Steward;
		}
		else if (skill == DefaultSkills.Trade)
		{
			result = CompanionTemplateType.Trade;
		}
		else if (skill == DefaultSkills.Roguery)
		{
			result = CompanionTemplateType.Roguery;
		}
		else if (skill == DefaultSkills.Medicine)
		{
			result = CompanionTemplateType.Medicine;
		}
		else if (skill == DefaultSkills.Crafting)
		{
			result = CompanionTemplateType.Smithing;
		}
		else if (skill == DefaultSkills.Scouting)
		{
			result = CompanionTemplateType.Scouting;
		}
		return result;
	}
}
