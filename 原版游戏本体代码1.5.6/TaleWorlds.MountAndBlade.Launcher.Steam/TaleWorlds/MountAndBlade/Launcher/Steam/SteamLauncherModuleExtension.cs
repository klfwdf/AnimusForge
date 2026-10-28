using System.Collections.Generic;
using Steamworks;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Launcher.Steam;

public class SteamLauncherModuleExtension : IPlatformModuleExtension
{
	private bool _steamInitialized;

	private List<string> _modulePaths;

	public SteamLauncherModuleExtension()
	{
		_modulePaths = new List<string>();
	}

	public void Initialize(List<string> args)
	{
		_steamInitialized = SteamAPI.Init();
		if (_steamInitialized)
		{
			if (SteamUser.BLoggedOn())
			{
				uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
				PublishedFileId_t[] array = null;
				if (numSubscribedItems == 0)
				{
					return;
				}
				array = new PublishedFileId_t[numSubscribedItems];
				SteamUGC.GetSubscribedItems(array, numSubscribedItems);
				for (int i = 0; i < numSubscribedItems; i++)
				{
					if (SteamUGC.GetItemInstallInfo(array[i], out var _, out var pchFolder, 4096u, out var _))
					{
						_modulePaths.Add(pchFolder);
					}
				}
			}
			else
			{
				Debug.Print("Steam user is not logged in. Please log in to Steam");
			}
		}
		else
		{
			Debug.Print("Could not initialize Steam");
		}
	}

	public string[] GetModulePaths()
	{
		return _modulePaths.ToArray();
	}

	public void Destroy()
	{
		if (_steamInitialized)
		{
			SteamAPI.Shutdown();
		}
	}

	public void SetLauncherMode(bool isLauncherModeActive)
	{
		if (_steamInitialized)
		{
			SteamUtils.SetGameLauncherMode(isLauncherModeActive);
		}
	}

	public bool CheckEntitlement(string title)
	{
		if (_steamInitialized)
		{
			int dLCCount = SteamApps.GetDLCCount();
			for (int i = 0; i < dLCCount; i++)
			{
				if (SteamApps.BGetDLCDataByIndex(i, out var pAppID, out var _, out var pchName, 128) && pchName == title && SteamApps.BIsSubscribedApp(pAppID))
				{
					return SteamApps.BIsDlcInstalled(pAppID);
				}
			}
		}
		else
		{
			Debug.Print("Could not initialize Steam");
		}
		return false;
	}
}
