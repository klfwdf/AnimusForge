using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.Information;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000018 RID: 24
	public static class CampaignUIHelper
	{
		// Token: 0x060000F4 RID: 244 RVA: 0x000047A0 File Offset: 0x000029A0
		private static void TooltipAddPropertyTitleWithValue(List<TooltipProperty> properties, string propertyName, float currentValue)
		{
			string value = currentValue.ToString("0.##");
			properties.Add(new TooltipProperty(propertyName, value, 0, false, TooltipProperty.TooltipPropertyFlags.Title));
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x000047CE File Offset: 0x000029CE
		private static void TooltipAddPropertyTitleWithValue(List<TooltipProperty> properties, string propertyName, string currentValue)
		{
			properties.Add(new TooltipProperty(propertyName, currentValue, 0, false, TooltipProperty.TooltipPropertyFlags.Title));
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x000047E4 File Offset: 0x000029E4
		private static void TooltipAddExplanation(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
		{
			List<ValueTuple<string, float>> lines = explainedNumber.GetLines();
			if (lines.Count > 0)
			{
				for (int i = 0; i < lines.Count; i++)
				{
					ValueTuple<string, float> valueTuple = lines[i];
					string item = valueTuple.Item1;
					float item2 = valueTuple.Item2;
					if ((double)MathF.Abs(item2) >= 0.01)
					{
						string changeValueString = CampaignUIHelper.GetChangeValueString(item2);
						properties.Add(new TooltipProperty(item, changeValueString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
				}
			}
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00004851 File Offset: 0x00002A51
		private static void TooltipAddPropertyTitle(List<TooltipProperty> properties, string propertyName)
		{
			properties.Add(new TooltipProperty(propertyName, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.Title));
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000486C File Offset: 0x00002A6C
		private static void TooltipAddExplainedResultChange(List<TooltipProperty> properties, float changeValue)
		{
			string changeValueString = CampaignUIHelper.GetChangeValueString(changeValue);
			properties.Add(new TooltipProperty(CampaignUIHelper._changeStr.ToString(), changeValueString, 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000489D File Offset: 0x00002A9D
		private static void TooltipAddExplanedChange(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
		{
			CampaignUIHelper.TooltipAddExplanation(properties, ref explainedNumber);
			CampaignUIHelper.TooltipAddDoubleSeperator(properties, false);
			CampaignUIHelper.TooltipAddExplainedResultChange(properties, explainedNumber.ResultNumber);
		}

		// Token: 0x060000FA RID: 250 RVA: 0x000048BC File Offset: 0x00002ABC
		private static void TooltipAddExplainedResultTotal(List<TooltipProperty> properties, float changeValue)
		{
			string changeValueString = CampaignUIHelper.GetChangeValueString(changeValue);
			properties.Add(new TooltipProperty(CampaignUIHelper._totalStr.ToString(), changeValueString, 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
		}

		// Token: 0x060000FB RID: 251 RVA: 0x000048ED File Offset: 0x00002AED
		public static List<TooltipProperty> GetTooltipForAccumulatingProperty(string propertyName, float currentValue, ExplainedNumber explainedNumber)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, propertyName, currentValue);
			CampaignUIHelper.TooltipAddExplanedChange(list, ref explainedNumber);
			return list;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00004904 File Offset: 0x00002B04
		public static List<TooltipProperty> GetTooltipForAccumulatingPropertyWithResult(string propertyName, float currentValue, ref ExplainedNumber explainedNumber)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			CampaignUIHelper.TooltipAddPropertyTitle(list, propertyName);
			CampaignUIHelper.TooltipAddExplanation(list, ref explainedNumber);
			CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
			CampaignUIHelper.TooltipAddExplainedResultTotal(list, currentValue);
			return list;
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00004927 File Offset: 0x00002B27
		public static List<TooltipProperty> GetTooltipForgProperty(string propertyName, float currentValue, ExplainedNumber explainedNumber)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, propertyName, currentValue);
			CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
			CampaignUIHelper.TooltipAddExplanation(list, ref explainedNumber);
			return list;
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00004945 File Offset: 0x00002B45
		private static void TooltipAddSeperator(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
		{
			properties.Add(new TooltipProperty("", string.Empty, 0, onlyShowOnExtend, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00004963 File Offset: 0x00002B63
		private static void TooltipAddDoubleSeperator(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
		{
			properties.Add(new TooltipProperty("", string.Empty, 0, onlyShowOnExtend, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00004984 File Offset: 0x00002B84
		private static void TooltipAddExtendInfo(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
		{
			properties.Add(new TooltipProperty("", "", -1, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
			if (Input.IsGamepadActive)
			{
				if (Input.ControllerType.IsPlaystation())
				{
					GameTexts.SetVariable("EXTEND_KEY", GameTexts.FindText("str_game_key_text", "controllerlbumper_ps").ToString());
				}
				else
				{
					GameTexts.SetVariable("EXTEND_KEY", GameTexts.FindText("str_game_key_text", "controllerlbumper").ToString());
				}
			}
			else
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.FindText("str_game_key_text", "anyalt").ToString());
			}
			properties.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_map_tooltip_info", null).ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.None)
			{
				OnlyShowWhenNotExtended = true
			});
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00004A55 File Offset: 0x00002C55
		private static void TooltipAddEmptyLine(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
		{
			properties.Add(new TooltipProperty(string.Empty, string.Empty, -1, onlyShowOnExtend, TooltipProperty.TooltipPropertyFlags.None));
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00004A70 File Offset: 0x00002C70
		public static string GetTownWallsTooltip(Town town)
		{
			TextObject textObject;
			bool flag = CampaignUIHelper.IsSettlementInformationHidden(town.Settlement, out textObject);
			GameTexts.SetVariable("newline", "\n");
			if (flag)
			{
				GameTexts.SetVariable("LEVEL", GameTexts.FindText("str_missing_info_indicator", null).ToString());
			}
			else
			{
				GameTexts.SetVariable("LEVEL", town.GetWallLevel());
			}
			return GameTexts.FindText("str_walls_with_value", null).ToString();
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00004AD7 File Offset: 0x00002CD7
		public static List<TooltipProperty> GetVillageMilitiaTooltip(Village village)
		{
			return CampaignUIHelper.GetSettlementPropertyTooltip(village.Settlement, CampaignUIHelper._militiaStr.ToString(), village.Militia, village.MilitiaChangeExplanation);
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00004AFA File Offset: 0x00002CFA
		public static List<TooltipProperty> GetTownMilitiaTooltip(Town town)
		{
			return CampaignUIHelper.GetSettlementPropertyTooltip(town.Settlement, CampaignUIHelper._militiaStr.ToString(), town.Militia, town.MilitiaChangeExplanation);
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00004B1D File Offset: 0x00002D1D
		public static List<TooltipProperty> GetTownFoodTooltip(Town town)
		{
			return CampaignUIHelper.GetSettlementPropertyTooltip(town.Settlement, CampaignUIHelper._foodStr.ToString(), town.FoodStocks, town.FoodChangeExplanation);
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00004B40 File Offset: 0x00002D40
		public static List<TooltipProperty> GetTownLoyaltyTooltip(Town town)
		{
			TextObject textObject;
			bool flag = CampaignUIHelper.IsSettlementInformationHidden(town.Settlement, out textObject);
			ExplainedNumber loyaltyChangeExplanation = town.LoyaltyChangeExplanation;
			List<TooltipProperty> settlementPropertyTooltip = CampaignUIHelper.GetSettlementPropertyTooltip(town.Settlement, CampaignUIHelper._loyaltyStr.ToString(), town.Loyalty, loyaltyChangeExplanation);
			if (!flag)
			{
				if (!town.OwnerClan.IsRebelClan)
				{
					if (town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebellionStartLoyaltyThreshold)
					{
						CampaignUIHelper.TooltipAddSeperator(settlementPropertyTooltip, false);
						settlementPropertyTooltip.Add(new TooltipProperty(" ", new TextObject("{=NxEy5Nbt}High risk of rebellion", null).ToString(), 1, UIColors.NegativeIndicator, false, TooltipProperty.TooltipPropertyFlags.None));
					}
					else if (town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold && loyaltyChangeExplanation.ResultNumber < 0f)
					{
						CampaignUIHelper.TooltipAddSeperator(settlementPropertyTooltip, false);
						settlementPropertyTooltip.Add(new TooltipProperty(" ", new TextObject("{=F0a7hyp0}Risk of rebellion", null).ToString(), 1, UIColors.NegativeIndicator, false, TooltipProperty.TooltipPropertyFlags.None));
					}
				}
				else
				{
					CampaignUIHelper.TooltipAddSeperator(settlementPropertyTooltip, false);
					settlementPropertyTooltip.Add(new TooltipProperty(" ", new TextObject("{=hOVPiG3z}Recently rebelled", null).ToString(), 1, UIColors.NegativeIndicator, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return settlementPropertyTooltip;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00004C70 File Offset: 0x00002E70
		public static List<TooltipProperty> GetTownProsperityTooltip(Town town)
		{
			return CampaignUIHelper.GetSettlementPropertyTooltip(town.Settlement, CampaignUIHelper._prosperityStr.ToString(), town.Prosperity, town.ProsperityChangeExplanation);
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00004C94 File Offset: 0x00002E94
		public static List<TooltipProperty> GetTownDailyProductionTooltip(Town town)
		{
			ExplainedNumber constructionExplanation = town.ConstructionExplanation;
			return CampaignUIHelper.GetSettlementPropertyTooltipWithResult(town.Settlement, CampaignUIHelper._dailyProductionStr.ToString(), constructionExplanation.ResultNumber, ref constructionExplanation);
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00004CC8 File Offset: 0x00002EC8
		public static List<TooltipProperty> GetTownSecurityTooltip(Town town)
		{
			ExplainedNumber securityChangeExplanation = town.SecurityChangeExplanation;
			return CampaignUIHelper.GetSettlementPropertyTooltip(town.Settlement, CampaignUIHelper._securityStr.ToString(), town.Security, securityChangeExplanation);
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00004CF8 File Offset: 0x00002EF8
		public static string GetTownPatrolTooltip(Town town)
		{
			TextObject textObject = GameTexts.FindText("str_string_newline_string", null);
			textObject.SetTextVariable("newline", "\n");
			textObject.SetTextVariable("STR1", CampaignUIHelper._patrolStr.ToString());
			TextObject textObject2;
			if (CampaignUIHelper.IsSettlementInformationHidden(town.Settlement, out textObject2))
			{
				textObject.SetTextVariable("STR2", GameTexts.FindText("str_missing_info_indicator", null).ToString());
			}
			else if (town.Settlement.PatrolParty != null)
			{
				textObject.SetTextVariable("STR2", town.Settlement.PatrolParty.MobileParty.GetBehaviorText().ToString());
			}
			else
			{
				textObject.SetTextVariable("STR2", Campaign.Current.GetCampaignBehavior<IPatrolPartiesCampaignBehavior>().GetSettlementPatrolStatus(town.Settlement).ToString());
			}
			return textObject.ToString();
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00004DC6 File Offset: 0x00002FC6
		public static List<TooltipProperty> GetVillageProsperityTooltip(Village village)
		{
			return CampaignUIHelper.GetSettlementPropertyTooltip(village.Settlement, CampaignUIHelper._hearthStr.ToString(), village.Hearth, village.HearthChangeExplanation);
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00004DE9 File Offset: 0x00002FE9
		public static List<TooltipProperty> GetTownGarrisonTooltip(Town town)
		{
			Settlement settlement = town.Settlement;
			string valueName = CampaignUIHelper._garrisonStr.ToString();
			MobileParty garrisonParty = town.GarrisonParty;
			return CampaignUIHelper.GetSettlementPropertyTooltip(settlement, valueName, (float)((garrisonParty != null) ? garrisonParty.MemberRoster.TotalManCount : 0), SettlementHelper.GetGarrisonChangeExplainedNumber(town));
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00004E20 File Offset: 0x00003020
		public static List<TooltipProperty> GetPartyTroopSizeLimitTooltip(PartyBase party)
		{
			ExplainedNumber partySizeLimitExplainer = party.PartySizeLimitExplainer;
			return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(CampaignUIHelper._partyTroopSizeLimitStr.ToString(), partySizeLimitExplainer.ResultNumber, ref partySizeLimitExplainer);
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00004E4C File Offset: 0x0000304C
		public static List<TooltipProperty> GetPartyPrisonerSizeLimitTooltip(PartyBase party)
		{
			ExplainedNumber prisonerSizeLimitExplainer = party.PrisonerSizeLimitExplainer;
			return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(CampaignUIHelper._partyPrisonerSizeLimitStr.ToString(), prisonerSizeLimitExplainer.ResultNumber, ref prisonerSizeLimitExplainer);
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00004E78 File Offset: 0x00003078
		public static List<TooltipProperty> GetUsedHorsesTooltip(List<Tuple<EquipmentElement, int>> usedUpgradeHorsesHistory)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (usedUpgradeHorsesHistory.Count > 0)
			{
				foreach (IGrouping<ItemObject, Tuple<EquipmentElement, int>> grouping in from h in usedUpgradeHorsesHistory
					group h by h.Item1.Item)
				{
					int num = grouping.Sum((Tuple<EquipmentElement, int> c) => c.Item2);
					list.Add(new TooltipProperty(grouping.Key.Name.ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
				list.Add(new TooltipProperty(CampaignUIHelper._totalStr.ToString(), usedUpgradeHorsesHistory.Sum((Tuple<EquipmentElement, int> x) => x.Item2).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
			}
			return list;
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00004FA4 File Offset: 0x000031A4
		public static List<TooltipProperty> GetArmyCohesionTooltip(Army army)
		{
			return CampaignUIHelper.GetTooltipForAccumulatingProperty(CampaignUIHelper._armyCohesionStr.ToString(), army.Cohesion, army.DailyCohesionChangeExplanation);
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00004FC4 File Offset: 0x000031C4
		public static List<TooltipProperty> GetArmyManCountTooltip(Army army)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (army.LeaderParty != null)
			{
				list.Add(new TooltipProperty("", CampaignUIHelper._numTotalTroopsInTheArmyStr.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
				Dictionary<FormationClass, int> dictionary = new Dictionary<FormationClass, int>();
				Dictionary<FormationClass, int> dictionary2 = new Dictionary<FormationClass, int>();
				for (int i = 0; i < army.LeaderParty.MemberRoster.Count; i++)
				{
					TroopRosterElement elementCopyAtIndex = army.LeaderParty.MemberRoster.GetElementCopyAtIndex(i);
					int num;
					dictionary.TryGetValue(elementCopyAtIndex.Character.DefaultFormationClass, out num);
					dictionary[elementCopyAtIndex.Character.DefaultFormationClass] = num + elementCopyAtIndex.WoundedNumber;
					int num2;
					dictionary2.TryGetValue(elementCopyAtIndex.Character.DefaultFormationClass, out num2);
					dictionary2[elementCopyAtIndex.Character.DefaultFormationClass] = num2 + elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber;
				}
				int num3 = army.LeaderParty.MemberRoster.TotalManCount;
				foreach (MobileParty mobileParty in army.LeaderParty.AttachedParties)
				{
					for (int j = 0; j < mobileParty.MemberRoster.Count; j++)
					{
						TroopRosterElement elementCopyAtIndex2 = mobileParty.MemberRoster.GetElementCopyAtIndex(j);
						int num4;
						dictionary.TryGetValue(elementCopyAtIndex2.Character.DefaultFormationClass, out num4);
						dictionary[elementCopyAtIndex2.Character.DefaultFormationClass] = num4 + elementCopyAtIndex2.WoundedNumber;
						int num5;
						dictionary2.TryGetValue(elementCopyAtIndex2.Character.DefaultFormationClass, out num5);
						dictionary2[elementCopyAtIndex2.Character.DefaultFormationClass] = num5 + elementCopyAtIndex2.Number - elementCopyAtIndex2.WoundedNumber;
					}
					num3 += mobileParty.MemberRoster.TotalManCount;
				}
				foreach (FormationClass formationClass in FormationClassExtensions.FormationClassValues)
				{
					int num6;
					dictionary.TryGetValue(formationClass, out num6);
					int num7;
					dictionary2.TryGetValue(formationClass, out num7);
					if (num6 + num7 > 0)
					{
						TextObject textObject = new TextObject("{=Dqydb21E} {PARTY_SIZE}", null);
						textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(num7, num6, true));
						TextObject textObject2 = GameTexts.FindText("str_troop_type_name", formationClass.GetName());
						list.Add(new TooltipProperty(textObject2.ToString(), textObject.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
				}
				list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
				list.Add(new TooltipProperty(CampaignUIHelper._totalStr.ToString(), num3.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
			}
			return list;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00005290 File Offset: 0x00003490
		public static string GetDaysUntilNoFood(float totalFood, float foodChange)
		{
			if (totalFood <= 1E-45f)
			{
				totalFood = 0f;
			}
			if (foodChange >= -1E-45f)
			{
				return GameTexts.FindText("str_days_until_no_food_never", null).ToString();
			}
			return MathF.Ceiling(MathF.Abs(totalFood / foodChange)).ToString();
		}

		// Token: 0x06000113 RID: 275 RVA: 0x000052DC File Offset: 0x000034DC
		public static List<TooltipProperty> GetSettlementPropertyTooltip(Settlement settlement, string valueName, float value, ExplainedNumber explainedNumber)
		{
			TextObject textObject;
			if (CampaignUIHelper.IsSettlementInformationHidden(settlement, out textObject))
			{
				List<TooltipProperty> list = new List<TooltipProperty>();
				string currentValue = GameTexts.FindText("str_missing_info_indicator", null).ToString();
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, valueName, currentValue);
				CampaignUIHelper.TooltipAddEmptyLine(list, false);
				list.Add(new TooltipProperty(string.Empty, textObject.ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.None));
				return list;
			}
			return CampaignUIHelper.GetTooltipForAccumulatingProperty(valueName, value, explainedNumber);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000533C File Offset: 0x0000353C
		public static List<TooltipProperty> GetSettlementPropertyTooltipWithResult(Settlement settlement, string valueName, float value, ref ExplainedNumber explainedNumber)
		{
			TextObject textObject;
			if (CampaignUIHelper.IsSettlementInformationHidden(settlement, out textObject))
			{
				List<TooltipProperty> list = new List<TooltipProperty>();
				string currentValue = GameTexts.FindText("str_missing_info_indicator", null).ToString();
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, valueName, currentValue);
				CampaignUIHelper.TooltipAddEmptyLine(list, false);
				list.Add(new TooltipProperty(string.Empty, textObject.ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.None));
				return list;
			}
			return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(valueName, value, ref explainedNumber);
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000539C File Offset: 0x0000359C
		public static List<TooltipProperty> GetArmyFoodTooltip(Army army)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty(new TextObject("{=Q8dhryRX}Parties' Food", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			float num = army.LeaderParty.Food;
			foreach (MobileParty mobileParty in army.LeaderParty.AttachedParties)
			{
				num += mobileParty.Food;
			}
			list.Add(new TooltipProperty(GameTexts.FindText("str_total_army_food", null).ToString(), CampaignUIHelper.FloatToString(num), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty("", string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
			double num2 = 0.0;
			foreach (MobileParty mobileParty2 in army.Parties)
			{
				if (army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty2))
				{
					float val = mobileParty2.Party.MobileParty.Food / -mobileParty2.Party.MobileParty.FoodChange;
					num2 += (double)Math.Max(val, 0f);
					string daysUntilNoFood = CampaignUIHelper.GetDaysUntilNoFood(mobileParty2.Party.MobileParty.Food, mobileParty2.Party.MobileParty.FoodChange);
					list.Add(new TooltipProperty(mobileParty2.Party.MobileParty.Name.ToString(), daysUntilNoFood, 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			list.Add(new TooltipProperty("", string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty(new TextObject("{=rwKBR4NE}Average Days Until Food Runs Out", null).ToString(), MathF.Ceiling(num2 / (double)army.LeaderPartyAndAttachedPartiesCount).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x000055A4 File Offset: 0x000037A4
		public static string GetClanWealthStatusText(Clan clan)
		{
			string result = string.Empty;
			if (clan.Leader.Gold < 15000)
			{
				result = new TextObject("{=SixPXaNh}Very Poor", null).ToString();
			}
			else if (clan.Leader.Gold < 45000)
			{
				result = new TextObject("{=poorWealthStatus}Poor", null).ToString();
			}
			else if (clan.Leader.Gold < 135000)
			{
				result = new TextObject("{=averageWealthStatus}Average", null).ToString();
			}
			else if (clan.Leader.Gold < 405000)
			{
				result = new TextObject("{=UbRqC0Yz}Rich", null).ToString();
			}
			else
			{
				result = new TextObject("{=oJmRg2ms}Very Rich", null).ToString();
			}
			return result;
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00005660 File Offset: 0x00003860
		public static List<TooltipProperty> GetClanProsperityTooltip(Clan clan)
		{
			List<TooltipProperty> list = new List<TooltipProperty>
			{
				new TooltipProperty("", GameTexts.FindText("str_prosperity", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title)
			};
			int num = 0;
			EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
			for (int i = 0; i < clan.Heroes.Count; i++)
			{
				Hero hero = clan.Heroes[i];
				if (hero.Gold != 0 && hero.IsAlive && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(hero))
				{
					int gold = hero.Gold;
					list.Add(new TooltipProperty(hero.Name.ToString(), gold.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					num += gold;
				}
			}
			for (int j = 0; j < clan.Companions.Count; j++)
			{
				Hero hero2 = clan.Companions[j];
				if (hero2.Gold != 0 && hero2.IsAlive && hero2.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(hero2))
				{
					int gold = hero2.Gold;
					list.Add(new TooltipProperty(hero2.Name.ToString(), hero2.Gold.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					num += gold;
				}
			}
			CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
			list.Add(new TooltipProperty(GameTexts.FindText("str_total_gold", null).ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
			return list;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000581C File Offset: 0x00003A1C
		private static List<TooltipProperty> GetDiplomacySettlementStatComparisonTooltip(List<Settlement> settlements, string title, string emptyExplanation = "")
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty("", title, 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			if (settlements.Count == 0)
			{
				list.Add(new TooltipProperty(emptyExplanation, "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty("", "", -1, false, TooltipProperty.TooltipPropertyFlags.None));
				return list;
			}
			for (int i = 0; i < settlements.Count; i++)
			{
				Settlement settlement = settlements[i];
				list.Add(new TooltipProperty(settlement.Name.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			list.Add(new TooltipProperty("", "", -1, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000058D0 File Offset: 0x00003AD0
		public static List<TooltipProperty> GetTruceOwnedSettlementsTooltip(List<Settlement> settlements, TextObject factionName, bool isTown)
		{
			TextObject textObject = (isTown ? new TextObject("{=o79dIa3L}Towns owned by {FACTION}", null) : new TextObject("{=z3Xg0IaG}Castles owned by {FACTION}", null));
			TextObject textObject2 = (isTown ? new TextObject("{=cedvCZ73}There is no town owned by {FACTION}", null) : new TextObject("{=ZZmlYrgL}There is no castle owned by {FACTION}", null));
			textObject.SetTextVariable("FACTION", factionName);
			textObject2.SetTextVariable("FACTION", factionName);
			return CampaignUIHelper.GetDiplomacySettlementStatComparisonTooltip(settlements, textObject.ToString(), textObject2.ToString());
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00005944 File Offset: 0x00003B44
		public static List<TooltipProperty> GetWarPrisonersTooltip(List<Hero> capturedPrisoners, TextObject factionName)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			TextObject textObject = new TextObject("{=8BJDQe6o}Prisoners captured by {FACTION}", null);
			textObject.SetTextVariable("FACTION", factionName);
			list.Add(new TooltipProperty("", textObject.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			if (capturedPrisoners.Count == 0)
			{
				TextObject textObject2 = new TextObject("{=CK68QXen}There is no prisoner captured by {FACTION}", null);
				textObject2.SetTextVariable("FACTION", factionName);
				list.Add(new TooltipProperty(textObject2.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty("", "", -1, false, TooltipProperty.TooltipPropertyFlags.None));
				return list;
			}
			string text = new TextObject("{=MT4b8H9h}Unknown", null).ToString();
			TextObject textObject3 = new TextObject("{=btoiLePb}{HERO} ({PLACE})", null);
			for (int i = 0; i < capturedPrisoners.Count; i++)
			{
				Hero hero = capturedPrisoners[i];
				PartyBase partyBelongedToAsPrisoner = hero.PartyBelongedToAsPrisoner;
				string variable = ((partyBelongedToAsPrisoner != null) ? partyBelongedToAsPrisoner.Name.ToString() : null) ?? text;
				textObject3.SetTextVariable("HERO", hero.Name.ToString());
				textObject3.SetTextVariable("PLACE", variable);
				list.Add(new TooltipProperty(textObject3.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			list.Add(new TooltipProperty("", "", -1, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00005A98 File Offset: 0x00003C98
		public static List<TooltipProperty> GetNormalizedWarProgressTooltip(ExplainedNumber warProgress, ExplainedNumber otherFactionWarProgress, float maxValue, TextObject faction1Name, TextObject faction2Name)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			float num = maxValue / 100f;
			int num2 = (int)(warProgress.ResultNumber / num);
			int num3 = (int)(otherFactionWarProgress.ResultNumber / num);
			int num4 = MathF.Max(0, num2 - num3);
			string definition = new TextObject("{=Pa4K0Paz}War Progress of {FACTION1} Against {FACTION2}", null).SetTextVariable("FACTION1", faction1Name).SetTextVariable("FACTION2", faction2Name).ToString();
			list.Add(new TooltipProperty(definition, num4.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			foreach (ValueTuple<string, float> valueTuple in warProgress.GetLines())
			{
				string item = valueTuple.Item1;
				float item2 = valueTuple.Item2;
				list.Add(new TooltipProperty(item, ((int)(item2 / num)).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			list.Add(new TooltipProperty(new TextObject("{=la6R4xaY}War Progress of Opposite Faction", null).ToString(), (-num3).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00005BB0 File Offset: 0x00003DB0
		public static List<TooltipProperty> GetClanStrengthTooltip(Clan clan)
		{
			List<TooltipProperty> list = new List<TooltipProperty>
			{
				new TooltipProperty("", GameTexts.FindText("str_strength", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title)
			};
			float num = 0f;
			for (int i = 0; i < MobileParty.AllLordParties.Count; i++)
			{
				MobileParty mobileParty = MobileParty.AllLordParties[i];
				if (mobileParty.ActualClan == clan && !mobileParty.IsDisbanding)
				{
					float num2 = mobileParty.Party.CalculateCurrentStrength();
					list.Add(new TooltipProperty(mobileParty.Name.ToString(), num2.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					num += num2;
				}
			}
			CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
			list.Add(new TooltipProperty(GameTexts.FindText("str_total_strength", null).ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
			return list;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00005C90 File Offset: 0x00003E90
		public static List<TooltipProperty> GetCrimeTooltip(Settlement settlement)
		{
			if (settlement.MapFaction == null)
			{
				ExplainedNumber explainedNumber = new ExplainedNumber(0f, true, null);
				return CampaignUIHelper.GetTooltipForAccumulatingProperty(CampaignUIHelper._criminalRatingStr.ToString(), 0f, explainedNumber);
			}
			return CampaignUIHelper.GetTooltipForAccumulatingProperty(CampaignUIHelper._criminalRatingStr.ToString(), settlement.MapFaction.MainHeroCrimeRating, settlement.MapFaction.DailyCrimeRatingChangeExplained);
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00005CF0 File Offset: 0x00003EF0
		public static List<TooltipProperty> GetInfluenceTooltip(Clan clan)
		{
			List<TooltipProperty> tooltipForAccumulatingProperty = CampaignUIHelper.GetTooltipForAccumulatingProperty(CampaignUIHelper._influenceStr.ToString(), clan.Influence, clan.InfluenceChangeExplained);
			if (tooltipForAccumulatingProperty != null && clan.IsUnderMercenaryService)
			{
				tooltipForAccumulatingProperty.Add(new TooltipProperty("", CampaignUIHelper._mercenaryClanInfluenceStr.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return tooltipForAccumulatingProperty;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00005D44 File Offset: 0x00003F44
		public static List<TooltipProperty> GetClanRenownTooltip(Clan clan)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			TextObject textObject;
			ValueTuple<ExplainedNumber, bool> valueTuple = Campaign.Current.Models.ClanTierModel.HasUpcomingTier(clan, out textObject, true);
			ExplainedNumber item = valueTuple.Item1;
			bool item2 = valueTuple.Item2;
			list.Add(new TooltipProperty(GameTexts.FindText("str_enc_sf_renown", null).ToString(), ((int)clan.Renown).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			if (item2)
			{
				CampaignUIHelper.TooltipAddEmptyLine(list, false);
				list.Add(new TooltipProperty(GameTexts.FindText("str_clan_next_tier", null).ToString(), clan.RenownRequirementForNextTier.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				CampaignUIHelper.TooltipAddEmptyLine(list, false);
				GameTexts.SetVariable("LEFT", GameTexts.FindText("str_next_tier_bonus", null).ToString());
				string text = item.GetExplanations().TrimEnd(new char[] { '\n' });
				if (!TextObject.IsNullOrEmpty(textObject))
				{
					TextObject textObject2 = GameTexts.FindText("str_string_newline_newline_string", null);
					textObject2.SetTextVariable("STR1", text);
					textObject2.SetTextVariable("STR2", textObject);
					list.Add(new TooltipProperty(GameTexts.FindText("str_LEFT_colon_wSpace", null).ToString(), textObject2.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				else
				{
					list.Add(new TooltipProperty(GameTexts.FindText("str_LEFT_colon_wSpace", null).ToString(), text, 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00005E94 File Offset: 0x00004094
		public static TooltipTriggerVM GetDenarTooltip()
		{
			ClanFinanceModel clanFinanceModel = Campaign.Current.Models.ClanFinanceModel;
			Func<ExplainedNumber> func = () => clanFinanceModel.CalculateClanGoldChange(Clan.PlayerClan, true, false, false);
			Func<ExplainedNumber> func2 = () => clanFinanceModel.CalculateClanGoldChange(Clan.PlayerClan, true, false, true);
			RundownTooltipVM.ValueCategorization valueCategorization = RundownTooltipVM.ValueCategorization.LargeIsBetter;
			TextObject changeStr = CampaignUIHelper._changeStr;
			TextObject totalStr = CampaignUIHelper._totalStr;
			return new TooltipTriggerVM(typeof(ExplainedNumber), new object[] { func, func2, changeStr, totalStr, valueCategorization });
		}

		// Token: 0x06000121 RID: 289 RVA: 0x00005F12 File Offset: 0x00004112
		public static List<TooltipProperty> GetPartyMoraleTooltip(MobileParty mainParty)
		{
			return CampaignUIHelper.GetTooltipForgProperty(CampaignUIHelper._partyMoraleStr.ToString(), mainParty.Morale, mainParty.MoraleExplained);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00005F30 File Offset: 0x00004130
		public static List<TooltipProperty> GetPartyHealthTooltip(PartyBase party)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._battleReadyTroopsStr.ToString(), (float)party.NumberOfHealthyMembers);
			int num = party.NumberOfAllMembers - party.NumberOfHealthyMembers;
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._woundedTroopsStr.ToString(), (float)num);
			if (num > 0)
			{
				ExplainedNumber healingRateForMemberRegularsExplained = MobileParty.MainParty.Party.HealingRateForMemberRegularsExplained;
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._regularsHealingRateStr.ToString(), healingRateForMemberRegularsExplained.ResultNumber);
				CampaignUIHelper.TooltipAddSeperator(list, false);
				CampaignUIHelper.TooltipAddExplanation(list, ref healingRateForMemberRegularsExplained);
			}
			return list;
		}

		// Token: 0x06000123 RID: 291 RVA: 0x00005FBC File Offset: 0x000041BC
		public static List<TooltipProperty> GetPlayerHitpointsTooltip()
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			ExplainedNumber maxHitPointsExplanation = Hero.MainHero.CharacterObject.MaxHitPointsExplanation;
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._hitPointsStr.ToString(), (float)Hero.MainHero.HitPoints);
			CampaignUIHelper.TooltipAddSeperator(list, false);
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._maxhitPointsStr.ToString(), maxHitPointsExplanation.ResultNumber);
			CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
			CampaignUIHelper.TooltipAddExplanation(list, ref maxHitPointsExplanation);
			if (Hero.MainHero.HitPoints < Hero.MainHero.MaxHitPoints)
			{
				ExplainedNumber healingRateForMemberHeroesExplained = MobileParty.MainParty.Party.HealingRateForMemberHeroesExplained;
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._heroesHealingRateStr.ToString(), healingRateForMemberHeroesExplained.ResultNumber);
				CampaignUIHelper.TooltipAddSeperator(list, false);
				CampaignUIHelper.TooltipAddExplanation(list, ref healingRateForMemberHeroesExplained);
			}
			return list;
		}

		// Token: 0x06000124 RID: 292 RVA: 0x0000607C File Offset: 0x0000427C
		public static List<TooltipProperty> GetPartyFoodTooltip(MobileParty mainParty)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			float num = ((mainParty.Food > 0f) ? mainParty.Food : 0f);
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._foodStr.ToString(), num);
			ExplainedNumber foodChangeExplained = mainParty.FoodChangeExplained;
			CampaignUIHelper.TooltipAddExplanedChange(list, ref foodChangeExplained);
			CampaignUIHelper.TooltipAddEmptyLine(list, false);
			List<TooltipProperty> list2 = new List<TooltipProperty>();
			int num2 = 0;
			List<TooltipProperty> list3 = new List<TooltipProperty>();
			int num3 = 0;
			for (int i = 0; i < mainParty.ItemRoster.Count; i++)
			{
				ItemRosterElement itemRosterElement = mainParty.ItemRoster[i];
				if (!itemRosterElement.IsEmpty)
				{
					ItemObject item = itemRosterElement.EquipmentElement.Item;
					if (item != null && item.IsFood)
					{
						list2.Add(new TooltipProperty(itemRosterElement.EquipmentElement.GetModifiedItemName().ToString(), itemRosterElement.Amount.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
						num2 += itemRosterElement.Amount;
					}
					else
					{
						ItemObject item2 = itemRosterElement.EquipmentElement.Item;
						bool flag;
						if (item2 == null)
						{
							flag = false;
						}
						else
						{
							HorseComponent horseComponent = item2.HorseComponent;
							bool? flag2 = ((horseComponent != null) ? new bool?(horseComponent.IsLiveStock) : null);
							bool flag3 = true;
							flag = (flag2.GetValueOrDefault() == flag3) & (flag2 != null);
						}
						if (flag)
						{
							GameTexts.SetVariable("RANK", itemRosterElement.EquipmentElement.Item.HorseComponent.MeatCount);
							GameTexts.SetVariable("NUMBER", GameTexts.FindText("str_meat", null));
							GameTexts.SetVariable("NUM2", GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString());
							GameTexts.SetVariable("NUM1", itemRosterElement.Amount);
							list3.Add(new TooltipProperty(itemRosterElement.EquipmentElement.GetModifiedItemName().ToString(), GameTexts.FindText("str_NUM_times_NUM_with_space", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
							num3 += itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.HorseComponent.MeatCount;
						}
					}
				}
			}
			if (num2 > 0)
			{
				list.Add(new TooltipProperty(CampaignUIHelper._foodItemsStr.ToString(), num2.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				list.AddRange(list2);
				CampaignUIHelper.TooltipAddEmptyLine(list, false);
			}
			if (num3 > 0)
			{
				list.Add(new TooltipProperty(CampaignUIHelper._livestockStr.ToString(), num3.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				list.AddRange(list3);
				CampaignUIHelper.TooltipAddEmptyLine(list, false);
			}
			list.Add(new TooltipProperty(GameTexts.FindText("str_total_days_until_no_food", null).ToString(), CampaignUIHelper.GetDaysUntilNoFood(num, foodChangeExplained.ResultNumber), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00006334 File Offset: 0x00004534
		public static List<TooltipProperty> GetPartySpeedTooltip(bool considerArmySpeed)
		{
			Game.Current.EventManager.TriggerEvent<PlayerInspectedPartySpeedEvent>(new PlayerInspectedPartySpeedEvent());
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (Hero.MainHero.IsPrisoner)
			{
				list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_main_hero_is_imprisoned", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			else
			{
				Army army = MobileParty.MainParty.Army;
				MobileParty mobileParty = ((army != null) ? army.LeaderParty : null);
				if (considerArmySpeed && mobileParty != null)
				{
					ExplainedNumber speedExplained = mobileParty.SpeedExplained;
					float resultNumber = speedExplained.ResultNumber;
					list = CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(CampaignUIHelper._partySpeedStr.ToString(), resultNumber, ref speedExplained);
				}
				else
				{
					ExplainedNumber speedExplained2 = MobileParty.MainParty.SpeedExplained;
					float resultNumber2 = speedExplained2.ResultNumber;
					list = CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(CampaignUIHelper._partySpeedStr.ToString(), resultNumber2, ref speedExplained2);
				}
			}
			return list;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x000063F8 File Offset: 0x000045F8
		public static List<TooltipProperty> GetPartyWageTooltip(MobileParty mobileParty)
		{
			ExplainedNumber totalWageExplained = mobileParty.TotalWageExplained;
			return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(GameTexts.FindText("str_party_wage", null).ToString(), totalWageExplained.ResultNumber, ref totalWageExplained);
		}

		// Token: 0x06000127 RID: 295 RVA: 0x0000642C File Offset: 0x0000462C
		public static List<TooltipProperty> GetViewDistanceTooltip()
		{
			ExplainedNumber seeingRangeExplanation = MobileParty.MainParty.SeeingRangeExplanation;
			return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(CampaignUIHelper._viewDistanceFoodStr.ToString(), seeingRangeExplanation.ResultNumber, ref seeingRangeExplanation);
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000645C File Offset: 0x0000465C
		public static List<TooltipProperty> GetMainPartyHealthTooltip()
		{
			PartyBase party = MobileParty.MainParty.Party;
			List<TooltipProperty> list = new List<TooltipProperty>();
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._battleReadyTroopsStr.ToString(), (float)party.NumberOfHealthyMembers);
			CampaignUIHelper.TooltipAddEmptyLine(list, false);
			int num = party.NumberOfAllMembers - party.NumberOfHealthyMembers;
			list.Add(new TooltipProperty(CampaignUIHelper._woundedTroopsStr.ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			if (num > 0)
			{
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				list.Add(new TooltipProperty(CampaignUIHelper._regularsHealingRateStr.ToString(), MobileParty.MainParty.Party.HealingRateForMemberRegulars.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				ExplainedNumber healingRateForMemberRegularsExplained = MobileParty.MainParty.Party.HealingRateForMemberRegularsExplained;
				CampaignUIHelper.TooltipAddExplanation(list, ref healingRateForMemberRegularsExplained);
			}
			int totalManCount = party.PrisonRoster.TotalManCount;
			if (totalManCount > 0)
			{
				CampaignUIHelper.TooltipAddSeperator(list, false);
				list.Add(new TooltipProperty(CampaignUIHelper._prisonersStr.ToString(), totalManCount.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			CampaignUIHelper.TooltipAddEmptyLine(list, false);
			TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT_no_space", null);
			Color color = new Color(0.82f, 0.12f, 0.07f, 1f);
			int totalManCount2 = party.MemberRoster.TotalManCount;
			int partySizeLimit = party.PartySizeLimit;
			textObject.SetTextVariable("LEFT", totalManCount2).SetTextVariable("RIGHT", partySizeLimit);
			if (totalManCount2 > partySizeLimit)
			{
				list.Add(new TooltipProperty(new TextObject("{=ZgYAGfbD}Land Troop Capacity", null).ToString(), textObject.ToString(), 0, color, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			else
			{
				list.Add(new TooltipProperty(new TextObject("{=ZgYAGfbD}Land Troop Capacity", null).ToString(), textObject.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			if (party.Ships.Count > 0)
			{
				int num2 = party.Ships.Sum((Ship s) => s.SkeletalCrewCapacity);
				textObject.SetTextVariable("LEFT", totalManCount2).SetTextVariable("RIGHT", num2);
				if (totalManCount2 < num2)
				{
					list.Add(new TooltipProperty(new TextObject("{=p9wUyxfb}Ship Skeletal Crew", null).ToString(), textObject.ToString(), 0, color, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				else
				{
					list.Add(new TooltipProperty(new TextObject("{=p9wUyxfb}Ship Skeletal Crew", null).ToString(), textObject.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				int num3 = party.Ships.Sum((Ship s) => s.TotalCrewCapacity);
				textObject.SetTextVariable("LEFT", totalManCount2).SetTextVariable("RIGHT", num3);
				if (totalManCount2 > num3)
				{
					list.Add(new TooltipProperty(new TextObject("{=w1tgTNvK}Ship Troop Capacity", null).ToString(), textObject.ToString(), 0, color, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				else
				{
					list.Add(new TooltipProperty(new TextObject("{=w1tgTNvK}Ship Troop Capacity", null).ToString(), textObject.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x06000129 RID: 297 RVA: 0x0000674C File Offset: 0x0000494C
		public static List<TooltipProperty> GetPartyInventoryCapacityTooltip(MobileParty party, bool forceLand = false, bool forceSea = false)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (forceLand)
			{
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._partyInventoryLandCapacityStr.ToString(), (float)((int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, false, false, 0, 0, 0, false).ResultNumber));
				CampaignUIHelper.TooltipAddSeperator(list, false);
				ExplainedNumber explainedNumber = Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, false, true, 0, 0, 0, false);
				CampaignUIHelper.TooltipAddExplanation(list, ref explainedNumber);
			}
			else if (forceSea)
			{
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._partyInventorySeaCapacityStr.ToString(), (float)((int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, true, false, 0, 0, 0, false).ResultNumber));
				CampaignUIHelper.TooltipAddSeperator(list, false);
				ExplainedNumber explainedNumber2 = Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, true, true, 0, 0, 0, false);
				CampaignUIHelper.TooltipAddExplanation(list, ref explainedNumber2);
			}
			else
			{
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._partyInventoryCapacityStr.ToString(), (float)party.InventoryCapacity);
				CampaignUIHelper.TooltipAddSeperator(list, false);
				ExplainedNumber inventoryCapacityExplainedNumber = party.InventoryCapacityExplainedNumber;
				CampaignUIHelper.TooltipAddExplanation(list, ref inventoryCapacityExplainedNumber);
			}
			return list;
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000685C File Offset: 0x00004A5C
		public static List<TooltipProperty> GetPartyInventoryWeightTooltip(MobileParty party, bool forceLand = false, bool forceSea = false)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (forceLand)
			{
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._partyInventoryLandWeightStr.ToString(), (float)((int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, false, false).ResultNumber));
				CampaignUIHelper.TooltipAddSeperator(list, false);
				ExplainedNumber explainedNumber = Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, false, true);
				CampaignUIHelper.TooltipAddExplanation(list, ref explainedNumber);
			}
			else if (forceSea)
			{
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._partyInventorySeaWeightStr.ToString(), (float)((int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, true, false).ResultNumber));
				CampaignUIHelper.TooltipAddSeperator(list, false);
				ExplainedNumber explainedNumber2 = Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, true, true);
				CampaignUIHelper.TooltipAddExplanation(list, ref explainedNumber2);
			}
			else
			{
				CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._partyInventoryWeightStr.ToString(), party.TotalWeightCarried);
				CampaignUIHelper.TooltipAddSeperator(list, false);
				ExplainedNumber totalWeightCarriedExplainedNumber = party.TotalWeightCarriedExplainedNumber;
				CampaignUIHelper.TooltipAddExplanation(list, ref totalWeightCarriedExplainedNumber);
			}
			return list;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0000695C File Offset: 0x00004B5C
		public static List<TooltipProperty> GetPerkEffectText(PerkObject perk, bool isActive)
		{
			List<TooltipProperty> list = new List<TooltipProperty>
			{
				new TooltipProperty("", perk.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title)
			};
			TextObject perkRoleText = CampaignUIHelper.GetPerkRoleText(perk, false);
			if (perkRoleText != null)
			{
				list.Add(new TooltipProperty("", perkRoleText.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty("", perk.PrimaryDescription.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			TextObject perkRoleText2 = CampaignUIHelper.GetPerkRoleText(perk, true);
			if (perkRoleText2 != null)
			{
				list.Add(new TooltipProperty("", perkRoleText2.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty("", perk.SecondaryDescription.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			if (isActive)
			{
				list.Add(new TooltipProperty("", GameTexts.FindText("str_perk_active", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			list.Add(new TooltipProperty(GameTexts.FindText("str_required_level_perk", null).ToString(), ((int)perk.RequiredSkillValue).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00006AA8 File Offset: 0x00004CA8
		public static TextObject GetPerkRoleText(PerkObject perk, bool getSecondary)
		{
			TextObject textObject = null;
			if (!getSecondary && perk.PrimaryRole != PartyRole.None)
			{
				textObject = GameTexts.FindText("str_perk_one_role", null);
				textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.PrimaryRole.ToString()));
			}
			else if (getSecondary && perk.SecondaryRole != PartyRole.None)
			{
				textObject = GameTexts.FindText("str_perk_one_role", null);
				textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.SecondaryRole.ToString()));
			}
			return textObject;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00006B3C File Offset: 0x00004D3C
		public static TextObject GetCombinedPerkRoleText(PerkObject perk)
		{
			TextObject textObject = null;
			if (perk.PrimaryRole != PartyRole.None && perk.SecondaryRole != PartyRole.None)
			{
				textObject = GameTexts.FindText("str_perk_two_roles", null);
				textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.PrimaryRole.ToString()));
				textObject.SetTextVariable("SECONDARY_ROLE", GameTexts.FindText("role", perk.SecondaryRole.ToString()));
			}
			else if (perk.PrimaryRole != PartyRole.None)
			{
				textObject = GameTexts.FindText("str_perk_one_role", null);
				textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.PrimaryRole.ToString()));
			}
			return textObject;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00006BFC File Offset: 0x00004DFC
		public static List<TooltipProperty> GetSiegeMachineTooltip(SiegeEngineType engineType, bool showDescription = true, int hoursUntilCompletion = 0)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (showDescription)
			{
				GameTexts.SetVariable("NEWLINE", "\n");
			}
			string value = (showDescription ? GameTexts.FindText("str_siege_weapon_tooltip_text", engineType.StringId).ToString() : engineType.Name.ToString());
			list.Add(new TooltipProperty(" ", value, 0, false, TooltipProperty.TooltipPropertyFlags.None));
			if (hoursUntilCompletion > 0)
			{
				TooltipProperty siegeMachineProgressLine = CampaignUIHelper.GetSiegeMachineProgressLine(hoursUntilCompletion);
				if (siegeMachineProgressLine != null)
				{
					list.Add(siegeMachineProgressLine);
				}
			}
			return list;
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00006C71 File Offset: 0x00004E71
		public static string GetSiegeMachineName(SiegeEngineType engineType)
		{
			if (engineType != null)
			{
				return engineType.Name.ToString();
			}
			return "";
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00006C87 File Offset: 0x00004E87
		public static string GetSiegeMachineNameWithDesctiption(SiegeEngineType engineType)
		{
			if (engineType != null)
			{
				return GameTexts.FindText("str_siege_weapon_tooltip_text", engineType.StringId).ToString();
			}
			return "";
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00006CA8 File Offset: 0x00004EA8
		public static List<TooltipProperty> GetTroopConformityTooltip(TroopRosterElement troop)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (troop.Character != null)
			{
				int elementXp = PartyBase.MainParty.PrisonRoster.GetElementXp(troop.Character);
				int conformityNeededToRecruitPrisoner = troop.Character.ConformityNeededToRecruitPrisoner;
				int num = ((elementXp >= conformityNeededToRecruitPrisoner * troop.Number) ? conformityNeededToRecruitPrisoner : (elementXp % conformityNeededToRecruitPrisoner));
				list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_current_conformity", null).ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_recruit_conformity_cost", null).ToString(), conformityNeededToRecruitPrisoner.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty(GameTexts.FindText("str_party_recruitable_troops", null).ToString(), MathF.Min(elementXp / conformityNeededToRecruitPrisoner, troop.Number).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				if (elementXp < conformityNeededToRecruitPrisoner * troop.Number)
				{
					GameTexts.SetVariable("CONFORMITY_AMOUNT", (conformityNeededToRecruitPrisoner - num).ToString());
					list.Add(new TooltipProperty("", GameTexts.FindText("str_party_troop_conformity_explanation", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
				}
			}
			return list;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00006DC4 File Offset: 0x00004FC4
		public static List<TooltipProperty> GetLearningRateTooltip(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill)
		{
			ExplainedNumber explainedNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(characterAttributes, focusValue, skillValue, skill, true);
			return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(CampaignUIHelper._learningRateStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00006E04 File Offset: 0x00005004
		public static List<TooltipProperty> GetTroopXPTooltip(TroopRosterElement troop)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (troop.Character != null && troop.Character.UpgradeTargets.Length != 0)
			{
				int xp = troop.Xp;
				int upgradeXpCost = troop.Character.GetUpgradeXpCost(PartyBase.MainParty, 0);
				int num = ((xp >= upgradeXpCost * troop.Number) ? upgradeXpCost : (xp % upgradeXpCost));
				list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_current_xp", null).ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_upgrade_xp_cost", null).ToString(), upgradeXpCost.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty(GameTexts.FindText("str_party_upgradable_troops", null).ToString(), MathF.Min(xp / upgradeXpCost, troop.Number).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				if (xp < upgradeXpCost * troop.Number)
				{
					int content = upgradeXpCost - num;
					GameTexts.SetVariable("XP_AMOUNT", content);
					list.Add(new TooltipProperty("", GameTexts.FindText("str_party_troop_xp_explanation", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
				}
			}
			return list;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00006F24 File Offset: 0x00005124
		public static List<TooltipProperty> GetLearningLimitTooltip(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill)
		{
			ExplainedNumber explainedNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(characterAttributes, focusValue, skill, true);
			return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(CampaignUIHelper._learningLimitStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00006F64 File Offset: 0x00005164
		public static List<TooltipProperty> GetSettlementConsumptionTooltip(Settlement settlement)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty("", GameTexts.FindText("str_consumption", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			if (settlement.IsTown)
			{
				using (IEnumerator<Town.SellLog> enumerator = settlement.Town.SoldItems.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Town.SellLog sellLog = enumerator.Current;
						list.Add(new TooltipProperty(sellLog.Category.GetName().ToString(), sellLog.Number.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
					return list;
				}
			}
			Debug.FailedAssert("Only towns' consumptions are tracked", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CampaignUIHelper.cs", "GetSettlementConsumptionTooltip", 1384);
			return list;
		}

		// Token: 0x06000136 RID: 310 RVA: 0x0000702C File Offset: 0x0000522C
		public static StringItemWithHintVM GetCharacterTierData(CharacterObject character, bool isBig = false)
		{
			int tier = character.Tier;
			if (tier <= 0 || tier > 7)
			{
				return new StringItemWithHintVM("", TextObject.GetEmpty());
			}
			string str = (isBig ? (tier.ToString() + "_big") : tier.ToString());
			string text = "General\\TroopTierIcons\\icon_tier_" + str;
			GameTexts.SetVariable("TIER_LEVEL", tier);
			TextObject hint = new TextObject("{=!}" + GameTexts.FindText("str_party_troop_tier", null).ToString(), null);
			return new StringItemWithHintVM(text, hint);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x000070B4 File Offset: 0x000052B4
		public static List<TooltipProperty> GetSettlementProductionTooltip(Settlement settlement)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty("", GameTexts.FindText("str_production", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			if (settlement.IsFortification)
			{
				list.Add(new TooltipProperty(GameTexts.FindText("str_villages", null).ToString(), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				for (int i = 0; i < settlement.BoundVillages.Count; i++)
				{
					Village village = settlement.BoundVillages[i];
					list.Add(new TooltipProperty(village.Name.ToString(), village.VillageType.PrimaryProduction.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				list.Add(new TooltipProperty(GameTexts.FindText("str_shops_in_town", null).ToString(), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				using (IEnumerator<Workshop> enumerator = (from w in settlement.Town.Workshops
					where w.WorkshopType != null && !w.WorkshopType.IsHidden
					select w).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Workshop workshop = enumerator.Current;
						list.Add(new TooltipProperty(" ", workshop.WorkshopType.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
					return list;
				}
			}
			if (settlement.IsVillage)
			{
				list.Add(new TooltipProperty(GameTexts.FindText("str_production_in_village", null).ToString(), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
				for (int j = 0; j < settlement.Village.VillageType.Productions.Count; j++)
				{
					ValueTuple<ItemObject, float> valueTuple = settlement.Village.VillageType.Productions[j];
					list.Add(new TooltipProperty(" ", valueTuple.Item1.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x000072C0 File Offset: 0x000054C0
		public static string GetHintTextFromReasons(List<TextObject> reasons)
		{
			TextObject textObject = TextObject.GetEmpty();
			for (int i = 0; i < reasons.Count; i++)
			{
				if (i >= 1)
				{
					GameTexts.SetVariable("STR1", textObject.ToString());
					GameTexts.SetVariable("STR2", reasons[i]);
					textObject = GameTexts.FindText("str_string_newline_string", null);
				}
				else
				{
					textObject = reasons[i];
				}
			}
			return textObject.ToString();
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00007328 File Offset: 0x00005528
		public static string MergeTextObjectsWithNewline(List<TextObject> textObjects)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < textObjects.Count; i++)
			{
				string value = textObjects[i].ToString();
				stringBuilder.AppendLine(value);
			}
			return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00007374 File Offset: 0x00005574
		public static TextObject GetHoursAndDaysTextFromHourValue(int hours)
		{
			TextObject textObject = TextObject.GetEmpty();
			if (hours == 0)
			{
				textObject = GameTexts.FindText("str_hours", null);
				textObject.SetTextVariable("HOUR", 0);
			}
			else if (hours > 0)
			{
				int num = hours / 24;
				int num2 = hours % 24;
				textObject = ((num > 0) ? ((num2 > 0) ? GameTexts.FindText("str_days_hours", null) : GameTexts.FindText("str_days", null)) : GameTexts.FindText("str_hours", null));
				textObject.SetTextVariable("DAY", num);
				textObject.SetTextVariable("PLURAL_DAYS", (num > 1) ? 1 : 0);
				textObject.SetTextVariable("HOUR", num2);
				textObject.SetTextVariable("PLURAL_HOURS", (num2 > 1) ? 1 : 0);
			}
			return textObject;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00007424 File Offset: 0x00005624
		public static TextObject GetTeleportationDelayText(Hero hero, PartyBase target)
		{
			TextObject result = TextObject.GetEmpty();
			if (hero != null && target != null)
			{
				float resultNumber = Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, target).ResultNumber;
				if (hero.IsTraveling)
				{
					result = CampaignUIHelper._travelingText.CopyTextObject();
				}
				else if (resultNumber > 0f)
				{
					TextObject textObject = new TextObject("{=P0To9aRW}Travel time: {TRAVEL_TIME}", null);
					textObject.SetTextVariable("TRAVEL_TIME", CampaignUIHelper.GetHoursAndDaysTextFromHourValue((int)Math.Ceiling((double)resultNumber)));
					result = textObject;
				}
				else
				{
					result = CampaignUIHelper._noDelayText.CopyTextObject();
				}
			}
			return result;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x000074AC File Offset: 0x000056AC
		public static List<TooltipProperty> GetTimeOfDayAndResetCameraTooltip()
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			int getHourOfDay = CampaignTime.Now.GetHourOfDay;
			TextObject textObject;
			if (getHourOfDay >= 6 && getHourOfDay < 12)
			{
				textObject = new TextObject("{=X3gcUz7C}Morning", null);
			}
			else if (getHourOfDay >= 12 && getHourOfDay < 15)
			{
				textObject = new TextObject("{=CTtjSwRb}Noon", null);
			}
			else if (getHourOfDay >= 15 && getHourOfDay < 18)
			{
				textObject = new TextObject("{=J2gvnexb}Afternoon", null);
			}
			else if (getHourOfDay >= 18 && getHourOfDay < 22)
			{
				textObject = new TextObject("{=gENb9SSW}Evening", null);
			}
			else
			{
				textObject = new TextObject("{=fAxjyMt5}Night", null);
			}
			list.Add(new TooltipProperty(textObject.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty("", new TextObject("{=sFiU3Ss2}Click to Reset Camera", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00007574 File Offset: 0x00005774
		public static List<TooltipProperty> GetTournamentChampionRewardsTooltip(Hero hero, Town town)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			CampaignUIHelper.TooltipAddPropertyTitle(list, new TextObject("{=CGVK6l8I}Champion Benefits", null).ToString());
			TextObject textObject = new TextObject("{=4vZLpzPi}+1 Renown / Day", null);
			list.Add(new TooltipProperty(textObject.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x000075C4 File Offset: 0x000057C4
		public static StringItemWithHintVM GetCharacterTypeData(CharacterObject character, bool isBig = false)
		{
			if (character.IsHero)
			{
				return new StringItemWithHintVM("", TextObject.GetEmpty());
			}
			bool isMariner = character.IsMariner;
			TextObject textObject = new TextObject("{=!}{TYPENAME}{MARINER}{BIG}", null);
			TextObject textObject2;
			if (character.IsRanged && character.IsMounted)
			{
				textObject.SetTextVariable("TYPENAME", "horse_archer");
				textObject2 = GameTexts.FindText("str_troop_type_name", "HorseArcher");
			}
			else if (character.IsRanged)
			{
				textObject.SetTextVariable("TYPENAME", "bow");
				string variation = (isMariner ? "Ranged_Mariner" : "Ranged");
				textObject2 = GameTexts.FindText("str_troop_type_name", variation);
			}
			else if (character.IsMounted)
			{
				textObject.SetTextVariable("TYPENAME", "cavalry");
				textObject2 = GameTexts.FindText("str_troop_type_name", "Cavalry");
			}
			else
			{
				if (!character.IsInfantry)
				{
					return new StringItemWithHintVM("", TextObject.GetEmpty());
				}
				textObject.SetTextVariable("TYPENAME", "infantry");
				string variation2 = (isMariner ? "Infantry_Mariner" : "Infantry");
				textObject2 = GameTexts.FindText("str_troop_type_name", variation2);
			}
			textObject.SetTextVariable("MARINER", isMariner ? "_mariner" : "");
			textObject.SetTextVariable("BIG", isBig ? "_big" : "");
			return new StringItemWithHintVM("General\\TroopTypeIcons\\icon_troop_type_" + textObject.ToString(), new TextObject("{=!}" + textObject2.ToString(), null));
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00007740 File Offset: 0x00005940
		public static List<TooltipProperty> GetHeroHealthTooltip(Hero hero)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			GameTexts.SetVariable("LEFT", hero.HitPoints.ToString("0.##"));
			GameTexts.SetVariable("RIGHT", hero.MaxHitPoints.ToString("0.##"));
			list.Add(new TooltipProperty(CampaignUIHelper._hitPointsStr.ToString(), GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			CampaignUIHelper.TooltipAddSeperator(list, false);
			CampaignUIHelper.TooltipAddPropertyTitleWithValue(list, CampaignUIHelper._maxhitPointsStr.ToString(), (float)hero.MaxHitPoints);
			CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
			ExplainedNumber maxHitPointsExplanation = hero.CharacterObject.MaxHitPointsExplanation;
			CampaignUIHelper.TooltipAddExplanation(list, ref maxHitPointsExplanation);
			return list;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x000077F4 File Offset: 0x000059F4
		public static List<TooltipProperty> GetSiegeWallTooltip(int wallLevel, int wallHitpoints)
		{
			return new List<TooltipProperty>
			{
				new TooltipProperty(GameTexts.FindText("str_map_tooltip_wall_level", null).ToString(), wallLevel.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None),
				new TooltipProperty(GameTexts.FindText("str_map_tooltip_wall_hitpoints", null).ToString(), wallHitpoints.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None)
			};
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00007850 File Offset: 0x00005A50
		public static List<TooltipProperty> GetGovernorPerksTooltipForHero(Hero hero)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty(GameTexts.FindText("str_clan_governor_perks", null).ToString(), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			CampaignUIHelper.TooltipAddSeperator(list, false);
			List<PerkObject> governorPerksForHero = PerkHelper.GetGovernorPerksForHero(hero);
			for (int i = 0; i < governorPerksForHero.Count; i++)
			{
				if (governorPerksForHero[i].PrimaryRole == PartyRole.Governor)
				{
					list.Add(new TooltipProperty(governorPerksForHero[i].Name.ToString(), governorPerksForHero[i].PrimaryDescription.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				if (governorPerksForHero[i].SecondaryRole == PartyRole.Governor)
				{
					list.Add(new TooltipProperty(governorPerksForHero[i].Name.ToString(), governorPerksForHero[i].SecondaryDescription.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			if (governorPerksForHero.Count == 0)
			{
				list.Add(new TooltipProperty("", new TextObject("{=oSfsqBwJ}No perks", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return list;
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00007958 File Offset: 0x00005B58
		[return: TupleElementNames(new string[] { "titleText", "bodyText" })]
		public static ValueTuple<TextObject, TextObject> GetGovernorSelectionConfirmationPopupTexts(Hero currentGovernor, Hero newGovernor, Settlement settlement)
		{
			if (settlement != null)
			{
				bool flag = newGovernor == null;
				DelayedTeleportationModel delayedTeleportationModel = Campaign.Current.Models.DelayedTeleportationModel;
				int num = ((!flag) ? ((int)Math.Ceiling((double)delayedTeleportationModel.GetTeleportationDelayAsHours(newGovernor, settlement.Party).ResultNumber)) : 0);
				MBTextManager.SetTextVariable("TRAVEL_DURATION", CampaignUIHelper.GetHoursAndDaysTextFromHourValue(num).ToString(), false);
				CharacterObject characterObject = (flag ? ((currentGovernor != null) ? currentGovernor.CharacterObject : null) : ((newGovernor != null) ? newGovernor.CharacterObject : null));
				if (characterObject != null)
				{
					StringHelpers.SetCharacterProperties("GOVERNOR", characterObject, null, false);
				}
				string variableName = "SETTLEMENT_NAME";
				TextObject name = settlement.Name;
				MBTextManager.SetTextVariable(variableName, ((name != null) ? name.ToString() : null) ?? string.Empty, false);
				TextObject item = GameTexts.FindText(flag ? "str_clan_remove_governor" : "str_clan_assign_governor", null);
				TextObject item2 = GameTexts.FindText(flag ? "str_remove_governor_inquiry" : ((num == 0) ? "str_change_governor_instantly_inquiry" : "str_change_governor_inquiry"), null);
				return new ValueTuple<TextObject, TextObject>(item, item2);
			}
			return new ValueTuple<TextObject, TextObject>(TextObject.GetEmpty(), TextObject.GetEmpty());
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00007A5C File Offset: 0x00005C5C
		public static List<TooltipProperty> GetHeroGovernorEffectsTooltip(Hero hero, Settlement settlement)
		{
			List<TooltipProperty> list = new List<TooltipProperty>
			{
				new TooltipProperty("", hero.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title)
			};
			list.Add(new TooltipProperty(string.Empty, CampaignUIHelper.GetTeleportationDelayText(hero, settlement.Party).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_relation", null), false);
			string definition = GameTexts.FindText("str_LEFT_ONLY", null).ToString();
			list.Add(new TooltipProperty(definition, ((int)hero.GetRelationWithPlayer()).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_type", null), false);
			string definition2 = GameTexts.FindText("str_LEFT_ONLY", null).ToString();
			list.Add(new TooltipProperty(definition2, HeroHelper.GetCharacterTypeName(hero).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_culture", null), false);
			string definition3 = GameTexts.FindText("str_LEFT_ONLY", null).ToString();
			list.Add(new TooltipProperty(definition3, hero.Culture.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			PartyRole? partyRole = ((partyBelongedTo != null) ? new PartyRole?(partyBelongedTo.GetHeroPartyRole(hero)) : null);
			if (partyRole != null)
			{
				PartyRole? partyRole2 = partyRole;
				PartyRole partyRole3 = PartyRole.None;
				if (!((partyRole2.GetValueOrDefault() == partyRole3) & (partyRole2 != null)))
				{
					TextObject textObject = GameTexts.FindText("role", partyRole.Value.ToString());
					list.Add(new TooltipProperty(new TextObject("{=9FJi2SaE}Party Role", null).ToString(), textObject.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			CampaignUIHelper.TooltipAddEmptyLine(list, false);
			list.Add(new TooltipProperty(new TextObject("{=J8ddrAOf}Governor Effects", null).ToString(), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			CampaignUIHelper.TooltipAddSeperator(list, false);
			ValueTuple<TextObject, TextObject> governorEngineeringSkillEffectForHero = PerkHelper.GetGovernorEngineeringSkillEffectForHero(hero);
			list.Add(new TooltipProperty(governorEngineeringSkillEffectForHero.Item1.ToString(), governorEngineeringSkillEffectForHero.Item2.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			CampaignUIHelper.TooltipAddEmptyLine(list, false);
			List<TooltipProperty> governorPerksTooltipForHero = CampaignUIHelper.GetGovernorPerksTooltipForHero(hero);
			list.AddRange(governorPerksTooltipForHero);
			return list;
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00007C88 File Offset: 0x00005E88
		public static List<TooltipProperty> GetEncounterPartyMoraleTooltip(List<MobileParty> parties)
		{
			return new List<TooltipProperty>();
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00007C90 File Offset: 0x00005E90
		public static TextObject GetCraftingTemplatePieceUnlockProgressHint(float progress)
		{
			TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis", null);
			textObject.SetTextVariable("LEFT", progress.ToString("F0"));
			textObject.SetTextVariable("RIGHT", "100");
			TextObject variable = new TextObject("{=opU0Nr2G}Progress for unlocking a new piece.", null);
			TextObject textObject2 = GameTexts.FindText("str_STR1_space_STR2", null);
			textObject2.SetTextVariable("STR1", variable);
			textObject2.SetTextVariable("STR2", textObject);
			return textObject2;
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00007D04 File Offset: 0x00005F04
		public static List<ValueTuple<string, TextObject>> GetWeaponFlagDetails(WeaponFlags weaponFlags, CharacterObject character = null)
		{
			List<ValueTuple<string, TextObject>> list = new List<ValueTuple<string, TextObject>>();
			if (weaponFlags.HasAnyFlag(WeaponFlags.BonusAgainstShield))
			{
				string item = "WeaponFlagIcons\\bonus_against_shield";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_bonus_against_shield", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			if (weaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
			{
				string item = "WeaponFlagIcons\\can_knock_down";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_can_knockdown", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			if (weaponFlags.HasAnyFlag(WeaponFlags.CanDismount))
			{
				string item = "WeaponFlagIcons\\can_dismount";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_can_dismount", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			if (weaponFlags.HasAnyFlag(WeaponFlags.CanHook))
			{
				string item = "WeaponFlagIcons\\can_dismount";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_can_hook", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			if (weaponFlags.HasAnyFlag(WeaponFlags.CanCrushThrough))
			{
				string item = "WeaponFlagIcons\\can_crush_through";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_can_crush_through", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithTwoHand))
			{
				string item = "WeaponFlagIcons\\not_usable_with_two_hand";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_not_usable_two_hand", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
			{
				string item = "WeaponFlagIcons\\not_usable_with_one_hand";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_not_usable_one_hand", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			if (weaponFlags.HasAnyFlag(WeaponFlags.CantReloadOnHorseback) && (character == null || !character.GetPerkValue(DefaultPerks.Crossbow.MountedCrossbowman)))
			{
				string item = "WeaponFlagIcons\\cant_reload_on_horseback";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_cant_reload_on_horseback", null);
				list.Add(new ValueTuple<string, TextObject>(item, item2));
			}
			return list;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00007E90 File Offset: 0x00006090
		public static List<Tuple<string, TextObject>> GetItemFlagDetails(ItemFlags itemFlags)
		{
			List<Tuple<string, TextObject>> list = new List<Tuple<string, TextObject>>();
			if (itemFlags.HasAnyFlag(ItemFlags.Civilian))
			{
				string item = "GeneralFlagIcons\\civillian";
				TextObject item2 = GameTexts.FindText("str_inventory_flag_civillian", null);
				list.Add(new Tuple<string, TextObject>(item, item2));
			}
			return list;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00007ED0 File Offset: 0x000060D0
		public static List<ValueTuple<string, TextObject>> GetItemUsageSetFlagDetails(ItemObject.ItemUsageSetFlags flags, CharacterObject character = null)
		{
			List<ValueTuple<string, TextObject>> list = new List<ValueTuple<string, TextObject>>();
			if (flags.HasAnyFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount) && (character == null || !character.GetPerkValue(DefaultPerks.Bow.HorseMaster)))
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\cant_use_on_horseback", GameTexts.FindText("str_inventory_flag_cant_use_with_mounts", null)));
			}
			if (flags.HasAnyFlag(ItemObject.ItemUsageSetFlags.RequiresNoShield))
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\cant_use_with_shields", GameTexts.FindText("str_inventory_flag_cant_use_with_shields", null)));
			}
			return list;
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00007F44 File Offset: 0x00006144
		public static List<ValueTuple<string, TextObject>> GetFlagDetailsForWeapon(WeaponComponentData weapon, ItemObject.ItemUsageSetFlags itemUsageFlags, CharacterObject character = null)
		{
			List<ValueTuple<string, TextObject>> list = new List<ValueTuple<string, TextObject>>();
			if (weapon == null)
			{
				return list;
			}
			if (weapon.RelevantSkill == DefaultSkills.Bow)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\bow", GameTexts.FindText("str_inventory_flag_bow", null)));
			}
			if (weapon.RelevantSkill == DefaultSkills.Crossbow)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\crossbow", GameTexts.FindText("str_inventory_flag_crossbow", null)));
			}
			if (weapon.RelevantSkill == DefaultSkills.Polearm)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\polearm", GameTexts.FindText("str_inventory_flag_polearm", null)));
			}
			if (weapon.RelevantSkill == DefaultSkills.OneHanded)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\one_handed", GameTexts.FindText("str_inventory_flag_one_handed", null)));
			}
			if (weapon.RelevantSkill == DefaultSkills.TwoHanded)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\two_handed", GameTexts.FindText("str_inventory_flag_two_handed", null)));
			}
			if (weapon.RelevantSkill == DefaultSkills.Throwing)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\throwing", GameTexts.FindText("str_inventory_flag_throwing", null)));
			}
			List<ValueTuple<string, TextObject>> weaponFlagDetails = CampaignUIHelper.GetWeaponFlagDetails(weapon.WeaponFlags, character);
			list.AddRange(weaponFlagDetails);
			List<ValueTuple<string, TextObject>> itemUsageSetFlagDetails = CampaignUIHelper.GetItemUsageSetFlagDetails(itemUsageFlags, character);
			list.AddRange(itemUsageSetFlagDetails);
			string weaponDescriptionId = weapon.WeaponDescriptionId;
			if (weaponDescriptionId != null && weaponDescriptionId.IndexOf("couch", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\can_couchable", GameTexts.FindText("str_inventory_flag_couchable", null)));
			}
			string weaponDescriptionId2 = weapon.WeaponDescriptionId;
			if (weaponDescriptionId2 != null && weaponDescriptionId2.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				list.Add(new ValueTuple<string, TextObject>("WeaponFlagIcons\\braceable", GameTexts.FindText("str_inventory_flag_braceable", null)));
			}
			return list;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x000080E8 File Offset: 0x000062E8
		public static string GetFormattedItemPropertyText(float propertyValue, bool typeRequiresInteger)
		{
			bool flag = propertyValue >= 100f || (propertyValue % 1f).ApproximatelyEqualsTo(0f, 0.001f);
			if (typeRequiresInteger || flag)
			{
				return propertyValue.ToString("F0");
			}
			if (propertyValue >= 10f)
			{
				return propertyValue.ToString("F1");
			}
			return propertyValue.ToString("F2");
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000814C File Offset: 0x0000634C
		public static List<TooltipProperty> GetCraftingHeroTooltip(Hero hero, CraftingOrder order)
		{
			object obj = order != null && !order.IsOrderAvailableForHero(hero);
			ICraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			List<TooltipProperty> list = new List<TooltipProperty>();
			object obj2 = obj;
			string propertyName = ((obj2 != null) ? GameTexts.FindText("str_crafting_hero_can_not_craft_item", null).ToString() : hero.Name.ToString());
			CampaignUIHelper.TooltipAddPropertyTitle(list, propertyName);
			if (obj2 != null)
			{
				List<Hero> list2 = (from h in CraftingHelper.GetAvailableHeroesForCrafting()
					where order.IsOrderAvailableForHero(h)
					select h).ToList<Hero>();
				if (list2.Count > 0)
				{
					GameTexts.SetVariable("SKILL", GameTexts.FindText("str_crafting", null).ToString());
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_hero_not_enough_skills", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
					CampaignUIHelper.TooltipAddEmptyLine(list, false);
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_following_can_craft_order", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
					for (int i = 0; i < list2.Count; i++)
					{
						Hero hero2 = list2[i];
						GameTexts.SetVariable("HERO_NAME", hero2.Name);
						list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_hero_able_to_craft", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
				}
				else
				{
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_one_can_craft_order", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			else
			{
				list.Add(new TooltipProperty(new TextObject("{=cUUI8u2G}Smithy Stamina", null).ToString(), campaignBehavior.GetHeroCraftingStamina(hero).ToString() + " / " + campaignBehavior.GetMaxHeroCraftingStamina(hero).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty(new TextObject("{=lVuGCYPC}Smithing Skill", null).ToString(), hero.GetSkillValue(DefaultSkills.Crafting).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return list;
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000834C File Offset: 0x0000654C
		public static List<TooltipProperty> GetOrderCannotBeCompletedReasonTooltip(CraftingOrder order, ItemObject item)
		{
			CampaignUIHelper.<>c__DisplayClass160_0 CS$<>8__locals1;
			CS$<>8__locals1.order = order;
			CS$<>8__locals1.properties = new List<TooltipProperty>();
			CampaignUIHelper.TooltipAddPropertyTitle(CS$<>8__locals1.properties, new TextObject("{=Syha8biz}Order Can Not Be Completed", null).ToString());
			CS$<>8__locals1.properties.Add(new TooltipProperty(new TextObject("{=gTbE6t9I}Following requirements are not met:", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			if (CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamageType != item.PrimaryWeapon.SwingDamageType)
			{
				DamageTypes swingDamageType = CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamageType;
				TextObject textObject;
				if (item.PrimaryWeapon.ThrustDamageType == DamageTypes.Invalid)
				{
					textObject = TextObject.GetEmpty();
				}
				else
				{
					textObject = new TextObject("{=MT5A04X8} - Swing Damage Type does not match. Should be: {TYPE}", null);
					TextObject textObject2 = textObject;
					string tag = "TYPE";
					string id = "str_inventory_dmg_type";
					int i = (int)swingDamageType;
					textObject2.SetTextVariable(tag, GameTexts.FindText(id, i.ToString()));
				}
				CS$<>8__locals1.properties.Add(new TooltipProperty(textObject.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			if (CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamageType != item.PrimaryWeapon.ThrustDamageType)
			{
				DamageTypes thrustDamageType = CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamageType;
				TextObject textObject3;
				if (item.PrimaryWeapon.ThrustDamageType == DamageTypes.Invalid)
				{
					textObject3 = TextObject.GetEmpty();
				}
				else
				{
					textObject3 = new TextObject("{=Tx9Mynbt} - Thrust Damage Type does not match. Should be: {TYPE}", null);
					TextObject textObject4 = textObject3;
					string tag2 = "TYPE";
					string id2 = "str_inventory_dmg_type";
					int i = (int)thrustDamageType;
					textObject4.SetTextVariable(tag2, GameTexts.FindText(id2, i.ToString()).ToString());
				}
				CS$<>8__locals1.properties.Add(new TooltipProperty(textObject3.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			float num = (float)CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustSpeed;
			float num2 = (float)item.PrimaryWeapon.ThrustSpeed;
			if (num > num2)
			{
				CampaignUIHelper.<GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes.ThrustSpeed, num, ref CS$<>8__locals1);
			}
			num = (float)CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingSpeed;
			num2 = (float)item.PrimaryWeapon.SwingSpeed;
			if (num > num2)
			{
				CampaignUIHelper.<GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes.SwingSpeed, num, ref CS$<>8__locals1);
			}
			num = (float)CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.MissileSpeed;
			num2 = (float)item.PrimaryWeapon.MissileSpeed;
			if (num > num2)
			{
				CampaignUIHelper.<GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes.MissileSpeed, num, ref CS$<>8__locals1);
			}
			num = (float)CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamage;
			num2 = (float)item.PrimaryWeapon.ThrustDamage;
			if (num > num2)
			{
				CampaignUIHelper.<GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes.ThrustDamage, num, ref CS$<>8__locals1);
			}
			num = (float)CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamage;
			num2 = (float)item.PrimaryWeapon.SwingDamage;
			if (num > num2)
			{
				CampaignUIHelper.<GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes.SwingDamage, num, ref CS$<>8__locals1);
			}
			num = (float)CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.Accuracy;
			num2 = (float)item.PrimaryWeapon.Accuracy;
			if (num > num2)
			{
				CampaignUIHelper.<GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes.Accuracy, num, ref CS$<>8__locals1);
			}
			num = (float)CS$<>8__locals1.order.PreCraftedWeaponDesignItem.PrimaryWeapon.Handling;
			num2 = (float)item.PrimaryWeapon.Handling;
			if (num > num2)
			{
				CampaignUIHelper.<GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes.Handling, num, ref CS$<>8__locals1);
			}
			bool flag = true;
			WeaponDescription[] weaponDescriptions = CS$<>8__locals1.order.PreCraftedWeaponDesignItem.WeaponDesign.Template.WeaponDescriptions;
			for (int i = 0; i < weaponDescriptions.Length; i++)
			{
				WeaponDescription weaponDescription = weaponDescriptions[i];
				if (item.WeaponDesign.Template.WeaponDescriptions.All((WeaponDescription d) => d.WeaponClass != weaponDescription.WeaponClass))
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				CS$<>8__locals1.properties.Add(new TooltipProperty(new TextObject("{=Q1KwpZYu}Weapon usage does not match requirements", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return CS$<>8__locals1.properties;
		}

		// Token: 0x0600014D RID: 333 RVA: 0x000086F8 File Offset: 0x000068F8
		public static List<TooltipProperty> GetCraftingOrderDisabledReasonTooltip(Hero heroToCheck, CraftingOrder order)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (order.IsOrderAvailableForHero(heroToCheck))
			{
				return list;
			}
			GameTexts.SetVariable("SKILL", GameTexts.FindText("str_crafting", null).ToString());
			CampaignUIHelper.TooltipAddPropertyTitle(list, GameTexts.FindText("str_crafting_cannot_be_crafted", null).ToString());
			if (!order.IsOrderAvailableForHero(heroToCheck))
			{
				List<Hero> list2 = (from h in CraftingHelper.GetAvailableHeroesForCrafting()
					where order.IsOrderAvailableForHero(h)
					select h).ToList<Hero>();
				if (list2.Count > 0)
				{
					GameTexts.SetVariable("HERO", heroToCheck.Name.ToString());
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_player_not_enough_skills", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
					CampaignUIHelper.TooltipAddEmptyLine(list, false);
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_following_can_craft_order", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
					for (int i = 0; i < list2.Count; i++)
					{
						Hero hero = list2[i];
						GameTexts.SetVariable("HERO_NAME", hero.Name);
						list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_hero_able_to_craft", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
				}
				else
				{
					int content = MathF.Ceiling(order.OrderDifficulty) - heroToCheck.GetSkillValue(DefaultSkills.Crafting) - 50;
					GameTexts.SetVariable("AMOUNT", content);
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_one_can_craft_order", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00008894 File Offset: 0x00006A94
		public static List<TooltipProperty> GetOrdersDisabledReasonTooltip(MBBindingList<CraftingOrderItemVM> craftingOrders, Hero heroToCheck)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (craftingOrders != null)
			{
				if (craftingOrders.Count((CraftingOrderItemVM x) => x.IsEnabled) > 0)
				{
					return list;
				}
			}
			bool flag = false;
			CampaignUIHelper.TooltipAddPropertyTitle(list, GameTexts.FindText("str_crafting_cannot_complete_orders", null).ToString());
			GameTexts.SetVariable("HERO_NAME", heroToCheck.Name);
			list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_available_orders_for_hero", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			CampaignUIHelper.TooltipAddEmptyLine(list, false);
			IEnumerable<Hero> availableHeroesForCrafting = CraftingHelper.GetAvailableHeroesForCrafting();
			for (int i = 0; i < availableHeroesForCrafting.Count<Hero>(); i++)
			{
				Hero hero = availableHeroesForCrafting.ToList<Hero>()[i];
				int num = craftingOrders.Count((CraftingOrderItemVM x) => x.CraftingOrder.IsOrderAvailableForHero(hero));
				if (num > 0)
				{
					flag = true;
					GameTexts.SetVariable("HERO_NAME", hero.Name);
					GameTexts.SetVariable("NUMBER", num);
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_available_orders_for_other_hero", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			if (!flag)
			{
				GameTexts.SetVariable("SKILL", GameTexts.FindText("str_crafting", null).ToString());
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_available_orders_for_party", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return list;
		}

		// Token: 0x0600014F RID: 335 RVA: 0x000089FC File Offset: 0x00006BFC
		public static string GetCraftingOrderMissingPropertyWarningText(CraftingOrder order, ItemObject craftedItem)
		{
			if (order == null)
			{
				return string.Empty;
			}
			bool flag = true;
			bool flag2 = true;
			WeaponComponentData statWeapon = order.GetStatWeapon();
			WeaponComponentData weaponComponentData = null;
			for (int i = 0; i < craftedItem.Weapons.Count; i++)
			{
				if (craftedItem.Weapons[i].WeaponDescriptionId == statWeapon.WeaponDescriptionId)
				{
					weaponComponentData = craftedItem.Weapons[i];
					break;
				}
			}
			if (weaponComponentData == null)
			{
				weaponComponentData = craftedItem.PrimaryWeapon;
			}
			string variable = string.Empty;
			if (statWeapon.SwingDamageType != DamageTypes.Invalid && statWeapon.SwingDamageType != weaponComponentData.SwingDamageType)
			{
				flag = false;
				variable = GameTexts.FindText("str_damage_types", statWeapon.SwingDamageType.ToString()).ToString();
			}
			else if (statWeapon.ThrustDamageType != DamageTypes.Invalid && statWeapon.ThrustDamageType != weaponComponentData.ThrustDamageType)
			{
				flag2 = false;
				variable = GameTexts.FindText("str_damage_types", statWeapon.ThrustDamageType.ToString()).ToString();
			}
			if (!flag)
			{
				return GameTexts.FindText("str_crafting_should_have_swing_damage", null).SetTextVariable("SWING_DAMAGE_TYPE", variable).ToString();
			}
			if (!flag2)
			{
				return GameTexts.FindText("str_crafting_should_have_thrust_damage", null).SetTextVariable("THRUST_DAMAGE_TYPE", variable).ToString();
			}
			return string.Empty;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00008B40 File Offset: 0x00006D40
		public static List<TooltipProperty> GetInventoryCharacterTooltip(Hero hero)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			CampaignUIHelper._inventorySkillTooltipTitle.SetTextVariable("HERO_NAME", hero.Name);
			CampaignUIHelper.TooltipAddPropertyTitle(list, CampaignUIHelper._inventorySkillTooltipTitle.ToString());
			CampaignUIHelper.TooltipAddDoubleSeperator(list, false);
			for (int i = 0; i < Skills.All.Count; i++)
			{
				SkillObject skillObject = Skills.All[i];
				list.Add(new TooltipProperty(skillObject.Name.ToString(), hero.GetSkillValue(skillObject).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return list;
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00008BCC File Offset: 0x00006DCC
		public static string GetHeroOccupationName(Hero hero)
		{
			string id;
			if (hero.IsWanderer)
			{
				id = "str_wanderer";
			}
			else if (hero.IsGangLeader)
			{
				id = "str_gang_leader";
			}
			else if (hero.IsPreacher)
			{
				id = "str_preacher";
			}
			else if (hero.IsMerchant)
			{
				id = "str_merchant";
			}
			else
			{
				Clan clan = hero.Clan;
				if (clan != null && clan.IsClanTypeMercenary)
				{
					id = "str_mercenary";
				}
				else if (hero.IsArtisan)
				{
					id = "str_artisan";
				}
				else if (hero.IsRuralNotable)
				{
					id = "str_charactertype_ruralnotable";
				}
				else if (hero.IsHeadman)
				{
					id = "str_charactertype_headman";
				}
				else if (hero.IsMinorFactionHero)
				{
					id = "str_charactertype_minorfaction";
				}
				else
				{
					if (!hero.IsLord)
					{
						return "";
					}
					if (hero.IsFemale)
					{
						id = "str_charactertype_lady";
					}
					else
					{
						id = "str_charactertype_lord";
					}
				}
			}
			return GameTexts.FindText(id, null).ToString();
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00008CBC File Offset: 0x00006EBC
		private static TooltipProperty GetSiegeMachineProgressLine(int hoursRemaining)
		{
			if (hoursRemaining > 0)
			{
				string text = CampaignUIHelper.GetHoursAndDaysTextFromHourValue(hoursRemaining).ToString();
				MBTextManager.SetTextVariable("PREPARATION_TIME", text, false);
				string value = GameTexts.FindText("str_preparations_complete_in_hours", null).ToString();
				return new TooltipProperty(" ", value, 0, false, TooltipProperty.TooltipPropertyFlags.None);
			}
			return null;
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00008D08 File Offset: 0x00006F08
		public static TextObject GetCommaSeparatedText(TextObject label, IEnumerable<TextObject> texts)
		{
			TextObject textObject = new TextObject("{=!}{RESULT}", null);
			int num = 0;
			foreach (TextObject text in texts)
			{
				if (num == 0)
				{
					MBTextManager.SetTextVariable("STR1", label ?? TextObject.GetEmpty(), false);
					MBTextManager.SetTextVariable("STR2", text, false);
					string text2 = GameTexts.FindText("str_STR1_STR2", null).ToString();
					MBTextManager.SetTextVariable("LEFT", text2, false);
					textObject.SetTextVariable("RESULT", text2);
				}
				else
				{
					MBTextManager.SetTextVariable("RIGHT", text, false);
					string text3 = GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString();
					MBTextManager.SetTextVariable("LEFT", text3, false);
					textObject.SetTextVariable("RESULT", text3);
				}
				num++;
			}
			return textObject;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00008DF0 File Offset: 0x00006FF0
		public static TextObject GetCommaNewlineSeparatedText(TextObject label, IEnumerable<TextObject> texts)
		{
			TextObject textObject = new TextObject("{=!}{RESULT}", null);
			int num = 0;
			foreach (TextObject text in texts)
			{
				if (num == 0)
				{
					MBTextManager.SetTextVariable("STR1", label ?? TextObject.GetEmpty(), false);
					MBTextManager.SetTextVariable("STR2", text, false);
					string text2 = GameTexts.FindText("str_STR1_STR2", null).ToString();
					MBTextManager.SetTextVariable("LEFT", text2, false);
					textObject.SetTextVariable("RESULT", text2);
				}
				else
				{
					MBTextManager.SetTextVariable("RIGHT", text, false);
					string text3 = GameTexts.FindText("str_LEFT_comma_newline_RIGHT", null).ToString();
					MBTextManager.SetTextVariable("newline", "\n", false);
					MBTextManager.SetTextVariable("LEFT", text3, false);
					textObject.SetTextVariable("RESULT", text3);
				}
				num++;
			}
			return textObject;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00008EE8 File Offset: 0x000070E8
		public static string GetHeroKingdomRank(Hero hero)
		{
			if (hero.Clan.Kingdom != null)
			{
				bool isUnderMercenaryService = hero.Clan.IsUnderMercenaryService;
				bool flag = hero == hero.Clan.Kingdom.Leader;
				bool flag2 = hero.Clan.Leader == hero;
				bool flag3 = !flag && !flag2;
				bool flag4 = hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero == hero;
				TextObject textObject = TextObject.GetEmpty();
				GameTexts.SetVariable("FACTION", hero.Clan.Kingdom.Name);
				GameTexts.SetVariable("FACTION_INFORMAL_NAME", hero.Clan.Kingdom.InformalName);
				if (flag)
				{
					textObject = GameTexts.FindText("str_hero_rank_of_faction", 1.ToString());
				}
				else if (isUnderMercenaryService)
				{
					textObject = GameTexts.FindText("str_hero_rank_of_faction_mercenary", null);
				}
				else if (flag2 || flag4)
				{
					textObject = GameTexts.FindText("str_hero_rank_of_faction", 0.ToString());
				}
				else if (flag3)
				{
					textObject = GameTexts.FindText("str_hero_rank_of_faction_nobleman", null);
				}
				textObject.SetCharacterProperties("HERO", hero.CharacterObject, false);
				return textObject.ToString();
			}
			return string.Empty;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000900C File Offset: 0x0000720C
		public static string GetHeroRank(Hero hero)
		{
			if (hero.Clan != null)
			{
				bool isUnderMercenaryService = hero.Clan.IsUnderMercenaryService;
				Kingdom kingdom = hero.Clan.Kingdom;
				bool flag = hero == ((kingdom != null) ? kingdom.Leader : null);
				bool flag2 = hero.Clan.Leader == hero && hero.Clan.Kingdom != null;
				bool flag3 = !flag && !flag2 && hero.Clan.Kingdom != null;
				if (flag)
				{
					return GameTexts.FindText("str_hero_rank", 1.ToString()).ToString();
				}
				if (isUnderMercenaryService)
				{
					return GameTexts.FindText("str_hero_rank_mercenary", null).ToString();
				}
				if (flag2)
				{
					return GameTexts.FindText("str_hero_rank", 0.ToString()).ToString();
				}
				if (flag3)
				{
					return GameTexts.FindText("str_hero_rank_nobleman", null).ToString();
				}
			}
			return string.Empty;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x000090E8 File Offset: 0x000072E8
		public static bool IsSettlementInformationHidden(Settlement settlement, out TextObject disableReason)
		{
			bool flag = !Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(settlement);
			disableReason = (flag ? new TextObject("{=cDkHJOkl}You need to be in the viewing range, control this settlement with your kingdom or have a clan member in the settlement to see its details.", null) : TextObject.GetEmpty());
			return flag;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00009128 File Offset: 0x00007328
		public static bool IsHeroInformationHidden(Hero hero, out TextObject disableReason)
		{
			bool flag = !Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(hero);
			disableReason = (flag ? new TextObject("{=akHsjtPh}You haven't met this hero yet.", null) : TextObject.GetEmpty());
			return flag;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00009168 File Offset: 0x00007368
		public static string GetPartyNameplateText(MobileParty party, bool includeAttachedParties)
		{
			int num = party.MemberRoster.TotalHealthyCount;
			int num2 = party.MemberRoster.TotalWounded;
			if (includeAttachedParties && party.Army != null && party.Army.LeaderParty == party)
			{
				for (int i = 0; i < party.Army.LeaderParty.AttachedParties.Count; i++)
				{
					MobileParty mobileParty = party.Army.LeaderParty.AttachedParties[i];
					num += mobileParty.MemberRoster.TotalHealthyCount;
					num2 += mobileParty.MemberRoster.TotalWounded;
				}
			}
			string abbreviatedValueTextFromValue = CampaignUIHelper.GetAbbreviatedValueTextFromValue(num);
			string abbreviatedValueTextFromValue2 = CampaignUIHelper.GetAbbreviatedValueTextFromValue(num2);
			return abbreviatedValueTextFromValue + ((num2 > 0) ? (" + " + abbreviatedValueTextFromValue2 + GameTexts.FindText("str_party_nameplate_wounded_abbr", null).ToString()) : "");
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00009234 File Offset: 0x00007434
		public static string GetPartyNameplateText(PartyBase party)
		{
			int totalHealthyCount = party.MemberRoster.TotalHealthyCount;
			int totalWounded = party.MemberRoster.TotalWounded;
			string abbreviatedValueTextFromValue = CampaignUIHelper.GetAbbreviatedValueTextFromValue(totalHealthyCount);
			string abbreviatedValueTextFromValue2 = CampaignUIHelper.GetAbbreviatedValueTextFromValue(totalWounded);
			return abbreviatedValueTextFromValue + ((totalWounded > 0) ? (" + " + abbreviatedValueTextFromValue2 + GameTexts.FindText("str_party_nameplate_wounded_abbr", null).ToString()) : "");
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00009290 File Offset: 0x00007490
		public static string GetValueChangeText(float originalValue, float valueChange, string valueFormat = "F0")
		{
			string text = originalValue.ToString(valueFormat);
			TextObject textObject = GameTexts.FindText("str_clan_workshop_material_daily_Change", null).SetTextVariable("IS_POSITIVE", (valueChange >= 0f) ? 1 : 0).SetTextVariable("CHANGE", MathF.Abs(valueChange).ToString(valueFormat));
			TextObject textObject2 = GameTexts.FindText("str_STR_in_parentheses", null);
			textObject2.SetTextVariable("STR", textObject.ToString());
			return GameTexts.FindText("str_STR1_space_STR2", null).SetTextVariable("STR1", text.ToString()).SetTextVariable("STR2", textObject2.ToString())
				.ToString();
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00009330 File Offset: 0x00007530
		public static string GetUpgradeHint(int index, int numOfItems, int availableUpgrades, int upgradeCoinCost, bool hasRequiredPerk, PerkObject requiredPerk, CharacterObject character, TroopRosterElement troop, int partyGoldChangeAmount, bool areUpgradesDisabled)
		{
			if (areUpgradesDisabled)
			{
				return new TextObject("{=R4rTlKMU}Troop upgrades are currently disabled.", null).ToString();
			}
			string text = null;
			CharacterObject characterObject = character.UpgradeTargets[index];
			int level = characterObject.Level;
			if (character.Culture.IsBandit ? (level >= character.Level) : (level > character.Level))
			{
				int upgradeXpCost = character.GetUpgradeXpCost(PartyBase.MainParty, index);
				GameTexts.SetVariable("newline", "\n");
				TextObject textObject = new TextObject("{=f4nc7FfE}Upgrade to {UPGRADE_NAME}", null);
				textObject.SetTextVariable("UPGRADE_NAME", characterObject.Name);
				text = textObject.ToString();
				if (troop.Xp < upgradeXpCost)
				{
					TextObject textObject2 = new TextObject("{=Voa0sinH}Required: {NEEDED_EXP_AMOUNT}xp (You have {CURRENT_EXP_AMOUNT})", null);
					textObject2.SetTextVariable("NEEDED_EXP_AMOUNT", upgradeXpCost);
					textObject2.SetTextVariable("CURRENT_EXP_AMOUNT", troop.Xp);
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", textObject2);
					text = GameTexts.FindText("str_string_newline_string", null).ToString();
				}
				if (characterObject.UpgradeRequiresItemFromCategory != null)
				{
					TextObject textObject3 = new TextObject((numOfItems > 0) ? "{=Raa4j4rF}Required: {UPGRADE_ITEM}" : "{=rThSy9ed}Required: {UPGRADE_ITEM} (You have none)", null);
					textObject3.SetTextVariable("UPGRADE_ITEM", characterObject.UpgradeRequiresItemFromCategory.GetName().ToString());
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", textObject3.ToString());
					text = GameTexts.FindText("str_string_newline_string", null).ToString();
				}
				TextObject textObject4 = new TextObject((Hero.MainHero.Gold + partyGoldChangeAmount < upgradeCoinCost) ? "{=63Ic1Ahe}Cost: {UPGRADE_COST} (You don't have)" : "{=McJjNM50}Cost: {UPGRADE_COST}", null);
				textObject4.SetTextVariable("UPGRADE_COST", upgradeCoinCost);
				GameTexts.SetVariable("STR1", textObject4);
				GameTexts.SetVariable("STR2", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				string content = GameTexts.FindText("str_STR1_STR2", null).ToString();
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", content);
				text = GameTexts.FindText("str_string_newline_string", null).ToString();
				if (!hasRequiredPerk)
				{
					GameTexts.SetVariable("STR1", text);
					TextObject textObject5 = new TextObject("{=68IlDbA2}You need to have {PERK_NAME} perk to upgrade a bandit troop to a normal troop.", null);
					textObject5.SetTextVariable("PERK_NAME", requiredPerk.Name);
					GameTexts.SetVariable("STR2", textObject5);
					text = GameTexts.FindText("str_string_newline_string", null).ToString();
				}
			}
			return text;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x00009578 File Offset: 0x00007778
		public static string GetStackModifierString(TextObject allStackText, TextObject fiveStackText, bool canFiveStack)
		{
			if (Input.IsGamepadActive)
			{
				return string.Empty;
			}
			TextObject variable = GameTexts.FindText("str_game_key_text", "anycontrol");
			allStackText.SetTextVariable("KEY_NAME", variable);
			if (canFiveStack)
			{
				TextObject variable2 = GameTexts.FindText("str_game_key_text", "anyshift");
				fiveStackText.SetTextVariable("KEY_NAME", variable2);
				return GameTexts.FindText("str_string_newline_string", null).SetTextVariable("newline", "\n").SetTextVariable("STR1", allStackText)
					.SetTextVariable("STR2", fiveStackText)
					.ToString();
			}
			return allStackText.ToString();
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000960C File Offset: 0x0000780C
		public static string ConvertToHexColor(uint color)
		{
			uint num = color % 4278190080U;
			return "#" + Convert.ToString((long)((ulong)num), 16).PadLeft(6, '0').ToUpper() + "FF";
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00009648 File Offset: 0x00007848
		public static bool GetMapScreenActionIsEnabledWithReason(out TextObject disabledReason)
		{
			if (Hero.MainHero.IsPrisoner)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_prisoner", null);
				return false;
			}
			if (MobileParty.MainParty.IsInRaftState)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_raft_state", null);
				return false;
			}
			if (CampaignMission.Current != null)
			{
				disabledReason = new TextObject("{=FdzsOvDq}This action is disabled while in a mission", null);
				return false;
			}
			if (PlayerEncounter.Current != null)
			{
				if (PlayerEncounter.EncounterSettlement == null)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_encounter", null);
					return false;
				}
				Village village = PlayerEncounter.EncounterSettlement.Village;
				if (village != null && village.VillageState == Village.VillageStates.BeingRaided)
				{
					MapEvent mapEvent = MobileParty.MainParty.MapEvent;
					if (mapEvent != null && mapEvent.IsRaid)
					{
						disabledReason = GameTexts.FindText("str_action_disabled_reason_raid", null);
						return false;
					}
				}
				if (PlayerEncounter.EncounterSettlement.IsUnderSiege)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_siege", null);
					return false;
				}
			}
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_siege", null);
				return false;
			}
			if (MobileParty.MainParty.MapEvent != null)
			{
				disabledReason = new TextObject("{=MIylzRc5}You can't perform this action while you are in a map event.", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00009754 File Offset: 0x00007954
		public static bool GetCanManageCurrentArmyWithReason(out TextObject disabledReason)
		{
			Army army = MobileParty.MainParty.Army;
			if (((army != null) ? army.LeaderParty : null) != MobileParty.MainParty)
			{
				disabledReason = TextObject.GetEmpty();
				return false;
			}
			if (Hero.MainHero.IsPrisoner)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_prisoner", null);
				return false;
			}
			if (PlayerEncounter.Current != null)
			{
				if (PlayerEncounter.EncounterSettlement == null)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_encounter", null);
					return false;
				}
				Village village = PlayerEncounter.EncounterSettlement.Village;
				if (village != null && village.VillageState == Village.VillageStates.BeingRaided && MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.IsRaid)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_raid", null);
					return false;
				}
			}
			if (MapEvent.PlayerMapEvent != null)
			{
				disabledReason = GameTexts.FindText("str_cannot_manage_army_while_in_event", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00009824 File Offset: 0x00007A24
		public static string GetClanSupportDisableReasonString(bool hasEnoughInfluence, bool isTargetMainClan, bool isMainClanMercenary)
		{
			if (Hero.MainHero.IsPrisoner)
			{
				return GameTexts.FindText("str_action_disabled_reason_prisoner", null).ToString();
			}
			if (isTargetMainClan)
			{
				return GameTexts.FindText("str_cannot_support_your_clan", null).ToString();
			}
			if (isMainClanMercenary)
			{
				return GameTexts.FindText("str_mercenaries_cannot_support_clans", null).ToString();
			}
			if (!hasEnoughInfluence)
			{
				return GameTexts.FindText("str_warning_you_dont_have_enough_influence", null).ToString();
			}
			return null;
		}

		// Token: 0x06000162 RID: 354 RVA: 0x0000988C File Offset: 0x00007A8C
		public static string GetClanExpelDisableReasonString(bool hasEnoughInfluence, bool isTargetMainClan, bool isTargetRulingClan, bool isMainClanMercenary)
		{
			if (Hero.MainHero.IsPrisoner)
			{
				return GameTexts.FindText("str_action_disabled_reason_prisoner", null).ToString();
			}
			if (isMainClanMercenary)
			{
				return GameTexts.FindText("str_mercenaries_cannot_expel_clans", null).ToString();
			}
			if (isTargetMainClan)
			{
				return GameTexts.FindText("str_cannot_expel_your_clan", null).ToString();
			}
			if (isTargetRulingClan)
			{
				return GameTexts.FindText("str_cannot_expel_ruling_clan", null).ToString();
			}
			if (!hasEnoughInfluence)
			{
				return GameTexts.FindText("str_warning_you_dont_have_enough_influence", null).ToString();
			}
			return null;
		}

		// Token: 0x06000163 RID: 355 RVA: 0x00009908 File Offset: 0x00007B08
		public static string GetArmyDisbandDisableReasonString(bool hasEnoughInfluence, bool isArmyInAnyEvent, bool isPlayerClanMercenary, bool isPlayerInThisArmy)
		{
			if (Hero.MainHero.IsPrisoner)
			{
				return GameTexts.FindText("str_action_disabled_reason_prisoner", null).ToString();
			}
			if (isPlayerClanMercenary)
			{
				return GameTexts.FindText("str_cannot_disband_army_while_mercenary", null).ToString();
			}
			if (isArmyInAnyEvent)
			{
				return GameTexts.FindText("str_cannot_disband_army_while_in_event", null).ToString();
			}
			if (isPlayerInThisArmy)
			{
				return GameTexts.FindText("str_cannot_disband_army_while_in_that_army", null).ToString();
			}
			if (!hasEnoughInfluence)
			{
				return GameTexts.FindText("str_warning_you_dont_have_enough_influence", null).ToString();
			}
			return null;
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00009983 File Offset: 0x00007B83
		public static TextObject GetCreateNewPartyReasonString(bool haveEmptyPartySlots, bool haveAvailableHero)
		{
			if (Hero.MainHero.IsPrisoner)
			{
				return GameTexts.FindText("str_action_disabled_reason_prisoner", null);
			}
			if (!haveEmptyPartySlots)
			{
				return GameTexts.FindText("str_clan_doesnt_have_empty_party_slots", null);
			}
			if (!haveAvailableHero)
			{
				return GameTexts.FindText("str_clan_doesnt_have_available_heroes", null);
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x06000165 RID: 357 RVA: 0x000099C0 File Offset: 0x00007BC0
		public static string GetCraftingDisableReasonString(bool playerHasEnoughMaterials)
		{
			if (!playerHasEnoughMaterials)
			{
				return GameTexts.FindText("str_warning_crafing_materials", null).ToString();
			}
			return string.Empty;
		}

		// Token: 0x06000166 RID: 358 RVA: 0x000099DC File Offset: 0x00007BDC
		public static string GetAddFocusHintString(bool playerHasEnoughPoints, bool isMaxedSkill, int currentFocusAmount)
		{
			GameTexts.SetVariable("newline", "\n");
			string content = GameTexts.FindText("str_focus_points", null).ToString();
			TextObject textObject = new TextObject("{=j3iwQmoA}Current focus amount: {CURRENT_AMOUNT}", null);
			textObject.SetTextVariable("CURRENT_AMOUNT", currentFocusAmount);
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", textObject);
			content = GameTexts.FindText("str_string_newline_string", null).ToString();
			if (!playerHasEnoughPoints)
			{
				GameTexts.SetVariable("STR1", content);
				GameTexts.SetVariable("STR2", GameTexts.FindText("str_player_doesnt_have_enough_points", null));
				return GameTexts.FindText("str_string_newline_string", null).ToString();
			}
			if (isMaxedSkill)
			{
				GameTexts.SetVariable("STR1", content);
				GameTexts.SetVariable("STR2", GameTexts.FindText("str_player_cannot_give_more_points", null));
				return GameTexts.FindText("str_string_newline_string", null).ToString();
			}
			GameTexts.SetVariable("COST", 1);
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_cost_COUNT", null));
			return GameTexts.FindText("str_string_newline_string", null).ToString();
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00009AEC File Offset: 0x00007CEC
		public static string GetSkillEffectText(SkillEffect effect, int skillLevel)
		{
			TextObject effectDescriptionForSkillLevel = SkillHelper.GetEffectDescriptionForSkillLevel(effect, skillLevel);
			if (effect.Role != PartyRole.None)
			{
				TextObject textObject = GameTexts.FindText("role", effect.Role.ToString());
				return string.Format("({0}) {1} ", textObject.ToString(), effectDescriptionForSkillLevel);
			}
			return effectDescriptionForSkillLevel.ToString();
		}

		// Token: 0x06000168 RID: 360 RVA: 0x00009B40 File Offset: 0x00007D40
		public static string GetMobilePartyBehaviorText(MobileParty party)
		{
			return party.GetBehaviorText().ToString();
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00009B50 File Offset: 0x00007D50
		public static string GetHeroBehaviorText(Hero hero, ITeleportationCampaignBehavior teleportationBehavior = null)
		{
			if (hero.CurrentSettlement != null)
			{
				GameTexts.SetVariable("SETTLEMENT_NAME", hero.CurrentSettlement.Name);
			}
			if (hero.IsPrisoner)
			{
				if (hero.CurrentSettlement != null)
				{
					return GameTexts.FindText("str_prisoner_at_settlement", null).ToString();
				}
				if (hero.PartyBelongedToAsPrisoner != null)
				{
					CampaignUIHelper._prisonerOfText.SetTextVariable("PARTY_NAME", hero.PartyBelongedToAsPrisoner.Name);
					return CampaignUIHelper._prisonerOfText.ToString();
				}
				return new TextObject("{=tYz4D8Or}Prisoner", null).ToString();
			}
			else
			{
				if (hero.IsTraveling)
				{
					IMapPoint mapPoint = null;
					bool flag = false;
					bool flag2 = false;
					if (teleportationBehavior == null || !teleportationBehavior.GetTargetOfTeleportingHero(hero, out flag, out flag2, out mapPoint))
					{
						return CampaignUIHelper._travelingText.ToString();
					}
					Settlement settlement;
					if (flag && (settlement = mapPoint as Settlement) != null)
					{
						TextObject textObject = new TextObject("{=gUUnZNGk}Moving to {SETTLEMENT_NAME} to be the new governor", null);
						textObject.SetTextVariable("SETTLEMENT_NAME", settlement.Name.ToString());
						return textObject.ToString();
					}
					if (flag2 && mapPoint is MobileParty)
					{
						return new TextObject("{=g08mptth}Moving to a party to be the new leader", null).ToString();
					}
					MobileParty mobileParty;
					if ((mobileParty = mapPoint as MobileParty) != null)
					{
						TextObject textObject2 = new TextObject("{=qaQqAYGc}Moving to {LEADER.NAME}{.o} Party", null);
						bool flag3;
						if (mobileParty == null)
						{
							flag3 = null != null;
						}
						else
						{
							Hero leaderHero = mobileParty.LeaderHero;
							flag3 = ((leaderHero != null) ? leaderHero.CharacterObject : null) != null;
						}
						if (flag3)
						{
							StringHelpers.SetCharacterProperties("LEADER", mobileParty.LeaderHero.CharacterObject, textObject2, false);
						}
						return textObject2.ToString();
					}
					Settlement settlement2;
					if ((settlement2 = mapPoint as Settlement) != null)
					{
						TextObject textObject3 = new TextObject("{=UUaW0dba}Moving to {SETTLEMENT_NAME}", null);
						string tag = "SETTLEMENT_NAME";
						string text;
						if (settlement2 == null)
						{
							text = null;
						}
						else
						{
							TextObject name = settlement2.Name;
							text = ((name != null) ? name.ToString() : null);
						}
						textObject3.SetTextVariable(tag, text ?? string.Empty);
						return textObject3.ToString();
					}
				}
				if (hero.PartyBelongedTo != null)
				{
					if (hero.PartyBelongedTo.LeaderHero == hero && hero.PartyBelongedTo.Army != null)
					{
						CampaignUIHelper._attachedToText.SetTextVariable("PARTY_NAME", hero.PartyBelongedTo.Army.Name);
						return CampaignUIHelper._attachedToText.ToString();
					}
					if (hero.PartyBelongedTo == MobileParty.MainParty)
					{
						return CampaignUIHelper._inYourPartyText.ToString();
					}
					Settlement settlement3 = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(hero.PartyBelongedTo.CurrentNavigationFace, hero.PartyBelongedTo.NavigationCapability).Item1;
					if (settlement3 == null)
					{
						float num = float.MaxValue;
						Settlement settlement4 = null;
						foreach (Settlement settlement5 in Settlement.All)
						{
							float num2 = settlement5.Position.Distance(hero.PartyBelongedTo.Position);
							if (num2 < num)
							{
								num = num2;
								settlement4 = settlement5;
							}
						}
						settlement3 = settlement4;
					}
					CampaignUIHelper._nearSettlementText.SetTextVariable("SETTLEMENT_NAME", settlement3.Name);
					return CampaignUIHelper._nearSettlementText.ToString();
				}
				else if (hero.CurrentSettlement != null)
				{
					if (hero.CurrentSettlement.Town != null && hero.GovernorOf == hero.CurrentSettlement.Town)
					{
						return GameTexts.FindText("str_governing_at_settlement", null).ToString();
					}
					if (Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().IsHeroAlleyLeaderOfAnyPlayerAlley(hero))
					{
						return GameTexts.FindText("str_alley_leader_at_settlement", null).ToString();
					}
					return GameTexts.FindText("str_staying_at_settlement", null).ToString();
				}
				else
				{
					if (Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
					{
						return GameTexts.FindText("str_solving_issue", null).ToString();
					}
					if (hero.IsFugitive)
					{
						return CampaignUIHelper._regroupingText.ToString();
					}
					if (hero.IsReleased)
					{
						GameTexts.SetVariable("LEFT", CampaignUIHelper._recoveringText);
						GameTexts.SetVariable("RIGHT", CampaignUIHelper._recentlyReleasedText);
						return GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString();
					}
					return new TextObject("{=RClxLG6N}Holding", null).ToString();
				}
			}
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00009F28 File Offset: 0x00008128
		public static string GetPartyLocationText(MobileParty mobileParty)
		{
			if (mobileParty.CurrentSettlement != null)
			{
				return mobileParty.CurrentSettlement.Name.ToString();
			}
			Settlement settlement;
			if ((settlement = SettlementHelper.FindNearestSettlementToMobileParty(mobileParty, MobileParty.NavigationType.All, null)) == null)
			{
				CampaignVec2 position = mobileParty.Position;
				settlement = SettlementHelper.FindNearestSettlementToPoint(position, null);
			}
			Settlement settlement2 = settlement;
			GameTexts.SetVariable("SETTLEMENT_NAME", settlement2.Name);
			return GameTexts.FindText("str_near_settlement", null).ToString();
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00009F8C File Offset: 0x0000818C
		public static Hero GetTeleportingLeaderHero(MobileParty party, ITeleportationCampaignBehavior teleportationBehavior)
		{
			if (party != null && teleportationBehavior != null)
			{
				foreach (Hero hero in from x in Hero.MainHero.Clan.Heroes
					where x.IsAlive && x.IsTraveling
					select x)
				{
					bool flag;
					bool flag2;
					IMapPoint mapPoint;
					MobileParty mobileParty;
					if (teleportationBehavior.GetTargetOfTeleportingHero(hero, out flag, out flag2, out mapPoint) && flag2 && (mobileParty = mapPoint as MobileParty) != null && mobileParty == party)
					{
						return hero;
					}
				}
			}
			return null;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0000A034 File Offset: 0x00008234
		public static Hero GetTeleportingGovernor(Settlement settlement, ITeleportationCampaignBehavior teleportationBehavior)
		{
			if (settlement != null && teleportationBehavior != null)
			{
				foreach (Hero hero in from x in Hero.MainHero.Clan.Heroes
					where x.IsAlive && x.IsTraveling
					select x)
				{
					bool flag;
					bool flag2;
					IMapPoint mapPoint;
					Settlement settlement2;
					if (teleportationBehavior.GetTargetOfTeleportingHero(hero, out flag, out flag2, out mapPoint) && flag && (settlement2 = mapPoint as Settlement) != null && settlement2 == settlement)
					{
						return hero;
					}
				}
			}
			return null;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000A0DC File Offset: 0x000082DC
		public static TextObject GetHeroRelationToHeroText(Hero queriedHero, Hero baseHero, bool uppercaseFirst)
		{
			GameTexts.SetVariable("RELATION_TEXT", ConversationHelper.GetHeroRelationToHeroTextShort(queriedHero, baseHero, uppercaseFirst));
			StringHelpers.SetCharacterProperties("BASE_HERO", baseHero.CharacterObject, null, false);
			return GameTexts.FindText("str_hero_family_relation", null);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0000A110 File Offset: 0x00008310
		public static string GetAbbreviatedValueTextFromValue(int valueAmount)
		{
			string variable = "";
			decimal num = valueAmount;
			if (valueAmount < 10000)
			{
				return valueAmount.ToString();
			}
			if (valueAmount >= 10000 && valueAmount < 1000000)
			{
				variable = new TextObject("{=thousandabbr}k", null).ToString();
				num /= 1000m;
			}
			else if (valueAmount >= 1000000 && valueAmount < 1000000000)
			{
				variable = new TextObject("{=millionabbr}m", null).ToString();
				num /= 1000000m;
			}
			else if (valueAmount >= 1000000000 && valueAmount <= 2147483647)
			{
				variable = new TextObject("{=billionabbr}b", null).ToString();
				num /= 1000000000m;
			}
			int num2 = (int)num;
			string text = num2.ToString();
			if (text.Length < 3)
			{
				text += ".";
				string text2 = num.ToString("F3").Split(new char[] { '.' }).ElementAtOrDefault(1);
				if (text2 != null)
				{
					for (int i = 0; i < 3 - num2.ToString().Length; i++)
					{
						if (text2.ElementAtOrDefault(i) != '\0')
						{
							text += text2.ElementAtOrDefault(i).ToString();
						}
					}
				}
			}
			CampaignUIHelper._denarValueInfoText.SetTextVariable("DENAR_AMOUNT", text);
			CampaignUIHelper._denarValueInfoText.SetTextVariable("VALUE_ABBREVIATION", variable);
			return CampaignUIHelper._denarValueInfoText.ToString();
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000A290 File Offset: 0x00008490
		public static string GetPartyDistanceByTimeText(float distance, float speed)
		{
			int num = MathF.Ceiling(distance / speed);
			int num2 = num / CampaignTime.HoursInDay;
			num %= CampaignTime.HoursInDay;
			GameTexts.SetVariable("IS_UNDER_A_DAY", (num2 <= 0) ? 1 : 0);
			GameTexts.SetVariable("IS_MORE_THAN_ONE_DAY", (num2 > 1) ? 1 : 0);
			GameTexts.SetVariable("DAY_VALUE", num2);
			GameTexts.SetVariable("IS_UNDER_ONE_HOUR", (num <= 0) ? 1 : 0);
			GameTexts.SetVariable("IS_MORE_THAN_AN_HOUR", (num > 1) ? 1 : 0);
			GameTexts.SetVariable("HOUR_VALUE", num);
			return GameTexts.FindText("str_distance_by_time", null).ToString();
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000A324 File Offset: 0x00008524
		public static string GetPartyDistanceByTimeTextAbbreviated(float distance, float speed)
		{
			int num = MathF.Ceiling(distance / speed);
			int num2 = num / CampaignTime.HoursInDay;
			num %= CampaignTime.HoursInDay;
			if (num2 < 0 || num < 0)
			{
				return "-";
			}
			GameTexts.SetVariable("DAY_VALUE", num2);
			GameTexts.SetVariable("HOUR_VALUE", num);
			return GameTexts.FindText("str_distance_by_time_abbreviated", null).ToString();
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000A380 File Offset: 0x00008580
		public static CharacterCode GetCharacterCode(CharacterObject character, bool useCivilian = false)
		{
			TextObject textObject;
			if (character == null || (character.IsHero && CampaignUIHelper.IsHeroInformationHidden(character.HeroObject, out textObject)))
			{
				return CharacterCode.CreateEmpty();
			}
			Hero heroObject = character.HeroObject;
			uint? num;
			if (heroObject == null)
			{
				num = null;
			}
			else
			{
				IFaction mapFaction = heroObject.MapFaction;
				num = ((mapFaction != null) ? new uint?(mapFaction.Color) : null);
			}
			uint color = num ?? ((character.Culture != null) ? character.Culture.Color : Color.White.ToUnsignedInteger());
			Hero heroObject2 = character.HeroObject;
			uint? num2;
			if (heroObject2 == null)
			{
				num2 = null;
			}
			else
			{
				IFaction mapFaction2 = heroObject2.MapFaction;
				num2 = ((mapFaction2 != null) ? new uint?(mapFaction2.Color2) : null);
			}
			uint color2 = num2 ?? ((character.Culture != null) ? character.Culture.Color2 : Color.White.ToUnsignedInteger());
			string equipmentCode = string.Empty;
			BodyProperties bodyProperties = character.GetBodyProperties(character.Equipment, -1);
			bool flag;
			if (!useCivilian)
			{
				Hero heroObject3 = character.HeroObject;
				flag = heroObject3 != null && heroObject3.IsNoncombatant;
			}
			else
			{
				flag = true;
			}
			useCivilian = flag;
			if (character.IsHero && character.HeroObject.IsLord)
			{
				Equipment equipment = ((useCivilian && character.FirstCivilianEquipment != null) ? character.FirstCivilianEquipment.Clone(false) : character.Equipment.Clone(false));
				equipment[EquipmentIndex.NumAllWeaponSlots] = new EquipmentElement(null, null, null, false);
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
				{
					ItemObject item = equipment[equipmentIndex].Item;
					bool flag2;
					if (item == null)
					{
						flag2 = false;
					}
					else
					{
						WeaponComponent weaponComponent = item.WeaponComponent;
						bool? flag3;
						if (weaponComponent == null)
						{
							flag3 = null;
						}
						else
						{
							WeaponComponentData primaryWeapon = weaponComponent.PrimaryWeapon;
							flag3 = ((primaryWeapon != null) ? new bool?(primaryWeapon.IsShield) : null);
						}
						bool? flag4 = flag3;
						bool flag5 = true;
						flag2 = (flag4.GetValueOrDefault() == flag5) & (flag4 != null);
					}
					if (flag2)
					{
						equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, default(EquipmentElement));
					}
				}
				equipmentCode = equipment.CalculateEquipmentCode();
			}
			else
			{
				equipmentCode = ((useCivilian && character.FirstCivilianEquipment != null) ? character.FirstCivilianEquipment.Clone(false) : character.FirstBattleEquipment.Clone(false)).CalculateEquipmentCode();
			}
			return CharacterCode.CreateFrom(equipmentCode, bodyProperties, character.IsFemale, character.IsHero, color, color2, character.DefaultFormationClass, character.Race);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x0000A5F0 File Offset: 0x000087F0
		public static string GetTraitNameText(TraitObject traitObject, Hero hero)
		{
			if (traitObject != DefaultTraits.Mercy && traitObject != DefaultTraits.Valor && traitObject != DefaultTraits.Honor && traitObject != DefaultTraits.Generosity && traitObject != DefaultTraits.Calculating)
			{
				Debug.FailedAssert("Cannot show this trait as text.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CampaignUIHelper.cs", "GetTraitNameText", 3303);
				return "";
			}
			int traitLevel = hero.GetTraitLevel(traitObject);
			if (traitLevel != 0)
			{
				return GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (traitLevel + MathF.Abs(traitObject.MinValue)).ToString()).ToString();
			}
			return "";
		}

		// Token: 0x06000173 RID: 371 RVA: 0x0000A68C File Offset: 0x0000888C
		public static string GetTraitTooltipText(TraitObject traitObject, int traitValue)
		{
			if (traitObject != DefaultTraits.Mercy && traitObject != DefaultTraits.Valor && traitObject != DefaultTraits.Honor && traitObject != DefaultTraits.Generosity && traitObject != DefaultTraits.Calculating)
			{
				Debug.FailedAssert("Cannot show this trait's tooltip.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CampaignUIHelper.cs", "GetTraitTooltipText", 3328);
				return null;
			}
			GameTexts.SetVariable("NEWLINE", "\n");
			if (traitValue != 0)
			{
				TextObject content = GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (traitValue + MathF.Abs(traitObject.MinValue)).ToString());
				GameTexts.SetVariable("TRAIT_VALUE", traitValue);
				GameTexts.SetVariable("TRAIT_NAME", content);
				TextObject content2 = GameTexts.FindText("str_trait", traitObject.StringId.ToLower());
				GameTexts.SetVariable("TRAIT", content2);
				GameTexts.SetVariable("TRAIT_DESCRIPTION", traitObject.Description);
				return GameTexts.FindText("str_trait_tooltip", null).ToString();
			}
			TextObject content3 = GameTexts.FindText("str_trait", traitObject.StringId.ToLower());
			GameTexts.SetVariable("TRAIT", content3);
			GameTexts.SetVariable("TRAIT_DESCRIPTION", traitObject.Description);
			return GameTexts.FindText("str_trait_description_tooltip", null).ToString();
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000A7BC File Offset: 0x000089BC
		public static string GetTextForRole(PartyRole role)
		{
			switch (role)
			{
			case PartyRole.None:
				return GameTexts.FindText("role", PartyRole.None.ToString()).ToString();
			case PartyRole.Ruler:
				return GameTexts.FindText("role", PartyRole.Ruler.ToString()).ToString();
			case PartyRole.ClanLeader:
				return GameTexts.FindText("role", PartyRole.ClanLeader.ToString()).ToString();
			case PartyRole.Governor:
				return GameTexts.FindText("role", PartyRole.Governor.ToString()).ToString();
			case PartyRole.ArmyCommander:
				return GameTexts.FindText("role", PartyRole.ArmyCommander.ToString()).ToString();
			case PartyRole.PartyLeader:
				return GameTexts.FindText("role", PartyRole.PartyLeader.ToString()).ToString();
			case PartyRole.PartyOwner:
				return GameTexts.FindText("role", PartyRole.PartyOwner.ToString()).ToString();
			case PartyRole.Surgeon:
				return GameTexts.FindText("role", PartyRole.Surgeon.ToString()).ToString();
			case PartyRole.Engineer:
				return GameTexts.FindText("role", PartyRole.Engineer.ToString()).ToString();
			case PartyRole.Scout:
				return GameTexts.FindText("role", PartyRole.Scout.ToString()).ToString();
			case PartyRole.Quartermaster:
				return GameTexts.FindText("role", PartyRole.Quartermaster.ToString()).ToString();
			case PartyRole.PartyMember:
				return GameTexts.FindText("role", PartyRole.PartyMember.ToString()).ToString();
			case PartyRole.Personal:
				return GameTexts.FindText("role", PartyRole.Personal.ToString()).ToString();
			default:
				return "";
			}
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0000A9A4 File Offset: 0x00008BA4
		public static int GetAttributeTypeSortIndex(CharacterAttribute attribute)
		{
			string stringId = attribute.StringId;
			for (int i = 0; i < CampaignUIHelper._attributeSortIndices.Count; i++)
			{
				if (stringId.Equals(CampaignUIHelper._attributeSortIndices[i], StringComparison.InvariantCultureIgnoreCase))
				{
					return CampaignUIHelper._attributeSortIndices.Count - i;
				}
			}
			return 0;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x0000A9F0 File Offset: 0x00008BF0
		public static int GetSkillObjectTypeSortIndex(SkillObject skill)
		{
			string stringId = skill.StringId;
			for (int i = 0; i < CampaignUIHelper._skillSortIndices.Count; i++)
			{
				if (stringId.Equals(CampaignUIHelper._skillSortIndices[i], StringComparison.InvariantCultureIgnoreCase))
				{
					return CampaignUIHelper._skillSortIndices.Count - i;
				}
			}
			return 0;
		}

		// Token: 0x06000177 RID: 375 RVA: 0x0000AA3C File Offset: 0x00008C3C
		public static string GetSkillMeshId(SkillObject skill, bool useSmallestVariation = true)
		{
			string str = "SPGeneral\\Skills\\gui_skills_icon_" + skill.StringId.ToLower();
			if (useSmallestVariation)
			{
				return str + "_tiny";
			}
			return str + "_small";
		}

		// Token: 0x06000178 RID: 376 RVA: 0x0000AA7C File Offset: 0x00008C7C
		public static bool GetIsNavalSkill(SkillObject skill)
		{
			string stringId = skill.StringId;
			for (int i = 0; i < CampaignUIHelper._navalSkills.Count; i++)
			{
				if (stringId.Equals(CampaignUIHelper._navalSkills[i], StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000AABC File Offset: 0x00008CBC
		public static int GetHeroCompareSortIndex(Hero x, Hero y)
		{
			int num;
			if (x.Clan == null && y.Clan == null)
			{
				num = 0;
			}
			else if (x.Clan == null)
			{
				num = 1;
			}
			else if (y.Clan == null)
			{
				num = -1;
			}
			else if (x.IsLord && !y.IsLord)
			{
				num = -1;
			}
			else if (!x.IsLord && y.IsLord)
			{
				num = 1;
			}
			else
			{
				num = -x.Clan.Renown.CompareTo(y.Clan.Renown);
			}
			if (num != 0)
			{
				return num;
			}
			int num2 = x.IsGangLeader.CompareTo(y.IsGangLeader);
			if (num2 != 0)
			{
				return num2;
			}
			num2 = y.Power.CompareTo(x.Power);
			if (num2 == 0)
			{
				return x.Name.ToString().CompareTo(y.Name.ToString());
			}
			return num2;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000AB94 File Offset: 0x00008D94
		public static string GetHeroClanRoleText(Hero hero, Clan clan)
		{
			return GameTexts.FindText("role", MobileParty.MainParty.GetHeroPartyRole(hero).ToString()).ToString();
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000ABCC File Offset: 0x00008DCC
		public static int GetItemObjectTypeSortIndex(ItemObject item)
		{
			if (item == null)
			{
				return -1;
			}
			int num = CampaignUIHelper._itemObjectTypeSortIndices.IndexOf(item.Type) * 100;
			switch (item.Type)
			{
			case ItemObject.ItemTypeEnum.Invalid:
			case ItemObject.ItemTypeEnum.HeadArmor:
			case ItemObject.ItemTypeEnum.BodyArmor:
			case ItemObject.ItemTypeEnum.LegArmor:
			case ItemObject.ItemTypeEnum.HandArmor:
			case ItemObject.ItemTypeEnum.Animal:
			case ItemObject.ItemTypeEnum.Book:
			case ItemObject.ItemTypeEnum.ChestArmor:
			case ItemObject.ItemTypeEnum.Cape:
			case ItemObject.ItemTypeEnum.HorseHarness:
			case ItemObject.ItemTypeEnum.Banner:
				return num;
			case ItemObject.ItemTypeEnum.Horse:
				if (!item.HorseComponent.IsRideable)
				{
					return num;
				}
				return num + 1;
			case ItemObject.ItemTypeEnum.OneHandedWeapon:
			case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			case ItemObject.ItemTypeEnum.Polearm:
			case ItemObject.ItemTypeEnum.Arrows:
			case ItemObject.ItemTypeEnum.Bolts:
			case ItemObject.ItemTypeEnum.SlingStones:
			case ItemObject.ItemTypeEnum.Shield:
			case ItemObject.ItemTypeEnum.Bow:
			case ItemObject.ItemTypeEnum.Crossbow:
			case ItemObject.ItemTypeEnum.Sling:
			case ItemObject.ItemTypeEnum.Thrown:
			case ItemObject.ItemTypeEnum.Pistol:
			case ItemObject.ItemTypeEnum.Musket:
			case ItemObject.ItemTypeEnum.Bullets:
				return (int)(num + item.PrimaryWeapon.WeaponClass);
			case ItemObject.ItemTypeEnum.Goods:
				if (!item.IsFood)
				{
					return num + 1;
				}
				return num;
			default:
				return 1;
			}
		}

		// Token: 0x0600017C RID: 380 RVA: 0x0000AC9F File Offset: 0x00008E9F
		public static string GetItemLockStringID(EquipmentElement equipmentElement)
		{
			return equipmentElement.Item.StringId + ((equipmentElement.ItemModifier != null) ? equipmentElement.ItemModifier.StringId : "");
		}

		// Token: 0x0600017D RID: 381 RVA: 0x0000ACCE File Offset: 0x00008ECE
		public static string GetTroopLockStringID(TroopRosterElement rosterElement)
		{
			return rosterElement.Character.StringId;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0000ACDC File Offset: 0x00008EDC
		public static List<ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject>> GetQuestStateOfHero(Hero queriedHero)
		{
			List<ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject>> list = new List<ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject>>();
			if (Campaign.Current != null)
			{
				IssueBase relatedIssue;
				Campaign.Current.IssueManager.Issues.TryGetValue(queriedHero, out relatedIssue);
				if (relatedIssue == null)
				{
					relatedIssue = queriedHero.Issue;
				}
				List<QuestBase> questsRelatedToHero = CampaignUIHelper.GetQuestsRelatedToHero(queriedHero);
				if (questsRelatedToHero.Count > 0)
				{
					for (int i = 0; i < questsRelatedToHero.Count; i++)
					{
						if (questsRelatedToHero[i].QuestGiver == queriedHero)
						{
							list.Add(new ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject>(questsRelatedToHero[i].IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest : CampaignUIHelper.IssueQuestFlags.ActiveIssue, questsRelatedToHero[i].Title, (questsRelatedToHero[i].JournalEntries.Count > 0) ? questsRelatedToHero[i].JournalEntries[0].LogText : TextObject.GetEmpty()));
						}
						else
						{
							list.Add(new ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject>(questsRelatedToHero[i].IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest : CampaignUIHelper.IssueQuestFlags.TrackedIssue, questsRelatedToHero[i].Title, (questsRelatedToHero[i].JournalEntries.Count > 0) ? questsRelatedToHero[i].JournalEntries[0].LogText : TextObject.GetEmpty()));
						}
					}
				}
				bool flag;
				if (questsRelatedToHero != null)
				{
					IssueBase relatedIssue2 = relatedIssue;
					if (((relatedIssue2 != null) ? relatedIssue2.IssueQuest : null) != null)
					{
						flag = questsRelatedToHero.Any((QuestBase q) => q == relatedIssue.IssueQuest);
						goto IL_171;
					}
				}
				flag = false;
				IL_171:
				bool flag2 = flag;
				if (relatedIssue != null && !flag2)
				{
					ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject> item = new ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject>(CampaignUIHelper.GetIssueType(relatedIssue), relatedIssue.Title, relatedIssue.Description);
					list.Add(item);
				}
			}
			return list;
		}

		// Token: 0x0600017F RID: 383 RVA: 0x0000AE98 File Offset: 0x00009098
		public static string GetQuestExplanationOfHero(CampaignUIHelper.IssueQuestFlags questType)
		{
			bool flag = (questType & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None || (questType & CampaignUIHelper.IssueQuestFlags.AvailableIssue) > CampaignUIHelper.IssueQuestFlags.None;
			bool flag2 = (questType & CampaignUIHelper.IssueQuestFlags.ActiveIssue) > CampaignUIHelper.IssueQuestFlags.None;
			string result = null;
			if (questType != CampaignUIHelper.IssueQuestFlags.None)
			{
				if (flag)
				{
					result = GameTexts.FindText("str_hero_has_" + (flag2 ? "active" : "available") + "_issue", null).ToString();
				}
				else
				{
					result = GameTexts.FindText("str_hero_has_active_quest", null).ToString();
				}
			}
			return result;
		}

		// Token: 0x06000180 RID: 384 RVA: 0x0000AF04 File Offset: 0x00009104
		public static List<QuestBase> GetQuestsRelatedToHero(Hero hero)
		{
			List<QuestBase> list = new List<QuestBase>();
			List<QuestBase> list2;
			Campaign.Current.QuestManager.TrackedObjects.TryGetValue(hero, out list2);
			foreach (QuestBase questBase in Campaign.Current.QuestManager.GetQuestGiverQuests(hero))
			{
				if (questBase.IsTrackEnabled && !list.Contains(questBase))
				{
					list.Add(questBase);
				}
			}
			if (list2 != null)
			{
				for (int i = 0; i < list2.Count; i++)
				{
					if (list2[i].IsTrackEnabled && !list.Contains(list2[i]))
					{
						list.Add(list2[i]);
					}
				}
			}
			IssueBase issue = hero.Issue;
			if (((issue != null) ? issue.IssueQuest : null) != null && hero.Issue.IssueQuest.IsTrackEnabled && !hero.Issue.IssueQuest.IsTracked(hero) && !list.Contains(hero.Issue.IssueQuest))
			{
				list.Add(hero.Issue.IssueQuest);
			}
			return list;
		}

		// Token: 0x06000181 RID: 385 RVA: 0x0000B030 File Offset: 0x00009230
		public static List<QuestBase> GetQuestsRelatedToParty(MobileParty party)
		{
			List<QuestBase> list = new List<QuestBase>();
			List<QuestBase> list2;
			Campaign.Current.QuestManager.TrackedObjects.TryGetValue(party, out list2);
			if (list2 != null)
			{
				for (int i = 0; i < list2.Count; i++)
				{
					if (list2[i].IsTrackEnabled)
					{
						list.Add(list2[i]);
					}
				}
			}
			if (party.MemberRoster.TotalHeroes > 0)
			{
				if (party.LeaderHero != null && party.MemberRoster.TotalHeroes == 1)
				{
					List<QuestBase> questsRelatedToHero = CampaignUIHelper.GetQuestsRelatedToHero(party.LeaderHero);
					if (questsRelatedToHero != null && questsRelatedToHero.Count > 0)
					{
						list.AddRange(questsRelatedToHero);
					}
				}
				else
				{
					for (int j = 0; j < party.MemberRoster.Count; j++)
					{
						CharacterObject characterAtIndex = party.MemberRoster.GetCharacterAtIndex(j);
						Hero hero = ((characterAtIndex != null) ? characterAtIndex.HeroObject : null);
						if (hero != null)
						{
							List<QuestBase> questsRelatedToHero2 = CampaignUIHelper.GetQuestsRelatedToHero(hero);
							if (questsRelatedToHero2 != null && questsRelatedToHero2.Count > 0)
							{
								list.AddRange(questsRelatedToHero2);
							}
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000182 RID: 386 RVA: 0x0000B12C File Offset: 0x0000932C
		[return: TupleElementNames(new string[] { "isHeroQuestGiver", "quest" })]
		public static List<ValueTuple<bool, QuestBase>> GetQuestsRelatedToSettlement(Settlement settlement)
		{
			List<ValueTuple<bool, QuestBase>> list = new List<ValueTuple<bool, QuestBase>>();
			foreach (KeyValuePair<ITrackableCampaignObject, List<QuestBase>> keyValuePair in Campaign.Current.QuestManager.TrackedObjects)
			{
				Hero hero = keyValuePair.Key as Hero;
				MobileParty mobileParty = keyValuePair.Key as MobileParty;
				if ((hero != null && hero.CurrentSettlement == settlement) || (mobileParty != null && mobileParty.CurrentSettlement == settlement))
				{
					for (int i = 0; i < keyValuePair.Value.Count; i++)
					{
						bool item = keyValuePair.Value[i].QuestGiver != null && (keyValuePair.Value[i].QuestGiver == hero || keyValuePair.Value[i].QuestGiver == ((mobileParty != null) ? mobileParty.LeaderHero : null));
						if (!list.Contains(new ValueTuple<bool, QuestBase>(item, keyValuePair.Value[i])) && keyValuePair.Value[i].IsTrackEnabled)
						{
							list.Add(new ValueTuple<bool, QuestBase>(item, keyValuePair.Value[i]));
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000183 RID: 387 RVA: 0x0000B298 File Offset: 0x00009498
		public static bool IsQuestRelatedToSettlement(QuestBase quest, Settlement settlement)
		{
			Hero questGiver = quest.QuestGiver;
			if (((questGiver != null) ? questGiver.CurrentSettlement : null) == settlement || quest.IsTracked(settlement))
			{
				return true;
			}
			foreach (KeyValuePair<ITrackableCampaignObject, List<QuestBase>> keyValuePair in Campaign.Current.QuestManager.TrackedObjects)
			{
				Hero hero = keyValuePair.Key as Hero;
				MobileParty mobileParty = keyValuePair.Key as MobileParty;
				if ((hero != null && hero.CurrentSettlement == settlement) || (mobileParty != null && mobileParty.CurrentSettlement == settlement))
				{
					for (int i = 0; i < keyValuePair.Value.Count; i++)
					{
						if (keyValuePair.Value[i].IsTrackEnabled && keyValuePair.Value[i] == quest)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x06000184 RID: 388 RVA: 0x0000B390 File Offset: 0x00009590
		public static CampaignUIHelper.IssueQuestFlags GetIssueType(IssueBase issue)
		{
			if (issue.IsSolvingWithAlternative || issue.IsSolvingWithLordSolution || issue.IsSolvingWithQuest)
			{
				return CampaignUIHelper.IssueQuestFlags.ActiveIssue;
			}
			return CampaignUIHelper.IssueQuestFlags.AvailableIssue;
		}

		// Token: 0x06000185 RID: 389 RVA: 0x0000B3AD File Offset: 0x000095AD
		public static CampaignUIHelper.IssueQuestFlags GetQuestType(QuestBase quest, Hero queriedQuestGiver)
		{
			if (quest.QuestGiver != null && quest.QuestGiver == queriedQuestGiver)
			{
				if (!quest.IsSpecialQuest)
				{
					return CampaignUIHelper.IssueQuestFlags.ActiveIssue;
				}
				return CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest;
			}
			else
			{
				if (!quest.IsSpecialQuest)
				{
					return CampaignUIHelper.IssueQuestFlags.TrackedIssue;
				}
				return CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest;
			}
		}

		// Token: 0x06000186 RID: 390 RVA: 0x0000B3D8 File Offset: 0x000095D8
		public static IEnumerable<TraitObject> GetHeroTraits()
		{
			yield return DefaultTraits.Generosity;
			yield return DefaultTraits.Honor;
			yield return DefaultTraits.Valor;
			yield return DefaultTraits.Mercy;
			yield return DefaultTraits.Calculating;
			yield break;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x0000B3E1 File Offset: 0x000095E1
		public static bool IsItemUsageApplicable(WeaponComponentData weapon)
		{
			WeaponDescription weaponDescription = ((weapon != null && weapon.WeaponDescriptionId != null) ? MBObjectManager.Instance.GetObject<WeaponDescription>(weapon.WeaponDescriptionId) : null);
			return weaponDescription != null && !weaponDescription.IsHiddenFromUI;
		}

		// Token: 0x06000188 RID: 392 RVA: 0x0000B40F File Offset: 0x0000960F
		public static string FloatToString(float x)
		{
			return x.ToString("F1");
		}

		// Token: 0x06000189 RID: 393 RVA: 0x0000B420 File Offset: 0x00009620
		private static Tuple<bool, TextObject> GetIsStringApplicableForHeroName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=C9tKA0ul}Character name cannot be empty", null));
			}
			bool flag;
			if (name.Length < 3)
			{
				if (!name.Any((char c) => Common.IsCharAsian(c)))
				{
					flag = false;
					goto IL_5A;
				}
			}
			flag = name.Length <= 50;
			IL_5A:
			if (!flag)
			{
				TextObject textObject = new TextObject("{=fPoB2u5m}Character name should be between {MIN} and {MAX} characters", null);
				textObject.SetTextVariable("MIN", 3);
				textObject.SetTextVariable("MAX", 50);
				return new Tuple<bool, TextObject>(false, textObject);
			}
			if (!name.All((char x) => (char.IsLetterOrDigit(x) || char.IsWhiteSpace(x) || char.IsPunctuation(x)) && x != '{' && x != '}'))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=P1hk0m4o}Character name cannot contain special characters", null));
			}
			if (name.StartsWith(" ") || name.EndsWith(" "))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=oofSja21}Character name cannot start or end with a white space", null));
			}
			if (name.Contains("  "))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=wcSSgFyK}Character name cannot contain consecutive white spaces", null));
			}
			return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0000B548 File Offset: 0x00009748
		public static Tuple<bool, string> IsStringApplicableForHeroName(string name)
		{
			Tuple<bool, TextObject> isStringApplicableForHeroName = CampaignUIHelper.GetIsStringApplicableForHeroName(name);
			return new Tuple<bool, string>(isStringApplicableForHeroName.Item1, isStringApplicableForHeroName.Item2.ToString());
		}

		// Token: 0x0600018B RID: 395 RVA: 0x0000B574 File Offset: 0x00009774
		public static Tuple<bool, TextObject> IsStringApplicableForItemName(string name)
		{
			bool flag;
			if (name.Length < 3)
			{
				if (!name.Any((char c) => Common.IsCharAsian(c)))
				{
					flag = false;
					goto IL_40;
				}
			}
			flag = name.Length <= 50;
			IL_40:
			if (!flag)
			{
				TextObject textObject = new TextObject("{=h0xoKxxo}Item name should be between {MIN} and {MAX} characters.", null);
				textObject.SetTextVariable("MIN", 3);
				textObject.SetTextVariable("MAX", 50);
				return new Tuple<bool, TextObject>(false, textObject);
			}
			if (string.IsNullOrEmpty(name))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=QQ03J6sf}Item name can not be empty.", null));
			}
			if (!name.All((char x) => (char.IsLetterOrDigit(x) || char.IsWhiteSpace(x) || char.IsPunctuation(x)) && x != '{' && x != '}'))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=NkY3Kq9l}Item name cannot contain special characters.", null));
			}
			if (name.StartsWith(" ") || name.EndsWith(" "))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=2Hbr4TEj}Item name cannot start or end with a white space.", null));
			}
			if (name.Contains("  "))
			{
				return new Tuple<bool, TextObject>(false, new TextObject("{=Z4GdqdgV}Item name cannot contain consecutive white spaces.", null));
			}
			return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
		}

		// Token: 0x0600018C RID: 396 RVA: 0x0000B69B File Offset: 0x0000989B
		public static CharacterObject GetVisualPartyLeader(PartyBase party)
		{
			return PartyBaseHelper.GetVisualPartyLeader(party);
		}

		// Token: 0x0600018D RID: 397 RVA: 0x0000B6A4 File Offset: 0x000098A4
		private static string GetChangeValueString(float value)
		{
			string text = value.ToString("0.##");
			if (value > 0.001f)
			{
				MBTextManager.SetTextVariable("NUMBER", text, false);
				return GameTexts.FindText("str_plus_with_number", null).ToString();
			}
			return text;
		}

		// Token: 0x0600018E RID: 398 RVA: 0x0000B6E4 File Offset: 0x000098E4
		public static List<Hero> GetChildrenAndGrandchildrenOfHero(Hero hero)
		{
			List<Hero> list = hero.Children.ToList<Hero>();
			foreach (Hero hero2 in hero.Children)
			{
				foreach (Hero item in hero2.Children)
				{
					list.Add(item);
				}
			}
			return list;
		}

		// Token: 0x06000190 RID: 400 RVA: 0x0000BDE8 File Offset: 0x00009FE8
		[CompilerGenerated]
		internal static void <GetOrderCannotBeCompletedReasonTooltip>g__AddProperty|160_0(CraftingTemplate.CraftingStatTypes type, float reqValue, ref CampaignUIHelper.<>c__DisplayClass160_0 A_2)
		{
			TextObject textObject = GameTexts.FindText("str_crafting_stat", type.ToString());
			TextObject variable = GameTexts.FindText("str_inventory_dmg_type", ((int)A_2.order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamageType).ToString());
			textObject.SetTextVariable("THRUST_DAMAGE_TYPE", variable);
			TextObject variable2 = GameTexts.FindText("str_inventory_dmg_type", ((int)A_2.order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamageType).ToString());
			textObject.SetTextVariable("SWING_DAMAGE_TYPE", variable2);
			CampaignUIHelper._orderRequirementText.SetTextVariable("STAT", textObject);
			CampaignUIHelper._orderRequirementText.SetTextVariable("REQUIREMENT", reqValue, 2);
			A_2.properties.Add(new TooltipProperty(CampaignUIHelper._orderRequirementText.ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
		}

		// Token: 0x04000079 RID: 121
		public static readonly CampaignUIHelper.IssueQuestFlags[] IssueQuestFlagsValues = (CampaignUIHelper.IssueQuestFlags[])Enum.GetValues(typeof(CampaignUIHelper.IssueQuestFlags));

		// Token: 0x0400007A RID: 122
		private static readonly TextObject _changeStr = new TextObject("{=R2AaCaPJ}Expected Change", null);

		// Token: 0x0400007B RID: 123
		private static readonly TextObject _totalStr = new TextObject("{=kWVbHPtT}Total", null);

		// Token: 0x0400007C RID: 124
		private static readonly TextObject _noChangesStr = new TextObject("{=XIioBPi0}No changes", null);

		// Token: 0x0400007D RID: 125
		private static readonly TextObject _hitPointsStr = new TextObject("{=oBbiVeKE}Hit Points", null);

		// Token: 0x0400007E RID: 126
		private static readonly TextObject _maxhitPointsStr = new TextObject("{=mDFhzEMC}Max. Hit Points", null);

		// Token: 0x0400007F RID: 127
		private static readonly TextObject _prosperityStr = new TextObject("{=IagYTD5O}Prosperity", null);

		// Token: 0x04000080 RID: 128
		private static readonly TextObject _hearthStr = new TextObject("{=2GWR9Cba}Hearth", null);

		// Token: 0x04000081 RID: 129
		private static readonly TextObject _dailyProductionStr = new TextObject("{=94aHU6nD}Construction", null);

		// Token: 0x04000082 RID: 130
		private static readonly TextObject _securityStr = new TextObject("{=MqCH7R4A}Security", null);

		// Token: 0x04000083 RID: 131
		private static readonly TextObject _criminalRatingStr = new TextObject("{=r0WIRUHo}Criminal Rating", null);

		// Token: 0x04000084 RID: 132
		private static readonly TextObject _militiaStr = new TextObject("{=gsVtO9A7}Militia", null);

		// Token: 0x04000085 RID: 133
		private static readonly TextObject _foodStr = new TextObject("{=qSi4DlT4}Food", null);

		// Token: 0x04000086 RID: 134
		private static readonly TextObject _foodItemsStr = new TextObject("{=IQY9yykn}Food Items", null);

		// Token: 0x04000087 RID: 135
		private static readonly TextObject _livestockStr = new TextObject("{=UI0q8rWw}Livestock", null);

		// Token: 0x04000088 RID: 136
		private static readonly TextObject _armyCohesionStr = new TextObject("{=iZ3w6opW}Cohesion", null);

		// Token: 0x04000089 RID: 137
		private static readonly TextObject _loyaltyStr = new TextObject("{=YO0x7ZAo}Loyalty", null);

		// Token: 0x0400008A RID: 138
		private static readonly TextObject _wallsStr = new TextObject("{=LsZEdD2z}Walls", null);

		// Token: 0x0400008B RID: 139
		private static readonly TextObject _plusStr = new TextObject("{=eTw2aNV5}+", null);

		// Token: 0x0400008C RID: 140
		private static readonly TextObject _heroesHealingRateStr = new TextObject("{=HHTQVp52}Heroes Healing Rate", null);

		// Token: 0x0400008D RID: 141
		private static readonly TextObject _numTotalTroopsInTheArmyStr = new TextObject("{=DRJOxrRF}Troops in Army", null);

		// Token: 0x0400008E RID: 142
		private static readonly TextObject _garrisonStr = new TextObject("{=jlgjLDo7}Garrison", null);

		// Token: 0x0400008F RID: 143
		private static readonly TextObject _hitPoints = new TextObject("{=UbZL2BJQ}Hitpoints", null);

		// Token: 0x04000090 RID: 144
		private static readonly TextObject _maxhitPoints = new TextObject("{=KTTyBbsp}Max HP", null);

		// Token: 0x04000091 RID: 145
		private static readonly TextObject _goldStr = new TextObject("{=Hxf6bzmR}Current Denars", null);

		// Token: 0x04000092 RID: 146
		private static readonly TextObject _resultGold = new TextObject("{=NC9bbrt5}End-of-day denars", null);

		// Token: 0x04000093 RID: 147
		private static readonly TextObject _influenceStr = new TextObject("{=RVPidk5a}Influence", null);

		// Token: 0x04000094 RID: 148
		private static readonly TextObject _partyMoraleStr = GameTexts.FindText("str_party_morale", null);

		// Token: 0x04000095 RID: 149
		private static readonly TextObject _partyFoodStr = new TextObject("{=mg7id9om}Number of Consumable Items", null);

		// Token: 0x04000096 RID: 150
		private static readonly TextObject _partySpeedStr = new TextObject("{=zWaVxD6T}Party Speed", null);

		// Token: 0x04000097 RID: 151
		private static readonly TextObject _partySizeLimitStr = new TextObject("{=mp68RYnD}Party Size Limit", null);

		// Token: 0x04000098 RID: 152
		private static readonly TextObject _viewDistanceFoodStr = new TextObject("{=hTzTMLsf}View Distance", null);

		// Token: 0x04000099 RID: 153
		private static readonly TextObject _battleReadyTroopsStr = new TextObject("{=LVmkE2Ow}Battle Ready Troops", null);

		// Token: 0x0400009A RID: 154
		private static readonly TextObject _patrolStr = new TextObject("{=townPatrol}Patrol", null);

		// Token: 0x0400009B RID: 155
		private static readonly TextObject _woundedTroopsStr = new TextObject("{=TzLtVzdg}Wounded Troops", null);

		// Token: 0x0400009C RID: 156
		private static readonly TextObject _prisonersStr = new TextObject("{=N6QTvjMf}Prisoners", null);

		// Token: 0x0400009D RID: 157
		private static readonly TextObject _regularsHealingRateStr = new TextObject("{=tf7301NC}Healing Rate", null);

		// Token: 0x0400009E RID: 158
		private static readonly TextObject _learningRateStr = new TextObject("{=q1J4a8rr}Learning Rate", null);

		// Token: 0x0400009F RID: 159
		private static readonly TextObject _learningLimitStr = new TextObject("{=YT9giTet}Learning Limit", null);

		// Token: 0x040000A0 RID: 160
		private static readonly TextObject _partyInventoryCapacityStr = new TextObject("{=fI7a7RoE}Inventory Capacity", null);

		// Token: 0x040000A1 RID: 161
		private static readonly TextObject _partyInventoryCargoCapacityStr = new TextObject("{=*}Cargo Capacity", null);

		// Token: 0x040000A2 RID: 162
		private static readonly TextObject _partyInventoryLandCapacityStr = new TextObject("{=cBqjZjfJ}Inventory Capacity on Land", null);

		// Token: 0x040000A3 RID: 163
		private static readonly TextObject _partyInventorySeaCapacityStr = new TextObject("{=*}Cargo Capacity at Sea", null);

		// Token: 0x040000A4 RID: 164
		private static readonly TextObject _partyInventoryWeightStr = new TextObject("{=4Dd2xgPm}Weight", null);

		// Token: 0x040000A5 RID: 165
		private static readonly TextObject _partyInventoryCargoStr = new TextObject("{=*}Cargo", null);

		// Token: 0x040000A6 RID: 166
		private static readonly TextObject _partyInventoryLandWeightStr = new TextObject("{=8d23bRmv}Weight on Land", null);

		// Token: 0x040000A7 RID: 167
		private static readonly TextObject _partyInventorySeaWeightStr = new TextObject("{=*}Cargo at Sea", null);

		// Token: 0x040000A8 RID: 168
		private static readonly TextObject _partyTroopSizeLimitStr = new TextObject("{=2Cq3tViJ}Party Troop Size Limit", null);

		// Token: 0x040000A9 RID: 169
		private static readonly TextObject _partyPrisonerSizeLimitStr = new TextObject("{=UHLcmf9A}Party Prisoner Size Limit", null);

		// Token: 0x040000AA RID: 170
		private static readonly TextObject _inventorySkillTooltipTitle = new TextObject("{=Y7qbwrWE}{HERO_NAME}'s Skills", null);

		// Token: 0x040000AB RID: 171
		private static readonly TextObject _mercenaryClanInfluenceStr = new TextObject("{=GP3jpU0X}Influence is periodically converted to denars for mercenary clans.", null);

		// Token: 0x040000AC RID: 172
		private static readonly TextObject _orderRequirementText = new TextObject("{=dVqowrRz} - {STAT} {REQUIREMENT}", null);

		// Token: 0x040000AD RID: 173
		private static readonly TextObject _denarValueInfoText = new TextObject("{=mapbardenarvalue}{DENAR_AMOUNT}{VALUE_ABBREVIATION}", null);

		// Token: 0x040000AE RID: 174
		private static readonly TextObject _prisonerOfText = new TextObject("{=a8nRxITn}Prisoner of {PARTY_NAME}", null);

		// Token: 0x040000AF RID: 175
		private static readonly TextObject _attachedToText = new TextObject("{=8Jy9DnKk}Attached to {PARTY_NAME}", null);

		// Token: 0x040000B0 RID: 176
		private static readonly TextObject _inYourPartyText = new TextObject("{=CRi905Ao}In your party", null);

		// Token: 0x040000B1 RID: 177
		private static readonly TextObject _travelingText = new TextObject("{=vdKiLwaf}Traveling", null);

		// Token: 0x040000B2 RID: 178
		private static readonly TextObject _recoveringText = new TextObject("{=heroRecovering}Recovering", null);

		// Token: 0x040000B3 RID: 179
		private static readonly TextObject _recentlyReleasedText = new TextObject("{=NLFeyz7m}Recently Released From Captivity", null);

		// Token: 0x040000B4 RID: 180
		private static readonly TextObject _recentlyEscapedText = new TextObject("{=84oSzquz}Recently Escaped Captivity", null);

		// Token: 0x040000B5 RID: 181
		private static readonly TextObject _nearSettlementText = new TextObject("{=XjT8S4ng}Near {SETTLEMENT_NAME}", null);

		// Token: 0x040000B6 RID: 182
		private static readonly TextObject _noDelayText = new TextObject("{=bDwTWrru}No delay", null);

		// Token: 0x040000B7 RID: 183
		private static readonly TextObject _regroupingText = new TextObject("{=KxLoeSEO}Regrouping", null);

		// Token: 0x040000B8 RID: 184
		public static readonly CampaignUIHelper.MobilePartyPrecedenceComparer MobilePartyPrecedenceComparerInstance = new CampaignUIHelper.MobilePartyPrecedenceComparer();

		// Token: 0x040000B9 RID: 185
		public static readonly CampaignUIHelper.SkillObjectComparer SkillObjectComparerInstance = new CampaignUIHelper.SkillObjectComparer();

		// Token: 0x040000BA RID: 186
		public static readonly CampaignUIHelper.CharacterAttributeComparer CharacterAttributeComparerInstance = new CampaignUIHelper.CharacterAttributeComparer();

		// Token: 0x040000BB RID: 187
		private static readonly List<ItemObject.ItemTypeEnum> _itemObjectTypeSortIndices = new List<ItemObject.ItemTypeEnum>
		{
			ItemObject.ItemTypeEnum.Horse,
			ItemObject.ItemTypeEnum.OneHandedWeapon,
			ItemObject.ItemTypeEnum.TwoHandedWeapon,
			ItemObject.ItemTypeEnum.Polearm,
			ItemObject.ItemTypeEnum.Shield,
			ItemObject.ItemTypeEnum.Bow,
			ItemObject.ItemTypeEnum.Arrows,
			ItemObject.ItemTypeEnum.Crossbow,
			ItemObject.ItemTypeEnum.Bolts,
			ItemObject.ItemTypeEnum.Sling,
			ItemObject.ItemTypeEnum.SlingStones,
			ItemObject.ItemTypeEnum.Thrown,
			ItemObject.ItemTypeEnum.Pistol,
			ItemObject.ItemTypeEnum.Musket,
			ItemObject.ItemTypeEnum.Bullets,
			ItemObject.ItemTypeEnum.Goods,
			ItemObject.ItemTypeEnum.HeadArmor,
			ItemObject.ItemTypeEnum.Cape,
			ItemObject.ItemTypeEnum.BodyArmor,
			ItemObject.ItemTypeEnum.ChestArmor,
			ItemObject.ItemTypeEnum.HandArmor,
			ItemObject.ItemTypeEnum.LegArmor,
			ItemObject.ItemTypeEnum.Invalid,
			ItemObject.ItemTypeEnum.Animal,
			ItemObject.ItemTypeEnum.Book,
			ItemObject.ItemTypeEnum.HorseHarness,
			ItemObject.ItemTypeEnum.Banner
		};

		// Token: 0x040000BC RID: 188
		private static readonly List<string> _attributeSortIndices = new List<string> { "Vigor", "Control", "Endurance", "Cunning", "Social", "Intelligence" };

		// Token: 0x040000BD RID: 189
		private static readonly List<string> _skillSortIndices = new List<string>
		{
			"OneHanded", "TwoHanded", "Polearm", "Bow", "Crossbow", "Throwing", "Riding", "Athletics", "Crafting", "Scouting",
			"Tactics", "Roguery", "Charm", "Leadership", "Trade", "Steward", "Medicine", "Engineering", "Mariner", "Boatswain",
			"Shipmaster"
		};

		// Token: 0x040000BE RID: 190
		private static readonly List<string> _navalSkills = new List<string> { "Mariner", "Boatswain", "Shipmaster" };

		// Token: 0x0200016A RID: 362
		[Flags]
		public enum IssueQuestFlags
		{
			// Token: 0x04000FED RID: 4077
			None = 0,
			// Token: 0x04000FEE RID: 4078
			AvailableIssue = 1,
			// Token: 0x04000FEF RID: 4079
			ActiveIssue = 2,
			// Token: 0x04000FF0 RID: 4080
			ActiveStoryQuest = 4,
			// Token: 0x04000FF1 RID: 4081
			TrackedIssue = 8,
			// Token: 0x04000FF2 RID: 4082
			TrackedStoryQuest = 16
		}

		// Token: 0x0200016B RID: 363
		public enum SortState
		{
			// Token: 0x04000FF4 RID: 4084
			Default,
			// Token: 0x04000FF5 RID: 4085
			Ascending,
			// Token: 0x04000FF6 RID: 4086
			Descending
		}

		// Token: 0x0200016C RID: 364
		public class CharacterAttributeComparer : IComparer<CharacterAttribute>
		{
			// Token: 0x0600220A RID: 8714 RVA: 0x0007AE78 File Offset: 0x00079078
			public int Compare(CharacterAttribute x, CharacterAttribute y)
			{
				int attributeTypeSortIndex = CampaignUIHelper.GetAttributeTypeSortIndex(x);
				int num = CampaignUIHelper.GetAttributeTypeSortIndex(y).CompareTo(attributeTypeSortIndex);
				if (num != 0)
				{
					return num;
				}
				return this.ResolveEquality(x, y);
			}

			// Token: 0x0600220B RID: 8715 RVA: 0x0007AEA9 File Offset: 0x000790A9
			private int ResolveEquality(CharacterAttribute x, CharacterAttribute y)
			{
				return x.StringId.CompareTo(y.StringId);
			}
		}

		// Token: 0x0200016D RID: 365
		public class SkillObjectComparer : IComparer<SkillObject>
		{
			// Token: 0x0600220D RID: 8717 RVA: 0x0007AEC4 File Offset: 0x000790C4
			public int Compare(SkillObject x, SkillObject y)
			{
				int skillObjectTypeSortIndex = CampaignUIHelper.GetSkillObjectTypeSortIndex(x);
				int num = CampaignUIHelper.GetSkillObjectTypeSortIndex(y).CompareTo(skillObjectTypeSortIndex);
				if (num != 0)
				{
					return num;
				}
				return this.ResolveEquality(x, y);
			}

			// Token: 0x0600220E RID: 8718 RVA: 0x0007AEF5 File Offset: 0x000790F5
			private int ResolveEquality(SkillObject x, SkillObject y)
			{
				return x.StringId.CompareTo(y.StringId);
			}
		}

		// Token: 0x0200016E RID: 366
		public class MobilePartyPrecedenceComparer : IComparer<MobileParty>
		{
			// Token: 0x06002210 RID: 8720 RVA: 0x0007AF10 File Offset: 0x00079110
			public int Compare(MobileParty x, MobileParty y)
			{
				if (x.IsGarrison && !y.IsGarrison)
				{
					return -1;
				}
				if (x.IsGarrison && y.IsGarrison)
				{
					return -x.Party.CalculateCurrentStrength().CompareTo(y.Party.CalculateCurrentStrength());
				}
				if (x.IsMilitia && y.IsGarrison)
				{
					return 1;
				}
				if (x.IsMilitia && !y.IsGarrison && !y.IsMilitia)
				{
					return -1;
				}
				if (x.IsMilitia && y.IsMilitia)
				{
					return -x.Party.CalculateCurrentStrength().CompareTo(y.Party.CalculateCurrentStrength());
				}
				if (x.LeaderHero != null && (y.IsGarrison || y.IsMilitia))
				{
					return 1;
				}
				if (x.LeaderHero != null && y.LeaderHero == null)
				{
					return -1;
				}
				if (x.LeaderHero != null && y.LeaderHero != null)
				{
					return -x.Party.CalculateCurrentStrength().CompareTo(y.Party.CalculateCurrentStrength());
				}
				if (x.LeaderHero == null && (y.IsGarrison || y.IsMilitia || y.LeaderHero != null))
				{
					return 1;
				}
				if (x.LeaderHero == null)
				{
					Hero leaderHero = y.LeaderHero;
					return -x.Party.CalculateCurrentStrength().CompareTo(y.Party.CalculateCurrentStrength());
				}
				return -x.Party.CalculateCurrentStrength().CompareTo(y.Party.CalculateCurrentStrength());
			}
		}

		// Token: 0x0200016F RID: 367
		public class ProductInputOutputEqualityComparer : IEqualityComparer<ValueTuple<ItemCategory, int>>
		{
			// Token: 0x06002212 RID: 8722 RVA: 0x0007B08F File Offset: 0x0007928F
			public bool Equals(ValueTuple<ItemCategory, int> x, ValueTuple<ItemCategory, int> y)
			{
				return x.Item1 == y.Item1;
			}

			// Token: 0x06002213 RID: 8723 RVA: 0x0007B09F File Offset: 0x0007929F
			public int GetHashCode(ValueTuple<ItemCategory, int> obj)
			{
				return obj.Item1.GetHashCode();
			}
		}
	}
}
