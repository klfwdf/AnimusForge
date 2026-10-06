using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IView
{
	[EngineMethod("set_render_option", false, null, false)]
	void SetRenderOption(UIntPtr ptr, int optionEnum, bool value);

	[EngineMethod("set_render_order", false, null, false)]
	void SetRenderOrder(UIntPtr ptr, int value);

	[EngineMethod("set_render_target", false, null, false)]
	void SetRenderTarget(UIntPtr ptr, UIntPtr texture_ptr);

	[EngineMethod("set_depth_target", false, null, false)]
	void SetDepthTarget(UIntPtr ptr, UIntPtr texture_ptr);

	[EngineMethod("set_scale", false, null, false)]
	void SetScale(UIntPtr ptr, float x, float y);

	[EngineMethod("set_offset", false, null, false)]
	void SetOffset(UIntPtr ptr, float x, float y);

	[EngineMethod("set_debug_render_functionality", false, null, false)]
	void SetDebugRenderFunctionality(UIntPtr ptr, bool value);

	[EngineMethod("set_clear_color", false, null, false)]
	void SetClearColor(UIntPtr ptr, uint rgba);

	[EngineMethod("set_enable", false, null, false)]
	void SetEnable(UIntPtr ptr, bool value);

	[EngineMethod("set_render_on_demand", false, null, false)]
	void SetRenderOnDemand(UIntPtr ptr, bool value);

	[EngineMethod("set_auto_depth_creation", false, null, false)]
	void SetAutoDepthTargetCreation(UIntPtr ptr, bool value);

	[EngineMethod("set_save_final_result_to_disk", false, null, false)]
	void SetSaveFinalResultToDisk(UIntPtr ptr, bool value);

	[EngineMethod("set_file_name_to_save_result", false, null, false)]
	void SetFileNameToSaveResult(UIntPtr ptr, string name);

	[EngineMethod("set_file_type_to_save", false, null, false)]
	void SetFileTypeToSave(UIntPtr ptr, int type);

	[EngineMethod("set_file_path_to_save_result", false, null, false)]
	void SetFilePathToSaveResult(UIntPtr ptr, string name);
}
