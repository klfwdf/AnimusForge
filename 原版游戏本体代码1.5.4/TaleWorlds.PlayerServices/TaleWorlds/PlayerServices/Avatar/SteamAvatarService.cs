using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TaleWorlds.PlayerServices.Avatar;

public class SteamAvatarService : ApiAvatarServiceBase
{
	private class GetPlayerSummariesResult
	{
		public SteamPlayers response { get; set; }
	}

	private class SteamPlayers
	{
		public SteamPlayerSummary[] players { get; set; }
	}

	private class SteamPlayerSummary
	{
		public string avatar { get; set; }

		public string avatarfull { get; set; }

		public string avatarmedium { get; set; }

		public int communityvisibilitystate { get; set; }

		public int lastlogoff { get; set; }

		public string personaname { get; set; }

		public int personastate { get; set; }

		public int personastateflags { get; set; }

		public string primaryclanid { get; set; }

		public int profilestate { get; set; }

		public string profileurl { get; set; }

		public string realname { get; set; }

		public string steamid { get; set; }

		public int timecreated { get; set; }
	}

	private const int FetchTaskWaitTime = 3000;

	private const string SteamWebApiKey = "820D6EC50E6AAE61E460EA207D8966F7";

	private const int MaxAccountsPerRequest = 100;

	protected override async Task FetchAvatars()
	{
		await Task.Delay(3000);
		lock (base.WaitingAccounts)
		{
			if (base.WaitingAccounts.Count < 1)
			{
				return;
			}
			if (base.WaitingAccounts.Count <= 100)
			{
				base.InProgressAccounts = base.WaitingAccounts;
				base.WaitingAccounts = new List<(ulong, AvatarData)>();
			}
			else
			{
				base.InProgressAccounts = base.WaitingAccounts.GetRange(0, 100);
				base.WaitingAccounts.RemoveRange(0, 100);
			}
		}
		string address = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=820D6EC50E6AAE61E460EA207D8966F7&steamids=" + string.Join(",", base.InProgressAccounts.Select(((ulong accountId, AvatarData avatarData) a) => a.accountId));
		SteamPlayers steamPlayers = null;
		try
		{
			GetPlayerSummariesResult getPlayerSummariesResult = JsonConvert.DeserializeObject<GetPlayerSummariesResult>(await new TimeoutWebClient().DownloadStringTaskAsync(address));
			if (getPlayerSummariesResult?.response?.players != null && getPlayerSummariesResult.response.players.Length != 0)
			{
				steamPlayers = getPlayerSummariesResult.response;
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
		if (steamPlayers == null || steamPlayers.players.Length < 1)
		{
			foreach (var inProgressAccount in base.InProgressAccounts)
			{
				inProgressAccount.avatarData.SetFailed();
			}
			return;
		}
		List<Task> list = new List<Task>();
		foreach (var inProgressAccount2 in base.InProgressAccounts)
		{
			ulong item = inProgressAccount2.accountId;
			AvatarData item2 = inProgressAccount2.avatarData;
			string text = string.Concat(item);
			string text2 = null;
			SteamPlayerSummary[] players = steamPlayers.players;
			foreach (SteamPlayerSummary steamPlayerSummary in players)
			{
				if (steamPlayerSummary.steamid == text)
				{
					text2 = steamPlayerSummary.avatarfull;
					break;
				}
			}
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(UpdateAvatarImageData(item, text2, item2));
			}
			else
			{
				item2.SetFailed();
			}
		}
		if (list.Count > 0)
		{
			await Task.WhenAll(list);
		}
	}

	private async Task UpdateAvatarImageData(ulong accountId, string avatarUrl, AvatarData avatarData)
	{
		if (string.IsNullOrWhiteSpace(avatarUrl))
		{
			return;
		}
		byte[] array = await new TimeoutWebClient().DownloadDataTaskAsync(avatarUrl);
		if (array == null || array.Length == 0)
		{
			return;
		}
		avatarData.SetImageData(array);
		lock (base.AvatarImageCache)
		{
			base.AvatarImageCache[accountId] = avatarData;
		}
	}
}
