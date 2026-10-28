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

namespace SandBox.ViewModelCollection;

public static class SandBoxUIHelper
{
	public enum SortState
	{
		Default,
		Ascending,
		Descending
	}

	public enum MapEventVisualTypes
	{
		None,
		Raid,
		Siege,
		Battle,
		Rebellion,
		SallyOut
	}

	public const float AgentMarkerWorldHeightOffset = 0.35f;

	private static readonly TextObject _soldStr = new TextObject("{=YgyHVu8S}Sold{ITEMS}");

	private static readonly TextObject _purchasedStr = new TextObject("{=qIeDZoSx}Purchased{ITEMS}");

	private static readonly TextObject _itemTransactionStr = new TextObject("{=CqAhj27p} {ITEM_NAME} x{ITEM_NUMBER}");

	private static readonly TextObject _lootStr = new TextObject("{=nvemmBZz}You earned {AMOUNT}% of the loot and prisoners");

	private static void TooltipAddExplanation(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
	{
		List<(string, float)> lines = explainedNumber.GetLines();
		if (lines.Count > 0)
		{
			for (int i = 0; i < lines.Count; i++)
			{
				(string, float) tuple = lines[i];
				string item = tuple.Item1;
				string changeValueString = GetChangeValueString(tuple.Item2);
				properties.Add(new TooltipProperty(item, changeValueString, 0));
			}
		}
	}

	public static List<TooltipProperty> GetExplainedNumberTooltip(ref ExplainedNumber explanation)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		TooltipAddExplanation(list, ref explanation);
		return list;
	}

	public static List<TooltipProperty> GetBattleLootAwardTooltip(float lootPercentage)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		GameTexts.SetVariable("AMOUNT", lootPercentage);
		list.Add(new TooltipProperty(string.Empty, _lootStr.ToString(), 0));
		return list;
	}

	public static List<TooltipProperty> GetFigureheadTooltip(Figurehead figurehead)
	{
		object[] invokedArgs = new object[1] { figurehead };
		return new PropertyBasedTooltipVM(typeof(Figurehead), invokedArgs).TooltipPropertyList.ToList();
	}

	public static string GetSkillEffectText(SkillEffect effect, int skillLevel)
	{
		TextObject effectDescriptionForSkillLevel = SkillHelper.GetEffectDescriptionForSkillLevel(effect, skillLevel);
		if (effect.Role != PartyRole.None)
		{
			TextObject textObject = GameTexts.FindText("role", effect.Role.ToString());
			return $"({textObject.ToString()}) {effectDescriptionForSkillLevel} ";
		}
		return effectDescriptionForSkillLevel.ToString();
	}

	public static string GetRecruitNotificationText(int recruitmentAmount)
	{
		TextObject textObject = GameTexts.FindText("str_settlement_recruit_notification");
		MBTextManager.SetTextVariable("RECRUIT_AMOUNT", recruitmentAmount);
		MBTextManager.SetTextVariable("ISPLURAL", recruitmentAmount > 1);
		return textObject.ToString();
	}

	public static string GetItemSoldNotificationText(ItemRosterElement item, int itemAmount, bool fromHeroToSettlement)
	{
		string text = item.EquipmentElement.Item.ItemCategory.GetName().ToString();
		TextObject textObject = GameTexts.FindText("str_settlement_item_sold_notification");
		MBTextManager.SetTextVariable("IS_POSITIVE", !fromHeroToSettlement);
		MBTextManager.SetTextVariable("ITEM_AMOUNT", itemAmount);
		MBTextManager.SetTextVariable("ITEM_TYPE", text);
		return textObject.ToString();
	}

	public static string GetShipSoldNotificationText(Ship ship, int itemAmount, bool fromHeroToSettlement)
	{
		string variable = ship.ShipHull.Name.ToString();
		TextObject textObject = ((!fromHeroToSettlement) ? _purchasedStr : _soldStr);
		TextObject itemTransactionStr = _itemTransactionStr;
		itemTransactionStr.SetTextVariable("ITEM_NAME", variable);
		itemTransactionStr.SetTextVariable("ITEM_NUMBER", itemAmount);
		textObject.SetTextVariable("ITEMS", itemTransactionStr.ToString());
		return textObject.ToString();
	}

	public static string GetTroopGivenToSettlementNotificationText(int givenAmount)
	{
		TextObject textObject = GameTexts.FindText("str_settlement_given_troop_notification");
		MBTextManager.SetTextVariable("TROOP_AMOUNT", givenAmount);
		MBTextManager.SetTextVariable("ISPLURAL", givenAmount > 1);
		return textObject.ToString();
	}

	internal static string GetItemsTradedNotificationText(List<(EquipmentElement, int)> items, bool isSelling)
	{
		TextObject textObject = ((!isSelling) ? _purchasedStr : _soldStr);
		List<IGrouping<ItemCategory, (EquipmentElement, int)>> list = (from i in items
			group i by i.Item1.Item.ItemCategory into i
			orderby i.Sum(((EquipmentElement, int) e) => e.Item2 * e.Item1.Item.Value)
			select i).ToList();
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "GetItemsTradedNotificationText");
		int num = TaleWorlds.Library.MathF.Min(3, list.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			IGrouping<ItemCategory, (EquipmentElement, int)> grouping = list[num2];
			int variable = TaleWorlds.Library.MathF.Abs(grouping.Sum(((EquipmentElement, int) x) => x.Item2));
			grouping.Key.GetName().ToString();
			_itemTransactionStr.SetTextVariable("ITEM_NAME", grouping.Key.GetName());
			_itemTransactionStr.SetTextVariable("ITEM_NUMBER", variable);
			mBStringBuilder.Append(_itemTransactionStr.ToString());
		}
		textObject.SetTextVariable("ITEMS", mBStringBuilder.ToStringAndRelease());
		return textObject.ToString();
	}

	public static List<TooltipProperty> GetSiegeEngineInProgressTooltip(SiegeEvent.SiegeEngineConstructionProgress engineInProgress)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (engineInProgress?.SiegeEngine != null)
		{
			int num = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(PlayerSiege.PlayerSide).SiegeEngines.DeployedSiegeEngines.Where((SiegeEvent.SiegeEngineConstructionProgress e) => !e.IsActive).ToList().IndexOf(engineInProgress);
			list = GetSiegeEngineTooltip(engineInProgress.SiegeEngine);
			if (engineInProgress.IsActive)
			{
				string content = ((int)(engineInProgress.Hitpoints / engineInProgress.MaxHitPoints * 100f)).ToString();
				GameTexts.SetVariable("NUMBER", content);
				GameTexts.SetVariable("STR1", GameTexts.FindText("str_NUMBER_percent"));
				GameTexts.SetVariable("LEFT", ((int)engineInProgress.Hitpoints).ToString());
				GameTexts.SetVariable("RIGHT", ((int)engineInProgress.MaxHitPoints).ToString());
				GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis"));
				list.Add(new TooltipProperty(GameTexts.FindText("str_hitpoints").ToString(), GameTexts.FindText("str_STR1_space_STR2").ToString(), 0));
			}
			else
			{
				string content2 = TaleWorlds.Library.MathF.Round((engineInProgress.IsBeingRedeployed ? engineInProgress.RedeploymentProgress : engineInProgress.Progress) / 1f * 100f).ToString();
				GameTexts.SetVariable("NUMBER", content2);
				TextObject textObject = (engineInProgress.IsBeingRedeployed ? GameTexts.FindText("str_redeploy") : GameTexts.FindText("str_inprogress"));
				list.Add(new TooltipProperty(textObject.ToString(), GameTexts.FindText("str_NUMBER_percent").ToString(), 0));
				if (num == 0)
				{
					list.Add(new TooltipProperty(GameTexts.FindText("str_currently_building").ToString(), " ", 0));
				}
				else if (num > 0)
				{
					list.Add(new TooltipProperty(GameTexts.FindText("str_in_queue").ToString(), num.ToString(), 0));
				}
			}
		}
		return list;
	}

	public static List<TooltipProperty> GetSiegeEngineTooltip(SiegeEngineType engine)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (PlayerSiege.PlayerSiegeEvent != null && engine != null)
		{
			list.Add(new TooltipProperty("", engine.Name.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
			list.Add(new TooltipProperty("", engine.Description.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine));
			list.Add(new TooltipProperty(new TextObject("{=Ahy035gM}Build Cost").ToString(), engine.ManDayCost.ToString("F1"), 0));
			float siegeEngineHitPoints = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineHitPoints(PlayerSiege.PlayerSiegeEvent, engine, PlayerSiege.PlayerSide);
			list.Add(new TooltipProperty(new TextObject("{=oBbiVeKE}Hit Points").ToString(), siegeEngineHitPoints.ToString(), 0));
			if (engine.Difficulty > 0)
			{
				list.Add(new TooltipProperty(new TextObject("{=raD9MK3O}Difficulty").ToString(), engine.Difficulty.ToString(), 0));
			}
			if (engine.ToolCost > 0)
			{
				list.Add(new TooltipProperty(new TextObject("{=lPMYSSAa}Tools Required").ToString(), engine.ToolCost.ToString(), 0));
			}
			if (engine.IsRanged)
			{
				list.Add(new TooltipProperty(GameTexts.FindText("str_daily_rate_of_fire").ToString(), engine.CampaignRateOfFirePerDay.ToString("F1"), 0));
				list.Add(new TooltipProperty(GameTexts.FindText("str_projectile_damage").ToString(), engine.Damage.ToString("F1"), 0));
				list.Add(new TooltipProperty(" ", " ", 0));
			}
		}
		return list;
	}

	public static List<TooltipProperty> GetWallSectionTooltip(Settlement settlement, int wallIndex)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (settlement.IsFortification)
		{
			list.Add(new TooltipProperty("", GameTexts.FindText("str_wall").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
			list.Add(new TooltipProperty(" ", " ", 0));
			float maxHitPointsOfOneWallSection = settlement.MaxHitPointsOfOneWallSection;
			float num = settlement.SettlementWallSectionHitPointsRatioList[wallIndex] * maxHitPointsOfOneWallSection;
			if (num > 0f)
			{
				string content = ((int)(num / maxHitPointsOfOneWallSection * 100f)).ToString();
				GameTexts.SetVariable("NUMBER", content);
				GameTexts.SetVariable("STR1", GameTexts.FindText("str_NUMBER_percent"));
				GameTexts.SetVariable("LEFT", ((int)num).ToString());
				GameTexts.SetVariable("RIGHT", ((int)maxHitPointsOfOneWallSection).ToString());
				GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis"));
				list.Add(new TooltipProperty(GameTexts.FindText("str_hitpoints").ToString(), GameTexts.FindText("str_STR1_space_STR2").ToString(), 0));
			}
			else
			{
				list.Add(new TooltipProperty(GameTexts.FindText("str_wall_breached").ToString(), " ", 0));
			}
		}
		return list;
	}

	public static string GetPrisonersSoldNotificationText(int soldPrisonerAmount)
	{
		TextObject textObject = GameTexts.FindText("str_settlement_prisoner_sold_notification");
		MBTextManager.SetTextVariable("PRISONERS_AMOUNT", soldPrisonerAmount);
		MBTextManager.SetTextVariable("ISPLURAL", soldPrisonerAmount > 1);
		return textObject.ToString();
	}

	public static int GetPartyHealthyCount(MobileParty party)
	{
		int num = party.Party.NumberOfHealthyMembers;
		if (party.Army != null && party.Army.LeaderParty == party)
		{
			foreach (MobileParty attachedParty in party.Army.LeaderParty.AttachedParties)
			{
				num += attachedParty.Party.NumberOfHealthyMembers;
			}
		}
		return num;
	}

	public static string GetPartyWoundedText(int woundedAmount)
	{
		TextObject textObject = new TextObject("{=O9nwLrYp}+{WOUNDEDAMOUNT}w");
		textObject.SetTextVariable("WOUNDEDAMOUNT", woundedAmount);
		return textObject.ToString();
	}

	public static string GetPartyPrisonerText(int prisonerAmount)
	{
		TextObject textObject = new TextObject("{=CiIWjF3f}+{PRISONERAMOUNT}p");
		textObject.SetTextVariable("PRISONERAMOUNT", prisonerAmount);
		return textObject.ToString();
	}

	public static int GetAllWoundedMembersAmount(MobileParty party)
	{
		int num = party.Party.NumberOfWoundedTotalMembers;
		if (party.Army != null && party.Army.LeaderParty == party)
		{
			num += party.Army.LeaderParty.AttachedParties.Sum((MobileParty p) => p.Party.NumberOfWoundedTotalMembers);
		}
		return num;
	}

	public static int GetAllPrisonerMembersAmount(MobileParty party)
	{
		int num = party.Party.NumberOfPrisoners;
		if (party.Army != null && party.Army.LeaderParty == party)
		{
			num += party.Army.LeaderParty.AttachedParties.Sum((MobileParty p) => p.Party.NumberOfPrisoners);
		}
		return num;
	}

	public static CharacterCode GetCharacterCode(CharacterObject character, bool useCivilian = false)
	{
		if (character.IsHero && IsHeroInformationHidden(character.HeroObject, out var _))
		{
			return CharacterCode.CreateEmpty();
		}
		if (character.IsHero && character.HeroObject.IsLord)
		{
			string[] array = CharacterCode.CreateFrom(character).Code.Split(new string[1] { "@---@" }, StringSplitOptions.RemoveEmptyEntries);
			Equipment equipment = ((useCivilian && character.FirstCivilianEquipment != null) ? character.FirstCivilianEquipment.Clone() : character.Equipment.Clone());
			equipment[EquipmentIndex.NumAllWeaponSlots] = new EquipmentElement(null);
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				ItemObject item = equipment[equipmentIndex].Item;
				if (item != null && item.WeaponComponent?.PrimaryWeapon?.IsShield == true)
				{
					equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, default(EquipmentElement));
				}
			}
			array[0] = equipment.CalculateEquipmentCode();
			return CharacterCode.CreateFrom(string.Join("@---@", array));
		}
		return CharacterCode.CreateFrom(character);
	}

	public static bool IsHeroInformationHidden(Hero hero, out TextObject disableReason)
	{
		bool flag = !Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(hero);
		disableReason = (flag ? new TextObject("{=akHsjtPh}You haven't met this hero yet.") : null);
		return flag;
	}

	public static MapEventVisualTypes GetMapEventVisualTypeFromMapEvent(MapEvent mapEvent)
	{
		if (mapEvent.MapEventSettlement != null)
		{
			if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside)
			{
				return MapEventVisualTypes.Siege;
			}
			if (mapEvent.IsSallyOut || mapEvent.IsBlockadeSallyOut)
			{
				return MapEventVisualTypes.SallyOut;
			}
			return MapEventVisualTypes.Raid;
		}
		return MapEventVisualTypes.Battle;
	}

	private static string GetChangeValueString(float value)
	{
		string text = value.ToString("0.##");
		if (value > 0.001f)
		{
			MBTextManager.SetTextVariable("NUMBER", text);
			return GameTexts.FindText("str_plus_with_number").ToString();
		}
		return text;
	}

	public static bool IsAgentInVisibilityRangeApproximate(Agent seerAgent, Agent seenAgent)
	{
		if (Mission.Current == null || seerAgent == null || seenAgent == null)
		{
			return false;
		}
		float num = seerAgent.Position.Distance(seenAgent.Position);
		return 250f / (16f + num * num) * 0.95f > 0.05f;
	}

	public static bool CanAgentBeAlarmed(Agent agent)
	{
		if (agent?.Team != null && agent.Team != Team.Invalid && !agent.Team.IsPlayerAlly && !agent.IsMainAgent)
		{
			return (agent.GetAgentFlags() & AgentFlag.CanGetAlarmed) == AgentFlag.CanGetAlarmed;
		}
		return false;
	}
}
