using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBTestRun
{
	[EngineMethod("auto_continue", false, null, false)]
	int AutoContinue(int type);

	[EngineMethod("get_fps", false, null, false)]
	int GetFPS();

	[EngineMethod("enter_edit_mode", false, null, false)]
	bool EnterEditMode();

	[EngineMethod("open_scene", false, null, false)]
	bool OpenScene(string sceneName);

	[EngineMethod("close_scene", false, null, false)]
	bool CloseScene();

	[EngineMethod("save_scene", false, null, false)]
	bool SaveScene();

	[EngineMethod("open_default_scene", false, null, false)]
	bool OpenDefaultScene();

	[EngineMethod("leave_edit_mode", false, null, false)]
	bool LeaveEditMode();

	[EngineMethod("new_scene", false, null, false)]
	bool NewScene();

	[EngineMethod("start_mission", false, null, false)]
	void StartMission();
}
