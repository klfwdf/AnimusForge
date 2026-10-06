using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Diamond.AccessProvider.Test;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService;

public class TestFriendListService : IFriendListService
{
	private string _userName;

	private PlayerId _playerId;

	private Dictionary<PlayerId, string> _testUserNames;

	private Dictionary<string, PlayerId> _testUserPlayerIds;

	bool IFriendListService.InGameStatusFetchable => true;

	bool IFriendListService.AllowsFriendOperations => false;

	bool IFriendListService.CanInvitePlayersToPlatformSession => false;

	bool IFriendListService.IncludeInAllFriends => true;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<PlayerId> OnFriendRemoved;

	public event Action OnFriendListChanged;

	public TestFriendListService(string userName, PlayerId myPlayerId)
	{
		_userName = userName;
		_playerId = myPlayerId;
		_testUserNames = new Dictionary<PlayerId, string>();
		_testUserPlayerIds = new Dictionary<string, PlayerId>();
		_testUserNames.Add(_playerId, _userName);
		_testUserPlayerIds.Add(_userName, _playerId);
		for (int i = 1; i <= 12; i++)
		{
			string text = "TestPlayer" + i;
			PlayerId playerIdFromUserName = TestLoginAccessProvider.GetPlayerIdFromUserName(text);
			if (!_testUserNames.ContainsKey(playerIdFromUserName))
			{
				_testUserNames.Add(playerIdFromUserName, text);
				_testUserPlayerIds.Add(text, playerIdFromUserName);
			}
		}
	}

	string IFriendListService.GetServiceCodeName()
	{
		return "Test";
	}

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=!}Test");
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.Test;
	}

	Task<bool> IFriendListService.GetUserOnlineStatus(PlayerId providedId)
	{
		if (_testUserNames.ContainsKey(providedId))
		{
			return Task.FromResult(result: true);
		}
		return Task.FromResult(result: false);
	}

	Task<bool> IFriendListService.IsPlayingThisGame(PlayerId providedId)
	{
		if (_testUserNames.ContainsKey(providedId))
		{
			return Task.FromResult(result: true);
		}
		return Task.FromResult(result: false);
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		List<string> list = new List<string>();
		if (_userName == "TestPlayer1" || _userName == "TestPlayer2" || _userName == "TestPlayer3" || _userName == "TestPlayer4" || _userName == "TestPlayer5" || _userName == "TestPlayer6")
		{
			list.Add("TestPlayer1");
			list.Add("TestPlayer2");
			list.Add("TestPlayer3");
			list.Add("TestPlayer4");
			list.Add("TestPlayer5");
			list.Add("TestPlayer6");
		}
		else if (_userName == "TestPlayer7" || _userName == "TestPlayer8" || _userName == "TestPlayer9" || _userName == "TestPlayer10" || _userName == "TestPlayer11" || _userName == "TestPlayer12")
		{
			list.Add("TestPlayer7");
			list.Add("TestPlayer8");
			list.Add("TestPlayer9");
			list.Add("TestPlayer10");
			list.Add("TestPlayer11");
			list.Add("TestPlayer12");
		}
		else
		{
			list.Add("TestPlayer1");
			list.Add("TestPlayer2");
			list.Add("TestPlayer3");
			list.Add("TestPlayer4");
			list.Add("TestPlayer5");
			list.Add("TestPlayer6");
			list.Add("TestPlayer7");
			list.Add("TestPlayer8");
			list.Add("TestPlayer9");
			list.Add("TestPlayer10");
			list.Add("TestPlayer11");
			list.Add("TestPlayer12");
		}
		foreach (string item in list)
		{
			if (_userName != item)
			{
				yield return TestLoginAccessProvider.GetPlayerIdFromUserName(item);
			}
		}
	}

	IEnumerable<PlayerId> IFriendListService.GetPendingRequests()
	{
		return null;
	}

	IEnumerable<PlayerId> IFriendListService.GetReceivedRequests()
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
		if (this.OnFriendListChanged != null)
		{
			this.OnFriendListChanged();
		}
	}

	Task<string> IFriendListService.GetUserName(PlayerId providedId)
	{
		string result = "-";
		if (_testUserNames.TryGetValue(providedId, out var value))
		{
			result = value;
		}
		return Task.FromResult(result);
	}

	Task<PlayerId> IFriendListService.GetUserWithName(string name)
	{
		PlayerId result = default(PlayerId);
		if (_testUserPlayerIds.TryGetValue(name, out var value))
		{
			result = value;
		}
		return Task.FromResult(result);
	}
}
