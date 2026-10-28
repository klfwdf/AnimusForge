using System;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public class BrightnessDemoTableau
{
	private static int _tableauIndex;

	private MatrixFrame _frame;

	private Scene _tableauScene;

	private Texture _demoTexture;

	private Camera _continuousRenderCamera;

	private bool _initialized;

	private int _tableauSizeX;

	private int _tableauSizeY;

	private int _demoType = -1;

	private bool _isEnabled;

	private float RenderScale = 1f;

	public Texture Texture { get; private set; }

	private TableauView View
	{
		get
		{
			if (Texture != null)
			{
				return Texture.TableauView;
			}
			return null;
		}
	}

	private void SetEnabled(bool enabled)
	{
		_isEnabled = enabled;
		TableauView view = View;
		if (!_initialized)
		{
			SetScene();
		}
		view?.SetEnable(_isEnabled);
	}

	public void SetDemoType(int demoType)
	{
		_demoType = demoType;
		_initialized = false;
		RefreshDemoTableau();
	}

	public void SetTargetSize(int width, int height)
	{
		int num = 0;
		int num2 = 0;
		if (width <= 0 || height <= 0)
		{
			num = 10;
			num2 = 10;
		}
		else
		{
			RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
			num = (int)((float)width * RenderScale);
			num2 = (int)((float)height * RenderScale);
		}
		if (num != _tableauSizeX || num2 != _tableauSizeY)
		{
			_tableauSizeX = num;
			_tableauSizeY = num2;
			View?.SetEnable(value: false);
			View?.AddClearTask(clearOnlySceneview: true);
			Texture?.Release();
			Texture = TableauView.AddTableau($"BrightnessDemo_{_tableauIndex++}", SceneTableauContinuousRenderFunction, _tableauScene, _tableauSizeX, _tableauSizeY);
		}
	}

	public void OnFinalize()
	{
		if (_continuousRenderCamera != null)
		{
			_continuousRenderCamera.ReleaseCameraEntity();
			_continuousRenderCamera = null;
		}
		View?.SetEnable(value: false);
		View?.AddClearTask();
		Texture = null;
		_tableauScene?.ManualInvalidate();
		_tableauScene = null;
	}

	public void SetScene()
	{
		_tableauScene = Scene.CreateNewScene();
		switch (_demoType)
		{
		case 0:
			_demoTexture = Texture.GetFromResource("brightness_calibration_wide");
			_tableauScene.SetAtmosphereWithName("brightness_calibration_screen");
			break;
		case 1:
			_demoTexture = Texture.GetFromResource("calibration_image_1");
			_tableauScene.SetAtmosphereWithName("TOD_11_00_SemiCloudy");
			break;
		case 2:
			_demoTexture = Texture.GetFromResource("calibration_image_2");
			_tableauScene.SetAtmosphereWithName("TOD_05_00_SemiCloudy");
			break;
		case 3:
			_demoTexture = Texture.GetFromResource("calibration_image_3");
			_tableauScene.SetAtmosphereWithName("TOD_05_00_SemiCloudy");
			break;
		case 4:
			_demoTexture = Texture.GetFromResource("naval_calibration_0");
			_tableauScene.SetAtmosphereWithName("TOD_naval_10_00_SemiCloudy");
			break;
		case 5:
			_demoTexture = Texture.GetFromResource("naval_calibration_1");
			_tableauScene.SetAtmosphereWithName("TOD_naval_06_00_sunset");
			break;
		case 6:
			_demoTexture = Texture.GetFromResource("naval_calibration_2");
			_tableauScene.SetAtmosphereWithName("TOD_naval_08_00_rain_storm");
			break;
		default:
			Debug.FailedAssert($"Undefined Brightness demo type({_demoType})", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\BrightnessDemoTableau.cs", "SetScene", 145);
			break;
		}
		_tableauScene.SetDepthOfFieldParameters(0f, 0f, isVignetteOn: false);
	}

	private void RefreshDemoTableau()
	{
		if (!_initialized)
		{
			SetEnabled(enabled: true);
		}
	}

	public void OnTick(float dt)
	{
		if (_continuousRenderCamera == null)
		{
			_continuousRenderCamera = Camera.CreateCamera();
		}
		View?.SetDoNotRenderThisFrame(value: false);
	}

	internal void SceneTableauContinuousRenderFunction(Texture sender, EventArgs e)
	{
		Scene scene = (Scene)sender.UserData;
		TableauView tableauView = sender.TableauView;
		tableauView.SetEnable(value: true);
		if (scene == null)
		{
			tableauView.SetContinuousRendering(value: false);
			tableauView.SetDeleteAfterRendering(value: true);
			return;
		}
		scene.SetShadow(shadowEnabled: false);
		scene.EnsurePostfxSystem();
		scene.SetDofMode(mode: false);
		scene.SetMotionBlurMode(mode: false);
		scene.SetBloom(mode: true);
		scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.31f);
		scene.SetExternalInjectionTexture(_demoTexture);
		tableauView.SetRenderWithPostfx(value: true);
		tableauView.SetDoQuickExposure(value: true);
		if (_continuousRenderCamera != null)
		{
			Camera continuousRenderCamera = _continuousRenderCamera;
			tableauView.SetCamera(continuousRenderCamera);
			tableauView.SetScene(scene);
			tableauView.SetSceneUsesSkybox(value: false);
			tableauView.SetDeleteAfterRendering(value: false);
			tableauView.SetContinuousRendering(value: true);
			tableauView.SetDoNotRenderThisFrame(value: true);
			tableauView.SetClearColor(4278190080u);
			tableauView.SetFocusedShadowmap(enable: true, ref _frame.origin, 1.55f);
		}
	}
}
