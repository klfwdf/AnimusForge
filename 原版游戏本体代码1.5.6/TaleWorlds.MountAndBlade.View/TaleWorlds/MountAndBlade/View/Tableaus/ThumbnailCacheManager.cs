using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public class ThumbnailCacheManager
{
	private ThumbnailCreatorView _thumbnailCreatorView;

	private Scene _inventoryScene;

	private bool _inventorySceneBeingUsed;

	private MBAgentRendererSceneController _inventorySceneAgentRenderer;

	private Scene _mapConversationScene;

	private bool _mapConversationSceneBeingUsed;

	private MBAgentRendererSceneController _mapConversationSceneAgentRenderer;

	private List<IThumbnailCache> _thumbnailCaches;

	private Texture _heroSilhouetteTexture;

	public static ThumbnailCacheManager Current { get; private set; }

	public MatrixFrame InventorySceneCameraFrame { get; private set; }

	private void InitializeThumbnailCreator()
	{
		_thumbnailCreatorView = ThumbnailCreatorView.CreateThumbnailCreatorView();
		ThumbnailCreatorView.renderCallback = (ThumbnailCreatorView.OnThumbnailRenderCompleteDelegate)Delegate.Combine(ThumbnailCreatorView.renderCallback, new ThumbnailCreatorView.OnThumbnailRenderCompleteDelegate(OnThumbnailRenderComplete));
		Scene[] tableauCharacterScenes = BannerlordTableauManager.TableauCharacterScenes;
		foreach (Scene scene in tableauCharacterScenes)
		{
			_thumbnailCreatorView.RegisterScene(scene);
		}
		SceneInitializationData initData = new SceneInitializationData(initializeWithDefaults: true);
		initData.InitPhysicsWorld = false;
		initData.DoNotUseLoadingScreen = true;
		_inventoryScene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false, DecalAtlasGroup.Battle);
		_inventoryScene.Read("inventory_character_scene", ref initData);
		_inventoryScene.SetShadow(shadowEnabled: true);
		_inventoryScene.DisableStaticShadows(value: true);
		InventorySceneCameraFrame = _inventoryScene.FindEntityWithTag("camera_instance").GetGlobalFrame();
		_inventorySceneAgentRenderer = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_inventoryScene);
	}

	public bool IsCachedInventoryTableauSceneUsed()
	{
		return _inventorySceneBeingUsed;
	}

	public Scene GetCachedInventoryTableauScene()
	{
		_inventorySceneBeingUsed = true;
		return _inventoryScene;
	}

	public void ReturnCachedInventoryTableauScene()
	{
		_inventorySceneBeingUsed = false;
	}

	public bool IsCachedMapConversationTableauSceneUsed()
	{
		return _mapConversationSceneBeingUsed;
	}

	public Scene GetCachedMapConversationTableauScene()
	{
		_mapConversationSceneBeingUsed = true;
		return _mapConversationScene;
	}

	public void ReturnCachedMapConversationTableauScene()
	{
		_mapConversationSceneBeingUsed = false;
	}

	public static int GetNumberOfPendingRequests()
	{
		if (Current != null)
		{
			return Current._thumbnailCreatorView.GetNumberOfPendingRequests();
		}
		return 0;
	}

	public static bool IsNativeMemoryCleared()
	{
		if (Current != null)
		{
			return Current._thumbnailCreatorView.IsMemoryCleared();
		}
		return false;
	}

	public static void InitializeManager()
	{
		Current = new ThumbnailCacheManager();
		Current._thumbnailCaches = new List<IThumbnailCache>();
		Current.InitializeThumbnailCreator();
		Current._heroSilhouetteTexture = Texture.GetFromResource("hero_silhouette");
	}

	public void RegisterThumbnailCache(IThumbnailCache thumbnailCache)
	{
		if (_thumbnailCaches.Contains(thumbnailCache))
		{
			Debug.FailedAssert("Thumbnail cache already registered: " + thumbnailCache.GetType().Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\ThumbnailCacheManager.cs", "RegisterThumbnailCache", 139);
			return;
		}
		_thumbnailCaches.Add(thumbnailCache);
		thumbnailCache.Initialize(_thumbnailCreatorView);
	}

	public void UnregisterThumbnailCache(IThumbnailCache thumbnailCache)
	{
		if (!_thumbnailCaches.Contains(thumbnailCache))
		{
			Debug.FailedAssert("Trying to remove a thumbnail cache that is not registered: " + thumbnailCache.GetType().Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\ThumbnailCacheManager.cs", "UnregisterThumbnailCache", 152);
			return;
		}
		_thumbnailCaches.Remove(thumbnailCache);
		thumbnailCache.Destroy();
	}

	public static void InitializeSandboxValues()
	{
		SceneInitializationData initData = new SceneInitializationData(initializeWithDefaults: true);
		initData.InitPhysicsWorld = false;
		initData.InitSkyboxFromStart = false;
		Current._mapConversationScene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		Current._mapConversationScene.SetName("MapConversationTableau");
		Current._mapConversationScene.DisableStaticShadows(value: true);
		Current._mapConversationScene.Read("scn_conversation_tableau", ref initData);
		Current._mapConversationScene.SetShadow(shadowEnabled: true);
		Current._mapConversationSceneAgentRenderer = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(Current._mapConversationScene);
		Utilities.LoadVirtualTextureTileset("WorldMap");
	}

	public static void ReleaseSandboxValues()
	{
		MBAgentRendererSceneController.DestructAgentRendererSceneController(Current._mapConversationScene, Current._mapConversationSceneAgentRenderer, deleteThisFrame: false);
		Current._mapConversationSceneAgentRenderer = null;
		Current._mapConversationScene.ClearAll();
		Current._mapConversationScene.ManualInvalidate();
		Current._mapConversationScene = null;
	}

	public static void ClearManager()
	{
		Debug.Print("ThumbnailCacheManager::ClearManager");
		if (Current != null)
		{
			for (int i = 0; i < Current._thumbnailCaches.Count; i++)
			{
				Current._thumbnailCaches[i].Destroy();
			}
			Current._thumbnailCaches.Clear();
			Current._thumbnailCaches = null;
			MBAgentRendererSceneController.DestructAgentRendererSceneController(Current._inventoryScene, Current._inventorySceneAgentRenderer, deleteThisFrame: true);
			Current._inventoryScene?.ManualInvalidate();
			Current._inventoryScene = null;
			ThumbnailCreatorView.renderCallback = (ThumbnailCreatorView.OnThumbnailRenderCompleteDelegate)Delegate.Remove(ThumbnailCreatorView.renderCallback, new ThumbnailCreatorView.OnThumbnailRenderCompleteDelegate(Current.OnThumbnailRenderComplete));
			Current._thumbnailCreatorView.ClearRequests();
			Current._thumbnailCreatorView.ManualInvalidate();
			Current._thumbnailCreatorView = null;
			Current = null;
		}
	}

	private void OnThumbnailRenderComplete(string renderId, Texture renderTarget)
	{
		Texture texture = null;
		for (int i = 0; i < _thumbnailCaches.Count; i++)
		{
			IThumbnailCache thumbnailCache = _thumbnailCaches[i];
			if (thumbnailCache.GetValue(renderId, out texture) && texture == null)
			{
				thumbnailCache.Add(renderId, renderTarget);
			}
		}
		bool flag = false;
		for (int j = 0; j < _thumbnailCaches.Count; j++)
		{
			flag = flag || _thumbnailCaches[j].OnThumbnailRenderCompleted(renderId, renderTarget);
		}
	}

	public TextureCreationInfo CreateTexture(ThumbnailCreationData thumbnailCreationData)
	{
		TextureCreationInfo result = default(TextureCreationInfo);
		for (int i = 0; i < _thumbnailCaches.Count; i++)
		{
			TextureCreationInfo textureCreationInfo = _thumbnailCaches[i].CreateTexture(thumbnailCreationData);
			if (textureCreationInfo.IsValid)
			{
				if (result.IsValid && textureCreationInfo.IsValid)
				{
					Debug.FailedAssert("Creating thumbnails in more than one caches: " + thumbnailCreationData.RenderId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\ThumbnailCacheManager.cs", "CreateTexture", 253);
				}
				result = textureCreationInfo;
			}
		}
		return result;
	}

	public bool DestroyTexture(ThumbnailCreationData thumbnailCreationData)
	{
		bool result = false;
		for (int i = 0; i < _thumbnailCaches.Count; i++)
		{
			if (_thumbnailCaches[i].ReleaseTexture(thumbnailCreationData))
			{
				result = true;
			}
		}
		return result;
	}

	public void ForceClearAllCache(bool releaseImmediately)
	{
		for (int i = 0; i < _thumbnailCaches.Count; i++)
		{
			_thumbnailCaches[i].Clear(releaseImmediately);
		}
	}

	public Texture GetCachedHeroSilhouetteTexture()
	{
		return _heroSilhouetteTexture;
	}

	public void ClearUnusedCache()
	{
		for (int i = 0; i < _thumbnailCaches.Count; i++)
		{
			_thumbnailCaches[i].ClearUnusedCache();
		}
	}

	public void Tick(float dt)
	{
		for (int i = 0; i < _thumbnailCaches.Count; i++)
		{
			_thumbnailCaches[i].Tick(dt);
		}
	}
}
