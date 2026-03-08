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

namespace SandBox.View.Map.Navigation.NavigationElements
{
	// Token: 0x02000070 RID: 112
	public class QuestsNavigationElement : MapNavigationElementBase
	{
		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060004D3 RID: 1235 RVA: 0x00025A08 File Offset: 0x00023C08
		public override string StringId
		{
			get
			{
				return "quest";
			}
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060004D4 RID: 1236 RVA: 0x00025A0F File Offset: 0x00023C0F
		public override bool IsActive
		{
			get
			{
				return base._game.GameStateManager.ActiveState is QuestsState;
			}
		}

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060004D5 RID: 1237 RVA: 0x00025A29 File Offset: 0x00023C29
		public override bool IsLockingNavigation
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060004D6 RID: 1238 RVA: 0x00025A2C File Offset: 0x00023C2C
		public override bool HasAlert
		{
			get
			{
				return this._viewDataTracker.IsQuestNotificationActive;
			}
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x00025A39 File Offset: 0x00023C39
		public QuestsNavigationElement(MapNavigationHandler handler)
			: base(handler)
		{
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x00025A44 File Offset: 0x00023C44
		protected override NavigationPermissionItem GetPermission()
		{
			if (!MapNavigationHelper.IsNavigationBarEnabled(this._handler))
			{
				return new NavigationPermissionItem(false, null);
			}
			if (this.IsActive)
			{
				return new NavigationPermissionItem(false, null);
			}
			Mission mission = Mission.Current;
			if (mission != null && !mission.IsQuestScreenAccessAllowed)
			{
				return new NavigationPermissionItem(false, null);
			}
			return new NavigationPermissionItem(true, null);
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x00025A9C File Offset: 0x00023C9C
		protected override TextObject GetTooltip()
		{
			if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || this.IsActive))
			{
				string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 42).ToString();
				TextObject textObject = GameTexts.FindText("str_hotkey_with_hint", null);
				textObject.SetTextVariable("TEXT", GameTexts.FindText("str_quest", null).ToString());
				textObject.SetTextVariable("HOTKEY", variable);
				return textObject;
			}
			return GameTexts.FindText("str_quest", null);
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x00025B24 File Offset: 0x00023D24
		protected override TextObject GetAlertTooltip()
		{
			if (this.HasAlert)
			{
				return this._viewDataTracker.GetQuestNotificationText();
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x00025B3F File Offset: 0x00023D3F
		public override void OpenView()
		{
			this.PrepareToOpenQuestsScreen(delegate
			{
				this.OpenQuestsAction();
			});
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x00025B54 File Offset: 0x00023D54
		public override void OpenView(params object[] parameters)
		{
			if (parameters.Length != 0)
			{
				QuestsNavigationElement.<>c__DisplayClass13_0 CS$<>8__locals1 = new QuestsNavigationElement.<>c__DisplayClass13_0();
				CS$<>8__locals1.<>4__this = this;
				object obj = parameters[0];
				if ((CS$<>8__locals1.issue = obj as IssueBase) != null)
				{
					this.PrepareToOpenQuestsScreen(delegate
					{
						CS$<>8__locals1.<>4__this.OpenQuestsAction(CS$<>8__locals1.issue);
					});
					return;
				}
				QuestsNavigationElement.<>c__DisplayClass13_1 CS$<>8__locals2 = new QuestsNavigationElement.<>c__DisplayClass13_1();
				CS$<>8__locals2.CS$<>8__locals1 = CS$<>8__locals1;
				if ((CS$<>8__locals2.quest = obj as QuestBase) != null)
				{
					this.PrepareToOpenQuestsScreen(delegate
					{
						CS$<>8__locals2.CS$<>8__locals1.<>4__this.OpenQuestsAction(CS$<>8__locals2.quest);
					});
					return;
				}
				JournalLogEntry log;
				if ((log = obj as JournalLogEntry) != null)
				{
					this.PrepareToOpenQuestsScreen(delegate
					{
						CS$<>8__locals2.CS$<>8__locals1.<>4__this.OpenQuestsAction(log);
					});
					return;
				}
				Debug.FailedAssert(string.Format("Invalid parameter type when opening the quest screen from navigation: {0}", obj.GetType()), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\QuestsNavigationElement.cs", "OpenView", 97);
			}
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x00025C28 File Offset: 0x00023E28
		public override void GoToLink()
		{
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x00025C2C File Offset: 0x00023E2C
		private void PrepareToOpenQuestsScreen(Action openQuestsAction)
		{
			if (base.Permission.IsAuthorized)
			{
				IChangeableScreen changeableScreen;
				if ((changeableScreen = ScreenManager.TopScreen as IChangeableScreen) != null && changeableScreen.AnyUnsavedChanges())
				{
					InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(openQuestsAction) : MapNavigationHelper.GetUnapplicableChangedInquiry(), false, false);
					return;
				}
				MapNavigationHelper.SwitchToANewScreen(openQuestsAction);
			}
		}

		// Token: 0x060004DF RID: 1247 RVA: 0x00025C84 File Offset: 0x00023E84
		private void OpenQuestsAction()
		{
			QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>();
			base._game.GameStateManager.PushState(gameState, 0);
		}

		// Token: 0x060004E0 RID: 1248 RVA: 0x00025CB4 File Offset: 0x00023EB4
		private void OpenQuestsAction(IssueBase issue)
		{
			QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>(new object[] { issue });
			base._game.GameStateManager.PushState(gameState, 0);
		}

		// Token: 0x060004E1 RID: 1249 RVA: 0x00025CF0 File Offset: 0x00023EF0
		private void OpenQuestsAction(QuestBase quest)
		{
			QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>(new object[] { quest });
			base._game.GameStateManager.PushState(gameState, 0);
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x00025D2C File Offset: 0x00023F2C
		private void OpenQuestsAction(JournalLogEntry log)
		{
			QuestsState gameState = base._game.GameStateManager.CreateState<QuestsState>(new object[] { log });
			base._game.GameStateManager.PushState(gameState, 0);
		}
	}
}
