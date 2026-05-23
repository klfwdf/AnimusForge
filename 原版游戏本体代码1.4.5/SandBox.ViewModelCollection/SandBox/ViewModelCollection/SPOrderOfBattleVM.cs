using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace SandBox.ViewModelCollection;

public class SPOrderOfBattleVM : OrderOfBattleVM
{
	private OrderOfBattleCampaignBehavior _orderOfBattleBehavior;

	private static readonly TextObject _perkDefinitionText = new TextObject("{=jCdZY3i4}{PERK_NAME} ({SKILL_LEVEL} - {SKILL})");

	private readonly TextObject _captainPerksText = new TextObject("{=pgXuyHxH}Captain Perks");

	private readonly TextObject _infantryInfluenceText = new TextObject("{=SSLUHH6j}Infantry Influence");

	private readonly TextObject _rangedInfluenceText = new TextObject("{=0DMM0agr}Ranged Influence");

	private readonly TextObject _cavalryInfluenceText = new TextObject("{=X8i3jZn8}Cavalry Influence");

	private readonly TextObject _horseArcherInfluenceText = new TextObject("{=gZIOG0wl}Horse Archer Influence");

	private readonly TextObject _noPerksText = new TextObject("{=7yaDnyKb}There is no additional perk influence.");

	private readonly PerkObjectComparer _perkComparer = new PerkObjectComparer();

	public SPOrderOfBattleVM()
	{
		RefreshValues();
	}

	protected override void LoadConfiguration()
	{
		base.LoadConfiguration();
		_orderOfBattleBehavior = Campaign.Current.GetCampaignBehavior<OrderOfBattleCampaignBehavior>();
		if (_orderOfBattleBehavior == null || !base.IsPlayerGeneral)
		{
			return;
		}
		for (int i = 0; i < base.TotalFormationCount; i++)
		{
			OrderOfBattleCampaignBehavior.OrderOfBattleFormationData formationInfo = _orderOfBattleBehavior.GetFormationDataAtIndex(i, Mission.Current.IsSiegeBattle, MobileParty.MainParty.Army != null);
			if (formationInfo != null && formationInfo.FormationClass != DeploymentFormationClass.Unset)
			{
				bool flag = formationInfo.PrimaryClassWeight > 0 || formationInfo.SecondaryClassWeight > 0;
				if (formationInfo.FormationClass == DeploymentFormationClass.Infantry)
				{
					_allFormations[i].Classes[0].Class = FormationClass.Infantry;
					_allFormations[i].Classes[1].Class = FormationClass.NumberOfAllFormations;
				}
				else if (formationInfo.FormationClass == DeploymentFormationClass.Ranged)
				{
					_allFormations[i].Classes[0].Class = FormationClass.Ranged;
					_allFormations[i].Classes[1].Class = FormationClass.NumberOfAllFormations;
				}
				else if (formationInfo.FormationClass == DeploymentFormationClass.Cavalry)
				{
					_allFormations[i].Classes[0].Class = FormationClass.Cavalry;
					_allFormations[i].Classes[1].Class = FormationClass.NumberOfAllFormations;
				}
				else if (formationInfo.FormationClass == DeploymentFormationClass.HorseArcher)
				{
					_allFormations[i].Classes[0].Class = FormationClass.HorseArcher;
					_allFormations[i].Classes[1].Class = FormationClass.NumberOfAllFormations;
				}
				else if (formationInfo.FormationClass == DeploymentFormationClass.InfantryAndRanged)
				{
					_allFormations[i].Classes[0].Class = FormationClass.Infantry;
					_allFormations[i].Classes[1].Class = FormationClass.Ranged;
				}
				else if (formationInfo.FormationClass == DeploymentFormationClass.CavalryAndHorseArcher)
				{
					_allFormations[i].Classes[0].Class = FormationClass.Cavalry;
					_allFormations[i].Classes[1].Class = FormationClass.HorseArcher;
				}
				if (flag)
				{
					formationInfo.Filters.TryGetValue(FormationFilterType.Shield, out var value);
					formationInfo.Filters.TryGetValue(FormationFilterType.Spear, out var value2);
					formationInfo.Filters.TryGetValue(FormationFilterType.Thrown, out var value3);
					formationInfo.Filters.TryGetValue(FormationFilterType.Heavy, out var value4);
					formationInfo.Filters.TryGetValue(FormationFilterType.HighTier, out var value5);
					formationInfo.Filters.TryGetValue(FormationFilterType.LowTier, out var value6);
					_allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Shield).IsActive = value;
					_allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Spear).IsActive = value2;
					_allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Thrown).IsActive = value3;
					_allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Heavy).IsActive = value4;
					_allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.HighTier).IsActive = value5;
					_allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.LowTier).IsActive = value6;
				}
				else
				{
					ClearFormationItem(_allFormations[i]);
				}
				DeploymentFormationClass deploymentFormationClass = formationInfo.FormationClass;
				if (Mission.Current.IsSiegeBattle)
				{
					switch (deploymentFormationClass)
					{
					case DeploymentFormationClass.HorseArcher:
						deploymentFormationClass = DeploymentFormationClass.Ranged;
						break;
					case DeploymentFormationClass.Cavalry:
						deploymentFormationClass = DeploymentFormationClass.Infantry;
						break;
					case DeploymentFormationClass.CavalryAndHorseArcher:
						deploymentFormationClass = DeploymentFormationClass.InfantryAndRanged;
						break;
					}
				}
				_allFormations[i].RefreshFormation(_allFormations[i].Formation, deploymentFormationClass, flag);
				if (flag && formationInfo.Captain != null)
				{
					OrderOfBattleHeroItemVM orderOfBattleHeroItemVM = _allHeroes.FirstOrDefault((OrderOfBattleHeroItemVM c) => c.Agent.Character == formationInfo.Captain.CharacterObject);
					if (orderOfBattleHeroItemVM != null)
					{
						AssignCaptain(orderOfBattleHeroItemVM.Agent, _allFormations[i]);
					}
				}
				if (!flag || formationInfo.HeroTroops == null)
				{
					continue;
				}
				Hero[] heroTroops = formationInfo.HeroTroops;
				foreach (Hero heroTroop in heroTroops)
				{
					OrderOfBattleHeroItemVM orderOfBattleHeroItemVM2 = _allHeroes.FirstOrDefault((OrderOfBattleHeroItemVM ht) => ht.Agent.Character == heroTroop.CharacterObject);
					if (orderOfBattleHeroItemVM2 != null)
					{
						_allFormations[i].AddHeroTroop(orderOfBattleHeroItemVM2);
					}
				}
			}
			else if (formationInfo != null)
			{
				ClearFormationItem(_allFormations[i]);
			}
		}
		for (int num2 = 0; num2 < base.TotalFormationCount; num2++)
		{
			OrderOfBattleCampaignBehavior.OrderOfBattleFormationData formationDataAtIndex = _orderOfBattleBehavior.GetFormationDataAtIndex(num2, Mission.Current.IsSiegeBattle, MobileParty.MainParty.Army != null);
			if (formationDataAtIndex != null && formationDataAtIndex.FormationClass != DeploymentFormationClass.Unset)
			{
				if (_allFormations[num2].Classes[0].Class != FormationClass.NumberOfAllFormations)
				{
					_allFormations[num2].Classes[0].Weight = formationDataAtIndex.PrimaryClassWeight;
				}
				if (_allFormations[num2].Classes[1].Class != FormationClass.NumberOfAllFormations)
				{
					_allFormations[num2].Classes[1].Weight = formationDataAtIndex.SecondaryClassWeight;
				}
			}
		}
	}

	protected override void SaveConfiguration()
	{
		base.SaveConfiguration();
		bool flag = MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle();
		if (_orderOfBattleBehavior != null && !(base.IsPlayerGeneral && flag))
		{
			return;
		}
		List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData> list = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
		for (int i = 0; i < base.TotalFormationCount; i++)
		{
			OrderOfBattleFormationItemVM formationItemVM = _allFormations[i];
			Hero captain = null;
			if (formationItemVM.Captain.Agent != null)
			{
				captain = Hero.FindFirst((Hero h) => h.CharacterObject == formationItemVM.Captain.Agent.Character);
			}
			Hero[] heroTroops = (from ht in formationItemVM.HeroTroops
				select Hero.FindFirst((Hero hero) => hero.CharacterObject == ht.Agent.Character) into h
				where h != null
				select h).ToArray();
			DeploymentFormationClass orderOfBattleClass = formationItemVM.GetOrderOfBattleClass();
			bool flag2 = orderOfBattleClass == DeploymentFormationClass.Unset;
			int primaryWeight = ((!flag2) ? formationItemVM.Classes[0].Weight : 0);
			int secondaryWeight = ((!flag2) ? formationItemVM.Classes[1].Weight : 0);
			Dictionary<FormationFilterType, bool> filters = new Dictionary<FormationFilterType, bool>
			{
				[FormationFilterType.Shield] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Shield),
				[FormationFilterType.Spear] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Spear),
				[FormationFilterType.Thrown] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Thrown),
				[FormationFilterType.Heavy] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Heavy),
				[FormationFilterType.HighTier] = !flag2 && formationItemVM.HasFilter(FormationFilterType.HighTier),
				[FormationFilterType.LowTier] = !flag2 && formationItemVM.HasFilter(FormationFilterType.LowTier)
			};
			list.Add(new OrderOfBattleCampaignBehavior.OrderOfBattleFormationData(captain, heroTroops, orderOfBattleClass, primaryWeight, secondaryWeight, filters));
		}
		_orderOfBattleBehavior.SetFormationInfos(list, Mission.Current.IsSiegeBattle, MobileParty.MainParty.Army != null);
	}

	protected override List<TooltipProperty> GetAgentTooltip(Agent agent)
	{
		List<TooltipProperty> agentTooltip = base.GetAgentTooltip(agent);
		if (agent == null)
		{
			return agentTooltip;
		}
		Hero hero = Hero.FindFirst((Hero h) => h.StringId == agent.Character.StringId);
		if (hero == null)
		{
			return agentTooltip;
		}
		agentTooltip.Add(new TooltipProperty(string.Empty, string.Empty, 0));
		foreach (SkillObject item in Skills.All)
		{
			if (item.StringId == "OneHanded" || item.StringId == "TwoHanded" || item.StringId == "Polearm" || item.StringId == "Bow" || item.StringId == "Crossbow" || item.StringId == "Throwing" || item.StringId == "Riding" || item.StringId == "Athletics" || item.StringId == "Tactics" || item.StringId == "Leadership")
			{
				agentTooltip.Add(new TooltipProperty(item.Name.ToString(), agent.Character.GetSkillValue(item).ToString(), 0)
				{
					OnlyShowWhenNotExtended = true
				});
			}
		}
		agentTooltip.Add(new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator)
		{
			OnlyShowWhenNotExtended = true
		});
		List<PerkObject> compatiblePerks;
		float captainRatingForTroopUsages = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Infantry.GetTroopUsageFlags(), out compatiblePerks);
		List<PerkObject> compatiblePerks2;
		float captainRatingForTroopUsages2 = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Ranged.GetTroopUsageFlags(), out compatiblePerks2);
		List<PerkObject> compatiblePerks3;
		float captainRatingForTroopUsages3 = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Cavalry.GetTroopUsageFlags(), out compatiblePerks3);
		List<PerkObject> compatiblePerks4;
		float captainRatingForTroopUsages4 = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.HorseArcher.GetTroopUsageFlags(), out compatiblePerks4);
		agentTooltip.Add(new TooltipProperty(_infantryInfluenceText.ToString(), ((int)(captainRatingForTroopUsages * 100f)).ToString(), 0)
		{
			OnlyShowWhenNotExtended = true
		});
		agentTooltip.Add(new TooltipProperty(_rangedInfluenceText.ToString(), ((int)(captainRatingForTroopUsages2 * 100f)).ToString(), 0)
		{
			OnlyShowWhenNotExtended = true
		});
		agentTooltip.Add(new TooltipProperty(_cavalryInfluenceText.ToString(), ((int)(captainRatingForTroopUsages3 * 100f)).ToString(), 0)
		{
			OnlyShowWhenNotExtended = true
		});
		agentTooltip.Add(new TooltipProperty(_horseArcherInfluenceText.ToString(), ((int)(captainRatingForTroopUsages4 * 100f)).ToString(), 0)
		{
			OnlyShowWhenNotExtended = true
		});
		agentTooltip.Add(new TooltipProperty(string.Empty, string.Empty, 0)
		{
			OnlyShowWhenNotExtended = true
		});
		List<PerkObject> list = compatiblePerks.Union(compatiblePerks2).Union(compatiblePerks3).Union(compatiblePerks4)
			.ToList();
		list.Sort(_perkComparer);
		bool num = list.Count != 0;
		if (num)
		{
			AddPerks(_captainPerksText, agentTooltip, list);
		}
		if (!num)
		{
			agentTooltip.Add(new TooltipProperty(_noPerksText.ToString(), string.Empty, 0, onlyShowWhenExtended: true));
		}
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", "MapFollowModifier").ToString());
		}
		else
		{
			GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.FindText("str_game_key_text", "anyalt").ToString());
		}
		agentTooltip.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_map_tooltip_info").ToString(), -1)
		{
			OnlyShowWhenNotExtended = true
		});
		return agentTooltip;
	}

	private static void AddPerks(TextObject title, List<TooltipProperty> tooltipProperties, List<PerkObject> perks)
	{
		tooltipProperties.Add(new TooltipProperty(title.ToString(), string.Empty, 0, onlyShowWhenExtended: true, TooltipProperty.TooltipPropertyFlags.Title));
		foreach (PerkObject perk in perks)
		{
			if (perk.PrimaryRole == PartyRole.Captain || perk.SecondaryRole == PartyRole.Captain)
			{
				TextObject textObject = ((perk.PrimaryRole == PartyRole.Captain) ? perk.PrimaryDescription : perk.SecondaryDescription);
				string genericImageText = HyperlinkTexts.GetGenericImageText(CampaignUIHelper.GetSkillMeshId(perk.Skill), 2);
				_perkDefinitionText.SetTextVariable("PERK_NAME", perk.Name).SetTextVariable("SKILL", genericImageText).SetTextVariable("SKILL_LEVEL", perk.RequiredSkillValue);
				tooltipProperties.Add(new TooltipProperty(_perkDefinitionText.ToString(), textObject.ToString(), 0, onlyShowWhenExtended: true));
			}
		}
	}
}
