using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map.Navigation.NavigationElements
{
	// Token: 0x0200006C RID: 108
	public class EscapeMenuNavigationElement : MapNavigationElementBase
	{
		// Token: 0x17000091 RID: 145
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x00024F52 File Offset: 0x00023152
		public override string StringId
		{
			get
			{
				return "escape_menu";
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x0600049F RID: 1183 RVA: 0x00024F59 File Offset: 0x00023159
		public override bool IsActive
		{
			get
			{
				if (base._game.GameStateManager.ActiveState is MapState)
				{
					MapScreen instance = MapScreen.Instance;
					return instance != null && instance.IsEscapeMenuOpened;
				}
				return false;
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060004A0 RID: 1184 RVA: 0x00024F84 File Offset: 0x00023184
		public override bool IsLockingNavigation
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060004A1 RID: 1185 RVA: 0x00024F87 File Offset: 0x00023187
		public override bool HasAlert
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060004A2 RID: 1186 RVA: 0x00024F8A File Offset: 0x0002318A
		public EscapeMenuNavigationElement(MapNavigationHandler handler)
			: base(handler)
		{
		}

		// Token: 0x060004A3 RID: 1187 RVA: 0x00024F94 File Offset: 0x00023194
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
			return new NavigationPermissionItem(base._game.GameStateManager.ActiveState is MapState, null);
		}

		// Token: 0x060004A4 RID: 1188 RVA: 0x00024FE4 File Offset: 0x000231E4
		protected override TextObject GetTooltip()
		{
			if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || this.IsActive))
			{
				string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericPanelGameKeyCategory", "ToggleEscapeMenu").ToString();
				TextObject textObject = GameTexts.FindText("str_hotkey_with_hint", null);
				textObject.SetTextVariable("TEXT", GameTexts.FindText("str_escape_menu", null).ToString());
				textObject.SetTextVariable("HOTKEY", variable);
				return textObject;
			}
			return GameTexts.FindText("str_escape_menu", null);
		}

		// Token: 0x060004A5 RID: 1189 RVA: 0x0002506F File Offset: 0x0002326F
		protected override TextObject GetAlertTooltip()
		{
			return TextObject.GetEmpty();
		}

		// Token: 0x060004A6 RID: 1190 RVA: 0x00025078 File Offset: 0x00023278
		public override void OpenView()
		{
			if (base.Permission.IsAuthorized)
			{
				MapScreen instance = MapScreen.Instance;
				if (instance == null)
				{
					return;
				}
				instance.OpenEscapeMenu();
			}
		}

		// Token: 0x060004A7 RID: 1191 RVA: 0x000250A4 File Offset: 0x000232A4
		public override void OpenView(params object[] parameters)
		{
			Debug.FailedAssert("Escape menu shouldn't be opened with parameters from navigation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\EscapeMenuNavigationElement.cs", "OpenView", 70);
			this.OpenView();
		}

		// Token: 0x060004A8 RID: 1192 RVA: 0x000250C2 File Offset: 0x000232C2
		public override void GoToLink()
		{
		}
	}
}
