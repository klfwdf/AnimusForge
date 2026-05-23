using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000093 RID: 147
	[EngineClass("rglThumbnail_creator_view")]
	public sealed class ThumbnailCreatorView : View
	{
		// Token: 0x06000D05 RID: 3333 RVA: 0x0000E794 File Offset: 0x0000C994
		internal ThumbnailCreatorView(UIntPtr pointer)
			: base(pointer)
		{
		}

		// Token: 0x06000D06 RID: 3334 RVA: 0x0000E79D File Offset: 0x0000C99D
		[EngineCallback(null, false)]
		internal static void OnThumbnailRenderComplete(string renderId, Texture renderTarget)
		{
			ThumbnailCreatorView.renderCallback(renderId, renderTarget);
		}

		// Token: 0x06000D07 RID: 3335 RVA: 0x0000E7AB File Offset: 0x0000C9AB
		public static ThumbnailCreatorView CreateThumbnailCreatorView()
		{
			return EngineApplicationInterface.IThumbnailCreatorView.CreateThumbnailCreatorView();
		}

		// Token: 0x06000D08 RID: 3336 RVA: 0x0000E7B7 File Offset: 0x0000C9B7
		public void RegisterScene(Scene scene, bool usePostFx = true)
		{
			EngineApplicationInterface.IThumbnailCreatorView.RegisterScene(base.Pointer, scene.Pointer, usePostFx);
		}

		// Token: 0x06000D09 RID: 3337 RVA: 0x0000E7D0 File Offset: 0x0000C9D0
		public void RegisterCachedEntity(Scene scene, GameEntity entity, string cacheId)
		{
			EngineApplicationInterface.IThumbnailCreatorView.RegisterCachedEntity(base.Pointer, scene.Pointer, entity.Pointer, cacheId);
		}

		// Token: 0x06000D0A RID: 3338 RVA: 0x0000E7EF File Offset: 0x0000C9EF
		public void UnregisterCachedEntity(string cacheId)
		{
			EngineApplicationInterface.IThumbnailCreatorView.UnregisterCachedEntity(base.Pointer, cacheId);
		}

		// Token: 0x06000D0B RID: 3339 RVA: 0x0000E802 File Offset: 0x0000CA02
		public void RegisterRenderRequest(ref ThumbnailRenderRequest request)
		{
			EngineApplicationInterface.IThumbnailCreatorView.RegisterRenderRequest(base.Pointer, ref request);
		}

		// Token: 0x06000D0C RID: 3340 RVA: 0x0000E815 File Offset: 0x0000CA15
		public void ClearRequests()
		{
			EngineApplicationInterface.IThumbnailCreatorView.ClearRequests(base.Pointer);
		}

		// Token: 0x06000D0D RID: 3341 RVA: 0x0000E827 File Offset: 0x0000CA27
		public void CancelRequest(string renderID)
		{
			EngineApplicationInterface.IThumbnailCreatorView.CancelRequest(base.Pointer, renderID);
		}

		// Token: 0x06000D0E RID: 3342 RVA: 0x0000E83A File Offset: 0x0000CA3A
		public int GetNumberOfPendingRequests()
		{
			return EngineApplicationInterface.IThumbnailCreatorView.GetNumberOfPendingRequests(base.Pointer);
		}

		// Token: 0x06000D0F RID: 3343 RVA: 0x0000E84C File Offset: 0x0000CA4C
		public bool IsMemoryCleared()
		{
			return EngineApplicationInterface.IThumbnailCreatorView.IsMemoryCleared(base.Pointer);
		}

		// Token: 0x040001CA RID: 458
		public static ThumbnailCreatorView.OnThumbnailRenderCompleteDelegate renderCallback;

		// Token: 0x020000D2 RID: 210
		// (Invoke) Token: 0x06001003 RID: 4099
		public delegate void OnThumbnailRenderCompleteDelegate(string renderId, Texture renderTarget);
	}
}
