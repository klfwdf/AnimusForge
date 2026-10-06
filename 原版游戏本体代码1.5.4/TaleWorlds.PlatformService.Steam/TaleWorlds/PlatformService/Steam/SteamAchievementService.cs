using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using TaleWorlds.AchievementSystem;

namespace TaleWorlds.PlatformService.Steam;

public class SteamAchievementService : IAchievementService
{
	private SteamPlatformServices _platform;

	private float _statsInvalidatedElapsed = -1f;

	private float _statsStoredElapsed;

	private Callback<UserStatsReceived_t> _userStatsReceivedT;

	private Callback<UserStatsStored_t> _userStatsStoredT;

	private bool _statsInitialized;

	private const int StatInvalidationInterval = 5;

	private const int StoreStatsInterval = 60;

	public SteamAchievementService(SteamPlatformServices steamPlatformServices)
	{
		_platform = steamPlatformServices;
	}

	bool IAchievementService.SetStat(string name, int value)
	{
		return SetStat(name, value);
	}

	Task<int> IAchievementService.GetStat(string name)
	{
		return GetStat(name);
	}

	Task<int[]> IAchievementService.GetStats(string[] names)
	{
		return GetStats(names);
	}

	bool IAchievementService.IsInitializationCompleted()
	{
		return true;
	}

	public void Tick(float dt)
	{
		StoreStats();
		if (_statsInvalidatedElapsed != -1f)
		{
			_statsInvalidatedElapsed += dt;
		}
		_statsStoredElapsed += dt;
	}

	private bool IsAchievementUnlocked(string id)
	{
		SteamUserStats.StoreStats();
		SteamUserStats.GetAchievement(id, out var pbAchieved);
		return pbAchieved;
	}

	private void ClearAchievement(string name)
	{
		SteamUserStats.ClearAchievement(name);
		SteamUserStats.StoreStats();
	}

	public void Initialize()
	{
		SteamUserStats.RequestCurrentStats();
		_userStatsReceivedT = Callback<UserStatsReceived_t>.Create(UserStatsReceived);
		_userStatsStoredT = Callback<UserStatsStored_t>.Create(UserStatsStored);
	}

	private void UserStatsReceived(UserStatsReceived_t userStatsReceivedT)
	{
		if (SteamUtils.GetAppID().m_AppId == userStatsReceivedT.m_nGameID && userStatsReceivedT.m_eResult == EResult.k_EResultOK)
		{
			_statsInitialized = true;
		}
	}

	private void UserStatsStored(UserStatsStored_t userStatsStoredT)
	{
	}

	internal bool SetStat(string name, int value)
	{
		if (!_statsInitialized)
		{
			return false;
		}
		if (!SteamUserStats.SetStat(name, value))
		{
			return false;
		}
		InvalidateStats();
		return true;
	}

	internal Task<int> GetStat(string name)
	{
		if (!_statsInitialized)
		{
			return Task.FromResult(-1);
		}
		int pData = -1;
		SteamUserStats.GetStat(name, out pData);
		return Task.FromResult(pData);
	}

	internal Task<int[]> GetStats(string[] names)
	{
		if (!_statsInitialized)
		{
			return Task.FromResult<int[]>(null);
		}
		List<int> list = new List<int>();
		foreach (string pchName in names)
		{
			int pData = -1;
			SteamUserStats.GetStat(pchName, out pData);
			list.Add(pData);
		}
		return Task.FromResult(list.ToArray());
	}

	private void InvalidateStats()
	{
		_statsInvalidatedElapsed = 0f;
	}

	private void StoreStats()
	{
		if (_statsInvalidatedElapsed > 5f && _statsStoredElapsed > 60f)
		{
			_statsStoredElapsed = 0f;
			if (SteamUserStats.StoreStats())
			{
				_statsInvalidatedElapsed = -1f;
			}
		}
	}
}
