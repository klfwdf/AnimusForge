using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation.NavigationElements;

public class QuestsNavigationElement : MapNavigationElementBase
{
	public override string StringId => "quest";

	public override bool IsActive => base._game.GameStateManager.ActiveState is QuestsState;

	public override bool IsLockingNavigation => false;

	public override bool HasAlert => _viewDataTracker.IsQuestNotificationActive;

	public QuestsNavigationElement(MapNavigationHandler handler)
		: base(handler)
	{
	}

	protected override NavigationPermissionItem GetPermission()
	{
		if (!MapNavigationHelper.IsNavigationBarEnabled(_handler))
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		if (IsActive)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		Mission current = Mission.Current;
		if (current != null && !current.IsQuestScreenAccessAllowed)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		return new NavigationPermissionItem(isAuthorized: true, null);
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 42).ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_quest").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_quest");
	}

	protected override TextObject GetAlertTooltip()
	{
		if (HasAlert)
		{
			return _viewDataTracker.GetQuestNotificationText();
		}
		return TextObject.GetEmpty();
	}

	public override void OpenView()
	{
		PrepareToOpenQuestsScreen(delegate
		{
			OpenQuestsAction();
		});
	}

	public override void OpenView(params object[] parameters)
	{
		if (parameters.Length == 0)
		{
			return;
		}
		object obj = parameters[0];
		IssueBase issue;
		QuestBase quest;
		JournalLogEntry log;
		if ((issue = obj as IssueBase) != null)
		{
			PrepareToOpenQuestsScreen(delegate
			{
				OpenQuestsAction(issue);
			});
		}
		else if ((quest = obj as QuestBase) != null)
		{
			PrepareToOpenQuestsScreen(delegate
			{
				OpenQuestsAction(quest);
			});
		}
		else if ((log = obj as JournalLogEntry) != null)
		{
			PrepareToOpenQuestsScreen(delegate
			{
				OpenQuestsAction(log);
			});
		}
		else
		{
			Debug.FailedAssert($"Invalid parameter type when opening the quest screen from navigation: {obj.GetType()}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\QuestsNavigationElement.cs", "OpenView", 97);
		}
	}

	public override void GoToLink()
	{
	}

	private void PrepareToOpenQuestsScreen(Action openQuestsAction)
	{
		if (base.Permission.IsAuthorized)
		{
			if (ScreenManager.TopScreen is IChangeableScreen changeableScreen && changeableScreen.AnyUnsavedChanges())
			{
				InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(openQuestsAction) : MapNavigationHelper.GetUnapplicableChangedInquiry());
			}
			else
			{
				MapNavigationHelper.SwitchToANewScreen(openQuestsAction);
			}
		}
	}

	private void OpenQuestsAction()
	{
		QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>();
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenQuestsAction(IssueBase issue)
	{
		QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>(new object[1] { issue });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenQuestsAction(QuestBase quest)
	{
		QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>(new object[1] { quest });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenQuestsAction(JournalLogEntry log)
	{
		QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>(new object[1] { log });
		base._game.GameStateManager.PushState(gameState);
	}
}
