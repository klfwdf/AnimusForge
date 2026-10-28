using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService.Steam;

public class SteamPlatformAvatarService : IAvatarService
{
	private SteamPlatformServices _steamPlatformServices;

	public SteamPlatformAvatarService(SteamPlatformServices steamPlatformServices)
	{
		_steamPlatformServices = steamPlatformServices;
	}

	public AvatarData GetPlayerAvatar(PlayerId playerId)
	{
		AvatarData avatarData = new AvatarData();
		FetchPlayerAvatar(avatarData, playerId);
		return avatarData;
	}

	public async void FetchPlayerAvatar(AvatarData avatarData, PlayerId playerId)
	{
		AvatarData avatarData2 = await ((IPlatformServices)_steamPlatformServices).GetUserAvatar(playerId);
		if (avatarData2 != null)
		{
			if (avatarData2.Width != 0 && avatarData2.Height != 0)
			{
				avatarData.SetImageData(avatarData2.Image, avatarData2.Width, avatarData2.Height);
			}
			else
			{
				avatarData.SetImageData(avatarData2.Image);
			}
		}
		else
		{
			avatarData.SetFailed();
		}
	}

	public void Initialize()
	{
	}

	public void ClearCache()
	{
		_steamPlatformServices.ClearAvatarCache();
	}

	public bool IsInitialized()
	{
		return true;
	}

	public void Tick(float dt)
	{
	}
}
