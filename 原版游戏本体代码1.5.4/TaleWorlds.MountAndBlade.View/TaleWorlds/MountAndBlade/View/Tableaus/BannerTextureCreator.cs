using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

internal static class BannerTextureCreator
{
	private static Scene _scene;

	private static Camera _bannerCamera;

	private static Camera _nineGridBannerCamera;

	private static ThumbnailCreatorView _thumbnailCreatorView;

	private static int _bannerTableauGPUAllocationIndex;

	internal static void Initialize(ThumbnailCreatorView thumbnailCreatorView)
	{
		_thumbnailCreatorView = thumbnailCreatorView;
		_scene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		_scene.DisableStaticShadows(value: true);
		_scene.SetName("ThumbnailCacheManager.BannerScene");
		_scene.SetDefaultLighting();
		_thumbnailCreatorView.RegisterScene(_scene, usePostFx: false);
		_bannerCamera = CreateDefaultBannerCamera();
		_nineGridBannerCamera = CreateNineGridBannerCamera();
		_bannerTableauGPUAllocationIndex = Utilities.RegisterGPUAllocationGroup("BannerTableauCache");
	}

	internal static void OnFinalize()
	{
		_scene?.ClearDecals();
		_scene?.ClearAll();
		_scene?.ManualInvalidate();
		_bannerCamera?.ReleaseCamera();
		_bannerCamera = null;
		_nineGridBannerCamera?.ReleaseCamera();
		_nineGridBannerCamera = null;
		_scene = null;
	}

	internal static Texture CreateTexture(BannerThumbnailCreationBaseData bannerCreationData)
	{
		bool isTableauOrNineGrid = bannerCreationData.IsTableauOrNineGrid;
		bool isLarge = bannerCreationData.IsLarge;
		Action<Texture> setAction = bannerCreationData.SetAction;
		string renderId = bannerCreationData.RenderId;
		BannerDebugInfo debugInfo = bannerCreationData.DebugInfo;
		Banner banner = bannerCreationData.Banner;
		bool flag = !(bannerCreationData is BannerTextureCreationData);
		int width = 512;
		int height = 512;
		Camera camera = _bannerCamera;
		if (isTableauOrNineGrid)
		{
			camera = _nineGridBannerCamera;
			if (isLarge)
			{
				width = 1024;
				height = 1024;
			}
		}
		MatrixFrame placementFrame = MatrixFrame.Identity;
		if (Game.Current == null)
		{
			banner.SetBannerVisual(((IBannerVisualCreator)new BannerVisualCreator()).CreateBannerVisual(banner));
		}
		string text = ThumbnailDebugUtility.CreateDebugIdFrom(renderId, "ban", debugInfo.CreateName());
		Texture texture = Texture.CreateRenderTarget(text, width, height, autoMipmaps: false, isTableau: false, createUninitialized: true, !flag);
		if (!flag)
		{
			setAction?.Invoke(texture);
		}
		if (!Banner.IsValidBannerCode(banner.BannerCode))
		{
			Debug.FailedAssert("Banner code is not valid: " + banner.BannerCode, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\BannerTextureCreator.cs", "CreateTexture", 93);
			return texture;
		}
		MetaMesh metaMesh = banner.ConvertToMultiMesh();
		GameEntity gameEntity = _scene.AddItemEntity(ref placementFrame, metaMesh);
		metaMesh.ManualInvalidate();
		gameEntity.SetVisibilityExcludeParents(visible: false);
		ThumbnailRenderRequest request = ThumbnailRenderRequest.CreateWithTexture(_scene, camera, texture, gameEntity, renderId, text, _bannerTableauGPUAllocationIndex);
		_thumbnailCreatorView.RegisterRenderRequest(ref request);
		return texture;
	}

	internal static Camera CreateDefaultBannerCamera()
	{
		return CreateCamera(1f / 3f, 2f / 3f, -2f / 3f, -1f / 3f, 0.001f, 510f);
	}

	internal static Camera CreateNineGridBannerCamera()
	{
		return CreateCamera(0f, 1f, -1f, 0f, 0.001f, 510f);
	}

	private static Camera CreateCamera(float left, float right, float bottom, float top, float near, float far)
	{
		Camera camera = Camera.CreateCamera();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin.z = 400f;
		camera.Frame = identity;
		camera.LookAt(new Vec3(0f, 0f, 400f), new Vec3(0f, 0f, 0f, -1f), new Vec3(0f, 1f));
		camera.SetViewVolume(perspective: false, left, right, bottom, top, near, far);
		return camera;
	}
}
