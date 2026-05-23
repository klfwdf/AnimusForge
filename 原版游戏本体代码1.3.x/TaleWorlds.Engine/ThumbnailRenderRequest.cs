using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000094 RID: 148
	[EngineStruct("rglThumbnail_render_request", false, null)]
	public struct ThumbnailRenderRequest
	{
		// Token: 0x06000D10 RID: 3344 RVA: 0x0000E860 File Offset: 0x0000CA60
		public static ThumbnailRenderRequest CreateWithTexture(Scene scene, Camera camera, Texture texture, GameEntity entity, string renderId, string debugName, int allocationGroupIndex)
		{
			return new ThumbnailRenderRequest
			{
				ScenePointer = scene.Pointer,
				CameraPointer = camera.Pointer,
				TexturePointer = texture.Pointer,
				EntityPointer = entity.Pointer,
				RenderId = renderId,
				DebugName = debugName,
				AllocationGroupIndex = allocationGroupIndex
			};
		}

		// Token: 0x06000D11 RID: 3345 RVA: 0x0000E8C8 File Offset: 0x0000CAC8
		public static ThumbnailRenderRequest CreateWithoutTexture(Scene scene, Camera camera, GameEntity entity, string renderId, int width, int height, string debugName, int allocationGroupIndex)
		{
			return new ThumbnailRenderRequest
			{
				ScenePointer = scene.Pointer,
				CameraPointer = camera.Pointer,
				EntityPointer = entity.Pointer,
				RenderId = renderId,
				Width = width,
				Height = height,
				DebugName = debugName,
				AllocationGroupIndex = allocationGroupIndex
			};
		}

		// Token: 0x06000D12 RID: 3346 RVA: 0x0000E934 File Offset: 0x0000CB34
		public static ThumbnailRenderRequest CreateForCachedEntity(Scene scene, Camera camera, Texture texture, string cachedEntityId, string renderId, string debugName, int allocationGroupIndex)
		{
			return new ThumbnailRenderRequest
			{
				ScenePointer = scene.Pointer,
				CameraPointer = camera.Pointer,
				TexturePointer = texture.Pointer,
				CachedEntityId = cachedEntityId,
				RenderId = renderId,
				DebugName = debugName,
				AllocationGroupIndex = allocationGroupIndex
			};
		}

		// Token: 0x06000D13 RID: 3347 RVA: 0x0000E994 File Offset: 0x0000CB94
		public static ThumbnailRenderRequest CreateForCachedEntityWithoutTexture(Scene scene, Camera camera, string cachedEntityId, string renderId, int width, int height, string debugName, int allocationGroupIndex)
		{
			return new ThumbnailRenderRequest
			{
				ScenePointer = scene.Pointer,
				CameraPointer = camera.Pointer,
				CachedEntityId = cachedEntityId,
				RenderId = renderId,
				Width = width,
				Height = height,
				DebugName = debugName,
				AllocationGroupIndex = allocationGroupIndex
			};
		}

		// Token: 0x040001CB RID: 459
		public UIntPtr ScenePointer;

		// Token: 0x040001CC RID: 460
		public UIntPtr CameraPointer;

		// Token: 0x040001CD RID: 461
		public UIntPtr TexturePointer;

		// Token: 0x040001CE RID: 462
		public string CachedEntityId;

		// Token: 0x040001CF RID: 463
		public UIntPtr EntityPointer;

		// Token: 0x040001D0 RID: 464
		public int Width;

		// Token: 0x040001D1 RID: 465
		public int Height;

		// Token: 0x040001D2 RID: 466
		public string RenderId;

		// Token: 0x040001D3 RID: 467
		public string DebugName;

		// Token: 0x040001D4 RID: 468
		public int AllocationGroupIndex;
	}
}
