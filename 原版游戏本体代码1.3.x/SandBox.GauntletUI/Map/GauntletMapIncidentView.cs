using System;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Map.Incidents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200003A RID: 58
	[OverrideView(typeof(MapIncidentView))]
	public class GauntletMapIncidentView : MapIncidentView
	{
		// Token: 0x060002AF RID: 687 RVA: 0x0000FEA4 File Offset: 0x0000E0A4
		public GauntletMapIncidentView(Incident incident)
			: base(incident)
		{
		}

		// Token: 0x060002B0 RID: 688 RVA: 0x0000FEAD File Offset: 0x0000E0AD
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._gauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._gauntletLayer, true);
			}
		}

		// Token: 0x060002B1 RID: 689 RVA: 0x0000FEC9 File Offset: 0x0000E0C9
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._gauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._gauntletLayer, false);
			}
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x0000FEE8 File Offset: 0x0000E0E8
		protected override void CreateLayout()
		{
			base.CreateLayout();
			if (this.Incident == null)
			{
				Debug.FailedAssert("Failed to start incident view", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapIncidentView.cs", "CreateLayout", 57);
				return;
			}
			this._controlModeBeforeIncident = Campaign.Current.TimeControlMode;
			this._controlModeLockBeforeIncident = Campaign.Current.TimeControlModeLock;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			Campaign.Current.SetTimeControlModeLock(true);
			MBCommon.PauseGameEngine();
			this._dataSource = new MapIncidentVM(this.Incident, new Action(this.OnCloseView));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._gauntletLayer = new GauntletLayer("MapIncidents", 203, false);
			this._gauntletLayer.LoadMovie("MapIncident", this._dataSource);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer = this._gauntletLayer;
			base.MapScreen.AddLayer(base.Layer);
			this._spriteCategory = UIResourceManager.LoadSpriteCategory("ui_map_incidents");
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			base.MapScreen.SetIsMapIncidentActive(true);
			this.PlayIncidentSound();
		}

		// Token: 0x060002B3 RID: 691 RVA: 0x00010040 File Offset: 0x0000E240
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.Tick();
		}

		// Token: 0x060002B4 RID: 692 RVA: 0x0001004F File Offset: 0x0000E24F
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			this.Tick();
		}

		// Token: 0x060002B5 RID: 693 RVA: 0x0001005E File Offset: 0x0000E25E
		protected override void OnMenuModeTick(float dt)
		{
			base.OnMenuModeTick(dt);
			this.Tick();
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x00010070 File Offset: 0x0000E270
		private void Tick()
		{
			if (this._dataSource != null && this._gauntletLayer.Input.IsHotKeyReleased("Confirm") && this._dataSource.CanConfirm)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.ExecuteConfirm();
			}
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x000100BE File Offset: 0x0000E2BE
		protected override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
		{
			return false;
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x000100C1 File Offset: 0x0000E2C1
		private void OnCloseView()
		{
			base.MapScreen.RemoveMapView(this);
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x000100D0 File Offset: 0x0000E2D0
		protected override void OnFinalize()
		{
			base.OnFinalize();
			if (MBCommon.IsPaused)
			{
				MBCommon.UnPauseGameEngine();
			}
			if (base.Layer != null)
			{
				this._spriteCategory.Unload();
				this._dataSource.OnFinalize();
				this._dataSource = null;
				base.Layer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(base.Layer);
				base.MapScreen.RemoveLayer(base.Layer);
				base.MapScreen.SetIsMapIncidentActive(false);
				Campaign.Current.TimeControlMode = this._controlModeBeforeIncident;
				Campaign.Current.SetTimeControlModeLock(this._controlModeLockBeforeIncident);
				return;
			}
			if (this._dataSource != null || this._spriteCategory != null)
			{
				Debug.FailedAssert("Incident view is was not propertly initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapIncidentView.cs", "OnFinalize", 162);
				MapIncidentVM dataSource = this._dataSource;
				if (dataSource != null)
				{
					dataSource.OnFinalize();
				}
				SpriteCategory spriteCategory = this._spriteCategory;
				if (spriteCategory == null)
				{
					return;
				}
				spriteCategory.Unload();
			}
		}

		// Token: 0x060002BA RID: 698 RVA: 0x000101B4 File Offset: 0x0000E3B4
		private void PlayIncidentSound()
		{
			string text = "";
			switch (this.Incident.Type)
			{
			case IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation:
				text = "event:/ui/encounter/troop_settlement";
				break;
			case IncidentsCampaignBehaviour.IncidentType.FoodConsumption:
				text = "event:/ui/encounter/food_spoil";
				break;
			case IncidentsCampaignBehaviour.IncidentType.PlightOfCivilians:
				text = "event:/ui/encounter/plight";
				break;
			case IncidentsCampaignBehaviour.IncidentType.PartyCampLife:
				text = "event:/ui/encounter/camp";
				break;
			case IncidentsCampaignBehaviour.IncidentType.AnimalIllness:
				text = "event:/ui/encounter/sick_animals";
				break;
			case IncidentsCampaignBehaviour.IncidentType.Illness:
				text = "event:/ui/encounter/illness";
				break;
			case IncidentsCampaignBehaviour.IncidentType.HuntingForaging:
				text = "event:/ui/encounter/hunting_foraging";
				break;
			case IncidentsCampaignBehaviour.IncidentType.PostBattle:
				text = "event:/ui/encounter/post_battle";
				break;
			case IncidentsCampaignBehaviour.IncidentType.HardTravel:
				text = "event:/ui/encounter/hard_travel";
				break;
			case IncidentsCampaignBehaviour.IncidentType.Profit:
				text = "event:/ui/encounter/profit";
				break;
			case IncidentsCampaignBehaviour.IncidentType.DreamsSongsAndSigns:
				text = "event:/ui/encounter/dreams_signs";
				break;
			case IncidentsCampaignBehaviour.IncidentType.FiefManagement:
				text = "event:/ui/encounter/fief";
				break;
			case IncidentsCampaignBehaviour.IncidentType.Siege:
				text = "event:/ui/encounter/siege";
				break;
			case IncidentsCampaignBehaviour.IncidentType.Workshop:
				text = "event:/ui/encounter/workshops";
				break;
			default:
				Debug.FailedAssert("Incident sound cannot be found!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapIncidentView.cs", "PlayIncidentSound", 233);
				break;
			}
			if (!string.IsNullOrEmpty(text))
			{
				UISoundsHelper.PlayUISound(text);
			}
		}

		// Token: 0x040000FF RID: 255
		private MapIncidentVM _dataSource;

		// Token: 0x04000100 RID: 256
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000101 RID: 257
		private SpriteCategory _spriteCategory;

		// Token: 0x04000102 RID: 258
		private bool _controlModeLockBeforeIncident;

		// Token: 0x04000103 RID: 259
		private CampaignTimeControlMode _controlModeBeforeIncident;
	}
}
