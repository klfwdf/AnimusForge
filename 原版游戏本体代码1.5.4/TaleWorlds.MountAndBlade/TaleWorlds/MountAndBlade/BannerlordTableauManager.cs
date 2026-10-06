using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public static class BannerlordTableauManager
{
	public delegate void RequestCharacterTableauSetupDelegate(int characterCodeId, Scene scene, GameEntity poseEntity);

	private static Scene[] _tableauCharacterScenes = new Scene[5];

	private static bool _isTableauRenderSystemInitialized = false;

	public static RequestCharacterTableauSetupDelegate RequestCallback;

	public static Scene[] TableauCharacterScenes => _tableauCharacterScenes;

	public static void RequestCharacterTableauRender(int characterCodeId, string path, GameEntity poseEntity, Camera cameraObject, int tableauType)
	{
		MBAPI.IMBBannerlordTableauManager.RequestCharacterTableauRender(characterCodeId, path, poseEntity.Pointer, cameraObject.Pointer, tableauType);
	}

	public static void ClearManager()
	{
		_tableauCharacterScenes = null;
		RequestCallback = null;
		_isTableauRenderSystemInitialized = false;
	}

	public static void InitializeCharacterTableauRenderSystem()
	{
		if (!_isTableauRenderSystemInitialized)
		{
			MBAPI.IMBBannerlordTableauManager.InitializeCharacterTableauRenderSystem();
			_isTableauRenderSystemInitialized = true;
		}
	}

	public static int GetNumberOfPendingTableauRequests()
	{
		return MBAPI.IMBBannerlordTableauManager.GetNumberOfPendingTableauRequests();
	}

	[MBCallback(null, false)]
	internal static void RequestCharacterTableauSetup(int characterCodeId, Scene scene, GameEntity poseEntity)
	{
		RequestCallback(characterCodeId, scene, poseEntity);
	}

	[MBCallback(null, false)]
	internal static void RegisterCharacterTableauScene(Scene scene, int type)
	{
		TableauCharacterScenes[type] = scene;
	}
}
