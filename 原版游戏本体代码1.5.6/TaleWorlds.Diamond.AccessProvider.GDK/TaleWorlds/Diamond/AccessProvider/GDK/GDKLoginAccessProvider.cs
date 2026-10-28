using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.GDK;

public class GDKLoginAccessProvider : ILoginAccessProvider
{
	private PlatformInitParams _initParams;

	private string _gamerTag;

	private ulong _xuid;

	private PlayerId _playerId;

	private TextObject _initializationFailReason;

	private string _token;

	public GDKLoginAccessProvider(string gamerTag, ulong xuid, string token, PlayerId playerId, TextObject initializationFailReason)
	{
		_gamerTag = gamerTag;
		_playerId = playerId;
		_initializationFailReason = initializationFailReason;
		_token = token;
		_xuid = xuid;
	}

	void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
	{
		_initParams = initParams;
	}

	string ILoginAccessProvider.GetUserName()
	{
		return _gamerTag;
	}

	PlayerId ILoginAccessProvider.GetPlayerId()
	{
		return _playerId;
	}

	AccessObjectResult ILoginAccessProvider.CreateAccessObject()
	{
		if (_initializationFailReason != null)
		{
			return AccessObjectResult.CreateFailed(_initializationFailReason);
		}
		if (_playerId == PlayerId.Empty)
		{
			return AccessObjectResult.CreateFailed(new TextObject("{=leU2EiDo}Could not initialize Xbox Network player on lobby server"));
		}
		return AccessObjectResult.CreateSuccess(new GDKAccessObject
		{
			Id = _xuid.ToString(),
			Token = _token
		});
	}
}
