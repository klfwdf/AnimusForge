using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000031 RID: 49
	[ApplicationInterfaceBase]
	internal interface ITableauView
	{
		// Token: 0x06000533 RID: 1331
		[EngineMethod("create_tableau_view", false, null, false)]
		TableauView CreateTableauView(string viewName);

		// Token: 0x06000534 RID: 1332
		[EngineMethod("set_sort_meshes", false, null, false)]
		void SetSortingEnabled(UIntPtr pointer, bool value);

		// Token: 0x06000535 RID: 1333
		[EngineMethod("set_continous_rendering", false, null, false)]
		void SetContinousRendering(UIntPtr pointer, bool value);

		// Token: 0x06000536 RID: 1334
		[EngineMethod("set_do_not_render_this_frame", false, null, false)]
		void SetDoNotRenderThisFrame(UIntPtr pointer, bool value);

		// Token: 0x06000537 RID: 1335
		[EngineMethod("set_delete_after_rendering", false, null, false)]
		void SetDeleteAfterRendering(UIntPtr pointer, bool value);
	}
}
