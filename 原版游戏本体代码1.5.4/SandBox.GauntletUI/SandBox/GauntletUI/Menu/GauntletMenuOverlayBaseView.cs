using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu;

[OverrideView(typeof(MenuOverlayBaseView))]
public class GauntletMenuOverlayBaseView : MenuView
{
	private GameMenuOverlay _overlayDataSource;

	private GauntletLayer _layerAsGauntletLayer;

	private bool _isContextMenuEnabled;

	protected override void OnInitialize()
	{
		GameMenu.MenuOverlayType menuOverlayType = Campaign.Current.GameMenuManager.GetMenuOverlayType(base.MenuContext);
		_overlayDataSource = GameMenuOverlayFactory.GetOverlay(menuOverlayType);
		_overlayDataSource.SetExitInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		base.Layer = new GauntletLayer("MapMenuOverlay", 202);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_layerAsGauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		base.MenuViewContext.AddLayer(base.Layer);
		if (_overlayDataSource is EncounterMenuOverlayVM)
		{
			_layerAsGauntletLayer.LoadMovie("EncounterOverlay", _overlayDataSource);
		}
		else if (_overlayDataSource is SettlementMenuOverlayVM)
		{
			_layerAsGauntletLayer.LoadMovie("SettlementOverlay", _overlayDataSource);
		}
		else if (_overlayDataSource is ArmyMenuOverlayVM)
		{
			Debug.FailedAssert("Trying to open army overlay in menu. Should be opened in map overlay", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Menu\\GauntletMenuOverlayBaseView.cs", "OnInitialize", 49);
		}
		else
		{
			Debug.FailedAssert("Game menu overlay not supported in gauntlet overlay", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Menu\\GauntletMenuOverlayBaseView.cs", "OnInitialize", 53);
		}
		base.OnInitialize();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		_overlayDataSource?.OnFrameTick(dt);
		if (ScreenManager.TopScreen is MapScreen && _overlayDataSource != null)
		{
			_overlayDataSource.IsInfoBarExtended = (ScreenManager.TopScreen as MapScreen)?.IsBarExtended ?? false;
		}
		if (!_isContextMenuEnabled && _overlayDataSource.IsContextMenuEnabled)
		{
			_isContextMenuEnabled = true;
			MapScreen.Instance?.SetIsOverlayContextMenuActive(isOverlayContextMenuEnabled: true);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
		}
		else if (_isContextMenuEnabled && !_overlayDataSource.IsContextMenuEnabled)
		{
			_isContextMenuEnabled = false;
			MapScreen.Instance?.SetIsOverlayContextMenuActive(isOverlayContextMenuEnabled: false);
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
		}
		if (_isContextMenuEnabled && base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_overlayDataSource.IsContextMenuEnabled = false;
		}
	}

	protected override void OnHourlyTick()
	{
		base.OnHourlyTick();
		_overlayDataSource?.Refresh();
	}

	protected override void OnOverlayTypeChange(GameMenu.MenuOverlayType newType)
	{
		base.OnOverlayTypeChange(newType);
		_overlayDataSource?.UpdateOverlayType(newType);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_overlayDataSource?.Refresh();
	}

	protected override void OnFinalize()
	{
		MapScreen.Instance?.SetIsOverlayContextMenuActive(isOverlayContextMenuEnabled: false);
		base.MenuViewContext.RemoveLayer(base.Layer);
		_overlayDataSource.OnFinalize();
		_overlayDataSource = null;
		base.Layer = null;
		_layerAsGauntletLayer = null;
		base.OnFinalize();
	}

	protected override void OnMapConversationActivated()
	{
		base.OnMapConversationActivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationDeactivated()
	{
		base.OnMapConversationDeactivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
	}
}
