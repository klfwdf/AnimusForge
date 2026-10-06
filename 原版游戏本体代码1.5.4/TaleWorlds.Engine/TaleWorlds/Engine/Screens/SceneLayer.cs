using System.Numerics;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine.Screens;

public class SceneLayer : ScreenLayer
{
	private SceneView _sceneView;

	public bool ClearSceneOnFinalize { get; private set; }

	public bool AutoToggleSceneView { get; private set; }

	public SceneView SceneView => _sceneView;

	public SceneLayer(bool clearSceneOnFinalize = true, bool autoToggleSceneView = true)
		: base("SceneLayer", -100)
	{
		ClearSceneOnFinalize = clearSceneOnFinalize;
		base.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		_sceneView = SceneView.CreateSceneView();
		AutoToggleSceneView = autoToggleSceneView;
		base.IsFocusLayer = true;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (AutoToggleSceneView)
		{
			_sceneView.SetEnable(value: true);
		}
		ScreenManager.TrySetFocus(this);
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		if (AutoToggleSceneView)
		{
			_sceneView.SetEnable(value: false);
		}
	}

	protected override void OnFinalize()
	{
		if (ClearSceneOnFinalize)
		{
			_sceneView.ClearAll(clearScene: true, removeTerrain: true);
		}
		base.OnFinalize();
	}

	public void SetScene(Scene scene)
	{
		_sceneView.SetScene(scene);
	}

	public void SetRenderWithPostfx(bool value)
	{
		_sceneView.SetRenderWithPostfx(value);
	}

	public void SetPostfxConfigParams(int value)
	{
		_sceneView.SetPostfxConfigParams(value);
	}

	public void SetCamera(Camera camera)
	{
		_sceneView.SetCamera(camera);
	}

	public void SetPostfxFromConfig()
	{
		_sceneView.SetPostfxFromConfig();
	}

	public Vec2 WorldPointToScreenPoint(Vec3 position)
	{
		return _sceneView.WorldPointToScreenPoint(position);
	}

	public Vec2 ScreenPointToViewportPoint(Vec2 position)
	{
		return _sceneView.ScreenPointToViewportPoint(position);
	}

	public bool ProjectedMousePositionOnGround(out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface)
	{
		return _sceneView.ProjectedMousePositionOnGround(out groundPosition, out groundNormal, mouseVisible, excludeBodyOwnerFlags, checkOccludedSurface);
	}

	public void TranslateMouse(ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance = -1f)
	{
		_sceneView.TranslateMouse(ref worldMouseNear, ref worldMouseFar, maxDistance);
	}

	public void SetSceneUsesSkybox(bool value)
	{
		_sceneView.SetSceneUsesSkybox(value);
	}

	public void SetSceneUsesShadows(bool value)
	{
		_sceneView.SetSceneUsesShadows(value);
	}

	public void SetSceneUsesContour(bool value)
	{
		_sceneView.SetSceneUsesContour(value);
	}

	public void SetShadowmapResolutionMultiplier(float value)
	{
		_sceneView.SetShadowmapResolutionMultiplier(value);
	}

	public void SetFocusedShadowmap(bool enable, ref Vec3 center, float radius)
	{
		_sceneView.SetFocusedShadowmap(enable, ref center, radius);
	}

	public void DoNotClear(bool value)
	{
		_sceneView.DoNotClear(value);
	}

	public bool ReadyToRender()
	{
		return _sceneView.ReadyToRender();
	}

	public void SetCleanScreenUntilLoadingDone(bool value)
	{
		_sceneView.SetCleanScreenUntilLoadingDone(value);
	}

	public void ClearAll()
	{
		_sceneView.ClearAll(clearScene: true, removeTerrain: true);
	}

	public void ClearRuntimeGPUMemory(bool remove_terrain)
	{
		_sceneView.ClearAll(clearScene: false, remove_terrain);
	}

	protected override void RefreshGlobalOrder(ref int currentOrder)
	{
		_sceneView.SetRenderOrder(currentOrder);
		currentOrder++;
	}

	public override bool HitTest(Vector2 position)
	{
		bool num = position.X >= 0f && position.X < Screen.RealScreenResolutionWidth;
		bool flag = position.Y >= 0f && position.Y < Screen.RealScreenResolutionHeight;
		return num && flag;
	}

	public override bool HitTest()
	{
		Vector2 vector = (Vector2)base.Input.GetMousePositionPixel();
		bool num = vector.X >= 0f && vector.X < Screen.RealScreenResolutionWidth;
		bool flag = vector.Y >= 0f && vector.Y < Screen.RealScreenResolutionHeight;
		return num && flag;
	}

	public override bool FocusTest()
	{
		return true;
	}
}
