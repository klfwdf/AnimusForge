using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ITableauView
{
	[EngineMethod("create_tableau_view", false, null, false)]
	TableauView CreateTableauView(string viewName);

	[EngineMethod("set_sort_meshes", false, null, false)]
	void SetSortingEnabled(UIntPtr pointer, bool value);

	[EngineMethod("set_continous_rendering", false, null, false)]
	void SetContinousRendering(UIntPtr pointer, bool value);

	[EngineMethod("set_do_not_render_this_frame", false, null, false)]
	void SetDoNotRenderThisFrame(UIntPtr pointer, bool value);

	[EngineMethod("set_delete_after_rendering", false, null, false)]
	void SetDeleteAfterRendering(UIntPtr pointer, bool value);
}
