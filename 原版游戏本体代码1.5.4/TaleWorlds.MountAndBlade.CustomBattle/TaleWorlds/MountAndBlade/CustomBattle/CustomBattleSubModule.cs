using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;
using TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;
using TaleWorlds.MountAndBlade.View.CustomBattle;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CustomBattleSubModule : MBSubModuleBase
{
	private bool _initialized;

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		CustomBattleFactory.RegisterProvider<CustomBattleProvider>();
		TauntUsageManager.Initialize();
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		if (!_initialized && GauntletSceneNotification.Current != null)
		{
			if (!Utilities.CommandLineArgumentExists("VisualTests"))
			{
				GauntletSceneNotification.Current.RegisterContextProvider(new CustomBattleSceneNotificationContextProvider());
			}
			_initialized = true;
		}
	}
}
