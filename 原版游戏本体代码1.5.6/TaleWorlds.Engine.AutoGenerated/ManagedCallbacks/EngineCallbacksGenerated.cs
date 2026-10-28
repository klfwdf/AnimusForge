using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal static class EngineCallbacksGenerated
{
	internal delegate UIntPtr CrashInformationCollector_CollectInformation_delegate();

	internal delegate UIntPtr EngineController_GetApplicationPlatformName_delegate();

	internal delegate UIntPtr EngineController_GetModulesVersionStr_delegate();

	internal delegate UIntPtr EngineController_GetVersionStr_delegate();

	internal delegate void EngineController_Initialize_delegate();

	internal delegate void EngineController_OnConfigChange_delegate();

	internal delegate void EngineController_OnConstrainedStateChange_delegate([MarshalAs(UnmanagedType.U1)] bool isConstrained);

	internal delegate void EngineController_OnControllerDisconnection_delegate();

	internal delegate void EngineController_OnDLCInstalled_delegate();

	internal delegate void EngineController_OnDLCLoaded_delegate();

	internal delegate void EngineManaged_CheckSharedStructureSizes_delegate();

	internal delegate void EngineManaged_EngineApiMethodInterfaceInitializer_delegate(int id, IntPtr pointer);

	internal delegate void EngineManaged_FillEngineApiPointers_delegate();

	internal delegate void EngineScreenManager_InitializeLastPressedKeys_delegate(NativeObjectPointer lastKeysPressed);

	internal delegate void EngineScreenManager_LateTick_delegate(float dt);

	internal delegate void EngineScreenManager_OnGameWindowFocusChange_delegate([MarshalAs(UnmanagedType.U1)] bool focusGained);

	internal delegate void EngineScreenManager_OnOnscreenKeyboardCanceled_delegate();

	internal delegate void EngineScreenManager_OnOnscreenKeyboardDone_delegate(IntPtr inputText);

	internal delegate void EngineScreenManager_PreTick_delegate(float dt);

	internal delegate void EngineScreenManager_Tick_delegate(float dt);

	internal delegate void EngineScreenManager_Update_delegate();

	internal delegate void ManagedExtensions_CollectCommandLineFunctions_delegate();

	internal delegate void ManagedExtensions_CopyObjectFieldsFrom_delegate(int dst, int src, IntPtr className, int callFieldChangeEventAsInteger);

	internal delegate int ManagedExtensions_CreateScriptComponentInstance_delegate(IntPtr className, UIntPtr entityPtr, NativeObjectPointer managedScriptComponent);

	internal delegate void ManagedExtensions_ForceGarbageCollect_delegate();

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool ManagedExtensions_GetEditorVisibilityOfField_delegate(uint classNameHash, uint fieldNamehash);

	internal delegate void ManagedExtensions_GetObjectField_delegate(int managedObject, uint classNameHash, ref ScriptComponentFieldHolder scriptComponentFieldHolder, uint fieldNameHash, RglScriptFieldType type);

	internal delegate UIntPtr ManagedExtensions_GetScriptComponentClassNames_delegate();

	internal delegate RglScriptFieldType ManagedExtensions_GetTypeOfField_delegate(uint classNameHash, uint fieldNameHash);

	internal delegate void ManagedExtensions_SetObjectFieldBool_delegate(int managedObject, uint classNameHash, uint fieldNameHash, [MarshalAs(UnmanagedType.U1)] bool value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldColor_delegate(int managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldDouble_delegate(int managedObject, uint classNameHash, uint fieldNameHash, double value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldEntity_delegate(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldEnum_delegate(int managedObject, uint classNameHash, uint fieldNameHash, IntPtr value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldFloat_delegate(int managedObject, uint classNameHash, uint fieldNameHash, float value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldInt_delegate(int managedObject, uint classNameHash, uint fieldNameHash, int value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldMaterial_delegate(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldMatrixFrame_delegate(int managedObject, uint classNameHash, uint fieldNameHash, MatrixFrame value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldMesh_delegate(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldString_delegate(int managedObject, uint classNameHash, uint fieldNameHash, IntPtr value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldTexture_delegate(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger);

	internal delegate void ManagedExtensions_SetObjectFieldVec3_delegate(int managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger);

	internal delegate int ManagedScriptHolder_CreateManagedScriptHolder_delegate();

	internal delegate void ManagedScriptHolder_FixedTickComponents_delegate(int thisPointer, float fixedDt);

	internal delegate int ManagedScriptHolder_GetNumberOfScripts_delegate(int thisPointer);

	internal delegate void ManagedScriptHolder_RemoveScriptComponentFromAllTickLists_delegate(int thisPointer, int sc);

	internal delegate void ManagedScriptHolder_SetScriptComponentHolder_delegate(int thisPointer, int sc);

	internal delegate void ManagedScriptHolder_TickComponents_delegate(int thisPointer, float dt);

	internal delegate void ManagedScriptHolder_TickComponentsEditor_delegate(int thisPointer, float dt);

	internal delegate void MessageManagerBase_PostMessageLine_delegate(int thisPointer, IntPtr text, uint color);

	internal delegate void MessageManagerBase_PostMessageLineFormatted_delegate(int thisPointer, IntPtr text, uint color);

	internal delegate void MessageManagerBase_PostSuccessLine_delegate(int thisPointer, IntPtr text);

	internal delegate void MessageManagerBase_PostWarningLine_delegate(int thisPointer, IntPtr text);

	internal delegate void NativeParallelDriver_ParalelForLoopBodyCaller_delegate(long loopBodyKey, int localStartIndex, int localEndIndex);

	internal delegate void NativeParallelDriver_ParalelForLoopBodyWithDtCaller_delegate(long loopBodyKey, int localStartIndex, int localEndIndex);

	internal delegate int RenderTargetComponent_CreateRenderTargetComponent_delegate(NativeObjectPointer renderTarget);

	internal delegate void RenderTargetComponent_OnPaintNeeded_delegate(int thisPointer);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool SceneProblemChecker_OnCheckForSceneProblems_delegate(NativeObjectPointer scene);

	internal delegate void ScriptComponentBehavior_AddScriptComponentToTick_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_DeregisterAsPrefabScriptComponent_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_DeregisterAsUndoStackScriptComponent_delegate(int thisPointer);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool ScriptComponentBehavior_DisablesOroCreation_delegate(int thisPointer);

	internal delegate int ScriptComponentBehavior_GetEditableFields_delegate(IntPtr className);

	internal delegate void ScriptComponentBehavior_HandleOnRemoved_delegate(int thisPointer, int removeReason);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool ScriptComponentBehavior_IsOnlyVisual_delegate(int thisPointer);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool ScriptComponentBehavior_MovesEntity_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnBoundingBoxValidate_delegate(int thisPointer);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool ScriptComponentBehavior_OnCheckForProblems_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnDynamicNavmeshVertexUpdate_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnEditModeVisibilityChanged_delegate(int thisPointer, [MarshalAs(UnmanagedType.U1)] bool currentVisibility);

	internal delegate void ScriptComponentBehavior_OnEditorInit_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnEditorTick_delegate(int thisPointer, float dt);

	internal delegate void ScriptComponentBehavior_OnEditorValidate_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnEditorVariableChanged_delegate(int thisPointer, IntPtr variableName);

	internal delegate void ScriptComponentBehavior_OnInit_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnPhysicsCollisionAux_delegate(int thisPointer, ref PhysicsContact contact, UIntPtr entity0, UIntPtr entity1);

	internal delegate void ScriptComponentBehavior_OnPreInit_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnSaveAsPrefab_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_OnSceneSave_delegate(int thisPointer, IntPtr saveFolder);

	internal delegate void ScriptComponentBehavior_OnTerrainReload_delegate(int thisPointer, int step);

	internal delegate void ScriptComponentBehavior_RegisterAsPrefabScriptComponent_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_RegisterAsUndoStackScriptComponent_delegate(int thisPointer);

	internal delegate void ScriptComponentBehavior_SetScene_delegate(int thisPointer, NativeObjectPointer scene);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool ScriptComponentBehavior_SkeletonPostIntegrateCallbackAux_delegate(int script, UIntPtr animResultPointer);

	internal delegate void ThumbnailCreatorView_OnThumbnailRenderComplete_delegate(IntPtr renderId, NativeObjectPointer renderTarget);

	internal static Delegate[] Delegates { get; private set; }

	public static void Initialize()
	{
		Delegates = new Delegate[85];
		Delegates[0] = new CrashInformationCollector_CollectInformation_delegate(CrashInformationCollector_CollectInformation);
		Delegates[1] = new EngineController_GetApplicationPlatformName_delegate(EngineController_GetApplicationPlatformName);
		Delegates[2] = new EngineController_GetModulesVersionStr_delegate(EngineController_GetModulesVersionStr);
		Delegates[3] = new EngineController_GetVersionStr_delegate(EngineController_GetVersionStr);
		Delegates[4] = new EngineController_Initialize_delegate(EngineController_Initialize);
		Delegates[5] = new EngineController_OnConfigChange_delegate(EngineController_OnConfigChange);
		Delegates[6] = new EngineController_OnConstrainedStateChange_delegate(EngineController_OnConstrainedStateChange);
		Delegates[7] = new EngineController_OnControllerDisconnection_delegate(EngineController_OnControllerDisconnection);
		Delegates[8] = new EngineController_OnDLCInstalled_delegate(EngineController_OnDLCInstalled);
		Delegates[9] = new EngineController_OnDLCLoaded_delegate(EngineController_OnDLCLoaded);
		Delegates[10] = new EngineManaged_CheckSharedStructureSizes_delegate(EngineManaged_CheckSharedStructureSizes);
		Delegates[11] = new EngineManaged_EngineApiMethodInterfaceInitializer_delegate(EngineManaged_EngineApiMethodInterfaceInitializer);
		Delegates[12] = new EngineManaged_FillEngineApiPointers_delegate(EngineManaged_FillEngineApiPointers);
		Delegates[13] = new EngineScreenManager_InitializeLastPressedKeys_delegate(EngineScreenManager_InitializeLastPressedKeys);
		Delegates[14] = new EngineScreenManager_LateTick_delegate(EngineScreenManager_LateTick);
		Delegates[15] = new EngineScreenManager_OnGameWindowFocusChange_delegate(EngineScreenManager_OnGameWindowFocusChange);
		Delegates[16] = new EngineScreenManager_OnOnscreenKeyboardCanceled_delegate(EngineScreenManager_OnOnscreenKeyboardCanceled);
		Delegates[17] = new EngineScreenManager_OnOnscreenKeyboardDone_delegate(EngineScreenManager_OnOnscreenKeyboardDone);
		Delegates[18] = new EngineScreenManager_PreTick_delegate(EngineScreenManager_PreTick);
		Delegates[19] = new EngineScreenManager_Tick_delegate(EngineScreenManager_Tick);
		Delegates[20] = new EngineScreenManager_Update_delegate(EngineScreenManager_Update);
		Delegates[21] = new ManagedExtensions_CollectCommandLineFunctions_delegate(ManagedExtensions_CollectCommandLineFunctions);
		Delegates[22] = new ManagedExtensions_CopyObjectFieldsFrom_delegate(ManagedExtensions_CopyObjectFieldsFrom);
		Delegates[23] = new ManagedExtensions_CreateScriptComponentInstance_delegate(ManagedExtensions_CreateScriptComponentInstance);
		Delegates[24] = new ManagedExtensions_ForceGarbageCollect_delegate(ManagedExtensions_ForceGarbageCollect);
		Delegates[25] = new ManagedExtensions_GetEditorVisibilityOfField_delegate(ManagedExtensions_GetEditorVisibilityOfField);
		Delegates[26] = new ManagedExtensions_GetObjectField_delegate(ManagedExtensions_GetObjectField);
		Delegates[27] = new ManagedExtensions_GetScriptComponentClassNames_delegate(ManagedExtensions_GetScriptComponentClassNames);
		Delegates[28] = new ManagedExtensions_GetTypeOfField_delegate(ManagedExtensions_GetTypeOfField);
		Delegates[29] = new ManagedExtensions_SetObjectFieldBool_delegate(ManagedExtensions_SetObjectFieldBool);
		Delegates[30] = new ManagedExtensions_SetObjectFieldColor_delegate(ManagedExtensions_SetObjectFieldColor);
		Delegates[31] = new ManagedExtensions_SetObjectFieldDouble_delegate(ManagedExtensions_SetObjectFieldDouble);
		Delegates[32] = new ManagedExtensions_SetObjectFieldEntity_delegate(ManagedExtensions_SetObjectFieldEntity);
		Delegates[33] = new ManagedExtensions_SetObjectFieldEnum_delegate(ManagedExtensions_SetObjectFieldEnum);
		Delegates[34] = new ManagedExtensions_SetObjectFieldFloat_delegate(ManagedExtensions_SetObjectFieldFloat);
		Delegates[35] = new ManagedExtensions_SetObjectFieldInt_delegate(ManagedExtensions_SetObjectFieldInt);
		Delegates[36] = new ManagedExtensions_SetObjectFieldMaterial_delegate(ManagedExtensions_SetObjectFieldMaterial);
		Delegates[37] = new ManagedExtensions_SetObjectFieldMatrixFrame_delegate(ManagedExtensions_SetObjectFieldMatrixFrame);
		Delegates[38] = new ManagedExtensions_SetObjectFieldMesh_delegate(ManagedExtensions_SetObjectFieldMesh);
		Delegates[39] = new ManagedExtensions_SetObjectFieldString_delegate(ManagedExtensions_SetObjectFieldString);
		Delegates[40] = new ManagedExtensions_SetObjectFieldTexture_delegate(ManagedExtensions_SetObjectFieldTexture);
		Delegates[41] = new ManagedExtensions_SetObjectFieldVec3_delegate(ManagedExtensions_SetObjectFieldVec3);
		Delegates[42] = new ManagedScriptHolder_CreateManagedScriptHolder_delegate(ManagedScriptHolder_CreateManagedScriptHolder);
		Delegates[43] = new ManagedScriptHolder_FixedTickComponents_delegate(ManagedScriptHolder_FixedTickComponents);
		Delegates[44] = new ManagedScriptHolder_GetNumberOfScripts_delegate(ManagedScriptHolder_GetNumberOfScripts);
		Delegates[45] = new ManagedScriptHolder_RemoveScriptComponentFromAllTickLists_delegate(ManagedScriptHolder_RemoveScriptComponentFromAllTickLists);
		Delegates[46] = new ManagedScriptHolder_SetScriptComponentHolder_delegate(ManagedScriptHolder_SetScriptComponentHolder);
		Delegates[47] = new ManagedScriptHolder_TickComponents_delegate(ManagedScriptHolder_TickComponents);
		Delegates[48] = new ManagedScriptHolder_TickComponentsEditor_delegate(ManagedScriptHolder_TickComponentsEditor);
		Delegates[49] = new MessageManagerBase_PostMessageLine_delegate(MessageManagerBase_PostMessageLine);
		Delegates[50] = new MessageManagerBase_PostMessageLineFormatted_delegate(MessageManagerBase_PostMessageLineFormatted);
		Delegates[51] = new MessageManagerBase_PostSuccessLine_delegate(MessageManagerBase_PostSuccessLine);
		Delegates[52] = new MessageManagerBase_PostWarningLine_delegate(MessageManagerBase_PostWarningLine);
		Delegates[53] = new NativeParallelDriver_ParalelForLoopBodyCaller_delegate(NativeParallelDriver_ParalelForLoopBodyCaller);
		Delegates[54] = new NativeParallelDriver_ParalelForLoopBodyWithDtCaller_delegate(NativeParallelDriver_ParalelForLoopBodyWithDtCaller);
		Delegates[55] = new RenderTargetComponent_CreateRenderTargetComponent_delegate(RenderTargetComponent_CreateRenderTargetComponent);
		Delegates[56] = new RenderTargetComponent_OnPaintNeeded_delegate(RenderTargetComponent_OnPaintNeeded);
		Delegates[57] = new SceneProblemChecker_OnCheckForSceneProblems_delegate(SceneProblemChecker_OnCheckForSceneProblems);
		Delegates[58] = new ScriptComponentBehavior_AddScriptComponentToTick_delegate(ScriptComponentBehavior_AddScriptComponentToTick);
		Delegates[59] = new ScriptComponentBehavior_DeregisterAsPrefabScriptComponent_delegate(ScriptComponentBehavior_DeregisterAsPrefabScriptComponent);
		Delegates[60] = new ScriptComponentBehavior_DeregisterAsUndoStackScriptComponent_delegate(ScriptComponentBehavior_DeregisterAsUndoStackScriptComponent);
		Delegates[61] = new ScriptComponentBehavior_DisablesOroCreation_delegate(ScriptComponentBehavior_DisablesOroCreation);
		Delegates[62] = new ScriptComponentBehavior_GetEditableFields_delegate(ScriptComponentBehavior_GetEditableFields);
		Delegates[63] = new ScriptComponentBehavior_HandleOnRemoved_delegate(ScriptComponentBehavior_HandleOnRemoved);
		Delegates[64] = new ScriptComponentBehavior_IsOnlyVisual_delegate(ScriptComponentBehavior_IsOnlyVisual);
		Delegates[65] = new ScriptComponentBehavior_MovesEntity_delegate(ScriptComponentBehavior_MovesEntity);
		Delegates[66] = new ScriptComponentBehavior_OnBoundingBoxValidate_delegate(ScriptComponentBehavior_OnBoundingBoxValidate);
		Delegates[67] = new ScriptComponentBehavior_OnCheckForProblems_delegate(ScriptComponentBehavior_OnCheckForProblems);
		Delegates[68] = new ScriptComponentBehavior_OnDynamicNavmeshVertexUpdate_delegate(ScriptComponentBehavior_OnDynamicNavmeshVertexUpdate);
		Delegates[69] = new ScriptComponentBehavior_OnEditModeVisibilityChanged_delegate(ScriptComponentBehavior_OnEditModeVisibilityChanged);
		Delegates[70] = new ScriptComponentBehavior_OnEditorInit_delegate(ScriptComponentBehavior_OnEditorInit);
		Delegates[71] = new ScriptComponentBehavior_OnEditorTick_delegate(ScriptComponentBehavior_OnEditorTick);
		Delegates[72] = new ScriptComponentBehavior_OnEditorValidate_delegate(ScriptComponentBehavior_OnEditorValidate);
		Delegates[73] = new ScriptComponentBehavior_OnEditorVariableChanged_delegate(ScriptComponentBehavior_OnEditorVariableChanged);
		Delegates[74] = new ScriptComponentBehavior_OnInit_delegate(ScriptComponentBehavior_OnInit);
		Delegates[75] = new ScriptComponentBehavior_OnPhysicsCollisionAux_delegate(ScriptComponentBehavior_OnPhysicsCollisionAux);
		Delegates[76] = new ScriptComponentBehavior_OnPreInit_delegate(ScriptComponentBehavior_OnPreInit);
		Delegates[77] = new ScriptComponentBehavior_OnSaveAsPrefab_delegate(ScriptComponentBehavior_OnSaveAsPrefab);
		Delegates[78] = new ScriptComponentBehavior_OnSceneSave_delegate(ScriptComponentBehavior_OnSceneSave);
		Delegates[79] = new ScriptComponentBehavior_OnTerrainReload_delegate(ScriptComponentBehavior_OnTerrainReload);
		Delegates[80] = new ScriptComponentBehavior_RegisterAsPrefabScriptComponent_delegate(ScriptComponentBehavior_RegisterAsPrefabScriptComponent);
		Delegates[81] = new ScriptComponentBehavior_RegisterAsUndoStackScriptComponent_delegate(ScriptComponentBehavior_RegisterAsUndoStackScriptComponent);
		Delegates[82] = new ScriptComponentBehavior_SetScene_delegate(ScriptComponentBehavior_SetScene);
		Delegates[83] = new ScriptComponentBehavior_SkeletonPostIntegrateCallbackAux_delegate(ScriptComponentBehavior_SkeletonPostIntegrateCallbackAux);
		Delegates[84] = new ThumbnailCreatorView_OnThumbnailRenderComplete_delegate(ThumbnailCreatorView_OnThumbnailRenderComplete);
	}

	[MonoPInvokeCallback(typeof(CrashInformationCollector_CollectInformation_delegate))]
	internal static UIntPtr CrashInformationCollector_CollectInformation()
	{
		string text = CrashInformationCollector.CollectInformation();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(EngineController_GetApplicationPlatformName_delegate))]
	internal static UIntPtr EngineController_GetApplicationPlatformName()
	{
		string applicationPlatformName = EngineController.GetApplicationPlatformName();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, applicationPlatformName);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(EngineController_GetModulesVersionStr_delegate))]
	internal static UIntPtr EngineController_GetModulesVersionStr()
	{
		string modulesVersionStr = EngineController.GetModulesVersionStr();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, modulesVersionStr);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(EngineController_GetVersionStr_delegate))]
	internal static UIntPtr EngineController_GetVersionStr()
	{
		string versionStr = EngineController.GetVersionStr();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, versionStr);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(EngineController_Initialize_delegate))]
	internal static void EngineController_Initialize()
	{
		EngineController.Initialize();
	}

	[MonoPInvokeCallback(typeof(EngineController_OnConfigChange_delegate))]
	internal static void EngineController_OnConfigChange()
	{
		EngineController.OnConfigChange();
	}

	[MonoPInvokeCallback(typeof(EngineController_OnConstrainedStateChange_delegate))]
	internal static void EngineController_OnConstrainedStateChange(bool isConstrained)
	{
		EngineController.OnConstrainedStateChange(isConstrained);
	}

	[MonoPInvokeCallback(typeof(EngineController_OnControllerDisconnection_delegate))]
	internal static void EngineController_OnControllerDisconnection()
	{
		EngineController.OnControllerDisconnection();
	}

	[MonoPInvokeCallback(typeof(EngineController_OnDLCInstalled_delegate))]
	internal static void EngineController_OnDLCInstalled()
	{
		EngineController.OnDLCInstalled();
	}

	[MonoPInvokeCallback(typeof(EngineController_OnDLCLoaded_delegate))]
	internal static void EngineController_OnDLCLoaded()
	{
		EngineController.OnDLCLoaded();
	}

	[MonoPInvokeCallback(typeof(EngineManaged_CheckSharedStructureSizes_delegate))]
	internal static void EngineManaged_CheckSharedStructureSizes()
	{
		EngineManaged.CheckSharedStructureSizes();
	}

	[MonoPInvokeCallback(typeof(EngineManaged_EngineApiMethodInterfaceInitializer_delegate))]
	internal static void EngineManaged_EngineApiMethodInterfaceInitializer(int id, IntPtr pointer)
	{
		EngineManaged.EngineApiMethodInterfaceInitializer(id, pointer);
	}

	[MonoPInvokeCallback(typeof(EngineManaged_FillEngineApiPointers_delegate))]
	internal static void EngineManaged_FillEngineApiPointers()
	{
		EngineManaged.FillEngineApiPointers();
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_InitializeLastPressedKeys_delegate))]
	internal static void EngineScreenManager_InitializeLastPressedKeys(NativeObjectPointer lastKeysPressed)
	{
		NativeArray lastKeysPressed2 = null;
		if (lastKeysPressed.Pointer != UIntPtr.Zero)
		{
			lastKeysPressed2 = new NativeArray(lastKeysPressed.Pointer);
		}
		EngineScreenManager.InitializeLastPressedKeys(lastKeysPressed2);
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_LateTick_delegate))]
	internal static void EngineScreenManager_LateTick(float dt)
	{
		EngineScreenManager.LateTick(dt);
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_OnGameWindowFocusChange_delegate))]
	internal static void EngineScreenManager_OnGameWindowFocusChange(bool focusGained)
	{
		EngineScreenManager.OnGameWindowFocusChange(focusGained);
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_OnOnscreenKeyboardCanceled_delegate))]
	internal static void EngineScreenManager_OnOnscreenKeyboardCanceled()
	{
		EngineScreenManager.OnOnscreenKeyboardCanceled();
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_OnOnscreenKeyboardDone_delegate))]
	internal static void EngineScreenManager_OnOnscreenKeyboardDone(IntPtr inputText)
	{
		EngineScreenManager.OnOnscreenKeyboardDone(Marshal.PtrToStringAnsi(inputText));
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_PreTick_delegate))]
	internal static void EngineScreenManager_PreTick(float dt)
	{
		EngineScreenManager.PreTick(dt);
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_Tick_delegate))]
	internal static void EngineScreenManager_Tick(float dt)
	{
		EngineScreenManager.Tick(dt);
	}

	[MonoPInvokeCallback(typeof(EngineScreenManager_Update_delegate))]
	internal static void EngineScreenManager_Update()
	{
		EngineScreenManager.Update();
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_CollectCommandLineFunctions_delegate))]
	internal static void ManagedExtensions_CollectCommandLineFunctions()
	{
		ManagedExtensions.CollectCommandLineFunctions();
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_CopyObjectFieldsFrom_delegate))]
	internal static void ManagedExtensions_CopyObjectFieldsFrom(int dst, int src, IntPtr className, int callFieldChangeEventAsInteger)
	{
		DotNetObject managedObjectWithId = DotNetObject.GetManagedObjectWithId(dst);
		DotNetObject managedObjectWithId2 = DotNetObject.GetManagedObjectWithId(src);
		string className2 = Marshal.PtrToStringAnsi(className);
		ManagedExtensions.CopyObjectFieldsFrom(managedObjectWithId, managedObjectWithId2, className2, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_CreateScriptComponentInstance_delegate))]
	internal static int ManagedExtensions_CreateScriptComponentInstance(IntPtr className, UIntPtr entityPtr, NativeObjectPointer managedScriptComponent)
	{
		string? className2 = Marshal.PtrToStringAnsi(className);
		ManagedScriptComponent managedScriptComponent2 = null;
		if (managedScriptComponent.Pointer != UIntPtr.Zero)
		{
			managedScriptComponent2 = new ManagedScriptComponent(managedScriptComponent.Pointer);
		}
		return ManagedExtensions.CreateScriptComponentInstance(className2, entityPtr, managedScriptComponent2).GetManagedId();
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_ForceGarbageCollect_delegate))]
	internal static void ManagedExtensions_ForceGarbageCollect()
	{
		ManagedExtensions.ForceGarbageCollect();
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_GetEditorVisibilityOfField_delegate))]
	internal static bool ManagedExtensions_GetEditorVisibilityOfField(uint classNameHash, uint fieldNamehash)
	{
		return ManagedExtensions.GetEditorVisibilityOfField(classNameHash, fieldNamehash);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_GetObjectField_delegate))]
	internal static void ManagedExtensions_GetObjectField(int managedObject, uint classNameHash, ref ScriptComponentFieldHolder scriptComponentFieldHolder, uint fieldNameHash, RglScriptFieldType type)
	{
		ManagedExtensions.GetObjectField(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, ref scriptComponentFieldHolder, fieldNameHash, type);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_GetScriptComponentClassNames_delegate))]
	internal static UIntPtr ManagedExtensions_GetScriptComponentClassNames()
	{
		string scriptComponentClassNames = ManagedExtensions.GetScriptComponentClassNames();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, scriptComponentClassNames);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_GetTypeOfField_delegate))]
	internal static RglScriptFieldType ManagedExtensions_GetTypeOfField(uint classNameHash, uint fieldNameHash)
	{
		return ManagedExtensions.GetTypeOfField(classNameHash, fieldNameHash);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldBool_delegate))]
	internal static void ManagedExtensions_SetObjectFieldBool(int managedObject, uint classNameHash, uint fieldNameHash, bool value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldBool(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldColor_delegate))]
	internal static void ManagedExtensions_SetObjectFieldColor(int managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldColor(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldDouble_delegate))]
	internal static void ManagedExtensions_SetObjectFieldDouble(int managedObject, uint classNameHash, uint fieldNameHash, double value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldDouble(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldEntity_delegate))]
	internal static void ManagedExtensions_SetObjectFieldEntity(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldEntity(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldEnum_delegate))]
	internal static void ManagedExtensions_SetObjectFieldEnum(int managedObject, uint classNameHash, uint fieldNameHash, IntPtr value, int callFieldChangeEventAsInteger)
	{
		DotNetObject managedObjectWithId = DotNetObject.GetManagedObjectWithId(managedObject);
		string value2 = Marshal.PtrToStringAnsi(value);
		ManagedExtensions.SetObjectFieldEnum(managedObjectWithId, classNameHash, fieldNameHash, value2, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldFloat_delegate))]
	internal static void ManagedExtensions_SetObjectFieldFloat(int managedObject, uint classNameHash, uint fieldNameHash, float value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldFloat(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldInt_delegate))]
	internal static void ManagedExtensions_SetObjectFieldInt(int managedObject, uint classNameHash, uint fieldNameHash, int value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldInt(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldMaterial_delegate))]
	internal static void ManagedExtensions_SetObjectFieldMaterial(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldMaterial(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldMatrixFrame_delegate))]
	internal static void ManagedExtensions_SetObjectFieldMatrixFrame(int managedObject, uint classNameHash, uint fieldNameHash, MatrixFrame value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldMatrixFrame(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldMesh_delegate))]
	internal static void ManagedExtensions_SetObjectFieldMesh(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldMesh(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldString_delegate))]
	internal static void ManagedExtensions_SetObjectFieldString(int managedObject, uint classNameHash, uint fieldNameHash, IntPtr value, int callFieldChangeEventAsInteger)
	{
		DotNetObject managedObjectWithId = DotNetObject.GetManagedObjectWithId(managedObject);
		string value2 = Marshal.PtrToStringAnsi(value);
		ManagedExtensions.SetObjectFieldString(managedObjectWithId, classNameHash, fieldNameHash, value2, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldTexture_delegate))]
	internal static void ManagedExtensions_SetObjectFieldTexture(int managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldTexture(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedExtensions_SetObjectFieldVec3_delegate))]
	internal static void ManagedExtensions_SetObjectFieldVec3(int managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger)
	{
		ManagedExtensions.SetObjectFieldVec3(DotNetObject.GetManagedObjectWithId(managedObject), classNameHash, fieldNameHash, value, callFieldChangeEventAsInteger);
	}

	[MonoPInvokeCallback(typeof(ManagedScriptHolder_CreateManagedScriptHolder_delegate))]
	internal static int ManagedScriptHolder_CreateManagedScriptHolder()
	{
		return ManagedScriptHolder.CreateManagedScriptHolder().GetManagedId();
	}

	[MonoPInvokeCallback(typeof(ManagedScriptHolder_FixedTickComponents_delegate))]
	internal static void ManagedScriptHolder_FixedTickComponents(int thisPointer, float fixedDt)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedScriptHolder).FixedTickComponents(fixedDt);
	}

	[MonoPInvokeCallback(typeof(ManagedScriptHolder_GetNumberOfScripts_delegate))]
	internal static int ManagedScriptHolder_GetNumberOfScripts(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedScriptHolder).GetNumberOfScripts();
	}

	[MonoPInvokeCallback(typeof(ManagedScriptHolder_RemoveScriptComponentFromAllTickLists_delegate))]
	internal static void ManagedScriptHolder_RemoveScriptComponentFromAllTickLists(int thisPointer, int sc)
	{
		ManagedScriptHolder obj = DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedScriptHolder;
		ScriptComponentBehavior sc2 = DotNetObject.GetManagedObjectWithId(sc) as ScriptComponentBehavior;
		obj.RemoveScriptComponentFromAllTickLists(sc2);
	}

	[MonoPInvokeCallback(typeof(ManagedScriptHolder_SetScriptComponentHolder_delegate))]
	internal static void ManagedScriptHolder_SetScriptComponentHolder(int thisPointer, int sc)
	{
		ManagedScriptHolder obj = DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedScriptHolder;
		ScriptComponentBehavior scriptComponentHolder = DotNetObject.GetManagedObjectWithId(sc) as ScriptComponentBehavior;
		obj.SetScriptComponentHolder(scriptComponentHolder);
	}

	[MonoPInvokeCallback(typeof(ManagedScriptHolder_TickComponents_delegate))]
	internal static void ManagedScriptHolder_TickComponents(int thisPointer, float dt)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedScriptHolder).TickComponents(dt);
	}

	[MonoPInvokeCallback(typeof(ManagedScriptHolder_TickComponentsEditor_delegate))]
	internal static void ManagedScriptHolder_TickComponentsEditor(int thisPointer, float dt)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedScriptHolder).TickComponentsEditor(dt);
	}

	[MonoPInvokeCallback(typeof(MessageManagerBase_PostMessageLine_delegate))]
	internal static void MessageManagerBase_PostMessageLine(int thisPointer, IntPtr text, uint color)
	{
		MessageManagerBase obj = DotNetObject.GetManagedObjectWithId(thisPointer) as MessageManagerBase;
		string text2 = Marshal.PtrToStringAnsi(text);
		obj.PostMessageLine(text2, color);
	}

	[MonoPInvokeCallback(typeof(MessageManagerBase_PostMessageLineFormatted_delegate))]
	internal static void MessageManagerBase_PostMessageLineFormatted(int thisPointer, IntPtr text, uint color)
	{
		MessageManagerBase obj = DotNetObject.GetManagedObjectWithId(thisPointer) as MessageManagerBase;
		string text2 = Marshal.PtrToStringAnsi(text);
		obj.PostMessageLineFormatted(text2, color);
	}

	[MonoPInvokeCallback(typeof(MessageManagerBase_PostSuccessLine_delegate))]
	internal static void MessageManagerBase_PostSuccessLine(int thisPointer, IntPtr text)
	{
		MessageManagerBase obj = DotNetObject.GetManagedObjectWithId(thisPointer) as MessageManagerBase;
		string text2 = Marshal.PtrToStringAnsi(text);
		obj.PostSuccessLine(text2);
	}

	[MonoPInvokeCallback(typeof(MessageManagerBase_PostWarningLine_delegate))]
	internal static void MessageManagerBase_PostWarningLine(int thisPointer, IntPtr text)
	{
		MessageManagerBase obj = DotNetObject.GetManagedObjectWithId(thisPointer) as MessageManagerBase;
		string text2 = Marshal.PtrToStringAnsi(text);
		obj.PostWarningLine(text2);
	}

	[MonoPInvokeCallback(typeof(NativeParallelDriver_ParalelForLoopBodyCaller_delegate))]
	internal static void NativeParallelDriver_ParalelForLoopBodyCaller(long loopBodyKey, int localStartIndex, int localEndIndex)
	{
		NativeParallelDriver.ParalelForLoopBodyCaller(loopBodyKey, localStartIndex, localEndIndex);
	}

	[MonoPInvokeCallback(typeof(NativeParallelDriver_ParalelForLoopBodyWithDtCaller_delegate))]
	internal static void NativeParallelDriver_ParalelForLoopBodyWithDtCaller(long loopBodyKey, int localStartIndex, int localEndIndex)
	{
		NativeParallelDriver.ParalelForLoopBodyWithDtCaller(loopBodyKey, localStartIndex, localEndIndex);
	}

	[MonoPInvokeCallback(typeof(RenderTargetComponent_CreateRenderTargetComponent_delegate))]
	internal static int RenderTargetComponent_CreateRenderTargetComponent(NativeObjectPointer renderTarget)
	{
		Texture renderTarget2 = null;
		if (renderTarget.Pointer != UIntPtr.Zero)
		{
			renderTarget2 = new Texture(renderTarget.Pointer);
		}
		return RenderTargetComponent.CreateRenderTargetComponent(renderTarget2).GetManagedId();
	}

	[MonoPInvokeCallback(typeof(RenderTargetComponent_OnPaintNeeded_delegate))]
	internal static void RenderTargetComponent_OnPaintNeeded(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as RenderTargetComponent).OnPaintNeeded();
	}

	[MonoPInvokeCallback(typeof(SceneProblemChecker_OnCheckForSceneProblems_delegate))]
	internal static bool SceneProblemChecker_OnCheckForSceneProblems(NativeObjectPointer scene)
	{
		Scene scene2 = null;
		if (scene.Pointer != UIntPtr.Zero)
		{
			scene2 = new Scene(scene.Pointer);
		}
		return SceneProblemChecker.OnCheckForSceneProblems(scene2);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_AddScriptComponentToTick_delegate))]
	internal static void ScriptComponentBehavior_AddScriptComponentToTick(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).AddScriptComponentToTick();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_DeregisterAsPrefabScriptComponent_delegate))]
	internal static void ScriptComponentBehavior_DeregisterAsPrefabScriptComponent(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).DeregisterAsPrefabScriptComponent();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_DeregisterAsUndoStackScriptComponent_delegate))]
	internal static void ScriptComponentBehavior_DeregisterAsUndoStackScriptComponent(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).DeregisterAsUndoStackScriptComponent();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_DisablesOroCreation_delegate))]
	internal static bool ScriptComponentBehavior_DisablesOroCreation(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).DisablesOroCreation();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_GetEditableFields_delegate))]
	internal static int ScriptComponentBehavior_GetEditableFields(IntPtr className)
	{
		return Managed.AddCustomParameter(ScriptComponentBehavior.GetEditableFields(Marshal.PtrToStringAnsi(className))).GetManagedId();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_HandleOnRemoved_delegate))]
	internal static void ScriptComponentBehavior_HandleOnRemoved(int thisPointer, int removeReason)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).HandleOnRemoved(removeReason);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_IsOnlyVisual_delegate))]
	internal static bool ScriptComponentBehavior_IsOnlyVisual(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).IsOnlyVisual();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_MovesEntity_delegate))]
	internal static bool ScriptComponentBehavior_MovesEntity(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).MovesEntity();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnBoundingBoxValidate_delegate))]
	internal static void ScriptComponentBehavior_OnBoundingBoxValidate(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnBoundingBoxValidate();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnCheckForProblems_delegate))]
	internal static bool ScriptComponentBehavior_OnCheckForProblems(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnCheckForProblems();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnDynamicNavmeshVertexUpdate_delegate))]
	internal static void ScriptComponentBehavior_OnDynamicNavmeshVertexUpdate(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnDynamicNavmeshVertexUpdate();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnEditModeVisibilityChanged_delegate))]
	internal static void ScriptComponentBehavior_OnEditModeVisibilityChanged(int thisPointer, bool currentVisibility)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnEditModeVisibilityChanged(currentVisibility);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnEditorInit_delegate))]
	internal static void ScriptComponentBehavior_OnEditorInit(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnEditorInit();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnEditorTick_delegate))]
	internal static void ScriptComponentBehavior_OnEditorTick(int thisPointer, float dt)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnEditorTick(dt);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnEditorValidate_delegate))]
	internal static void ScriptComponentBehavior_OnEditorValidate(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnEditorValidate();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnEditorVariableChanged_delegate))]
	internal static void ScriptComponentBehavior_OnEditorVariableChanged(int thisPointer, IntPtr variableName)
	{
		ScriptComponentBehavior obj = DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior;
		string variableName2 = Marshal.PtrToStringAnsi(variableName);
		obj.OnEditorVariableChanged(variableName2);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnInit_delegate))]
	internal static void ScriptComponentBehavior_OnInit(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnInit();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnPhysicsCollisionAux_delegate))]
	internal static void ScriptComponentBehavior_OnPhysicsCollisionAux(int thisPointer, ref PhysicsContact contact, UIntPtr entity0, UIntPtr entity1)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnPhysicsCollisionAux(ref contact, entity0, entity1);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnPreInit_delegate))]
	internal static void ScriptComponentBehavior_OnPreInit(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnPreInit();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnSaveAsPrefab_delegate))]
	internal static void ScriptComponentBehavior_OnSaveAsPrefab(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnSaveAsPrefab();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnSceneSave_delegate))]
	internal static void ScriptComponentBehavior_OnSceneSave(int thisPointer, IntPtr saveFolder)
	{
		ScriptComponentBehavior obj = DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior;
		string saveFolder2 = Marshal.PtrToStringAnsi(saveFolder);
		obj.OnSceneSave(saveFolder2);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_OnTerrainReload_delegate))]
	internal static void ScriptComponentBehavior_OnTerrainReload(int thisPointer, int step)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).OnTerrainReload(step);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_RegisterAsPrefabScriptComponent_delegate))]
	internal static void ScriptComponentBehavior_RegisterAsPrefabScriptComponent(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).RegisterAsPrefabScriptComponent();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_RegisterAsUndoStackScriptComponent_delegate))]
	internal static void ScriptComponentBehavior_RegisterAsUndoStackScriptComponent(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior).RegisterAsUndoStackScriptComponent();
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_SetScene_delegate))]
	internal static void ScriptComponentBehavior_SetScene(int thisPointer, NativeObjectPointer scene)
	{
		ScriptComponentBehavior obj = DotNetObject.GetManagedObjectWithId(thisPointer) as ScriptComponentBehavior;
		Scene scene2 = null;
		if (scene.Pointer != UIntPtr.Zero)
		{
			scene2 = new Scene(scene.Pointer);
		}
		obj.SetScene(scene2);
	}

	[MonoPInvokeCallback(typeof(ScriptComponentBehavior_SkeletonPostIntegrateCallbackAux_delegate))]
	internal static bool ScriptComponentBehavior_SkeletonPostIntegrateCallbackAux(int script, UIntPtr animResultPointer)
	{
		return ScriptComponentBehavior.SkeletonPostIntegrateCallbackAux(DotNetObject.GetManagedObjectWithId(script) as ScriptComponentBehavior, animResultPointer);
	}

	[MonoPInvokeCallback(typeof(ThumbnailCreatorView_OnThumbnailRenderComplete_delegate))]
	internal static void ThumbnailCreatorView_OnThumbnailRenderComplete(IntPtr renderId, NativeObjectPointer renderTarget)
	{
		string? renderId2 = Marshal.PtrToStringAnsi(renderId);
		Texture renderTarget2 = null;
		if (renderTarget.Pointer != UIntPtr.Zero)
		{
			renderTarget2 = new Texture(renderTarget.Pointer);
		}
		ThumbnailCreatorView.OnThumbnailRenderComplete(renderId2, renderTarget2);
	}
}
