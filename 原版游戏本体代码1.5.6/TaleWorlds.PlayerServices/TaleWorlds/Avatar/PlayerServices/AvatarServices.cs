using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.Avatar.PlayerServices;

public static class AvatarServices
{
	private static Dictionary<PlayerIdProvidedTypes, IAvatarService> _allAvatarServices;

	private static ForcedAvatarService _forcedAvatarService;

	public static int ForcedAvatarCount { get; private set; }

	public static int GetForcedAvatarIndexOfPlayer(PlayerId playerID)
	{
		return MathF.Abs(playerID.GetHashCode()) % ForcedAvatarCount;
	}

	static AvatarServices()
	{
		_allAvatarServices = new Dictionary<PlayerIdProvidedTypes, IAvatarService>();
		AddAvatarService(PlayerIdProvidedTypes.Steam, new SteamAvatarService());
		AddAvatarService(PlayerIdProvidedTypes.GOG, new GOGAvatarService());
		InitializeFallbackAvatarService();
	}

	public static void UpdateAvatarServices(float dt)
	{
		foreach (IAvatarService value in _allAvatarServices.Values)
		{
			value.Tick(dt);
		}
	}

	public static AvatarDataResponse GetPlayerAvatar(PlayerId playerId, int forcedIndex)
	{
		_allAvatarServices.TryGetValue(playerId.ProvidedType, out var value);
		if (forcedIndex >= 0 || (forcedIndex < 0 && value == null))
		{
			value = _forcedAvatarService;
		}
		if (value != null && !value.IsInitialized())
		{
			value?.Initialize();
		}
		return new AvatarDataResponse(value == _forcedAvatarService, value.GetPlayerAvatar(playerId));
	}

	private static void InitializeFallbackAvatarService()
	{
		_forcedAvatarService = new ForcedAvatarService();
		_forcedAvatarService.Initialize();
		ForcedAvatarCount = _forcedAvatarService.AvatarCount;
		AddAvatarService(PlayerIdProvidedTypes.Forced, _forcedAvatarService);
	}

	public static void AddAvatarService(PlayerIdProvidedTypes type, IAvatarService avatarService)
	{
		if (_allAvatarServices.ContainsKey(type))
		{
			_allAvatarServices[type] = avatarService;
		}
		else
		{
			_allAvatarServices.Add(type, avatarService);
		}
	}

	public static void ClearAvatarCaches()
	{
		foreach (KeyValuePair<PlayerIdProvidedTypes, IAvatarService> allAvatarService in _allAvatarServices)
		{
			allAvatarService.Value.ClearCache();
		}
	}
}
