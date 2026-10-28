using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;

public class MultiplayerAdminInformationScreen : GlobalLayer
{
	private MultiplayerAdminInformationVM _dataSource;

	public static MultiplayerAdminInformationScreen Current { get; private set; }

	public MultiplayerAdminInformationScreen()
	{
		_dataSource = new MultiplayerAdminInformationVM();
		GauntletLayer gauntletLayer = new GauntletLayer("MultiplayerAdminInformation", 15300);
		gauntletLayer.LoadMovie("MultiplayerAdminInformation", _dataSource);
		base.Layer = gauntletLayer;
		InformationManager.OnAddSystemNotification += OnSystemNotificationReceived;
	}

	public static void OnInitialize()
	{
		if (Current == null)
		{
			Current = new MultiplayerAdminInformationScreen();
			ScreenManager.AddGlobalLayer(Current, isFocusable: false);
		}
	}

	public void OnFinalize()
	{
		InformationManager.OnAddSystemNotification -= OnSystemNotificationReceived;
	}

	private void OnSystemNotificationReceived(string obj)
	{
		_dataSource.OnNewMessageReceived(obj);
	}

	public static void OnRemove()
	{
		if (Current != null)
		{
			Current.OnFinalize();
			ScreenManager.RemoveGlobalLayer(Current);
			Current._dataSource = null;
			Current = null;
		}
	}
}
