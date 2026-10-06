using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBEditor : IMBEditor
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ActivateSceneEditorPresentationDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddEditorWarningDelegate(byte[] msg);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddEntityWarningDelegate(UIntPtr entityId, byte[] msg);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddNavMeshWarningDelegate(UIntPtr sceneId, in PathFaceRecord record, byte[] msg);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ApplyDeltaToEditorCameraDelegate(in Vec3 delta);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool BorderHelpersEnabledDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeactivateSceneEditorPresentationDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnterEditMissionModeDelegate(UIntPtr missionPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnterEditModeDelegate(UIntPtr sceneWidgetPointer, ref MatrixFrame initialCameraFrame, float initialCameraElevation, float initialCameraBearing);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ExitEditModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAllPrefabsAndChildWithTagDelegate(byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetEditorSceneViewDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HelpersEnabledDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsEditModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsEditModeEnabledDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsEntitySelectedDelegate(UIntPtr entityId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsReplayManagerRecordingDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsReplayManagerRenderingDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsReplayManagerReplayingDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LeaveEditMissionModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LeaveEditModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderEditorMeshDelegate(UIntPtr metaMeshId, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLevelVisibilityDelegate(byte[] cumulated_string);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUpgradeLevelVisibilityDelegate(byte[] cumulated_string);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TickEditModeDelegate(float dt);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TickSceneEditorPresentationDelegate(float dt);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ToggleEnableEditorPhysicsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateSceneTreeDelegate([MarshalAs(UnmanagedType.U1)] bool do_next_frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ZoomToPositionDelegate(Vec3 pos);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static ActivateSceneEditorPresentationDelegate call_ActivateSceneEditorPresentationDelegate;

	public static AddEditorWarningDelegate call_AddEditorWarningDelegate;

	public static AddEntityWarningDelegate call_AddEntityWarningDelegate;

	public static AddNavMeshWarningDelegate call_AddNavMeshWarningDelegate;

	public static ApplyDeltaToEditorCameraDelegate call_ApplyDeltaToEditorCameraDelegate;

	public static BorderHelpersEnabledDelegate call_BorderHelpersEnabledDelegate;

	public static DeactivateSceneEditorPresentationDelegate call_DeactivateSceneEditorPresentationDelegate;

	public static EnterEditMissionModeDelegate call_EnterEditMissionModeDelegate;

	public static EnterEditModeDelegate call_EnterEditModeDelegate;

	public static ExitEditModeDelegate call_ExitEditModeDelegate;

	public static GetAllPrefabsAndChildWithTagDelegate call_GetAllPrefabsAndChildWithTagDelegate;

	public static GetEditorSceneViewDelegate call_GetEditorSceneViewDelegate;

	public static HelpersEnabledDelegate call_HelpersEnabledDelegate;

	public static IsEditModeDelegate call_IsEditModeDelegate;

	public static IsEditModeEnabledDelegate call_IsEditModeEnabledDelegate;

	public static IsEntitySelectedDelegate call_IsEntitySelectedDelegate;

	public static IsReplayManagerRecordingDelegate call_IsReplayManagerRecordingDelegate;

	public static IsReplayManagerRenderingDelegate call_IsReplayManagerRenderingDelegate;

	public static IsReplayManagerReplayingDelegate call_IsReplayManagerReplayingDelegate;

	public static LeaveEditMissionModeDelegate call_LeaveEditMissionModeDelegate;

	public static LeaveEditModeDelegate call_LeaveEditModeDelegate;

	public static RenderEditorMeshDelegate call_RenderEditorMeshDelegate;

	public static SetLevelVisibilityDelegate call_SetLevelVisibilityDelegate;

	public static SetUpgradeLevelVisibilityDelegate call_SetUpgradeLevelVisibilityDelegate;

	public static TickEditModeDelegate call_TickEditModeDelegate;

	public static TickSceneEditorPresentationDelegate call_TickSceneEditorPresentationDelegate;

	public static ToggleEnableEditorPhysicsDelegate call_ToggleEnableEditorPhysicsDelegate;

	public static UpdateSceneTreeDelegate call_UpdateSceneTreeDelegate;

	public static ZoomToPositionDelegate call_ZoomToPositionDelegate;

	public void ActivateSceneEditorPresentation()
	{
		call_ActivateSceneEditorPresentationDelegate();
	}

	public void AddEditorWarning(string msg)
	{
		byte[] array = null;
		if (msg != null)
		{
			int byteCount = _utf8.GetByteCount(msg);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(msg, 0, msg.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddEditorWarningDelegate(array);
	}

	public void AddEntityWarning(UIntPtr entityId, string msg)
	{
		byte[] array = null;
		if (msg != null)
		{
			int byteCount = _utf8.GetByteCount(msg);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(msg, 0, msg.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddEntityWarningDelegate(entityId, array);
	}

	public void AddNavMeshWarning(UIntPtr sceneId, in PathFaceRecord record, string msg)
	{
		byte[] array = null;
		if (msg != null)
		{
			int byteCount = _utf8.GetByteCount(msg);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(msg, 0, msg.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddNavMeshWarningDelegate(sceneId, in record, array);
	}

	public void ApplyDeltaToEditorCamera(in Vec3 delta)
	{
		call_ApplyDeltaToEditorCameraDelegate(in delta);
	}

	public bool BorderHelpersEnabled()
	{
		return call_BorderHelpersEnabledDelegate();
	}

	public void DeactivateSceneEditorPresentation()
	{
		call_DeactivateSceneEditorPresentationDelegate();
	}

	public void EnterEditMissionMode(UIntPtr missionPointer)
	{
		call_EnterEditMissionModeDelegate(missionPointer);
	}

	public void EnterEditMode(UIntPtr sceneWidgetPointer, ref MatrixFrame initialCameraFrame, float initialCameraElevation, float initialCameraBearing)
	{
		call_EnterEditModeDelegate(sceneWidgetPointer, ref initialCameraFrame, initialCameraElevation, initialCameraBearing);
	}

	public void ExitEditMode()
	{
		call_ExitEditModeDelegate();
	}

	public string GetAllPrefabsAndChildWithTag(string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetAllPrefabsAndChildWithTagDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public SceneView GetEditorSceneView()
	{
		NativeObjectPointer nativeObjectPointer = call_GetEditorSceneViewDelegate();
		SceneView result = NativeObject.CreateNativeObjectWrapper<SceneView>(nativeObjectPointer);
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public bool HelpersEnabled()
	{
		return call_HelpersEnabledDelegate();
	}

	public bool IsEditMode()
	{
		return call_IsEditModeDelegate();
	}

	public bool IsEditModeEnabled()
	{
		return call_IsEditModeEnabledDelegate();
	}

	public bool IsEntitySelected(UIntPtr entityId)
	{
		return call_IsEntitySelectedDelegate(entityId);
	}

	public bool IsReplayManagerRecording()
	{
		return call_IsReplayManagerRecordingDelegate();
	}

	public bool IsReplayManagerRendering()
	{
		return call_IsReplayManagerRenderingDelegate();
	}

	public bool IsReplayManagerReplaying()
	{
		return call_IsReplayManagerReplayingDelegate();
	}

	public void LeaveEditMissionMode()
	{
		call_LeaveEditMissionModeDelegate();
	}

	public void LeaveEditMode()
	{
		call_LeaveEditModeDelegate();
	}

	public void RenderEditorMesh(UIntPtr metaMeshId, ref MatrixFrame frame)
	{
		call_RenderEditorMeshDelegate(metaMeshId, ref frame);
	}

	public void SetLevelVisibility(string cumulated_string)
	{
		byte[] array = null;
		if (cumulated_string != null)
		{
			int byteCount = _utf8.GetByteCount(cumulated_string);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(cumulated_string, 0, cumulated_string.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetLevelVisibilityDelegate(array);
	}

	public void SetUpgradeLevelVisibility(string cumulated_string)
	{
		byte[] array = null;
		if (cumulated_string != null)
		{
			int byteCount = _utf8.GetByteCount(cumulated_string);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(cumulated_string, 0, cumulated_string.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetUpgradeLevelVisibilityDelegate(array);
	}

	public void TickEditMode(float dt)
	{
		call_TickEditModeDelegate(dt);
	}

	public void TickSceneEditorPresentation(float dt)
	{
		call_TickSceneEditorPresentationDelegate(dt);
	}

	public void ToggleEnableEditorPhysics()
	{
		call_ToggleEnableEditorPhysicsDelegate();
	}

	public void UpdateSceneTree(bool do_next_frame)
	{
		call_UpdateSceneTreeDelegate(do_next_frame);
	}

	public void ZoomToPosition(Vec3 pos)
	{
		call_ZoomToPositionDelegate(pos);
	}

	void IMBEditor.ApplyDeltaToEditorCamera(in Vec3 delta)
	{
		ApplyDeltaToEditorCamera(in delta);
	}

	void IMBEditor.AddNavMeshWarning(UIntPtr sceneId, in PathFaceRecord record, string msg)
	{
		AddNavMeshWarning(sceneId, in record, msg);
	}
}
