using System;
using System.Collections.Generic;
using System.Linq;
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

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public static class CampaignUIHelper
{
	[Flags]
	public enum IssueQuestFlags
	{
		None = 0,
		AvailableIssue = 1,
		ActiveIssue = 2,
		ActiveStoryQuest = 4,
		TrackedIssue = 8,
		TrackedStoryQuest = 0x10
	}

	public enum SortState
	{
		Default,
		Ascending,
		Descending
	}

	public class CharacterAttributeComparer : IComparer<CharacterAttribute>
	{
		public int Compare(CharacterAttribute x, CharacterAttribute y)
		{
			int attributeTypeSortIndex = GetAttributeTypeSortIndex(x);
			int num = GetAttributeTypeSortIndex(y).CompareTo(attributeTypeSortIndex);
			if (num != 0)
			{
				return num;
			}
			return ResolveEquality(x, y);
		}

		private int ResolveEquality(CharacterAttribute x, CharacterAttribute y)
		{
			return x.StringId.CompareTo(y.StringId);
		}
	}

	public class SkillObjectComparer : IComparer<SkillObject>
	{
		public int Compare(SkillObject x, SkillObject y)
		{
			int skillObjectTypeSortIndex = GetSkillObjectTypeSortIndex(x);
			int num = GetSkillObjectTypeSortIndex(y).CompareTo(skillObjectTypeSortIndex);
			if (num != 0)
			{
				return num;
			}
			return ResolveEquality(x, y);
		}

		private int ResolveEquality(SkillObject x, SkillObject y)
		{
			return x.StringId.CompareTo(y.StringId);
		}
	}

	public class MobilePartyPrecedenceComparer : IComparer<MobileParty>
	{
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
				_ = y.LeaderHero;
				return -x.Party.CalculateCurrentStrength().CompareTo(y.Party.CalculateCurrentStrength());
			}
			return -x.Party.CalculateCurrentStrength().CompareTo(y.Party.CalculateCurrentStrength());
		}
	}

	public class ProductInputOutputEqualityComparer : IEqualityComparer<(ItemCategory, int)>
	{
		public bool Equals((ItemCategory, int) x, (ItemCategory, int) y)
		{
			return x.Item1 == y.Item1;
		}

		public int GetHashCode((ItemCategory, int) obj)
		{
			return obj.Item1.GetHashCode();
		}
	}

	public static readonly IssueQuestFlags[] IssueQuestFlagsValues = (IssueQuestFlags[])Enum.GetValues(typeof(IssueQuestFlags));

	private static readonly TextObject _changeStr = new TextObject("{=R2AaCaPJ}Expected Change");

	private static readonly TextObject _totalStr = new TextObject("{=kWVbHPtT}Total");

	private static readonly TextObject _noChangesStr = new TextObject("{=XIioBPi0}No changes");

	private static readonly TextObject _hitPointsStr = new TextObject("{=oBbiVeKE}Hit Points");

	private static readonly TextObject _maxhitPointsStr = new TextObject("{=mDFhzEMC}Max. Hit Points");

	private static readonly TextObject _prosperityStr = new TextObject("{=IagYTD5O}Prosperity");

	private static readonly TextObject _hearthStr = new TextObject("{=2GWR9Cba}Hearth");

	private static readonly TextObject _dailyProductionStr = new TextObject("{=94aHU6nD}Construction");

	private static readonly TextObject _securityStr = new TextObject("{=MqCH7R4A}Security");

	private static readonly TextObject _criminalRatingStr = new TextObject("{=r0WIRUHo}Criminal Rating");

	private static readonly TextObject _militiaStr = new TextObject("{=gsVtO9A7}Militia");

	private static readonly TextObject _foodStr = new TextObject("{=qSi4DlT4}Food");

	private static readonly TextObject _foodItemsStr = new TextObject("{=IQY9yykn}Food Items");

	private static readonly TextObject _livestockStr = new TextObject("{=UI0q8rWw}Livestock");

	private static readonly TextObject _armyCohesionStr = new TextObject("{=iZ3w6opW}Cohesion");

	private static readonly TextObject _loyaltyStr = new TextObject("{=YO0x7ZAo}Loyalty");

	private static readonly TextObject _wallsStr = new TextObject("{=LsZEdD2z}Walls");

	private static readonly TextObject _plusStr = new TextObject("{=eTw2aNV5}+");

	private static readonly TextObject _heroesHealingRateStr = new TextObject("{=HHTQVp52}Heroes Healing Rate");

	private static readonly TextObject _numTotalTroopsInTheArmyStr = new TextObject("{=DRJOxrRF}Troops in Army");

	private static readonly TextObject _garrisonStr = new TextObject("{=jlgjLDo7}Garrison");

	private static readonly TextObject _hitPoints = new TextObject("{=UbZL2BJQ}Hitpoints");

	private static readonly TextObject _maxhitPoints = new TextObject("{=KTTyBbsp}Max HP");

	private static readonly TextObject _goldStr = new TextObject("{=Hxf6bzmR}Current Denars");

	private static readonly TextObject _resultGold = new TextObject("{=NC9bbrt5}End-of-day denars");

	private static readonly TextObject _influenceStr = new TextObject("{=RVPidk5a}Influence");

	private static readonly TextObject _partyMoraleStr = GameTexts.FindText("str_party_morale");

	private static readonly TextObject _partyFoodStr = new TextObject("{=mg7id9om}Number of Consumable Items");

	private static readonly TextObject _partySpeedStr = new TextObject("{=zWaVxD6T}Party Speed");

	private static readonly TextObject _partySizeLimitStr = new TextObject("{=mp68RYnD}Party Size Limit");

	private static readonly TextObject _viewDistanceFoodStr = new TextObject("{=hTzTMLsf}View Distance");

	private static readonly TextObject _battleReadyTroopsStr = new TextObject("{=LVmkE2Ow}Battle Ready Troops");

	private static readonly TextObject _patrolStr = new TextObject("{=townPatrol}Patrol");

	private static readonly TextObject _woundedTroopsStr = new TextObject("{=TzLtVzdg}Wounded Troops");

	private static readonly TextObject _prisonersStr = new TextObject("{=N6QTvjMf}Prisoners");

	private static readonly TextObject _regularsHealingRateStr = new TextObject("{=tf7301NC}Healing Rate");

	private static readonly TextObject _learningRateStr = new TextObject("{=q1J4a8rr}Learning Rate");

	private static readonly TextObject _learningLimitStr = new TextObject("{=YT9giTet}Learning Limit");

	private static readonly TextObject _partyInventoryCapacityStr = new TextObject("{=fI7a7RoE}Inventory Capacity");

	private static readonly TextObject _partyInventoryCargoCapacityStr = new TextObject("{=fI7a7RoE}Cargo Capacity");

	private static readonly TextObject _partyInventoryLandCapacityStr = new TextObject("{=cBqjZjfJ}Inventory Capacity on Land");

	private static readonly TextObject _partyInventorySeaCapacityStr = new TextObject("{=aAqMSU2d}Cargo Capacity at Sea");

	private static readonly TextObject _partyInventoryWeightStr = new TextObject("{=4Dd2xgPm}Weight");

	private static readonly TextObject _partyInventoryCargoStr = new TextObject("{=cblOOivk}Cargo");

	private static readonly TextObject _partyInventoryLandWeightStr = new TextObject("{=8d23bRmv}Weight on Land");

	private static readonly TextObject _partyInventorySeaWeightStr = new TextObject("{=Tc5y7Tgd}Cargo at Sea");

	private static readonly TextObject _partyTroopSizeLimitStr = new TextObject("{=2Cq3tViJ}Party Troop Size Limit");

	private static readonly TextObject _partyPrisonerSizeLimitStr = new TextObject("{=UHLcmf9A}Party Prisoner Size Limit");

	private static readonly TextObject _inventorySkillTooltipTitle = new TextObject("{=Y7qbwrWE}{HERO_NAME}'s Skills");

	private static readonly TextObject _mercenaryClanInfluenceStr = new TextObject("{=GP3jpU0X}Influence is periodically converted to denars for mercenary clans.");

	private static readonly TextObject _orderRequirementText = new TextObject("{=dVqowrRz} - {STAT} {REQUIREMENT}");

	private static readonly TextObject _denarValueInfoText = new TextObject("{=mapbardenarvalue}{DENAR_AMOUNT}{VALUE_ABBREVIATION}");

	private static readonly TextObject _prisonerOfText = new TextObject("{=a8nRxITn}Prisoner of {PARTY_NAME}");

	private static readonly TextObject _attachedToText = new TextObject("{=8Jy9DnKk}Attached to {PARTY_NAME}");

	private static readonly TextObject _inYourPartyText = new TextObject("{=CRi905Ao}In your party");

	private static readonly TextObject _travelingText = new TextObject("{=vdKiLwaf}Traveling");

	private static readonly TextObject _recoveringText = new TextObject("{=heroRecovering}Recovering");

	private static readonly TextObject _recentlyReleasedText = new TextObject("{=NLFeyz7m}Recently Released From Captivity");

	private static readonly TextObject _recentlyEscapedText = new TextObject("{=84oSzquz}Recently Escaped Captivity");

	private static readonly TextObject _nearSettlementText = new TextObject("{=XjT8S4ng}Near {SETTLEMENT_NAME}");

	private static readonly TextObject _noDelayText = new TextObject("{=bDwTWrru}No delay");

	private static readonly TextObject _regroupingText = new TextObject("{=KxLoeSEO}Regrouping");

	private static readonly TextObject _partyRolesText = new TextObject("{=RrY2qMan}Party Roles");

	private static readonly TextObject _governerEffectsText = new TextObject("{=J8ddrAOf}Governor Effects");

	public static readonly MobilePartyPrecedenceComparer MobilePartyPrecedenceComparerInstance = new MobilePartyPrecedenceComparer();

	public static readonly SkillObjectComparer SkillObjectComparerInstance = new SkillObjectComparer();

	public static readonly CharacterAttributeComparer CharacterAttributeComparerInstance = new CharacterAttributeComparer();

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

	private static readonly List<string> _attributeSortIndices = new List<string> { "Vigor", "Control", "Endurance", "Cunning", "Social", "Intelligence" };

	private static readonly List<string> _skillSortIndices = new List<string>
	{
		"OneHanded", "TwoHanded", "Polearm", "Bow", "Crossbow", "Throwing", "Riding", "Athletics", "Crafting", "Scouting",
		"Tactics", "Roguery", "Charm", "Leadership", "Trade", "Steward", "Medicine", "Engineering", "Mariner", "Boatswain",
		"Shipmaster"
	};

	private static readonly List<string> _navalSkills = new List<string> { "Mariner", "Boatswain", "Shipmaster" };

	private static void TooltipAddPropertyTitleWithValue(List<TooltipProperty> properties, string propertyName, float currentValue)
	{
		string value = currentValue.ToString("0.##");
		properties.Add(new TooltipProperty(propertyName, value, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
	}

	private static void TooltipAddPropertyTitleWithValue(List<TooltipProperty> properties, string propertyName, string currentValue)
	{
		properties.Add(new TooltipProperty(propertyName, currentValue, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
	}

	private static void TooltipAddExplanation(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
	{
		List<(string, float)> lines = explainedNumber.GetLines();
		if (lines.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < lines.Count; i++)
		{
			var (definition, num) = lines[i];
			if ((double)TaleWorlds.Library.MathF.Abs(num) >= 0.01)
			{
				string changeValueString = GetChangeValueString(num);
				properties.Add(new TooltipProperty(definition, changeValueString, 0));
			}
		}
	}

	private static void TooltipAddPropertyTitle(List<TooltipProperty> properties, string propertyName)
	{
		properties.Add(new TooltipProperty(propertyName, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
	}

	private static void TooltipAddExplainedResultChange(List<TooltipProperty> properties, float changeValue)
	{
		string changeValueString = GetChangeValueString(changeValue);
		properties.Add(new TooltipProperty(_changeStr.ToString(), changeValueString, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownResult));
	}

	private static void TooltipAddExplanedChange(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
	{
		TooltipAddExplanation(properties, ref explainedNumber);
		TooltipAddDoubleSeperator(properties);
		TooltipAddExplainedResultChange(properties, explainedNumber.ResultNumber);
	}

	private static void TooltipAddExplainedResultTotal(List<TooltipProperty> properties, float changeValue)
	{
		string changeValueString = GetChangeValueString(changeValue);
		properties.Add(new TooltipProperty(_totalStr.ToString(), changeValueString, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownResult));
	}

	public static List<TooltipProperty> GetTooltipForAccumulatingProperty(string propertyName, float currentValue, ExplainedNumber explainedNumber)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		TooltipAddPropertyTitleWithValue(list, propertyName, currentValue);
		TooltipAddExplanedChange(list, ref explainedNumber);
		return list;
	}

	public static List<TooltipProperty> GetTooltipForAccumulatingPropertyWithResult(string propertyName, float currentValue, ref ExplainedNumber explainedNumber)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		TooltipAddPropertyTitle(list, propertyName);
		TooltipAddExplanation(list, ref explainedNumber);
		TooltipAddDoubleSeperator(list);
		TooltipAddExplainedResultTotal(list, currentValue);
		return list;
	}

	public static List<TooltipProperty> GetTooltipForgProperty(string propertyName, float currentValue, ExplainedNumber explainedNumber)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		TooltipAddPropertyTitleWithValue(list, propertyName, currentValue);
		TooltipAddDoubleSeperator(list);
		TooltipAddExplanation(list, ref explainedNumber);
		return list;
	}

	private static void TooltipAddSeperator(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
	{
		properties.Add(new TooltipProperty("", string.Empty, 0, onlyShowOnExtend, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
	}

	private static void TooltipAddDoubleSeperator(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
	{
		properties.Add(new TooltipProperty("", string.Empty, 0, onlyShowOnExtend, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
	}

	private static void TooltipAddExtendInfo(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
	{
		properties.Add(new TooltipProperty("", "", -1)
		{
			OnlyShowWhenNotExtended = true
		});
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			if (TaleWorlds.InputSystem.Input.ControllerType.IsPlaystation())
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
		properties.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_map_tooltip_info").ToString(), -1)
		{
			OnlyShowWhenNotExtended = true
		});
	}

	private static void TooltipAddEmptyLine(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
	{
		properties.Add(new TooltipProperty(string.Empty, string.Empty, -1, onlyShowOnExtend));
	}

	public static string GetTownWallsTooltip(Town town)
	{
		TextObject disableReason;
		bool num = IsSettlementInformationHidden(town.Settlement, out disableReason);
		GameTexts.SetVariable("newline", "\n");
		if (num)
		{
			GameTexts.SetVariable("LEVEL", GameTexts.FindText("str_missing_info_indicator").ToString());
		}
		else
		{
			GameTexts.SetVariable("LEVEL", town.GetWallLevel());
		}
		return GameTexts.FindText("str_walls_with_value").ToString();
	}

	public static List<TooltipProperty> GetVillageMilitiaTooltip(Village village)
	{
		return GetSettlementPropertyTooltip(village.Settlement, _militiaStr.ToString(), village.Militia, village.MilitiaChangeExplanation);
	}

	public static List<TooltipProperty> GetTownMilitiaTooltip(Town town)
	{
		return GetSettlementPropertyTooltip(town.Settlement, _militiaStr.ToString(), town.Militia, town.MilitiaChangeExplanation);
	}

	public static List<TooltipProperty> GetTownFoodTooltip(Town town)
	{
		return GetSettlementPropertyTooltip(town.Settlement, _foodStr.ToString(), town.FoodStocks, town.FoodChangeExplanation);
	}

	public static List<TooltipProperty> GetTownLoyaltyTooltip(Town town)
	{
		TextObject disableReason;
		bool num = IsSettlementInformationHidden(town.Settlement, out disableReason);
		ExplainedNumber loyaltyChangeExplanation = town.LoyaltyChangeExplanation;
		List<TooltipProperty> settlementPropertyTooltip = GetSettlementPropertyTooltip(town.Settlement, _loyaltyStr.ToString(), town.Loyalty, loyaltyChangeExplanation);
		if (!num)
		{
			if (!town.OwnerClan.IsRebelClan)
			{
				if (town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebellionStartLoyaltyThreshold)
				{
					TooltipAddSeperator(settlementPropertyTooltip);
					settlementPropertyTooltip.Add(new TooltipProperty(" ", new TextObject("{=NxEy5Nbt}High risk of rebellion").ToString(), 1, UIColors.NegativeIndicator));
				}
				else if (town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold && loyaltyChangeExplanation.ResultNumber < 0f)
				{
					TooltipAddSeperator(settlementPropertyTooltip);
					settlementPropertyTooltip.Add(new TooltipProperty(" ", new TextObject("{=F0a7hyp0}Risk of rebellion").ToString(), 1, UIColors.NegativeIndicator));
				}
			}
			else
			{
				TooltipAddSeperator(settlementPropertyTooltip);
				settlementPropertyTooltip.Add(new TooltipProperty(" ", new TextObject("{=hOVPiG3z}Recently rebelled").ToString(), 1, UIColors.NegativeIndicator));
			}
		}
		return settlementPropertyTooltip;
	}

	public static List<TooltipProperty> GetTownProsperityTooltip(Town town)
	{
		return GetSettlementPropertyTooltip(town.Settlement, _prosperityStr.ToString(), town.Prosperity, town.ProsperityChangeExplanation);
	}

	public static List<TooltipProperty> GetTownDailyProductionTooltip(Town town)
	{
		ExplainedNumber explainedNumber = town.ConstructionExplanation;
		return GetSettlementPropertyTooltipWithResult(town.Settlement, _dailyProductionStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
	}

	public static List<TooltipProperty> GetTownSecurityTooltip(Town town)
	{
		ExplainedNumber securityChangeExplanation = town.SecurityChangeExplanation;
		return GetSettlementPropertyTooltip(town.Settlement, _securityStr.ToString(), town.Security, securityChangeExplanation);
	}

	public static string GetTownPatrolTooltip(Town town)
	{
		TextObject textObject = GameTexts.FindText("str_string_newline_string");
		textObject.SetTextVariable("newline", "\n");
		textObject.SetTextVariable("STR1", _patrolStr.ToString());
		if (IsSettlementInformationHidden(town.Settlement, out var _))
		{
			textObject.SetTextVariable("STR2", GameTexts.FindText("str_missing_info_indicator").ToString());
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

	public static List<TooltipProperty> GetVillageProsperityTooltip(Village village)
	{
		return GetSettlementPropertyTooltip(village.Settlement, _hearthStr.ToString(), village.Hearth, village.HearthChangeExplanation);
	}

	public static List<TooltipProperty> GetTownGarrisonTooltip(Town town)
	{
		return GetSettlementPropertyTooltip(town.Settlement, _garrisonStr.ToString(), town.GarrisonParty?.MemberRoster.TotalManCount ?? 0, SettlementHelper.GetGarrisonChangeExplainedNumber(town));
	}

	public static List<TooltipProperty> GetPartyTroopSizeLimitTooltip(PartyBase party)
	{
		ExplainedNumber explainedNumber = party.PartySizeLimitExplainer;
		return GetTooltipForAccumulatingPropertyWithResult(_partyTroopSizeLimitStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
	}

	public static List<TooltipProperty> GetPartyPrisonerSizeLimitTooltip(PartyBase party)
	{
		ExplainedNumber explainedNumber = party.PrisonerSizeLimitExplainer;
		return GetTooltipForAccumulatingPropertyWithResult(_partyPrisonerSizeLimitStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
	}

	public static List<TooltipProperty> GetUsedHorsesTooltip(List<Tuple<EquipmentElement, int>> usedUpgradeHorsesHistory)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (usedUpgradeHorsesHistory.Count > 0)
		{
			foreach (IGrouping<ItemObject, Tuple<EquipmentElement, int>> item in from h in usedUpgradeHorsesHistory
				group h by h.Item1.Item)
			{
				int num = item.Sum((Tuple<EquipmentElement, int> c) => c.Item2);
				list.Add(new TooltipProperty(item.Key.Name.ToString(), num.ToString(), 0));
			}
			list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty(_totalStr.ToString(), usedUpgradeHorsesHistory.Sum((Tuple<EquipmentElement, int> x) => x.Item2).ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownResult));
		}
		return list;
	}

	public static List<TooltipProperty> GetArmyCohesionTooltip(Army army)
	{
		return GetTooltipForAccumulatingProperty(_armyCohesionStr.ToString(), army.Cohesion, army.DailyCohesionChangeExplanation);
	}

	public static List<TooltipProperty> GetArmyManCountTooltip(Army army)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (army.LeaderParty != null)
		{
			list.Add(new TooltipProperty("", _numTotalTroopsInTheArmyStr.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
			Dictionary<FormationClass, int> dictionary = new Dictionary<FormationClass, int>();
			Dictionary<FormationClass, int> dictionary2 = new Dictionary<FormationClass, int>();
			for (int i = 0; i < army.LeaderParty.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = army.LeaderParty.MemberRoster.GetElementCopyAtIndex(i);
				dictionary.TryGetValue(elementCopyAtIndex.Character.DefaultFormationClass, out var value);
				dictionary[elementCopyAtIndex.Character.DefaultFormationClass] = value + elementCopyAtIndex.WoundedNumber;
				dictionary2.TryGetValue(elementCopyAtIndex.Character.DefaultFormationClass, out var value2);
				dictionary2[elementCopyAtIndex.Character.DefaultFormationClass] = value2 + elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber;
			}
			int num = army.LeaderParty.MemberRoster.TotalManCount;
			foreach (MobileParty attachedParty in army.LeaderParty.AttachedParties)
			{
				for (int j = 0; j < attachedParty.MemberRoster.Count; j++)
				{
					TroopRosterElement elementCopyAtIndex2 = attachedParty.MemberRoster.GetElementCopyAtIndex(j);
					dictionary.TryGetValue(elementCopyAtIndex2.Character.DefaultFormationClass, out var value3);
					dictionary[elementCopyAtIndex2.Character.DefaultFormationClass] = value3 + elementCopyAtIndex2.WoundedNumber;
					dictionary2.TryGetValue(elementCopyAtIndex2.Character.DefaultFormationClass, out var value4);
					dictionary2[elementCopyAtIndex2.Character.DefaultFormationClass] = value4 + elementCopyAtIndex2.Number - elementCopyAtIndex2.WoundedNumber;
				}
				num += attachedParty.MemberRoster.TotalManCount;
			}
			FormationClass[] formationClassValues = FormationClassExtensions.FormationClassValues;
			foreach (FormationClass formationClass in formationClassValues)
			{
				dictionary.TryGetValue(formationClass, out var value5);
				dictionary2.TryGetValue(formationClass, out var value6);
				if (value5 + value6 > 0)
				{
					TextObject textObject = new TextObject("{=Dqydb21E} {PARTY_SIZE}");
					textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(value6, value5, isInspected: true));
					TextObject textObject2 = GameTexts.FindText("str_troop_type_name", formationClass.GetName());
					list.Add(new TooltipProperty(textObject2.ToString(), textObject.ToString(), 0));
				}
			}
			list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty(_totalStr.ToString(), num.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownResult));
		}
		return list;
	}

	public static string GetDaysUntilNoFood(float totalFood, float foodChange)
	{
		if (totalFood <= float.Epsilon)
		{
			totalFood = 0f;
		}
		if (foodChange >= -1E-45f)
		{
			return GameTexts.FindText("str_days_until_no_food_never").ToString();
		}
		return TaleWorlds.Library.MathF.Ceiling(TaleWorlds.Library.MathF.Abs(totalFood / foodChange)).ToString();
	}

	public static List<TooltipProperty> GetSettlementPropertyTooltip(Settlement settlement, string valueName, float value, ExplainedNumber explainedNumber)
	{
		if (IsSettlementInformationHidden(settlement, out var disableReason))
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			string currentValue = GameTexts.FindText("str_missing_info_indicator").ToString();
			TooltipAddPropertyTitleWithValue(list, valueName, currentValue);
			TooltipAddEmptyLine(list);
			list.Add(new TooltipProperty(string.Empty, disableReason.ToString(), -1));
			return list;
		}
		return GetTooltipForAccumulatingProperty(valueName, value, explainedNumber);
	}

	public static List<TooltipProperty> GetSettlementPropertyTooltipWithResult(Settlement settlement, string valueName, float value, ref ExplainedNumber explainedNumber)
	{
		if (IsSettlementInformationHidden(settlement, out var disableReason))
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			string currentValue = GameTexts.FindText("str_missing_info_indicator").ToString();
			TooltipAddPropertyTitleWithValue(list, valueName, currentValue);
			TooltipAddEmptyLine(list);
			list.Add(new TooltipProperty(string.Empty, disableReason.ToString(), -1));
			return list;
		}
		return GetTooltipForAccumulatingPropertyWithResult(valueName, value, ref explainedNumber);
	}

	public static List<TooltipProperty> GetArmyFoodTooltip(Army army)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty(new TextObject("{=Q8dhryRX}Parties' Food").ToString(), "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		float num = army.LeaderParty.Food;
		foreach (MobileParty attachedParty in army.LeaderParty.AttachedParties)
		{
			num += attachedParty.Food;
		}
		list.Add(new TooltipProperty(GameTexts.FindText("str_total_army_food").ToString(), FloatToString(num), 0));
		list.Add(new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
		double num2 = 0.0;
		foreach (MobileParty party in army.Parties)
		{
			if (army.DoesLeaderPartyAndAttachedPartiesContain(party))
			{
				float val = party.Party.MobileParty.Food / (0f - party.Party.MobileParty.FoodChange);
				num2 += (double)Math.Max(val, 0f);
				string daysUntilNoFood = GetDaysUntilNoFood(party.Party.MobileParty.Food, party.Party.MobileParty.FoodChange);
				list.Add(new TooltipProperty(party.Party.MobileParty.Name.ToString(), daysUntilNoFood, 0));
			}
		}
		list.Add(new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
		list.Add(new TooltipProperty(new TextObject("{=rwKBR4NE}Average Days Until Food Runs Out").ToString(), TaleWorlds.Library.MathF.Ceiling(num2 / (double)army.LeaderPartyAndAttachedPartiesCount).ToString(), 0));
		return list;
	}

	public static string GetClanWealthStatusText(Clan clan)
	{
		string empty = string.Empty;
		if (clan.Leader.Gold < 15000)
		{
			return new TextObject("{=SixPXaNh}Very Poor").ToString();
		}
		if (clan.Leader.Gold < 45000)
		{
			return new TextObject("{=poorWealthStatus}Poor").ToString();
		}
		if (clan.Leader.Gold < 135000)
		{
			return new TextObject("{=averageWealthStatus}Average").ToString();
		}
		if (clan.Leader.Gold < 405000)
		{
			return new TextObject("{=UbRqC0Yz}Rich").ToString();
		}
		return new TextObject("{=oJmRg2ms}Very Rich").ToString();
	}

	public static List<TooltipProperty> GetClanProsperityTooltip(Clan clan)
	{
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty("", GameTexts.FindText("str_prosperity").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		int num = 0;
		int num2 = 0;
		EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
		for (int i = 0; i < clan.Heroes.Count; i++)
		{
			Hero hero = clan.Heroes[i];
			if (hero.Gold != 0 && hero.IsAlive && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(hero))
			{
				num2 = hero.Gold;
				list.Add(new TooltipProperty(hero.Name.ToString(), num2.ToString(), 0));
				num += num2;
			}
		}
		for (int j = 0; j < clan.Companions.Count; j++)
		{
			Hero hero2 = clan.Companions[j];
			if (hero2.Gold != 0 && hero2.IsAlive && hero2.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(hero2))
			{
				num2 = hero2.Gold;
				list.Add(new TooltipProperty(hero2.Name.ToString(), hero2.Gold.ToString(), 0));
				num += num2;
			}
		}
		TooltipAddDoubleSeperator(list);
		list.Add(new TooltipProperty(GameTexts.FindText("str_total_gold").ToString(), num.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownResult));
		return list;
	}

	private static List<TooltipProperty> GetDiplomacySettlementStatComparisonTooltip(List<Settlement> settlements, string title, string emptyExplanation = "")
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty("", title, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		if (settlements.Count == 0)
		{
			list.Add(new TooltipProperty(emptyExplanation, "", 0));
			list.Add(new TooltipProperty("", "", -1));
			return list;
		}
		for (int i = 0; i < settlements.Count; i++)
		{
			Settlement settlement = settlements[i];
			list.Add(new TooltipProperty(settlement.Name.ToString(), "", 0));
		}
		list.Add(new TooltipProperty("", "", -1));
		return list;
	}

	public static List<TooltipProperty> GetTruceOwnedSettlementsTooltip(List<Settlement> settlements, TextObject factionName, bool isTown)
	{
		TextObject textObject = (isTown ? new TextObject("{=o79dIa3L}Towns owned by {FACTION}") : new TextObject("{=z3Xg0IaG}Castles owned by {FACTION}"));
		TextObject textObject2 = (isTown ? new TextObject("{=cedvCZ73}There is no town owned by {FACTION}") : new TextObject("{=ZZmlYrgL}There is no castle owned by {FACTION}"));
		textObject.SetTextVariable("FACTION", factionName);
		textObject2.SetTextVariable("FACTION", factionName);
		return GetDiplomacySettlementStatComparisonTooltip(settlements, textObject.ToString(), textObject2.ToString());
	}

	public static List<TooltipProperty> GetWarPrisonersTooltip(List<Hero> capturedPrisoners, TextObject factionName)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		TextObject textObject = new TextObject("{=8BJDQe6o}Prisoners captured by {FACTION}");
		textObject.SetTextVariable("FACTION", factionName);
		list.Add(new TooltipProperty("", textObject.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		if (capturedPrisoners.Count == 0)
		{
			TextObject textObject2 = new TextObject("{=CK68QXen}There is no prisoner captured by {FACTION}");
			textObject2.SetTextVariable("FACTION", factionName);
			list.Add(new TooltipProperty(textObject2.ToString(), "", 0));
			list.Add(new TooltipProperty("", "", -1));
			return list;
		}
		string text = new TextObject("{=MT4b8H9h}Unknown").ToString();
		TextObject textObject3 = new TextObject("{=btoiLePb}{HERO} ({PLACE})");
		for (int i = 0; i < capturedPrisoners.Count; i++)
		{
			Hero hero = capturedPrisoners[i];
			string variable = hero.PartyBelongedToAsPrisoner?.Name.ToString() ?? text;
			textObject3.SetTextVariable("HERO", hero.Name.ToString());
			textObject3.SetTextVariable("PLACE", variable);
			list.Add(new TooltipProperty(textObject3.ToString(), "", 0));
		}
		list.Add(new TooltipProperty("", "", -1));
		return list;
	}

	public static List<TooltipProperty> GetNormalizedWarProgressTooltip(ExplainedNumber warProgress, ExplainedNumber otherFactionWarProgress, float maxValue, TextObject faction1Name, TextObject faction2Name)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		float num = maxValue / 100f;
		int num2 = (int)(warProgress.ResultNumber / num);
		int num3 = (int)(otherFactionWarProgress.ResultNumber / num);
		int num4 = TaleWorlds.Library.MathF.Max(0, num2 - num3);
		string definition = new TextObject("{=Pa4K0Paz}War Progress of {FACTION1} Against {FACTION2}").SetTextVariable("FACTION1", faction1Name).SetTextVariable("FACTION2", faction2Name).ToString();
		list.Add(new TooltipProperty(definition, num4.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		foreach (var (definition2, num5) in warProgress.GetLines())
		{
			list.Add(new TooltipProperty(definition2, ((int)(num5 / num)).ToString(), 0));
		}
		list.Add(new TooltipProperty(new TextObject("{=la6R4xaY}War Progress of Opposite Faction").ToString(), (-num3).ToString(), 0));
		return list;
	}

	public static List<TooltipProperty> GetClanStrengthTooltip(Clan clan)
	{
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty("", GameTexts.FindText("str_strength").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < MobileParty.AllLordParties.Count; i++)
		{
			MobileParty mobileParty = MobileParty.AllLordParties[i];
			if (mobileParty.ActualClan == clan && !mobileParty.IsDisbanding)
			{
				num2 = mobileParty.Party.CalculateCurrentStrength();
				list.Add(new TooltipProperty(mobileParty.Name.ToString(), num2.ToString(), 0));
				num += num2;
			}
		}
		TooltipAddDoubleSeperator(list);
		list.Add(new TooltipProperty(GameTexts.FindText("str_total_strength").ToString(), num.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownResult));
		return list;
	}

	public static List<TooltipProperty> GetCrimeTooltip(Settlement settlement)
	{
		if (settlement.MapFaction == null)
		{
			return GetTooltipForAccumulatingProperty(explainedNumber: new ExplainedNumber(0f, includeDescriptions: true), propertyName: _criminalRatingStr.ToString(), currentValue: 0f);
		}
		return GetTooltipForAccumulatingProperty(_criminalRatingStr.ToString(), settlement.MapFaction.MainHeroCrimeRating, settlement.MapFaction.DailyCrimeRatingChangeExplained);
	}

	public static List<TooltipProperty> GetInfluenceTooltip(Clan clan)
	{
		List<TooltipProperty> tooltipForAccumulatingProperty = GetTooltipForAccumulatingProperty(_influenceStr.ToString(), clan.Influence, clan.InfluenceChangeExplained);
		if (tooltipForAccumulatingProperty != null && clan.IsUnderMercenaryService)
		{
			tooltipForAccumulatingProperty.Add(new TooltipProperty("", _mercenaryClanInfluenceStr.ToString(), 0));
		}
		return tooltipForAccumulatingProperty;
	}

	public static List<TooltipProperty> GetClanRenownTooltip(Clan clan)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		var (explainedNumber, flag) = Campaign.Current.Models.ClanTierModel.HasUpcomingTier(clan, out var extraExplanation, includeDescriptions: true);
		list.Add(new TooltipProperty(GameTexts.FindText("str_enc_sf_renown").ToString(), ((int)clan.Renown).ToString(), 0));
		if (flag)
		{
			TooltipAddEmptyLine(list);
			list.Add(new TooltipProperty(GameTexts.FindText("str_clan_next_tier").ToString(), clan.RenownRequirementForNextTier.ToString(), 0));
			TooltipAddEmptyLine(list);
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_next_tier_bonus").ToString());
			string text = explainedNumber.GetExplanations().TrimEnd(new char[1] { '\n' });
			if (!TextObject.IsNullOrEmpty(extraExplanation))
			{
				TextObject textObject = GameTexts.FindText("str_string_newline_newline_string");
				textObject.SetTextVariable("STR1", text);
				textObject.SetTextVariable("STR2", extraExplanation);
				list.Add(new TooltipProperty(GameTexts.FindText("str_LEFT_colon_wSpace").ToString(), textObject.ToString(), 0));
			}
			else
			{
				list.Add(new TooltipProperty(GameTexts.FindText("str_LEFT_colon_wSpace").ToString(), text, 0));
			}
		}
		return list;
	}

	public static TooltipTriggerVM GetDenarTooltip()
	{
		ClanFinanceModel clanFinanceModel = Campaign.Current.Models.ClanFinanceModel;
		Func<ExplainedNumber> func = () => clanFinanceModel.CalculateClanGoldChange(Clan.PlayerClan, includeDescriptions: true);
		Func<ExplainedNumber> func2 = () => clanFinanceModel.CalculateClanGoldChange(Clan.PlayerClan, includeDescriptions: true, applyWithdrawals: false, includeDetails: true);
		RundownTooltipVM.ValueCategorization valueCategorization = RundownTooltipVM.ValueCategorization.LargeIsBetter;
		TextObject changeStr = _changeStr;
		TextObject totalStr = _totalStr;
		return new TooltipTriggerVM(typeof(ExplainedNumber), func, func2, changeStr, totalStr, valueCategorization);
	}

	public static List<TooltipProperty> GetPartyMoraleTooltip(MobileParty mainParty)
	{
		return GetTooltipForgProperty(_partyMoraleStr.ToString(), mainParty.Morale, mainParty.MoraleExplained);
	}

	public static List<TooltipProperty> GetPartyHealthTooltip(PartyBase party)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		TooltipAddPropertyTitleWithValue(list, _battleReadyTroopsStr.ToString(), party.NumberOfHealthyMembers);
		int num = party.NumberOfAllMembers - party.NumberOfHealthyMembers;
		TooltipAddPropertyTitleWithValue(list, _woundedTroopsStr.ToString(), num);
		if (num > 0)
		{
			ExplainedNumber explainedNumber = MobileParty.MainParty.Party.HealingRateForMemberRegularsExplained;
			TooltipAddDoubleSeperator(list);
			TooltipAddPropertyTitleWithValue(list, _regularsHealingRateStr.ToString(), explainedNumber.ResultNumber);
			TooltipAddSeperator(list);
			TooltipAddExplanation(list, ref explainedNumber);
		}
		return list;
	}

	public static List<TooltipProperty> GetPlayerHitpointsTooltip()
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		ExplainedNumber explainedNumber = Hero.MainHero.CharacterObject.MaxHitPointsExplanation;
		TooltipAddPropertyTitleWithValue(list, _hitPointsStr.ToString(), Hero.MainHero.HitPoints);
		TooltipAddSeperator(list);
		TooltipAddPropertyTitleWithValue(list, _maxhitPointsStr.ToString(), explainedNumber.ResultNumber);
		TooltipAddDoubleSeperator(list);
		TooltipAddExplanation(list, ref explainedNumber);
		if (Hero.MainHero.HitPoints < Hero.MainHero.MaxHitPoints)
		{
			ExplainedNumber explainedNumber2 = MobileParty.MainParty.Party.HealingRateForMemberHeroesExplained;
			TooltipAddDoubleSeperator(list);
			TooltipAddPropertyTitleWithValue(list, _heroesHealingRateStr.ToString(), explainedNumber2.ResultNumber);
			TooltipAddSeperator(list);
			TooltipAddExplanation(list, ref explainedNumber2);
		}
		return list;
	}

	public static List<TooltipProperty> GetPartyFoodTooltip(MobileParty mainParty)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		float num = ((mainParty.Food > 0f) ? mainParty.Food : 0f);
		TooltipAddPropertyTitleWithValue(list, _foodStr.ToString(), num);
		ExplainedNumber explainedNumber = mainParty.FoodChangeExplained;
		TooltipAddExplanedChange(list, ref explainedNumber);
		TooltipAddEmptyLine(list);
		List<TooltipProperty> list2 = new List<TooltipProperty>();
		int num2 = 0;
		List<TooltipProperty> list3 = new List<TooltipProperty>();
		int num3 = 0;
		for (int i = 0; i < mainParty.ItemRoster.Count; i++)
		{
			ItemRosterElement itemRosterElement = mainParty.ItemRoster[i];
			if (itemRosterElement.IsEmpty)
			{
				continue;
			}
			ItemObject item = itemRosterElement.EquipmentElement.Item;
			if (item != null && item.IsFood)
			{
				list2.Add(new TooltipProperty(itemRosterElement.EquipmentElement.GetModifiedItemName().ToString(), itemRosterElement.Amount.ToString(), 0));
				num2 += itemRosterElement.Amount;
				continue;
			}
			ItemObject item2 = itemRosterElement.EquipmentElement.Item;
			if (item2 != null && item2.HorseComponent?.IsLiveStock == true)
			{
				GameTexts.SetVariable("RANK", itemRosterElement.EquipmentElement.Item.HorseComponent.MeatCount);
				GameTexts.SetVariable("NUMBER", GameTexts.FindText("str_meat"));
				GameTexts.SetVariable("NUM2", GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString());
				GameTexts.SetVariable("NUM1", itemRosterElement.Amount);
				list3.Add(new TooltipProperty(itemRosterElement.EquipmentElement.GetModifiedItemName().ToString(), GameTexts.FindText("str_NUM_times_NUM_with_space").ToString(), 0));
				num3 += itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.HorseComponent.MeatCount;
			}
		}
		if (num2 > 0)
		{
			list.Add(new TooltipProperty(_foodItemsStr.ToString(), num2.ToString(), 0));
			TooltipAddDoubleSeperator(list);
			list.AddRange(list2);
			TooltipAddEmptyLine(list);
		}
		if (num3 > 0)
		{
			list.Add(new TooltipProperty(_livestockStr.ToString(), num3.ToString(), 0));
			TooltipAddDoubleSeperator(list);
			list.AddRange(list3);
			TooltipAddEmptyLine(list);
		}
		list.Add(new TooltipProperty(GameTexts.FindText("str_total_days_until_no_food").ToString(), GetDaysUntilNoFood(num, explainedNumber.ResultNumber), 0));
		return list;
	}

	public static List<TooltipProperty> GetPartySpeedTooltip(bool considerArmySpeed)
	{
		Game.Current.EventManager.TriggerEvent(new PlayerInspectedPartySpeedEvent());
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (Hero.MainHero.IsPrisoner)
		{
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_main_hero_is_imprisoned").ToString(), 0));
		}
		else
		{
			MobileParty mobileParty = MobileParty.MainParty.Army?.LeaderParty;
			if (considerArmySpeed && mobileParty != null)
			{
				ExplainedNumber explainedNumber = mobileParty.SpeedExplained;
				float resultNumber = explainedNumber.ResultNumber;
				list = GetTooltipForAccumulatingPropertyWithResult(_partySpeedStr.ToString(), resultNumber, ref explainedNumber);
			}
			else
			{
				ExplainedNumber explainedNumber2 = MobileParty.MainParty.SpeedExplained;
				float resultNumber2 = explainedNumber2.ResultNumber;
				list = GetTooltipForAccumulatingPropertyWithResult(_partySpeedStr.ToString(), resultNumber2, ref explainedNumber2);
			}
		}
		return list;
	}

	public static List<TooltipProperty> GetPartyWageTooltip(MobileParty mobileParty)
	{
		ExplainedNumber explainedNumber = mobileParty.TotalWageExplained;
		return GetTooltipForAccumulatingPropertyWithResult(GameTexts.FindText("str_party_wage").ToString(), explainedNumber.ResultNumber, ref explainedNumber);
	}

	public static List<TooltipProperty> GetViewDistanceTooltip()
	{
		ExplainedNumber explainedNumber = MobileParty.MainParty.SeeingRangeExplanation;
		return GetTooltipForAccumulatingPropertyWithResult(_viewDistanceFoodStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
	}

	public static List<TooltipProperty> GetMainPartyHealthTooltip()
	{
		PartyBase party = MobileParty.MainParty.Party;
		List<TooltipProperty> list = new List<TooltipProperty>();
		TooltipAddPropertyTitleWithValue(list, _battleReadyTroopsStr.ToString(), party.NumberOfHealthyMembers);
		TooltipAddEmptyLine(list);
		int num = party.NumberOfAllMembers - party.NumberOfHealthyMembers;
		list.Add(new TooltipProperty(_woundedTroopsStr.ToString(), num.ToString(), 0));
		if (num > 0)
		{
			TooltipAddDoubleSeperator(list);
			list.Add(new TooltipProperty(_regularsHealingRateStr.ToString(), MobileParty.MainParty.Party.HealingRateForMemberRegulars.ToString(), 0));
			ExplainedNumber explainedNumber = MobileParty.MainParty.Party.HealingRateForMemberRegularsExplained;
			TooltipAddExplanation(list, ref explainedNumber);
		}
		int totalManCount = party.PrisonRoster.TotalManCount;
		if (totalManCount > 0)
		{
			TooltipAddSeperator(list);
			list.Add(new TooltipProperty(_prisonersStr.ToString(), totalManCount.ToString(), 0));
		}
		TooltipAddEmptyLine(list);
		TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT_no_space");
		Color color = new Color(0.82f, 0.12f, 0.07f);
		int totalManCount2 = party.MemberRoster.TotalManCount;
		int partySizeLimit = party.PartySizeLimit;
		textObject.SetTextVariable("LEFT", totalManCount2).SetTextVariable("RIGHT", partySizeLimit);
		if (totalManCount2 > partySizeLimit)
		{
			list.Add(new TooltipProperty(new TextObject("{=ZgYAGfbD}Land Troop Capacity").ToString(), textObject.ToString(), 0, color));
		}
		else
		{
			list.Add(new TooltipProperty(new TextObject("{=ZgYAGfbD}Land Troop Capacity").ToString(), textObject.ToString(), 0));
		}
		if (party.Ships.Count > 0)
		{
			int num2 = party.Ships.Sum((Ship s) => s.SkeletalCrewCapacity);
			textObject.SetTextVariable("LEFT", totalManCount2).SetTextVariable("RIGHT", num2);
			if (totalManCount2 < num2)
			{
				list.Add(new TooltipProperty(new TextObject("{=p9wUyxfb}Ship Skeletal Crew").ToString(), textObject.ToString(), 0, color));
			}
			else
			{
				list.Add(new TooltipProperty(new TextObject("{=p9wUyxfb}Ship Skeletal Crew").ToString(), textObject.ToString(), 0));
			}
			int num3 = party.Ships.Sum((Ship s) => s.TotalCrewCapacity);
			textObject.SetTextVariable("LEFT", totalManCount2).SetTextVariable("RIGHT", num3);
			if (totalManCount2 > num3)
			{
				list.Add(new TooltipProperty(new TextObject("{=w1tgTNvK}Ship Troop Capacity").ToString(), textObject.ToString(), 0, color));
			}
			else
			{
				list.Add(new TooltipProperty(new TextObject("{=w1tgTNvK}Ship Troop Capacity").ToString(), textObject.ToString(), 0));
			}
		}
		return list;
	}

	public static List<TooltipProperty> GetPartyInventoryCapacityTooltip(MobileParty party, bool forceLand = false, bool forceSea = false)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (forceLand)
		{
			TooltipAddPropertyTitleWithValue(list, _partyInventoryLandCapacityStr.ToString(), (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, isCurrentlyAtSea: false).ResultNumber);
			TooltipAddSeperator(list);
			ExplainedNumber explainedNumber = Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, isCurrentlyAtSea: false, includeDescriptions: true);
			TooltipAddExplanation(list, ref explainedNumber);
		}
		else if (forceSea)
		{
			TooltipAddPropertyTitleWithValue(list, _partyInventorySeaCapacityStr.ToString(), (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, isCurrentlyAtSea: true).ResultNumber);
			TooltipAddSeperator(list);
			ExplainedNumber explainedNumber2 = Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(party, isCurrentlyAtSea: true, includeDescriptions: true);
			TooltipAddExplanation(list, ref explainedNumber2);
		}
		else
		{
			TextObject textObject = (party.IsCurrentlyAtSea ? _partyInventoryCargoCapacityStr : _partyInventoryCapacityStr);
			TooltipAddPropertyTitleWithValue(list, textObject.ToString(), party.InventoryCapacity);
			TooltipAddSeperator(list);
			ExplainedNumber explainedNumber3 = party.InventoryCapacityExplainedNumber;
			TooltipAddExplanation(list, ref explainedNumber3);
		}
		return list;
	}

	public static List<TooltipProperty> GetPartyInventoryWeightTooltip(MobileParty party, bool forceLand = false, bool forceSea = false)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (forceLand)
		{
			TooltipAddPropertyTitleWithValue(list, _partyInventoryLandWeightStr.ToString(), (int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, isCurrentlyAtSea: false).ResultNumber);
			TooltipAddSeperator(list);
			ExplainedNumber explainedNumber = Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, isCurrentlyAtSea: false, includeDescriptions: true);
			TooltipAddExplanation(list, ref explainedNumber);
		}
		else if (forceSea)
		{
			TooltipAddPropertyTitleWithValue(list, _partyInventorySeaWeightStr.ToString(), (int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, isCurrentlyAtSea: true).ResultNumber);
			TooltipAddSeperator(list);
			ExplainedNumber explainedNumber2 = Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(party, isCurrentlyAtSea: true, includeDescriptions: true);
			TooltipAddExplanation(list, ref explainedNumber2);
		}
		else
		{
			TextObject textObject = (party.IsCurrentlyAtSea ? _partyInventoryCargoStr : _partyInventoryWeightStr);
			TooltipAddPropertyTitleWithValue(list, textObject.ToString(), party.TotalWeightCarried);
			TooltipAddSeperator(list);
			ExplainedNumber explainedNumber3 = party.TotalWeightCarriedExplainedNumber;
			TooltipAddExplanation(list, ref explainedNumber3);
		}
		return list;
	}

	public static List<TooltipProperty> GetPerkEffectText(PerkObject perk, bool isActive)
	{
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty("", perk.Name.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		TextObject perkRoleText = GetPerkRoleText(perk, getSecondary: false);
		if (perkRoleText != null)
		{
			list.Add(new TooltipProperty("", perkRoleText.ToString(), 0));
			list.Add(new TooltipProperty("", perk.PrimaryDescription.ToString(), 0));
			list.Add(new TooltipProperty("", "", 0));
		}
		TextObject perkRoleText2 = GetPerkRoleText(perk, getSecondary: true);
		if (perkRoleText2 != null)
		{
			list.Add(new TooltipProperty("", perkRoleText2.ToString(), 0));
			list.Add(new TooltipProperty("", perk.SecondaryDescription.ToString(), 0));
			list.Add(new TooltipProperty("", "", 0));
		}
		if (isActive)
		{
			list.Add(new TooltipProperty("", GameTexts.FindText("str_perk_active").ToString(), 0));
		}
		list.Add(new TooltipProperty(GameTexts.FindText("str_required_level_perk").ToString(), ((int)perk.RequiredSkillValue).ToString(), 0));
		return list;
	}

	public static TextObject GetPerkRoleText(PerkObject perk, bool getSecondary)
	{
		TextObject textObject = null;
		if (!getSecondary && perk.PrimaryRole != PartyRole.None)
		{
			textObject = GameTexts.FindText("str_perk_one_role");
			textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.PrimaryRole.ToString()));
		}
		else if (getSecondary && perk.SecondaryRole != PartyRole.None)
		{
			textObject = GameTexts.FindText("str_perk_one_role");
			textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.SecondaryRole.ToString()));
		}
		return textObject;
	}

	public static TextObject GetCombinedPerkRoleText(PerkObject perk)
	{
		TextObject textObject = null;
		if (perk.PrimaryRole != PartyRole.None && perk.SecondaryRole != PartyRole.None)
		{
			textObject = GameTexts.FindText("str_perk_two_roles");
			textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.PrimaryRole.ToString()));
			textObject.SetTextVariable("SECONDARY_ROLE", GameTexts.FindText("role", perk.SecondaryRole.ToString()));
		}
		else if (perk.PrimaryRole != PartyRole.None)
		{
			textObject = GameTexts.FindText("str_perk_one_role");
			textObject.SetTextVariable("PRIMARY_ROLE", GameTexts.FindText("role", perk.PrimaryRole.ToString()));
		}
		return textObject;
	}

	public static List<TooltipProperty> GetSiegeMachineTooltip(SiegeEngineType engineType, bool showDescription = true, int hoursUntilCompletion = 0)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (showDescription)
		{
			GameTexts.SetVariable("NEWLINE", "\n");
		}
		string value = (showDescription ? GameTexts.FindText("str_siege_weapon_tooltip_text", engineType.StringId).ToString() : engineType.Name.ToString());
		list.Add(new TooltipProperty(" ", value, 0));
		if (hoursUntilCompletion > 0)
		{
			TooltipProperty siegeMachineProgressLine = GetSiegeMachineProgressLine(hoursUntilCompletion);
			if (siegeMachineProgressLine != null)
			{
				list.Add(siegeMachineProgressLine);
			}
		}
		return list;
	}

	public static string GetSiegeMachineName(SiegeEngineType engineType)
	{
		if (engineType != null)
		{
			return engineType.Name.ToString();
		}
		return "";
	}

	public static string GetSiegeMachineNameWithDesctiption(SiegeEngineType engineType)
	{
		if (engineType != null)
		{
			return GameTexts.FindText("str_siege_weapon_tooltip_text", engineType.StringId).ToString();
		}
		return "";
	}

	public static List<TooltipProperty> GetTroopConformityTooltip(TroopRosterElement troop)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (troop.Character != null)
		{
			int elementXp = PartyBase.MainParty.PrisonRoster.GetElementXp(troop.Character);
			int conformityNeededToRecruitPrisoner = troop.Character.ConformityNeededToRecruitPrisoner;
			int num = ((elementXp >= conformityNeededToRecruitPrisoner * troop.Number) ? conformityNeededToRecruitPrisoner : (elementXp % conformityNeededToRecruitPrisoner));
			list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_current_conformity").ToString(), num.ToString(), 0));
			list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_recruit_conformity_cost").ToString(), conformityNeededToRecruitPrisoner.ToString(), 0));
			list.Add(new TooltipProperty(GameTexts.FindText("str_party_recruitable_troops").ToString(), TaleWorlds.Library.MathF.Min(elementXp / conformityNeededToRecruitPrisoner, troop.Number).ToString(), 0));
			if (elementXp < conformityNeededToRecruitPrisoner * troop.Number)
			{
				GameTexts.SetVariable("CONFORMITY_AMOUNT", (conformityNeededToRecruitPrisoner - num).ToString());
				list.Add(new TooltipProperty("", GameTexts.FindText("str_party_troop_conformity_explanation").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine));
			}
		}
		return list;
	}

	public static List<TooltipProperty> GetLearningRateTooltip(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill)
	{
		ExplainedNumber explainedNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(characterAttributes, focusValue, skillValue, skill, includeDescriptions: true);
		return GetTooltipForAccumulatingPropertyWithResult(_learningRateStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
	}

	public static List<TooltipProperty> GetTroopXPTooltip(TroopRosterElement troop)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (troop.Character != null && troop.Character.UpgradeTargets.Length != 0)
		{
			int xp = troop.Xp;
			int upgradeXpCost = troop.Character.GetUpgradeXpCost(PartyBase.MainParty, 0);
			int num = ((xp >= upgradeXpCost * troop.Number) ? upgradeXpCost : (xp % upgradeXpCost));
			list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_current_xp").ToString(), num.ToString(), 0));
			list.Add(new TooltipProperty(GameTexts.FindText("str_party_troop_upgrade_xp_cost").ToString(), upgradeXpCost.ToString(), 0));
			list.Add(new TooltipProperty(GameTexts.FindText("str_party_upgradable_troops").ToString(), TaleWorlds.Library.MathF.Min(xp / upgradeXpCost, troop.Number).ToString(), 0));
			if (xp < upgradeXpCost * troop.Number)
			{
				int content = upgradeXpCost - num;
				GameTexts.SetVariable("XP_AMOUNT", content);
				list.Add(new TooltipProperty("", GameTexts.FindText("str_party_troop_xp_explanation").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine));
			}
		}
		return list;
	}

	public static List<TooltipProperty> GetLearningLimitTooltip(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill)
	{
		ExplainedNumber explainedNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(characterAttributes, focusValue, skill, includeDescriptions: true);
		return GetTooltipForAccumulatingPropertyWithResult(_learningLimitStr.ToString(), explainedNumber.ResultNumber, ref explainedNumber);
	}

	public static List<TooltipProperty> GetSettlementConsumptionTooltip(Settlement settlement)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty("", GameTexts.FindText("str_consumption").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		if (settlement.IsTown)
		{
			foreach (Town.SellLog soldItem in settlement.Town.SoldItems)
			{
				list.Add(new TooltipProperty(soldItem.Category.GetName().ToString(), soldItem.Number.ToString(), 0));
			}
		}
		else
		{
			Debug.FailedAssert("Only towns' consumptions are tracked", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CampaignUIHelper.cs", "GetSettlementConsumptionTooltip", 1388);
		}
		return list;
	}

	public static StringItemWithHintVM GetCharacterTierData(CharacterObject character, bool isBig = false)
	{
		int tier = character.Tier;
		if (tier <= 0 || tier > 7)
		{
			return new StringItemWithHintVM("", TextObject.GetEmpty());
		}
		string text = (isBig ? (tier + "_big") : tier.ToString());
		string text2 = "General\\TroopTierIcons\\icon_tier_" + text;
		GameTexts.SetVariable("TIER_LEVEL", tier);
		TextObject hint = new TextObject("{=!}" + GameTexts.FindText("str_party_troop_tier").ToString());
		return new StringItemWithHintVM(text2, hint);
	}

	public static List<TooltipProperty> GetSettlementProductionTooltip(Settlement settlement)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty("", GameTexts.FindText("str_production").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		if (settlement.IsFortification)
		{
			list.Add(new TooltipProperty(GameTexts.FindText("str_villages").ToString(), " ", 0));
			TooltipAddDoubleSeperator(list);
			for (int i = 0; i < settlement.BoundVillages.Count; i++)
			{
				Village village = settlement.BoundVillages[i];
				list.Add(new TooltipProperty(village.Name.ToString(), village.VillageType.PrimaryProduction.Name.ToString(), 0));
			}
			list.Add(new TooltipProperty(GameTexts.FindText("str_shops_in_town").ToString(), " ", 0));
			TooltipAddDoubleSeperator(list);
			foreach (Workshop item in settlement.Town.Workshops.Where((Workshop w) => w.WorkshopType != null && !w.WorkshopType.IsHidden))
			{
				list.Add(new TooltipProperty(" ", item.WorkshopType.Name.ToString(), 0));
			}
		}
		else if (settlement.IsVillage)
		{
			list.Add(new TooltipProperty(GameTexts.FindText("str_production_in_village").ToString(), " ", 0));
			TooltipAddDoubleSeperator(list);
			for (int num = 0; num < settlement.Village.VillageType.Productions.Count; num++)
			{
				list.Add(new TooltipProperty(" ", settlement.Village.VillageType.Productions[num].Item1.Name.ToString(), 0));
			}
		}
		return list;
	}

	public static string GetHintTextFromReasons(List<TextObject> reasons)
	{
		TextObject textObject = TextObject.GetEmpty();
		for (int i = 0; i < reasons.Count; i++)
		{
			if (i >= 1)
			{
				GameTexts.SetVariable("STR1", textObject.ToString());
				GameTexts.SetVariable("STR2", reasons[i]);
				textObject = GameTexts.FindText("str_string_newline_string");
			}
			else
			{
				textObject = reasons[i];
			}
		}
		return textObject.ToString();
	}

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

	public static TextObject GetHoursAndDaysTextFromHourValue(int hours)
	{
		TextObject textObject = TextObject.GetEmpty();
		if (hours == 0)
		{
			textObject = GameTexts.FindText("str_hours");
			textObject.SetTextVariable("HOUR", 0);
		}
		else if (hours > 0)
		{
			int num = hours / 24;
			int num2 = hours % 24;
			textObject = ((num <= 0) ? GameTexts.FindText("str_hours") : ((num2 > 0) ? GameTexts.FindText("str_days_hours") : GameTexts.FindText("str_days")));
			textObject.SetTextVariable("DAY", num);
			textObject.SetTextVariable("PLURAL_DAYS", (num > 1) ? 1 : 0);
			textObject.SetTextVariable("HOUR", num2);
			textObject.SetTextVariable("PLURAL_HOURS", (num2 > 1) ? 1 : 0);
		}
		return textObject;
	}

	public static TextObject GetTeleportationDelayText(Hero hero, PartyBase target)
	{
		TextObject result = TextObject.GetEmpty();
		if (hero != null && target != null)
		{
			float resultNumber = Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, target).ResultNumber;
			if (hero.IsTraveling)
			{
				result = _travelingText.CopyTextObject();
			}
			else if (resultNumber > 0f)
			{
				TextObject textObject = new TextObject("{=P0To9aRW}Travel time: {TRAVEL_TIME}");
				textObject.SetTextVariable("TRAVEL_TIME", GetHoursAndDaysTextFromHourValue((int)Math.Ceiling(resultNumber)));
				result = textObject;
			}
			else
			{
				result = _noDelayText.CopyTextObject();
			}
		}
		return result;
	}

	public static List<TooltipProperty> GetTimeOfDayAndResetCameraTooltip()
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		int getHourOfDay = CampaignTime.Now.GetHourOfDay;
		TextObject textObject = ((getHourOfDay >= 6 && getHourOfDay < 12) ? new TextObject("{=X3gcUz7C}Morning") : ((getHourOfDay >= 12 && getHourOfDay < 15) ? new TextObject("{=CTtjSwRb}Noon") : ((getHourOfDay >= 15 && getHourOfDay < 18) ? new TextObject("{=J2gvnexb}Afternoon") : ((getHourOfDay < 18 || getHourOfDay >= 22) ? new TextObject("{=fAxjyMt5}Night") : new TextObject("{=gENb9SSW}Evening")))));
		list.Add(new TooltipProperty(textObject.ToString(), "", 0));
		list.Add(new TooltipProperty("", new TextObject("{=sFiU3Ss2}Click to Reset Camera").ToString(), 0));
		return list;
	}

	public static List<TooltipProperty> GetTournamentChampionRewardsTooltip(Hero hero, Town town)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		TooltipAddPropertyTitle(list, new TextObject("{=CGVK6l8I}Champion Benefits").ToString());
		TextObject textObject = new TextObject("{=4vZLpzPi}+1 Renown / Day");
		list.Add(new TooltipProperty(textObject.ToString(), "", 0));
		return list;
	}

	public static StringItemWithHintVM GetCharacterTypeData(CharacterObject character, bool isBig = false)
	{
		if (character.IsHero)
		{
			return new StringItemWithHintVM("", TextObject.GetEmpty());
		}
		bool flag = !character.IsHero && character.IsMariner;
		TextObject textObject = new TextObject("{=!}{TYPENAME}{MARINER}{BIG}");
		TextObject textObject2;
		if (character.IsRanged && character.IsMounted)
		{
			textObject.SetTextVariable("TYPENAME", "horse_archer");
			textObject2 = GameTexts.FindText("str_troop_type_name", "HorseArcher");
		}
		else if (character.IsRanged)
		{
			textObject.SetTextVariable("TYPENAME", "bow");
			string variation = (flag ? "Ranged_Mariner" : "Ranged");
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
			string variation2 = (flag ? "Infantry_Mariner" : "Infantry");
			textObject2 = GameTexts.FindText("str_troop_type_name", variation2);
		}
		textObject.SetTextVariable("MARINER", flag ? "_mariner" : "");
		textObject.SetTextVariable("BIG", isBig ? "_big" : "");
		return new StringItemWithHintVM("General\\TroopTypeIcons\\icon_troop_type_" + textObject.ToString(), new TextObject("{=!}" + textObject2.ToString()));
	}

	public static List<TooltipProperty> GetHeroHealthTooltip(Hero hero)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		GameTexts.SetVariable("LEFT", hero.HitPoints.ToString("0.##"));
		GameTexts.SetVariable("RIGHT", hero.MaxHitPoints.ToString("0.##"));
		list.Add(new TooltipProperty(_hitPointsStr.ToString(), GameTexts.FindText("str_LEFT_over_RIGHT").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		TooltipAddSeperator(list);
		TooltipAddPropertyTitleWithValue(list, _maxhitPointsStr.ToString(), hero.MaxHitPoints);
		TooltipAddDoubleSeperator(list);
		ExplainedNumber explainedNumber = hero.CharacterObject.MaxHitPointsExplanation;
		TooltipAddExplanation(list, ref explainedNumber);
		return list;
	}

	public static List<TooltipProperty> GetSiegeWallTooltip(int wallLevel, int wallHitpoints)
	{
		return new List<TooltipProperty>
		{
			new TooltipProperty(GameTexts.FindText("str_map_tooltip_wall_level").ToString(), wallLevel.ToString(), 0),
			new TooltipProperty(GameTexts.FindText("str_map_tooltip_wall_hitpoints").ToString(), wallHitpoints.ToString(), 0)
		};
	}

	public static List<TooltipProperty> GetGovernorPerksTooltipForHero(Hero hero)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty(GameTexts.FindText("str_clan_governor_perks").ToString(), " ", 0));
		TooltipAddSeperator(list);
		List<PerkObject> governorPerksForHero = PerkHelper.GetGovernorPerksForHero(hero);
		for (int i = 0; i < governorPerksForHero.Count; i++)
		{
			if (governorPerksForHero[i].PrimaryRole == PartyRole.Governor)
			{
				list.Add(new TooltipProperty(governorPerksForHero[i].Name.ToString(), governorPerksForHero[i].PrimaryDescription.ToString(), 0));
			}
			if (governorPerksForHero[i].SecondaryRole == PartyRole.Governor)
			{
				list.Add(new TooltipProperty(governorPerksForHero[i].Name.ToString(), governorPerksForHero[i].SecondaryDescription.ToString(), 0));
			}
		}
		if (governorPerksForHero.Count == 0)
		{
			list.Add(new TooltipProperty("", new TextObject("{=oSfsqBwJ}No perks").ToString(), 0));
		}
		return list;
	}

	public static (TextObject titleText, TextObject bodyText) GetGovernorSelectionConfirmationPopupTexts(Hero currentGovernor, Hero newGovernor, Settlement settlement)
	{
		if (settlement != null)
		{
			bool num = newGovernor == null;
			DelayedTeleportationModel delayedTeleportationModel = Campaign.Current.Models.DelayedTeleportationModel;
			int num2 = ((!num) ? ((int)Math.Ceiling(delayedTeleportationModel.GetTeleportationDelayAsHours(newGovernor, settlement.Party).ResultNumber)) : 0);
			MBTextManager.SetTextVariable("TRAVEL_DURATION", GetHoursAndDaysTextFromHourValue(num2).ToString());
			CharacterObject characterObject = ((!num) ? newGovernor?.CharacterObject : currentGovernor?.CharacterObject);
			if (characterObject != null)
			{
				StringHelpers.SetCharacterProperties("GOVERNOR", characterObject);
			}
			MBTextManager.SetTextVariable("SETTLEMENT_NAME", settlement.Name?.ToString() ?? string.Empty);
			TextObject item = GameTexts.FindText(num ? "str_clan_remove_governor" : "str_clan_assign_governor");
			TextObject item2 = GameTexts.FindText(num ? "str_remove_governor_inquiry" : ((num2 == 0) ? "str_change_governor_instantly_inquiry" : "str_change_governor_inquiry"));
			return (titleText: item, bodyText: item2);
		}
		return (titleText: TextObject.GetEmpty(), bodyText: TextObject.GetEmpty());
	}

	public static List<TooltipProperty> GetHeroGovernorEffectsTooltip(Hero hero, Settlement settlement)
	{
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty("", hero.Name.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		list.Add(new TooltipProperty(string.Empty, GetTeleportationDelayText(hero, settlement.Party).ToString(), 0));
		MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_relation"));
		string definition = GameTexts.FindText("str_LEFT_ONLY").ToString();
		list.Add(new TooltipProperty(definition, ((int)hero.GetRelationWithPlayer()).ToString(), 0));
		MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_type"));
		string definition2 = GameTexts.FindText("str_LEFT_ONLY").ToString();
		list.Add(new TooltipProperty(definition2, HeroHelper.GetCharacterTypeName(hero).ToString(), 0));
		MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_culture"));
		string definition3 = GameTexts.FindText("str_LEFT_ONLY").ToString();
		list.Add(new TooltipProperty(definition3, hero.Culture.Name.ToString(), 0));
		list.Add(new TooltipProperty(_partyRolesText.ToString(), "", 0));
		List<PartyRole> list2 = hero.PartyBelongedTo?.GetHeroPartyRoles(hero);
		if (list2 != null)
		{
			foreach (PartyRole item in list2)
			{
				TextObject textObject = GameTexts.FindText("role", item.ToString());
				list.Add(new TooltipProperty("", textObject.ToString(), 0));
			}
		}
		TooltipAddEmptyLine(list);
		list.Add(new TooltipProperty(_governerEffectsText.ToString(), "", 0));
		TooltipAddSeperator(list);
		(TextObject, TextObject) governorEngineeringSkillEffectForHero = PerkHelper.GetGovernorEngineeringSkillEffectForHero(hero);
		list.Add(new TooltipProperty(governorEngineeringSkillEffectForHero.Item1.ToString(), governorEngineeringSkillEffectForHero.Item2.ToString(), 0));
		TooltipAddEmptyLine(list);
		List<TooltipProperty> governorPerksTooltipForHero = GetGovernorPerksTooltipForHero(hero);
		list.AddRange(governorPerksTooltipForHero);
		return list;
	}

	public static List<TooltipProperty> GetEncounterPartyMoraleTooltip(List<MobileParty> parties)
	{
		return new List<TooltipProperty>();
	}

	public static TextObject GetCraftingTemplatePieceUnlockProgressHint(float progress)
	{
		TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis");
		textObject.SetTextVariable("LEFT", progress.ToString("F0"));
		textObject.SetTextVariable("RIGHT", "100");
		TextObject variable = new TextObject("{=opU0Nr2G}Progress for unlocking a new piece.");
		TextObject textObject2 = GameTexts.FindText("str_STR1_space_STR2");
		textObject2.SetTextVariable("STR1", variable);
		textObject2.SetTextVariable("STR2", textObject);
		return textObject2;
	}

	public static List<(string, TextObject)> GetWeaponFlagDetails(WeaponFlags weaponFlags, CharacterObject character = null)
	{
		List<(string, TextObject)> list = new List<(string, TextObject)>();
		if (weaponFlags.HasAnyFlag(WeaponFlags.BonusAgainstShield))
		{
			string item = "WeaponFlagIcons\\bonus_against_shield";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_bonus_against_shield");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
		{
			string item = "WeaponFlagIcons\\can_knock_down";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_knockdown");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanDismount))
		{
			string item = "WeaponFlagIcons\\can_dismount";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_dismount");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanHook))
		{
			string item = "WeaponFlagIcons\\can_dismount";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_hook");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanCrushThrough))
		{
			string item = "WeaponFlagIcons\\can_crush_through";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_crush_through");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithTwoHand))
		{
			string item = "WeaponFlagIcons\\not_usable_with_two_hand";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_not_usable_two_hand");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
		{
			string item = "WeaponFlagIcons\\not_usable_with_one_hand";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_not_usable_one_hand");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CantReloadOnHorseback) && (character == null || !character.GetPerkValue(DefaultPerks.Crossbow.MountedCrossbowman)))
		{
			string item = "WeaponFlagIcons\\cant_reload_on_horseback";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_cant_reload_on_horseback");
			list.Add((item, item2));
		}
		return list;
	}

	public static List<Tuple<string, TextObject>> GetItemFlagDetails(ItemFlags itemFlags)
	{
		List<Tuple<string, TextObject>> list = new List<Tuple<string, TextObject>>();
		if (itemFlags.HasAnyFlag(ItemFlags.Civilian))
		{
			string item = "GeneralFlagIcons\\civillian";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_civillian");
			list.Add(new Tuple<string, TextObject>(item, item2));
		}
		return list;
	}

	public static List<(string, TextObject)> GetItemUsageSetFlagDetails(ItemObject.ItemUsageSetFlags flags, CharacterObject character = null)
	{
		List<(string, TextObject)> list = new List<(string, TextObject)>();
		if (flags.HasAnyFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount) && (character == null || !character.GetPerkValue(DefaultPerks.Bow.HorseMaster)))
		{
			list.Add(("WeaponFlagIcons\\cant_use_on_horseback", GameTexts.FindText("str_inventory_flag_cant_use_with_mounts")));
		}
		if (flags.HasAnyFlag(ItemObject.ItemUsageSetFlags.RequiresNoShield))
		{
			list.Add(("WeaponFlagIcons\\cant_use_with_shields", GameTexts.FindText("str_inventory_flag_cant_use_with_shields")));
		}
		return list;
	}

	public static List<(string, TextObject)> GetFlagDetailsForWeapon(WeaponComponentData weapon, ItemObject.ItemUsageSetFlags itemUsageFlags, CharacterObject character = null)
	{
		List<(string, TextObject)> list = new List<(string, TextObject)>();
		if (weapon == null)
		{
			return list;
		}
		if (weapon.RelevantSkill == DefaultSkills.Bow)
		{
			list.Add(("WeaponFlagIcons\\bow", GameTexts.FindText("str_inventory_flag_bow")));
		}
		if (weapon.RelevantSkill == DefaultSkills.Crossbow)
		{
			list.Add(("WeaponFlagIcons\\crossbow", GameTexts.FindText("str_inventory_flag_crossbow")));
		}
		if (weapon.RelevantSkill == DefaultSkills.Polearm)
		{
			list.Add(("WeaponFlagIcons\\polearm", GameTexts.FindText("str_inventory_flag_polearm")));
		}
		if (weapon.RelevantSkill == DefaultSkills.OneHanded)
		{
			list.Add(("WeaponFlagIcons\\one_handed", GameTexts.FindText("str_inventory_flag_one_handed")));
		}
		if (weapon.RelevantSkill == DefaultSkills.TwoHanded)
		{
			list.Add(("WeaponFlagIcons\\two_handed", GameTexts.FindText("str_inventory_flag_two_handed")));
		}
		if (weapon.RelevantSkill == DefaultSkills.Throwing)
		{
			list.Add(("WeaponFlagIcons\\throwing", GameTexts.FindText("str_inventory_flag_throwing")));
		}
		List<(string, TextObject)> weaponFlagDetails = GetWeaponFlagDetails(weapon.WeaponFlags, character);
		list.AddRange(weaponFlagDetails);
		List<(string, TextObject)> itemUsageSetFlagDetails = GetItemUsageSetFlagDetails(itemUsageFlags, character);
		list.AddRange(itemUsageSetFlagDetails);
		string weaponDescriptionId = weapon.WeaponDescriptionId;
		if (weaponDescriptionId != null && weaponDescriptionId.IndexOf("couch", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			list.Add(("WeaponFlagIcons\\can_couchable", GameTexts.FindText("str_inventory_flag_couchable")));
		}
		string weaponDescriptionId2 = weapon.WeaponDescriptionId;
		if (weaponDescriptionId2 != null && weaponDescriptionId2.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			list.Add(("WeaponFlagIcons\\braceable", GameTexts.FindText("str_inventory_flag_braceable")));
		}
		return list;
	}

	public static string GetFormattedItemPropertyText(float propertyValue, bool typeRequiresInteger)
	{
		bool flag = propertyValue >= 100f || (propertyValue % 1f).ApproximatelyEqualsTo(0f, 0.001f);
		if (!(typeRequiresInteger || flag))
		{
			if (!(propertyValue < 10f))
			{
				return propertyValue.ToString("F1");
			}
			return propertyValue.ToString("F2");
		}
		return propertyValue.ToString("F0");
	}

	public static List<TooltipProperty> GetCraftingHeroTooltip(Hero hero, CraftingOrder order)
	{
		bool num = order != null && !order.IsOrderAvailableForHero(hero);
		ICraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		List<TooltipProperty> list = new List<TooltipProperty>();
		string propertyName = (num ? GameTexts.FindText("str_crafting_hero_can_not_craft_item").ToString() : hero.Name.ToString());
		TooltipAddPropertyTitle(list, propertyName);
		if (num)
		{
			List<Hero> list2 = (from h in CraftingHelper.GetAvailableHeroesForCrafting()
				where order.IsOrderAvailableForHero(h)
				select h).ToList();
			if (list2.Count > 0)
			{
				GameTexts.SetVariable("SKILL", GameTexts.FindText("str_crafting").ToString());
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_hero_not_enough_skills").ToString(), "", 0));
				TooltipAddEmptyLine(list);
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_following_can_craft_order").ToString(), "", 0));
				for (int num2 = 0; num2 < list2.Count; num2++)
				{
					Hero hero2 = list2[num2];
					GameTexts.SetVariable("HERO_NAME", hero2.Name);
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_hero_able_to_craft").ToString(), "", 0));
				}
			}
			else
			{
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_one_can_craft_order").ToString(), "", 0));
			}
		}
		else
		{
			list.Add(new TooltipProperty(new TextObject("{=cUUI8u2G}Smithy Stamina").ToString(), campaignBehavior.GetHeroCraftingStamina(hero) + " / " + campaignBehavior.GetMaxHeroCraftingStamina(hero), 0));
			list.Add(new TooltipProperty(new TextObject("{=lVuGCYPC}Smithing Skill").ToString(), hero.GetSkillValue(DefaultSkills.Crafting).ToString(), 0));
			list.Add(new TooltipProperty(new TextObject("{=ZU4dlbym}Click to change the smith").ToString(), "", 0));
		}
		return list;
	}

	public static List<TooltipProperty> GetOrderCannotBeCompletedReasonTooltip(CraftingOrder order, ItemObject item)
	{
		List<TooltipProperty> properties = new List<TooltipProperty>();
		TooltipAddPropertyTitle(properties, new TextObject("{=Syha8biz}Order Can Not Be Completed").ToString());
		properties.Add(new TooltipProperty(new TextObject("{=gTbE6t9I}Following requirements are not met:").ToString(), "", 0));
		float num = 0f;
		float num2 = 0f;
		if (order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamageType != item.PrimaryWeapon.SwingDamageType)
		{
			DamageTypes swingDamageType = order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamageType;
			TextObject textObject;
			if (item.PrimaryWeapon.ThrustDamageType == DamageTypes.Invalid)
			{
				textObject = TextObject.GetEmpty();
			}
			else
			{
				textObject = new TextObject("{=MT5A04X8} - Swing Damage Type does not match. Should be: {TYPE}");
				TextObject textObject2 = textObject;
				int num3 = (int)swingDamageType;
				textObject2.SetTextVariable("TYPE", GameTexts.FindText("str_inventory_dmg_type", num3.ToString()));
			}
			properties.Add(new TooltipProperty(textObject.ToString(), "", 0));
		}
		if (order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamageType != item.PrimaryWeapon.ThrustDamageType)
		{
			DamageTypes thrustDamageType = order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamageType;
			TextObject textObject3;
			if (item.PrimaryWeapon.ThrustDamageType == DamageTypes.Invalid)
			{
				textObject3 = TextObject.GetEmpty();
			}
			else
			{
				textObject3 = new TextObject("{=Tx9Mynbt} - Thrust Damage Type does not match. Should be: {TYPE}");
				TextObject textObject4 = textObject3;
				int num3 = (int)thrustDamageType;
				textObject4.SetTextVariable("TYPE", GameTexts.FindText("str_inventory_dmg_type", num3.ToString()).ToString());
			}
			properties.Add(new TooltipProperty(textObject3.ToString(), "", 0));
		}
		num = order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustSpeed;
		num2 = item.PrimaryWeapon.ThrustSpeed;
		if (num > num2)
		{
			AddProperty(CraftingTemplate.CraftingStatTypes.ThrustSpeed, num);
		}
		num = order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingSpeed;
		num2 = item.PrimaryWeapon.SwingSpeed;
		if (num > num2)
		{
			AddProperty(CraftingTemplate.CraftingStatTypes.SwingSpeed, num);
		}
		num = order.PreCraftedWeaponDesignItem.PrimaryWeapon.MissileSpeed;
		num2 = item.PrimaryWeapon.MissileSpeed;
		if (num > num2)
		{
			AddProperty(CraftingTemplate.CraftingStatTypes.MissileSpeed, num);
		}
		num = order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamage;
		num2 = item.PrimaryWeapon.ThrustDamage;
		if (num > num2)
		{
			AddProperty(CraftingTemplate.CraftingStatTypes.ThrustDamage, num);
		}
		num = order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamage;
		num2 = item.PrimaryWeapon.SwingDamage;
		if (num > num2)
		{
			AddProperty(CraftingTemplate.CraftingStatTypes.SwingDamage, num);
		}
		num = order.PreCraftedWeaponDesignItem.PrimaryWeapon.Accuracy;
		num2 = item.PrimaryWeapon.Accuracy;
		if (num > num2)
		{
			AddProperty(CraftingTemplate.CraftingStatTypes.Accuracy, num);
		}
		num = order.PreCraftedWeaponDesignItem.PrimaryWeapon.Handling;
		num2 = item.PrimaryWeapon.Handling;
		if (num > num2)
		{
			AddProperty(CraftingTemplate.CraftingStatTypes.Handling, num);
		}
		bool flag = true;
		WeaponDescription[] weaponDescriptions = order.PreCraftedWeaponDesignItem.WeaponDesign.Template.WeaponDescriptions;
		foreach (WeaponDescription weaponDescription in weaponDescriptions)
		{
			if (item.WeaponDesign.Template.WeaponDescriptions.All((WeaponDescription d) => d.WeaponClass != weaponDescription.WeaponClass))
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			properties.Add(new TooltipProperty(new TextObject("{=Q1KwpZYu}Weapon usage does not match requirements").ToString(), "", 0));
		}
		return properties;
		void AddProperty(CraftingTemplate.CraftingStatTypes type, float reqValue)
		{
			TextObject textObject5 = GameTexts.FindText("str_crafting_stat", type.ToString());
			TextObject variable = GameTexts.FindText("str_inventory_dmg_type", ((int)order.PreCraftedWeaponDesignItem.PrimaryWeapon.ThrustDamageType).ToString());
			textObject5.SetTextVariable("THRUST_DAMAGE_TYPE", variable);
			TextObject variable2 = GameTexts.FindText("str_inventory_dmg_type", ((int)order.PreCraftedWeaponDesignItem.PrimaryWeapon.SwingDamageType).ToString());
			textObject5.SetTextVariable("SWING_DAMAGE_TYPE", variable2);
			_orderRequirementText.SetTextVariable("STAT", textObject5);
			_orderRequirementText.SetTextVariable("REQUIREMENT", reqValue);
			properties.Add(new TooltipProperty(_orderRequirementText.ToString(), "", 0));
		}
	}

	public static List<TooltipProperty> GetCraftingOrderDisabledReasonTooltip(Hero heroToCheck, CraftingOrder order)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (order.IsOrderAvailableForHero(heroToCheck))
		{
			return list;
		}
		GameTexts.SetVariable("SKILL", GameTexts.FindText("str_crafting").ToString());
		TooltipAddPropertyTitle(list, GameTexts.FindText("str_crafting_cannot_be_crafted").ToString());
		if (!order.IsOrderAvailableForHero(heroToCheck))
		{
			List<Hero> list2 = (from h in CraftingHelper.GetAvailableHeroesForCrafting()
				where order.IsOrderAvailableForHero(h)
				select h).ToList();
			if (list2.Count > 0)
			{
				GameTexts.SetVariable("HERO", heroToCheck.Name.ToString());
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_player_not_enough_skills").ToString(), "", 0));
				TooltipAddEmptyLine(list);
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_following_can_craft_order").ToString(), "", 0));
				for (int num = 0; num < list2.Count; num++)
				{
					Hero hero = list2[num];
					GameTexts.SetVariable("HERO_NAME", hero.Name);
					list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_hero_able_to_craft").ToString(), "", 0));
				}
			}
			else
			{
				int content = TaleWorlds.Library.MathF.Ceiling(order.OrderDifficulty) - heroToCheck.GetSkillValue(DefaultSkills.Crafting) - 50;
				GameTexts.SetVariable("AMOUNT", content);
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_one_can_craft_order").ToString(), "", 0));
			}
		}
		return list;
	}

	public static List<TooltipProperty> GetOrdersDisabledReasonTooltip(MBBindingList<CraftingOrderItemVM> craftingOrders, Hero heroToCheck)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (craftingOrders != null && craftingOrders.Count((CraftingOrderItemVM x) => x.IsEnabled) > 0)
		{
			return list;
		}
		bool flag = false;
		TooltipAddPropertyTitle(list, GameTexts.FindText("str_crafting_cannot_complete_orders").ToString());
		GameTexts.SetVariable("HERO_NAME", heroToCheck.Name);
		list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_available_orders_for_hero").ToString(), "", 0));
		TooltipAddEmptyLine(list);
		IEnumerable<Hero> availableHeroesForCrafting = CraftingHelper.GetAvailableHeroesForCrafting();
		for (int num = 0; num < availableHeroesForCrafting.Count(); num++)
		{
			Hero hero = availableHeroesForCrafting.ToList()[num];
			int num2 = craftingOrders.Count((CraftingOrderItemVM x) => x.CraftingOrder.IsOrderAvailableForHero(hero));
			if (num2 > 0)
			{
				flag = true;
				GameTexts.SetVariable("HERO_NAME", hero.Name);
				GameTexts.SetVariable("NUMBER", num2);
				list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_available_orders_for_other_hero").ToString(), "", 0));
			}
		}
		if (!flag)
		{
			GameTexts.SetVariable("SKILL", GameTexts.FindText("str_crafting").ToString());
			list.Add(new TooltipProperty(GameTexts.FindText("str_crafting_no_available_orders_for_party").ToString(), "", 0));
		}
		return list;
	}

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
			return GameTexts.FindText("str_crafting_should_have_swing_damage").SetTextVariable("SWING_DAMAGE_TYPE", variable).ToString();
		}
		if (!flag2)
		{
			return GameTexts.FindText("str_crafting_should_have_thrust_damage").SetTextVariable("THRUST_DAMAGE_TYPE", variable).ToString();
		}
		return string.Empty;
	}

	public static List<TooltipProperty> GetInventoryCharacterTooltip(Hero hero)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		_inventorySkillTooltipTitle.SetTextVariable("HERO_NAME", hero.Name);
		TooltipAddPropertyTitle(list, _inventorySkillTooltipTitle.ToString());
		TooltipAddDoubleSeperator(list);
		for (int i = 0; i < Skills.All.Count; i++)
		{
			SkillObject skillObject = Skills.All[i];
			list.Add(new TooltipProperty(skillObject.Name.ToString(), hero.GetSkillValue(skillObject).ToString(), 0));
		}
		return list;
	}

	public static string GetHeroOccupationName(Hero hero)
	{
		string text = "";
		if (hero.IsWanderer)
		{
			text = "str_wanderer";
		}
		else if (hero.IsGangLeader)
		{
			text = "str_gang_leader";
		}
		else if (hero.IsPreacher)
		{
			text = "str_preacher";
		}
		else if (hero.IsMerchant)
		{
			text = "str_merchant";
		}
		else
		{
			Clan clan = hero.Clan;
			if (clan != null && clan.IsClanTypeMercenary)
			{
				text = "str_mercenary";
			}
			else if (hero.IsArtisan)
			{
				text = "str_artisan";
			}
			else if (hero.IsRuralNotable)
			{
				text = "str_charactertype_ruralnotable";
			}
			else if (hero.IsHeadman)
			{
				text = "str_charactertype_headman";
			}
			else if (hero.IsMinorFactionHero)
			{
				text = "str_charactertype_minorfaction";
			}
			else
			{
				if (!hero.IsLord)
				{
					return "";
				}
				text = ((!hero.IsFemale) ? "str_charactertype_lord" : "str_charactertype_lady");
			}
		}
		return GameTexts.FindText(text).ToString();
	}

	private static TooltipProperty GetSiegeMachineProgressLine(int hoursRemaining)
	{
		if (hoursRemaining > 0)
		{
			string text = GetHoursAndDaysTextFromHourValue(hoursRemaining).ToString();
			MBTextManager.SetTextVariable("PREPARATION_TIME", text);
			string value = GameTexts.FindText("str_preparations_complete_in_hours").ToString();
			return new TooltipProperty(" ", value, 0);
		}
		return null;
	}

	public static TextObject GetCommaSeparatedText(TextObject label, IEnumerable<TextObject> texts)
	{
		TextObject textObject = new TextObject("{=!}{RESULT}");
		int num = 0;
		foreach (TextObject text3 in texts)
		{
			if (num == 0)
			{
				MBTextManager.SetTextVariable("STR1", label ?? TextObject.GetEmpty());
				MBTextManager.SetTextVariable("STR2", text3);
				string text = GameTexts.FindText("str_STR1_STR2").ToString();
				MBTextManager.SetTextVariable("LEFT", text);
				textObject.SetTextVariable("RESULT", text);
			}
			else
			{
				MBTextManager.SetTextVariable("RIGHT", text3);
				string text2 = GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
				MBTextManager.SetTextVariable("LEFT", text2);
				textObject.SetTextVariable("RESULT", text2);
			}
			num++;
		}
		return textObject;
	}

	public static TextObject GetCommaNewlineSeparatedText(TextObject label, IEnumerable<TextObject> texts)
	{
		TextObject textObject = new TextObject("{=!}{RESULT}");
		int num = 0;
		foreach (TextObject text3 in texts)
		{
			if (num == 0)
			{
				MBTextManager.SetTextVariable("STR1", label ?? TextObject.GetEmpty());
				MBTextManager.SetTextVariable("STR2", text3);
				string text = GameTexts.FindText("str_STR1_STR2").ToString();
				MBTextManager.SetTextVariable("LEFT", text);
				textObject.SetTextVariable("RESULT", text);
			}
			else
			{
				MBTextManager.SetTextVariable("RIGHT", text3);
				string text2 = GameTexts.FindText("str_LEFT_comma_newline_RIGHT").ToString();
				MBTextManager.SetTextVariable("newline", "\n");
				MBTextManager.SetTextVariable("LEFT", text2);
				textObject.SetTextVariable("RESULT", text2);
			}
			num++;
		}
		return textObject;
	}

	public static string GetHeroKingdomRank(Hero hero)
	{
		if (hero.Clan.Kingdom != null)
		{
			bool isUnderMercenaryService = hero.Clan.IsUnderMercenaryService;
			bool num = hero == hero.Clan.Kingdom.Leader;
			bool flag = hero.Clan.Leader == hero;
			bool flag2 = !num && !flag;
			bool flag3 = hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero == hero;
			TextObject textObject = TextObject.GetEmpty();
			GameTexts.SetVariable("FACTION", hero.Clan.Kingdom.Name);
			GameTexts.SetVariable("FACTION_INFORMAL_NAME", hero.Clan.Kingdom.InformalName);
			if (num)
			{
				textObject = GameTexts.FindText("str_hero_rank_of_faction", 1.ToString());
			}
			else if (isUnderMercenaryService)
			{
				textObject = GameTexts.FindText("str_hero_rank_of_faction_mercenary");
			}
			else if (flag || flag3)
			{
				textObject = GameTexts.FindText("str_hero_rank_of_faction", 0.ToString());
			}
			else if (flag2)
			{
				textObject = GameTexts.FindText("str_hero_rank_of_faction_nobleman");
			}
			textObject.SetCharacterProperties("HERO", hero.CharacterObject);
			return textObject.ToString();
		}
		return string.Empty;
	}

	public static string GetHeroRank(Hero hero)
	{
		if (hero.Clan != null)
		{
			bool isUnderMercenaryService = hero.Clan.IsUnderMercenaryService;
			bool num = hero == hero.Clan.Kingdom?.Leader;
			bool flag = hero.Clan.Leader == hero && hero.Clan.Kingdom != null;
			bool flag2 = !num && !flag && hero.Clan.Kingdom != null;
			if (num)
			{
				return GameTexts.FindText("str_hero_rank", 1.ToString()).ToString();
			}
			if (isUnderMercenaryService)
			{
				return GameTexts.FindText("str_hero_rank_mercenary").ToString();
			}
			if (flag)
			{
				return GameTexts.FindText("str_hero_rank", 0.ToString()).ToString();
			}
			if (flag2)
			{
				return GameTexts.FindText("str_hero_rank_nobleman").ToString();
			}
		}
		return string.Empty;
	}

	public static List<TooltipProperty> GetSmithingDifficultyTooltip()
	{
		return new List<TooltipProperty>
		{
			new TooltipProperty("", new TextObject("{=JfmiJEHG}Smithing Difficulty").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title),
			new TooltipProperty("", new TextObject("{=w1jJqTaJ}Determines how difficult the design is to forge. Difficulties lower than the Smithing skill of the forger will produce at least adequate results while difficulties higher than the Smithing skill of the forger will use more stamina and may result in poor craftsmanship.").ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine)
		};
	}

	public static bool IsSettlementInformationHidden(Settlement settlement, out TextObject disableReason)
	{
		bool flag = !Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(settlement);
		disableReason = (flag ? new TextObject("{=cDkHJOkl}You need to be in the viewing range, control this settlement with your kingdom or have a clan member in the settlement to see its details.") : TextObject.GetEmpty());
		return flag;
	}

	public static bool IsHeroInformationHidden(Hero hero, out TextObject disableReason)
	{
		bool flag = !Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(hero);
		disableReason = (flag ? new TextObject("{=akHsjtPh}You haven't met this hero yet.") : TextObject.GetEmpty());
		return flag;
	}

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
		string abbreviatedValueTextFromValue = GetAbbreviatedValueTextFromValue(num);
		string abbreviatedValueTextFromValue2 = GetAbbreviatedValueTextFromValue(num2);
		return abbreviatedValueTextFromValue + ((num2 > 0) ? (" + " + abbreviatedValueTextFromValue2 + GameTexts.FindText("str_party_nameplate_wounded_abbr").ToString()) : "");
	}

	public static string GetPartyNameplateText(PartyBase party)
	{
		int totalHealthyCount = party.MemberRoster.TotalHealthyCount;
		int totalWounded = party.MemberRoster.TotalWounded;
		string abbreviatedValueTextFromValue = GetAbbreviatedValueTextFromValue(totalHealthyCount);
		string abbreviatedValueTextFromValue2 = GetAbbreviatedValueTextFromValue(totalWounded);
		return abbreviatedValueTextFromValue + ((totalWounded > 0) ? (" + " + abbreviatedValueTextFromValue2 + GameTexts.FindText("str_party_nameplate_wounded_abbr").ToString()) : "");
	}

	public static string GetValueChangeText(float originalValue, float valueChange, string valueFormat = "F0")
	{
		string text = originalValue.ToString(valueFormat);
		TextObject textObject = GameTexts.FindText("str_clan_workshop_material_daily_Change").SetTextVariable("IS_POSITIVE", (valueChange >= 0f) ? 1 : 0).SetTextVariable("CHANGE", TaleWorlds.Library.MathF.Abs(valueChange).ToString(valueFormat));
		TextObject textObject2 = GameTexts.FindText("str_STR_in_parentheses");
		textObject2.SetTextVariable("STR", textObject.ToString());
		return GameTexts.FindText("str_STR1_space_STR2").SetTextVariable("STR1", text.ToString()).SetTextVariable("STR2", textObject2.ToString())
			.ToString();
	}

	public static string GetUpgradeHint(int index, int numOfItems, int availableUpgrades, int upgradeCoinCost, bool hasRequiredPerk, PerkObject requiredPerk, CharacterObject character, TroopRosterElement troop, int partyGoldChangeAmount, bool areUpgradesDisabled)
	{
		if (areUpgradesDisabled)
		{
			return new TextObject("{=R4rTlKMU}Troop upgrades are currently disabled.").ToString();
		}
		string text = null;
		CharacterObject characterObject = character.UpgradeTargets[index];
		int level = characterObject.Level;
		if (character.Culture.IsBandit ? (level >= character.Level) : (level > character.Level))
		{
			int upgradeXpCost = character.GetUpgradeXpCost(PartyBase.MainParty, index);
			GameTexts.SetVariable("newline", "\n");
			TextObject textObject = new TextObject("{=f4nc7FfE}Upgrade to {UPGRADE_NAME}");
			textObject.SetTextVariable("UPGRADE_NAME", characterObject.Name);
			text = textObject.ToString();
			if (troop.Xp < upgradeXpCost)
			{
				TextObject textObject2 = new TextObject("{=Voa0sinH}Required: {NEEDED_EXP_AMOUNT}xp (You have {CURRENT_EXP_AMOUNT})");
				textObject2.SetTextVariable("NEEDED_EXP_AMOUNT", upgradeXpCost);
				textObject2.SetTextVariable("CURRENT_EXP_AMOUNT", troop.Xp);
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", textObject2);
				text = GameTexts.FindText("str_string_newline_string").ToString();
			}
			if (characterObject.UpgradeRequiresItemFromCategory != null)
			{
				TextObject textObject3 = new TextObject((numOfItems > 0) ? "{=Raa4j4rF}Required: {UPGRADE_ITEM}" : "{=rThSy9ed}Required: {UPGRADE_ITEM} (You have none)");
				textObject3.SetTextVariable("UPGRADE_ITEM", characterObject.UpgradeRequiresItemFromCategory.GetName().ToString());
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", textObject3.ToString());
				text = GameTexts.FindText("str_string_newline_string").ToString();
			}
			TextObject textObject4 = new TextObject((Hero.MainHero.Gold + partyGoldChangeAmount < upgradeCoinCost) ? "{=63Ic1Ahe}Cost: {UPGRADE_COST} (You don't have)" : "{=McJjNM50}Cost: {UPGRADE_COST}");
			textObject4.SetTextVariable("UPGRADE_COST", upgradeCoinCost);
			GameTexts.SetVariable("STR1", textObject4);
			GameTexts.SetVariable("STR2", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			string content = GameTexts.FindText("str_STR1_STR2").ToString();
			GameTexts.SetVariable("STR1", text);
			GameTexts.SetVariable("STR2", content);
			text = GameTexts.FindText("str_string_newline_string").ToString();
			if (!hasRequiredPerk)
			{
				GameTexts.SetVariable("STR1", text);
				TextObject textObject5 = new TextObject("{=68IlDbA2}You need to have {PERK_NAME} perk to upgrade a bandit troop to a normal troop.");
				textObject5.SetTextVariable("PERK_NAME", requiredPerk.Name);
				GameTexts.SetVariable("STR2", textObject5);
				text = GameTexts.FindText("str_string_newline_string").ToString();
			}
		}
		return text;
	}

	public static string GetStackModifierString(TextObject allStackText, TextObject fiveStackText, bool canFiveStack)
	{
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			return string.Empty;
		}
		TextObject variable = GameTexts.FindText("str_game_key_text", "anycontrol");
		allStackText.SetTextVariable("KEY_NAME", variable);
		if (canFiveStack)
		{
			TextObject variable2 = GameTexts.FindText("str_game_key_text", "anyshift");
			fiveStackText.SetTextVariable("KEY_NAME", variable2);
			return GameTexts.FindText("str_string_newline_string").SetTextVariable("newline", "\n").SetTextVariable("STR1", allStackText)
				.SetTextVariable("STR2", fiveStackText)
				.ToString();
		}
		return allStackText.ToString();
	}

	public static string ConvertToHexColor(uint color)
	{
		uint num = color % 4278190080u;
		return "#" + Convert.ToString(num, 16).PadLeft(6, '0').ToUpper() + "FF";
	}

	public static bool GetMapScreenActionIsEnabledWithReason(out TextObject disabledReason)
	{
		if (Hero.MainHero.IsPrisoner)
		{
			disabledReason = GameTexts.FindText("str_action_disabled_reason_prisoner");
			return false;
		}
		if (MobileParty.MainParty.IsInRaftState)
		{
			disabledReason = GameTexts.FindText("str_action_disabled_reason_raft_state");
			return false;
		}
		if (CampaignMission.Current != null)
		{
			disabledReason = new TextObject("{=FdzsOvDq}This action is disabled while in a mission");
			return false;
		}
		if (PlayerEncounter.Current != null)
		{
			if (PlayerEncounter.EncounterSettlement == null)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_encounter");
				return false;
			}
			Village village = PlayerEncounter.EncounterSettlement.Village;
			if (village != null && village.VillageState == Village.VillageStates.BeingRaided)
			{
				MapEvent mapEvent = MobileParty.MainParty.MapEvent;
				if (mapEvent != null && mapEvent.IsRaid)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_raid");
					return false;
				}
			}
			if (PlayerEncounter.EncounterSettlement.IsUnderSiege)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_siege");
				return false;
			}
		}
		if (PlayerSiege.PlayerSiegeEvent != null)
		{
			disabledReason = GameTexts.FindText("str_action_disabled_reason_siege");
			return false;
		}
		if (MobileParty.MainParty.MapEvent != null)
		{
			disabledReason = new TextObject("{=MIylzRc5}You can't perform this action while you are in a map event.");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public static bool GetCanManageCurrentArmyWithReason(out TextObject disabledReason)
	{
		if (MobileParty.MainParty.Army?.LeaderParty != MobileParty.MainParty)
		{
			disabledReason = TextObject.GetEmpty();
			return false;
		}
		if (Hero.MainHero.IsPrisoner)
		{
			disabledReason = GameTexts.FindText("str_action_disabled_reason_prisoner");
			return false;
		}
		if (PlayerEncounter.Current != null)
		{
			if (PlayerEncounter.EncounterSettlement == null)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_encounter");
				return false;
			}
			Village village = PlayerEncounter.EncounterSettlement.Village;
			if (village != null && village.VillageState == Village.VillageStates.BeingRaided && MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.IsRaid)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_raid");
				return false;
			}
		}
		if (MapEvent.PlayerMapEvent != null)
		{
			disabledReason = GameTexts.FindText("str_cannot_manage_army_while_in_event");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public static string GetClanSupportDisableReasonString(bool hasEnoughInfluence, bool isTargetMainClan, bool isMainClanMercenary)
	{
		if (Hero.MainHero.IsPrisoner)
		{
			return GameTexts.FindText("str_action_disabled_reason_prisoner").ToString();
		}
		if (isTargetMainClan)
		{
			return GameTexts.FindText("str_cannot_support_your_clan").ToString();
		}
		if (isMainClanMercenary)
		{
			return GameTexts.FindText("str_mercenaries_cannot_support_clans").ToString();
		}
		if (!hasEnoughInfluence)
		{
			return GameTexts.FindText("str_warning_you_dont_have_enough_influence").ToString();
		}
		return null;
	}

	public static string GetClanExpelDisableReasonString(bool hasEnoughInfluence, bool isTargetMainClan, bool isTargetRulingClan, bool isMainClanMercenary)
	{
		if (Hero.MainHero.IsPrisoner)
		{
			return GameTexts.FindText("str_action_disabled_reason_prisoner").ToString();
		}
		if (isMainClanMercenary)
		{
			return GameTexts.FindText("str_mercenaries_cannot_expel_clans").ToString();
		}
		if (isTargetMainClan)
		{
			return GameTexts.FindText("str_cannot_expel_your_clan").ToString();
		}
		if (isTargetRulingClan)
		{
			return GameTexts.FindText("str_cannot_expel_ruling_clan").ToString();
		}
		if (!hasEnoughInfluence)
		{
			return GameTexts.FindText("str_warning_you_dont_have_enough_influence").ToString();
		}
		return null;
	}

	public static string GetArmyDisbandDisableReasonString(bool hasEnoughInfluence, bool isArmyInAnyEvent, bool isPlayerClanMercenary, bool isPlayerInThisArmy)
	{
		if (Hero.MainHero.IsPrisoner)
		{
			return GameTexts.FindText("str_action_disabled_reason_prisoner").ToString();
		}
		if (isPlayerClanMercenary)
		{
			return GameTexts.FindText("str_cannot_disband_army_while_mercenary").ToString();
		}
		if (isArmyInAnyEvent)
		{
			return GameTexts.FindText("str_cannot_disband_army_while_in_event").ToString();
		}
		if (isPlayerInThisArmy)
		{
			return GameTexts.FindText("str_cannot_disband_army_while_in_that_army").ToString();
		}
		if (!hasEnoughInfluence)
		{
			return GameTexts.FindText("str_warning_you_dont_have_enough_influence").ToString();
		}
		return null;
	}

	public static TextObject GetCreateNewPartyReasonString(bool haveEmptyPartySlots, bool haveAvailableHero)
	{
		if (Hero.MainHero.IsPrisoner)
		{
			return GameTexts.FindText("str_action_disabled_reason_prisoner");
		}
		if (!haveEmptyPartySlots)
		{
			return GameTexts.FindText("str_clan_doesnt_have_empty_party_slots");
		}
		if (!haveAvailableHero)
		{
			return GameTexts.FindText("str_clan_doesnt_have_available_heroes");
		}
		return TextObject.GetEmpty();
	}

	public static string GetCraftingDisableReasonString(bool playerHasEnoughMaterials)
	{
		if (!playerHasEnoughMaterials)
		{
			return GameTexts.FindText("str_warning_crafing_materials").ToString();
		}
		return string.Empty;
	}

	public static string GetAddFocusHintString(bool playerHasEnoughPoints, bool isMaxedSkill, int currentFocusAmount)
	{
		GameTexts.SetVariable("newline", "\n");
		string content = GameTexts.FindText("str_focus_points").ToString();
		TextObject textObject = new TextObject("{=j3iwQmoA}Current focus amount: {CURRENT_AMOUNT}");
		textObject.SetTextVariable("CURRENT_AMOUNT", currentFocusAmount);
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("STR2", textObject);
		content = GameTexts.FindText("str_string_newline_string").ToString();
		if (!playerHasEnoughPoints)
		{
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_player_doesnt_have_enough_points"));
			return GameTexts.FindText("str_string_newline_string").ToString();
		}
		if (isMaxedSkill)
		{
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_player_cannot_give_more_points"));
			return GameTexts.FindText("str_string_newline_string").ToString();
		}
		GameTexts.SetVariable("COST", 1);
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_cost_COUNT"));
		return GameTexts.FindText("str_string_newline_string").ToString();
	}

	public static string GetSkillEffectText(SkillEffect effect, int skillLevel)
	{
		TextObject effectDescriptionForSkillLevel = SkillHelper.GetEffectDescriptionForSkillLevel(effect, skillLevel);
		if (effect.Role != PartyRole.None)
		{
			TextObject textObject = GameTexts.FindText("role", effect.Role.ToString());
			return $"• {textObject.ToString()}: {effectDescriptionForSkillLevel} ";
		}
		return effectDescriptionForSkillLevel.ToString();
	}

	public static string GetMobilePartyBehaviorText(MobileParty party)
	{
		return party.GetBehaviorText().ToString();
	}

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
				return GameTexts.FindText("str_prisoner_at_settlement").ToString();
			}
			if (hero.PartyBelongedToAsPrisoner != null)
			{
				_prisonerOfText.SetTextVariable("PARTY_NAME", hero.PartyBelongedToAsPrisoner.Name);
				return _prisonerOfText.ToString();
			}
			return new TextObject("{=tYz4D8Or}Prisoner").ToString();
		}
		if (hero.IsTraveling)
		{
			IMapPoint target = null;
			bool isGovernor = false;
			bool isPartyLeader = false;
			if (teleportationBehavior == null || !teleportationBehavior.GetTargetOfTeleportingHero(hero, out isGovernor, out isPartyLeader, out target))
			{
				return _travelingText.ToString();
			}
			if (isGovernor && target is Settlement settlement)
			{
				TextObject textObject = new TextObject("{=gUUnZNGk}Moving to {SETTLEMENT_NAME} to be the new governor");
				textObject.SetTextVariable("SETTLEMENT_NAME", settlement.Name.ToString());
				return textObject.ToString();
			}
			if (isPartyLeader && target is MobileParty)
			{
				return new TextObject("{=g08mptth}Moving to a party to be the new leader").ToString();
			}
			if (target is MobileParty mobileParty)
			{
				TextObject textObject2 = new TextObject("{=qaQqAYGc}Moving to {LEADER.NAME}{.o} Party");
				if (mobileParty?.LeaderHero?.CharacterObject != null)
				{
					StringHelpers.SetCharacterProperties("LEADER", mobileParty.LeaderHero.CharacterObject, textObject2);
				}
				return textObject2.ToString();
			}
			if (target is Settlement settlement2)
			{
				TextObject textObject3 = new TextObject("{=UUaW0dba}Moving to {SETTLEMENT_NAME}");
				textObject3.SetTextVariable("SETTLEMENT_NAME", settlement2?.Name?.ToString() ?? string.Empty);
				return textObject3.ToString();
			}
		}
		if (hero.PartyBelongedTo != null)
		{
			if (hero.PartyBelongedTo.LeaderHero == hero && hero.PartyBelongedTo.Army != null)
			{
				_attachedToText.SetTextVariable("PARTY_NAME", hero.PartyBelongedTo.Army.Name);
				return _attachedToText.ToString();
			}
			if (hero.PartyBelongedTo == MobileParty.MainParty)
			{
				return _inYourPartyText.ToString();
			}
			Settlement settlement3 = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(hero.PartyBelongedTo.CurrentNavigationFace, hero.PartyBelongedTo.NavigationCapability).Item1;
			if (settlement3 == null)
			{
				float num = float.MaxValue;
				Settlement settlement4 = null;
				foreach (Settlement item in Settlement.All)
				{
					float num2 = item.Position.Distance(hero.PartyBelongedTo.Position);
					if (num2 < num)
					{
						num = num2;
						settlement4 = item;
					}
				}
				settlement3 = settlement4;
			}
			_nearSettlementText.SetTextVariable("SETTLEMENT_NAME", settlement3.Name);
			return _nearSettlementText.ToString();
		}
		if (hero.CurrentSettlement != null)
		{
			if (hero.CurrentSettlement.Town != null && hero.GovernorOf == hero.CurrentSettlement.Town)
			{
				return GameTexts.FindText("str_governing_at_settlement").ToString();
			}
			if (Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().IsHeroAlleyLeaderOfAnyPlayerAlley(hero))
			{
				return GameTexts.FindText("str_alley_leader_at_settlement").ToString();
			}
			return GameTexts.FindText("str_staying_at_settlement").ToString();
		}
		if (Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
		{
			return GameTexts.FindText("str_solving_issue").ToString();
		}
		if (hero.IsFugitive)
		{
			return _regroupingText.ToString();
		}
		if (hero.IsReleased)
		{
			GameTexts.SetVariable("LEFT", _recoveringText);
			GameTexts.SetVariable("RIGHT", _recentlyReleasedText);
			return GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
		}
		return new TextObject("{=RClxLG6N}Holding").ToString();
	}

	public static string GetPartyLocationText(MobileParty mobileParty)
	{
		if (mobileParty.CurrentSettlement != null)
		{
			return mobileParty.CurrentSettlement.Name.ToString();
		}
		Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(mobileParty, MobileParty.NavigationType.All) ?? SettlementHelper.FindNearestSettlementToPoint(mobileParty.Position);
		GameTexts.SetVariable("SETTLEMENT_NAME", settlement.Name);
		return GameTexts.FindText("str_near_settlement").ToString();
	}

	public static Hero GetTeleportingLeaderHero(MobileParty party, ITeleportationCampaignBehavior teleportationBehavior)
	{
		if (party != null && teleportationBehavior != null)
		{
			foreach (Hero item in Hero.MainHero.Clan.Heroes.Where((Hero x) => x.IsAlive && x.IsTraveling))
			{
				if (teleportationBehavior.GetTargetOfTeleportingHero(item, out var _, out var isPartyLeader, out var target) && isPartyLeader && target is MobileParty mobileParty && mobileParty == party)
				{
					return item;
				}
			}
		}
		return null;
	}

	public static Hero GetTeleportingGovernor(Settlement settlement, ITeleportationCampaignBehavior teleportationBehavior)
	{
		if (settlement != null && teleportationBehavior != null)
		{
			foreach (Hero item in Hero.MainHero.Clan.Heroes.Where((Hero x) => x.IsAlive && x.IsTraveling))
			{
				if (teleportationBehavior.GetTargetOfTeleportingHero(item, out var isGovernor, out var _, out var target) && isGovernor && target is Settlement settlement2 && settlement2 == settlement)
				{
					return item;
				}
			}
		}
		return null;
	}

	public static TextObject GetHeroRelationToHeroText(Hero queriedHero, Hero baseHero, bool uppercaseFirst)
	{
		GameTexts.SetVariable("RELATION_TEXT", ConversationHelper.GetHeroRelationToHeroTextShort(queriedHero, baseHero, uppercaseFirst));
		StringHelpers.SetCharacterProperties("BASE_HERO", baseHero.CharacterObject);
		return GameTexts.FindText("str_hero_family_relation");
	}

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
			variable = new TextObject("{=thousandabbr}k").ToString();
			num /= 1000m;
		}
		else if (valueAmount >= 1000000 && valueAmount < 1000000000)
		{
			variable = new TextObject("{=millionabbr}m").ToString();
			num /= 1000000m;
		}
		else if (valueAmount >= 1000000000 && valueAmount <= int.MaxValue)
		{
			variable = new TextObject("{=billionabbr}b").ToString();
			num /= 1000000000m;
		}
		int num2 = (int)num;
		string text = num2.ToString();
		if (text.Length < 3)
		{
			text += ".";
			string text2 = num.ToString("F3").Split(new char[1] { '.' }).ElementAtOrDefault(1);
			if (text2 != null)
			{
				for (int i = 0; i < 3 - num2.ToString().Length; i++)
				{
					if (text2.ElementAtOrDefault(i) != 0)
					{
						text += text2.ElementAtOrDefault(i);
					}
				}
			}
		}
		_denarValueInfoText.SetTextVariable("DENAR_AMOUNT", text);
		_denarValueInfoText.SetTextVariable("VALUE_ABBREVIATION", variable);
		return _denarValueInfoText.ToString();
	}

	public static string GetPartyDistanceByTimeText(float distance, float speed)
	{
		int num = TaleWorlds.Library.MathF.Ceiling(distance / speed);
		int num2 = num / CampaignTime.HoursInDay;
		num %= CampaignTime.HoursInDay;
		GameTexts.SetVariable("IS_UNDER_A_DAY", (num2 <= 0) ? 1 : 0);
		GameTexts.SetVariable("IS_MORE_THAN_ONE_DAY", (num2 > 1) ? 1 : 0);
		GameTexts.SetVariable("DAY_VALUE", num2);
		GameTexts.SetVariable("IS_UNDER_ONE_HOUR", (num <= 0) ? 1 : 0);
		GameTexts.SetVariable("IS_MORE_THAN_AN_HOUR", (num > 1) ? 1 : 0);
		GameTexts.SetVariable("HOUR_VALUE", num);
		return GameTexts.FindText("str_distance_by_time").ToString();
	}

	public static string GetPartyDistanceByTimeTextAbbreviated(float distance, float speed)
	{
		int num = TaleWorlds.Library.MathF.Ceiling(distance / speed);
		int num2 = num / CampaignTime.HoursInDay;
		num %= CampaignTime.HoursInDay;
		if (num2 < 0 || num < 0)
		{
			return "-";
		}
		GameTexts.SetVariable("DAY_VALUE", num2);
		GameTexts.SetVariable("HOUR_VALUE", num);
		return GameTexts.FindText("str_distance_by_time_abbreviated").ToString();
	}

	public static CharacterCode GetCharacterCode(CharacterObject character, bool useCivilian = false)
	{
		if (character == null || (character.IsHero && IsHeroInformationHidden(character.HeroObject, out var _)))
		{
			return CharacterCode.CreateEmpty();
		}
		uint color = character.HeroObject?.MapFaction?.Color ?? ((character.Culture != null) ? character.Culture.Color : Color.White.ToUnsignedInteger());
		uint color2 = character.HeroObject?.MapFaction?.Color2 ?? ((character.Culture != null) ? character.Culture.Color2 : Color.White.ToUnsignedInteger());
		string empty = string.Empty;
		BodyProperties bodyProperties = character.GetBodyProperties(character.Equipment);
		useCivilian = useCivilian || (character.HeroObject?.IsNoncombatant ?? false);
		if (character.IsHero && character.HeroObject.IsLord)
		{
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
			empty = equipment.CalculateEquipmentCode();
		}
		else
		{
			empty = ((useCivilian && character.FirstCivilianEquipment != null) ? character.FirstCivilianEquipment.Clone() : character.FirstBattleEquipment.Clone()).CalculateEquipmentCode();
		}
		return CharacterCode.CreateFrom(empty, bodyProperties, character.IsFemale, character.IsHero, color, color2, character.DefaultFormationClass, character.Race);
	}

	public static string GetTraitNameText(TraitObject traitObject, Hero hero)
	{
		if (traitObject != DefaultTraits.Mercy && traitObject != DefaultTraits.Valor && traitObject != DefaultTraits.Honor && traitObject != DefaultTraits.Generosity && traitObject != DefaultTraits.Calculating)
		{
			Debug.FailedAssert("Cannot show this trait as text.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CampaignUIHelper.cs", "GetTraitNameText", 3326);
			return "";
		}
		int traitLevel = hero.GetTraitLevel(traitObject);
		if (traitLevel != 0)
		{
			return GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (traitLevel + TaleWorlds.Library.MathF.Abs(traitObject.MinValue)).ToString()).ToString();
		}
		return "";
	}

	public static string GetTraitTooltipText(TraitObject traitObject, int traitValue)
	{
		if (traitObject != DefaultTraits.Mercy && traitObject != DefaultTraits.Valor && traitObject != DefaultTraits.Honor && traitObject != DefaultTraits.Generosity && traitObject != DefaultTraits.Calculating)
		{
			Debug.FailedAssert("Cannot show this trait's tooltip.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CampaignUIHelper.cs", "GetTraitTooltipText", 3351);
			return null;
		}
		GameTexts.SetVariable("NEWLINE", "\n");
		if (traitValue != 0)
		{
			TextObject content = GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (traitValue + TaleWorlds.Library.MathF.Abs(traitObject.MinValue)).ToString());
			GameTexts.SetVariable("TRAIT_VALUE", traitValue);
			GameTexts.SetVariable("TRAIT_NAME", content);
			TextObject content2 = GameTexts.FindText("str_trait", traitObject.StringId.ToLower());
			GameTexts.SetVariable("TRAIT", content2);
			GameTexts.SetVariable("TRAIT_DESCRIPTION", traitObject.Description);
			return GameTexts.FindText("str_trait_tooltip").ToString();
		}
		TextObject content3 = GameTexts.FindText("str_trait", traitObject.StringId.ToLower());
		GameTexts.SetVariable("TRAIT", content3);
		GameTexts.SetVariable("TRAIT_DESCRIPTION", traitObject.Description);
		return GameTexts.FindText("str_trait_description_tooltip").ToString();
	}

	public static string GetTextForRole(PartyRole role)
	{
		return role switch
		{
			PartyRole.None => GameTexts.FindText("role", PartyRole.None.ToString()).ToString(), 
			PartyRole.ArmyCommander => GameTexts.FindText("role", PartyRole.ArmyCommander.ToString()).ToString(), 
			PartyRole.ClanLeader => GameTexts.FindText("role", PartyRole.ClanLeader.ToString()).ToString(), 
			PartyRole.Engineer => GameTexts.FindText("role", PartyRole.Engineer.ToString()).ToString(), 
			PartyRole.Governor => GameTexts.FindText("role", PartyRole.Governor.ToString()).ToString(), 
			PartyRole.PartyLeader => GameTexts.FindText("role", PartyRole.PartyLeader.ToString()).ToString(), 
			PartyRole.PartyMember => GameTexts.FindText("role", PartyRole.PartyMember.ToString()).ToString(), 
			PartyRole.PartyOwner => GameTexts.FindText("role", PartyRole.PartyOwner.ToString()).ToString(), 
			PartyRole.Personal => GameTexts.FindText("role", PartyRole.Personal.ToString()).ToString(), 
			PartyRole.Quartermaster => GameTexts.FindText("role", PartyRole.Quartermaster.ToString()).ToString(), 
			PartyRole.Ruler => GameTexts.FindText("role", PartyRole.Ruler.ToString()).ToString(), 
			PartyRole.Scout => GameTexts.FindText("role", PartyRole.Scout.ToString()).ToString(), 
			PartyRole.Surgeon => GameTexts.FindText("role", PartyRole.Surgeon.ToString()).ToString(), 
			_ => "", 
		};
	}

	public static int GetAttributeTypeSortIndex(CharacterAttribute attribute)
	{
		string stringId = attribute.StringId;
		for (int i = 0; i < _attributeSortIndices.Count; i++)
		{
			if (stringId.Equals(_attributeSortIndices[i], StringComparison.InvariantCultureIgnoreCase))
			{
				return _attributeSortIndices.Count - i;
			}
		}
		return 0;
	}

	public static int GetSkillObjectTypeSortIndex(SkillObject skill)
	{
		string stringId = skill.StringId;
		for (int i = 0; i < _skillSortIndices.Count; i++)
		{
			if (stringId.Equals(_skillSortIndices[i], StringComparison.InvariantCultureIgnoreCase))
			{
				return _skillSortIndices.Count - i;
			}
		}
		return 0;
	}

	public static string GetSkillMeshId(SkillObject skill, bool useSmallestVariation = true)
	{
		string text = "SPGeneral\\Skills\\gui_skills_icon_" + skill.StringId.ToLower();
		if (useSmallestVariation)
		{
			return text + "_tiny";
		}
		return text + "_small";
	}

	public static bool GetIsNavalSkill(SkillObject skill)
	{
		string stringId = skill.StringId;
		for (int i = 0; i < _navalSkills.Count; i++)
		{
			if (stringId.Equals(_navalSkills[i], StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public static int GetHeroCompareSortIndex(Hero x, Hero y)
	{
		int num = 0;
		num = ((x.Clan != null || y.Clan != null) ? ((x.Clan == null) ? 1 : ((y.Clan == null) ? (-1) : ((x.IsLord && !y.IsLord) ? (-1) : ((!x.IsLord && y.IsLord) ? 1 : (-x.Clan.Renown.CompareTo(y.Clan.Renown)))))) : 0);
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

	public static string GetHeroClanRoleText(Hero hero, Clan clan)
	{
		List<PartyRole> heroPartyRoles = MobileParty.MainParty.GetHeroPartyRoles(hero);
		if (heroPartyRoles.Count == 1)
		{
			return GameTexts.FindText("role", heroPartyRoles.First().ToString()).ToString();
		}
		List<TextObject> list = new List<TextObject>();
		foreach (PartyRole item in heroPartyRoles)
		{
			list.Add(GameTexts.FindText("role", item.ToString()));
		}
		return GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, includeAnd: true).ToString();
	}

	public static int GetItemObjectTypeSortIndex(ItemObject item)
	{
		if (item == null)
		{
			return -1;
		}
		int num = _itemObjectTypeSortIndices.IndexOf(item.Type) * 100;
		switch (item.Type)
		{
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
		default:
			return 1;
		}
	}

	public static string GetItemLockStringID(EquipmentElement equipmentElement)
	{
		return equipmentElement.Item.StringId + ((equipmentElement.ItemModifier != null) ? equipmentElement.ItemModifier.StringId : "");
	}

	public static string GetTroopLockStringID(TroopRosterElement rosterElement)
	{
		return rosterElement.Character.StringId;
	}

	public static List<(IssueQuestFlags, TextObject, TextObject)> GetQuestStateOfHero(Hero queriedHero)
	{
		List<(IssueQuestFlags, TextObject, TextObject)> list = new List<(IssueQuestFlags, TextObject, TextObject)>();
		if (Campaign.Current != null)
		{
			Campaign.Current.IssueManager.Issues.TryGetValue(queriedHero, out var relatedIssue);
			if (relatedIssue == null)
			{
				relatedIssue = queriedHero.Issue;
			}
			List<QuestBase> questsRelatedToHero = GetQuestsRelatedToHero(queriedHero);
			if (questsRelatedToHero.Count > 0)
			{
				for (int i = 0; i < questsRelatedToHero.Count; i++)
				{
					if (questsRelatedToHero[i].QuestGiver == queriedHero)
					{
						list.Add((questsRelatedToHero[i].IsSpecialQuest ? IssueQuestFlags.ActiveStoryQuest : IssueQuestFlags.ActiveIssue, questsRelatedToHero[i].Title, (questsRelatedToHero[i].JournalEntries.Count > 0) ? questsRelatedToHero[i].JournalEntries[0].LogText : TextObject.GetEmpty()));
					}
					else
					{
						list.Add((questsRelatedToHero[i].IsSpecialQuest ? IssueQuestFlags.TrackedStoryQuest : IssueQuestFlags.TrackedIssue, questsRelatedToHero[i].Title, (questsRelatedToHero[i].JournalEntries.Count > 0) ? questsRelatedToHero[i].JournalEntries[0].LogText : TextObject.GetEmpty()));
					}
				}
			}
			bool flag = questsRelatedToHero != null && relatedIssue?.IssueQuest != null && questsRelatedToHero.Any((QuestBase q) => q == relatedIssue.IssueQuest);
			if (relatedIssue != null && !flag)
			{
				(IssueQuestFlags, TextObject, TextObject) item = (GetIssueType(relatedIssue), relatedIssue.Title, relatedIssue.Description);
				list.Add(item);
			}
		}
		return list;
	}

	public static string GetQuestExplanationOfHero(IssueQuestFlags questType)
	{
		bool flag = (questType & IssueQuestFlags.ActiveIssue) != IssueQuestFlags.None || (questType & IssueQuestFlags.AvailableIssue) != 0;
		bool flag2 = (questType & IssueQuestFlags.ActiveIssue) != 0;
		string result = null;
		if (questType != IssueQuestFlags.None)
		{
			result = ((!flag) ? GameTexts.FindText("str_hero_has_active_quest").ToString() : GameTexts.FindText("str_hero_has_" + (flag2 ? "active" : "available") + "_issue").ToString());
		}
		return result;
	}

	public static List<QuestBase> GetQuestsRelatedToHero(Hero hero)
	{
		List<QuestBase> list = new List<QuestBase>();
		Campaign.Current.QuestManager.TrackedObjects.TryGetValue(hero, out var value);
		foreach (QuestBase questGiverQuest in Campaign.Current.QuestManager.GetQuestGiverQuests(hero))
		{
			if (questGiverQuest.IsTrackEnabled && !list.Contains(questGiverQuest))
			{
				list.Add(questGiverQuest);
			}
		}
		if (value != null)
		{
			for (int i = 0; i < value.Count; i++)
			{
				if (value[i].IsTrackEnabled && !list.Contains(value[i]))
				{
					list.Add(value[i]);
				}
			}
		}
		if (hero.Issue?.IssueQuest != null && hero.Issue.IssueQuest.IsTrackEnabled && !hero.Issue.IssueQuest.IsTracked(hero) && !list.Contains(hero.Issue.IssueQuest))
		{
			list.Add(hero.Issue.IssueQuest);
		}
		return list;
	}

	public static List<QuestBase> GetQuestsRelatedToParty(MobileParty party)
	{
		List<QuestBase> list = new List<QuestBase>();
		Campaign.Current.QuestManager.TrackedObjects.TryGetValue(party, out var value);
		if (value != null)
		{
			for (int i = 0; i < value.Count; i++)
			{
				if (value[i].IsTrackEnabled)
				{
					list.Add(value[i]);
				}
			}
		}
		if (party.MemberRoster.TotalHeroes > 0)
		{
			if (party.LeaderHero != null && party.MemberRoster.TotalHeroes == 1)
			{
				List<QuestBase> questsRelatedToHero = GetQuestsRelatedToHero(party.LeaderHero);
				if (questsRelatedToHero != null && questsRelatedToHero.Count > 0)
				{
					list.AddRange(questsRelatedToHero);
				}
			}
			else
			{
				for (int j = 0; j < party.MemberRoster.Count; j++)
				{
					Hero hero = party.MemberRoster.GetCharacterAtIndex(j)?.HeroObject;
					if (hero != null)
					{
						List<QuestBase> questsRelatedToHero2 = GetQuestsRelatedToHero(hero);
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

	public static List<(bool isHeroQuestGiver, QuestBase quest)> GetQuestsRelatedToSettlement(Settlement settlement)
	{
		List<(bool, QuestBase)> list = new List<(bool, QuestBase)>();
		foreach (KeyValuePair<ITrackableCampaignObject, List<QuestBase>> trackedObject in Campaign.Current.QuestManager.TrackedObjects)
		{
			Hero hero = trackedObject.Key as Hero;
			MobileParty mobileParty = trackedObject.Key as MobileParty;
			if ((hero == null || hero.PartyBelongedTo == MobileParty.MainParty || hero.PartyBelongedToAsPrisoner == PartyBase.MainParty || hero.CurrentSettlement != settlement) && (mobileParty == null || mobileParty.CurrentSettlement != settlement))
			{
				continue;
			}
			for (int i = 0; i < trackedObject.Value.Count; i++)
			{
				bool item = trackedObject.Value[i].QuestGiver != null && (trackedObject.Value[i].QuestGiver == hero || trackedObject.Value[i].QuestGiver == mobileParty?.LeaderHero);
				if (!list.Contains((item, trackedObject.Value[i])) && trackedObject.Value[i].IsTrackEnabled)
				{
					list.Add((item, trackedObject.Value[i]));
				}
			}
		}
		return list;
	}

	public static bool IsQuestRelatedToSettlement(QuestBase quest, Settlement settlement)
	{
		if (quest.QuestGiver?.CurrentSettlement == settlement || quest.IsTracked(settlement))
		{
			return true;
		}
		foreach (KeyValuePair<ITrackableCampaignObject, List<QuestBase>> trackedObject in Campaign.Current.QuestManager.TrackedObjects)
		{
			Hero hero = trackedObject.Key as Hero;
			MobileParty mobileParty = trackedObject.Key as MobileParty;
			if ((hero == null || hero.CurrentSettlement != settlement) && (mobileParty == null || mobileParty.CurrentSettlement != settlement))
			{
				continue;
			}
			for (int i = 0; i < trackedObject.Value.Count; i++)
			{
				if (trackedObject.Value[i].IsTrackEnabled && trackedObject.Value[i] == quest)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static IssueQuestFlags GetIssueType(IssueBase issue)
	{
		if (issue.IsSolvingWithAlternative || issue.IsSolvingWithLordSolution || issue.IsSolvingWithQuest)
		{
			return IssueQuestFlags.ActiveIssue;
		}
		return IssueQuestFlags.AvailableIssue;
	}

	public static IssueQuestFlags GetQuestType(QuestBase quest, Hero queriedQuestGiver)
	{
		if (quest.QuestGiver != null && quest.QuestGiver == queriedQuestGiver)
		{
			if (!quest.IsSpecialQuest)
			{
				return IssueQuestFlags.ActiveIssue;
			}
			return IssueQuestFlags.ActiveStoryQuest;
		}
		if (!quest.IsSpecialQuest)
		{
			return IssueQuestFlags.TrackedIssue;
		}
		return IssueQuestFlags.TrackedStoryQuest;
	}

	public static IEnumerable<TraitObject> GetHeroTraits()
	{
		yield return DefaultTraits.Generosity;
		yield return DefaultTraits.Honor;
		yield return DefaultTraits.Valor;
		yield return DefaultTraits.Mercy;
		yield return DefaultTraits.Calculating;
	}

	public static bool IsItemUsageApplicable(WeaponComponentData weapon)
	{
		WeaponDescription obj = ((weapon != null && weapon.WeaponDescriptionId != null) ? MBObjectManager.Instance.GetObject<WeaponDescription>(weapon.WeaponDescriptionId) : null);
		if (obj == null)
		{
			return false;
		}
		return !obj.IsHiddenFromUI;
	}

	public static string FloatToString(float x)
	{
		return x.ToString("F1");
	}

	private static Tuple<bool, TextObject> GetIsStringApplicableForHeroName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=C9tKA0ul}Character name cannot be empty"));
		}
		if ((name.Length < 3 && !name.Any((char c) => Common.IsCharAsian(c))) || name.Length > 50)
		{
			TextObject textObject = new TextObject("{=fPoB2u5m}Character name should be between {MIN} and {MAX} characters");
			textObject.SetTextVariable("MIN", 3);
			textObject.SetTextVariable("MAX", 50);
			return new Tuple<bool, TextObject>(item1: false, textObject);
		}
		if (!name.All((char x) => (char.IsLetterOrDigit(x) || char.IsWhiteSpace(x) || char.IsPunctuation(x)) && x != '{' && x != '}'))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=P1hk0m4o}Character name cannot contain special characters"));
		}
		if (name.StartsWith(" ") || name.EndsWith(" "))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=oofSja21}Character name cannot start or end with a white space"));
		}
		if (name.Contains("  "))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=wcSSgFyK}Character name cannot contain consecutive white spaces"));
		}
		return new Tuple<bool, TextObject>(item1: true, TextObject.GetEmpty());
	}

	public static Tuple<bool, string> IsStringApplicableForHeroName(string name)
	{
		Tuple<bool, TextObject> isStringApplicableForHeroName = GetIsStringApplicableForHeroName(name);
		return new Tuple<bool, string>(isStringApplicableForHeroName.Item1, isStringApplicableForHeroName.Item2.ToString());
	}

	public static Tuple<bool, TextObject> IsStringApplicableForItemName(string name)
	{
		if ((name.Length < 3 && !name.Any((char c) => Common.IsCharAsian(c))) || name.Length > 50)
		{
			TextObject textObject = new TextObject("{=h0xoKxxo}Item name should be between {MIN} and {MAX} characters.");
			textObject.SetTextVariable("MIN", 3);
			textObject.SetTextVariable("MAX", 50);
			return new Tuple<bool, TextObject>(item1: false, textObject);
		}
		if (string.IsNullOrEmpty(name))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=QQ03J6sf}Item name can not be empty."));
		}
		if (!name.All((char x) => (char.IsLetterOrDigit(x) || char.IsWhiteSpace(x) || char.IsPunctuation(x)) && x != '{' && x != '}'))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=NkY3Kq9l}Item name cannot contain special characters."));
		}
		if (name.StartsWith(" ") || name.EndsWith(" "))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=2Hbr4TEj}Item name cannot start or end with a white space."));
		}
		if (name.Contains("  "))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=Z4GdqdgV}Item name cannot contain consecutive white spaces."));
		}
		return new Tuple<bool, TextObject>(item1: true, TextObject.GetEmpty());
	}

	public static CharacterObject GetVisualPartyLeader(PartyBase party)
	{
		return PartyBaseHelper.GetVisualPartyLeader(party);
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

	public static List<Hero> GetChildrenAndGrandchildrenOfHero(Hero hero)
	{
		List<Hero> list = hero.Children.ToList();
		foreach (Hero child in hero.Children)
		{
			foreach (Hero child2 in child.Children)
			{
				list.Add(child2);
			}
		}
		return list;
	}
}
