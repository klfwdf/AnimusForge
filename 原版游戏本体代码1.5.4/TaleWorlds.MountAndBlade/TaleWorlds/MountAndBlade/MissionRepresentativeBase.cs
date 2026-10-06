using System;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionRepresentativeBase : PeerComponent
{
	protected enum PlayerTypes
	{
		Bot,
		Client,
		Server
	}

	private int _gold;

	private MissionPeer _missionPeer;

	protected PlayerTypes PlayerType
	{
		get
		{
			if (base.Peer.Communicator.IsNetworkActive)
			{
				if (!base.Peer.Communicator.IsServerPeer)
				{
					return PlayerTypes.Client;
				}
				return PlayerTypes.Server;
			}
			return PlayerTypes.Bot;
		}
	}

	public Agent ControlledAgent { get; private set; }

	public int Gold
	{
		get
		{
			if (_gold < 0)
			{
				return _gold;
			}
			MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.UnlimitedGold).GetValue(out bool value);
			if (!value)
			{
				return _gold;
			}
			return 2000;
		}
		private set
		{
			if (value < 0)
			{
				_gold = value;
				return;
			}
			MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.UnlimitedGold).GetValue(out bool value2);
			_gold = ((!value2) ? value : 2000);
		}
	}

	public MissionPeer MissionPeer
	{
		get
		{
			if (_missionPeer == null)
			{
				_missionPeer = GetComponent<MissionPeer>();
			}
			return _missionPeer;
		}
	}

	public event Action OnGoldUpdated;

	public void SetAgent(Agent agent)
	{
		ControlledAgent = agent;
		if (ControlledAgent != null)
		{
			ControlledAgent.SetMissionRepresentative(this);
			OnAgentSpawned();
		}
	}

	public virtual void OnAgentSpawned()
	{
	}

	public virtual void Tick(float dt)
	{
	}

	public void UpdateGold(int gold)
	{
		Gold = gold;
		this.OnGoldUpdated?.Invoke();
	}
}
