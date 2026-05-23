using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletFullScreenNoticeView : GlobalLayer
{
	private readonly FullScreenNoticeVM _dataSource;

	public static GauntletFullScreenNoticeView Current { get; private set; }

	public GauntletFullScreenNoticeView()
	{
		_dataSource = new FullScreenNoticeVM();
		GauntletLayer gauntletLayer = new GauntletLayer("FullScreenNotice", 15010);
		gauntletLayer.LoadMovie("FullScreenNotice", _dataSource);
		base.Layer = gauntletLayer;
		base.Layer.IsFocusLayer = true;
		base.Layer.InputRestrictions.SetInputRestrictions();
		gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
	}

	public static void Initialize()
	{
		if (Current == null && !BannerlordConfig.IAPNoticeConfirmed)
		{
			Current = new GauntletFullScreenNoticeView();
			ScreenManager.AddGlobalLayer(Current, isFocusable: false);
		}
	}

	public static void SkipNotice()
	{
		Current?._dataSource?.ExecuteCloseNotice();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (Current?._dataSource == null)
		{
			return;
		}
		if (Current._dataSource.IsNoticeActive)
		{
			ScreenManager.TrySetFocus(base.Layer);
			if (base.Layer.Input.IsHotKeyReleased("Confirm"))
			{
				SkipNotice();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
		}
		else
		{
			ScreenManager.RemoveGlobalLayer(Current);
			Current._dataSource.OnFinalize();
			Current = null;
		}
	}
}
