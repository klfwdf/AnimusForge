using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBPeer
{
	[EngineMethod("set_user_data", false, null, false)]
	void SetUserData(int index, MBNetworkPeer data);

	[EngineMethod("set_controlled_agent", false, null, false)]
	void SetControlledAgent(int index, UIntPtr missionPointer, int agentIndex);

	[EngineMethod("set_team", false, null, false)]
	void SetTeam(int index, int teamIndex);

	[EngineMethod("is_active", false, null, false)]
	bool IsActive(int index);

	[EngineMethod("set_is_synchronized", false, null, false)]
	void SetIsSynchronized(int index, bool value);

	[EngineMethod("get_is_synchronized", false, null, false)]
	bool GetIsSynchronized(int index);

	[EngineMethod("send_existing_objects", false, null, false)]
	void SendExistingObjects(int index, UIntPtr missionPointer);

	[EngineMethod("begin_module_event", false, null, false)]
	void BeginModuleEvent(int index, bool isReliable);

	[EngineMethod("end_module_event", false, null, false)]
	void EndModuleEvent(bool isReliable);

	[EngineMethod("get_average_ping_in_milliseconds", false, null, false)]
	double GetAveragePingInMilliseconds(int index);

	[EngineMethod("get_average_loss_percent", false, null, false)]
	double GetAverageLossPercent(int index);

	[EngineMethod("set_relevant_game_options", false, null, false)]
	void SetRelevantGameOptions(int index, bool sendMeBloodEvents, bool sendMeSoundEvents);

	[EngineMethod("get_reversed_host", false, null, false)]
	uint GetReversedHost(int index);

	[EngineMethod("get_host", false, null, false)]
	uint GetHost(int index);

	[EngineMethod("get_port", false, null, false)]
	ushort GetPort(int index);
}
