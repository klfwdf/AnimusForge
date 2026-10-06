using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBGame
{
	[EngineMethod("start_new", false, null, false)]
	void StartNew();

	[EngineMethod("load_module_data", false, null, false)]
	void LoadModuleData(bool isLoadGame);
}
