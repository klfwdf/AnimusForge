using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglThumbnail_creator_view")]
public sealed class ThumbnailCreatorView : View
{
	public delegate void OnThumbnailRenderCompleteDelegate(string renderId, Texture renderTarget);

	public static OnThumbnailRenderCompleteDelegate renderCallback;

	internal ThumbnailCreatorView(UIntPtr pointer)
		: base(pointer)
	{
	}

	[EngineCallback(null, false)]
	internal static void OnThumbnailRenderComplete(string renderId, Texture renderTarget)
	{
		renderCallback(renderId, renderTarget);
	}

	public static ThumbnailCreatorView CreateThumbnailCreatorView()
	{
		return EngineApplicationInterface.IThumbnailCreatorView.CreateThumbnailCreatorView();
	}

	public void RegisterScene(Scene scene, bool usePostFx = true)
	{
		EngineApplicationInterface.IThumbnailCreatorView.RegisterScene(base.Pointer, scene.Pointer, usePostFx);
	}

	public void RegisterCachedEntity(Scene scene, GameEntity entity, string cacheId)
	{
		EngineApplicationInterface.IThumbnailCreatorView.RegisterCachedEntity(base.Pointer, scene.Pointer, entity.Pointer, cacheId);
	}

	public void UnregisterCachedEntity(string cacheId)
	{
		EngineApplicationInterface.IThumbnailCreatorView.UnregisterCachedEntity(base.Pointer, cacheId);
	}

	public void RegisterRenderRequest(ref ThumbnailRenderRequest request)
	{
		EngineApplicationInterface.IThumbnailCreatorView.RegisterRenderRequest(base.Pointer, ref request);
	}

	public void ClearRequests()
	{
		EngineApplicationInterface.IThumbnailCreatorView.ClearRequests(base.Pointer);
	}

	public void CancelRequest(string renderID)
	{
		EngineApplicationInterface.IThumbnailCreatorView.CancelRequest(base.Pointer, renderID);
	}

	public int GetNumberOfPendingRequests()
	{
		return EngineApplicationInterface.IThumbnailCreatorView.GetNumberOfPendingRequests(base.Pointer);
	}

	public bool IsMemoryCleared()
	{
		return EngineApplicationInterface.IThumbnailCreatorView.IsMemoryCleared(base.Pointer);
	}
}
