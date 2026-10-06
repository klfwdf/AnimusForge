using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class ClanFriendListService : IFriendListService
{
	public const string CodeName = "ClanFriends";

	private readonly Dictionary<PlayerId, ClanPlayerInfo> _clanPlayerInfos;

	bool IFriendListService.InGameStatusFetchable => false;

	bool IFriendListService.AllowsFriendOperations => false;

	bool IFriendListService.CanInvitePlayersToPlatformSession => false;

	bool IFriendListService.IncludeInAllFriends => false;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<PlayerId> OnFriendRemoved;

	public event Action OnFriendListChanged;

	public ClanFriendListService()
	{
		_clanPlayerInfos = new Dictionary<PlayerId, ClanPlayerInfo>();
	}

	string IFriendListService.GetServiceCodeName()
	{
		return "ClanFriends";
	}

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=j4F7tTzy}Clan");
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.Clan;
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		return _clanPlayerInfos.Keys;
	}

	async Task<bool> IFriendListService.GetUserOnlineStatus(PlayerId providedId)
	{
		bool result = false;
		_clanPlayerInfos.TryGetValue(providedId, out var value);
		if (value != null)
		{
			result = value.State == AnotherPlayerState.InMultiplayerGame || value.State == AnotherPlayerState.AtLobby || value.State == AnotherPlayerState.InParty;
		}
		return await Task.FromResult(result);
	}

	async Task<bool> IFriendListService.IsPlayingThisGame(PlayerId providedId)
	{
		return await ((IFriendListService)this).GetUserOnlineStatus(providedId);
	}

	async Task<string> IFriendListService.GetUserName(PlayerId providedId)
	{
		_clanPlayerInfos.TryGetValue(providedId, out var value);
		return await Task.FromResult(value?.PlayerName);
	}

	public async Task<PlayerId> GetUserWithName(string name)
	{
		return await Task.FromResult(_clanPlayerInfos.Values.FirstOrDefaultQ((ClanPlayerInfo playerInfo) => playerInfo.PlayerName == name)?.PlayerId ?? PlayerId.Empty);
	}

	public IEnumerable<PlayerId> GetPendingRequests()
	{
		return null;
	}

	public IEnumerable<PlayerId> GetReceivedRequests()
	{
		return null;
	}

	private void Dummy()
	{
		if (this.OnUserStatusChanged != null)
		{
			this.OnUserStatusChanged(default(PlayerId));
		}
		if (this.OnFriendRemoved != null)
		{
			this.OnFriendRemoved(default(PlayerId));
		}
	}

	public void OnClanInfoChanged(List<ClanPlayerInfo> playerInfosInClan)
	{
		_clanPlayerInfos.Clear();
		if (playerInfosInClan != null)
		{
			foreach (ClanPlayerInfo item in playerInfosInClan)
			{
				_clanPlayerInfos.Add(item.PlayerId, item);
			}
		}
		this.OnFriendListChanged?.Invoke();
	}
}
