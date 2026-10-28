using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Barter;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map;

public class GauntletMapConversationBarterView
{
	public delegate void OnBarterActiveStateChanged(bool isBarterActive);

	private readonly GauntletLayer _gauntletLayer;

	private readonly OnBarterActiveStateChanged _onActiveStateChanged;

	private SpriteCategory _barterCategory;

	private BarterVM _barterDataSource;

	private GauntletMovieIdentifier _barterMovie;

	public bool IsCreated { get; private set; }

	public bool IsActive { get; private set; }

	public GauntletMapConversationBarterView(GauntletLayer layer, OnBarterActiveStateChanged onActiveStateChanged)
	{
		_gauntletLayer = layer;
		_onActiveStateChanged = onActiveStateChanged;
	}

	public void CreateBarterView(BarterData args)
	{
		_barterDataSource = new BarterVM(args);
		_barterDataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		_barterDataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_barterDataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_onActiveStateChanged?.Invoke(isBarterActive: true);
		_barterCategory = UIResourceManager.GetSpriteCategory("ui_barter");
		Activate();
		IsCreated = true;
	}

	public void DestroyBarterView()
	{
		Deactivate();
		_barterDataSource.OnFinalize();
		_barterDataSource = null;
		_barterCategory = null;
		_onActiveStateChanged?.Invoke(isBarterActive: false);
		BarterItemVM.IsFiveStackModifierActive = false;
		BarterItemVM.IsEntireStackModifierActive = false;
		IsCreated = false;
	}

	public void Activate()
	{
		_barterMovie = _gauntletLayer.LoadMovie("BarterScreen", _barterDataSource);
		_barterCategory.Load();
		_onActiveStateChanged?.Invoke(isBarterActive: true);
		IsActive = true;
	}

	public void Deactivate()
	{
		_gauntletLayer.ReleaseMovie(_barterMovie);
		_barterCategory.Unload();
		IsActive = false;
	}

	public void TickInput()
	{
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_barterDataSource.ExecuteCancel();
			return;
		}
		if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
		{
			BarterVM barterDataSource = _barterDataSource;
			if (barterDataSource != null && !barterDataSource.IsOfferDisabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_barterDataSource.ExecuteOffer();
				return;
			}
		}
		if (_gauntletLayer.Input.IsHotKeyReleased("Reset"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_barterDataSource.ExecuteReset();
		}
	}
}
