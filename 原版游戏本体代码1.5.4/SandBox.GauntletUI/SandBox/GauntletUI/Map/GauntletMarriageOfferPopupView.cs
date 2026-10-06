using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MarriageOfferPopupView))]
public class GauntletMarriageOfferPopupView : MapView
{
	private GauntletLayer _layerAsGauntletLayer;

	private MarriageOfferPopupVM _dataSource;

	private GauntletMovieIdentifier _movie;

	private CampaignTimeControlMode _previousTimeControlMode;

	private Hero _suitor;

	private Hero _maiden;

	public GauntletMarriageOfferPopupView(Hero suitor, Hero maiden)
	{
		_suitor = suitor;
		_maiden = maiden;
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new MarriageOfferPopupVM(_suitor, _maiden, OnPopupClosed);
		InitializeKeyVisuals();
		base.Layer = new GauntletLayer("MapMarriageOffer", 203);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(base.Layer);
		_movie = _layerAsGauntletLayer.LoadMovie("MarriageOfferPopup", _dataSource);
		base.MapScreen.AddLayer(base.Layer);
		base.MapScreen.SetIsMarriageOfferPopupActive(isMarriageOfferPopupActive: true);
		_previousTimeControlMode = Campaign.Current.TimeControlMode;
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		Campaign.Current.SetTimeControlModeLock(isLocked: true);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		HandleInput();
		_dataSource?.Update();
	}

	protected override void OnMenuModeTick(float dt)
	{
		base.OnMenuModeTick(dt);
		HandleInput();
		_dataSource?.Update();
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		HandleInput();
		_dataSource?.Update();
	}

	protected override void OnFinalize()
	{
		_layerAsGauntletLayer.ReleaseMovie(_movie);
		base.MapScreen.RemoveLayer(base.Layer);
		_movie = null;
		_dataSource = null;
		base.Layer = null;
		_layerAsGauntletLayer = null;
		base.MapScreen.SetIsMarriageOfferPopupActive(isMarriageOfferPopupActive: false);
		Campaign.Current.SetTimeControlModeLock(isLocked: false);
		Campaign.Current.TimeControlMode = _previousTimeControlMode;
		base.OnFinalize();
	}

	protected override bool IsEscaped()
	{
		_dataSource?.ExecuteDeclineOffer();
		return true;
	}

	protected override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return false;
	}

	private void OnPopupClosed()
	{
		base.MapScreen.CloseMarriageOfferPopup();
	}

	private void HandleInput()
	{
		if (_dataSource != null)
		{
			if (base.Layer.Input.IsGameKeyPressed(39))
			{
				base.MapScreen.OpenEncyclopedia();
			}
			else if (base.Layer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				_dataSource.ExecuteAcceptOffer();
			}
			else if (base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				_dataSource.ExecuteDeclineOffer();
			}
		}
	}

	private void InitializeKeyVisuals()
	{
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
	}
}
