using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[OverrideView(typeof(MissionBoundaryCrossingView))]
public class MissionGauntletBoundaryCrossingView : MissionBattleUIBaseView
{
	private GauntletLayer _gauntletLayer;

	private BoundaryCrossingVM _dataSource;

	protected override void OnCreateView()
	{
		_dataSource = new BoundaryCrossingVM(base.Mission, OnEscapeMenuToggled);
		_gauntletLayer = new GauntletLayer("BoundaryCrossing", 47);
		_gauntletLayer.LoadMovie("BoundaryCrossing", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	protected override void OnDestroyView()
	{
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	protected override void OnSuspendView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	protected override void OnResumeView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	private void OnEscapeMenuToggled(bool isOpened)
	{
		if (base.IsViewCreated)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, !isOpened);
		}
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (base.IsViewCreated)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (base.IsViewCreated)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}
}
