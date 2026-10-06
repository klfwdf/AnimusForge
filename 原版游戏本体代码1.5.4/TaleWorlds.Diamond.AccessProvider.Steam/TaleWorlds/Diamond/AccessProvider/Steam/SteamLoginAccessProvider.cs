using System;
using System.Text;
using Steamworks;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.Steam;

public class SteamLoginAccessProvider : ILoginAccessProvider
{
	private string _steamUserName;

	private ulong _steamId;

	private PlatformInitParams _initParams;

	private uint _appId;

	private int AppId => Convert.ToInt32(_appId);

	void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
	{
		if (SteamAPI.Init() && Packsize.Test())
		{
			_appId = SteamUtils.GetAppID().m_AppId;
			_steamId = SteamUser.GetSteamID().m_SteamID;
			_steamUserName = SteamFriends.GetPersonaName();
			_initParams = initParams;
		}
	}

	string ILoginAccessProvider.GetUserName()
	{
		return _steamUserName;
	}

	PlayerId ILoginAccessProvider.GetPlayerId()
	{
		return new PlayerId(2, 0uL, _steamId);
	}

	AccessObjectResult ILoginAccessProvider.CreateAccessObject()
	{
		if (!SteamAPI.IsSteamRunning())
		{
			return AccessObjectResult.CreateFailed(new TextObject("{=uunRVBPN}Steam is not running."));
		}
		byte[] array = new byte[1024];
		if (SteamUser.GetAuthSessionTicket(array, 1024, out var _) == HAuthTicket.Invalid)
		{
			return AccessObjectResult.CreateFailed(new TextObject("{=XSU8Bbbb}Invalid Steam session."));
		}
		StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			stringBuilder.AppendFormat("{0:x2}", b);
		}
		string externalAccessToken = stringBuilder.ToString();
		return AccessObjectResult.CreateSuccess(new SteamAccessObject(_steamUserName, externalAccessToken, AppId));
	}
}
