using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfICamera : ICamera
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckEntityVisibilityDelegate(UIntPtr cameraPointer, UIntPtr entityPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ConstructCameraFromPositionElevationBearingDelegate(Vec3 position, float elevation, float bearing, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateCameraDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool EnclosesPointDelegate(UIntPtr cameraPointer, Vec3 pointInWorldSpace);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FillParametersFromDelegate(UIntPtr cameraPointer, UIntPtr otherCameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAspectRatioDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetEntityDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetFarDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetFovHorizontalDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetFovVerticalDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetFrameDelegate(UIntPtr cameraPointer, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetHorizontalFovDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetNearDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetNearPlanePointsDelegate(UIntPtr cameraPointer, IntPtr nearPlanePoints);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetNearPlanePointsStaticDelegate(ref MatrixFrame cameraFrame, float verticalFov, float aspectRatioXY, float newDNear, float newDFar, IntPtr nearPlanePoints);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetViewProjMatrixDelegate(UIntPtr cameraPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LookAtDelegate(UIntPtr cameraPointer, Vec3 position, Vec3 target, Vec3 upVector);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseCameraEntityDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderFrustrumDelegate(UIntPtr cameraPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ScreenSpaceRayProjectionDelegate(UIntPtr cameraPointer, Vec2 screenPosition, ref Vec3 rayBegin, ref Vec3 rayEnd);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEntityDelegate(UIntPtr cameraPointer, UIntPtr entityId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFovHorizontalDelegate(UIntPtr cameraPointer, float horizontalFov, float aspectRatio, float newDNear, float newDFar);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFovVerticalDelegate(UIntPtr cameraPointer, float verticalFov, float aspectRatio, float newDNear, float newDFar);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFrameDelegate(UIntPtr cameraPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPositionDelegate(UIntPtr cameraPointer, Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetViewVolumeDelegate(UIntPtr cameraPointer, [MarshalAs(UnmanagedType.U1)] bool perspective, float dLeft, float dRight, float dBottom, float dTop, float dNear, float dFar);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ViewportPointToWorldRayDelegate(UIntPtr cameraPointer, ref Vec3 rayBegin, ref Vec3 rayEnd, Vec3 viewportPoint);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 WorldPointToViewportPointDelegate(UIntPtr cameraPointer, ref Vec3 worldPoint);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CheckEntityVisibilityDelegate call_CheckEntityVisibilityDelegate;

	public static ConstructCameraFromPositionElevationBearingDelegate call_ConstructCameraFromPositionElevationBearingDelegate;

	public static CreateCameraDelegate call_CreateCameraDelegate;

	public static EnclosesPointDelegate call_EnclosesPointDelegate;

	public static FillParametersFromDelegate call_FillParametersFromDelegate;

	public static GetAspectRatioDelegate call_GetAspectRatioDelegate;

	public static GetEntityDelegate call_GetEntityDelegate;

	public static GetFarDelegate call_GetFarDelegate;

	public static GetFovHorizontalDelegate call_GetFovHorizontalDelegate;

	public static GetFovVerticalDelegate call_GetFovVerticalDelegate;

	public static GetFrameDelegate call_GetFrameDelegate;

	public static GetHorizontalFovDelegate call_GetHorizontalFovDelegate;

	public static GetNearDelegate call_GetNearDelegate;

	public static GetNearPlanePointsDelegate call_GetNearPlanePointsDelegate;

	public static GetNearPlanePointsStaticDelegate call_GetNearPlanePointsStaticDelegate;

	public static GetViewProjMatrixDelegate call_GetViewProjMatrixDelegate;

	public static LookAtDelegate call_LookAtDelegate;

	public static ReleaseDelegate call_ReleaseDelegate;

	public static ReleaseCameraEntityDelegate call_ReleaseCameraEntityDelegate;

	public static RenderFrustrumDelegate call_RenderFrustrumDelegate;

	public static ScreenSpaceRayProjectionDelegate call_ScreenSpaceRayProjectionDelegate;

	public static SetEntityDelegate call_SetEntityDelegate;

	public static SetFovHorizontalDelegate call_SetFovHorizontalDelegate;

	public static SetFovVerticalDelegate call_SetFovVerticalDelegate;

	public static SetFrameDelegate call_SetFrameDelegate;

	public static SetPositionDelegate call_SetPositionDelegate;

	public static SetViewVolumeDelegate call_SetViewVolumeDelegate;

	public static ViewportPointToWorldRayDelegate call_ViewportPointToWorldRayDelegate;

	public static WorldPointToViewportPointDelegate call_WorldPointToViewportPointDelegate;

	public bool CheckEntityVisibility(UIntPtr cameraPointer, UIntPtr entityPointer)
	{
		return call_CheckEntityVisibilityDelegate(cameraPointer, entityPointer);
	}

	public void ConstructCameraFromPositionElevationBearing(Vec3 position, float elevation, float bearing, ref MatrixFrame outFrame)
	{
		call_ConstructCameraFromPositionElevationBearingDelegate(position, elevation, bearing, ref outFrame);
	}

	public Camera CreateCamera()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateCameraDelegate();
		Camera result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Camera(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public bool EnclosesPoint(UIntPtr cameraPointer, Vec3 pointInWorldSpace)
	{
		return call_EnclosesPointDelegate(cameraPointer, pointInWorldSpace);
	}

	public void FillParametersFrom(UIntPtr cameraPointer, UIntPtr otherCameraPointer)
	{
		call_FillParametersFromDelegate(cameraPointer, otherCameraPointer);
	}

	public float GetAspectRatio(UIntPtr cameraPointer)
	{
		return call_GetAspectRatioDelegate(cameraPointer);
	}

	public GameEntity GetEntity(UIntPtr cameraPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_GetEntityDelegate(cameraPointer);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public float GetFar(UIntPtr cameraPointer)
	{
		return call_GetFarDelegate(cameraPointer);
	}

	public float GetFovHorizontal(UIntPtr cameraPointer)
	{
		return call_GetFovHorizontalDelegate(cameraPointer);
	}

	public float GetFovVertical(UIntPtr cameraPointer)
	{
		return call_GetFovVerticalDelegate(cameraPointer);
	}

	public void GetFrame(UIntPtr cameraPointer, ref MatrixFrame outFrame)
	{
		call_GetFrameDelegate(cameraPointer, ref outFrame);
	}

	public float GetHorizontalFov(UIntPtr cameraPointer)
	{
		return call_GetHorizontalFovDelegate(cameraPointer);
	}

	public float GetNear(UIntPtr cameraPointer)
	{
		return call_GetNearDelegate(cameraPointer);
	}

	public void GetNearPlanePoints(UIntPtr cameraPointer, Vec3[] nearPlanePoints)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(nearPlanePoints);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetNearPlanePointsDelegate(cameraPointer, pointer);
		pinnedArrayData.Dispose();
	}

	public void GetNearPlanePointsStatic(ref MatrixFrame cameraFrame, float verticalFov, float aspectRatioXY, float newDNear, float newDFar, Vec3[] nearPlanePoints)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(nearPlanePoints);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetNearPlanePointsStaticDelegate(ref cameraFrame, verticalFov, aspectRatioXY, newDNear, newDFar, pointer);
		pinnedArrayData.Dispose();
	}

	public void GetViewProjMatrix(UIntPtr cameraPointer, ref MatrixFrame frame)
	{
		call_GetViewProjMatrixDelegate(cameraPointer, ref frame);
	}

	public void LookAt(UIntPtr cameraPointer, Vec3 position, Vec3 target, Vec3 upVector)
	{
		call_LookAtDelegate(cameraPointer, position, target, upVector);
	}

	public void Release(UIntPtr cameraPointer)
	{
		call_ReleaseDelegate(cameraPointer);
	}

	public void ReleaseCameraEntity(UIntPtr cameraPointer)
	{
		call_ReleaseCameraEntityDelegate(cameraPointer);
	}

	public void RenderFrustrum(UIntPtr cameraPointer)
	{
		call_RenderFrustrumDelegate(cameraPointer);
	}

	public void ScreenSpaceRayProjection(UIntPtr cameraPointer, Vec2 screenPosition, ref Vec3 rayBegin, ref Vec3 rayEnd)
	{
		call_ScreenSpaceRayProjectionDelegate(cameraPointer, screenPosition, ref rayBegin, ref rayEnd);
	}

	public void SetEntity(UIntPtr cameraPointer, UIntPtr entityId)
	{
		call_SetEntityDelegate(cameraPointer, entityId);
	}

	public void SetFovHorizontal(UIntPtr cameraPointer, float horizontalFov, float aspectRatio, float newDNear, float newDFar)
	{
		call_SetFovHorizontalDelegate(cameraPointer, horizontalFov, aspectRatio, newDNear, newDFar);
	}

	public void SetFovVertical(UIntPtr cameraPointer, float verticalFov, float aspectRatio, float newDNear, float newDFar)
	{
		call_SetFovVerticalDelegate(cameraPointer, verticalFov, aspectRatio, newDNear, newDFar);
	}

	public void SetFrame(UIntPtr cameraPointer, ref MatrixFrame frame)
	{
		call_SetFrameDelegate(cameraPointer, ref frame);
	}

	public void SetPosition(UIntPtr cameraPointer, Vec3 position)
	{
		call_SetPositionDelegate(cameraPointer, position);
	}

	public void SetViewVolume(UIntPtr cameraPointer, bool perspective, float dLeft, float dRight, float dBottom, float dTop, float dNear, float dFar)
	{
		call_SetViewVolumeDelegate(cameraPointer, perspective, dLeft, dRight, dBottom, dTop, dNear, dFar);
	}

	public void ViewportPointToWorldRay(UIntPtr cameraPointer, ref Vec3 rayBegin, ref Vec3 rayEnd, Vec3 viewportPoint)
	{
		call_ViewportPointToWorldRayDelegate(cameraPointer, ref rayBegin, ref rayEnd, viewportPoint);
	}

	public Vec3 WorldPointToViewportPoint(UIntPtr cameraPointer, ref Vec3 worldPoint)
	{
		return call_WorldPointToViewportPointDelegate(cameraPointer, ref worldPoint);
	}
}
