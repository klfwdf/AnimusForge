using System;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade.Missions;

namespace TaleWorlds.MountAndBlade;

internal sealed class DebugAgentScaleOnNetworkTestComponent : UdpNetworkComponent
{
	private float _lastTestSendTime;

	public override void OnUdpNetworkHandlerTick(float dt)
	{
		if (!GameNetwork.IsServer)
		{
			return;
		}
		float totalMissionTime = MBCommon.GetTotalMissionTime();
		if (_lastTestSendTime < totalMissionTime + 10f)
		{
			AgentReadOnlyList agents = Mission.Current.Agents;
			int count = agents.Count;
			_lastTestSendTime = totalMissionTime;
			int index = (int)(new Random().NextDouble() * (double)count);
			Agent agent = agents[index];
			if (agent.IsActive())
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new DebugAgentScaleOnNetworkTest(agent.Index, agent.AgentScale));
				GameNetwork.EndBroadcastModuleEventUnreliable(GameNetwork.EventBroadcastFlags.None);
			}
		}
	}

	public DebugAgentScaleOnNetworkTestComponent()
	{
		if (GameNetwork.IsClientOrReplay)
		{
			AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
		}
	}

	public override void OnUdpNetworkHandlerClose()
	{
		base.OnUdpNetworkHandlerClose();
		if (GameNetwork.IsClientOrReplay)
		{
			AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
		}
	}

	private static void HandleServerMessageDebugAgentScaleOnNetworkTest(DebugAgentScaleOnNetworkTest message)
	{
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(message.AgentToTestIndex, canBeNull: true);
		if (agentFromIndex != null && agentFromIndex.IsActive())
		{
			CompressionMission.DebugScaleValueCompressionInfo.GetPrecision();
			_ = agentFromIndex.AgentScale;
		}
	}

	private static void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
	{
		new GameNetwork.NetworkMessageHandlerRegisterer(mode).Register<DebugAgentScaleOnNetworkTest>(HandleServerMessageDebugAgentScaleOnNetworkTest);
	}
}
