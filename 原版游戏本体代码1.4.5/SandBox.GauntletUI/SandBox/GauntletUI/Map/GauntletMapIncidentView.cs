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

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapIncidentView))]
public class GauntletMapIncidentView : MapIncidentView
{
	private MapIncidentVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _spriteCategory;

	private bool _controlModeLockBeforeIncident;

	private CampaignTimeControlMode _controlModeBeforeIncident;

	public GauntletMapIncidentView(Incident incident)
		: base(incident)
	{
	}

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		if (Incident == null)
		{
			Debug.FailedAssert("Failed to start incident view", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapIncidentView.cs", "CreateLayout", 57);
			return;
		}
		_controlModeBeforeIncident = Campaign.Current.TimeControlMode;
		_controlModeLockBeforeIncident = Campaign.Current.TimeControlModeLock;
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		Campaign.Current.SetTimeControlModeLock(isLocked: true);
		MBCommon.PauseGameEngine();
		_dataSource = new MapIncidentVM(Incident, OnCloseView);
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_gauntletLayer = new GauntletLayer("MapIncidents", 203);
		_gauntletLayer.LoadMovie("MapIncident", _dataSource);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer = _gauntletLayer;
		base.MapScreen.AddLayer(base.Layer);
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_map_incidents");
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(base.Layer);
		base.MapScreen.SetIsMapIncidentActive(isMapIncidentActive: true);
		PlayIncidentSound();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		Tick();
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		Tick();
	}

	protected override void OnMenuModeTick(float dt)
	{
		base.OnMenuModeTick(dt);
		Tick();
	}

	private void Tick()
	{
		if (_dataSource != null && _gauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanConfirm)
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteConfirm();
		}
	}

	protected override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return false;
	}

	private void OnCloseView()
	{
		base.MapScreen.RemoveMapView(this);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		if (MBCommon.IsPaused)
		{
			MBCommon.UnPauseGameEngine();
		}
		if (base.Layer != null)
		{
			_spriteCategory.Unload();
			_dataSource.OnFinalize();
			_dataSource = null;
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			base.MapScreen.RemoveLayer(base.Layer);
			base.MapScreen.SetIsMapIncidentActive(isMapIncidentActive: false);
			Campaign.Current.TimeControlMode = _controlModeBeforeIncident;
			Campaign.Current.SetTimeControlModeLock(_controlModeLockBeforeIncident);
		}
		else if (_dataSource != null || _spriteCategory != null)
		{
			Debug.FailedAssert("Incident view is was not propertly initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapIncidentView.cs", "OnFinalize", 162);
			_dataSource?.OnFinalize();
			_spriteCategory?.Unload();
		}
	}

	private void PlayIncidentSound()
	{
		string text = "";
		switch (Incident.Type)
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
}
