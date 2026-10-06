using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;

public class MultiplayerGauntletUISubModule : MBSubModuleBase
{
	private bool _initialized;

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		if (!_initialized)
		{
			if (!Utilities.CommandLineArgumentExists("VisualTests"))
			{
				GauntletSceneNotification.Current.RegisterContextProvider(new MultiplayerSceneNotificationContextProvider());
			}
			_initialized = true;
		}
	}
}
