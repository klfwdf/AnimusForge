using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class CameraDisplay : ScriptComponentBehavior
{
	private Camera _myCamera;

	private SceneView _sceneView;

	public int renderOrder;

	private void BuildView()
	{
		_sceneView = SceneView.CreateSceneView();
		_myCamera = Camera.CreateCamera();
		_sceneView.SetScene(base.GameEntity.Scene);
		_sceneView.SetPostfxFromConfig();
		_sceneView.SetRenderOption(View.ViewRenderOptions.ClearColor, value: false);
		_sceneView.SetRenderOption(View.ViewRenderOptions.ClearDepth, value: true);
		_sceneView.SetScale(new Vec2(0.2f, 0.2f));
	}

	private void SetCamera()
	{
		Vec2 realScreenResolution = Screen.RealScreenResolution;
		float aspectRatioXY = realScreenResolution.x / realScreenResolution.y;
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		_myCamera.SetFovVertical(System.MathF.PI / 4f, aspectRatioXY, 0.2f, 200f);
		_myCamera.Frame = globalFrame;
		_sceneView.SetCamera(_myCamera);
	}

	private void RenderCameraFrustrum()
	{
		_myCamera.RenderFrustrum();
	}

	protected internal override void OnEditorInit()
	{
		BuildView();
	}

	protected internal override void OnInit()
	{
		BuildView();
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (MBEditor.IsEntitySelected(base.GameEntity))
		{
			RenderCameraFrustrum();
			_sceneView.SetEnable(value: true);
		}
		else
		{
			_sceneView.SetEnable(value: false);
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		_sceneView = null;
		_myCamera = null;
	}
}
