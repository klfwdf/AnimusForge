using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200000F RID: 15
	[EngineClass("rglCamera_object")]
	public sealed class Camera : NativeObject
	{
		// Token: 0x0600004E RID: 78 RVA: 0x00002FEE File Offset: 0x000011EE
		internal Camera(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00002FFD File Offset: 0x000011FD
		public static Camera CreateCamera()
		{
			return EngineApplicationInterface.ICamera.CreateCamera();
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003009 File Offset: 0x00001209
		public void ReleaseCamera()
		{
			EngineApplicationInterface.ICamera.Release(base.Pointer);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x0000301B File Offset: 0x0000121B
		public void ReleaseCameraEntity()
		{
			EngineApplicationInterface.ICamera.ReleaseCameraEntity(base.Pointer);
			this.ReleaseCamera();
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003033 File Offset: 0x00001233
		public void LookAt(Vec3 position, Vec3 target, Vec3 upVector)
		{
			EngineApplicationInterface.ICamera.LookAt(base.Pointer, position, target, upVector);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003048 File Offset: 0x00001248
		public void ScreenSpaceRayProjection(Vec2 screenPosition, ref Vec3 rayBegin, ref Vec3 rayEnd)
		{
			EngineApplicationInterface.ICamera.ScreenSpaceRayProjection(base.Pointer, screenPosition, ref rayBegin, ref rayEnd);
			if (this.Entity != null)
			{
				rayBegin = this.Entity.GetGlobalFrame().TransformToParent(rayBegin);
				rayEnd = this.Entity.GetGlobalFrame().TransformToParent(rayEnd);
			}
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000030AA File Offset: 0x000012AA
		public bool CheckEntityVisibility(GameEntity entity)
		{
			return EngineApplicationInterface.ICamera.CheckEntityVisibility(base.Pointer, entity.Pointer);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000030C4 File Offset: 0x000012C4
		public void SetViewVolume(bool perspective, float dLeft, float dRight, float dBottom, float dTop, float dNear, float dFar)
		{
			EngineApplicationInterface.ICamera.SetViewVolume(base.Pointer, perspective, dLeft, dRight, dBottom, dTop, dNear, dFar);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000030EC File Offset: 0x000012EC
		public static void GetNearPlanePointsStatic(ref MatrixFrame cameraFrame, float verticalFov, float aspectRatioXY, float newDNear, float newDFar, Vec3[] nearPlanePoints)
		{
			EngineApplicationInterface.ICamera.GetNearPlanePointsStatic(ref cameraFrame, verticalFov, aspectRatioXY, newDNear, newDFar, nearPlanePoints);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003100 File Offset: 0x00001300
		public void GetNearPlanePoints(Vec3[] nearPlanePoints)
		{
			EngineApplicationInterface.ICamera.GetNearPlanePoints(base.Pointer, nearPlanePoints);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00003113 File Offset: 0x00001313
		public void SetFovVertical(float verticalFov, float aspectRatioXY, float newDNear, float newDFar)
		{
			EngineApplicationInterface.ICamera.SetFovVertical(base.Pointer, verticalFov, aspectRatioXY, newDNear, newDFar);
		}

		// Token: 0x06000059 RID: 89 RVA: 0x0000312A File Offset: 0x0000132A
		public void SetFovHorizontal(float horizontalFov, float aspectRatioXY, float newDNear, float newDFar)
		{
			EngineApplicationInterface.ICamera.SetFovHorizontal(base.Pointer, horizontalFov, aspectRatioXY, newDNear, newDFar);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003141 File Offset: 0x00001341
		public void GetViewProjMatrix(ref MatrixFrame viewProj)
		{
			EngineApplicationInterface.ICamera.GetViewProjMatrix(base.Pointer, ref viewProj);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003154 File Offset: 0x00001354
		public float GetFovVertical()
		{
			return EngineApplicationInterface.ICamera.GetFovVertical(base.Pointer);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003166 File Offset: 0x00001366
		public float GetFovHorizontal()
		{
			return EngineApplicationInterface.ICamera.GetFovHorizontal(base.Pointer);
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00003178 File Offset: 0x00001378
		public float GetAspectRatio()
		{
			return EngineApplicationInterface.ICamera.GetAspectRatio(base.Pointer);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x0000318A File Offset: 0x0000138A
		public void FillParametersFrom(Camera otherCamera)
		{
			EngineApplicationInterface.ICamera.FillParametersFrom(base.Pointer, otherCamera.Pointer);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x000031A2 File Offset: 0x000013A2
		public void RenderFrustrum()
		{
			EngineApplicationInterface.ICamera.RenderFrustrum(base.Pointer);
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000060 RID: 96 RVA: 0x000031B4 File Offset: 0x000013B4
		// (set) Token: 0x06000061 RID: 97 RVA: 0x000031C6 File Offset: 0x000013C6
		public GameEntity Entity
		{
			get
			{
				return EngineApplicationInterface.ICamera.GetEntity(base.Pointer);
			}
			set
			{
				EngineApplicationInterface.ICamera.SetEntity(base.Pointer, value.Pointer);
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000062 RID: 98 RVA: 0x000031DE File Offset: 0x000013DE
		// (set) Token: 0x06000063 RID: 99 RVA: 0x000031EB File Offset: 0x000013EB
		public Vec3 Position
		{
			get
			{
				return this.Frame.origin;
			}
			set
			{
				EngineApplicationInterface.ICamera.SetPosition(base.Pointer, value);
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000064 RID: 100 RVA: 0x000031FE File Offset: 0x000013FE
		public Vec3 Direction
		{
			get
			{
				return -this.Frame.rotation.u;
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00003218 File Offset: 0x00001418
		// (set) Token: 0x06000066 RID: 102 RVA: 0x00003240 File Offset: 0x00001440
		public MatrixFrame Frame
		{
			get
			{
				MatrixFrame result = default(MatrixFrame);
				EngineApplicationInterface.ICamera.GetFrame(base.Pointer, ref result);
				return result;
			}
			set
			{
				EngineApplicationInterface.ICamera.SetFrame(base.Pointer, ref value);
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00003254 File Offset: 0x00001454
		public float Near
		{
			get
			{
				return EngineApplicationInterface.ICamera.GetNear(base.Pointer);
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00003266 File Offset: 0x00001466
		public float Far
		{
			get
			{
				return EngineApplicationInterface.ICamera.GetFar(base.Pointer);
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000069 RID: 105 RVA: 0x00003278 File Offset: 0x00001478
		public float HorizontalFov
		{
			get
			{
				return EngineApplicationInterface.ICamera.GetHorizontalFov(base.Pointer);
			}
		}

		// Token: 0x0600006A RID: 106 RVA: 0x0000328A File Offset: 0x0000148A
		public void ViewportPointToWorldRay(ref Vec3 rayBegin, ref Vec3 rayEnd, Vec2 viewportPoint)
		{
			EngineApplicationInterface.ICamera.ViewportPointToWorldRay(base.Pointer, ref rayBegin, ref rayEnd, viewportPoint.ToVec3(0f));
		}

		// Token: 0x0600006B RID: 107 RVA: 0x000032AA File Offset: 0x000014AA
		public Vec3 WorldPointToViewPortPoint(ref Vec3 worldPoint)
		{
			return EngineApplicationInterface.ICamera.WorldPointToViewportPoint(base.Pointer, ref worldPoint);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x000032BD File Offset: 0x000014BD
		public bool EnclosesPoint(Vec3 pointInWorldSpace)
		{
			return EngineApplicationInterface.ICamera.EnclosesPoint(base.Pointer, pointInWorldSpace);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x000032D0 File Offset: 0x000014D0
		public static MatrixFrame ConstructCameraFromPositionElevationBearing(Vec3 position, float elevation, float bearing)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ICamera.ConstructCameraFromPositionElevationBearing(position, elevation, bearing, ref result);
			return result;
		}
	}
}
