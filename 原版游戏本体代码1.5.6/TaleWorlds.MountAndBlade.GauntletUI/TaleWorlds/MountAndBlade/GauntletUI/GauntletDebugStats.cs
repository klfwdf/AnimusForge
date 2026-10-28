using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletDebugStats : GlobalLayer
{
	private DebugStatsVM _dataSource;

	public void Initialize()
	{
		_dataSource = new DebugStatsVM();
		GauntletLayer gauntletLayer = new GauntletLayer("DebugStats", 30000);
		gauntletLayer.LoadMovie("DebugStats", _dataSource);
		gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		base.Layer = gauntletLayer;
		ScreenManager.AddGlobalLayer(this, isFocusable: true);
	}
}
