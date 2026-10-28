using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfISceneView : ISceneView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddClearTaskDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool clearOnlySceneview);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckSceneReadyToRenderDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearAllDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool clear_scene, [MarshalAs(UnmanagedType.U1)] bool remove_terrain);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateSceneViewDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DoNotClearDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetSceneDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ProjectedMousePositionOnGroundDelegate(UIntPtr pointer, out Vec3 groundPosition, out Vec3 groundNormal, [MarshalAs(UnmanagedType.U1)] bool mouseVisible, BodyFlags excludeBodyOwnerFlags, [MarshalAs(UnmanagedType.U1)] bool checkOccludedSurface);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ProjectedMousePositionOnWaterDelegate(UIntPtr pointer, out Vec3 groundPosition, [MarshalAs(UnmanagedType.U1)] bool mouseVisible);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool RayCastForClosestEntityOrTerrainDelegate(UIntPtr ptr, ref Vec3 sourcePoint, ref Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ReadyToRenderDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 ScreenPointToViewportPointDelegate(UIntPtr ptr, float position_x, float position_y);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAcceptGlobalDebugRenderObjectsDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCameraDelegate(UIntPtr ptr, UIntPtr cameraPtr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCleanScreenUntilLoadingDoneDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetClearAndDisableAfterSucessfullRenderDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetClearGbufferDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDoQuickExposureDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFocusedShadowmapDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool enable, ref Vec3 center, float radius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetForceShaderCompilationDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPointlightResolutionMultiplierDelegate(UIntPtr pointer, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPostfxConfigParamsDelegate(UIntPtr ptr, int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPostfxFromConfigDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRenderWithPostfxDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetResolutionScalingDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSceneDelegate(UIntPtr ptr, UIntPtr scenePtr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSceneUsesContourDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSceneUsesShadowsDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSceneUsesSkyboxDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetShadowmapResolutionMultiplierDelegate(UIntPtr pointer, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TranslateMouseDelegate(UIntPtr pointer, ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 WorldPointToScreenPointDelegate(UIntPtr ptr, Vec3 position);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddClearTaskDelegate call_AddClearTaskDelegate;

	public static CheckSceneReadyToRenderDelegate call_CheckSceneReadyToRenderDelegate;

	public static ClearAllDelegate call_ClearAllDelegate;

	public static CreateSceneViewDelegate call_CreateSceneViewDelegate;

	public static DoNotClearDelegate call_DoNotClearDelegate;

	public static GetSceneDelegate call_GetSceneDelegate;

	public static ProjectedMousePositionOnGroundDelegate call_ProjectedMousePositionOnGroundDelegate;

	public static ProjectedMousePositionOnWaterDelegate call_ProjectedMousePositionOnWaterDelegate;

	public static RayCastForClosestEntityOrTerrainDelegate call_RayCastForClosestEntityOrTerrainDelegate;

	public static ReadyToRenderDelegate call_ReadyToRenderDelegate;

	public static ScreenPointToViewportPointDelegate call_ScreenPointToViewportPointDelegate;

	public static SetAcceptGlobalDebugRenderObjectsDelegate call_SetAcceptGlobalDebugRenderObjectsDelegate;

	public static SetCameraDelegate call_SetCameraDelegate;

	public static SetCleanScreenUntilLoadingDoneDelegate call_SetCleanScreenUntilLoadingDoneDelegate;

	public static SetClearAndDisableAfterSucessfullRenderDelegate call_SetClearAndDisableAfterSucessfullRenderDelegate;

	public static SetClearGbufferDelegate call_SetClearGbufferDelegate;

	public static SetDoQuickExposureDelegate call_SetDoQuickExposureDelegate;

	public static SetFocusedShadowmapDelegate call_SetFocusedShadowmapDelegate;

	public static SetForceShaderCompilationDelegate call_SetForceShaderCompilationDelegate;

	public static SetPointlightResolutionMultiplierDelegate call_SetPointlightResolutionMultiplierDelegate;

	public static SetPostfxConfigParamsDelegate call_SetPostfxConfigParamsDelegate;

	public static SetPostfxFromConfigDelegate call_SetPostfxFromConfigDelegate;

	public static SetRenderWithPostfxDelegate call_SetRenderWithPostfxDelegate;

	public static SetResolutionScalingDelegate call_SetResolutionScalingDelegate;

	public static SetSceneDelegate call_SetSceneDelegate;

	public static SetSceneUsesContourDelegate call_SetSceneUsesContourDelegate;

	public static SetSceneUsesShadowsDelegate call_SetSceneUsesShadowsDelegate;

	public static SetSceneUsesSkyboxDelegate call_SetSceneUsesSkyboxDelegate;

	public static SetShadowmapResolutionMultiplierDelegate call_SetShadowmapResolutionMultiplierDelegate;

	public static TranslateMouseDelegate call_TranslateMouseDelegate;

	public static WorldPointToScreenPointDelegate call_WorldPointToScreenPointDelegate;

	public void AddClearTask(UIntPtr ptr, bool clearOnlySceneview)
	{
		call_AddClearTaskDelegate(ptr, clearOnlySceneview);
	}

	public bool CheckSceneReadyToRender(UIntPtr ptr)
	{
		return call_CheckSceneReadyToRenderDelegate(ptr);
	}

	public void ClearAll(UIntPtr pointer, bool clear_scene, bool remove_terrain)
	{
		call_ClearAllDelegate(pointer, clear_scene, remove_terrain);
	}

	public SceneView CreateSceneView()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateSceneViewDelegate();
		SceneView result = NativeObject.CreateNativeObjectWrapper<SceneView>(nativeObjectPointer);
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void DoNotClear(UIntPtr pointer, bool value)
	{
		call_DoNotClearDelegate(pointer, value);
	}

	public Scene GetScene(UIntPtr ptr)
	{
		NativeObjectPointer nativeObjectPointer = call_GetSceneDelegate(ptr);
		Scene result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Scene(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public bool ProjectedMousePositionOnGround(UIntPtr pointer, out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface)
	{
		return call_ProjectedMousePositionOnGroundDelegate(pointer, out groundPosition, out groundNormal, mouseVisible, excludeBodyOwnerFlags, checkOccludedSurface);
	}

	public bool ProjectedMousePositionOnWater(UIntPtr pointer, out Vec3 groundPosition, bool mouseVisible)
	{
		return call_ProjectedMousePositionOnWaterDelegate(pointer, out groundPosition, mouseVisible);
	}

	public bool RayCastForClosestEntityOrTerrain(UIntPtr ptr, ref Vec3 sourcePoint, ref Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags)
	{
		return call_RayCastForClosestEntityOrTerrainDelegate(ptr, ref sourcePoint, ref targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags);
	}

	public bool ReadyToRender(UIntPtr pointer)
	{
		return call_ReadyToRenderDelegate(pointer);
	}

	public Vec2 ScreenPointToViewportPoint(UIntPtr ptr, float position_x, float position_y)
	{
		return call_ScreenPointToViewportPointDelegate(ptr, position_x, position_y);
	}

	public void SetAcceptGlobalDebugRenderObjects(UIntPtr ptr, bool value)
	{
		call_SetAcceptGlobalDebugRenderObjectsDelegate(ptr, value);
	}

	public void SetCamera(UIntPtr ptr, UIntPtr cameraPtr)
	{
		call_SetCameraDelegate(ptr, cameraPtr);
	}

	public void SetCleanScreenUntilLoadingDone(UIntPtr pointer, bool value)
	{
		call_SetCleanScreenUntilLoadingDoneDelegate(pointer, value);
	}

	public void SetClearAndDisableAfterSucessfullRender(UIntPtr pointer, bool value)
	{
		call_SetClearAndDisableAfterSucessfullRenderDelegate(pointer, value);
	}

	public void SetClearGbuffer(UIntPtr pointer, bool value)
	{
		call_SetClearGbufferDelegate(pointer, value);
	}

	public void SetDoQuickExposure(UIntPtr ptr, bool value)
	{
		call_SetDoQuickExposureDelegate(ptr, value);
	}

	public void SetFocusedShadowmap(UIntPtr ptr, bool enable, ref Vec3 center, float radius)
	{
		call_SetFocusedShadowmapDelegate(ptr, enable, ref center, radius);
	}

	public void SetForceShaderCompilation(UIntPtr ptr, bool value)
	{
		call_SetForceShaderCompilationDelegate(ptr, value);
	}

	public void SetPointlightResolutionMultiplier(UIntPtr pointer, float value)
	{
		call_SetPointlightResolutionMultiplierDelegate(pointer, value);
	}

	public void SetPostfxConfigParams(UIntPtr ptr, int value)
	{
		call_SetPostfxConfigParamsDelegate(ptr, value);
	}

	public void SetPostfxFromConfig(UIntPtr ptr)
	{
		call_SetPostfxFromConfigDelegate(ptr);
	}

	public void SetRenderWithPostfx(UIntPtr ptr, bool value)
	{
		call_SetRenderWithPostfxDelegate(ptr, value);
	}

	public void SetResolutionScaling(UIntPtr ptr, bool value)
	{
		call_SetResolutionScalingDelegate(ptr, value);
	}

	public void SetScene(UIntPtr ptr, UIntPtr scenePtr)
	{
		call_SetSceneDelegate(ptr, scenePtr);
	}

	public void SetSceneUsesContour(UIntPtr pointer, bool value)
	{
		call_SetSceneUsesContourDelegate(pointer, value);
	}

	public void SetSceneUsesShadows(UIntPtr pointer, bool value)
	{
		call_SetSceneUsesShadowsDelegate(pointer, value);
	}

	public void SetSceneUsesSkybox(UIntPtr pointer, bool value)
	{
		call_SetSceneUsesSkyboxDelegate(pointer, value);
	}

	public void SetShadowmapResolutionMultiplier(UIntPtr pointer, float value)
	{
		call_SetShadowmapResolutionMultiplierDelegate(pointer, value);
	}

	public void TranslateMouse(UIntPtr pointer, ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance)
	{
		call_TranslateMouseDelegate(pointer, ref worldMouseNear, ref worldMouseFar, maxDistance);
	}

	public Vec2 WorldPointToScreenPoint(UIntPtr ptr, Vec3 position)
	{
		return call_WorldPointToScreenPointDelegate(ptr, position);
	}
}
