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
	// Token: 0x0200006D RID: 109
	public class InventoryNavigationElement : MapNavigationElementBase
	{
		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060004A9 RID: 1193 RVA: 0x000250C4 File Offset: 0x000232C4
		public override string StringId
		{
			get
			{
				return "inventory";
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060004AA RID: 1194 RVA: 0x000250CB File Offset: 0x000232CB
		public override bool IsActive
		{
			get
			{
				return base._game.GameStateManager.ActiveState is InventoryState;
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060004AB RID: 1195 RVA: 0x000250E8 File Offset: 0x000232E8
		public override bool IsLockingNavigation
		{
			get
			{
				GameStateManager gameStateManager = GameStateManager.Current;
				InventoryState inventoryState;
				return (inventoryState = ((gameStateManager != null) ? gameStateManager.ActiveState : null) as InventoryState) != null && inventoryState.InventoryLogic != null && inventoryState.InventoryMode != InventoryScreenHelper.InventoryMode.Default;
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060004AC RID: 1196 RVA: 0x00025122 File Offset: 0x00023322
		public override bool HasAlert
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060004AD RID: 1197 RVA: 0x00025125 File Offset: 0x00023325
		public InventoryNavigationElement(MapNavigationHandler handler)
			: base(handler)
		{
		}

		// Token: 0x060004AE RID: 1198 RVA: 0x00025130 File Offset: 0x00023330
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
			Mission mission = Mission.Current;
			if (mission != null && !mission.IsInventoryAccessAllowed)
			{
				return new NavigationPermissionItem(false, null);
			}
			return new NavigationPermissionItem(true, null);
		}

		// Token: 0x060004AF RID: 1199 RVA: 0x000251A8 File Offset: 0x000233A8
		protected override TextObject GetTooltip()
		{
			if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || this.IsActive))
			{
				string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 38).ToString();
				TextObject textObject = GameTexts.FindText("str_hotkey_with_hint", null);
				textObject.SetTextVariable("TEXT", GameTexts.FindText("str_inventory", null).ToString());
				textObject.SetTextVariable("HOTKEY", variable);
				return textObject;
			}
			return GameTexts.FindText("str_inventory", null);
		}

		// Token: 0x060004B0 RID: 1200 RVA: 0x00025230 File Offset: 0x00023430
		protected override TextObject GetAlertTooltip()
		{
			return TextObject.GetEmpty();
		}

		// Token: 0x060004B1 RID: 1201 RVA: 0x00025238 File Offset: 0x00023438
		public override void OpenView()
		{
			if (base.Permission.IsAuthorized)
			{
				IChangeableScreen changeableScreen;
				if ((changeableScreen = ScreenManager.TopScreen as IChangeableScreen) != null && changeableScreen.AnyUnsavedChanges())
				{
					InquiryData data;
					if (!changeableScreen.CanChangesBeApplied())
					{
						data = MapNavigationHelper.GetUnapplicableChangedInquiry();
					}
					else
					{
						data = MapNavigationHelper.GetUnsavedChangedInquiry(delegate
						{
							InventoryScreenHelper.OpenScreenAsInventory(null);
						});
					}
					InformationManager.ShowInquiry(data, false, false);
					return;
				}
				MapNavigationHelper.SwitchToANewScreen(delegate
				{
					InventoryScreenHelper.OpenScreenAsInventory(null);
				});
			}
		}

		// Token: 0x060004B2 RID: 1202 RVA: 0x000252CA File Offset: 0x000234CA
		public override void OpenView(params object[] parameters)
		{
			Debug.FailedAssert("Inventory screen shouldn't be opened with parameters from navigation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\InventoryNavigationElement.cs", "OpenView", 106);
			this.OpenView();
		}

		// Token: 0x060004B3 RID: 1203 RVA: 0x000252E8 File Offset: 0x000234E8
		public override void GoToLink()
		{
		}
	}
}
