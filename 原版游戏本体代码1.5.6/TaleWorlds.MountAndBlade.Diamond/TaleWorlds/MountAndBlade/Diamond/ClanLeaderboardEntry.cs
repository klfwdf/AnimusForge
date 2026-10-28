using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanLeaderboardEntry
{
	public Guid ClanId { get; private set; }

	public string Name { get; private set; }

	public string Tag { get; private set; }

	public string Sigil { get; private set; }

	public int WinCount { get; private set; }

	public int LossCount { get; private set; }

	public float Score { get; private set; }

	[JsonConstructor]
	public ClanLeaderboardEntry(Guid clanId, string name, string tag, string sigil, int winCount, int lossCount, float score)
	{
		ClanId = clanId;
		Name = name;
		Tag = tag;
		Sigil = sigil;
		WinCount = winCount;
		LossCount = lossCount;
		Score = score;
	}
}
