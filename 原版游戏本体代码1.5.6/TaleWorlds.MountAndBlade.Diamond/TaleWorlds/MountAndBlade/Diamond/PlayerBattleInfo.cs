using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerBattleInfo
{
	public enum State
	{
		Created,
		AssignedToBattle,
		AtBattle,
		Disconnected,
		Fled
	}

	private State _state;

	public PlayerId PlayerId { get; set; }

	public string Name { get; set; }

	public int TeamNo { get; set; }

	public bool Fled => _state == State.Fled;

	public bool Disconnected => _state == State.Disconnected;

	public BattleJoinType JoinType { get; set; }

	public int PeerIndex { get; set; }

	public State CurrentState => _state;

	public PlayerBattleInfo()
	{
	}

	public PlayerBattleInfo(PlayerId playerId, string name, int teamNo)
	{
		PlayerId = playerId;
		Name = name;
		TeamNo = teamNo;
		PeerIndex = -1;
		_state = State.AssignedToBattle;
	}

	public PlayerBattleInfo(PlayerId playerId, string name, int teamNo, int peerIndex, State state)
	{
		PlayerId = playerId;
		Name = name;
		TeamNo = teamNo;
		PeerIndex = peerIndex;
		_state = state;
	}

	public void Flee()
	{
		if (_state != State.Disconnected && _state != State.AtBattle)
		{
			throw new Exception("PlayerBattleInfo incorrect state, expected AtBattle or Disconnected; got " + _state);
		}
		_state = State.Fled;
	}

	public void Disconnect()
	{
		if (_state != State.AtBattle)
		{
			throw new Exception("PlayerBattleInfo incorrect state, expected AtBattle got " + _state);
		}
		_state = State.Disconnected;
	}

	public void Initialize(int peerIndex)
	{
		if (_state != State.AssignedToBattle)
		{
			throw new Exception("PlayerBattleInfo incorrect state, expected AssignedToBattle got " + _state);
		}
		PeerIndex = peerIndex;
		_state = State.AtBattle;
	}

	public void RejoinBattle(int teamNo)
	{
		if (_state != State.Disconnected)
		{
			throw new Exception("PlayerBattleInfo incorrect state, expected Disconnected got " + _state);
		}
		TeamNo = teamNo;
		PeerIndex = -1;
		_state = State.AssignedToBattle;
	}

	public PlayerBattleInfo Clone()
	{
		return new PlayerBattleInfo(PlayerId, Name, TeamNo, PeerIndex, _state);
	}
}
