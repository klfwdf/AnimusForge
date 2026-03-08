using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation.NavigationElements
{
	// Token: 0x02000069 RID: 105
	public class CharacterDeveloperNavigationElement : MapNavigationElementBase
	{
		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000478 RID: 1144 RVA: 0x000247E6 File Offset: 0x000229E6
		public override string StringId
		{
			get
			{
				return "character_developer";
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000479 RID: 1145 RVA: 0x000247ED File Offset: 0x000229ED
		public override bool IsActive
		{
			get
			{
				return base._game.GameStateManager.ActiveState is CharacterDeveloperState;
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x0600047A RID: 1146 RVA: 0x00024807 File Offset: 0x00022A07
		public override bool IsLockingNavigation
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600047B RID: 1147 RVA: 0x0002480A File Offset: 0x00022A0A
		public override bool HasAlert
		{
			get
			{
				return this._viewDataTracker.IsCharacterNotificationActive;
			}
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x00024817 File Offset: 0x00022A17
		public CharacterDeveloperNavigationElement(MapNavigationHandler handler)
			: base(handler)
		{
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x00024820 File Offset: 0x00022A20
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
			if (mission != null && !mission.IsCharacterWindowAccessAllowed)
			{
				return new NavigationPermissionItem(false, null);
			}
			return new NavigationPermissionItem(true, null);
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x00024878 File Offset: 0x00022A78
		protected override TextObject GetTooltip()
		{
			if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || this.IsActive))
			{
				string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 37).ToString();
				TextObject textObject = GameTexts.FindText("str_hotkey_with_hint", null);
				textObject.SetTextVariable("TEXT", GameTexts.FindText("str_character", null).ToString());
				textObject.SetTextVariable("HOTKEY", variable);
				return textObject;
			}
			return GameTexts.FindText("str_character", null);
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x00024900 File Offset: 0x00022B00
		protected override TextObject GetAlertTooltip()
		{
			if (this.HasAlert)
			{
				return this._viewDataTracker.GetCharacterNotificationText();
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x0002491B File Offset: 0x00022B1B
		public override void OpenView()
		{
			this.PrepareToOpenCharacterDeveloper(delegate
			{
				this.OpenCharacterDeveloperScreenAction();
			});
		}

		// Token: 0x06000481 RID: 1153 RVA: 0x00024930 File Offset: 0x00022B30
		public override void OpenView(params object[] parameters)
		{
			if (parameters.Length != 0)
			{
				object obj = parameters[0];
				Hero hero;
				if ((hero = obj as Hero) != null)
				{
					this.PrepareToOpenCharacterDeveloper(delegate
					{
						this.OpenCharacterDeveloperScreenAction(hero);
					});
					return;
				}
				Debug.FailedAssert(string.Format("Invalid parameter type when opening the character developer screen from navigation: {0}", obj.GetType()), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\CharacterDeveloperNavigationElement.cs", "OpenView", 90);
			}
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x00024997 File Offset: 0x00022B97
		public override void GoToLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.EncyclopediaLink);
		}

		// Token: 0x06000483 RID: 1155 RVA: 0x000249B4 File Offset: 0x00022BB4
		private void PrepareToOpenCharacterDeveloper(Action openCharacterDeveloperAction)
		{
			if (base.Permission.IsAuthorized)
			{
				IChangeableScreen changeableScreen;
				if ((changeableScreen = ScreenManager.TopScreen as IChangeableScreen) != null && changeableScreen.AnyUnsavedChanges())
				{
					InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(openCharacterDeveloperAction) : MapNavigationHelper.GetUnapplicableChangedInquiry(), false, false);
					return;
				}
				MapNavigationHelper.SwitchToANewScreen(openCharacterDeveloperAction);
			}
		}

		// Token: 0x06000484 RID: 1156 RVA: 0x00024A0C File Offset: 0x00022C0C
		private void OpenCharacterDeveloperScreenAction()
		{
			CharacterDeveloperState gameState = base._game.GameStateManager.CreateState<CharacterDeveloperState>();
			base._game.GameStateManager.PushState(gameState, 0);
		}

		// Token: 0x06000485 RID: 1157 RVA: 0x00024A3C File Offset: 0x00022C3C
		private void OpenCharacterDeveloperScreenAction(Hero hero)
		{
			CharacterDeveloperState gameState = base._game.GameStateManager.CreateState<CharacterDeveloperState>(new object[] { hero });
			base._game.GameStateManager.PushState(gameState, 0);
		}
	}
}
