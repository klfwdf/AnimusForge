using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection
{
	// Token: 0x02000006 RID: 6
	public static class SandBoxUIHelper
	{
		// Token: 0x06000030 RID: 48 RVA: 0x000024B8 File Offset: 0x000006B8
		private static void TooltipAddExplanation(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
		{
			List<ValueTuple<string, float>> lines = explainedNumber.GetLines();
			if (lines.Count > 0)
			{
				for (int i = 0; i < lines.Count; i++)
				{
					ValueTuple<string, float> valueTuple = lines[i];
					string item = valueTuple.Item1;
					string changeValueString = SandBoxUIHelper.GetChangeValueString(valueTuple.Item2);
					properties.Add(new TooltipProperty(item, changeValueString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x0000250F File Offset: 0x0000070F
		public static List<TooltipProperty> GetExplainedNumberTooltip(ref ExplainedNumber explanation)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			SandBoxUIHelper.TooltipAddExplanation(list, ref explanation);
			return list;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x0000251D File Offset: 0x0000071D
		public static List<TooltipProperty> GetBattleLootAwardTooltip(float lootPercentage)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			GameTexts.SetVariable("AMOUNT", lootPercentage);
			list.Add(new TooltipProperty(string.Empty, SandBoxUIHelper._lootStr.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x0000254C File Offset: 0x0000074C
		public static List<TooltipProperty> GetFigureheadTooltip(Figurehead figurehead)
		{
			object[] invokedArgs = new object[] { figurehead };
			return new PropertyBasedTooltipVM(typeof(Figurehead), invokedArgs).TooltipPropertyList.ToList<TooltipProperty>();
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002580 File Offset: 0x00000780
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

		// Token: 0x06000035 RID: 53 RVA: 0x000025D4 File Offset: 0x000007D4
		public static string GetRecruitNotificationText(int recruitmentAmount)
		{
			object obj = GameTexts.FindText("str_settlement_recruit_notification", null);
			MBTextManager.SetTextVariable("RECRUIT_AMOUNT", recruitmentAmount);
			MBTextManager.SetTextVariable("ISPLURAL", recruitmentAmount > 1);
			return obj.ToString();
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002604 File Offset: 0x00000804
		public static string GetItemSoldNotificationText(ItemRosterElement item, int itemAmount, bool fromHeroToSettlement)
		{
			string text = item.EquipmentElement.Item.ItemCategory.GetName().ToString();
			object obj = GameTexts.FindText("str_settlement_item_sold_notification", null);
			MBTextManager.SetTextVariable("IS_POSITIVE", !fromHeroToSettlement);
			MBTextManager.SetTextVariable("ITEM_AMOUNT", itemAmount);
			MBTextManager.SetTextVariable("ITEM_TYPE", text, false);
			return obj.ToString();
		}

		// Token: 0x06000037 RID: 55 RVA: 0x0000266C File Offset: 0x0000086C
		public static string GetShipSoldNotificationText(Ship ship, int itemAmount, bool fromHeroToSettlement)
		{
			string variable = ship.ShipHull.Name.ToString();
			TextObject textObject;
			if (fromHeroToSettlement)
			{
				textObject = SandBoxUIHelper._soldStr;
			}
			else
			{
				textObject = SandBoxUIHelper._purchasedStr;
			}
			TextObject itemTransactionStr = SandBoxUIHelper._itemTransactionStr;
			itemTransactionStr.SetTextVariable("ITEM_NAME", variable);
			itemTransactionStr.SetTextVariable("ITEM_NUMBER", itemAmount);
			textObject.SetTextVariable("ITEMS", itemTransactionStr.ToString());
			return textObject.ToString();
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000026D3 File Offset: 0x000008D3
		public static string GetTroopGivenToSettlementNotificationText(int givenAmount)
		{
			object obj = GameTexts.FindText("str_settlement_given_troop_notification", null);
			MBTextManager.SetTextVariable("TROOP_AMOUNT", givenAmount);
			MBTextManager.SetTextVariable("ISPLURAL", givenAmount > 1);
			return obj.ToString();
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002704 File Offset: 0x00000904
		internal static string GetItemsTradedNotificationText(List<ValueTuple<EquipmentElement, int>> items, bool isSelling)
		{
			TextObject textObject;
			if (isSelling)
			{
				textObject = SandBoxUIHelper._soldStr;
			}
			else
			{
				textObject = SandBoxUIHelper._purchasedStr;
			}
			List<IGrouping<ItemCategory, ValueTuple<EquipmentElement, int>>> list = (from i in items
				group i by i.Item1.Item.ItemCategory into i
				orderby i.Sum((ValueTuple<EquipmentElement, int> e) => e.Item2 * e.Item1.Item.Value)
				select i).ToList<IGrouping<ItemCategory, ValueTuple<EquipmentElement, int>>>();
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, "GetItemsTradedNotificationText");
			int num = MathF.Min(3, list.Count);
			for (int j = 0; j < num; j++)
			{
				IGrouping<ItemCategory, ValueTuple<EquipmentElement, int>> grouping = list[j];
				int variable = MathF.Abs(grouping.Sum((ValueTuple<EquipmentElement, int> x) => x.Item2));
				grouping.Key.GetName().ToString();
				SandBoxUIHelper._itemTransactionStr.SetTextVariable("ITEM_NAME", grouping.Key.GetName());
				SandBoxUIHelper._itemTransactionStr.SetTextVariable("ITEM_NUMBER", variable);
				mbstringBuilder.Append<string>(SandBoxUIHelper._itemTransactionStr.ToString());
			}
			textObject.SetTextVariable("ITEMS", mbstringBuilder.ToStringAndRelease());
			return textObject.ToString();
		}

		// Token: 0x0600003A RID: 58 RVA: 0x0000284C File Offset: 0x00000A4C
		public static List<TooltipProperty> GetSiegeEngineInProgressTooltip(SiegeEvent.SiegeEngineConstructionProgress engineInProgress)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (((engineInProgress != null) ? engineInProgress.SiegeEngine : null) != null)
			{
				int num = (from e in PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(PlayerSiege.PlayerSide).SiegeEngines.DeployedSiegeEngines
					where !e.IsActive
					select e).ToList<SiegeEvent.SiegeEngineConstructionProgress>().IndexOf(engineInProgress);
				list = SandBoxUIHelper.GetSiegeEngineTooltip(engineInProgress.SiegeEngine);
				if (engineInProgress.IsActive)
				{
					string content = ((int)(engineInProgress.Hitpoints / engineInProgress.MaxHitPoints * 100f)).ToString();
					GameTexts.SetVariable("NUMBER", content);
					GameTexts.SetVariable("STR1", GameTexts.FindText("str_NUMBER_percent", null));
					GameTexts.SetVariable("LEFT", ((int)engineInProgress.Hitpoints).ToString());
					GameTexts.SetVariable("RIGHT", ((int)engineInProgress.MaxHitPoints).ToString());
					GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis", null));
					list.Add(new TooltipProperty(GameTexts.FindText("str_hitpoints", null).ToString(), GameTexts.FindText("str_STR1_space_STR2", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				else
				{
					string content2 = MathF.Round((engineInProgress.IsBeingRedeployed ? engineInProgress.RedeploymentProgress : engineInProgress.Progress) / 1f * 100f).ToString();
					GameTexts.SetVariable("NUMBER", content2);
					TextObject textObject = (engineInProgress.IsBeingRedeployed ? GameTexts.FindText("str_redeploy", null) : GameTexts.FindText("str_inprogress", null));
					list.Add(new TooltipProperty(textObject.ToString(), GameTexts.FindText("str_NUMBER_percent", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					if (num == 0)
					{
						list.Add(new TooltipProperty(GameTexts.FindText("str_currently_building", null).ToString(), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
					else if (num > 0)
					{
						list.Add(new TooltipProperty(GameTexts.FindText("str_in_queue", null).ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
				}
			}
			return list;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002A60 File Offset: 0x00000C60
		public static List<TooltipProperty> GetSiegeEngineTooltip(SiegeEngineType engine)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (PlayerSiege.PlayerSiegeEvent != null && engine != null)
			{
				list.Add(new TooltipProperty("", engine.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
				list.Add(new TooltipProperty("", engine.Description.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
				list.Add(new TooltipProperty(new TextObject("{=Ahy035gM}Build Cost", null).ToString(), engine.ManDayCost.ToString("F1"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				float siegeEngineHitPoints = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineHitPoints(PlayerSiege.PlayerSiegeEvent, engine, PlayerSiege.PlayerSide);
				list.Add(new TooltipProperty(new TextObject("{=oBbiVeKE}Hit Points", null).ToString(), siegeEngineHitPoints.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				if (engine.Difficulty > 0)
				{
					list.Add(new TooltipProperty(new TextObject("{=raD9MK3O}Difficulty", null).ToString(), engine.Difficulty.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				if (engine.ToolCost > 0)
				{
					list.Add(new TooltipProperty(new TextObject("{=lPMYSSAa}Tools Required", null).ToString(), engine.ToolCost.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				if (engine.IsRanged)
				{
					list.Add(new TooltipProperty(GameTexts.FindText("str_daily_rate_of_fire", null).ToString(), engine.CampaignRateOfFirePerDay.ToString("F1"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					list.Add(new TooltipProperty(GameTexts.FindText("str_projectile_damage", null).ToString(), engine.Damage.ToString("F1"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					list.Add(new TooltipProperty(" ", " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002C28 File Offset: 0x00000E28
		public static List<TooltipProperty> GetWallSectionTooltip(Settlement settlement, int wallIndex)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			if (settlement.IsFortification)
			{
				list.Add(new TooltipProperty("", GameTexts.FindText("str_wall", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
				list.Add(new TooltipProperty(" ", " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				float maxHitPointsOfOneWallSection = settlement.MaxHitPointsOfOneWallSection;
				float num = settlement.SettlementWallSectionHitPointsRatioList[wallIndex] * maxHitPointsOfOneWallSection;
				if (num > 0f)
				{
					string content = ((int)(num / maxHitPointsOfOneWallSection * 100f)).ToString();
					GameTexts.SetVariable("NUMBER", content);
					GameTexts.SetVariable("STR1", GameTexts.FindText("str_NUMBER_percent", null));
					GameTexts.SetVariable("LEFT", ((int)num).ToString());
					GameTexts.SetVariable("RIGHT", ((int)maxHitPointsOfOneWallSection).ToString());
					GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis", null));
					list.Add(new TooltipProperty(GameTexts.FindText("str_hitpoints", null).ToString(), GameTexts.FindText("str_STR1_space_STR2", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				else
				{
					list.Add(new TooltipProperty(GameTexts.FindText("str_wall_breached", null).ToString(), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002D6D File Offset: 0x00000F6D
		public static string GetPrisonersSoldNotificationText(int soldPrisonerAmount)
		{
			object obj = GameTexts.FindText("str_settlement_prisoner_sold_notification", null);
			MBTextManager.SetTextVariable("PRISONERS_AMOUNT", soldPrisonerAmount);
			MBTextManager.SetTextVariable("ISPLURAL", soldPrisonerAmount > 1);
			return obj.ToString();
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002DA0 File Offset: 0x00000FA0
		public static int GetPartyHealthyCount(MobileParty party)
		{
			int num = party.Party.NumberOfHealthyMembers;
			if (party.Army != null && party.Army.LeaderParty == party)
			{
				foreach (MobileParty mobileParty in party.Army.LeaderParty.AttachedParties)
				{
					num += mobileParty.Party.NumberOfHealthyMembers;
				}
			}
			return num;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002E28 File Offset: 0x00001028
		public static string GetPartyWoundedText(int woundedAmount)
		{
			TextObject textObject = new TextObject("{=O9nwLrYp}+{WOUNDEDAMOUNT}w", null);
			textObject.SetTextVariable("WOUNDEDAMOUNT", woundedAmount);
			return textObject.ToString();
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002E47 File Offset: 0x00001047
		public static string GetPartyPrisonerText(int prisonerAmount)
		{
			TextObject textObject = new TextObject("{=CiIWjF3f}+{PRISONERAMOUNT}p", null);
			textObject.SetTextVariable("PRISONERAMOUNT", prisonerAmount);
			return textObject.ToString();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002E68 File Offset: 0x00001068
		public static int GetAllWoundedMembersAmount(MobileParty party)
		{
			int num = party.Party.NumberOfWoundedTotalMembers;
			if (party.Army != null && party.Army.LeaderParty == party)
			{
				num += party.Army.LeaderParty.AttachedParties.Sum((MobileParty p) => p.Party.NumberOfWoundedTotalMembers);
			}
			return num;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002ED0 File Offset: 0x000010D0
		public static int GetAllPrisonerMembersAmount(MobileParty party)
		{
			int num = party.Party.NumberOfPrisoners;
			if (party.Army != null && party.Army.LeaderParty == party)
			{
				num += party.Army.LeaderParty.AttachedParties.Sum((MobileParty p) => p.Party.NumberOfPrisoners);
			}
			return num;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002F38 File Offset: 0x00001138
		public static CharacterCode GetCharacterCode(CharacterObject character, bool useCivilian = false)
		{
			TextObject textObject;
			if (character.IsHero && SandBoxUIHelper.IsHeroInformationHidden(character.HeroObject, out textObject))
			{
				return CharacterCode.CreateEmpty();
			}
			if (character.IsHero && character.HeroObject.IsLord)
			{
				string[] array = CharacterCode.CreateFrom(character).Code.Split(new string[] { "@---@" }, StringSplitOptions.RemoveEmptyEntries);
				Equipment equipment = ((useCivilian && character.FirstCivilianEquipment != null) ? character.FirstCivilianEquipment.Clone(false) : character.Equipment.Clone(false));
				equipment[EquipmentIndex.NumAllWeaponSlots] = new EquipmentElement(null, null, null, false);
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
				{
					ItemObject item = equipment[equipmentIndex].Item;
					bool flag;
					if (item == null)
					{
						flag = false;
					}
					else
					{
						WeaponComponent weaponComponent = item.WeaponComponent;
						bool? flag2;
						if (weaponComponent == null)
						{
							flag2 = null;
						}
						else
						{
							WeaponComponentData primaryWeapon = weaponComponent.PrimaryWeapon;
							flag2 = ((primaryWeapon != null) ? new bool?(primaryWeapon.IsShield) : null);
						}
						bool? flag3 = flag2;
						bool flag4 = true;
						flag = (flag3.GetValueOrDefault() == flag4) & (flag3 != null);
					}
					if (flag)
					{
						equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, default(EquipmentElement));
					}
				}
				array[0] = equipment.CalculateEquipmentCode();
				return CharacterCode.CreateFrom(string.Join("@---@", array));
			}
			return CharacterCode.CreateFrom(character);
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003078 File Offset: 0x00001278
		public static bool IsHeroInformationHidden(Hero hero, out TextObject disableReason)
		{
			bool flag = !Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(hero);
			disableReason = (flag ? new TextObject("{=akHsjtPh}You haven't met this hero yet.", null) : null);
			return flag;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000030B2 File Offset: 0x000012B2
		public static SandBoxUIHelper.MapEventVisualTypes GetMapEventVisualTypeFromMapEvent(MapEvent mapEvent)
		{
			if (mapEvent.MapEventSettlement == null)
			{
				return SandBoxUIHelper.MapEventVisualTypes.Battle;
			}
			if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside)
			{
				return SandBoxUIHelper.MapEventVisualTypes.Siege;
			}
			if (mapEvent.IsSallyOut || mapEvent.IsBlockadeSallyOut)
			{
				return SandBoxUIHelper.MapEventVisualTypes.SallyOut;
			}
			return SandBoxUIHelper.MapEventVisualTypes.Raid;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000030E4 File Offset: 0x000012E4
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

		// Token: 0x06000047 RID: 71 RVA: 0x00003124 File Offset: 0x00001324
		public static bool IsAgentInVisibilityRangeApproximate(Agent seerAgent, Agent seenAgent)
		{
			if (Mission.Current == null || seerAgent == null || seenAgent == null)
			{
				return false;
			}
			float num = seerAgent.Position.Distance(seenAgent.Position);
			return 250f / (16f + num * num) * 0.95f > 0.05f;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003174 File Offset: 0x00001374
		public static bool CanAgentBeAlarmed(Agent agent)
		{
			return ((agent != null) ? agent.Team : null) != null && agent.Team != Team.Invalid && !agent.Team.IsPlayerAlly && !agent.IsMainAgent && (agent.GetAgentFlags() & AgentFlag.CanGetAlarmed) == AgentFlag.CanGetAlarmed;
		}

		// Token: 0x04000017 RID: 23
		public const float AgentMarkerWorldHeightOffset = 0.35f;

		// Token: 0x04000018 RID: 24
		private static readonly TextObject _soldStr = new TextObject("{=YgyHVu8S}Sold{ITEMS}", null);

		// Token: 0x04000019 RID: 25
		private static readonly TextObject _purchasedStr = new TextObject("{=qIeDZoSx}Purchased{ITEMS}", null);

		// Token: 0x0400001A RID: 26
		private static readonly TextObject _itemTransactionStr = new TextObject("{=CqAhj27p} {ITEM_NAME} x{ITEM_NUMBER}", null);

		// Token: 0x0400001B RID: 27
		private static readonly TextObject _lootStr = new TextObject("{=nvemmBZz}You earned {AMOUNT}% of the loot and prisoners", null);

		// Token: 0x02000062 RID: 98
		public enum SortState
		{
			// Token: 0x040002E9 RID: 745
			Default,
			// Token: 0x040002EA RID: 746
			Ascending,
			// Token: 0x040002EB RID: 747
			Descending
		}

		// Token: 0x02000063 RID: 99
		public enum MapEventVisualTypes
		{
			// Token: 0x040002ED RID: 749
			None,
			// Token: 0x040002EE RID: 750
			Raid,
			// Token: 0x040002EF RID: 751
			Siege,
			// Token: 0x040002F0 RID: 752
			Battle,
			// Token: 0x040002F1 RID: 753
			Rebellion,
			// Token: 0x040002F2 RID: 754
			SallyOut
		}
	}
}
