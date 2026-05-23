using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation.NavigationElements
{
	// Token: 0x0200006F RID: 111
	public class PartyNavigationElement : MapNavigationElementBase
	{
		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060004C8 RID: 1224 RVA: 0x000257DA File Offset: 0x000239DA
		public override string StringId
		{
			get
			{
				return "party";
			}
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060004C9 RID: 1225 RVA: 0x000257E1 File Offset: 0x000239E1
		public override bool IsActive
		{
			get
			{
				return base._game.GameStateManager.ActiveState is PartyState;
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060004CA RID: 1226 RVA: 0x000257FC File Offset: 0x000239FC
		public override bool IsLockingNavigation
		{
			get
			{
				GameStateManager gameStateManager = GameStateManager.Current;
				PartyState partyState;
				return (partyState = ((gameStateManager != null) ? gameStateManager.ActiveState : null) as PartyState) != null && partyState.PartyScreenLogic != null && partyState.PartyScreenMode != PartyScreenHelper.PartyScreenMode.Normal;
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060004CB RID: 1227 RVA: 0x00025836 File Offset: 0x00023A36
		public override bool HasAlert
		{
			get
			{
				return this._viewDataTracker.IsPartyNotificationActive;
			}
		}

		// Token: 0x060004CC RID: 1228 RVA: 0x00025843 File Offset: 0x00023A43
		public PartyNavigationElement(MapNavigationHandler handler)
			: base(handler)
		{
		}

		// Token: 0x060004CD RID: 1229 RVA: 0x0002584C File Offset: 0x00023A4C
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
			if (MobileParty.MainParty.IsInRaftState || Hero.MainHero.HeroState == Hero.CharacterStates.Prisoner)
			{
				return new NavigationPermissionItem(false, null);
			}
			if (MobileParty.MainParty.MapEvent != null)
			{
				return new NavigationPermissionItem(false, null);
			}
			Mission mission = Mission.Current;
			if (mission != null && !mission.IsPartyWindowAccessAllowed)
			{
				return new NavigationPermissionItem(false, null);
			}
			return new NavigationPermissionItem(true, null);
		}

		// Token: 0x060004CE RID: 1230 RVA: 0x000258D8 File Offset: 0x00023AD8
		protected override TextObject GetTooltip()
		{
			if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || this.IsActive))
			{
				string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 43).ToString();
				TextObject textObject = GameTexts.FindText("str_hotkey_with_hint", null);
				textObject.SetTextVariable("TEXT", GameTexts.FindText("str_party", null).ToString());
				textObject.SetTextVariable("HOTKEY", variable);
				return textObject;
			}
			return GameTexts.FindText("str_party", null);
		}

		// Token: 0x060004CF RID: 1231 RVA: 0x00025960 File Offset: 0x00023B60
		protected override TextObject GetAlertTooltip()
		{
			if (this.HasAlert)
			{
				return this._viewDataTracker.GetPartyNotificationText();
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x060004D0 RID: 1232 RVA: 0x0002597C File Offset: 0x00023B7C
		public override void OpenView()
		{
			if (base.Permission.IsAuthorized)
			{
				IChangeableScreen changeableScreen;
				if ((changeableScreen = ScreenManager.TopScreen as IChangeableScreen) != null && changeableScreen.AnyUnsavedChanges())
				{
					InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(new Action(PartyScreenHelper.OpenScreenAsNormal)) : MapNavigationHelper.GetUnapplicableChangedInquiry(), false, false);
					return;
				}
				MapNavigationHelper.SwitchToANewScreen(new Action(PartyScreenHelper.OpenScreenAsNormal));
			}
		}

		// Token: 0x060004D1 RID: 1233 RVA: 0x000259E8 File Offset: 0x00023BE8
		public override void OpenView(params object[] parameters)
		{
			Debug.FailedAssert("Party screen shouldn't be opened with parameters from navigation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\PartyNavigationElement.cs", "OpenView", 118);
			this.OpenView();
		}

		// Token: 0x060004D2 RID: 1234 RVA: 0x00025A06 File Offset: 0x00023C06
		public override void GoToLink()
		{
		}
	}
}
