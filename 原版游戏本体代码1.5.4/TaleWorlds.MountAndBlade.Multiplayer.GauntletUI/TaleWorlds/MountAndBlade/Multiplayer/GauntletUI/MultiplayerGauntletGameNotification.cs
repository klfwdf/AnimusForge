using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;

public class MultiplayerGauntletGameNotification : GauntletGameNotification
{
	protected override string MovieName => "MultiplayerGameNotificationUI";

	public new static void Initialize()
	{
		GauntletGameNotification.Current?.OnFinalize();
		GauntletGameNotification.Current = new MultiplayerGauntletGameNotification();
		ScreenManager.AddGlobalLayer(GauntletGameNotification.Current, isFocusable: false);
		GauntletGameNotification.Current.RegisterEvents();
	}
}
