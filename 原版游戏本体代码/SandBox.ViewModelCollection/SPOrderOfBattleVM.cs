using System;
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

namespace SandBox.ViewModelCollection
{
	// Token: 0x02000007 RID: 7
	public class SPOrderOfBattleVM : OrderOfBattleVM
	{
		// Token: 0x0600004A RID: 74 RVA: 0x00003218 File Offset: 0x00001418
		public SPOrderOfBattleVM()
		{
			this.RefreshValues();
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000032A4 File Offset: 0x000014A4
		protected override void LoadConfiguration()
		{
			base.LoadConfiguration();
			this._orderOfBattleBehavior = Campaign.Current.GetCampaignBehavior<OrderOfBattleCampaignBehavior>();
			if (this._orderOfBattleBehavior == null || !base.IsPlayerGeneral)
			{
				return;
			}
			for (int i = 0; i < base.TotalFormationCount; i++)
			{
				OrderOfBattleCampaignBehavior.OrderOfBattleFormationData formationInfo = this._orderOfBattleBehavior.GetFormationDataAtIndex(i, Mission.Current.IsSiegeBattle, MobileParty.MainParty.Army != null);
				if (formationInfo != null && formationInfo.FormationClass != DeploymentFormationClass.Unset)
				{
					bool flag = formationInfo.PrimaryClassWeight > 0 || formationInfo.SecondaryClassWeight > 0;
					if (formationInfo.FormationClass == DeploymentFormationClass.Infantry)
					{
						this._allFormations[i].Classes[0].Class = FormationClass.Infantry;
					}
					else if (formationInfo.FormationClass == DeploymentFormationClass.Ranged)
					{
						this._allFormations[i].Classes[0].Class = FormationClass.Ranged;
					}
					else if (formationInfo.FormationClass == DeploymentFormationClass.Cavalry)
					{
						this._allFormations[i].Classes[0].Class = FormationClass.Cavalry;
					}
					else if (formationInfo.FormationClass == DeploymentFormationClass.HorseArcher)
					{
						this._allFormations[i].Classes[0].Class = FormationClass.HorseArcher;
					}
					else if (formationInfo.FormationClass == DeploymentFormationClass.InfantryAndRanged)
					{
						this._allFormations[i].Classes[0].Class = FormationClass.Infantry;
						this._allFormations[i].Classes[1].Class = FormationClass.Ranged;
					}
					else if (formationInfo.FormationClass == DeploymentFormationClass.CavalryAndHorseArcher)
					{
						this._allFormations[i].Classes[0].Class = FormationClass.Cavalry;
						this._allFormations[i].Classes[1].Class = FormationClass.HorseArcher;
					}
					if (flag)
					{
						bool isActive;
						formationInfo.Filters.TryGetValue(FormationFilterType.Shield, out isActive);
						bool isActive2;
						formationInfo.Filters.TryGetValue(FormationFilterType.Spear, out isActive2);
						bool isActive3;
						formationInfo.Filters.TryGetValue(FormationFilterType.Thrown, out isActive3);
						bool isActive4;
						formationInfo.Filters.TryGetValue(FormationFilterType.Heavy, out isActive4);
						bool isActive5;
						formationInfo.Filters.TryGetValue(FormationFilterType.HighTier, out isActive5);
						bool isActive6;
						formationInfo.Filters.TryGetValue(FormationFilterType.LowTier, out isActive6);
						this._allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Shield).IsActive = isActive;
						this._allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Spear).IsActive = isActive2;
						this._allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Thrown).IsActive = isActive3;
						this._allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Heavy).IsActive = isActive4;
						this._allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.HighTier).IsActive = isActive5;
						this._allFormations[i].FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.LowTier).IsActive = isActive6;
					}
					else
					{
						base.ClearFormationItem(this._allFormations[i]);
					}
					DeploymentFormationClass deploymentFormationClass = formationInfo.FormationClass;
					if (Mission.Current.IsSiegeBattle)
					{
						if (deploymentFormationClass == DeploymentFormationClass.HorseArcher)
						{
							deploymentFormationClass = DeploymentFormationClass.Ranged;
						}
						else if (deploymentFormationClass == DeploymentFormationClass.Cavalry)
						{
							deploymentFormationClass = DeploymentFormationClass.Infantry;
						}
						else if (deploymentFormationClass == DeploymentFormationClass.CavalryAndHorseArcher)
						{
							deploymentFormationClass = DeploymentFormationClass.InfantryAndRanged;
						}
					}
					this._allFormations[i].RefreshFormation(this._allFormations[i].Formation, deploymentFormationClass, flag);
					if (flag && formationInfo.Captain != null)
					{
						OrderOfBattleHeroItemVM orderOfBattleHeroItemVM = this._allHeroes.FirstOrDefault((OrderOfBattleHeroItemVM c) => c.Agent.Character == formationInfo.Captain.CharacterObject);
						if (orderOfBattleHeroItemVM != null)
						{
							base.AssignCaptain(orderOfBattleHeroItemVM.Agent, this._allFormations[i]);
						}
					}
					if (flag && formationInfo.HeroTroops != null)
					{
						Hero[] heroTroops = formationInfo.HeroTroops;
						for (int j = 0; j < heroTroops.Length; j++)
						{
							Hero heroTroop = heroTroops[j];
							OrderOfBattleHeroItemVM orderOfBattleHeroItemVM2 = this._allHeroes.FirstOrDefault((OrderOfBattleHeroItemVM ht) => ht.Agent.Character == heroTroop.CharacterObject);
							if (orderOfBattleHeroItemVM2 != null)
							{
								this._allFormations[i].AddHeroTroop(orderOfBattleHeroItemVM2);
							}
						}
					}
				}
				else if (formationInfo != null)
				{
					base.ClearFormationItem(this._allFormations[i]);
				}
			}
			for (int k = 0; k < base.TotalFormationCount; k++)
			{
				OrderOfBattleCampaignBehavior.OrderOfBattleFormationData formationDataAtIndex = this._orderOfBattleBehavior.GetFormationDataAtIndex(k, Mission.Current.IsSiegeBattle, MobileParty.MainParty.Army != null);
				if (formationDataAtIndex != null && formationDataAtIndex.FormationClass != DeploymentFormationClass.Unset)
				{
					if (this._allFormations[k].Classes[0].Class != FormationClass.NumberOfAllFormations)
					{
						this._allFormations[k].Classes[0].Weight = formationDataAtIndex.PrimaryClassWeight;
					}
					if (this._allFormations[k].Classes[1].Class != FormationClass.NumberOfAllFormations)
					{
						this._allFormations[k].Classes[1].Weight = formationDataAtIndex.SecondaryClassWeight;
					}
				}
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000038B4 File Offset: 0x00001AB4
		protected override void SaveConfiguration()
		{
			base.SaveConfiguration();
			bool flag = MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle();
			if (this._orderOfBattleBehavior == null || (base.IsPlayerGeneral && flag))
			{
				List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData> list = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
				for (int i = 0; i < base.TotalFormationCount; i++)
				{
					OrderOfBattleFormationItemVM formationItemVM = this._allFormations[i];
					Hero captain = null;
					if (formationItemVM.Captain.Agent != null)
					{
						captain = Hero.FindFirst((Hero h) => h.CharacterObject == formationItemVM.Captain.Agent.Character);
					}
					List<Hero> heroTroops = (from ht in formationItemVM.HeroTroops
						select Hero.FindFirst((Hero hero) => hero.CharacterObject == ht.Agent.Character) into h
						where h != null
						select h).ToList<Hero>();
					DeploymentFormationClass orderOfBattleClass = formationItemVM.GetOrderOfBattleClass();
					bool flag2 = orderOfBattleClass == DeploymentFormationClass.Unset;
					int primaryWeight = (flag2 ? 0 : formationItemVM.Classes[0].Weight);
					int secondaryWeight = (flag2 ? 0 : formationItemVM.Classes[1].Weight);
					Dictionary<FormationFilterType, bool> dictionary = new Dictionary<FormationFilterType, bool>();
					dictionary[FormationFilterType.Shield] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Shield);
					dictionary[FormationFilterType.Spear] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Spear);
					dictionary[FormationFilterType.Thrown] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Thrown);
					dictionary[FormationFilterType.Heavy] = !flag2 && formationItemVM.HasFilter(FormationFilterType.Heavy);
					dictionary[FormationFilterType.HighTier] = !flag2 && formationItemVM.HasFilter(FormationFilterType.HighTier);
					dictionary[FormationFilterType.LowTier] = !flag2 && formationItemVM.HasFilter(FormationFilterType.LowTier);
					Dictionary<FormationFilterType, bool> filters = dictionary;
					list.Add(new OrderOfBattleCampaignBehavior.OrderOfBattleFormationData(captain, heroTroops, orderOfBattleClass, primaryWeight, secondaryWeight, filters));
				}
				this._orderOfBattleBehavior.SetFormationInfos(list, Mission.Current.IsSiegeBattle, MobileParty.MainParty.Army != null);
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003AD4 File Offset: 0x00001CD4
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
			agentTooltip.Add(new TooltipProperty(string.Empty, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.None));
			foreach (SkillObject skillObject in Skills.All)
			{
				if (skillObject.StringId == "OneHanded" || skillObject.StringId == "TwoHanded" || skillObject.StringId == "Polearm" || skillObject.StringId == "Bow" || skillObject.StringId == "Crossbow" || skillObject.StringId == "Throwing" || skillObject.StringId == "Riding" || skillObject.StringId == "Athletics" || skillObject.StringId == "Tactics" || skillObject.StringId == "Leadership")
				{
					agentTooltip.Add(new TooltipProperty(skillObject.Name.ToString(), agent.Character.GetSkillValue(skillObject).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None)
					{
						OnlyShowWhenNotExtended = true
					});
				}
			}
			agentTooltip.Add(new TooltipProperty("", string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator)
			{
				OnlyShowWhenNotExtended = true
			});
			List<PerkObject> first;
			float captainRatingForTroopUsages = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Infantry.GetTroopUsageFlags(), out first);
			List<PerkObject> second;
			float captainRatingForTroopUsages2 = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Ranged.GetTroopUsageFlags(), out second);
			List<PerkObject> second2;
			float captainRatingForTroopUsages3 = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Cavalry.GetTroopUsageFlags(), out second2);
			List<PerkObject> second3;
			float captainRatingForTroopUsages4 = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.HorseArcher.GetTroopUsageFlags(), out second3);
			agentTooltip.Add(new TooltipProperty(this._infantryInfluenceText.ToString(), ((int)(captainRatingForTroopUsages * 100f)).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
			agentTooltip.Add(new TooltipProperty(this._rangedInfluenceText.ToString(), ((int)(captainRatingForTroopUsages2 * 100f)).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
			agentTooltip.Add(new TooltipProperty(this._cavalryInfluenceText.ToString(), ((int)(captainRatingForTroopUsages3 * 100f)).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
			agentTooltip.Add(new TooltipProperty(this._horseArcherInfluenceText.ToString(), ((int)(captainRatingForTroopUsages4 * 100f)).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
			agentTooltip.Add(new TooltipProperty(string.Empty, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
			List<PerkObject> list = first.Union(second).Union(second2).Union(second3)
				.ToList<PerkObject>();
			list.Sort(this._perkComparer);
			bool flag = list.Count != 0;
			if (flag)
			{
				SPOrderOfBattleVM.AddPerks(this._captainPerksText, agentTooltip, list);
			}
			if (!flag)
			{
				agentTooltip.Add(new TooltipProperty(this._noPerksText.ToString(), string.Empty, 0, true, TooltipProperty.TooltipPropertyFlags.None));
			}
			if (Input.IsGamepadActive)
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", "MapFollowModifier").ToString());
			}
			else
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.FindText("str_game_key_text", "anyalt").ToString());
			}
			agentTooltip.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_map_tooltip_info", null).ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
			return agentTooltip;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003EF8 File Offset: 0x000020F8
		private static void AddPerks(TextObject title, List<TooltipProperty> tooltipProperties, List<PerkObject> perks)
		{
			tooltipProperties.Add(new TooltipProperty(title.ToString(), string.Empty, 0, true, TooltipProperty.TooltipPropertyFlags.Title));
			foreach (PerkObject perkObject in perks)
			{
				if (perkObject.PrimaryRole == PartyRole.Captain || perkObject.SecondaryRole == PartyRole.Captain)
				{
					TextObject textObject = ((perkObject.PrimaryRole == PartyRole.Captain) ? perkObject.PrimaryDescription : perkObject.SecondaryDescription);
					string genericImageText = HyperlinkTexts.GetGenericImageText(CampaignUIHelper.GetSkillMeshId(perkObject.Skill, true), 2);
					SPOrderOfBattleVM._perkDefinitionText.SetTextVariable("PERK_NAME", perkObject.Name).SetTextVariable("SKILL", genericImageText).SetTextVariable("SKILL_LEVEL", perkObject.RequiredSkillValue, 2);
					tooltipProperties.Add(new TooltipProperty(SPOrderOfBattleVM._perkDefinitionText.ToString(), textObject.ToString(), 0, true, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
		}

		// Token: 0x0400001C RID: 28
		private OrderOfBattleCampaignBehavior _orderOfBattleBehavior;

		// Token: 0x0400001D RID: 29
		private static readonly TextObject _perkDefinitionText = new TextObject("{=jCdZY3i4}{PERK_NAME} ({SKILL_LEVEL} - {SKILL})", null);

		// Token: 0x0400001E RID: 30
		private readonly TextObject _captainPerksText = new TextObject("{=pgXuyHxH}Captain Perks", null);

		// Token: 0x0400001F RID: 31
		private readonly TextObject _infantryInfluenceText = new TextObject("{=SSLUHH6j}Infantry Influence", null);

		// Token: 0x04000020 RID: 32
		private readonly TextObject _rangedInfluenceText = new TextObject("{=0DMM0agr}Ranged Influence", null);

		// Token: 0x04000021 RID: 33
		private readonly TextObject _cavalryInfluenceText = new TextObject("{=X8i3jZn8}Cavalry Influence", null);

		// Token: 0x04000022 RID: 34
		private readonly TextObject _horseArcherInfluenceText = new TextObject("{=gZIOG0wl}Horse Archer Influence", null);

		// Token: 0x04000023 RID: 35
		private readonly TextObject _noPerksText = new TextObject("{=7yaDnyKb}There is no additional perk influence.", null);

		// Token: 0x04000024 RID: 36
		private readonly PerkObjectComparer _perkComparer = new PerkObjectComparer();
	}
}
