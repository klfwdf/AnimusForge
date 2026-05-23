using System.Collections.Generic;

namespace TaleWorlds.Engine;

internal class EngineApplicationInterface
{
	internal static IPath IPath;

	internal static IShader IShader;

	internal static ITexture ITexture;

	internal static IMaterial IMaterial;

	internal static IMetaMesh IMetaMesh;

	internal static IDecal IDecal;

	internal static IClothSimulatorComponent IClothSimulatorComponent;

	internal static ICompositeComponent ICompositeComponent;

	internal static IPhysicsShape IPhysicsShape;

	internal static IBodyPart IBodyPart;

	internal static IParticleSystem IParticleSystem;

	internal static IMesh IMesh;

	internal static IMeshBuilder IMeshBuilder;

	internal static ICamera ICamera;

	internal static ISkeleton ISkeleton;

	internal static IGameEntity IGameEntity;

	internal static IGameEntityComponent IGameEntityComponent;

	internal static IScene IScene;

	internal static IScriptComponent IScriptComponent;

	internal static ILight ILight;

	internal static IAsyncTask IAsyncTask;

	internal static IPhysicsMaterial IPhysicsMaterial;

	internal static ISceneView ISceneView;

	internal static IView IView;

	internal static ITableauView ITableauView;

	internal static ITextureView ITextureView;

	internal static IVideoPlayerView IVideoPlayerView;

	internal static IThumbnailCreatorView IThumbnailCreatorView;

	internal static IDebug IDebug;

	internal static ITwoDimensionView ITwoDimensionView;

	internal static IUtil IUtil;

	internal static IEngineSizeChecker IEngineSizeChecker;

	internal static IInput IInput;

	internal static ITime ITime;

	internal static IScreen IScreen;

	internal static IMusic IMusic;

	internal static IImgui IImgui;

	internal static IMouseManager IMouseManager;

	internal static IHighlights IHighlights;

	internal static ISoundEvent ISoundEvent;

	internal static ISoundManager ISoundManager;

	internal static IConfig IConfig;

	internal static IManagedMeshEditOperations IManagedMeshEditOperations;

	private static Dictionary<string, object> _objects;

	private static T GetObject<T>() where T : class
	{
		if (_objects.TryGetValue(typeof(T).FullName, out var value))
		{
			return value as T;
		}
		return null;
	}

	internal static void SetObjects(Dictionary<string, object> objects)
	{
		_objects = objects;
		IPath = GetObject<IPath>();
		IShader = GetObject<IShader>();
		ITexture = GetObject<ITexture>();
		IMaterial = GetObject<IMaterial>();
		IMetaMesh = GetObject<IMetaMesh>();
		IDecal = GetObject<IDecal>();
		IClothSimulatorComponent = GetObject<IClothSimulatorComponent>();
		ICompositeComponent = GetObject<ICompositeComponent>();
		IPhysicsShape = GetObject<IPhysicsShape>();
		IBodyPart = GetObject<IBodyPart>();
		IMesh = GetObject<IMesh>();
		IMeshBuilder = GetObject<IMeshBuilder>();
		ICamera = GetObject<ICamera>();
		ISkeleton = GetObject<ISkeleton>();
		IGameEntity = GetObject<IGameEntity>();
		IGameEntityComponent = GetObject<IGameEntityComponent>();
		IScene = GetObject<IScene>();
		IScriptComponent = GetObject<IScriptComponent>();
		ILight = GetObject<ILight>();
		IAsyncTask = GetObject<IAsyncTask>();
		IParticleSystem = GetObject<IParticleSystem>();
		IPhysicsMaterial = GetObject<IPhysicsMaterial>();
		ISceneView = GetObject<ISceneView>();
		IView = GetObject<IView>();
		ITableauView = GetObject<ITableauView>();
		ITextureView = GetObject<ITextureView>();
		IVideoPlayerView = GetObject<IVideoPlayerView>();
		IThumbnailCreatorView = GetObject<IThumbnailCreatorView>();
		IDebug = GetObject<IDebug>();
		ITwoDimensionView = GetObject<ITwoDimensionView>();
		IUtil = GetObject<IUtil>();
		IEngineSizeChecker = GetObject<IEngineSizeChecker>();
		IInput = GetObject<IInput>();
		ITime = GetObject<ITime>();
		IScreen = GetObject<IScreen>();
		IMusic = GetObject<IMusic>();
		IImgui = GetObject<IImgui>();
		IMouseManager = GetObject<IMouseManager>();
		IHighlights = GetObject<IHighlights>();
		ISoundEvent = GetObject<ISoundEvent>();
		ISoundManager = GetObject<ISoundManager>();
		IConfig = GetObject<IConfig>();
		IManagedMeshEditOperations = GetObject<IManagedMeshEditOperations>();
	}
}
