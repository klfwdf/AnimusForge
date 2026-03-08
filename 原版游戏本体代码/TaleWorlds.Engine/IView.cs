using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000030 RID: 48
	[ApplicationInterfaceBase]
	internal interface IView
	{
		// Token: 0x06000524 RID: 1316
		[EngineMethod("set_render_option", false, null, false)]
		void SetRenderOption(UIntPtr ptr, int optionEnum, bool value);

		// Token: 0x06000525 RID: 1317
		[EngineMethod("set_render_order", false, null, false)]
		void SetRenderOrder(UIntPtr ptr, int value);

		// Token: 0x06000526 RID: 1318
		[EngineMethod("set_render_target", false, null, false)]
		void SetRenderTarget(UIntPtr ptr, UIntPtr texture_ptr);

		// Token: 0x06000527 RID: 1319
		[EngineMethod("set_depth_target", false, null, false)]
		void SetDepthTarget(UIntPtr ptr, UIntPtr texture_ptr);

		// Token: 0x06000528 RID: 1320
		[EngineMethod("set_scale", false, null, false)]
		void SetScale(UIntPtr ptr, float x, float y);

		// Token: 0x06000529 RID: 1321
		[EngineMethod("set_offset", false, null, false)]
		void SetOffset(UIntPtr ptr, float x, float y);

		// Token: 0x0600052A RID: 1322
		[EngineMethod("set_debug_render_functionality", false, null, false)]
		void SetDebugRenderFunctionality(UIntPtr ptr, bool value);

		// Token: 0x0600052B RID: 1323
		[EngineMethod("set_clear_color", false, null, false)]
		void SetClearColor(UIntPtr ptr, uint rgba);

		// Token: 0x0600052C RID: 1324
		[EngineMethod("set_enable", false, null, false)]
		void SetEnable(UIntPtr ptr, bool value);

		// Token: 0x0600052D RID: 1325
		[EngineMethod("set_render_on_demand", false, null, false)]
		void SetRenderOnDemand(UIntPtr ptr, bool value);

		// Token: 0x0600052E RID: 1326
		[EngineMethod("set_auto_depth_creation", false, null, false)]
		void SetAutoDepthTargetCreation(UIntPtr ptr, bool value);

		// Token: 0x0600052F RID: 1327
		[EngineMethod("set_save_final_result_to_disk", false, null, false)]
		void SetSaveFinalResultToDisk(UIntPtr ptr, bool value);

		// Token: 0x06000530 RID: 1328
		[EngineMethod("set_file_name_to_save_result", false, null, false)]
		void SetFileNameToSaveResult(UIntPtr ptr, string name);

		// Token: 0x06000531 RID: 1329
		[EngineMethod("set_file_type_to_save", false, null, false)]
		void SetFileTypeToSave(UIntPtr ptr, int type);

		// Token: 0x06000532 RID: 1330
		[EngineMethod("set_file_path_to_save_result", false, null, false)]
		void SetFilePathToSaveResult(UIntPtr ptr, string name);
	}
}
