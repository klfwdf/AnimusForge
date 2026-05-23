using SandBox.View;
using SandBox.ViewModelCollection.SaveLoad;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI;

[OverrideView(typeof(SaveLoadScreen))]
public class GauntletSaveLoadScreen : ScreenBase
{
	private GauntletLayer _gauntletLayer;

	private SaveLoadVM _dataSource;

	private SpriteCategory _spriteCategory;

	private readonly bool _isSaving;

	public GauntletSaveLoadScreen(bool isSaving)
	{
		_isSaving = isSaving;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		bool isCampaignMapOnStack = GameStateManager.Current.GameStates.FirstOrDefaultQ((GameState s) => s is MapState) != null;
		_dataSource = new SaveLoadVM(_isSaving, isCampaignMapOnStack);
		_dataSource.SetDeleteInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Delete"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		if (Game.Current != null)
		{
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
		}
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_saveload");
		_gauntletLayer = new GauntletLayer("SaveLoadScreen", 1, shouldClear: true);
		_gauntletLayer.LoadMovie("SaveLoadScreen", _dataSource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		AddLayer(_gauntletLayer);
		if (BannerlordConfig.ForceVSyncInMenus)
		{
			Utilities.SetForceVsync(value: true);
		}
		InformationManager.HideAllMessages();
		_dataSource.Initialize();
	}

	protected override void OnPostFrameTick(float dt)
	{
		base.OnPostFrameTick(dt);
		UpdateInputRestrictions();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		UpdateInputRestrictions();
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			_dataSource.ExecuteDone();
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
		}
		else if (_gauntletLayer.Input.IsHotKeyPressed("Confirm") && !_gauntletLayer.IsFocusedOnInput())
		{
			_dataSource.ExecuteLoadSave();
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
		}
		else if (_gauntletLayer.Input.IsHotKeyPressed("Delete") && !_gauntletLayer.IsFocusedOnInput())
		{
			_dataSource.DeleteSelectedSave();
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
		}
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		if (Game.Current != null)
		{
			Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
		}
		RemoveLayer(_gauntletLayer);
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
		_spriteCategory.Unload();
		Utilities.SetForceVsync(value: false);
	}

	private void UpdateInputRestrictions()
	{
		if (_dataSource.IsBusyWithAnAction)
		{
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
		}
		else
		{
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
	}
}
