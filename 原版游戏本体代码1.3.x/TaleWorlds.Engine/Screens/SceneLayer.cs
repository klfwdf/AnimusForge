using System;
using System.Numerics;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine.Screens
{
	// Token: 0x020000A2 RID: 162
	public class SceneLayer : ScreenLayer
	{
		// Token: 0x170000BE RID: 190
		// (get) Token: 0x06000F08 RID: 3848 RVA: 0x00011960 File Offset: 0x0000FB60
		// (set) Token: 0x06000F09 RID: 3849 RVA: 0x00011968 File Offset: 0x0000FB68
		public bool ClearSceneOnFinalize { get; private set; }

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x06000F0A RID: 3850 RVA: 0x00011971 File Offset: 0x0000FB71
		// (set) Token: 0x06000F0B RID: 3851 RVA: 0x00011979 File Offset: 0x0000FB79
		public bool AutoToggleSceneView { get; private set; }

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x06000F0C RID: 3852 RVA: 0x00011982 File Offset: 0x0000FB82
		public SceneView SceneView
		{
			get
			{
				return this._sceneView;
			}
		}

		// Token: 0x06000F0D RID: 3853 RVA: 0x0001198A File Offset: 0x0000FB8A
		public SceneLayer(bool clearSceneOnFinalize = true, bool autoToggleSceneView = true)
			: base("SceneLayer", -100)
		{
			this.ClearSceneOnFinalize = clearSceneOnFinalize;
			base.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
			this._sceneView = SceneView.CreateSceneView();
			this.AutoToggleSceneView = autoToggleSceneView;
			base.IsFocusLayer = true;
		}

		// Token: 0x06000F0E RID: 3854 RVA: 0x000119C6 File Offset: 0x0000FBC6
		protected override void OnActivate()
		{
			base.OnActivate();
			if (this.AutoToggleSceneView)
			{
				this._sceneView.SetEnable(true);
			}
			ScreenManager.TrySetFocus(this);
		}

		// Token: 0x06000F0F RID: 3855 RVA: 0x000119E8 File Offset: 0x0000FBE8
		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			if (this.AutoToggleSceneView)
			{
				this._sceneView.SetEnable(false);
			}
		}

		// Token: 0x06000F10 RID: 3856 RVA: 0x00011A04 File Offset: 0x0000FC04
		protected override void OnFinalize()
		{
			if (this.ClearSceneOnFinalize)
			{
				this._sceneView.ClearAll(true, true);
			}
			base.OnFinalize();
		}

		// Token: 0x06000F11 RID: 3857 RVA: 0x00011A21 File Offset: 0x0000FC21
		public void SetScene(Scene scene)
		{
			this._sceneView.SetScene(scene);
		}

		// Token: 0x06000F12 RID: 3858 RVA: 0x00011A2F File Offset: 0x0000FC2F
		public void SetRenderWithPostfx(bool value)
		{
			this._sceneView.SetRenderWithPostfx(value);
		}

		// Token: 0x06000F13 RID: 3859 RVA: 0x00011A3D File Offset: 0x0000FC3D
		public void SetPostfxConfigParams(int value)
		{
			this._sceneView.SetPostfxConfigParams(value);
		}

		// Token: 0x06000F14 RID: 3860 RVA: 0x00011A4B File Offset: 0x0000FC4B
		public void SetCamera(Camera camera)
		{
			this._sceneView.SetCamera(camera);
		}

		// Token: 0x06000F15 RID: 3861 RVA: 0x00011A59 File Offset: 0x0000FC59
		public void SetPostfxFromConfig()
		{
			this._sceneView.SetPostfxFromConfig();
		}

		// Token: 0x06000F16 RID: 3862 RVA: 0x00011A66 File Offset: 0x0000FC66
		public Vec2 WorldPointToScreenPoint(Vec3 position)
		{
			return this._sceneView.WorldPointToScreenPoint(position);
		}

		// Token: 0x06000F17 RID: 3863 RVA: 0x00011A74 File Offset: 0x0000FC74
		public Vec2 ScreenPointToViewportPoint(Vec2 position)
		{
			return this._sceneView.ScreenPointToViewportPoint(position);
		}

		// Token: 0x06000F18 RID: 3864 RVA: 0x00011A82 File Offset: 0x0000FC82
		public bool ProjectedMousePositionOnGround(out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface)
		{
			return this._sceneView.ProjectedMousePositionOnGround(out groundPosition, out groundNormal, mouseVisible, excludeBodyOwnerFlags, checkOccludedSurface);
		}

		// Token: 0x06000F19 RID: 3865 RVA: 0x00011A96 File Offset: 0x0000FC96
		public void TranslateMouse(ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance = -1f)
		{
			this._sceneView.TranslateMouse(ref worldMouseNear, ref worldMouseFar, maxDistance);
		}

		// Token: 0x06000F1A RID: 3866 RVA: 0x00011AA6 File Offset: 0x0000FCA6
		public void SetSceneUsesSkybox(bool value)
		{
			this._sceneView.SetSceneUsesSkybox(value);
		}

		// Token: 0x06000F1B RID: 3867 RVA: 0x00011AB4 File Offset: 0x0000FCB4
		public void SetSceneUsesShadows(bool value)
		{
			this._sceneView.SetSceneUsesShadows(value);
		}

		// Token: 0x06000F1C RID: 3868 RVA: 0x00011AC2 File Offset: 0x0000FCC2
		public void SetSceneUsesContour(bool value)
		{
			this._sceneView.SetSceneUsesContour(value);
		}

		// Token: 0x06000F1D RID: 3869 RVA: 0x00011AD0 File Offset: 0x0000FCD0
		public void SetShadowmapResolutionMultiplier(float value)
		{
			this._sceneView.SetShadowmapResolutionMultiplier(value);
		}

		// Token: 0x06000F1E RID: 3870 RVA: 0x00011ADE File Offset: 0x0000FCDE
		public void SetFocusedShadowmap(bool enable, ref Vec3 center, float radius)
		{
			this._sceneView.SetFocusedShadowmap(enable, ref center, radius);
		}

		// Token: 0x06000F1F RID: 3871 RVA: 0x00011AEE File Offset: 0x0000FCEE
		public void DoNotClear(bool value)
		{
			this._sceneView.DoNotClear(value);
		}

		// Token: 0x06000F20 RID: 3872 RVA: 0x00011AFC File Offset: 0x0000FCFC
		public bool ReadyToRender()
		{
			return this._sceneView.ReadyToRender();
		}

		// Token: 0x06000F21 RID: 3873 RVA: 0x00011B09 File Offset: 0x0000FD09
		public void SetCleanScreenUntilLoadingDone(bool value)
		{
			this._sceneView.SetCleanScreenUntilLoadingDone(value);
		}

		// Token: 0x06000F22 RID: 3874 RVA: 0x00011B17 File Offset: 0x0000FD17
		public void ClearAll()
		{
			this._sceneView.ClearAll(true, true);
		}

		// Token: 0x06000F23 RID: 3875 RVA: 0x00011B26 File Offset: 0x0000FD26
		public void ClearRuntimeGPUMemory(bool remove_terrain)
		{
			this._sceneView.ClearAll(false, remove_terrain);
		}

		// Token: 0x06000F24 RID: 3876 RVA: 0x00011B35 File Offset: 0x0000FD35
		protected override void RefreshGlobalOrder(ref int currentOrder)
		{
			this._sceneView.SetRenderOrder(currentOrder);
			currentOrder++;
		}

		// Token: 0x06000F25 RID: 3877 RVA: 0x00011B4C File Offset: 0x0000FD4C
		public override bool HitTest(Vector2 position)
		{
			bool flag = position.X >= 0f && position.X < Screen.RealScreenResolutionWidth;
			bool flag2 = position.Y >= 0f && position.Y < Screen.RealScreenResolutionHeight;
			return flag && flag2;
		}

		// Token: 0x06000F26 RID: 3878 RVA: 0x00011B98 File Offset: 0x0000FD98
		public override bool HitTest()
		{
			Vector2 vector = (Vector2)base.Input.GetMousePositionPixel();
			bool flag = vector.X >= 0f && vector.X < Screen.RealScreenResolutionWidth;
			bool flag2 = vector.Y >= 0f && vector.Y < Screen.RealScreenResolutionHeight;
			return flag && flag2;
		}

		// Token: 0x06000F27 RID: 3879 RVA: 0x00011BF3 File Offset: 0x0000FDF3
		public override bool FocusTest()
		{
			return true;
		}

		// Token: 0x04000217 RID: 535
		private SceneView _sceneView;
	}
}
