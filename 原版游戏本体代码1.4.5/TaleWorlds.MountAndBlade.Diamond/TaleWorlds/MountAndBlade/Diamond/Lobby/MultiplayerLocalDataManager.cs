using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby;

public class MultiplayerLocalDataManager
{
	internal const float FileUpdateIntervalInSeconds = 2f;

	private bool _isBusy;

	public static MultiplayerLocalDataManager Instance { get; private set; }

	public TauntSlotDataContainer TauntSlotData { get; private set; }

	public MatchHistoryDataContainer MatchHistory { get; private set; }

	public FavoriteServerDataContainer FavoriteServers { get; private set; }

	private MultiplayerLocalDataManager()
	{
		TauntSlotData = new TauntSlotDataContainer();
		MatchHistory = new MatchHistoryDataContainer();
		FavoriteServers = new FavoriteServerDataContainer();
	}

	public static void InitializeManager()
	{
		if (Instance != null)
		{
			Debug.FailedAssert("Multiplayer local data manager is already initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "InitializeManager", 34);
		}
		else
		{
			Instance = new MultiplayerLocalDataManager();
		}
	}

	public static void FinalizeManager()
	{
		if (Instance == null)
		{
			Debug.FailedAssert("Multiplayer local data manager is not initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "FinalizeManager", 45);
			return;
		}
		Instance.WaitForAsyncOperations();
		Instance = null;
	}

	public async void Tick(float dt)
	{
		if (!_isBusy)
		{
			_isBusy = true;
			await TauntSlotData.Tick(dt);
			await MatchHistory.Tick(dt);
			await FavoriteServers.Tick(dt);
			_isBusy = false;
		}
	}

	private void WaitForAsyncOperations()
	{
		while (_isBusy)
		{
		}
	}
}
