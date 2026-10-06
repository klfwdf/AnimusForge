using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IThumbnailCreatorView
{
	[EngineMethod("create_thumbnail_creator_view", false, null, false)]
	ThumbnailCreatorView CreateThumbnailCreatorView();

	[EngineMethod("register_scene", false, null, false)]
	void RegisterScene(UIntPtr pointer, UIntPtr scene_ptr, bool use_postfx);

	[EngineMethod("clear_requests", false, null, false)]
	void ClearRequests(UIntPtr pointer);

	[EngineMethod("cancel_request", false, null, false)]
	void CancelRequest(UIntPtr pointer, string render_id);

	[EngineMethod("register_cached_entity", false, null, false)]
	void RegisterCachedEntity(UIntPtr pointer, UIntPtr scene, UIntPtr entity_ptr, string cacheId);

	[EngineMethod("unregister_cached_entity", false, null, false)]
	void UnregisterCachedEntity(UIntPtr pointer, string cacheId);

	[EngineMethod("register_render_request", false, null, false)]
	void RegisterRenderRequest(UIntPtr pointer, ref ThumbnailRenderRequest request);

	[EngineMethod("get_number_of_pending_requests", false, null, false)]
	int GetNumberOfPendingRequests(UIntPtr pointer);

	[EngineMethod("is_memory_cleared", false, null, false)]
	bool IsMemoryCleared(UIntPtr pointer);
}
