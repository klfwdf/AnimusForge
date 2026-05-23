using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public static class PermaMuteList
{
	private static Dictionary<string, List<(string Id, string Name)>> _mutedPlayers;

	private static string CurrentPlayerId;

	private static Func<bool> _getPermanentMuteAvailable;

	public static bool HasMutedPlayersLoaded { get; private set; }

	private static PlatformFilePath PermaMuteFilePath
	{
		get
		{
			PlatformDirectoryPath folderPath = new PlatformDirectoryPath(PlatformFileType.User, "Data");
			return new PlatformFilePath(folderPath, "Muted.json");
		}
	}

	public static IReadOnlyList<(string Id, string Name)> MutedPlayers
	{
		get
		{
			if (!HasMutedPlayersLoaded || !_mutedPlayers.TryGetValue(CurrentPlayerId, out List<(string, string)> value))
			{
				return new List<(string, string)>();
			}
			return value;
		}
	}

	static PermaMuteList()
	{
		_mutedPlayers = new Dictionary<string, List<(string, string)>>();
	}

	public static void SetPermanentMuteAvailableCallback(Func<bool> getPermanentMuteAvailable)
	{
		_getPermanentMuteAvailable = getPermanentMuteAvailable;
	}

	public static async Task LoadMutedPlayers(PlayerId currentPlayerId)
	{
		CurrentPlayerId = currentPlayerId.ToString();
		if (!FileHelper.FileExists(PermaMuteFilePath))
		{
			return;
		}
		try
		{
			Dictionary<string, List<(string, string)>> dictionary = JsonConvert.DeserializeObject<Dictionary<string, List<(string, string)>>>(await FileHelper.GetFileContentStringAsync(PermaMuteFilePath));
			if (dictionary != null)
			{
				_mutedPlayers = dictionary;
			}
			HasMutedPlayersLoaded = true;
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Could not load muted players. " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\PermaMuteList.cs", "LoadMutedPlayers", 61);
			try
			{
				FileHelper.DeleteFile(PermaMuteFilePath);
			}
			catch (Exception ex2)
			{
				Debug.FailedAssert("Could not delete muted players file. " + ex2.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\PermaMuteList.cs", "LoadMutedPlayers", 68);
			}
		}
	}

	public static async void SaveMutedPlayers()
	{
		try
		{
			byte[] data = Common.SerializeObjectAsJson(_mutedPlayers);
			await FileHelper.SaveFileAsync(PermaMuteFilePath, data);
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Could not save muted players. " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\PermaMuteList.cs", "SaveMutedPlayers", 83);
		}
	}

	public static bool IsPlayerMuted(PlayerId player)
	{
		Func<bool> getPermanentMuteAvailable = _getPermanentMuteAvailable;
		if ((getPermanentMuteAvailable == null || getPermanentMuteAvailable()) && CurrentPlayerId != null)
		{
			string text = player.ToString();
			lock (_mutedPlayers)
			{
				if (!_mutedPlayers.TryGetValue(CurrentPlayerId, out List<(string, string)> value))
				{
					return false;
				}
				foreach (var item in value)
				{
					if (item.Item1 == text)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static void MutePlayer(PlayerId player, string name)
	{
		Func<bool> getPermanentMuteAvailable = _getPermanentMuteAvailable;
		if (getPermanentMuteAvailable != null && !getPermanentMuteAvailable())
		{
			return;
		}
		lock (_mutedPlayers)
		{
			if (!_mutedPlayers.TryGetValue(CurrentPlayerId, out List<(string, string)> value))
			{
				value = new List<(string, string)>();
				_mutedPlayers.Add(CurrentPlayerId, value);
			}
			value.Add((player.ToString(), name));
		}
	}

	public static void RemoveMutedPlayer(PlayerId player)
	{
		Func<bool> getPermanentMuteAvailable = _getPermanentMuteAvailable;
		if (getPermanentMuteAvailable != null && !getPermanentMuteAvailable())
		{
			return;
		}
		string text = player.ToString();
		lock (_mutedPlayers)
		{
			if (!_mutedPlayers.TryGetValue(CurrentPlayerId, out List<(string, string)> value))
			{
				return;
			}
			int num = -1;
			for (int i = 0; i < value.Count; i++)
			{
				if (value[i].Item1 == text)
				{
					num = i;
					break;
				}
			}
			if (num >= 0)
			{
				value.RemoveAt(num);
			}
		}
	}
}
