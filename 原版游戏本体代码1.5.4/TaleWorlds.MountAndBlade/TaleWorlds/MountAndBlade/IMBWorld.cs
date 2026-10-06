using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBWorld
{
	[EngineMethod("get_global_time", false, null, false)]
	float GetGlobalTime(MBCommon.TimeType timeType);

	[EngineMethod("get_last_messages", false, null, false)]
	string GetLastMessages();

	[EngineMethod("get_game_type", false, null, false)]
	int GetGameType();

	[EngineMethod("set_game_type", false, null, false)]
	void SetGameType(int gameType);

	[EngineMethod("pause_game", false, null, false)]
	void PauseGame();

	[EngineMethod("unpause_game", false, null, false)]
	void UnpauseGame();

	[EngineMethod("set_mesh_used", false, null, false)]
	void SetMeshUsed(string meshName);

	[EngineMethod("set_material_used", false, null, false)]
	void SetMaterialUsed(string materialName);

	[EngineMethod("set_body_used", false, null, false)]
	void SetBodyUsed(string bodyName);

	[EngineMethod("fix_skeletons", false, null, false)]
	void FixSkeletons();

	[EngineMethod("check_resource_modifications", false, null, false)]
	void CheckResourceModifications();
}
