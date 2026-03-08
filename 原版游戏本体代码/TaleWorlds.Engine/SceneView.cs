using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000084 RID: 132
	[EngineClass("rglScene_view")]
	public class SceneView : View
	{
		// Token: 0x06000BE4 RID: 3044 RVA: 0x0000D041 File Offset: 0x0000B241
		internal SceneView(UIntPtr meshPointer)
			: base(meshPointer)
		{
		}

		// Token: 0x06000BE5 RID: 3045 RVA: 0x0000D04A File Offset: 0x0000B24A
		public static SceneView CreateSceneView()
		{
			return EngineApplicationInterface.ISceneView.CreateSceneView();
		}

		// Token: 0x06000BE6 RID: 3046 RVA: 0x0000D056 File Offset: 0x0000B256
		public void SetScene(Scene scene)
		{
			EngineApplicationInterface.ISceneView.SetScene(base.Pointer, scene.Pointer);
		}

		// Token: 0x06000BE7 RID: 3047 RVA: 0x0000D06E File Offset: 0x0000B26E
		public void SetAcceptGlobalDebugRenderObjects(bool value)
		{
			EngineApplicationInterface.ISceneView.SetAcceptGlobalDebugRenderObjects(base.Pointer, value);
		}

		// Token: 0x06000BE8 RID: 3048 RVA: 0x0000D081 File Offset: 0x0000B281
		public void SetRenderWithPostfx(bool value)
		{
			EngineApplicationInterface.ISceneView.SetRenderWithPostfx(base.Pointer, value);
		}

		// Token: 0x06000BE9 RID: 3049 RVA: 0x0000D094 File Offset: 0x0000B294
		public void SetPostfxConfigParams(int value)
		{
			EngineApplicationInterface.ISceneView.SetPostfxConfigParams(base.Pointer, value);
		}

		// Token: 0x06000BEA RID: 3050 RVA: 0x0000D0A7 File Offset: 0x0000B2A7
		public void SetForceShaderCompilation(bool value)
		{
			EngineApplicationInterface.ISceneView.SetForceShaderCompilation(base.Pointer, value);
		}

		// Token: 0x06000BEB RID: 3051 RVA: 0x0000D0BA File Offset: 0x0000B2BA
		public bool CheckSceneReadyToRender()
		{
			return EngineApplicationInterface.ISceneView.CheckSceneReadyToRender(base.Pointer);
		}

		// Token: 0x06000BEC RID: 3052 RVA: 0x0000D0CC File Offset: 0x0000B2CC
		public void SetDoQuickExposure(bool value)
		{
			EngineApplicationInterface.ISceneView.SetDoQuickExposure(base.Pointer, value);
		}

		// Token: 0x06000BED RID: 3053 RVA: 0x0000D0DF File Offset: 0x0000B2DF
		public void SetCamera(Camera camera)
		{
			EngineApplicationInterface.ISceneView.SetCamera(base.Pointer, camera.Pointer);
		}

		// Token: 0x06000BEE RID: 3054 RVA: 0x0000D0F7 File Offset: 0x0000B2F7
		public void SetResolutionScaling(bool value)
		{
			EngineApplicationInterface.ISceneView.SetResolutionScaling(base.Pointer, value);
		}

		// Token: 0x06000BEF RID: 3055 RVA: 0x0000D10A File Offset: 0x0000B30A
		public void SetPostfxFromConfig()
		{
			EngineApplicationInterface.ISceneView.SetPostfxFromConfig(base.Pointer);
		}

		// Token: 0x06000BF0 RID: 3056 RVA: 0x0000D11C File Offset: 0x0000B31C
		public Vec2 WorldPointToScreenPoint(Vec3 position)
		{
			return EngineApplicationInterface.ISceneView.WorldPointToScreenPoint(base.Pointer, position);
		}

		// Token: 0x06000BF1 RID: 3057 RVA: 0x0000D12F File Offset: 0x0000B32F
		public Vec2 ScreenPointToViewportPoint(Vec2 position)
		{
			return EngineApplicationInterface.ISceneView.ScreenPointToViewportPoint(base.Pointer, position.x, position.y);
		}

		// Token: 0x06000BF2 RID: 3058 RVA: 0x0000D14D File Offset: 0x0000B34D
		public bool ProjectedMousePositionOnGround(out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface)
		{
			return EngineApplicationInterface.ISceneView.ProjectedMousePositionOnGround(base.Pointer, out groundPosition, out groundNormal, mouseVisible, excludeBodyOwnerFlags, checkOccludedSurface);
		}

		// Token: 0x06000BF3 RID: 3059 RVA: 0x0000D166 File Offset: 0x0000B366
		public bool ProjectedMousePositionOnWater(out Vec3 waterPosition, bool mouseVisible)
		{
			return EngineApplicationInterface.ISceneView.ProjectedMousePositionOnWater(base.Pointer, out waterPosition, mouseVisible);
		}

		// Token: 0x06000BF4 RID: 3060 RVA: 0x0000D17A File Offset: 0x0000B37A
		public void TranslateMouse(ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance = -1f)
		{
			EngineApplicationInterface.ISceneView.TranslateMouse(base.Pointer, ref worldMouseNear, ref worldMouseFar, maxDistance);
		}

		// Token: 0x06000BF5 RID: 3061 RVA: 0x0000D18F File Offset: 0x0000B38F
		public void SetSceneUsesSkybox(bool value)
		{
			EngineApplicationInterface.ISceneView.SetSceneUsesSkybox(base.Pointer, value);
		}

		// Token: 0x06000BF6 RID: 3062 RVA: 0x0000D1A2 File Offset: 0x0000B3A2
		public void SetSceneUsesShadows(bool value)
		{
			EngineApplicationInterface.ISceneView.SetSceneUsesShadows(base.Pointer, value);
		}

		// Token: 0x06000BF7 RID: 3063 RVA: 0x0000D1B5 File Offset: 0x0000B3B5
		public void SetSceneUsesContour(bool value)
		{
			EngineApplicationInterface.ISceneView.SetSceneUsesContour(base.Pointer, value);
		}

		// Token: 0x06000BF8 RID: 3064 RVA: 0x0000D1C8 File Offset: 0x0000B3C8
		public void DoNotClear(bool value)
		{
			EngineApplicationInterface.ISceneView.DoNotClear(base.Pointer, value);
		}

		// Token: 0x06000BF9 RID: 3065 RVA: 0x0000D1DB File Offset: 0x0000B3DB
		public void AddClearTask(bool clearOnlySceneview = false)
		{
			EngineApplicationInterface.ISceneView.AddClearTask(base.Pointer, clearOnlySceneview);
		}

		// Token: 0x06000BFA RID: 3066 RVA: 0x0000D1EE File Offset: 0x0000B3EE
		public bool ReadyToRender()
		{
			return EngineApplicationInterface.ISceneView.ReadyToRender(base.Pointer);
		}

		// Token: 0x06000BFB RID: 3067 RVA: 0x0000D200 File Offset: 0x0000B400
		public void SetClearAndDisableAfterSucessfullRender(bool value)
		{
			EngineApplicationInterface.ISceneView.SetClearAndDisableAfterSucessfullRender(base.Pointer, value);
		}

		// Token: 0x06000BFC RID: 3068 RVA: 0x0000D213 File Offset: 0x0000B413
		public void SetClearGbuffer(bool value)
		{
			EngineApplicationInterface.ISceneView.SetClearGbuffer(base.Pointer, value);
		}

		// Token: 0x06000BFD RID: 3069 RVA: 0x0000D226 File Offset: 0x0000B426
		public void SetShadowmapResolutionMultiplier(float value)
		{
			EngineApplicationInterface.ISceneView.SetShadowmapResolutionMultiplier(base.Pointer, value);
		}

		// Token: 0x06000BFE RID: 3070 RVA: 0x0000D239 File Offset: 0x0000B439
		public void SetPointlightResolutionMultiplier(float value)
		{
			EngineApplicationInterface.ISceneView.SetPointlightResolutionMultiplier(base.Pointer, value);
		}

		// Token: 0x06000BFF RID: 3071 RVA: 0x0000D24C File Offset: 0x0000B44C
		public void SetCleanScreenUntilLoadingDone(bool value)
		{
			EngineApplicationInterface.ISceneView.SetCleanScreenUntilLoadingDone(base.Pointer, value);
		}

		// Token: 0x06000C00 RID: 3072 RVA: 0x0000D25F File Offset: 0x0000B45F
		public void ClearAll(bool clearScene, bool removeTerrain)
		{
			EngineApplicationInterface.ISceneView.ClearAll(base.Pointer, clearScene, removeTerrain);
		}

		// Token: 0x06000C01 RID: 3073 RVA: 0x0000D273 File Offset: 0x0000B473
		public void SetFocusedShadowmap(bool enable, ref Vec3 center, float radius)
		{
			EngineApplicationInterface.ISceneView.SetFocusedShadowmap(base.Pointer, enable, ref center, radius);
		}

		// Token: 0x06000C02 RID: 3074 RVA: 0x0000D288 File Offset: 0x0000B488
		public Scene GetScene()
		{
			return EngineApplicationInterface.ISceneView.GetScene(base.Pointer);
		}

		// Token: 0x06000C03 RID: 3075 RVA: 0x0000D29C File Offset: 0x0000B49C
		public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			return EngineApplicationInterface.ISceneView.RayCastForClosestEntityOrTerrain(base.Pointer, ref sourcePoint, ref targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref zero, excludeBodyFlags);
		}
	}
}
