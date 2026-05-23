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

namespace TaleWorlds.CampaignSystem.Incidents
{
	// Token: 0x02000227 RID: 551
	public class IncidentEffect
	{
		// Token: 0x060020F1 RID: 8433 RVA: 0x0009159C File Offset: 0x0008F79C
		private IncidentEffect(Func<bool> condition, Func<List<TextObject>> consequence, Func<IncidentEffect, List<TextObject>> hint)
		{
			this._condition = condition;
			this._consequence = consequence;
			this._hint = hint;
		}

		// Token: 0x060020F2 RID: 8434 RVA: 0x000915C4 File Offset: 0x0008F7C4
		public bool Condition()
		{
			return this._condition == null || this._condition();
		}

		// Token: 0x060020F3 RID: 8435 RVA: 0x000915DC File Offset: 0x0008F7DC
		public List<TextObject> Consequence()
		{
			List<TextObject> result = new List<TextObject>();
			if (MBRandom.RandomFloat <= this._chanceToOccur)
			{
				Func<List<TextObject>> consequence = this._consequence;
				result = ((consequence != null) ? consequence() : null);
				if (this._customInformation != null)
				{
					result = this._customInformation();
				}
			}
			return result;
		}

		// Token: 0x060020F4 RID: 8436 RVA: 0x00091624 File Offset: 0x0008F824
		public List<TextObject> GetHint()
		{
			Func<IncidentEffect, List<TextObject>> hint = this._hint;
			if (hint == null)
			{
				return null;
			}
			return hint(this);
		}

		// Token: 0x060020F5 RID: 8437 RVA: 0x00091638 File Offset: 0x0008F838
		public IncidentEffect WithChance(float chance)
		{
			this._chanceToOccur = chance;
			return this;
		}

		// Token: 0x060020F6 RID: 8438 RVA: 0x00091642 File Offset: 0x0008F842
		public IncidentEffect WithCustomInformation(Func<List<TextObject>> customInformation)
		{
			this._customInformation = customInformation;
			return this;
		}

		// Token: 0x060020F7 RID: 8439 RVA: 0x0009164C File Offset: 0x0008F84C
		public IncidentEffect WithHint(Func<IncidentEffect, List<TextObject>> hint)
		{
			this._hint = hint;
			return this;
		}

		// Token: 0x060020F8 RID: 8440 RVA: 0x00091658 File Offset: 0x0008F858
		public static IncidentEffect GoldChange(Func<int> amountGetter)
		{
			return new IncidentEffect(delegate()
			{
				int num = amountGetter();
				return Hero.MainHero.Gold >= -num;
			}, delegate()
			{
				int num = amountGetter();
				if (num > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, num, false);
				}
				else
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, MathF.Abs(num), false);
				}
				return new List<TextObject>();
			}, delegate(IncidentEffect effect)
			{
				int variable = amountGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=YGgPUH3r}{?AMOUNT > 0}Earn{?}Lose{\\?} {ABS(AMOUNT)}{GOLD_ICON}", null);
				}
				else
				{
					textObject = new TextObject("{=Lo1o8fqp}{CHANCE}% chance to {?AMOUNT > 0}earn{?}lose{\\?} {ABS(AMOUNT)}{GOLD_ICON}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", variable);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x060020F9 RID: 8441 RVA: 0x0009169C File Offset: 0x0008F89C
		public static IncidentEffect TraitChange(TraitObject trait, int amount)
		{
			return new IncidentEffect(null, delegate()
			{
				TraitLevelingHelper.OnIncidentResolved(trait, amount);
				TextObject textObject = new TextObject("{=UM8ZOtar}Increased reputation for being {TRAIT}.", null);
				textObject.SetTextVariable("TRAIT", HeroHelper.GetPersonalityTraitChangeName(trait, Hero.MainHero, amount >= 0));
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=fnkkMRl0}Increase reputation for being {TRAIT}", null);
				}
				else
				{
					textObject = new TextObject("{=9mGtDERC}{CHANCE}% chance of increasing reputation for being {TRAIT}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("TRAIT", HeroHelper.GetPersonalityTraitChangeName(trait, Hero.MainHero, amount >= 0));
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x060020FA RID: 8442 RVA: 0x000916DC File Offset: 0x0008F8DC
		public static IncidentEffect BuildingLevelChange(Func<Building> buildingGetter, Func<int> amountGetter)
		{
			return new IncidentEffect(delegate()
			{
				Func<int> amountGetter2 = amountGetter;
				int? num = ((amountGetter2 != null) ? new int?(amountGetter2()) : null);
				Func<Building> buildingGetter2 = buildingGetter;
				Building building = ((buildingGetter2 != null) ? buildingGetter2() : null);
				if (building != null)
				{
					int? num2 = num;
					int num3 = 0;
					if (!((num2.GetValueOrDefault() == num3) & (num2 != null)))
					{
						num2 = num;
						num3 = 0;
						if ((num2.GetValueOrDefault() > num3) & (num2 != null))
						{
							num2 = building.CurrentLevel + num;
							num3 = 3;
							if ((num2.GetValueOrDefault() <= num3) & (num2 != null))
							{
								return true;
							}
						}
						num2 = num;
						num3 = 0;
						if ((num2.GetValueOrDefault() < num3) & (num2 != null))
						{
							num2 = building.CurrentLevel + num;
							num3 = building.BuildingType.StartLevel;
							return (num2.GetValueOrDefault() >= num3) & (num2 != null);
						}
						return false;
					}
				}
				return false;
			}, delegate()
			{
				int num = amountGetter();
				Building building = buildingGetter();
				if (num > 0)
				{
					for (int i = 0; i < num; i++)
					{
						int constructionCost = building.GetConstructionCost();
						building.CurrentLevel++;
						building.BuildingProgress -= (float)constructionCost;
					}
				}
				else
				{
					building.CurrentLevel += num;
					building.BuildingProgress = 0f;
				}
				CampaignEventDispatcher.Instance.OnBuildingLevelChanged(building.Town, building, num);
				TextObject textObject = new TextObject("{=bdfoDUM0}{?AMOUNT > 0}Increased{?}Decreased{\\?} {BUILDING} level by {ABS(AMOUNT)}.", null);
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
					textObject = new TextObject("{=nAft4SaD}{?AMOUNT > 0}Increase{?}Decrease{\\?} {BUILDING} level by {ABS(AMOUNT)}.", null);
				}
				else
				{
					textObject = new TextObject("{=BaffFLtX}{CHANCE}% chance of {=*}{?AMOUNT > 0}increasing{?}decreasing{\\?} {BUILDING} level by {ABS(AMOUNT)}.", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("BUILDING", building.Name);
				textObject.SetTextVariable("AMOUNT", variable);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x060020FB RID: 8443 RVA: 0x00091728 File Offset: 0x0008F928
		public static IncidentEffect SiegeProgressChange(Func<float> amountGetter)
		{
			return new IncidentEffect(delegate()
			{
				SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
				bool flag;
				if (playerSiegeEvent == null)
				{
					flag = null != null;
				}
				else
				{
					BesiegerCamp besiegerCamp = playerSiegeEvent.BesiegerCamp;
					if (besiegerCamp == null)
					{
						flag = null != null;
					}
					else
					{
						SiegeEvent.SiegeEnginesContainer siegeEngines = besiegerCamp.SiegeEngines;
						flag = ((siegeEngines != null) ? siegeEngines.SiegePreparations : null) != null;
					}
				}
				return flag && !PlayerSiege.PlayerSiegeEvent.BesiegerCamp.IsPreparationComplete;
			}, delegate()
			{
				float num = amountGetter();
				PlayerSiege.PlayerSiegeEvent.BesiegerCamp.SiegeEngines.SiegePreparations.SetProgress(PlayerSiege.PlayerSiegeEvent.BesiegerCamp.SiegeEngines.SiegePreparations.Progress + num);
				TextObject textObject = new TextObject("{=C0kUpB48}{?AMOUNT > 0}Increased{?}Decreased{\\?} siege progress by {ABS(AMOUNT)}%.", null);
				textObject.SetTextVariable("AMOUNT", MathF.Round(num * 100f));
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				float num = amountGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=aqE0C4s8}{?AMOUNT > 0}Increase{?}Decrease{\\?} siege progress by {ABS(AMOUNT)}%", null);
				}
				else
				{
					textObject = new TextObject("{=BaffFLtX}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} siege progress by {ABS(AMOUNT)}%", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", MathF.Round(num * 100f));
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x060020FC RID: 8444 RVA: 0x00091780 File Offset: 0x0008F980
		public static IncidentEffect WorkshopProfitabilityChange(Func<Workshop> workshopGetter, float percentage)
		{
			return new IncidentEffect(delegate()
			{
				Func<Workshop> workshopGetter2 = workshopGetter;
				return ((workshopGetter2 != null) ? workshopGetter2() : null) != null;
			}, delegate()
			{
				Workshop workshop = workshopGetter();
				int num = MathF.Round((float)workshop.ProfitMade * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction()) * percentage);
				workshop.ChangeGold(num);
				TextObject textObject = new TextObject("{=9bIi78RK}{WORKSHOP} {?PERCENTAGE > 0}gained{?}lost{\\?} {AMOUNT} gold.", null);
				textObject.SetTextVariable("WORKSHOP", workshop.Name);
				textObject.SetTextVariable("PERCENTAGE", percentage, 2);
				textObject.SetTextVariable("AMOUNT", Math.Abs(num));
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=rMWw9ieF}Workshop {?PERCENTAGE > 0}gains{?}loses{\\?} {ABS(PERCENTAGE)}% of its revenue.", null);
				}
				else
				{
					textObject = new TextObject("{=s8DBEakZ}{CHANCE}% chance of workshop {?PERCENTAGE > 0}gaining{?}losing{\\?} {ABS(PERCENTAGE)}% of its revenue.", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("PERCENTAGE", percentage * 100f, 2);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x060020FD RID: 8445 RVA: 0x000917CC File Offset: 0x0008F9CC
		public static IncidentEffect SkillChange(SkillObject skill, float amount)
		{
			return new IncidentEffect(() => true, delegate()
			{
				Hero.MainHero.AddSkillXp(skill, amount);
				TextObject textObject = new TextObject("{=ySoK6FLl}{?AMOUNT > 0}Gained{?}Lost{\\?} {ABS(AMOUNT)} {SKILL} XP.", null);
				textObject.SetTextVariable("AMOUNT", MathF.Round(amount));
				textObject.SetTextVariable("SKILL", skill.Name);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=7aKxuBVH}+{AMOUNT} XP {SKILL}", null);
				}
				else
				{
					textObject = new TextObject("{=ZucFKCqy}{CHANCE}% chance of +{AMOUNT} XP {SKILL}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", MathF.Round(amount));
				textObject.SetTextVariable("SKILL", skill.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x060020FE RID: 8446 RVA: 0x0009182C File Offset: 0x0008FA2C
		public static IncidentEffect MoraleChange(float amount)
		{
			return new IncidentEffect(null, delegate()
			{
				MobileParty.MainParty.RecentEventsMorale += amount;
				TextObject textObject = new TextObject("{=QG50JVu8}{?AMOUNT > 0}Increased{?}Decreased{\\?} morale by {ABS(AMOUNT)}", null);
				textObject.SetTextVariable("AMOUNT", amount, 2);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=YAyISGjV}{?AMOUNT > 0}Increase{?}Decrease{\\?} morale by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=I8WRkX2a}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} morale by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount, 2);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x060020FF RID: 8447 RVA: 0x00091864 File Offset: 0x0008FA64
		public static IncidentEffect HealthChance(int amount)
		{
			return new IncidentEffect(null, delegate()
			{
				Hero.MainHero.HitPoints += amount;
				TextObject textObject = new TextObject("{=sPa4O70I}{?AMOUNT > 0}Healed{?}Lost{\\?} {ABS(AMOUNT)} hit points.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=94UTHJCl}{?AMOUNT > 0}Heal{?}Lose{\\?} {ABS(AMOUNT)} hit points", null);
				}
				else
				{
					textObject = new TextObject("{=t7YszJ1w}{CHANCE}% chance of {?AMOUNT > 0}healing{?}losing{\\?} {ABS(AMOUNT)} hit points", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002100 RID: 8448 RVA: 0x0009189C File Offset: 0x0008FA9C
		public static IncidentEffect RenownChange(float amount)
		{
			return new IncidentEffect(() => true, delegate()
			{
				GainRenownAction.Apply(Hero.MainHero, amount, false);
				TextObject textObject = new TextObject("{=NHzq8L83}Gained {AMOUNT} renown.", null);
				textObject.SetTextVariable("AMOUNT", amount, 2);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=noOOudW8}Gain {AMOUNT} renown", null);
				}
				else
				{
					textObject = new TextObject("{=6a8nEuqX}{CHANCE}% chance of gaining {AMOUNT} renown", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount, 2);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002101 RID: 8449 RVA: 0x000918F4 File Offset: 0x0008FAF4
		public static IncidentEffect CrimeRatingChange(Func<IFaction> factionGetter, float amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<IFaction> factionGetter2 = factionGetter;
				return ((factionGetter2 != null) ? factionGetter2() : null) != null;
			}, delegate()
			{
				ChangeCrimeRatingAction.Apply(factionGetter(), amount, true);
				TextObject textObject = new TextObject("{=V2CGB9Sw}{?AMOUNT > 0}Increased{?}Decreased{\\?} crime rating by {ABS(AMOUNT)} in {FACTION}.", null);
				textObject.SetTextVariable("AMOUNT", amount, 2);
				textObject.SetTextVariable("FACTION", factionGetter().Name);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=t01zXDvG}{?AMOUNT > 0}Increase{?}Decrease{\\?} crime rating by {ABS(AMOUNT)} in {FACTION}", null);
				}
				else
				{
					textObject = new TextObject("{=b029BWMC}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} crime rating by {ABS(AMOUNT)} in {FACTION}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount, 2);
				textObject.SetTextVariable("FACTION", factionGetter().Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002102 RID: 8450 RVA: 0x00091940 File Offset: 0x0008FB40
		public static IncidentEffect InfluenceChange(float amount)
		{
			return new IncidentEffect(() => Hero.MainHero.Clan.Kingdom != null, delegate()
			{
				float num = ((Hero.MainHero.Clan.Influence + amount > 0f) ? amount : (-Hero.MainHero.Clan.Influence));
				GainKingdomInfluenceAction.ApplyForDefault(Hero.MainHero, num);
				TextObject textObject = new TextObject("{=MW0ah7pi}{?AMOUNT > 0}Gained{?}Lost{\\?} {ABS(AMOUNT)} influence.", null);
				textObject.SetTextVariable("AMOUNT", num, 2);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				float variable = ((Hero.MainHero.Clan.Influence + amount > 0f) ? amount : (-Hero.MainHero.Clan.Influence));
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=3a3D6aAt}{?AMOUNT > 0}Gain{?}Lose{\\?} {ABS(AMOUNT)} influence", null);
				}
				else
				{
					textObject = new TextObject("{=3K85XqUs}{CHANCE}% chance of {?AMOUNT > 0}gaining{?}losing{\\?} {ABS(AMOUNT)} influence", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", variable, 2);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002103 RID: 8451 RVA: 0x00091998 File Offset: 0x0008FB98
		public static IncidentEffect SettlementRelationChange(Func<Settlement> settlementGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Settlement> settlementGetter2 = settlementGetter;
				return ((settlementGetter2 != null) ? settlementGetter2() : null) != null;
			}, delegate()
			{
				Settlement settlement = settlementGetter();
				List<TextObject> list = new List<TextObject>();
				foreach (Hero gainedRelationWith in settlement.Notables)
				{
					ChangeRelationAction.ApplyPlayerRelation(gainedRelationWith, amount, true, true);
					TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.", null);
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
					textObject = new TextObject("{=PaKstm1Q}{?AMOUNT > 0}Increase{?}Decrease{\\?} relationship with notables in {SETTLEMENT} by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=8yruI4lJ}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} relationship with notables in {SETTLEMENT} by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("SETTLEMENT", settlement.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002104 RID: 8452 RVA: 0x000919E4 File Offset: 0x0008FBE4
		public static IncidentEffect TownBoundVillageRelationChange(Func<Town> townGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Town> townGetter2 = townGetter;
				return ((townGetter2 != null) ? townGetter2() : null) != null && townGetter().Villages.Count > 0;
			}, delegate()
			{
				Town town = townGetter();
				List<TextObject> list = new List<TextObject>();
				foreach (Village village in town.Villages)
				{
					foreach (Hero gainedRelationWith in village.Settlement.Notables)
					{
						ChangeRelationAction.ApplyPlayerRelation(gainedRelationWith, amount, true, true);
					}
					TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.", null);
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
					textObject = new TextObject("{=SwEQJC4s}{?AMOUNT > 0}Increase{?}Decrease{\\?} relationship with each bound village of {SETTLEMENT} by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=bSMXHl3A}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} relationship with each bound village of {SETTLEMENT} by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("SETTLEMENT", town.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002105 RID: 8453 RVA: 0x00091A30 File Offset: 0x0008FC30
		public static IncidentEffect TownBoundVillageHearthChange(Func<Town> townGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Town> townGetter2 = townGetter;
				return ((townGetter2 != null) ? townGetter2() : null) != null && townGetter().Villages.Count > 0;
			}, delegate()
			{
				Town town = townGetter();
				List<TextObject> list = new List<TextObject>();
				foreach (Village village in town.Villages)
				{
					village.Hearth += (float)amount;
					TextObject textObject = new TextObject("{=qMCaLtKm}{?AMOUNT > 0}Increased{?}Decreased{\\?} hearth of {SETTLEMENT} by {ABS(AMOUNT)}.", null);
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
					textObject = new TextObject("{=lWOTWQdF}{?AMOUNT > 0}Increase{?}Decrease{\\?} hearth of each bound village of {SETTLEMENT} by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=pBNxbnBk}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} hearth of each bound village of {SETTLEMENT} by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("SETTLEMENT", town.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002106 RID: 8454 RVA: 0x00091A7C File Offset: 0x0008FC7C
		public static IncidentEffect VillageHearthChange(Func<Village> villageGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Village> villageGetter2 = villageGetter;
				return ((villageGetter2 != null) ? villageGetter2() : null) != null;
			}, delegate()
			{
				Village village = villageGetter();
				village.Hearth += (float)amount;
				TextObject textObject = new TextObject("{=qMCaLtKm}{?AMOUNT > 0}Increased{?}Decreased{\\?} hearth of {SETTLEMENT} by {ABS(AMOUNT)}.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("SETTLEMENT", village.Name);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				Village village = villageGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=ZpE5eWj9}{?AMOUNT > 0}Increase{?}Decrease{\\?} {VILLAGE} hearth by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=bSTaPVG7}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {VILLAGE} hearth by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("VILLAGE", village.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002107 RID: 8455 RVA: 0x00091AC8 File Offset: 0x0008FCC8
		public static IncidentEffect TownSecurityChange(Func<Town> townGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Town> townGetter2 = townGetter;
				return ((townGetter2 != null) ? townGetter2() : null) != null;
			}, delegate()
			{
				Town town = townGetter();
				town.Security += (float)amount;
				TextObject textObject = new TextObject("{=ShfAk7aC}{?AMOUNT > 0}Increased{?}Decreased{\\?} {TOWN} security by {ABS(AMOUNT)}.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("TOWN", town.Name);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				Town town = townGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=sJ9OpLKp}{?AMOUNT > 0}Increase{?}Decrease{\\?} {TOWN} security by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=6E7sQIfW}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {TOWN} security by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("TOWN", town.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002108 RID: 8456 RVA: 0x00091B14 File Offset: 0x0008FD14
		public static IncidentEffect HeroRelationChange(Func<Hero> heroGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Hero> heroGetter2 = heroGetter;
				return ((heroGetter2 != null) ? heroGetter2() : null) != null;
			}, delegate()
			{
				ChangeRelationAction.ApplyPlayerRelation(heroGetter(), amount, true, true);
				return new List<TextObject>();
			}, delegate(IncidentEffect effect)
			{
				Hero hero = heroGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=T8GYg3tv}{?AMOUNT > 0}Increase{?}Decrease{\\?} relationship with {HERO.NAME} by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=6tnegb19}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} relationship with {HERO.NAME} by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("HERO", hero.CharacterObject, false);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002109 RID: 8457 RVA: 0x00091B60 File Offset: 0x0008FD60
		public static IncidentEffect TownProsperityChange(Func<Town> townGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Town> townGetter2 = townGetter;
				return ((townGetter2 != null) ? townGetter2() : null) != null;
			}, delegate()
			{
				Town town = townGetter();
				town.Prosperity += (float)amount;
				TextObject textObject = new TextObject("{=gd2Ppaae}{?AMOUNT > 0}Increased{?}Decreased{\\?} {TOWN}'s prosperity by {ABS(AMOUNT)}.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("TOWN", town.Name);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				Town town = townGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=sBoVFny0}{?AMOUNT > 0}Increase{?}Decrease{\\?} {TOWN}'s prosperity by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=CirbWpGB}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {TOWN}'s prosperity by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("TOWN", town.Name);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600210A RID: 8458 RVA: 0x00091BAC File Offset: 0x0008FDAC
		public static IncidentEffect SettlementMilitiaChange(Func<Settlement> settlementGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<Settlement> settlementGetter2 = settlementGetter;
				Settlement settlement = ((settlementGetter2 != null) ? settlementGetter2() : null);
				return settlement != null && settlement.Militia > (float)(-(float)amount);
			}, delegate()
			{
				Settlement settlement = settlementGetter();
				settlement.Militia += (float)amount;
				TextObject textObject = new TextObject("{=Zu4loCJR}{?AMOUNT > 0}Increased{?}Decreased{\\?} {SETTLEMENT}'s militia by {ABS(AMOUNT)}.", null);
				textObject.SetTextVariable("SETTLEMENT", settlement.Name);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				Settlement settlement = settlementGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=UUXAl3un}{?AMOUNT > 0}Increase{?}Decrease{\\?} {SETTLEMENT}'s militia by {ABS(AMOUNT)}", null);
				}
				else
				{
					textObject = new TextObject("{=b2Iu3WsA}{CHANCE}% chance of {?AMOUNT > 0}increasing{?}decreasing{\\?} {SETTLEMENT}'s militia by {ABS(AMOUNT)}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("SETTLEMENT", settlement.Name);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600210B RID: 8459 RVA: 0x00091BF8 File Offset: 0x0008FDF8
		public static IncidentEffect InfestNearbyHideout(Func<Settlement> settlementGetter)
		{
			return new IncidentEffect(delegate()
			{
				Func<Settlement> settlementGetter2 = settlementGetter;
				return ((settlementGetter2 != null) ? settlementGetter2() : null) != null;
			}, delegate()
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
					for (int i = 0; i < num; i++)
					{
						behavior.AddBanditToHideout(hideout, null, false);
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
					textObject = new TextObject("{=VIMgmfp8}Infest a hideout nearby {SETTLEMENT}", null);
				}
				else
				{
					textObject = new TextObject("{=5HYYbGDe}{CHANCE}% chance of infesting a hideout nearby {SETTLEMENT}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("SETTLEMENT", settlement.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600210C RID: 8460 RVA: 0x00091C3C File Offset: 0x0008FE3C
		public static IncidentEffect WoundTroopsRandomly(float percentage)
		{
			return new IncidentEffect(() => (float)(MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars) >= (float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage, delegate()
			{
				int num = MathF.Round((float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage);
				MobileParty.MainParty.MemberRoster.WoundNumberOfNonHeroTroopsRandomly(num);
				TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.", null);
				textObject.SetTextVariable("AMOUNT", num);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=v9Ad4uGn}{AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} gets wounded", null);
				}
				else
				{
					textObject = new TextObject("{=c1aajRyU}{CHANCE}% chance of {AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} getting wounded", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", percentage * 100f, 2);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600210D RID: 8461 RVA: 0x00091C80 File Offset: 0x0008FE80
		public static IncidentEffect WoundTroopsRandomly(Func<TroopRosterElement, bool> predicate, Func<int> amountGetter, bool specifyUnitTypeOnHint = true)
		{
			return new IncidentEffect(delegate()
			{
				IEnumerable<TroopRosterElement> source = MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(predicate).ToList<TroopRosterElement>();
				int num = amountGetter();
				return source.Sum((TroopRosterElement x) => x.Number - x.WoundedNumber) > num;
			}, delegate()
			{
				List<TroopRosterElement> list = MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(predicate).ToList<TroopRosterElement>();
				int i = amountGetter();
				TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.", null);
				textObject.SetTextVariable("AMOUNT", i);
				while (i > 0)
				{
					if (!list.Any((TroopRosterElement x) => x.Number > x.WoundedNumber))
					{
						break;
					}
					TroopRosterElement randomElementWithPredicate = list.GetRandomElementWithPredicate((TroopRosterElement x) => x.Number > x.WoundedNumber);
					int num = randomElementWithPredicate.Number - randomElementWithPredicate.WoundedNumber;
					int num2 = ((i > num) ? num : MBRandom.RandomInt(1, i + 1));
					MobileParty.MainParty.MemberRoster.WoundTroop(randomElementWithPredicate.Character, num2, default(UniqueTroopDescriptor));
					i -= num2;
				}
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (specifyUnitTypeOnHint)
				{
					if (effect._chanceToOccur >= 1f)
					{
						textObject = new TextObject("{=Q4j44aOp}{AMOUNT} {UNIT_TYPE} get wounded", null);
					}
					else
					{
						textObject = new TextObject("{=oXtob6vV}{CHANCE}% chance of {AMOUNT} {UNIT_TYPE} getting wounded", null);
						textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
					}
					TroopRosterElement randomElement = MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(predicate).ToList<TroopRosterElement>()
						.GetRandomElement<TroopRosterElement>();
					textObject.SetTextVariable("UNIT_TYPE", randomElement.Character.DefaultFormationClass.GetName());
				}
				else if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=3SDefWu5}{AMOUNT} random troops get wounded", null);
				}
				else
				{
					textObject = new TextObject("{=Owc4NdbL}{CHANCE}% chance of {AMOUNT} random troops getting wounded", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600210E RID: 8462 RVA: 0x00091CD4 File Offset: 0x0008FED4
		public static IncidentEffect WoundTroopsRandomlyWithChanceOfDeath(float percentage, float chanceOfDeathPerUnit)
		{
			return new IncidentEffect(() => (float)(MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars) >= (float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage, delegate()
			{
				int num = MathF.Round((float)MobileParty.MainParty.MemberRoster.TotalRegulars * percentage);
				int num2;
				MobilePartyHelper.WoundNumberOfNonHeroTroopsRandomlyWithChanceOfDeath(MobileParty.MainParty.MemberRoster, num, chanceOfDeathPerUnit, out num2);
				int num3 = num - num2;
				List<TextObject> list = new List<TextObject>();
				if (num2 > 0)
				{
					if (num3 == 0)
					{
						TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.", null);
						textObject.SetTextVariable("AMOUNT", num2);
						list.Add(textObject);
					}
					else
					{
						TextObject textObject2 = new TextObject("{=zXmhbszd}{AMOUNT} of your troops got wounded, and {AMOUNT_DEAD} died.", null);
						textObject2.SetTextVariable("AMOUNT", num3);
						textObject2.SetTextVariable("AMOUNT_DEAD", num2);
						list.Add(textObject2);
					}
				}
				else
				{
					TextObject textObject3 = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.", null);
					textObject3.SetTextVariable("AMOUNT", num3);
					list.Add(textObject3);
				}
				return list;
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=lbS9uHyp}{AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} gets wounded and each unit has {DEATH_CHANCE}% chance of dying", null);
				}
				else
				{
					textObject = new TextObject("{=J6ppDY1Y}{CHANCE}% chance of {AMOUNT}% of your {?AMOUNT > 1}troops{?}troop{\\?} getting wounded and each unit has {DEATH_CHANCE}% chance of dying", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("DEATH_CHANCE", MathF.Round(chanceOfDeathPerUnit * 100f));
				textObject.SetTextVariable("AMOUNT", MathF.Round(percentage * 100f));
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600210F RID: 8463 RVA: 0x00091D20 File Offset: 0x0008FF20
		public static IncidentEffect BreachSiegeWall(int amount)
		{
			return new IncidentEffect(() => MobileParty.MainParty.SiegeEvent != null, delegate()
			{
				MBReadOnlyList<float> hitPointRatioList = PlayerSiege.BesiegedSettlement.SettlementWallSectionHitPointsRatioList;
				int num = amount;
				List<int> list = (from x in Enumerable.Range(0, hitPointRatioList.Count)
					where hitPointRatioList[x] > 0f
					select x).ToList<int>();
				while (num > 0 && list.Count > 0)
				{
					int randomElement = list.GetRandomElement<int>();
					PlayerSiege.BesiegedSettlement.SetWallSectionHitPointsRatioAtIndex(randomElement, 0f);
					num--;
				}
				PlayerSiege.BesiegedSettlement.Party.SetVisualAsDirty();
				TextObject textObject = new TextObject("{=WXCl7J36}{?AMOUNT > 1}Walls{?}Wall{\\?} breached successfully.", null);
				textObject.SetTextVariable("AMOUNT", amount - num);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=zDqwzpYf}Breach {AMOUNT} siege {?AMOUNT > 1}walls{?}wall{\\?}", null);
				}
				else
				{
					textObject = new TextObject("{=KCLDqhsV}{CHANCE}% chance of breaching {AMOUNT} siege {?AMOUNT > 1}walls{?}wall{\\?}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002110 RID: 8464 RVA: 0x00091D78 File Offset: 0x0008FF78
		public static IncidentEffect WoundTroopsRandomly(int amount)
		{
			return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= amount, delegate()
			{
				MobileParty.MainParty.MemberRoster.WoundNumberOfNonHeroTroopsRandomly(amount);
				TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=3K7NyOSr}{AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?} gets wounded", null);
				}
				else
				{
					textObject = new TextObject("{=CV7rDOtO}{CHANCE}% chance of {AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?} getting wounded", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002111 RID: 8465 RVA: 0x00091DBC File Offset: 0x0008FFBC
		public static IncidentEffect WoundTroopsRandomlyWithChanceOfDeath(int amount, float chanceOfDeathPerUnit)
		{
			return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= amount, delegate()
			{
				int num;
				MobilePartyHelper.WoundNumberOfNonHeroTroopsRandomlyWithChanceOfDeath(MobileParty.MainParty.MemberRoster, amount, chanceOfDeathPerUnit, out num);
				int num2 = amount - num;
				List<TextObject> list = new List<TextObject>();
				if (num > 0)
				{
					if (num2 == 0)
					{
						TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.", null);
						textObject.SetTextVariable("AMOUNT", num);
						list.Add(textObject);
					}
					else
					{
						TextObject textObject2 = new TextObject("{=zXmhbszd}{AMOUNT} of your troops got wounded, and {AMOUNT_DEAD} died.", null);
						textObject2.SetTextVariable("AMOUNT", num2);
						textObject2.SetTextVariable("AMOUNT_DEAD", num);
						list.Add(textObject2);
					}
				}
				else
				{
					TextObject textObject3 = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.", null);
					textObject3.SetTextVariable("AMOUNT", num2);
					list.Add(textObject3);
				}
				return list;
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=n5lPsPKq}{AMOUNT} of your troops {?AMOUNT > 1}get{?}gets{\\?} wounded and each unit has {DEATH_CHANCE}% chance of dying", null);
				}
				else
				{
					textObject = new TextObject("{=v679SFPt}{CHANCE}% chance of {AMOUNT} of your troops getting wounded and each unit has {DEATH_CHANCE}% chance of dying", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("DEATH_CHANCE", MathF.Round(chanceOfDeathPerUnit * 100f));
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002112 RID: 8466 RVA: 0x00091E08 File Offset: 0x00090008
		public static IncidentEffect WoundTroop(Func<CharacterObject> characterGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<CharacterObject> characterGetter2 = characterGetter;
				CharacterObject characterObject = ((characterGetter2 != null) ? characterGetter2() : null);
				if (characterObject == null)
				{
					return false;
				}
				int num = MobileParty.MainParty.MemberRoster.FindIndexOfTroop(characterObject);
				if (num == -1)
				{
					return false;
				}
				TroopRosterElement troopRosterElement = MobileParty.MainParty.MemberRoster.GetTroopRoster()[num];
				return troopRosterElement.Number - troopRosterElement.WoundedNumber >= amount;
			}, delegate()
			{
				CharacterObject characterObject = characterGetter();
				MobileParty.MainParty.MemberRoster.WoundTroop(characterObject, amount, default(UniqueTroopDescriptor));
				TextObject textObject = new TextObject("{=Cep8OD72}{AMOUNT} {TROOP.NAME} got wounded.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("TROOP", characterObject, false);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				CharacterObject character = characterGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=wFtO2y5R}{AMOUNT} {TROOP.NAME} gets wounded", null);
				}
				else
				{
					textObject = new TextObject("{=PMgz8Dah}{CHANCE}% chance of {AMOUNT} {TROOP.NAME} getting wounded", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("TROOP", character, false);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002113 RID: 8467 RVA: 0x00091E54 File Offset: 0x00090054
		public static IncidentEffect WoundTroopsRandomlyByChance(float chancePerUnit)
		{
			return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars > 0, delegate()
			{
				List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
					where !x.Character.IsHero
					select x).ToList<TroopRosterElement>();
				TextObject textObject = new TextObject("{=GMF8G6IQ}{AMOUNT} of your troops got wounded.", null);
				textObject.SetTextVariable("AMOUNT", 0);
				for (int i = list.Count - 1; i >= 0; i--)
				{
					TroopRosterElement troopRosterElement = list[i];
					int num = 0;
					while (num < troopRosterElement.Number - troopRosterElement.WoundedNumber && MBRandom.RandomFloat < chancePerUnit)
					{
						num++;
					}
					textObject.SetTextVariable("AMOUNT", num);
					MobileParty.MainParty.MemberRoster.WoundTroop(troopRosterElement.Character, num, default(UniqueTroopDescriptor));
				}
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=bsRI2wUz}Wound troops with {CHANCE_PER_UNIT}% chance each", null);
				}
				else
				{
					textObject = new TextObject("{=8k6NCb2S}{CHANCE}% chance of wounding troops with {CHANCE_PER_UNIT}% chance each", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("CHANCE_PER_UNIT", chancePerUnit, 2);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002114 RID: 8468 RVA: 0x00091EAC File Offset: 0x000900AC
		public static IncidentEffect KillTroopsRandomlyOrderedByTier(Func<TroopRosterElement, bool> predicate, Func<int> amountGetter)
		{
			Func<TroopRosterElement, bool> <>9__3;
			return new IncidentEffect(() => IncidentEffect.KillTroopsRandomly(predicate, amountGetter)._condition(), delegate()
			{
				int num = amountGetter();
				int i = num;
				IEnumerable<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
				Func<TroopRosterElement, bool> predicate2;
				if ((predicate2 = <>9__3) == null)
				{
					predicate2 = (<>9__3 = (TroopRosterElement x) => predicate(x) && !x.Character.IsHero);
				}
				List<TroopRosterElement> list = (from x in troopRoster.Where(predicate2)
					orderby x.Character.Tier
					select x).ToList<TroopRosterElement>();
				while (i > 0)
				{
					TroopRosterElement randomElementInefficiently = (from x in list
						group x by x.Character.Tier).First<IGrouping<int, TroopRosterElement>>().GetRandomElementInefficiently<TroopRosterElement>();
					int num2 = Math.Min(MBRandom.RandomInt(1, randomElementInefficiently.Number + 1), i);
					MobileParty.MainParty.MemberRoster.AddToCounts(randomElementInefficiently.Character, -num2, false, 0, 0, true, -1);
					i -= num2;
					if (num2 == randomElementInefficiently.Number)
					{
						list.Remove(randomElementInefficiently);
					}
					else
					{
						randomElementInefficiently.Number -= num2;
						list[list.IndexOf(randomElementInefficiently)] = randomElementInefficiently;
					}
				}
				TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.", null);
				textObject.SetTextVariable("AMOUNT", num);
				return new List<TextObject> { textObject };
			}, (IncidentEffect effect) => IncidentEffect.KillTroopsRandomly(predicate, amountGetter)._hint(effect));
		}

		// Token: 0x06002115 RID: 8469 RVA: 0x00091EF8 File Offset: 0x000900F8
		public static IncidentEffect KillTroopsRandomly(Func<TroopRosterElement, bool> predicate, Func<int> amountGetter)
		{
			Func<TroopRosterElement, bool> <>9__3;
			Func<TroopRosterElement, bool> <>9__5;
			return new IncidentEffect(delegate()
			{
				IEnumerable<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
				Func<TroopRosterElement, bool> predicate2;
				if ((predicate2 = <>9__3) == null)
				{
					predicate2 = (<>9__3 = (TroopRosterElement x) => predicate(x) && !x.Character.IsHero);
				}
				return troopRoster.Where(predicate2).Sum((TroopRosterElement x) => x.Number) >= amountGetter();
			}, delegate()
			{
				int num = amountGetter();
				int i = num;
				IEnumerable<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
				Func<TroopRosterElement, bool> predicate2;
				if ((predicate2 = <>9__5) == null)
				{
					predicate2 = (<>9__5 = (TroopRosterElement x) => predicate(x) && !x.Character.IsHero);
				}
				List<TroopRosterElement> list = troopRoster.Where(predicate2).ToList<TroopRosterElement>();
				while (i > 0)
				{
					int index = MBRandom.RandomInt(list.Count);
					TroopRosterElement troopRosterElement = list[index];
					int num2 = MBRandom.RandomInt(1, Math.Min(troopRosterElement.Number + 1, i));
					MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -num2, false, 0, 0, true, -1);
					i -= num2;
					if (num2 == troopRosterElement.Number)
					{
						list.RemoveAt(index);
					}
					else
					{
						troopRosterElement.Number -= num2;
						list[index] = troopRosterElement;
					}
				}
				TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.", null);
				textObject.SetTextVariable("AMOUNT", num);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=JKivaEW6}Lose {AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?}", null);
				}
				else
				{
					textObject = new TextObject("{=5TogqruR}{CHANCE}% chance of losing {AMOUNT} of your {?AMOUNT > 1}troops{?}troop{\\?}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amountGetter());
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002116 RID: 8470 RVA: 0x00091F44 File Offset: 0x00090144
		public static IncidentEffect KillTroopsRandomlyByChance(float chancePerUnit)
		{
			return new IncidentEffect(() => MobileParty.MainParty.MemberRoster.TotalRegulars > 0, delegate()
			{
				List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
					where !x.Character.IsHero
					select x).ToList<TroopRosterElement>();
				TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.", null);
				for (int i = list.Count - 1; i >= 0; i--)
				{
					TroopRosterElement troopRosterElement = list[i];
					int num = 0;
					while (num < troopRosterElement.Number && MBRandom.RandomFloat < chancePerUnit)
					{
						num++;
					}
					textObject.SetTextVariable("AMOUNT", num);
					MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -num, false, 0, 0, true, -1);
				}
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=hXentcdy}Lose troops with {CHANCE_PER_UNIT}% chance each", null);
				}
				else
				{
					textObject = new TextObject("{=Zl6LASZq}{CHANCE}% chance of losing troops with {CHANCE_PER_UNIT}% chance each", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("CHANCE_PER_UNIT", chancePerUnit * 100f, 2);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002117 RID: 8471 RVA: 0x00091F9C File Offset: 0x0009019C
		public static IncidentEffect KillTroop(Func<CharacterObject> characterGetter, int amount)
		{
			return IncidentEffect.ChangeTroopAmount(characterGetter, -amount).WithHint(delegate(IncidentEffect effect)
			{
				CharacterObject character = characterGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=kHooobeJ}{AMOUNT} {TROOP.NAME} gets killed", null);
				}
				else
				{
					textObject = new TextObject("{=Y6s7AxWu}{CHANCE}% chance of {AMOUNT} {TROOP.NAME} getting killed", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("TROOP", character, false);
				return new List<TextObject> { textObject };
			}).WithCustomInformation(delegate
			{
				TextObject textObject = new TextObject("{=ni1m6VDh}{AMOUNT} of your troops died.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002118 RID: 8472 RVA: 0x00091FF4 File Offset: 0x000901F4
		public static IncidentEffect ChangeTroopAmount(Func<CharacterObject> characterGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				Func<CharacterObject> characterGetter2 = characterGetter;
				CharacterObject characterObject = ((characterGetter2 != null) ? characterGetter2() : null);
				if (characterObject == null)
				{
					return false;
				}
				if (amount >= 0)
				{
					return true;
				}
				int num = MobileParty.MainParty.MemberRoster.FindIndexOfTroop(characterObject);
				return num != -1 && MobileParty.MainParty.MemberRoster.GetElementNumber(num) >= Math.Abs(amount);
			}, delegate()
			{
				CharacterObject character = characterGetter();
				MobileParty.MainParty.MemberRoster.AddToCounts(character, amount, false, 0, 0, true, -1);
				TextObject textObject = new TextObject("{=Ckj7L2Sz}{ABS(AMOUNT)} {CHARACTER.NAME} {?AMOUNT > 0}joined{?}left{\\?} your party", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("CHARACTER", character, false);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				CharacterObject character = characterGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=AlgTsbSu}{ABS(AMOUNT)} {CHARACTER.NAME} {?AMOUNT > 0}joins to{?}leaves from{\\?} your party", null);
				}
				else
				{
					textObject = new TextObject("{=eQv4dYQz}{CHANCE}% chance of {ABS(AMOUNT)} {CHARACTER.NAME} {?AMOUNT > 0}joining to{?}leaving from{\\?} your party", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("CHARACTER", character, false);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002119 RID: 8473 RVA: 0x00092040 File Offset: 0x00090240
		public static IncidentEffect UpgradeTroop(Func<CharacterObject> characterGetter, Func<CharacterObject, bool> upgradePredicate, int amount, Func<long> incidentSeedGetter)
		{
			return new IncidentEffect(delegate()
			{
				Func<CharacterObject> characterGetter2 = characterGetter;
				CharacterObject characterObject = ((characterGetter2 != null) ? characterGetter2() : null);
				if (characterObject == null)
				{
					return false;
				}
				List<CharacterObject> list = CharacterHelper.GetTroopTree(characterObject, -1f, float.MaxValue).Where(upgradePredicate).ToList<CharacterObject>();
				return MobileParty.MainParty.MemberRoster.GetElementNumber(characterObject) >= amount && list.Count != 0;
			}, delegate()
			{
				CharacterObject characterObject = characterGetter();
				CharacterObject seededRandomElement = IncidentHelper.GetSeededRandomElement<CharacterObject>(CharacterHelper.GetTroopTree(characterObject, -1f, float.MaxValue).Where(upgradePredicate).ToList<CharacterObject>(), incidentSeedGetter());
				TroopRoster memberRoster = MobileParty.MainParty.MemberRoster;
				memberRoster.AddToCounts(characterObject, -amount, false, 0, 0, true, -1);
				memberRoster.AddToCounts(seededRandomElement, amount, false, 0, 0, true, -1);
				TextObject textObject = new TextObject("{=0yugQtfB}Upgraded {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("TROOP", characterObject, false);
				textObject.SetCharacterProperties("UPGRADED_TROOP", seededRandomElement, false);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				CharacterObject characterObject = characterGetter();
				CharacterObject seededRandomElement = IncidentHelper.GetSeededRandomElement<CharacterObject>(CharacterHelper.GetTroopTree(characterObject, -1f, float.MaxValue).Where(upgradePredicate).ToList<CharacterObject>(), incidentSeedGetter());
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=nm2XK1pt}Upgrade {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}", null);
				}
				else
				{
					textObject = new TextObject("{=tRipzFIP}{CHANCE}% chance of upgrading {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("TROOP", characterObject, false);
				textObject.SetCharacterProperties("UPGRADED_TROOP", seededRandomElement, false);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600211A RID: 8474 RVA: 0x00092098 File Offset: 0x00090298
		public static IncidentEffect UpgradeTroop(Func<CharacterObject> characterGetter, Func<CharacterObject> upgradedCharacterGetter, int amount, Func<long> incidentSeedGetter)
		{
			return new IncidentEffect(delegate()
			{
				Func<CharacterObject> characterGetter2 = characterGetter;
				CharacterObject characterObject = ((characterGetter2 != null) ? characterGetter2() : null);
				if (characterObject == null)
				{
					return false;
				}
				Func<CharacterObject> upgradedCharacterGetter2 = upgradedCharacterGetter;
				return ((upgradedCharacterGetter2 != null) ? upgradedCharacterGetter2() : null) != null && MobileParty.MainParty.MemberRoster.GetElementNumber(characterObject) >= amount;
			}, delegate()
			{
				CharacterObject character = characterGetter();
				CharacterObject character2 = upgradedCharacterGetter();
				TroopRoster memberRoster = MobileParty.MainParty.MemberRoster;
				memberRoster.AddToCounts(character, -amount, false, 0, 0, true, -1);
				memberRoster.AddToCounts(character2, amount, false, 0, 0, true, -1);
				TextObject textObject = new TextObject("{=0yugQtfB}Upgraded {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("TROOP", character, false);
				textObject.SetCharacterProperties("UPGRADED_TROOP", character2, false);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				CharacterObject character = characterGetter();
				CharacterObject character2 = upgradedCharacterGetter();
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=nm2XK1pt}Upgrade {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}", null);
				}
				else
				{
					textObject = new TextObject("{=tRipzFIP}{CHANCE}% chance of upgrading {AMOUNT} of your {TROOP.NAME} to {UPGRADED_TROOP.NAME}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetCharacterProperties("TROOP", character, false);
				textObject.SetCharacterProperties("UPGRADED_TROOP", character2, false);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600211B RID: 8475 RVA: 0x000920EC File Offset: 0x000902EC
		public static IncidentEffect RemovePrisonersRandomlyWithPredicate(Func<TroopRosterElement, bool> predicate, int amount)
		{
			return new IncidentEffect(() => MobileParty.MainParty.PrisonRoster.GetTroopRoster().Where(predicate).Sum((TroopRosterElement x) => x.Number) >= amount, delegate()
			{
				int i = amount;
				MBList<TroopRosterElement> troopRoster = MobileParty.MainParty.PrisonRoster.GetTroopRoster();
				while (i > 0)
				{
					List<TroopRosterElement> list = troopRoster.Where(predicate).ToList<TroopRosterElement>();
					TroopRosterElement troopRosterElement = list[MobileParty.MainParty.RandomInt(list.Count)];
					int num = Math.Min(MBRandom.RandomInt(1, troopRosterElement.Number), i);
					MobileParty.MainParty.PrisonRoster.AddToCounts(troopRosterElement.Character, -num, false, 0, 0, true, -1);
					i -= num;
				}
				TextObject textObject = new TextObject("{=tvshVXKT}Lost {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?}.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=6XW3fKhU}Lose {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?}", null);
				}
				else
				{
					textObject = new TextObject("{=F3AEpszA}{CHANCE}% chance of losing {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600211C RID: 8476 RVA: 0x00092138 File Offset: 0x00090338
		public static IncidentEffect ChangeItemsAmount(Func<List<ItemObject>> itemsGetter, int amount)
		{
			return new IncidentEffect(delegate()
			{
				IncidentEffect.<>c__DisplayClass48_1 CS$<>8__locals2 = new IncidentEffect.<>c__DisplayClass48_1();
				IncidentEffect.<>c__DisplayClass48_1 CS$<>8__locals3 = CS$<>8__locals2;
				Func<List<ItemObject>> itemsGetter2 = itemsGetter;
				CS$<>8__locals3.items = ((itemsGetter2 != null) ? itemsGetter2() : null);
				if (CS$<>8__locals2.items == null || CS$<>8__locals2.items.Count < 0)
				{
					return false;
				}
				if (amount >= 0)
				{
					return true;
				}
				return (from x in MobileParty.MainParty.ItemRoster
					where CS$<>8__locals2.items.Contains(x.EquipmentElement.Item)
					select x).Sum((ItemRosterElement x) => x.Amount) >= amount;
			}, delegate()
			{
				List<ItemObject> list = itemsGetter();
				ItemObject itemObject = list.First<ItemObject>();
				int i = Math.Abs(amount);
				while (i > 0)
				{
					ItemObject randomElement = list.GetRandomElement<ItemObject>();
					int index = MobileParty.MainParty.ItemRoster.FindIndexOfItem(randomElement);
					int elementNumber = MobileParty.MainParty.ItemRoster.GetElementNumber(index);
					int num = Math.Min(MBRandom.RandomInt(1, Math.Min(elementNumber, i)), i);
					MobileParty.MainParty.ItemRoster.AddToCounts(MobileParty.MainParty.ItemRoster[index].EquipmentElement, num * Math.Sign(amount));
					i -= num;
					if (elementNumber - num == 0)
					{
						list.Remove(randomElement);
					}
				}
				TextObject variable = ((list.Count == 1) ? list.First<ItemObject>().Name : itemObject.ItemCategory.GetName());
				TextObject textObject = new TextObject("{=shZVdlRQ}{?AMOUNT > 1}Received{?}Lost{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("ITEM", variable);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				List<ItemObject> list = itemsGetter();
				TextObject variable = ((list.Count == 1) ? list.First<ItemObject>().Name : list.First<ItemObject>().ItemCategory.GetName());
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=OZAKqzln}{?AMOUNT > 1}Get{?}Lose{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}", null);
				}
				else
				{
					textObject = new TextObject("{=aVtM937J}{CHANCE}% chance of {?AMOUNT > 1}getting{?}losing{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				textObject.SetTextVariable("ITEM", variable);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600211D RID: 8477 RVA: 0x00092184 File Offset: 0x00090384
		public static IncidentEffect ChangeItemAmount(Func<ItemObject> itemGetter, Func<int> amountGetter)
		{
			return new IncidentEffect(delegate()
			{
				Func<ItemObject> itemGetter2 = itemGetter;
				return ((itemGetter2 != null) ? itemGetter2() : null) != null && amountGetter != null && (amountGetter() > 0 || MobileParty.MainParty.ItemRoster.GetItemNumber(itemGetter()) >= Math.Abs(amountGetter()));
			}, delegate()
			{
				ItemObject itemObject = itemGetter();
				int num = amountGetter();
				MobileParty.MainParty.ItemRoster.AddToCounts(itemObject, num);
				TextObject textObject = new TextObject("{=0utzQGvE}{?AMOUNT >= 1}Received{?}Lost{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}.", null);
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
					textObject = new TextObject("{=GQWArgN4}{?AMOUNT >= 1}Get{?}Lose{\\?} {ABS(AMOUNT)} {?AMOUNT > 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}", null);
				}
				else
				{
					textObject = new TextObject("{=ZCIsZTe2}{CHANCE}% chance of {?AMOUNT >= 1}getting{?}losing{\\?} {ABS(AMOUNT)} {?AMOUNT >= 1}{PLURAL(ITEM)}{?}{ITEM}{\\?}", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", variable);
				textObject.SetTextVariable("ITEM", itemObject.Name);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600211E RID: 8478 RVA: 0x000921D0 File Offset: 0x000903D0
		public static IncidentEffect PartyExperienceChance(int amount)
		{
			return new IncidentEffect(null, delegate()
			{
				MobilePartyHelper.PartyAddSharedXp(MobileParty.MainParty, (float)amount);
				TextObject textObject = new TextObject("{=LgzX3fDk}Party gained {AMOUNT} shared experience.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=CCfVud1f}Party gains {AMOUNT} shared experience", null);
				}
				else
				{
					textObject = new TextObject("{=aFUVF8VO}{CHANCE}% chance of party gaining {AMOUNT} shared experience", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x0600211F RID: 8479 RVA: 0x00092208 File Offset: 0x00090408
		public static IncidentEffect DisorganizeParty()
		{
			return new IncidentEffect(null, delegate()
			{
				MobileParty.MainParty.SetDisorganized(true);
				TextObject item = new TextObject("{=ylqcMuBF}Your party got disorganized.", null);
				return new List<TextObject> { item };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=02DbEPC1}Party becomes disorganized", null);
				}
				else
				{
					textObject = new TextObject("{=aXXF3aJE}{CHANCE}% chance of party becoming disorganized", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002120 RID: 8480 RVA: 0x0009225C File Offset: 0x0009045C
		public static IncidentEffect HealTroopsRandomly(int amount)
		{
			return new IncidentEffect(null, delegate()
			{
				TroopRoster memberRoster = MobileParty.MainParty.MemberRoster;
				int num = MBRandom.RandomInt(memberRoster.Count);
				int num2 = 0;
				while (num2 < memberRoster.Count && amount > 0)
				{
					int index = (num + num2) % memberRoster.Count;
					if (memberRoster.GetCharacterAtIndex(index).IsRegular)
					{
						int num3 = MathF.Min(amount, memberRoster.GetElementWoundedNumber(index));
						if (num3 > 0)
						{
							memberRoster.AddToCountsAtIndex(index, 0, -num3, 0, true);
							amount -= num3;
						}
					}
					num2++;
				}
				TextObject textObject = new TextObject("{=pawoTBr8}Healed {AMOUNT} wounded troops.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				TextObject textObject;
				if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=EbBazlbZ}Heal {AMOUNT} wounded troops", null);
				}
				else
				{
					textObject = new TextObject("{=riVJZgSU}{CHANCE}% chance of healing {AMOUNT} wounded troops", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002121 RID: 8481 RVA: 0x00092294 File Offset: 0x00090494
		public static IncidentEffect DemoteTroopsRandomlyWithPredicate(Func<TroopRosterElement, bool> predicate, Func<CharacterObject, bool> demotionPredicate, int amount, bool specifyUnitTypeOnHint = true)
		{
			Func<TroopRosterElement, bool> <>9__3;
			Func<TroopRosterElement, bool> <>9__5;
			Func<TroopRosterElement, bool> <>9__6;
			return new IncidentEffect(delegate()
			{
				IEnumerable<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
				Func<TroopRosterElement, bool> predicate2;
				if ((predicate2 = <>9__3) == null)
				{
					predicate2 = (<>9__3 = delegate(TroopRosterElement x)
					{
						CharacterObject characterObject;
						return predicate != null && predicate(x) && x.Character != CharacterObject.PlayerCharacter && IncidentEffect.FindTroopToDemoteTo(x.Character, demotionPredicate, out characterObject);
					});
				}
				return troopRoster.Where(predicate2).Sum((TroopRosterElement x) => x.Number) > amount;
			}, delegate()
			{
				int i = amount;
				while (i > 0)
				{
					MBList<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
					Func<TroopRosterElement, bool> predicate2;
					if ((predicate2 = <>9__5) == null)
					{
						predicate2 = (<>9__5 = delegate(TroopRosterElement x)
						{
							CharacterObject characterObject;
							return predicate(x) && x.Character != CharacterObject.PlayerCharacter && IncidentEffect.FindTroopToDemoteTo(x.Character, demotionPredicate, out characterObject);
						});
					}
					TroopRosterElement randomElementWithPredicate = troopRoster.GetRandomElementWithPredicate(predicate2);
					CharacterObject character;
					IncidentEffect.FindTroopToDemoteTo(randomElementWithPredicate.Character, demotionPredicate, out character);
					if (randomElementWithPredicate.WoundedNumber > 0)
					{
						int num = Math.Min(MBRandom.RandomInt(1, Math.Min(randomElementWithPredicate.WoundedNumber, i)), i);
						MobileParty.MainParty.MemberRoster.AddToCounts(randomElementWithPredicate.Character, -num, false, num, 0, true, -1);
						MobileParty.MainParty.MemberRoster.AddToCounts(character, num, false, num, 0, true, -1);
						i -= num;
					}
					else
					{
						int num2 = Math.Min(MBRandom.RandomInt(1, Math.Min(randomElementWithPredicate.Number, i)), i);
						MobileParty.MainParty.MemberRoster.AddToCounts(randomElementWithPredicate.Character, -num2, false, 0, 0, true, -1);
						MobileParty.MainParty.MemberRoster.AddToCounts(character, num2, false, 0, 0, true, -1);
						i -= num2;
					}
				}
				TextObject textObject = new TextObject("{=211WkLlN}{AMOUNT} of your troops got demoted.", null);
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			}, delegate(IncidentEffect effect)
			{
				IEnumerable<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
				Func<TroopRosterElement, bool> predicate2;
				if ((predicate2 = <>9__6) == null)
				{
					predicate2 = (<>9__6 = (TroopRosterElement x) => predicate(x) && CharacterHelper.GetTroopTree(x.Character.Culture.BasicTroop, -1f, (float)(x.Character.Tier - 1)).FirstOrDefault(demotionPredicate) != null);
				}
				TroopRosterElement troopRosterElement = troopRoster.FirstOrDefault(predicate2);
				TextObject textObject;
				if (specifyUnitTypeOnHint)
				{
					if (effect._chanceToOccur >= 1f)
					{
						textObject = new TextObject("{=64YgbSH8}{AMOUNT} {UNIT_TYPE} get demoted", null);
					}
					else
					{
						textObject = new TextObject("{=CC1tYZMa}{CHANCE}% chance of {AMOUNT} {UNIT_TYPE} getting demoted", null);
						textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
					}
					textObject.SetTextVariable("UNIT_TYPE", troopRosterElement.Character.DefaultFormationClass.GetName());
				}
				else if (effect._chanceToOccur >= 1f)
				{
					textObject = new TextObject("{=fMCKvvCa}{AMOUNT} random troops get demoted", null);
				}
				else
				{
					textObject = new TextObject("{=47MFRfCt}{CHANCE}% chance of {AMOUNT} random troops getting demoted", null);
					textObject.SetTextVariable("CHANCE", MathF.Round(effect._chanceToOccur * 100f));
				}
				textObject.SetTextVariable("AMOUNT", amount);
				return new List<TextObject> { textObject };
			});
		}

		// Token: 0x06002122 RID: 8482 RVA: 0x000922EC File Offset: 0x000904EC
		private static bool FindTroopToDemoteTo(CharacterObject troop, Func<CharacterObject, bool> demotionPredicate, out CharacterObject troopToDemoteTo)
		{
			List<CharacterObject> source = CharacterHelper.GetTroopTree(troop.Culture.BasicTroop, -1f, (float)(troop.Tier - 1)).Where(demotionPredicate).ToList<CharacterObject>();
			if (source.Any<CharacterObject>())
			{
				IGrouping<int, CharacterObject> grouping = (from x in source
					group x by IncidentEffect.FindUpgradeDistanceBfs(x, troop) into x
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
					troopToDemoteTo = grouping.ToList<CharacterObject>().GetRandomElement<CharacterObject>();
					return true;
				}
			}
			List<CharacterObject> source2 = CharacterHelper.GetTroopTree(troop.Culture.EliteBasicTroop, -1f, (float)(troop.Tier - 1)).Where(demotionPredicate).ToList<CharacterObject>();
			if (source2.Any<CharacterObject>())
			{
				IGrouping<int, CharacterObject> grouping2 = (from x in source2
					group x by IncidentEffect.FindUpgradeDistanceBfs(x, troop) into x
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
					troopToDemoteTo = grouping2.ToList<CharacterObject>().GetRandomElement<CharacterObject>();
					return true;
				}
			}
			troopToDemoteTo = null;
			return false;
		}

		// Token: 0x06002123 RID: 8483 RVA: 0x000924A4 File Offset: 0x000906A4
		private static int FindUpgradeDistanceBfs(CharacterObject start, CharacterObject target)
		{
			if (start == target)
			{
				return 0;
			}
			HashSet<CharacterObject> hashSet = new HashSet<CharacterObject>();
			Queue<ValueTuple<CharacterObject, int>> queue = new Queue<ValueTuple<CharacterObject, int>>();
			queue.Enqueue(new ValueTuple<CharacterObject, int>(start, 0));
			hashSet.Add(start);
			while (queue.Count > 0)
			{
				ValueTuple<CharacterObject, int> valueTuple = queue.Dequeue();
				CharacterObject item = valueTuple.Item1;
				int item2 = valueTuple.Item2;
				foreach (CharacterObject characterObject in item.UpgradeTargets)
				{
					if (!hashSet.Contains(characterObject))
					{
						if (characterObject == target)
						{
							return item2 + 1;
						}
						hashSet.Add(characterObject);
						queue.Enqueue(new ValueTuple<CharacterObject, int>(characterObject, item2 + 1));
					}
				}
			}
			return -1;
		}

		// Token: 0x06002124 RID: 8484 RVA: 0x00092548 File Offset: 0x00090748
		public static IncidentEffect Group(params IncidentEffect[] effects)
		{
			return new IncidentEffect(() => effects.All((IncidentEffect x) => x.Condition()), delegate()
			{
				List<TextObject> list = new List<TextObject>();
				foreach (IncidentEffect incidentEffect in effects)
				{
					list.AddRange(incidentEffect.Consequence());
				}
				return list;
			}, delegate(IncidentEffect effect)
			{
				List<TextObject> list = new List<TextObject>();
				foreach (TextObject item in effects.SelectMany((IncidentEffect x) => x.GetHint()).ToList<TextObject>())
				{
					list.Add(item);
				}
				if (effect._chanceToOccur < 1f)
				{
					list.Insert(0, new TextObject("{=wa9W68Wp}{CHANCE}% chance of following occurs:", null).SetTextVariable("CHANCE", effect._chanceToOccur * 100f, 2));
				}
				return list;
			});
		}

		// Token: 0x06002125 RID: 8485 RVA: 0x0009258C File Offset: 0x0009078C
		public static IncidentEffect Select(IncidentEffect effectOne, IncidentEffect effectTwo, float chanceOfFirstOne)
		{
			return new IncidentEffect(() => effectOne.Condition() && effectTwo.Condition(), delegate()
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
			}, delegate(IncidentEffect effect)
			{
				List<TextObject> list = new List<TextObject>();
				list.Add(new TextObject("{=haovqFEg}{CHANCE}% chance of:", null).SetTextVariable("CHANCE", chanceOfFirstOne * 100f, 2));
				foreach (TextObject item in effectOne.GetHint())
				{
					list.Add(item);
				}
				list.Add(new TextObject("{=dQqR2DXD}or {CHANCE}% chance of:", null).SetTextVariable("CHANCE", (1f - chanceOfFirstOne) * 100f, 2));
				foreach (TextObject item2 in effectTwo.GetHint())
				{
					list.Add(item2);
				}
				return list;
			});
		}

		// Token: 0x06002126 RID: 8486 RVA: 0x000925DD File Offset: 0x000907DD
		public static IncidentEffect Custom(Func<bool> condition, Func<List<TextObject>> consequence, Func<IncidentEffect, List<TextObject>> hint)
		{
			return new IncidentEffect(condition, consequence, hint);
		}

		// Token: 0x0400099E RID: 2462
		private readonly Func<bool> _condition;

		// Token: 0x0400099F RID: 2463
		private readonly Func<List<TextObject>> _consequence;

		// Token: 0x040009A0 RID: 2464
		private Func<IncidentEffect, List<TextObject>> _hint;

		// Token: 0x040009A1 RID: 2465
		private Func<List<TextObject>> _customInformation;

		// Token: 0x040009A2 RID: 2466
		private float _chanceToOccur = 1f;
	}
}
