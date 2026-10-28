using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public class SceneTableau
{
	private float _animationFrequencyThreshold = 2.5f;

	private MatrixFrame _frame;

	private Scene _tableauScene;

	private Camera _continuousRenderCamera;

	private GameEntity _cameraEntity;

	private float _cameraRatio;

	private bool _initialized;

	private int _tableauSizeX;

	private int _tableauSizeY;

	private SceneView View;

	private bool _isRotatingCharacter;

	private float _animationGap;

	private bool _isEnabled;

	private float RenderScale = 1f;

	public Texture _texture { get; private set; }

	public bool? IsReady => View?.ReadyToRender();

	public SceneTableau()
	{
		SetEnabled(enabled: true);
	}

	private void SetEnabled(bool enabled)
	{
		_isEnabled = enabled;
		View?.SetEnable(_isEnabled);
	}

	private void CreateTexture()
	{
		_texture = Texture.CreateRenderTarget("SceneTableau", _tableauSizeX, _tableauSizeY, autoMipmaps: true, isTableau: false);
		View = SceneView.CreateSceneView();
		View.SetScene(_tableauScene);
		View.SetRenderTarget(_texture);
		View.SetAutoDepthTargetCreation(value: true);
		View.SetSceneUsesSkybox(value: true);
		View.SetClearColor(4294902015u);
	}

	public void SetTargetSize(int width, int height)
	{
		_isRotatingCharacter = false;
		if (width <= 0 || height <= 0)
		{
			_tableauSizeX = 10;
			_tableauSizeY = 10;
		}
		else
		{
			RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
			_tableauSizeX = (int)((float)width * RenderScale);
			_tableauSizeY = (int)((float)height * RenderScale);
		}
		_cameraRatio = (float)_tableauSizeX / (float)_tableauSizeY;
		_ = View;
		View?.SetEnable(value: false);
		View?.AddClearTask(clearOnlySceneview: true);
		CreateTexture();
	}

	public void OnFinalize()
	{
		if (_continuousRenderCamera != null)
		{
			_continuousRenderCamera.ReleaseCameraEntity();
			_continuousRenderCamera = null;
			_cameraEntity = null;
		}
		View?.SetEnable(value: false);
		View?.AddClearTask();
		_texture?.Release();
		_texture = null;
		_tableauScene = null;
	}

	public void SetScene(object scene)
	{
		if (scene is Scene tableauScene)
		{
			_tableauScene = tableauScene;
			if (_tableauSizeX != 0 && _tableauSizeY != 0)
			{
				CreateTexture();
			}
		}
		else
		{
			Debug.FailedAssert("Given scene object is not Scene type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\SceneTableau.cs", "SetScene", 120);
		}
	}

	public void SetBannerCode(string value)
	{
		RefreshCharacterTableau();
	}

	private void RefreshCharacterTableau(Equipment oldEquipment = null)
	{
		if (!_initialized)
		{
			FirstTimeInit();
		}
	}

	public void RotateCharacter(bool value)
	{
		_isRotatingCharacter = value;
	}

	public void OnTick(float dt)
	{
		if (_animationFrequencyThreshold > _animationGap)
		{
			_animationGap += dt;
		}
		if (!(View != null))
		{
			return;
		}
		if (_continuousRenderCamera == null)
		{
			GameEntity gameEntity = _tableauScene.FindEntityWithTag("customcamera");
			if (gameEntity != null)
			{
				_continuousRenderCamera = Camera.CreateCamera();
				Vec3 dofParams = default(Vec3);
				gameEntity.GetCameraParamsFromCameraScript(_continuousRenderCamera, ref dofParams);
				_cameraEntity = gameEntity;
			}
		}
		PopupSceneContinuousRenderFunction();
	}

	private void FirstTimeInit()
	{
		_initialized = true;
	}

	private void PopupSceneContinuousRenderFunction()
	{
		GameEntity gameEntity = _tableauScene.FindEntityWithTag("customcamera");
		_tableauScene.SetShadow(shadowEnabled: true);
		_tableauScene.EnsurePostfxSystem();
		_tableauScene.SetMotionBlurMode(mode: true);
		_tableauScene.SetBloom(mode: true);
		_tableauScene.SetDynamicShadowmapCascadesRadiusMultiplier(1f);
		View.SetRenderWithPostfx(value: true);
		View.SetSceneUsesShadows(value: true);
		View.SetScene(_tableauScene);
		View.SetSceneUsesSkybox(value: true);
		View.SetClearColor(4278190080u);
		View.SetFocusedShadowmap(enable: false, ref _frame.origin, 1.55f);
		View.SetEnable(value: true);
		if (gameEntity != null)
		{
			Vec3 dofParams = default(Vec3);
			gameEntity.GetCameraParamsFromCameraScript(_continuousRenderCamera, ref dofParams);
			if (_continuousRenderCamera != null)
			{
				Camera continuousRenderCamera = _continuousRenderCamera;
				View.SetCamera(continuousRenderCamera);
			}
		}
	}
}
