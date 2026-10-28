using SandBox.GauntletUI.Tutorial;
using SandBox.View.Map;
using StoryMode.ViewModelCollection.Map;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace StoryMode.GauntletUI;

public class StoryModeGauntletUISubModule : MBSubModuleBase
{
	private bool _registered;

	public override void OnGameInitializationFinished(Game game)
	{
		base.OnGameInitializationFinished(game);
		if (game.GameType.RequiresTutorial)
		{
			GauntletTutorialSystem.OnInitialize();
			ScreenManager.OnPushScreen += OnScreenManagerPushScreen;
		}
	}

	private void OnScreenManagerPushScreen(ScreenBase pushedScreen)
	{
		if (!_registered && pushedScreen is MapScreen mapScreen)
		{
			mapScreen.MapNotificationView.RegisterMapNotificationType(typeof(ConspiracyQuestMapNotification), typeof(ConspiracyQuestMapNotificationItemVM));
			_registered = true;
		}
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		if (game.GameType.RequiresTutorial)
		{
			GauntletTutorialSystem.OnUnload();
			ScreenManager.OnPushScreen -= OnScreenManagerPushScreen;
		}
		_registered = false;
	}
}
