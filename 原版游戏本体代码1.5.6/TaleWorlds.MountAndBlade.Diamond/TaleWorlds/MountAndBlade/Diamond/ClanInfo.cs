using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanInfo
{
	[JsonProperty]
	public Guid ClanId { get; private set; }

	[JsonProperty]
	public string Name { get; private set; }

	[JsonProperty]
	public string Tag { get; private set; }

	[JsonProperty]
	public string Faction { get; private set; }

	[JsonProperty]
	public string Sigil { get; private set; }

	[JsonProperty]
	public string InformationText { get; private set; }

	[JsonProperty]
	public ClanPlayer[] Players { get; private set; }

	[JsonProperty]
	public ClanAnnouncement[] Announcements { get; private set; }

	public ClanInfo(Guid clanId, string name, string tag, string faction, string sigil, string information, ClanPlayer[] players, ClanAnnouncement[] announcements)
	{
		ClanId = clanId;
		Name = name;
		Tag = tag;
		Faction = faction;
		Sigil = sigil;
		Players = players;
		InformationText = information;
		Announcements = announcements;
	}

	public static ClanInfo CreateUnavailableClanInfo()
	{
		return new ClanInfo(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, new ClanPlayer[0], new ClanAnnouncement[0]);
	}
}
