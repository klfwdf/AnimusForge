using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglScene_view")]
public class SceneView : View
{
	internal SceneView(UIntPtr meshPointer)
		: base(meshPointer)
	{
	}

	public static SceneView CreateSceneView()
	{
		return EngineApplicationInterface.ISceneView.CreateSceneView();
	}

	public void SetScene(Scene scene)
	{
		EngineApplicationInterface.ISceneView.SetScene(base.Pointer, scene.Pointer);
	}

	public void SetAcceptGlobalDebugRenderObjects(bool value)
	{
		EngineApplicationInterface.ISceneView.SetAcceptGlobalDebugRenderObjects(base.Pointer, value);
	}

	public void SetRenderWithPostfx(bool value)
	{
		EngineApplicationInterface.ISceneView.SetRenderWithPostfx(base.Pointer, value);
	}

	public void SetPostfxConfigParams(int value)
	{
		EngineApplicationInterface.ISceneView.SetPostfxConfigParams(base.Pointer, value);
	}

	public void SetForceShaderCompilation(bool value)
	{
		EngineApplicationInterface.ISceneView.SetForceShaderCompilation(base.Pointer, value);
	}

	public bool CheckSceneReadyToRender()
	{
		return EngineApplicationInterface.ISceneView.CheckSceneReadyToRender(base.Pointer);
	}

	public void SetDoQuickExposure(bool value)
	{
		EngineApplicationInterface.ISceneView.SetDoQuickExposure(base.Pointer, value);
	}

	public void SetCamera(Camera camera)
	{
		EngineApplicationInterface.ISceneView.SetCamera(base.Pointer, camera.Pointer);
	}

	public void SetResolutionScaling(bool value)
	{
		EngineApplicationInterface.ISceneView.SetResolutionScaling(base.Pointer, value);
	}

	public void SetPostfxFromConfig()
	{
		EngineApplicationInterface.ISceneView.SetPostfxFromConfig(base.Pointer);
	}

	public Vec2 WorldPointToScreenPoint(Vec3 position)
	{
		return EngineApplicationInterface.ISceneView.WorldPointToScreenPoint(base.Pointer, position);
	}

	public Vec2 ScreenPointToViewportPoint(Vec2 position)
	{
		return EngineApplicationInterface.ISceneView.ScreenPointToViewportPoint(base.Pointer, position.x, position.y);
	}

	public bool ProjectedMousePositionOnGround(out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface)
	{
		return EngineApplicationInterface.ISceneView.ProjectedMousePositionOnGround(base.Pointer, out groundPosition, out groundNormal, mouseVisible, excludeBodyOwnerFlags, checkOccludedSurface);
	}

	public bool ProjectedMousePositionOnWater(out Vec3 waterPosition, bool mouseVisible)
	{
		return EngineApplicationInterface.ISceneView.ProjectedMousePositionOnWater(base.Pointer, out waterPosition, mouseVisible);
	}

	public void TranslateMouse(ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance = -1f)
	{
		EngineApplicationInterface.ISceneView.TranslateMouse(base.Pointer, ref worldMouseNear, ref worldMouseFar, maxDistance);
	}

	public void SetSceneUsesSkybox(bool value)
	{
		EngineApplicationInterface.ISceneView.SetSceneUsesSkybox(base.Pointer, value);
	}

	public void SetSceneUsesShadows(bool value)
	{
		EngineApplicationInterface.ISceneView.SetSceneUsesShadows(base.Pointer, value);
	}

	public void SetSceneUsesContour(bool value)
	{
		EngineApplicationInterface.ISceneView.SetSceneUsesContour(base.Pointer, value);
	}

	public void DoNotClear(bool value)
	{
		EngineApplicationInterface.ISceneView.DoNotClear(base.Pointer, value);
	}

	public void AddClearTask(bool clearOnlySceneview = false)
	{
		EngineApplicationInterface.ISceneView.AddClearTask(base.Pointer, clearOnlySceneview);
	}

	public bool ReadyToRender()
	{
		return EngineApplicationInterface.ISceneView.ReadyToRender(base.Pointer);
	}

	public void SetClearAndDisableAfterSucessfullRender(bool value)
	{
		EngineApplicationInterface.ISceneView.SetClearAndDisableAfterSucessfullRender(base.Pointer, value);
	}

	public void SetClearGbuffer(bool value)
	{
		EngineApplicationInterface.ISceneView.SetClearGbuffer(base.Pointer, value);
	}

	public void SetShadowmapResolutionMultiplier(float value)
	{
		EngineApplicationInterface.ISceneView.SetShadowmapResolutionMultiplier(base.Pointer, value);
	}

	public void SetPointlightResolutionMultiplier(float value)
	{
		EngineApplicationInterface.ISceneView.SetPointlightResolutionMultiplier(base.Pointer, value);
	}

	public void SetCleanScreenUntilLoadingDone(bool value)
	{
		EngineApplicationInterface.ISceneView.SetCleanScreenUntilLoadingDone(base.Pointer, value);
	}

	public void ClearAll(bool clearScene, bool removeTerrain)
	{
		EngineApplicationInterface.ISceneView.ClearAll(base.Pointer, clearScene, removeTerrain);
	}

	public void SetFocusedShadowmap(bool enable, ref Vec3 center, float radius)
	{
		EngineApplicationInterface.ISceneView.SetFocusedShadowmap(base.Pointer, enable, ref center, radius);
	}

	public Scene GetScene()
	{
		return EngineApplicationInterface.ISceneView.GetScene(base.Pointer);
	}

	public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		return EngineApplicationInterface.ISceneView.RayCastForClosestEntityOrTerrain(base.Pointer, ref sourcePoint, ref targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, excludeBodyFlags);
	}
}
