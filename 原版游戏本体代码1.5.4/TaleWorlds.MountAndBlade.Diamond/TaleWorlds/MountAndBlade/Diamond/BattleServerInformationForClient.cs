using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public struct BattleServerInformationForClient
{
	public string MatchId { get; set; }

	public string ServerAddress { get; set; }

	public ushort ServerPort { get; set; }

	public int PeerIndex { get; set; }

	public int TeamNo { get; set; }

	public int SessionKey { get; set; }

	public string SceneName { get; set; }

	public string GameType { get; set; }
}
