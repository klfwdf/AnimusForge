using System;
using System.Threading.Tasks;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public static class InternetAvailabilityChecker
{
	public static Action<bool> OnInternetConnectionAvailabilityChanged;

	private static bool _internetConnectionAvailable;

	private static long _lastInternetConnectionCheck;

	private static bool _checkingConnection;

	private const long InternetConnectionCheckIntervalShort = 100000000L;

	private const long InternetConnectionCheckIntervalLong = 300000000L;

	public static bool InternetConnectionAvailable
	{
		get
		{
			return _internetConnectionAvailable;
		}
		private set
		{
			if (value != _internetConnectionAvailable)
			{
				_internetConnectionAvailable = value;
				OnInternetConnectionAvailabilityChanged?.Invoke(value);
			}
		}
	}

	private static async void CheckInternetConnection()
	{
		if (NetworkMain.GameClient != null)
		{
			InternetConnectionAvailable = await NetworkMain.GameClient.CheckConnection();
		}
		_lastInternetConnectionCheck = DateTime.Now.Ticks;
		_checkingConnection = false;
	}

	internal static void Tick(float dt)
	{
		long num = (InternetConnectionAvailable ? 300000000 : 100000000);
		if (Module.CurrentModule != null && Module.CurrentModule.StartupInfo.StartupType != GameStartupType.Singleplayer && !_checkingConnection && DateTime.Now.Ticks - _lastInternetConnectionCheck > num)
		{
			_checkingConnection = true;
			Task.Run(delegate
			{
				CheckInternetConnection();
			});
		}
	}
}
