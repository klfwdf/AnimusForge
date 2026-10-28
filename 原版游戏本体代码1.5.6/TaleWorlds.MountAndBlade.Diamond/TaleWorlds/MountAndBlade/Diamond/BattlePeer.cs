using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public class BattlePeer
{
	public int Index { get; private set; }

	public string Name { get; private set; }

	public PlayerId PlayerId => PlayerData.PlayerId;

	public int TeamNo { get; private set; }

	public BattleJoinType BattleJoinType { get; private set; }

	public bool Quit => QuitType != BattlePeerQuitType.None;

	public PlayerData PlayerData { get; private set; }

	public Dictionary<string, List<string>> UsedCosmetics { get; private set; }

	public int SessionKey { get; private set; }

	public BattlePeerQuitType QuitType { get; private set; }

	public BattlePeer(string name, PlayerData playerData, Dictionary<string, List<string>> usedCosmetics, int teamNo, BattleJoinType battleJoinType)
	{
		Index = -1;
		Name = name;
		PlayerData = playerData;
		UsedCosmetics = usedCosmetics;
		TeamNo = teamNo;
		BattleJoinType = battleJoinType;
	}

	internal void Flee()
	{
		QuitType = BattlePeerQuitType.Fled;
		Index = -1;
		SessionKey = 0;
	}

	internal void SetPlayerDisconnectdFromLobby()
	{
		QuitType = BattlePeerQuitType.DisconnectedFromLobby;
		Index = -1;
		SessionKey = 0;
	}

	internal void SetPlayerDisconnectdFromGameSession()
	{
		QuitType = BattlePeerQuitType.DisconnectedFromGameSession;
		Index = -1;
		SessionKey = 0;
	}

	public void Rejoin(int teamNo)
	{
		QuitType = BattlePeerQuitType.None;
		TeamNo = teamNo;
	}

	public void InitializeSession(int index, int sessionKey)
	{
		Index = index;
		SessionKey = sessionKey;
	}

	internal void SetPlayerKickedDueToFriendlyDamage()
	{
		QuitType = BattlePeerQuitType.KickedDueToFriendlyDamage;
		Index = -1;
		SessionKey = 0;
	}
}
