using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus;

public class GameMenuOption
{
	public delegate bool OnConditionDelegate(MenuCallbackArgs args);

	public delegate void OnConsequenceDelegate(MenuCallbackArgs args);

	public enum LeaveType
	{
		Default,
		Mission,
		Submenu,
		BribeAndEscape,
		Escape,
		Craft,
		ForceToGiveGoods,
		ForceToGiveTroops,
		Bribe,
		LeaveTroopsAndFlee,
		OrderTroopsToAttack,
		Raid,
		HostileAction,
		Recruit,
		Trade,
		Wait,
		Leave,
		Continue,
		Manage,
		TroopSelection,
		WaitQuest,
		Surrender,
		Conversation,
		DefendAction,
		Devastate,
		Pillage,
		ShowMercy,
		Leaderboard,
		OpenStash,
		ManageGarrison,
		StagePrisonBreak,
		ManagePrisoners,
		Ransom,
		PracticeFight,
		BesiegeTown,
		SneakIn,
		LeadAssault,
		DonateTroops,
		DonatePrisoners,
		SiegeAmbush,
		Warehouse,
		VisitPort,
		VisitTown,
		SetSail,
		ManageFleet,
		CallFleet,
		OrderShipsToAttack,
		RepairShips
	}

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

	public static IssueQuestFlags[] IssueQuestFlagsValues = (IssueQuestFlags[])Enum.GetValues(typeof(IssueQuestFlags));

	public OnConditionDelegate OnCondition;

	public OnConsequenceDelegate OnConsequence;

	public GameMenu.MenuAndOptionType Type { get; private set; }

	public LeaveType OptionLeaveType { get; set; }

	public IssueQuestFlags OptionQuestData { get; set; }

	public string IdString { get; private set; }

	public TextObject Text { get; private set; }

	public TextObject Text2 { get; private set; }

	public TextObject Tooltip { get; private set; }

	public bool IsLeave { get; private set; }

	public bool IsRepeatable { get; private set; }

	public bool IsEnabled { get; private set; }

	public object RelatedObject { get; private set; }

	internal GameMenuOption()
	{
		Text = null;
		Tooltip = null;
		IsEnabled = true;
	}

	public GameMenuOption(GameMenu.MenuAndOptionType type, string idString, TextObject text, TextObject text2, OnConditionDelegate condition, OnConsequenceDelegate consequence, bool isLeave = false, bool isRepeatable = false, object relatedObject = null)
	{
		Type = type;
		IdString = idString;
		Text = text;
		Text2 = text2;
		OnCondition = condition;
		OnConsequence = consequence;
		Tooltip = null;
		IsRepeatable = isRepeatable;
		IsEnabled = true;
		IsLeave = isLeave;
		RelatedObject = relatedObject;
	}

	public bool GetConditionsHold(Game game, MenuContext menuContext)
	{
		if (OnCondition != null)
		{
			MenuCallbackArgs menuCallbackArgs = new MenuCallbackArgs(menuContext, Text);
			bool result = OnCondition(menuCallbackArgs);
			IsEnabled = menuCallbackArgs.IsEnabled;
			Tooltip = menuCallbackArgs.Tooltip;
			OptionQuestData = menuCallbackArgs.OptionQuestData;
			OptionLeaveType = menuCallbackArgs.optionLeaveType;
			return result;
		}
		return true;
	}

	public void RunConsequence(MenuContext menuContext)
	{
		if (OnConsequence != null)
		{
			MenuCallbackArgs args = new MenuCallbackArgs(menuContext, Text);
			OnConsequence(args);
		}
		menuContext.OnConsequence(this);
	}

	public void SetEnable(bool isEnable)
	{
		IsEnabled = isEnable;
	}
}
