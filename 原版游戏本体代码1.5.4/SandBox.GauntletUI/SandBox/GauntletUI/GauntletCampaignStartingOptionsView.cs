using System;
using SandBox.AdvancedStartOptions;
using SandBox.View;
using SandBox.ViewModelCollection.CampaignStartingOptions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI;

[OverrideView(typeof(CampaignAdvancedStartingOptionsView))]
public class GauntletCampaignStartingOptionsView : GlobalLayer
{
	private readonly CampaignStartingOptionsVM _dataSource;

	private readonly Action<SandBox.AdvancedStartOptions.AdvancedStartOptions> _onConfirm;

	private readonly Action _onClose;

	public GauntletCampaignStartingOptionsView(SandBox.AdvancedStartOptions.AdvancedStartOptions startOptions, Action<SandBox.AdvancedStartOptions.AdvancedStartOptions> onConfirm, Action onClose)
	{
		MBTextManager.SetTextVariable("newline", "\n");
		_dataSource = new CampaignStartingOptionsVM(startOptions, OnConfirm, Close);
		_onConfirm = onConfirm;
		_onClose = onClose;
		GauntletLayer gauntletLayer = new GauntletLayer("CampaignStartingOptions", 11);
		gauntletLayer.LoadMovie("CampaignStartingOptionsScreen", _dataSource);
		base.Layer = gauntletLayer;
		base.Layer.IsFocusLayer = true;
		base.Layer.InputRestrictions.SetInputRestrictions();
		gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetRandomizeInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Randomize"));
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_dataSource == null)
		{
			return;
		}
		if (!(ScreenManager.TopScreen is GauntletInitialScreen))
		{
			_dataSource.ExecuteCancel();
			return;
		}
		ScreenManager.TrySetFocus(base.Layer);
		if (base.Layer.Input.IsHotKeyReleased("Confirm"))
		{
			if (_dataSource.CanConfirmStartType || _dataSource.IsASOShown)
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				_dataSource.ExecuteConfirm();
			}
		}
		else if (base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			if (_dataSource.IsASOShown)
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
			}
			else
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
			_dataSource.ExecuteCancel();
		}
		else if (base.Layer.Input.IsHotKeyReleased("Randomize") && _dataSource.IsASOShown)
		{
			StartingOptionVM focusedOption = _dataSource.FocusedOption;
			if (focusedOption != null && focusedOption.AllowRandomization && !_dataSource.FocusedOption.IsDisabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.FocusedOption.ExecuteRandomize();
			}
		}
	}

	private void OnConfirm(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		_onConfirm?.Invoke(options);
		ScreenManager.RemoveGlobalLayer(this);
		_dataSource?.OnFinalize();
		MBGameManager.StartNewGame(new SandBoxGameManager(() => new Campaign(CampaignGameMode.Campaign, options.GetChangedOptions())));
	}

	private void Close()
	{
		_onClose?.Invoke();
		ScreenManager.RemoveGlobalLayer(this);
		_dataSource?.OnFinalize();
	}
}
