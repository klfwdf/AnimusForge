using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000034 RID: 52
	[ApplicationInterfaceBase]
	internal interface IThumbnailCreatorView
	{
		// Token: 0x0600053F RID: 1343
		[EngineMethod("create_thumbnail_creator_view", false, null, false)]
		ThumbnailCreatorView CreateThumbnailCreatorView();

		// Token: 0x06000540 RID: 1344
		[EngineMethod("register_scene", false, null, false)]
		void RegisterScene(UIntPtr pointer, UIntPtr scene_ptr, bool use_postfx);

		// Token: 0x06000541 RID: 1345
		[EngineMethod("clear_requests", false, null, false)]
		void ClearRequests(UIntPtr pointer);

		// Token: 0x06000542 RID: 1346
		[EngineMethod("cancel_request", false, null, false)]
		void CancelRequest(UIntPtr pointer, string render_id);

		// Token: 0x06000543 RID: 1347
		[EngineMethod("register_cached_entity", false, null, false)]
		void RegisterCachedEntity(UIntPtr pointer, UIntPtr scene, UIntPtr entity_ptr, string cacheId);

		// Token: 0x06000544 RID: 1348
		[EngineMethod("unregister_cached_entity", false, null, false)]
		void UnregisterCachedEntity(UIntPtr pointer, string cacheId);

		// Token: 0x06000545 RID: 1349
		[EngineMethod("register_render_request", false, null, false)]
		void RegisterRenderRequest(UIntPtr pointer, ref ThumbnailRenderRequest request);

		// Token: 0x06000546 RID: 1350
		[EngineMethod("get_number_of_pending_requests", false, null, false)]
		int GetNumberOfPendingRequests(UIntPtr pointer);

		// Token: 0x06000547 RID: 1351
		[EngineMethod("is_memory_cleared", false, null, false)]
		bool IsMemoryCleared(UIntPtr pointer);
	}
}
