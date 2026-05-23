using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map
{
	// Token: 0x0200004B RID: 75
	public class MapCursor
	{
		// Token: 0x06000287 RID: 647 RVA: 0x000177B0 File Offset: 0x000159B0
		public void Initialize(MapScreen parentMapScreen)
		{
			this._targetCircleRotationStartTime = 0f;
			this._smallAtlasTextureIndex = 0;
			this._mapScreen = parentMapScreen;
			Scene scene = (Campaign.Current.MapSceneWrapper as MapScene).Scene;
			this._gameCursorValidDecalMaterial = Material.GetFromResource("map_cursor_valid_decal");
			this._gameCursorInvalidDecalMaterial = Material.GetFromResource("map_cursor_invalid_decal");
			this._mapCursorDecalEntity = GameEntity.CreateEmpty(scene, true, true, true);
			this._mapCursorDecalEntity.Name = "tCursor";
			this._mapCursorDecal = Decal.CreateDecal(null);
			this._mapCursorDecal.SetMaterial(this._gameCursorValidDecalMaterial);
			this._mapCursorDecalEntity.AddComponent(this._mapCursorDecal);
			scene.AddDecalInstance(this._mapCursorDecal, "editor_set", true);
			MatrixFrame frame = this._mapCursorDecalEntity.GetFrame();
			Vec3 vec = new Vec3(0.38f, 0.38f, 0.38f, -1f);
			frame.Scale(vec);
			this._mapCursorDecal.SetFrame(frame);
		}

		// Token: 0x06000288 RID: 648 RVA: 0x000178A8 File Offset: 0x00015AA8
		public void BeforeTick(float dt)
		{
			SceneLayer sceneLayer = this._mapScreen.SceneLayer;
			Camera camera = this._mapScreen.MapCameraView.Camera;
			float cameraDistance = this._mapScreen.MapCameraView.CameraDistance;
			Vec3 zero = Vec3.Zero;
			Vec3 zero2 = Vec3.Zero;
			Vec2 viewportPoint = sceneLayer.SceneView.ScreenPointToViewportPoint(new Vec2(0.5f, 0.5f));
			camera.ViewportPointToWorldRay(ref zero, ref zero2, viewportPoint);
			PathFaceRecord pathFaceRecord = default(PathFaceRecord);
			float num;
			Vec3 origin;
			bool isOnLand;
			this._mapScreen.GetCursorIntersectionPoint(ref zero, ref zero2, out num, out origin, ref pathFaceRecord, out isOnLand, BodyFlags.CommonFocusRayCastExcludeFlags);
			Vec3 vec;
			sceneLayer.SceneView.ProjectedMousePositionOnGround(out origin, out vec, false, BodyFlags.None, false);
			if (this._mapCursorDecalEntity != null)
			{
				this._smallAtlasTextureIndex = this.GetCircleIndex();
				if (this._navigatablePositionCheckTimer > 0.2f)
				{
					bool flag = false;
					if (!this._anotherEntityHiglighted)
					{
						MobileParty.NavigationType navigationType;
						flag = NavigationHelper.CanPlayerNavigateToPosition(new CampaignVec2(origin.AsVec2, isOnLand), out navigationType);
					}
					this._mapCursorDecal.SetMaterial((flag || this._anotherEntityHiglighted) ? this._gameCursorValidDecalMaterial : this._gameCursorInvalidDecalMaterial);
					this._mapCursorDecal.SetVectorArgument(0.166f, 1f, 0.166f * (float)this._smallAtlasTextureIndex, 0f);
					this.SetAlpha(this._anotherEntityHiglighted ? 0.2f : 1f);
				}
				MatrixFrame frame = this._mapCursorDecalEntity.GetFrame();
				frame.origin = origin;
				bool flag2 = !this._smoothRotationNormalStart.IsNonZero;
				Vec3 vec2 = ((cameraDistance > 160f) ? Vec3.Up : vec);
				if (!this._smoothRotationNormalEnd.NearlyEquals(vec2, 1E-05f))
				{
					this._smoothRotationNormalStart = (flag2 ? vec2 : this._smoothRotationNormalCurrent);
					this._smoothRotationNormalEnd = vec2;
					this._smoothRotationNormalStart.Normalize();
					this._smoothRotationNormalEnd.Normalize();
					this._smoothRotationAlpha = 0f;
				}
				this._smoothRotationNormalCurrent = Vec3.Lerp(this._smoothRotationNormalStart, this._smoothRotationNormalEnd, this._smoothRotationAlpha);
				this._smoothRotationAlpha += 12f * dt;
				this._smoothRotationAlpha = MathF.Clamp(this._smoothRotationAlpha, 0f, 1f);
				this._smoothRotationNormalCurrent.Normalize();
				frame.rotation.f = camera.Frame.rotation.f;
				frame.rotation.f.z = 0f;
				frame.rotation.f.Normalize();
				frame.rotation.u = this._smoothRotationNormalCurrent;
				frame.rotation.u.Normalize();
				frame.rotation.s = Vec3.CrossProduct(frame.rotation.u, frame.rotation.f);
				float num2 = (cameraDistance + 80f) * (cameraDistance + 80f) / 10000f;
				num2 = MathF.Clamp(num2, 0.2f, 38f);
				Vec3 vec3 = Vec3.One * num2;
				frame.Scale(vec3);
				this._mapCursorDecalEntity.SetGlobalFrame(frame, true);
				this._anotherEntityHiglighted = false;
			}
			if (this._navigatablePositionCheckTimer > 0.2f)
			{
				this._navigatablePositionCheckTimer = 0f;
			}
			this._navigatablePositionCheckTimer += dt;
		}

		// Token: 0x06000289 RID: 649 RVA: 0x00017BF4 File Offset: 0x00015DF4
		public void SetVisible(bool value)
		{
			if (value)
			{
				if (this._gameCursorActive && !this._mapScreen.GetMouseVisible())
				{
					return;
				}
				this._mapScreen.SetMouseVisible(false);
				this._mapCursorDecalEntity.SetVisibilityExcludeParents(true);
				if (this._mapScreen.CurrentVisualOfTooltip != null)
				{
					this._mapScreen.RemoveMapTooltip();
				}
				Vec2 resolution = Input.Resolution;
				Input.SetMousePosition((int)(resolution.X / 2f), (int)(resolution.Y / 2f));
				this._gameCursorActive = true;
				return;
			}
			else
			{
				bool flag = !(GameStateManager.Current.ActiveState is MapState) || (!this._mapScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) && !this._mapScreen.SceneLayer.Input.IsKeyDown(InputKey.MiddleMouseButton));
				if (!this._gameCursorActive && this._mapScreen.GetMouseVisible() == flag)
				{
					return;
				}
				this._mapScreen.SetMouseVisible(flag);
				this._mapCursorDecalEntity.SetVisibilityExcludeParents(false);
				this._gameCursorActive = false;
				return;
			}
		}

		// Token: 0x0600028A RID: 650 RVA: 0x00017D03 File Offset: 0x00015F03
		protected internal void OnMapTerrainClick()
		{
			this._targetCircleRotationStartTime = MBCommon.GetApplicationTime();
		}

		// Token: 0x0600028B RID: 651 RVA: 0x00017D10 File Offset: 0x00015F10
		protected internal void OnAnotherEntityHighlighted()
		{
			this._anotherEntityHiglighted = true;
		}

		// Token: 0x0600028C RID: 652 RVA: 0x00017D1C File Offset: 0x00015F1C
		protected internal void SetAlpha(float alpha)
		{
			Color color = Color.FromUint(this._mapCursorDecal.GetFactor1());
			Color color2 = new Color(color.Red, color.Green, color.Blue, alpha);
			this._mapCursorDecal.SetFactor1(color2.ToUnsignedInteger());
		}

		// Token: 0x0600028D RID: 653 RVA: 0x00017D68 File Offset: 0x00015F68
		private int GetCircleIndex()
		{
			int num = (int)((MBCommon.GetApplicationTime() - this._targetCircleRotationStartTime) / 0.033f);
			if (num >= 10)
			{
				return 0;
			}
			int num2 = num % 10;
			if (num2 >= 5)
			{
				num2 = 10 - num2 - 1;
			}
			return num2;
		}

		// Token: 0x04000167 RID: 359
		private const string GameCursorValidDecalMaterialName = "map_cursor_valid_decal";

		// Token: 0x04000168 RID: 360
		private const string GameCursorInvalidDecalMaterialName = "map_cursor_invalid_decal";

		// Token: 0x04000169 RID: 361
		private const float CursorDecalBaseScale = 0.38f;

		// Token: 0x0400016A RID: 362
		private GameEntity _mapCursorDecalEntity;

		// Token: 0x0400016B RID: 363
		private Decal _mapCursorDecal;

		// Token: 0x0400016C RID: 364
		private MapScreen _mapScreen;

		// Token: 0x0400016D RID: 365
		private Material _gameCursorValidDecalMaterial;

		// Token: 0x0400016E RID: 366
		private Material _gameCursorInvalidDecalMaterial;

		// Token: 0x0400016F RID: 367
		private Vec3 _smoothRotationNormalStart;

		// Token: 0x04000170 RID: 368
		private Vec3 _smoothRotationNormalEnd;

		// Token: 0x04000171 RID: 369
		private Vec3 _smoothRotationNormalCurrent;

		// Token: 0x04000172 RID: 370
		private float _smoothRotationAlpha;

		// Token: 0x04000173 RID: 371
		private int _smallAtlasTextureIndex;

		// Token: 0x04000174 RID: 372
		private float _targetCircleRotationStartTime;

		// Token: 0x04000175 RID: 373
		private bool _gameCursorActive;

		// Token: 0x04000176 RID: 374
		private bool _anotherEntityHiglighted;

		// Token: 0x04000177 RID: 375
		private const float _navigationPositionCheckFrequency = 0.2f;

		// Token: 0x04000178 RID: 376
		private float _navigatablePositionCheckTimer;
	}
}
