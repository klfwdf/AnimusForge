using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Incidents;

public class IncidentEffect
{
	private readonly Func<bool> _condition;

	private readonly Func<List<TextObject>> _consequence;

	private Func<IncidentEffect, List<TextObject>> _hint;

	private Func<List<TextObject>> _customInformation;

	private float _chanceToOccur = 1f;

	private IncidentEffect(Func<bool> condition, Func<List<TextObject>> consequence, Func<IncidentEffect, List<TextObject>> hint)
	{
		_condition = condition;
		_consequence = consequence;
		_hint = hint;
	}

	public bool Condition()
	{
		if (_condition != null)
		{
			return _condition();
		}
		return true;
	}

	public List<TextObject> Consequence()
	{
		List<TextObject> result = new List<TextObject>();
		if (MBRandom.RandomFloat <= _chanceToOccur)
		{
			result = _consequence?.Invoke();
			if (_customInformation != null)
			{
				result = _customInformation();
			}
		}
		return result;
	}

	public List<TextObject> GetHint()
	{
		return _hint?.Invoke(this);
	}

	public IncidentEffect WithChance(float chance)
	{
		_chanceToOccur = chance;
		return this;
	}

	public IncidentEffect WithCustomInformation(Func<List<TextObject>> customInformation)
	{
		_customInformation = customInformation;
		return this;
	}

	public IncidentEffect WithHint(Func<IncidentEffect, List<TextObject>> hint)
	{
		_hint = hint;
		return this;
	}

	public static IncidentEffect GoldChange(Func<int> amountGetter)
	{
		return new IncidentEffect(delegate
		{
			int num = amountGetter();
			return Hero.MainHero.Gold >= -num;
		}, delegate
		{
			int num = amountGetter();
			if (num > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, num);
			}
			else
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, TaleWorlds.Library.MathF.Abs(num));
			}
			return new List<TextObject>();
		}, delegate(IncidentEffect effect)
		{
			int variable = amountGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=YGgPUH3r}{?AMOUNT > 0}Earn{?}Lose{\\?} {ABS(AMOUNT)}{GOLD_ICON}");
			}
			else
			{
				textObject = new TextObject("{=Lo1o8fqp}{CHANCE}% chance to {?AMOUNT > 0}earn{?}lose{\\?} {ABS(AMOUNT)}{GOLD_ICON}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", variable);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect TraitChange(TraitObject trait, int amount)
	{
		return new IncidentEffect(null, delegate
		{
			TraitLevelingHelper.OnIncidentResolved(trait, amount);
			TextObject textObject = new TextObject("{=UM8ZOtar}Increased reputation for being {TRAIT}.");
			textObject.SetTextVariable("TRAIT", HeroHelper.GetPersonalityTraitChangeName(trait, Hero.MainHero, amount >= 0));
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=fnkkMRl0}Increase reputation for being {TRAIT}");
			}
			else
			{
				textObject = new TextObject("{=9mGtDERC}{CHANCE}% chance of increasing reputation for being {TRAIT}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("TRAIT", HeroHelper.GetPersonalityTraitChangeName(trait, Hero.MainHero, amount >= 0));
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect BuildingLevelChange(Func<Building> buildingGetter, Func<int> amountGetter)
	{
		return new IncidentEffect(delegate
		{
			int? num = amountGetter?.Invoke();
			Building building = buildingGetter?.Invoke();
			if (building == null || num == 0)
			{
				return false;
			}
			if (num > 0 && building.CurrentLevel + num <= 3)
			{
				return true;
			}
			return num < 0 && building.CurrentLevel + num >= building.BuildingType.StartLevel;
		}, delegate
		{
			int num = amountGetter();
			Building building = buildingGetter();
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					int constructionCost = building.GetConstructionCost();
					building.CurrentLevel++;
					building.BuildingProgress -= constructionCost;
				}
			}
			else
			{
				building.CurrentLevel += num;
				building.BuildingProgress = 0f;
			}
			CampaignEventDispatcher.Instance.OnBuildingLevelChanged(building.Town, building, num);
			TextObject textObject = new TextObject("{=bdfoDUM0}{?AMOUNT > 0}Increased{?}Decreased{\\?} {BUILDING} level by {ABS(AMOUNT)}.");
			textObject.SetTextVariable("BUILDING", building.Name);
			textObject.SetTextVariable("AMOUNT", num);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			int variable = amountGetter();
			Building building = buildingGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=nAft4SaD}{?AMOUNT > 0}Increase{?}Decrease{\\?} {BUILDING} level by {ABS(AMOUNT)}.");
			}
			else
			{
				textObject = new TextObject("{=BaffFLtX}{CHANCE}% chance of {=*}{?AMOUNT > 0}increasing{?}decreasing{\\?} {BUILDING} level by {ABS(AMOUNT)}.");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("BUILDING", building.Name);
			textObject.SetTextVariable("AMOUNT", variable);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect SiegeProgressChange(Func<float> amountGetter)
	{
		return new IncidentEffect(() => PlayerSiege.PlayerSiegeEvent?.BesiegerCamp?.SiegeEngines?.SiegePreparations != null && !PlayerSiege.PlayerSiegeEvent.BesiegerCamp.IsPreparationComplete, delegate
		{
			float num = amountGetter();
			PlayerSiege.PlayerSiegeEvent.BesiegerCamp.SiegeEngines.SiegePreparations.SetProgress(PlayerSiege.PlayerSiegeEvent.BesiegerCamp.SiegeEngines.SiegePreparations.Progress + num);
			TextObject textObject = new TextObject("{=C0kUpB48}{?AMOUNT > 0}Increased{?}Decreased{\\?} siege progress by {ABS(AMOUNT)}%.");
			textObject.SetTextVariable("AMOUNT", TaleWorlds.Library.MathF.Round(num * 100f));
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			float num = amountGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=aqE0C4s8}{?AMOUNT > 0}Increase{?}Decrease{\\?} siege progress by {ABS(AMOUNT)}%");
			}
			else
			{
				textObject = new TextObject("{=BaffFLtX}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} siege progress by {ABS(AMOUNT)}%");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", TaleWorlds.Library.MathF.Round(num * 100f));
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WorkshopProfitabilityChange(Func<Workshop> workshopGetter, float percentage)
	{
		return new IncidentEffect(() => workshopGetter?.Invoke() != null, delegate
		{
			Workshop workshop = workshopGetter();
			int num = TaleWorlds.Library.MathF.Round((float)workshop.ProfitMade * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction()) * percentage);
			workshop.ChangeGold(num);
			TextObject textObject = new TextObject("{=9bIi78RK}{WORKSHOP} {?PERCENTAGE > 0}gained{?}lost{\\?} {AMOUNT} gold.");
			textObject.SetTextVariable("WORKSHOP", workshop.Name);
			textObject.SetTextVariable("PERCENTAGE", percentage);
			textObject.SetTextVariable("AMOUNT", Math.Abs(num));
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=rMWw9ieF}Workshop {?PERCENTAGE > 0}gains{?}loses{\\?} {ABS(PERCENTAGE)}% of its revenue.");
			}
			else
			{
				textObject = new TextObject("{=s8DBEakZ}{CHANCE}% chance of workshop {?PERCENTAGE > 0}gaining{?}losing{\\?} {ABS(PERCENTAGE)}% of its revenue.");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("PERCENTAGE", percentage * 100f);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect SkillChange(SkillObject skill, float amount)
	{
		return new IncidentEffect(() => true, delegate
		{
			ExplainedNumber explainedNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(Hero.MainHero.CharacterAttributes, Hero.MainHero.HeroDeveloper.GetFocus(skill), Hero.MainHero.GetSkillValue(skill), skill);
			bool flag = explainedNumber.ResultNumber >= 1f;
			Hero.MainHero.HeroDeveloper.AddSkillXp(skill, amount, flag);
			float num = amount;
			if (flag)
			{
				num *= explainedNumber.ResultNumber;
			}
			TextObject textObject = new TextObject("{=ySoK6FLl}{?AMOUNT > 0}Gained{?}Lost{\\?} {ABS(AMOUNT)} {SKILL} XP.");
			textObject.SetTextVariable("AMOUNT", TaleWorlds.Library.MathF.Round(num));
			textObject.SetTextVariable("SKILL", skill.Name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=7aKxuBVH}+{AMOUNT} XP {SKILL}");
			}
			else
			{
				textObject = new TextObject("{=ZucFKCqy}{CHANCE}% chance of +{AMOUNT} XP {SKILL}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			ExplainedNumber explainedNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(Hero.MainHero.CharacterAttributes, Hero.MainHero.HeroDeveloper.GetFocus(skill), Hero.MainHero.GetSkillValue(skill), skill);
			bool num = explainedNumber.ResultNumber >= 1f;
			float num2 = amount;
			if (num)
			{
				num2 *= explainedNumber.ResultNumber;
			}
			textObject.SetTextVariable("AMOUNT", TaleWorlds.Library.MathF.Round(num2));
			textObject.SetTextVariable("SKILL", skill.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect MoraleChange(float amount)
	{
		return new IncidentEffect(null, delegate
		{
			MobileParty.MainParty.RecentEventsMorale += amount;
			TextObject textObject = new TextObject("{=QG50JVu8}{?AMOUNT > 0}Increased{?}Decreased{\\?} morale by {ABS(AMOUNT)}");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=YAyISGjV}{?AMOUNT > 0}Increase{?}Decrease{\\?} morale by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=I8WRkX2a}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} morale by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect HealthChance(int amount)
	{
		return new IncidentEffect(null, delegate
		{
			Hero.MainHero.HitPoints += amount;
			TextObject textObject = new TextObject("{=sPa4O70I}{?AMOUNT > 0}Healed{?}Lost{\\?} {ABS(AMOUNT)} hit points.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=94UTHJCl}{?AMOUNT > 0}Heal{?}Lose{\\?} {ABS(AMOUNT)} hit points");
			}
			else
			{
				textObject = new TextObject("{=t7YszJ1w}{CHANCE}% chance of {?AMOUNT > 0}healing{?}losing{\\?} {ABS(AMOUNT)} hit points");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect RenownChange(float amount)
	{
		return new IncidentEffect(() => true, delegate
		{
			GainRenownAction.Apply(Hero.MainHero, amount);
			TextObject textObject = new TextObject("{=NHzq8L83}Gained {AMOUNT} renown.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=noOOudW8}Gain {AMOUNT} renown");
			}
			else
			{
				textObject = new TextObject("{=6a8nEuqX}{CHANCE}% chance of gaining {AMOUNT} renown");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect CrimeRatingChange(Func<IFaction> factionGetter, float amount)
	{
		return new IncidentEffect(() => factionGetter?.Invoke() != null, delegate
		{
			ChangeCrimeRatingAction.Apply(factionGetter(), amount);
			TextObject textObject = new TextObject("{=V2CGB9Sw}{?AMOUNT > 0}Increased{?}Decreased{\\?} crime rating by {ABS(AMOUNT)} in {FACTION}.");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("FACTION", factionGetter().Name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=t01zXDvG}{?AMOUNT > 0}Increase{?}Decrease{\\?} crime rating by {ABS(AMOUNT)} in {FACTION}");
			}
			else
			{
				textObject = new TextObject("{=b029BWMC}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} crime rating by {ABS(AMOUNT)} in {FACTION}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("FACTION", factionGetter().Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect InfluenceChange(float amount)
	{
		return new IncidentEffect(() => Hero.MainHero.Clan.Kingdom != null, delegate
		{
			float num = ((Hero.MainHero.Clan.Influence + amount > 0f) ? amount : (0f - Hero.MainHero.Clan.Influence));
			GainKingdomInfluenceAction.ApplyForDefault(Hero.MainHero, num);
			TextObject textObject = new TextObject("{=MW0ah7pi}{?AMOUNT > 0}Gained{?}Lost{\\?} {ABS(AMOUNT)} influence.");
			textObject.SetTextVariable("AMOUNT", num);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			float variable = ((Hero.MainHero.Clan.Influence + amount > 0f) ? amount : (0f - Hero.MainHero.Clan.Influence));
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=3a3D6aAt}{?AMOUNT > 0}Gain{?}Lose{\\?} {ABS(AMOUNT)} influence");
			}
			else
			{
				textObject = new TextObject("{=3K85XqUs}{CHANCE}% chance of {?AMOUNT > 0}gaining{?}losing{\\?} {ABS(AMOUNT)} influence");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", variable);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect SettlementRelationChange(Func<Settlement> settlementGetter, int amount)
	{
		return new IncidentEffect(() => settlementGetter?.Invoke() != null, delegate
		{
			Settlement settlement = settlementGetter();
			List<TextObject> list = new List<TextObject>();
			foreach (Hero notable in settlement.Notables)
			{
				ChangeRelationAction.ApplyPlayerRelation(notable, amount);
				TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.");
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("SETTLEMENT", settlement.Name);
				list.Add(textObject);
			}
			return list;
		}, delegate(IncidentEffect effect)
		{
			Settlement settlement = settlementGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=PaKstm1Q}{?AMOUNT > 0}Increase{?}Decrease{\\?} relationship with notables in {SETTLEMENT} by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=8yruI4lJ}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} relationship with notables in {SETTLEMENT} by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("SETTLEMENT", settlement.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect TownBoundVillageRelationChange(Func<Town> townGetter, int amount)
	{
		return new IncidentEffect(() => townGetter?.Invoke() != null && townGetter().Villages.Count > 0, delegate
		{
			Town town = townGetter();
			List<TextObject> list = new List<TextObject>();
			foreach (Village village in town.Villages)
			{
				foreach (Hero notable in village.Settlement.Notables)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, amount);
				}
				TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.");
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("SETTLEMENT", village.Name);
				list.Add(textObject);
			}
			return list;
		}, delegate(IncidentEffect effect)
		{
			Town town = townGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=SwEQJC4s}{?AMOUNT > 0}Increase{?}Decrease{\\?} relationship with each bound village of {SETTLEMENT} by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=bSMXHl3A}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} relationship with each bound village of {SETTLEMENT} by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("SETTLEMENT", town.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect TownBoundVillageHearthChange(Func<Town> townGetter, int amount)
	{
		return new IncidentEffect(() => townGetter?.Invoke() != null && townGetter().Villages.Count > 0, delegate
		{
			Town town = townGetter();
			List<TextObject> list = new List<TextObject>();
			foreach (Village village in town.Villages)
			{
				village.Hearth += amount;
				TextObject textObject = new TextObject("{=qMCaLtKm}{?AMOUNT > 0}Increased{?}Decreased{\\?} hearth of {SETTLEMENT} by {ABS(AMOUNT)}.");
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("SETTLEMENT", village.Name);
				list.Add(textObject);
			}
			return list;
		}, delegate(IncidentEffect effect)
		{
			Town town = townGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=lWOTWQdF}{?AMOUNT > 0}Increase{?}Decrease{\\?} hearth of each bound village of {SETTLEMENT} by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=pBNxbnBk}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} hearth of each bound village of {SETTLEMENT} by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("SETTLEMENT", town.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect VillageHearthChange(Func<Village> villageGetter, int amount)
	{
		return new IncidentEffect(() => villageGetter?.Invoke() != null, delegate
		{
			Village village = villageGetter();
			village.Hearth += amount;
			TextObject textObject = new TextObject("{=qMCaLtKm}{?AMOUNT > 0}Increased{?}Decreased{\\?} hearth of {SETTLEMENT} by {ABS(AMOUNT)}.");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("SETTLEMENT", village.Name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			Village village = villageGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=ZpE5eWj9}{?AMOUNT > 0}Increase{?}Decrease{\\?} {VILLAGE} hearth by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=bSTaPVG7}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {VILLAGE} hearth by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("VILLAGE", village.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect TownSecurityChange(Func<Town> townGetter, int amount)
	{
		return new IncidentEffect(() => townGetter?.Invoke() != null, delegate
		{
			Town town = townGetter();
			town.Security += amount;
			TextObject textObject = new TextObject("{=ShfAk7aC}{?AMOUNT > 0}Increased{?}Decreased{\\?} {TOWN} security by {ABS(AMOUNT)}.");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("TOWN", town.Name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			Town town = townGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=sJ9OpLKp}{?AMOUNT > 0}Increase{?}Decrease{\\?} {TOWN} security by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=6E7sQIfW}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {TOWN} security by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("TOWN", town.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect HeroRelationChange(Func<Hero> heroGetter, int amount)
	{
		return new IncidentEffect(() => heroGetter?.Invoke() != null, delegate
		{
			ChangeRelationAction.ApplyPlayerRelation(heroGetter(), amount);
			return new List<TextObject>();
		}, delegate(IncidentEffect effect)
		{
			Hero hero = heroGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=T8GYg3tv}{?AMOUNT > 0}Increase{?}Decrease{\\?} relationship with {HERO.NAME} by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=6tnegb19}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} relationship with {HERO.NAME} by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("HERO", hero.CharacterObject);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect TownProsperityChange(Func<Town> townGetter, int amount)
	{
		return new IncidentEffect(() => townGetter?.Invoke() != null, delegate
		{
			Town town = townGetter();
			town.Prosperity += amount;
			TextObject textObject = new TextObject("{=gd2Ppaae}{?AMOUNT > 0}Increased{?}Decreased{\\?} {TOWN}'s prosperity by {ABS(AMOUNT)}.");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("TOWN", town.Name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			Town town = townGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=sBoVFny0}{?AMOUNT > 0}Increase{?}Decrease{\\?} {TOWN}'s prosperity by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=CirbWpGB}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {TOWN}'s prosperity by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("TOWN", town.Name);
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect SettlementMilitiaChange(Func<Settlement> settlementGetter, int amount)
	{
		return new IncidentEffect(delegate
		{
			Settlement settlement = settlementGetter?.Invoke();
			return settlement != null && settlement.Militia > (float)(-amount);
		}, delegate
		{
			Settlement settlement = settlementGetter();
			settlement.Militia += amount;
			TextObject textObject = new TextObject("{=Zu4loCJR}{?AMOUNT > 0}Increased{?}Decreased{\\?} {SETTLEMENT}'s militia by {ABS(AMOUNT)}.");
			textObject.SetTextVariable("SETTLEMENT", settlement.Name);
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			Settlement settlement = settlementGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=UUXAl3un}{?AMOUNT > 0}Increase{?}Decrease{\\?} {SETTLEMENT}'s militia by {ABS(AMOUNT)}");
			}
			else
			{
				textObject = new TextObject("{=b2Iu3WsA}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {SETTLEMENT}'s militia by {ABS(AMOUNT)}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("SETTLEMENT", settlement.Name);
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect InfestNearbyHideout(Func<Settlement> settlementGetter)
	{
		return new IncidentEffect(() => settlementGetter?.Invoke() != null, delegate
		{
			Hideout hideout = SettlementHelper.FindNearestHideoutToSettlement(settlementGetter(), MobileParty.NavigationType.Default, (Settlement settlement) => settlement.IsHideout && !settlement.Hideout.IsSpotted);
			BanditSpawnCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<BanditSpawnCampaignBehavior>();
			if (hideout == null)
			{
				return new List<TextObject>();
			}
			if (!hideout.IsInfested)
			{
				int num = Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt - hideout.Settlement.Parties.Count((MobileParty x) => x.IsBandit);
				for (int num2 = 0; num2 < num; num2++)
				{
					behavior.AddBanditToHideout(hideout);
				}
			}
			hideout.IsSpotted = true;
			hideout.Settlement.IsVisible = false;
			CampaignEventDispatcher.Instance.OnHideoutSpotted(MobileParty.MainParty.Party, hideout.Settlement.Party);
			return new List<TextObject>();
		}, delegate(IncidentEffect effect)
		{
			Settlement settlement = settlementGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=VIMgmfp8}Infest a hideout nearby {SETTLEMENT}");
			}
			else
			{
				textObject = new TextObject("{=5HYYbGDe}{CHANCE}% chance of infesting a hideout nearby {SETTLEMENT}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("SETTLEMENT", settlement.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WoundTroopsRandomly(float percentage)
	{
		return new IncidentEffect(() => (float)(MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars) >= (float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage, delegate
		{
			int num = TaleWorlds.Library.MathF.Round((float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage);
			MobileParty.MainParty.MemberRoster.WoundNumberOfNonHeroTroopsRandomly(num);
			TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.");
			textObject.SetTextVariable("AMOUNT", num);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=v9Ad4uGn}{AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} gets wounded");
			}
			else
			{
				textObject = new TextObject("{=c1aajRyU}{CHANCE}% chance of {AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} getting wounded");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", percentage * 100f);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WoundTroopsRandomly(Func<TroopRosterElement, bool> predicate, Func<int> amountGetter, bool specifyUnitTypeOnHint = true)
	{
		return new IncidentEffect(delegate
		{
			List<TroopRosterElement> source = MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(predicate).ToList();
			int num = amountGetter();
			return source.Sum((TroopRosterElement x) => x.Number - x.WoundedNumber) > num;
		}, delegate
		{
			List<TroopRosterElement> list = MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(predicate).ToList();
			int num = amountGetter();
			TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.");
			textObject.SetTextVariable("AMOUNT", num);
			while (num > 0 && list.Any((TroopRosterElement x) => x.Number > x.WoundedNumber))
			{
				TroopRosterElement randomElementWithPredicate = list.GetRandomElementWithPredicate((TroopRosterElement x) => x.Number > x.WoundedNumber);
				int num2 = randomElementWithPredicate.Number - randomElementWithPredicate.WoundedNumber;
				int num3 = ((num > num2) ? num2 : MBRandom.RandomInt(1, num + 1));
				MobileParty.MainParty.MemberRoster.WoundTroop(randomElementWithPredicate.Character, num3);
				num -= num3;
			}
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (specifyUnitTypeOnHint)
			{
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=Q4j44aOp}{AMOUNT} {UNIT_TYPE} get wounded");
				}
				else
				{
					textObject = new TextObject("{=oXtob6vV}{CHANCE}% chance of {AMOUNT} {UNIT_TYPE} getting wounded");
					textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("UNIT_TYPE", MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(predicate).ToList()
					.GetRandomElement()
					.Character.DefaultFormationClass.GetName());
			}
			else if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=3SDefWu5}{AMOUNT} random troops get wounded");
			}
			else
			{
				textObject = new TextObject("{=Owc4NdbL}{CHANCE}% chance of {AMOUNT} random troops getting wounded");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WoundTroopsRandomlyWithChanceOfDeath(float percentage, float chanceOfDeathPerUnit)
	{
		return new IncidentEffect(() => (float)(MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars) >= (float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage, delegate
		{
			int num = TaleWorlds.Library.MathF.Round((float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage);
			MobilePartyHelper.WoundNumberOfNonHeroTroopsRandomlyWithChanceOfDeath(MobileParty.MainParty.MemberRoster, num, chanceOfDeathPerUnit, out var deathAmount);
			int num2 = num - deathAmount;
			List<TextObject> list = new List<TextObject>();
			if (deathAmount > 0)
			{
				if (num2 == 0)
				{
					TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.");
					textObject.SetTextVariable("AMOUNT", deathAmount);
					list.Add(textObject);
				}
				else
				{
					TextObject textObject2 = new TextObject("{=zXmhbszd}{AMOUNT} of your troops got wounded, and {AMOUNT_DEAD} died.");
					textObject2.SetTextVariable("AMOUNT", num2);
					textObject2.SetTextVariable("AMOUNT_DEAD", deathAmount);
					list.Add(textObject2);
				}
			}
			else
			{
				TextObject textObject3 = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.");
				textObject3.SetTextVariable("AMOUNT", num2);
				list.Add(textObject3);
			}
			return list;
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=lbS9uHyp}{AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} gets wounded and each unit has {DEATH_CHANCE}% chance of dying");
			}
			else
			{
				textObject = new TextObject("{=J6ppDY1Y}{CHANCE}% chance of {AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} getting wounded and each unit has {DEATH_CHANCE}% chance of dying");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("DEATH_CHANCE", TaleWorlds.Library.MathF.Round(chanceOfDeathPerUnit * 100f));
			textObject.SetTextVariable("AMOUNT", TaleWorlds.Library.MathF.Round(percentage * 100f));
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect BreachSiegeWall(int amount)
	{
		return new IncidentEffect(() => MobileParty.MainParty.SiegeEvent != null, delegate
		{
			MBReadOnlyList<float> hitPointRatioList = PlayerSiege.BesiegedSettlement.SettlementWallSectionHitPointsRatioList;
			int num = amount;
			List<int> list = (from x in Enumerable.Range(0, hitPointRatioList.Count)
				where hitPointRatioList[x] > 0f
				select x).ToList();
			while (num > 0 && list.Count > 0)
			{
				int randomElement = list.GetRandomElement();
				PlayerSiege.BesiegedSettlement.SetWallSectionHitPointsRatioAtIndex(randomElement, 0f);
				num--;
			}
			PlayerSiege.BesiegedSettlement.Party.SetVisualAsDirty();
			TextObject textObject = new TextObject("{=WXCl7J36}{?AMOUNT > 1}Walls{?}Wall{\\?} breached successfully.");
			textObject.SetTextVariable("AMOUNT", amount - num);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=zDqwzpYf}Breach {AMOUNT} siege {?AMOUNT > 1}walls{?}wall{\\?}");
			}
			else
			{
				textObject = new TextObject("{=KCLDqhsV}{CHANCE}% chance of breaching {AMOUNT} siege {?AMOUNT > 1}walls{?}wall{\\?}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WoundTroopsRandomly(int amount)
	{
		return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= amount, delegate
		{
			MobileParty.MainParty.MemberRoster.WoundNumberOfNonHeroTroopsRandomly(amount);
			TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=3K7NyOSr}{AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?} gets wounded");
			}
			else
			{
				textObject = new TextObject("{=CV7rDOtO}{CHANCE}% chance of {AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?} getting wounded");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WoundTroopsRandomlyWithChanceOfDeath(int amount, float chanceOfDeathPerUnit)
	{
		return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= amount, delegate
		{
			MobilePartyHelper.WoundNumberOfNonHeroTroopsRandomlyWithChanceOfDeath(MobileParty.MainParty.MemberRoster, amount, chanceOfDeathPerUnit, out var deathAmount);
			int num = amount - deathAmount;
			List<TextObject> list = new List<TextObject>();
			if (deathAmount > 0)
			{
				if (num == 0)
				{
					TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.");
					textObject.SetTextVariable("AMOUNT", deathAmount);
					list.Add(textObject);
				}
				else
				{
					TextObject textObject2 = new TextObject("{=zXmhbszd}{AMOUNT} of your troops got wounded, and {AMOUNT_DEAD} died.");
					textObject2.SetTextVariable("AMOUNT", num);
					textObject2.SetTextVariable("AMOUNT_DEAD", deathAmount);
					list.Add(textObject2);
				}
			}
			else
			{
				TextObject textObject3 = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.");
				textObject3.SetTextVariable("AMOUNT", num);
				list.Add(textObject3);
			}
			return list;
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=n5lPsPKq}{AMOUNT} of your troops {?AMOUNT > 1}get{?}gets{\\?} wounded and each unit has {DEATH_CHANCE}% chance of dying");
			}
			else
			{
				textObject = new TextObject("{=v679SFPt}{CHANCE}% chance of {AMOUNT} of your troops getting wounded and each unit has {DEATH_CHANCE}% chance of dying");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("DEATH_CHANCE", TaleWorlds.Library.MathF.Round(chanceOfDeathPerUnit * 100f));
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WoundTroop(Func<CharacterObject> characterGetter, int amount)
	{
		return new IncidentEffect(delegate
		{
			CharacterObject characterObject = characterGetter?.Invoke();
			if (characterObject != null)
			{
				int num = MobileParty.MainParty.MemberRoster.FindIndexOfTroop(characterObject);
				if (num == -1)
				{
					return false;
				}
				TroopRosterElement troopRosterElement = MobileParty.MainParty.MemberRoster.GetTroopRoster()[num];
				return troopRosterElement.Number - troopRosterElement.WoundedNumber >= amount;
			}
			return false;
		}, delegate
		{
			CharacterObject characterObject = characterGetter();
			MobileParty.MainParty.MemberRoster.WoundTroop(characterObject, amount);
			TextObject textObject = new TextObject("{=Cep8OD72}{AMOUNT} {TROOP.NAME} got wounded.");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("TROOP", characterObject);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			CharacterObject character = characterGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=wFtO2y5R}{AMOUNT} {TROOP.NAME} gets wounded");
			}
			else
			{
				textObject = new TextObject("{=PMgz8Dah}{CHANCE}% chance of {AMOUNT} {TROOP.NAME} getting wounded");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("TROOP", character);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect WoundTroopsRandomlyByChance(float chancePerUnit)
	{
		return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars > 0, delegate
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList();
			TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.");
			textObject.SetTextVariable("AMOUNT", 0);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				TroopRosterElement troopRosterElement = list[num];
				int num2;
				for (num2 = 0; num2 < troopRosterElement.Number - troopRosterElement.WoundedNumber; num2++)
				{
					if (!(MBRandom.RandomFloat < chancePerUnit))
					{
						break;
					}
				}
				textObject.SetTextVariable("AMOUNT", num2);
				MobileParty.MainParty.MemberRoster.WoundTroop(troopRosterElement.Character, num2);
			}
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=bsRI2wUz}Wound troops with {CHANCE_PER_UNIT}% chance each");
			}
			else
			{
				textObject = new TextObject("{=8k6NCb2S}{CHANCE}% chance of wounding troops with {CHANCE_PER_UNIT}% chance each");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("CHANCE_PER_UNIT", chancePerUnit);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect KillTroopsRandomlyOrderedByTier(Func<TroopRosterElement, bool> predicate, Func<int> amountGetter)
	{
		return new IncidentEffect(() => KillTroopsRandomly(predicate, amountGetter)._condition(), delegate
		{
			int num = amountGetter();
			int num2 = num;
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where predicate(x) && !x.Character.IsHero
				orderby x.Character.Tier
				select x).ToList();
			while (num2 > 0)
			{
				TroopRosterElement randomElementInefficiently = (from x in list
					group x by x.Character.Tier).First().GetRandomElementInefficiently();
				int num3 = Math.Min(MBRandom.RandomInt(1, randomElementInefficiently.Number + 1), num2);
				MobileParty.MainParty.MemberRoster.AddToCounts(randomElementInefficiently.Character, -num3);
				num2 -= num3;
				if (num3 == randomElementInefficiently.Number)
				{
					list.Remove(randomElementInefficiently);
				}
				else
				{
					randomElementInefficiently.Number -= num3;
					list[list.IndexOf(randomElementInefficiently)] = randomElementInefficiently;
				}
			}
			TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.");
			textObject.SetTextVariable("AMOUNT", num);
			return new List<TextObject> { textObject };
		}, (IncidentEffect effect) => KillTroopsRandomly(predicate, amountGetter)._hint(effect));
	}

	public static IncidentEffect KillTroopsRandomly(Func<TroopRosterElement, bool> predicate, Func<int> amountGetter)
	{
		return new IncidentEffect(() => (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
			where predicate(x) && !x.Character.IsHero
			select x).Sum((TroopRosterElement x) => x.Number) >= amountGetter(), delegate
		{
			int num = amountGetter();
			int num2 = num;
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where predicate(x) && !x.Character.IsHero
				select x).ToList();
			while (num2 > 0)
			{
				int index = MBRandom.RandomInt(list.Count);
				TroopRosterElement value = list[index];
				int num3 = MBRandom.RandomInt(1, Math.Min(value.Number + 1, num2));
				MobileParty.MainParty.MemberRoster.AddToCounts(value.Character, -num3);
				num2 -= num3;
				if (num3 == value.Number)
				{
					list.RemoveAt(index);
				}
				else
				{
					value.Number -= num3;
					list[index] = value;
				}
			}
			TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.");
			textObject.SetTextVariable("AMOUNT", num);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=JKivaEW6}Lose {AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?}");
			}
			else
			{
				textObject = new TextObject("{=5TogqruR}{CHANCE}% chance of losing {AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amountGetter());
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect KillTroopsRandomlyByChance(float chancePerUnit)
	{
		return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars > 0, delegate
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList();
			TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.");
			for (int num = list.Count - 1; num >= 0; num--)
			{
				TroopRosterElement troopRosterElement = list[num];
				int num2;
				for (num2 = 0; num2 < troopRosterElement.Number; num2++)
				{
					if (!(MBRandom.RandomFloat < chancePerUnit))
					{
						break;
					}
				}
				textObject.SetTextVariable("AMOUNT", num2);
				MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -num2);
			}
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=hXentcdy}Lose troops with {CHANCE_PER_UNIT}% chance each");
			}
			else
			{
				textObject = new TextObject("{=Zl6LASZq}{CHANCE}% chance of losing troops with {CHANCE_PER_UNIT}% chance each");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("CHANCE_PER_UNIT", chancePerUnit * 100f);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect KillTroop(Func<CharacterObject> characterGetter, int amount)
	{
		return ChangeTroopAmount(characterGetter, -amount).WithHint(delegate(IncidentEffect effect)
		{
			CharacterObject character = characterGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=kHooobeJ}{AMOUNT} {TROOP.NAME} gets killed");
			}
			else
			{
				textObject = new TextObject("{=Y6s7AxWu}{CHANCE}% chance of {AMOUNT} {TROOP.NAME} getting killed");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("TROOP", character);
			return new List<TextObject> { textObject };
		}).WithCustomInformation(delegate
		{
			TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect ChangeTroopAmount(Func<CharacterObject> characterGetter, int amount)
	{
		return new IncidentEffect(delegate
		{
			CharacterObject characterObject = characterGetter?.Invoke();
			if (characterObject != null)
			{
				if (amount >= 0)
				{
					return true;
				}
				int num = MobileParty.MainParty.MemberRoster.FindIndexOfTroop(characterObject);
				if (num != -1)
				{
					return MobileParty.MainParty.MemberRoster.GetElementNumber(num) >= Math.Abs(amount);
				}
				return false;
			}
			return false;
		}, delegate
		{
			CharacterObject character = characterGetter();
			MobileParty.MainParty.MemberRoster.AddToCounts(character, amount);
			TextObject textObject = new TextObject("{=Ckj7L2Sz}{ABS(AMOUNT)} {CHARACTER.NAME} {?AMOUNT > 0}joined{?}left{\\?} your party");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("CHARACTER", character);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			CharacterObject character = characterGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=AlgTsbSu}{ABS(AMOUNT)} {CHARACTER.NAME} {?AMOUNT > 0}joins to{?}leaves from{\\?} your party");
			}
			else
			{
				textObject = new TextObject("{=eQv4dYQz}{CHANCE}% chance of {ABS(AMOUNT)} {CHARACTER.NAME} {?AMOUNT > 0}joining to{?}leaving from{\\?} your party");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("CHARACTER", character);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect UpgradeTroop(Func<CharacterObject> characterGetter, Func<CharacterObject, bool> upgradePredicate, int amount, Func<long> incidentSeedGetter)
	{
		return new IncidentEffect(delegate
		{
			CharacterObject characterObject = characterGetter?.Invoke();
			if (characterObject == null)
			{
				return false;
			}
			List<CharacterObject> list = CharacterHelper.GetTroopTree(characterObject).Where(upgradePredicate).ToList();
			return MobileParty.MainParty.MemberRoster.GetElementNumber(characterObject) >= amount && list.Count != 0;
		}, delegate
		{
			CharacterObject characterObject = characterGetter();
			CharacterObject seededRandomElement = IncidentHelper.GetSeededRandomElement(CharacterHelper.GetTroopTree(characterObject).Where(upgradePredicate).ToList(), incidentSeedGetter());
			TroopRoster memberRoster = MobileParty.MainParty.MemberRoster;
			memberRoster.AddToCounts(characterObject, -amount);
			memberRoster.AddToCounts(seededRandomElement, amount);
			TextObject textObject = new TextObject("{=0yugQtfB}Upgraded {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("TROOP", characterObject);
			textObject.SetCharacterProperties("UPGRADED_TROOP", seededRandomElement);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			CharacterObject characterObject = characterGetter();
			CharacterObject seededRandomElement = IncidentHelper.GetSeededRandomElement(CharacterHelper.GetTroopTree(characterObject).Where(upgradePredicate).ToList(), incidentSeedGetter());
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=nm2XK1pt}Upgrade {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}");
			}
			else
			{
				textObject = new TextObject("{=tRipzFIP}{CHANCE}% chance of upgrading {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("TROOP", characterObject);
			textObject.SetCharacterProperties("UPGRADED_TROOP", seededRandomElement);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect UpgradeTroop(Func<CharacterObject> characterGetter, Func<CharacterObject> upgradedCharacterGetter, int amount, Func<long> incidentSeedGetter)
	{
		return new IncidentEffect(delegate
		{
			CharacterObject characterObject = characterGetter?.Invoke();
			if (characterObject == null)
			{
				return false;
			}
			return upgradedCharacterGetter?.Invoke() != null && MobileParty.MainParty.MemberRoster.GetElementNumber(characterObject) >= amount;
		}, delegate
		{
			CharacterObject character = characterGetter();
			CharacterObject character2 = upgradedCharacterGetter();
			TroopRoster memberRoster = MobileParty.MainParty.MemberRoster;
			memberRoster.AddToCounts(character, -amount);
			memberRoster.AddToCounts(character2, amount);
			TextObject textObject = new TextObject("{=0yugQtfB}Upgraded {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("TROOP", character);
			textObject.SetCharacterProperties("UPGRADED_TROOP", character2);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			CharacterObject character = characterGetter();
			CharacterObject character2 = upgradedCharacterGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=nm2XK1pt}Upgrade {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}");
			}
			else
			{
				textObject = new TextObject("{=tRipzFIP}{CHANCE}% chance of upgrading {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetCharacterProperties("TROOP", character);
			textObject.SetCharacterProperties("UPGRADED_TROOP", character2);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect RemovePrisonersRandomlyWithPredicate(Func<TroopRosterElement, bool> predicate, int amount)
	{
		return new IncidentEffect(() => MobileParty.MainParty.PrisonRoster.GetTroopRoster().Where(predicate).Sum((TroopRosterElement x) => x.Number) >= amount, delegate
		{
			int num = amount;
			MBList<TroopRosterElement> troopRoster = MobileParty.MainParty.PrisonRoster.GetTroopRoster();
			while (num > 0)
			{
				List<TroopRosterElement> list = troopRoster.Where(predicate).ToList();
				TroopRosterElement troopRosterElement = list[MobileParty.MainParty.RandomInt(list.Count)];
				int num2 = Math.Min(MBRandom.RandomInt(1, troopRosterElement.Number), num);
				MobileParty.MainParty.PrisonRoster.AddToCounts(troopRosterElement.Character, -num2);
				num -= num2;
			}
			TextObject textObject = new TextObject("{=tvshVXKT}Lost {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?}.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=6XW3fKhU}Lose {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?}");
			}
			else
			{
				textObject = new TextObject("{=F3AEpszA}{CHANCE}% chance of losing {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect ChangeItemsAmount(Func<List<ItemObject>> itemsGetter, int amount)
	{
		return new IncidentEffect(delegate
		{
			List<ItemObject> items = itemsGetter?.Invoke();
			return items != null && items.Count >= 0 && (amount >= 0 || MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => items.Contains(x.EquipmentElement.Item)).Sum((ItemRosterElement x) => x.Amount) >= amount);
		}, delegate
		{
			List<ItemObject> list = itemsGetter();
			ItemObject itemObject = list.First();
			int num = Math.Abs(amount);
			while (num > 0)
			{
				ItemObject randomElement = list.GetRandomElement();
				int index = MobileParty.MainParty.ItemRoster.FindIndexOfItem(randomElement);
				int elementNumber = MobileParty.MainParty.ItemRoster.GetElementNumber(index);
				int num2 = Math.Min(MBRandom.RandomInt(1, Math.Min(elementNumber, num)), num);
				MobileParty.MainParty.ItemRoster.AddToCounts(MobileParty.MainParty.ItemRoster[index].EquipmentElement, num2 * Math.Sign(amount));
				num -= num2;
				if (elementNumber - num2 == 0)
				{
					list.Remove(randomElement);
				}
			}
			TextObject variable = ((list.Count == 1) ? list.First().Name : itemObject.ItemCategory.GetName());
			TextObject textObject = new TextObject("{=shZVdlRQ}{?AMOUNT > 1}Received{?}Lost{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}.");
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("ITEM", variable);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			List<ItemObject> list = itemsGetter();
			TextObject variable = ((list.Count == 1) ? list.First().Name : list.First().ItemCategory.GetName());
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=OZAKqzln}{?AMOUNT > 1}Get{?}Lose{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}");
			}
			else
			{
				textObject = new TextObject("{=aVtM937J}{CHANCE}% chance of {?AMOUNT > 1}getting{?}losing{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			textObject.SetTextVariable("ITEM", variable);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect ChangeItemAmount(Func<ItemObject> itemGetter, Func<int> amountGetter)
	{
		return new IncidentEffect(() => itemGetter?.Invoke() != null && amountGetter != null && (amountGetter() > 0 || MobileParty.MainParty.ItemRoster.GetItemNumber(itemGetter()) >= Math.Abs(amountGetter())), delegate
		{
			ItemObject itemObject = itemGetter();
			int num = amountGetter();
			MobileParty.MainParty.ItemRoster.AddToCounts(itemObject, num);
			TextObject textObject = new TextObject("{=0utzQGvE}{?AMOUNT >= 1}Received{?}Lost{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}.");
			textObject.SetTextVariable("AMOUNT", num);
			textObject.SetTextVariable("ITEM", itemObject.Name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			ItemObject itemObject = itemGetter();
			int variable = amountGetter();
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=GQWArgN4}{?AMOUNT >= 1}Get{?}Lose{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}");
			}
			else
			{
				textObject = new TextObject("{=ZCIsZTe2}{CHANCE}% chance of {?AMOUNT >= 1}getting{?}losing{\\?} {ABS(AMOUNT)} {?AMOUNT >= 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", variable);
			textObject.SetTextVariable("ITEM", itemObject.Name);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect PartyExperienceChance(int amount)
	{
		return new IncidentEffect(null, delegate
		{
			MobilePartyHelper.PartyAddSharedXp(MobileParty.MainParty, amount);
			TextObject textObject = new TextObject("{=LgzX3fDk}Party gained {AMOUNT} shared experience.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=CCfVud1f}Party gains {AMOUNT} shared experience");
			}
			else
			{
				textObject = new TextObject("{=aFUVF8VO}{CHANCE}% chance of party gaining {AMOUNT} shared experience");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect DisorganizeParty()
	{
		return new IncidentEffect(null, delegate
		{
			MobileParty.MainParty.SetDisorganized(isDisorganized: true);
			TextObject item = new TextObject("{=ylqcMuBF}Your party got disorganized.");
			return new List<TextObject> { item };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=02DbEPC1}Party becomes disorganized");
			}
			else
			{
				textObject = new TextObject("{=aXXF3aJE}{CHANCE}% chance of party becoming disorganized");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect HealTroopsRandomly(int amount)
	{
		return new IncidentEffect(null, delegate
		{
			TroopRoster memberRoster = MobileParty.MainParty.MemberRoster;
			int num = MBRandom.RandomInt(memberRoster.Count);
			for (int i = 0; i < memberRoster.Count; i++)
			{
				if (amount <= 0)
				{
					break;
				}
				int index = (num + i) % memberRoster.Count;
				if (memberRoster.GetCharacterAtIndex(index).IsRegular)
				{
					int num2 = TaleWorlds.Library.MathF.Min(amount, memberRoster.GetElementWoundedNumber(index));
					if (num2 > 0)
					{
						memberRoster.AddToCountsAtIndex(index, 0, -num2);
						amount -= num2;
					}
				}
			}
			TextObject textObject = new TextObject("{=pawoTBr8}Healed {AMOUNT} wounded troops.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=EbBazlbZ}Heal {AMOUNT} wounded troops");
			}
			else
			{
				textObject = new TextObject("{=riVJZgSU}{CHANCE}% chance of healing {AMOUNT} wounded troops");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	public static IncidentEffect DemoteTroopsRandomlyWithPredicate(Func<TroopRosterElement, bool> predicate, Func<CharacterObject, bool> demotionPredicate, int amount, bool specifyUnitTypeOnHint = true)
	{
		CharacterObject troopToDemoteTo;
		return new IncidentEffect(() => (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
			where predicate != null && predicate(x) && x.Character != CharacterObject.PlayerCharacter && FindTroopToDemoteTo(x.Character, demotionPredicate, out troopToDemoteTo)
			select x).Sum((TroopRosterElement x) => x.Number) > amount, delegate
		{
			int num = amount;
			while (num > 0)
			{
				CharacterObject troopToDemoteTo2;
				TroopRosterElement randomElementWithPredicate = MobileParty.MainParty.MemberRoster.GetTroopRoster().GetRandomElementWithPredicate((TroopRosterElement x) => predicate(x) && x.Character != CharacterObject.PlayerCharacter && FindTroopToDemoteTo(x.Character, demotionPredicate, out troopToDemoteTo2));
				FindTroopToDemoteTo(randomElementWithPredicate.Character, demotionPredicate, out troopToDemoteTo);
				if (randomElementWithPredicate.WoundedNumber > 0)
				{
					int num2 = Math.Min(MBRandom.RandomInt(1, Math.Min(randomElementWithPredicate.WoundedNumber, num)), num);
					MobileParty.MainParty.MemberRoster.AddToCounts(randomElementWithPredicate.Character, -num2, insertAtFront: false, num2);
					MobileParty.MainParty.MemberRoster.AddToCounts(troopToDemoteTo, num2, insertAtFront: false, num2);
					num -= num2;
				}
				else
				{
					int num3 = Math.Min(MBRandom.RandomInt(1, Math.Min(randomElementWithPredicate.Number, num)), num);
					MobileParty.MainParty.MemberRoster.AddToCounts(randomElementWithPredicate.Character, -num3);
					MobileParty.MainParty.MemberRoster.AddToCounts(troopToDemoteTo, num3);
					num -= num3;
				}
			}
			TextObject textObject = new TextObject("{=211WkLlN}{AMOUNT} of your troops got demoted.");
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TroopRosterElement troopRosterElement = MobileParty.MainParty.MemberRoster.GetTroopRoster().FirstOrDefault((TroopRosterElement x) => predicate(x) && CharacterHelper.GetTroopTree(x.Character.Culture.BasicTroop, -1f, x.Character.Tier - 1).FirstOrDefault(demotionPredicate) != null);
			TextObject textObject;
			if (specifyUnitTypeOnHint)
			{
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=64YgbSH8}{AMOUNT} {UNIT_TYPE} get demoted");
				}
				else
				{
					textObject = new TextObject("{=CC1tYZMa}{CHANCE}% chance of {AMOUNT} {UNIT_TYPE} getting demoted");
					textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("UNIT_TYPE", troopRosterElement.Character.DefaultFormationClass.GetName());
			}
			else if (effect._chanceToOccur >= 1f)
			{
				textObject = new TextObject("{=fMCKvvCa}{AMOUNT} random troops get demoted");
			}
			else
			{
				textObject = new TextObject("{=47MFRfCt}{CHANCE}% chance of {AMOUNT} random troops getting demoted");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect._chanceToOccur * 100f));
			}
			textObject.SetTextVariable("AMOUNT", amount);
			return new List<TextObject> { textObject };
		});
	}

	private static bool FindTroopToDemoteTo(CharacterObject troop, Func<CharacterObject, bool> demotionPredicate, out CharacterObject troopToDemoteTo)
	{
		List<CharacterObject> source = CharacterHelper.GetTroopTree(troop.Culture.BasicTroop, -1f, troop.Tier - 1).Where(demotionPredicate).ToList();
		if (source.Any())
		{
			IGrouping<int, CharacterObject> grouping = (from x in source
				group x by FindUpgradeDistanceBfs(x, troop) into x
				orderby x.Key
				select x).FirstOrDefault((IGrouping<int, CharacterObject> x) => x.Key != -1);
			if (grouping != null)
			{
				CharacterObject characterObject = grouping.FirstOrDefault((CharacterObject x) => x.UpgradeTargets.Contains(troop));
				if (characterObject != null)
				{
					troopToDemoteTo = characterObject;
					return true;
				}
				troopToDemoteTo = grouping.ToList().GetRandomElement();
				return true;
			}
		}
		List<CharacterObject> source2 = CharacterHelper.GetTroopTree(troop.Culture.EliteBasicTroop, -1f, troop.Tier - 1).Where(demotionPredicate).ToList();
		if (source2.Any())
		{
			IGrouping<int, CharacterObject> grouping2 = (from x in source2
				group x by FindUpgradeDistanceBfs(x, troop) into x
				orderby x.Key
				select x).FirstOrDefault((IGrouping<int, CharacterObject> x) => x.Key != -1);
			if (grouping2 != null)
			{
				CharacterObject characterObject2 = grouping2.FirstOrDefault((CharacterObject x) => x.UpgradeTargets.Contains(troop));
				if (characterObject2 != null)
				{
					troopToDemoteTo = characterObject2;
					return true;
				}
				troopToDemoteTo = grouping2.ToList().GetRandomElement();
				return true;
			}
		}
		troopToDemoteTo = null;
		return false;
	}

	private static int FindUpgradeDistanceBfs(CharacterObject start, CharacterObject target)
	{
		if (start == target)
		{
			return 0;
		}
		HashSet<CharacterObject> hashSet = new HashSet<CharacterObject>();
		Queue<(CharacterObject, int)> queue = new Queue<(CharacterObject, int)>();
		queue.Enqueue((start, 0));
		hashSet.Add(start);
		while (queue.Count > 0)
		{
			(CharacterObject, int) tuple = queue.Dequeue();
			CharacterObject item = tuple.Item1;
			int item2 = tuple.Item2;
			CharacterObject[] upgradeTargets = item.UpgradeTargets;
			foreach (CharacterObject characterObject in upgradeTargets)
			{
				if (!hashSet.Contains(characterObject))
				{
					if (characterObject == target)
					{
						return item2 + 1;
					}
					hashSet.Add(characterObject);
					queue.Enqueue((characterObject, item2 + 1));
				}
			}
		}
		return -1;
	}

	public static IncidentEffect Group(params IncidentEffect[] effects)
	{
		return new IncidentEffect(() => effects.All((IncidentEffect x) => x.Condition()), delegate
		{
			List<TextObject> list = new List<TextObject>();
			IncidentEffect[] array = effects;
			foreach (IncidentEffect incidentEffect in array)
			{
				list.AddRange(incidentEffect.Consequence());
			}
			return list;
		}, delegate(IncidentEffect effect)
		{
			List<TextObject> list = new List<TextObject>();
			foreach (TextObject item in effects.SelectMany((IncidentEffect x) => x.GetHint()).ToList())
			{
				list.Add(item);
			}
			if (effect._chanceToOccur < 1f)
			{
				list.Insert(0, new TextObject("{=wa9W68Wp}{CHANCE}% chance of following occurs:").SetTextVariable("CHANCE", effect._chanceToOccur * 100f));
			}
			return list;
		});
	}

	public static IncidentEffect Select(IncidentEffect effectOne, IncidentEffect effectTwo, float chanceOfFirstOne)
	{
		return new IncidentEffect(() => effectOne.Condition() && effectTwo.Condition(), delegate
		{
			List<TextObject> list = new List<TextObject>();
			if (MBRandom.RandomFloat < chanceOfFirstOne)
			{
				list.AddRange(effectOne.Consequence());
			}
			else
			{
				list.AddRange(effectTwo.Consequence());
			}
			return list;
		}, delegate
		{
			List<TextObject> list = new List<TextObject> { new TextObject("{=haovqFEg}{CHANCE}% chance of:").SetTextVariable("CHANCE", chanceOfFirstOne * 100f) };
			foreach (TextObject item in effectOne.GetHint())
			{
				list.Add(item);
			}
			list.Add(new TextObject("{=dQqR2DXD}or {CHANCE}% chance of:").SetTextVariable("CHANCE", (1f - chanceOfFirstOne) * 100f));
			foreach (TextObject item2 in effectTwo.GetHint())
			{
				list.Add(item2);
			}
			return list;
		});
	}

	public static IncidentEffect Custom(Func<bool> condition, Func<List<TextObject>> consequence, Func<IncidentEffect, List<TextObject>> hint)
	{
		return new IncidentEffect(condition, consequence, hint);
	}
}
