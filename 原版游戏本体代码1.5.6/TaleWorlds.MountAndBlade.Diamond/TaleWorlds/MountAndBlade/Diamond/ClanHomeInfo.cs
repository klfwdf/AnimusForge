using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanHomeInfo
{
	[JsonProperty]
	public bool IsInClan { get; private set; }

	[JsonProperty]
	public bool CanCreateClan { get; private set; }

	[JsonProperty]
	public ClanInfo ClanInfo { get; private set; }

	[JsonProperty]
	public NotEnoughPlayersInfo NotEnoughPlayersInfo { get; private set; }

	[JsonProperty]
	public PlayerNotEligibleInfo[] PlayerNotEligibleInfos { get; private set; }

	[JsonProperty]
	public ClanPlayerInfo[] ClanPlayerInfos { get; private set; }

	public ClanHomeInfo(bool isInClan, bool canCreateClan, ClanInfo clanInfo, NotEnoughPlayersInfo notEnoughPlayersInfo, PlayerNotEligibleInfo[] playerNotEligibleInfos, ClanPlayerInfo[] clanPlayerInfos)
	{
		IsInClan = isInClan;
		CanCreateClan = canCreateClan;
		ClanInfo = clanInfo;
		NotEnoughPlayersInfo = notEnoughPlayersInfo;
		PlayerNotEligibleInfos = playerNotEligibleInfos;
		ClanPlayerInfos = clanPlayerInfos;
	}

	public static ClanHomeInfo CreateInClanInfo(ClanInfo clanInfo, ClanPlayerInfo[] clanPlayerInfos)
	{
		return new ClanHomeInfo(isInClan: true, canCreateClan: false, clanInfo, null, null, clanPlayerInfos);
	}

	public static ClanHomeInfo CreateCanCreateClanInfo()
	{
		return new ClanHomeInfo(isInClan: false, canCreateClan: true, null, null, null, null);
	}

	public static ClanHomeInfo CreateCantCreateClanInfo(NotEnoughPlayersInfo notEnoughPlayersInfo, PlayerNotEligibleInfo[] playerNotEligibleInfos)
	{
		return new ClanHomeInfo(isInClan: false, canCreateClan: false, null, notEnoughPlayersInfo, playerNotEligibleInfos, null);
	}

	public static ClanHomeInfo CreateInvalidStateClanInfo()
	{
		return new ClanHomeInfo(isInClan: false, canCreateClan: false, null, null, null, null);
	}
}
