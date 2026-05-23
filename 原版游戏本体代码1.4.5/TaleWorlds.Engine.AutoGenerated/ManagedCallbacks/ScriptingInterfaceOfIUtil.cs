using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIUtil : IUtil
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddCommandLineFunctionDelegate(byte[] concatName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMainThreadPerformanceQueryDelegate(byte[] parent, byte[] name, float seconds);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPerformanceReportTokenDelegate(byte[] performance_type, byte[] name, float loading_time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddSceneObjectReportDelegate(byte[] scene_name, byte[] report_name, float report_value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CheckIfAssetsAndSourcesAreSameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckIfTerrainShaderHeaderGenerationFinishedDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CheckResourceModificationsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CheckSceneForProblemsDelegate(byte[] path);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckShaderCompilationDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void clear_decal_atlasDelegate(DecalAtlasGroup atlasGroup);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearOldResourcesAndObjectsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearShaderMemoryDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CommandLineArgumentExistsDelegate(byte[] str);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CompileAllShadersDelegate(byte[] targetPlatform);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CompileTerrainShadersDistDelegate(byte[] targetPlatform, byte[] targetConfig, byte[] output_path);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CreateSelectionInEditorDelegate(IntPtr gameEntities, int entityCount, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DebugSetGlobalLoadingWindowStateDelegate([MarshalAs(UnmanagedType.U1)] bool s);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeleteEntitiesInEditorSceneDelegate(IntPtr gameEntities, int entityCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DetachWatchdogDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool DidAutomatedGIBakeFinishedDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisableCoreGameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisableGlobalEditDataCacherDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisableGlobalLoadingWindowDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DoDelayedexitDelegate(int returnCode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DoFullBakeAllLevelsAutomatedDelegate(byte[] module, byte[] sceneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DoFullBakeSingleLevelAutomatedDelegate(byte[] module, byte[] sceneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DoLightOnlyBakeAllLevelsAutomatedDelegate(byte[] module, byte[] sceneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DoLightOnlyBakeSingleLevelAutomatedDelegate(byte[] module, byte[] sceneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DumpGPUMemoryStatisticsDelegate(byte[] filePath);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnableGlobalEditDataCacherDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnableGlobalLoadingWindowDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnableSingleGPUQueryPerFrameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndLoadingStuckCheckStateDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int ExecuteCommandLineCommandDelegate(byte[] command);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ExitProcessDelegate(int exitCode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int ExportNavMeshFaceMarksDelegate(byte[] file_name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FindMeshesWithoutLodsDelegate(byte[] module_name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FlushManagedObjectsMemoryDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GatherCoreGameReferencesDelegate(byte[] scene_names);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GenerateTerrainShaderHeadersDelegate(byte[] targetPlatform, byte[] targetConfig, byte[] output_path);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetApplicationMemoryDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetApplicationMemoryStatisticsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetApplicationNameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAttachmentsPathDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetBaseDirectoryDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetBenchmarkStatusDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetBuildNumberDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetConsoleHostMachineDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetCoreGameStateDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate ulong GetCurrentCpuMemoryUsageDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetCurrentEstimatedGPUMemoryCostMBDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetCurrentProcessIDDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate ulong GetCurrentThreadIdDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetDeltaTimeDelegate(int timerId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetDetailedGPUBufferMemoryStatsDelegate(ref int totalMemoryAllocated, ref int totalMemoryUsed, ref int emptyChunkCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDetailedXBOXMemoryInfoDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetEditorSelectedEntitiesDelegate(IntPtr gameEntitiesTemp);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetEditorSelectedEntityCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetEngineFrameNoDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetEntitiesOfSelectionSetDelegate(byte[] name, IntPtr gameEntitiesTemp);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetEntityCountOfSelectionSetDelegate(byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetExecutableWorkingDirectoryDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetFpsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetFrameLimiterWithSleepDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFullCommandLineStringDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFullFilePathOfSceneDelegate(byte[] sceneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFullModulePathDelegate(byte[] moduleName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFullModulePathsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetGPUMemoryMBDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate ulong GetGpuMemoryOfAllocationGroupDelegate(byte[] allocationName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetGPUMemoryStatsDelegate(ref float totalMemory, ref float renderTargetMemory, ref float depthTargetMemory, ref float srvMemory, ref float bufferMemory);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetLocalOutputPathDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMainFpsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate ulong GetMainThreadIdDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMemoryUsageOfCategoryDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetModulesCodeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNativeMemoryStatisticsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfShaderCompilationsInProgressDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetPCInfoDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetPlatformModulePathsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetPossibleCommandLineStartingWithDelegate(byte[] command, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRendererFpsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetReturnCodeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSingleModuleScenesOfModuleDelegate(byte[] moduleName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSteamAppIdDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSystemLanguageDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVertexBufferChunkSystemMemoryUsageDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVisualTestsTestFilesPathDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVisualTestsValidatePathDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsAsyncPhysicsThreadDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsBenchmarkQuitedDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int IsDetailedSoundLogOnDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsDevkitDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsEditModeEnabledDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsLockhartPlatformDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsSceneReportFinishedDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LoadSkyBoxesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LoadVirtualTextureTilesetDelegate(byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ManagedParallelForDelegate(int fromInclusive, int toExclusive, long curKey, int grainSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ManagedParallelForWithDtDelegate(int fromInclusive, int toExclusive, long curKey, int grainSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ManagedParallelForWithoutRenderThreadDelegate(int fromInclusive, int toExclusive, long curKey, int grainSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ManagedParallelForWithoutRenderThreadDtDelegate(int fromInclusive, int toExclusive, long curKey, int grainSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OnLoadingWindowDisabledDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OnLoadingWindowEnabledDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OpenConsoleStorePageDelegate(byte[] productId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OpenOnscreenKeyboardDelegate(byte[] initialText, byte[] descriptionText, int maxLength, int keyboardTypeEnum);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OutputBenchmarkValuesToPerformanceReporterDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OutputPerformanceReportsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PairSceneNameToModuleNameDelegate(byte[] sceneName, byte[] moduleName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int ProcessWindowTitleDelegate(byte[] title);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void QuitGameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int RegisterGPUAllocationGroupDelegate(byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RegisterMeshForGPUMorphDelegate(byte[] metaMeshName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int SaveDataAsTextureDelegate(byte[] path, int width, int height, IntPtr data);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SelectEntitiesDelegate(IntPtr gameEntities, int entityCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAllocationAlwaysValidSceneDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAssertionAtShaderCompileDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAssertionsAndWarningsSetExitCodeDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBenchmarkStatusDelegate(int status, byte[] def);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCanLoadModulesDelegate([MarshalAs(UnmanagedType.U1)] bool canLoadModules);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCoreGameStateDelegate(int state);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCrashOnAssertsDelegate([MarshalAs(UnmanagedType.U1)] bool val);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCrashOnWarningsDelegate([MarshalAs(UnmanagedType.U1)] bool val);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCrashReportCustomStackDelegate(byte[] customStack);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCrashReportCustomStringDelegate(byte[] customString);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCreateDumpOnWarningsDelegate([MarshalAs(UnmanagedType.U1)] bool val);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDisableDumpGenerationDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDumpFolderPathDelegate(byte[] path);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFixedDtDelegate([MarshalAs(UnmanagedType.U1)] bool enabled, float dt);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetForceDrawEntityIDDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetForceVsyncDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFrameLimiterWithSleepDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetGraphicsPresetDelegate(int preset);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLoadingScreenPercentageDelegate(float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMessageLineRenderingStateDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPrintCallstackAtCrahsesDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRenderAgentsDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRenderModeDelegate(int mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetReportModeDelegate([MarshalAs(UnmanagedType.U1)] bool reportMode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetScreenTextRenderingStateDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetWatchdogAutoreportDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetWatchdogValueDelegate(byte[] fileName, byte[] groupName, byte[] key, byte[] value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetWindowTitleDelegate(byte[] title);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StartLoadingStuckCheckStateDelegate(float seconds);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StartScenePerformanceReportDelegate(byte[] folderPath);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TakeScreenshotFromPlatformPathDelegate(PlatformFilePath path);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TakeScreenshotFromStringPathDelegate(byte[] path);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int TakeSSFromTopDelegate(byte[] file_name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ToggleRenderDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddCommandLineFunctionDelegate call_AddCommandLineFunctionDelegate;

	public static AddMainThreadPerformanceQueryDelegate call_AddMainThreadPerformanceQueryDelegate;

	public static AddPerformanceReportTokenDelegate call_AddPerformanceReportTokenDelegate;

	public static AddSceneObjectReportDelegate call_AddSceneObjectReportDelegate;

	public static CheckIfAssetsAndSourcesAreSameDelegate call_CheckIfAssetsAndSourcesAreSameDelegate;

	public static CheckIfTerrainShaderHeaderGenerationFinishedDelegate call_CheckIfTerrainShaderHeaderGenerationFinishedDelegate;

	public static CheckResourceModificationsDelegate call_CheckResourceModificationsDelegate;

	public static CheckSceneForProblemsDelegate call_CheckSceneForProblemsDelegate;

	public static CheckShaderCompilationDelegate call_CheckShaderCompilationDelegate;

	public static clear_decal_atlasDelegate call_clear_decal_atlasDelegate;

	public static ClearOldResourcesAndObjectsDelegate call_ClearOldResourcesAndObjectsDelegate;

	public static ClearShaderMemoryDelegate call_ClearShaderMemoryDelegate;

	public static CommandLineArgumentExistsDelegate call_CommandLineArgumentExistsDelegate;

	public static CompileAllShadersDelegate call_CompileAllShadersDelegate;

	public static CompileTerrainShadersDistDelegate call_CompileTerrainShadersDistDelegate;

	public static CreateSelectionInEditorDelegate call_CreateSelectionInEditorDelegate;

	public static DebugSetGlobalLoadingWindowStateDelegate call_DebugSetGlobalLoadingWindowStateDelegate;

	public static DeleteEntitiesInEditorSceneDelegate call_DeleteEntitiesInEditorSceneDelegate;

	public static DetachWatchdogDelegate call_DetachWatchdogDelegate;

	public static DidAutomatedGIBakeFinishedDelegate call_DidAutomatedGIBakeFinishedDelegate;

	public static DisableCoreGameDelegate call_DisableCoreGameDelegate;

	public static DisableGlobalEditDataCacherDelegate call_DisableGlobalEditDataCacherDelegate;

	public static DisableGlobalLoadingWindowDelegate call_DisableGlobalLoadingWindowDelegate;

	public static DoDelayedexitDelegate call_DoDelayedexitDelegate;

	public static DoFullBakeAllLevelsAutomatedDelegate call_DoFullBakeAllLevelsAutomatedDelegate;

	public static DoFullBakeSingleLevelAutomatedDelegate call_DoFullBakeSingleLevelAutomatedDelegate;

	public static DoLightOnlyBakeAllLevelsAutomatedDelegate call_DoLightOnlyBakeAllLevelsAutomatedDelegate;

	public static DoLightOnlyBakeSingleLevelAutomatedDelegate call_DoLightOnlyBakeSingleLevelAutomatedDelegate;

	public static DumpGPUMemoryStatisticsDelegate call_DumpGPUMemoryStatisticsDelegate;

	public static EnableGlobalEditDataCacherDelegate call_EnableGlobalEditDataCacherDelegate;

	public static EnableGlobalLoadingWindowDelegate call_EnableGlobalLoadingWindowDelegate;

	public static EnableSingleGPUQueryPerFrameDelegate call_EnableSingleGPUQueryPerFrameDelegate;

	public static EndLoadingStuckCheckStateDelegate call_EndLoadingStuckCheckStateDelegate;

	public static ExecuteCommandLineCommandDelegate call_ExecuteCommandLineCommandDelegate;

	public static ExitProcessDelegate call_ExitProcessDelegate;

	public static ExportNavMeshFaceMarksDelegate call_ExportNavMeshFaceMarksDelegate;

	public static FindMeshesWithoutLodsDelegate call_FindMeshesWithoutLodsDelegate;

	public static FlushManagedObjectsMemoryDelegate call_FlushManagedObjectsMemoryDelegate;

	public static GatherCoreGameReferencesDelegate call_GatherCoreGameReferencesDelegate;

	public static GenerateTerrainShaderHeadersDelegate call_GenerateTerrainShaderHeadersDelegate;

	public static GetApplicationMemoryDelegate call_GetApplicationMemoryDelegate;

	public static GetApplicationMemoryStatisticsDelegate call_GetApplicationMemoryStatisticsDelegate;

	public static GetApplicationNameDelegate call_GetApplicationNameDelegate;

	public static GetAttachmentsPathDelegate call_GetAttachmentsPathDelegate;

	public static GetBaseDirectoryDelegate call_GetBaseDirectoryDelegate;

	public static GetBenchmarkStatusDelegate call_GetBenchmarkStatusDelegate;

	public static GetBuildNumberDelegate call_GetBuildNumberDelegate;

	public static GetConsoleHostMachineDelegate call_GetConsoleHostMachineDelegate;

	public static GetCoreGameStateDelegate call_GetCoreGameStateDelegate;

	public static GetCurrentCpuMemoryUsageDelegate call_GetCurrentCpuMemoryUsageDelegate;

	public static GetCurrentEstimatedGPUMemoryCostMBDelegate call_GetCurrentEstimatedGPUMemoryCostMBDelegate;

	public static GetCurrentProcessIDDelegate call_GetCurrentProcessIDDelegate;

	public static GetCurrentThreadIdDelegate call_GetCurrentThreadIdDelegate;

	public static GetDeltaTimeDelegate call_GetDeltaTimeDelegate;

	public static GetDetailedGPUBufferMemoryStatsDelegate call_GetDetailedGPUBufferMemoryStatsDelegate;

	public static GetDetailedXBOXMemoryInfoDelegate call_GetDetailedXBOXMemoryInfoDelegate;

	public static GetEditorSelectedEntitiesDelegate call_GetEditorSelectedEntitiesDelegate;

	public static GetEditorSelectedEntityCountDelegate call_GetEditorSelectedEntityCountDelegate;

	public static GetEngineFrameNoDelegate call_GetEngineFrameNoDelegate;

	public static GetEntitiesOfSelectionSetDelegate call_GetEntitiesOfSelectionSetDelegate;

	public static GetEntityCountOfSelectionSetDelegate call_GetEntityCountOfSelectionSetDelegate;

	public static GetExecutableWorkingDirectoryDelegate call_GetExecutableWorkingDirectoryDelegate;

	public static GetFpsDelegate call_GetFpsDelegate;

	public static GetFrameLimiterWithSleepDelegate call_GetFrameLimiterWithSleepDelegate;

	public static GetFullCommandLineStringDelegate call_GetFullCommandLineStringDelegate;

	public static GetFullFilePathOfSceneDelegate call_GetFullFilePathOfSceneDelegate;

	public static GetFullModulePathDelegate call_GetFullModulePathDelegate;

	public static GetFullModulePathsDelegate call_GetFullModulePathsDelegate;

	public static GetGPUMemoryMBDelegate call_GetGPUMemoryMBDelegate;

	public static GetGpuMemoryOfAllocationGroupDelegate call_GetGpuMemoryOfAllocationGroupDelegate;

	public static GetGPUMemoryStatsDelegate call_GetGPUMemoryStatsDelegate;

	public static GetLocalOutputPathDelegate call_GetLocalOutputPathDelegate;

	public static GetMainFpsDelegate call_GetMainFpsDelegate;

	public static GetMainThreadIdDelegate call_GetMainThreadIdDelegate;

	public static GetMemoryUsageOfCategoryDelegate call_GetMemoryUsageOfCategoryDelegate;

	public static GetModulesCodeDelegate call_GetModulesCodeDelegate;

	public static GetNativeMemoryStatisticsDelegate call_GetNativeMemoryStatisticsDelegate;

	public static GetNumberOfShaderCompilationsInProgressDelegate call_GetNumberOfShaderCompilationsInProgressDelegate;

	public static GetPCInfoDelegate call_GetPCInfoDelegate;

	public static GetPlatformModulePathsDelegate call_GetPlatformModulePathsDelegate;

	public static GetPossibleCommandLineStartingWithDelegate call_GetPossibleCommandLineStartingWithDelegate;

	public static GetRendererFpsDelegate call_GetRendererFpsDelegate;

	public static GetReturnCodeDelegate call_GetReturnCodeDelegate;

	public static GetSingleModuleScenesOfModuleDelegate call_GetSingleModuleScenesOfModuleDelegate;

	public static GetSteamAppIdDelegate call_GetSteamAppIdDelegate;

	public static GetSystemLanguageDelegate call_GetSystemLanguageDelegate;

	public static GetVertexBufferChunkSystemMemoryUsageDelegate call_GetVertexBufferChunkSystemMemoryUsageDelegate;

	public static GetVisualTestsTestFilesPathDelegate call_GetVisualTestsTestFilesPathDelegate;

	public static GetVisualTestsValidatePathDelegate call_GetVisualTestsValidatePathDelegate;

	public static IsAsyncPhysicsThreadDelegate call_IsAsyncPhysicsThreadDelegate;

	public static IsBenchmarkQuitedDelegate call_IsBenchmarkQuitedDelegate;

	public static IsDetailedSoundLogOnDelegate call_IsDetailedSoundLogOnDelegate;

	public static IsDevkitDelegate call_IsDevkitDelegate;

	public static IsEditModeEnabledDelegate call_IsEditModeEnabledDelegate;

	public static IsLockhartPlatformDelegate call_IsLockhartPlatformDelegate;

	public static IsSceneReportFinishedDelegate call_IsSceneReportFinishedDelegate;

	public static LoadSkyBoxesDelegate call_LoadSkyBoxesDelegate;

	public static LoadVirtualTextureTilesetDelegate call_LoadVirtualTextureTilesetDelegate;

	public static ManagedParallelForDelegate call_ManagedParallelForDelegate;

	public static ManagedParallelForWithDtDelegate call_ManagedParallelForWithDtDelegate;

	public static ManagedParallelForWithoutRenderThreadDelegate call_ManagedParallelForWithoutRenderThreadDelegate;

	public static ManagedParallelForWithoutRenderThreadDtDelegate call_ManagedParallelForWithoutRenderThreadDtDelegate;

	public static OnLoadingWindowDisabledDelegate call_OnLoadingWindowDisabledDelegate;

	public static OnLoadingWindowEnabledDelegate call_OnLoadingWindowEnabledDelegate;

	public static OpenConsoleStorePageDelegate call_OpenConsoleStorePageDelegate;

	public static OpenOnscreenKeyboardDelegate call_OpenOnscreenKeyboardDelegate;

	public static OutputBenchmarkValuesToPerformanceReporterDelegate call_OutputBenchmarkValuesToPerformanceReporterDelegate;

	public static OutputPerformanceReportsDelegate call_OutputPerformanceReportsDelegate;

	public static PairSceneNameToModuleNameDelegate call_PairSceneNameToModuleNameDelegate;

	public static ProcessWindowTitleDelegate call_ProcessWindowTitleDelegate;

	public static QuitGameDelegate call_QuitGameDelegate;

	public static RegisterGPUAllocationGroupDelegate call_RegisterGPUAllocationGroupDelegate;

	public static RegisterMeshForGPUMorphDelegate call_RegisterMeshForGPUMorphDelegate;

	public static SaveDataAsTextureDelegate call_SaveDataAsTextureDelegate;

	public static SelectEntitiesDelegate call_SelectEntitiesDelegate;

	public static SetAllocationAlwaysValidSceneDelegate call_SetAllocationAlwaysValidSceneDelegate;

	public static SetAssertionAtShaderCompileDelegate call_SetAssertionAtShaderCompileDelegate;

	public static SetAssertionsAndWarningsSetExitCodeDelegate call_SetAssertionsAndWarningsSetExitCodeDelegate;

	public static SetBenchmarkStatusDelegate call_SetBenchmarkStatusDelegate;

	public static SetCanLoadModulesDelegate call_SetCanLoadModulesDelegate;

	public static SetCoreGameStateDelegate call_SetCoreGameStateDelegate;

	public static SetCrashOnAssertsDelegate call_SetCrashOnAssertsDelegate;

	public static SetCrashOnWarningsDelegate call_SetCrashOnWarningsDelegate;

	public static SetCrashReportCustomStackDelegate call_SetCrashReportCustomStackDelegate;

	public static SetCrashReportCustomStringDelegate call_SetCrashReportCustomStringDelegate;

	public static SetCreateDumpOnWarningsDelegate call_SetCreateDumpOnWarningsDelegate;

	public static SetDisableDumpGenerationDelegate call_SetDisableDumpGenerationDelegate;

	public static SetDumpFolderPathDelegate call_SetDumpFolderPathDelegate;

	public static SetFixedDtDelegate call_SetFixedDtDelegate;

	public static SetForceDrawEntityIDDelegate call_SetForceDrawEntityIDDelegate;

	public static SetForceVsyncDelegate call_SetForceVsyncDelegate;

	public static SetFrameLimiterWithSleepDelegate call_SetFrameLimiterWithSleepDelegate;

	public static SetGraphicsPresetDelegate call_SetGraphicsPresetDelegate;

	public static SetLoadingScreenPercentageDelegate call_SetLoadingScreenPercentageDelegate;

	public static SetMessageLineRenderingStateDelegate call_SetMessageLineRenderingStateDelegate;

	public static SetPrintCallstackAtCrahsesDelegate call_SetPrintCallstackAtCrahsesDelegate;

	public static SetRenderAgentsDelegate call_SetRenderAgentsDelegate;

	public static SetRenderModeDelegate call_SetRenderModeDelegate;

	public static SetReportModeDelegate call_SetReportModeDelegate;

	public static SetScreenTextRenderingStateDelegate call_SetScreenTextRenderingStateDelegate;

	public static SetWatchdogAutoreportDelegate call_SetWatchdogAutoreportDelegate;

	public static SetWatchdogValueDelegate call_SetWatchdogValueDelegate;

	public static SetWindowTitleDelegate call_SetWindowTitleDelegate;

	public static StartLoadingStuckCheckStateDelegate call_StartLoadingStuckCheckStateDelegate;

	public static StartScenePerformanceReportDelegate call_StartScenePerformanceReportDelegate;

	public static TakeScreenshotFromPlatformPathDelegate call_TakeScreenshotFromPlatformPathDelegate;

	public static TakeScreenshotFromStringPathDelegate call_TakeScreenshotFromStringPathDelegate;

	public static TakeSSFromTopDelegate call_TakeSSFromTopDelegate;

	public static ToggleRenderDelegate call_ToggleRenderDelegate;

	public void AddCommandLineFunction(string concatName)
	{
		byte[] array = null;
		if (concatName != null)
		{
			int byteCount = _utf8.GetByteCount(concatName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(concatName, 0, concatName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddCommandLineFunctionDelegate(array);
	}

	public void AddMainThreadPerformanceQuery(string parent, string name, float seconds)
	{
		byte[] array = null;
		if (parent != null)
		{
			int byteCount = _utf8.GetByteCount(parent);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(parent, 0, parent.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (name != null)
		{
			int byteCount2 = _utf8.GetByteCount(name);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(name, 0, name.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_AddMainThreadPerformanceQueryDelegate(array, array2, seconds);
	}

	public void AddPerformanceReportToken(string performance_type, string name, float loading_time)
	{
		byte[] array = null;
		if (performance_type != null)
		{
			int byteCount = _utf8.GetByteCount(performance_type);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(performance_type, 0, performance_type.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (name != null)
		{
			int byteCount2 = _utf8.GetByteCount(name);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(name, 0, name.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_AddPerformanceReportTokenDelegate(array, array2, loading_time);
	}

	public void AddSceneObjectReport(string scene_name, string report_name, float report_value)
	{
		byte[] array = null;
		if (scene_name != null)
		{
			int byteCount = _utf8.GetByteCount(scene_name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(scene_name, 0, scene_name.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (report_name != null)
		{
			int byteCount2 = _utf8.GetByteCount(report_name);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(report_name, 0, report_name.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_AddSceneObjectReportDelegate(array, array2, report_value);
	}

	public void CheckIfAssetsAndSourcesAreSame()
	{
		call_CheckIfAssetsAndSourcesAreSameDelegate();
	}

	public bool CheckIfTerrainShaderHeaderGenerationFinished()
	{
		return call_CheckIfTerrainShaderHeaderGenerationFinishedDelegate();
	}

	public void CheckResourceModifications()
	{
		call_CheckResourceModificationsDelegate();
	}

	public void CheckSceneForProblems(string path)
	{
		byte[] array = null;
		if (path != null)
		{
			int byteCount = _utf8.GetByteCount(path);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(path, 0, path.Length, array, 0);
			array[byteCount] = 0;
		}
		call_CheckSceneForProblemsDelegate(array);
	}

	public bool CheckShaderCompilation()
	{
		return call_CheckShaderCompilationDelegate();
	}

	public void clear_decal_atlas(DecalAtlasGroup atlasGroup)
	{
		call_clear_decal_atlasDelegate(atlasGroup);
	}

	public void ClearOldResourcesAndObjects()
	{
		call_ClearOldResourcesAndObjectsDelegate();
	}

	public void ClearShaderMemory()
	{
		call_ClearShaderMemoryDelegate();
	}

	public bool CommandLineArgumentExists(string str)
	{
		byte[] array = null;
		if (str != null)
		{
			int byteCount = _utf8.GetByteCount(str);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(str, 0, str.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_CommandLineArgumentExistsDelegate(array);
	}

	public void CompileAllShaders(string targetPlatform)
	{
		byte[] array = null;
		if (targetPlatform != null)
		{
			int byteCount = _utf8.GetByteCount(targetPlatform);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(targetPlatform, 0, targetPlatform.Length, array, 0);
			array[byteCount] = 0;
		}
		call_CompileAllShadersDelegate(array);
	}

	public void CompileTerrainShadersDist(string targetPlatform, string targetConfig, string output_path)
	{
		byte[] array = null;
		if (targetPlatform != null)
		{
			int byteCount = _utf8.GetByteCount(targetPlatform);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(targetPlatform, 0, targetPlatform.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (targetConfig != null)
		{
			int byteCount2 = _utf8.GetByteCount(targetConfig);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(targetConfig, 0, targetConfig.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (output_path != null)
		{
			int byteCount3 = _utf8.GetByteCount(output_path);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(output_path, 0, output_path.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		call_CompileTerrainShadersDistDelegate(array, array2, array3);
	}

	public void CreateSelectionInEditor(UIntPtr[] gameEntities, int entityCount, string name)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntities);
		IntPtr pointer = pinnedArrayData.Pointer;
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_CreateSelectionInEditorDelegate(pointer, entityCount, array);
		pinnedArrayData.Dispose();
	}

	public void DebugSetGlobalLoadingWindowState(bool s)
	{
		call_DebugSetGlobalLoadingWindowStateDelegate(s);
	}

	public void DeleteEntitiesInEditorScene(UIntPtr[] gameEntities, int entityCount)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntities);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_DeleteEntitiesInEditorSceneDelegate(pointer, entityCount);
		pinnedArrayData.Dispose();
	}

	public void DetachWatchdog()
	{
		call_DetachWatchdogDelegate();
	}

	public bool DidAutomatedGIBakeFinished()
	{
		return call_DidAutomatedGIBakeFinishedDelegate();
	}

	public void DisableCoreGame()
	{
		call_DisableCoreGameDelegate();
	}

	public void DisableGlobalEditDataCacher()
	{
		call_DisableGlobalEditDataCacherDelegate();
	}

	public void DisableGlobalLoadingWindow()
	{
		call_DisableGlobalLoadingWindowDelegate();
	}

	public void DoDelayedexit(int returnCode)
	{
		call_DoDelayedexitDelegate(returnCode);
	}

	public void DoFullBakeAllLevelsAutomated(string module, string sceneName)
	{
		byte[] array = null;
		if (module != null)
		{
			int byteCount = _utf8.GetByteCount(module);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(module, 0, module.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (sceneName != null)
		{
			int byteCount2 = _utf8.GetByteCount(sceneName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_DoFullBakeAllLevelsAutomatedDelegate(array, array2);
	}

	public void DoFullBakeSingleLevelAutomated(string module, string sceneName)
	{
		byte[] array = null;
		if (module != null)
		{
			int byteCount = _utf8.GetByteCount(module);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(module, 0, module.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (sceneName != null)
		{
			int byteCount2 = _utf8.GetByteCount(sceneName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_DoFullBakeSingleLevelAutomatedDelegate(array, array2);
	}

	public void DoLightOnlyBakeAllLevelsAutomated(string module, string sceneName)
	{
		byte[] array = null;
		if (module != null)
		{
			int byteCount = _utf8.GetByteCount(module);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(module, 0, module.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (sceneName != null)
		{
			int byteCount2 = _utf8.GetByteCount(sceneName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_DoLightOnlyBakeAllLevelsAutomatedDelegate(array, array2);
	}

	public void DoLightOnlyBakeSingleLevelAutomated(string module, string sceneName)
	{
		byte[] array = null;
		if (module != null)
		{
			int byteCount = _utf8.GetByteCount(module);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(module, 0, module.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (sceneName != null)
		{
			int byteCount2 = _utf8.GetByteCount(sceneName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_DoLightOnlyBakeSingleLevelAutomatedDelegate(array, array2);
	}

	public void DumpGPUMemoryStatistics(string filePath)
	{
		byte[] array = null;
		if (filePath != null)
		{
			int byteCount = _utf8.GetByteCount(filePath);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(filePath, 0, filePath.Length, array, 0);
			array[byteCount] = 0;
		}
		call_DumpGPUMemoryStatisticsDelegate(array);
	}

	public void EnableGlobalEditDataCacher()
	{
		call_EnableGlobalEditDataCacherDelegate();
	}

	public void EnableGlobalLoadingWindow()
	{
		call_EnableGlobalLoadingWindowDelegate();
	}

	public void EnableSingleGPUQueryPerFrame()
	{
		call_EnableSingleGPUQueryPerFrameDelegate();
	}

	public void EndLoadingStuckCheckState()
	{
		call_EndLoadingStuckCheckStateDelegate();
	}

	public string ExecuteCommandLineCommand(string command)
	{
		byte[] array = null;
		if (command != null)
		{
			int byteCount = _utf8.GetByteCount(command);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(command, 0, command.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_ExecuteCommandLineCommandDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void ExitProcess(int exitCode)
	{
		call_ExitProcessDelegate(exitCode);
	}

	public string ExportNavMeshFaceMarks(string file_name)
	{
		byte[] array = null;
		if (file_name != null)
		{
			int byteCount = _utf8.GetByteCount(file_name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(file_name, 0, file_name.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_ExportNavMeshFaceMarksDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void FindMeshesWithoutLods(string module_name)
	{
		byte[] array = null;
		if (module_name != null)
		{
			int byteCount = _utf8.GetByteCount(module_name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(module_name, 0, module_name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_FindMeshesWithoutLodsDelegate(array);
	}

	public void FlushManagedObjectsMemory()
	{
		call_FlushManagedObjectsMemoryDelegate();
	}

	public void GatherCoreGameReferences(string scene_names)
	{
		byte[] array = null;
		if (scene_names != null)
		{
			int byteCount = _utf8.GetByteCount(scene_names);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(scene_names, 0, scene_names.Length, array, 0);
			array[byteCount] = 0;
		}
		call_GatherCoreGameReferencesDelegate(array);
	}

	public void GenerateTerrainShaderHeaders(string targetPlatform, string targetConfig, string output_path)
	{
		byte[] array = null;
		if (targetPlatform != null)
		{
			int byteCount = _utf8.GetByteCount(targetPlatform);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(targetPlatform, 0, targetPlatform.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (targetConfig != null)
		{
			int byteCount2 = _utf8.GetByteCount(targetConfig);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(targetConfig, 0, targetConfig.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (output_path != null)
		{
			int byteCount3 = _utf8.GetByteCount(output_path);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(output_path, 0, output_path.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		call_GenerateTerrainShaderHeadersDelegate(array, array2, array3);
	}

	public float GetApplicationMemory()
	{
		return call_GetApplicationMemoryDelegate();
	}

	public string GetApplicationMemoryStatistics()
	{
		if (call_GetApplicationMemoryStatisticsDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetApplicationName()
	{
		if (call_GetApplicationNameDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetAttachmentsPath()
	{
		if (call_GetAttachmentsPathDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetBaseDirectory()
	{
		if (call_GetBaseDirectoryDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetBenchmarkStatus()
	{
		return call_GetBenchmarkStatusDelegate();
	}

	public int GetBuildNumber()
	{
		return call_GetBuildNumberDelegate();
	}

	public string GetConsoleHostMachine()
	{
		if (call_GetConsoleHostMachineDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetCoreGameState()
	{
		return call_GetCoreGameStateDelegate();
	}

	public ulong GetCurrentCpuMemoryUsage()
	{
		return call_GetCurrentCpuMemoryUsageDelegate();
	}

	public int GetCurrentEstimatedGPUMemoryCostMB()
	{
		return call_GetCurrentEstimatedGPUMemoryCostMBDelegate();
	}

	public uint GetCurrentProcessID()
	{
		return call_GetCurrentProcessIDDelegate();
	}

	public ulong GetCurrentThreadId()
	{
		return call_GetCurrentThreadIdDelegate();
	}

	public float GetDeltaTime(int timerId)
	{
		return call_GetDeltaTimeDelegate(timerId);
	}

	public void GetDetailedGPUBufferMemoryStats(ref int totalMemoryAllocated, ref int totalMemoryUsed, ref int emptyChunkCount)
	{
		call_GetDetailedGPUBufferMemoryStatsDelegate(ref totalMemoryAllocated, ref totalMemoryUsed, ref emptyChunkCount);
	}

	public string GetDetailedXBOXMemoryInfo()
	{
		if (call_GetDetailedXBOXMemoryInfoDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void GetEditorSelectedEntities(UIntPtr[] gameEntitiesTemp)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntitiesTemp);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetEditorSelectedEntitiesDelegate(pointer);
		pinnedArrayData.Dispose();
	}

	public int GetEditorSelectedEntityCount()
	{
		return call_GetEditorSelectedEntityCountDelegate();
	}

	public int GetEngineFrameNo()
	{
		return call_GetEngineFrameNoDelegate();
	}

	public void GetEntitiesOfSelectionSet(string name, UIntPtr[] gameEntitiesTemp)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntitiesTemp);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetEntitiesOfSelectionSetDelegate(array, pointer);
		pinnedArrayData.Dispose();
	}

	public int GetEntityCountOfSelectionSet(string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetEntityCountOfSelectionSetDelegate(array);
	}

	public string GetExecutableWorkingDirectory()
	{
		if (call_GetExecutableWorkingDirectoryDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetFps()
	{
		return call_GetFpsDelegate();
	}

	public bool GetFrameLimiterWithSleep()
	{
		return call_GetFrameLimiterWithSleepDelegate();
	}

	public string GetFullCommandLineString()
	{
		if (call_GetFullCommandLineStringDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetFullFilePathOfScene(string sceneName)
	{
		byte[] array = null;
		if (sceneName != null)
		{
			int byteCount = _utf8.GetByteCount(sceneName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetFullFilePathOfSceneDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetFullModulePath(string moduleName)
	{
		byte[] array = null;
		if (moduleName != null)
		{
			int byteCount = _utf8.GetByteCount(moduleName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(moduleName, 0, moduleName.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetFullModulePathDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetFullModulePaths()
	{
		if (call_GetFullModulePathsDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetGPUMemoryMB()
	{
		return call_GetGPUMemoryMBDelegate();
	}

	public ulong GetGpuMemoryOfAllocationGroup(string allocationName)
	{
		byte[] array = null;
		if (allocationName != null)
		{
			int byteCount = _utf8.GetByteCount(allocationName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(allocationName, 0, allocationName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetGpuMemoryOfAllocationGroupDelegate(array);
	}

	public void GetGPUMemoryStats(ref float totalMemory, ref float renderTargetMemory, ref float depthTargetMemory, ref float srvMemory, ref float bufferMemory)
	{
		call_GetGPUMemoryStatsDelegate(ref totalMemory, ref renderTargetMemory, ref depthTargetMemory, ref srvMemory, ref bufferMemory);
	}

	public string GetLocalOutputPath()
	{
		if (call_GetLocalOutputPathDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetMainFps()
	{
		return call_GetMainFpsDelegate();
	}

	public ulong GetMainThreadId()
	{
		return call_GetMainThreadIdDelegate();
	}

	public int GetMemoryUsageOfCategory(int index)
	{
		return call_GetMemoryUsageOfCategoryDelegate(index);
	}

	public string GetModulesCode()
	{
		if (call_GetModulesCodeDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetNativeMemoryStatistics()
	{
		if (call_GetNativeMemoryStatisticsDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetNumberOfShaderCompilationsInProgress()
	{
		return call_GetNumberOfShaderCompilationsInProgressDelegate();
	}

	public string GetPCInfo()
	{
		if (call_GetPCInfoDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetPlatformModulePaths()
	{
		if (call_GetPlatformModulePathsDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetPossibleCommandLineStartingWith(string command, int index)
	{
		byte[] array = null;
		if (command != null)
		{
			int byteCount = _utf8.GetByteCount(command);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(command, 0, command.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetPossibleCommandLineStartingWithDelegate(array, index) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetRendererFps()
	{
		return call_GetRendererFpsDelegate();
	}

	public int GetReturnCode()
	{
		return call_GetReturnCodeDelegate();
	}

	public string GetSingleModuleScenesOfModule(string moduleName)
	{
		byte[] array = null;
		if (moduleName != null)
		{
			int byteCount = _utf8.GetByteCount(moduleName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(moduleName, 0, moduleName.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetSingleModuleScenesOfModuleDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetSteamAppId()
	{
		return call_GetSteamAppIdDelegate();
	}

	public string GetSystemLanguage()
	{
		if (call_GetSystemLanguageDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetVertexBufferChunkSystemMemoryUsage()
	{
		return call_GetVertexBufferChunkSystemMemoryUsageDelegate();
	}

	public string GetVisualTestsTestFilesPath()
	{
		if (call_GetVisualTestsTestFilesPathDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetVisualTestsValidatePath()
	{
		if (call_GetVisualTestsValidatePathDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public bool IsAsyncPhysicsThread()
	{
		return call_IsAsyncPhysicsThreadDelegate();
	}

	public bool IsBenchmarkQuited()
	{
		return call_IsBenchmarkQuitedDelegate();
	}

	public int IsDetailedSoundLogOn()
	{
		return call_IsDetailedSoundLogOnDelegate();
	}

	public bool IsDevkit()
	{
		return call_IsDevkitDelegate();
	}

	public bool IsEditModeEnabled()
	{
		return call_IsEditModeEnabledDelegate();
	}

	public bool IsLockhartPlatform()
	{
		return call_IsLockhartPlatformDelegate();
	}

	public bool IsSceneReportFinished()
	{
		return call_IsSceneReportFinishedDelegate();
	}

	public void LoadSkyBoxes()
	{
		call_LoadSkyBoxesDelegate();
	}

	public void LoadVirtualTextureTileset(string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_LoadVirtualTextureTilesetDelegate(array);
	}

	public void ManagedParallelFor(int fromInclusive, int toExclusive, long curKey, int grainSize)
	{
		call_ManagedParallelForDelegate(fromInclusive, toExclusive, curKey, grainSize);
	}

	public void ManagedParallelForWithDt(int fromInclusive, int toExclusive, long curKey, int grainSize)
	{
		call_ManagedParallelForWithDtDelegate(fromInclusive, toExclusive, curKey, grainSize);
	}

	public void ManagedParallelForWithoutRenderThread(int fromInclusive, int toExclusive, long curKey, int grainSize)
	{
		call_ManagedParallelForWithoutRenderThreadDelegate(fromInclusive, toExclusive, curKey, grainSize);
	}

	public void ManagedParallelForWithoutRenderThreadDt(int fromInclusive, int toExclusive, long curKey, int grainSize)
	{
		call_ManagedParallelForWithoutRenderThreadDtDelegate(fromInclusive, toExclusive, curKey, grainSize);
	}

	public void OnLoadingWindowDisabled()
	{
		call_OnLoadingWindowDisabledDelegate();
	}

	public void OnLoadingWindowEnabled()
	{
		call_OnLoadingWindowEnabledDelegate();
	}

	public void OpenConsoleStorePage(string productId)
	{
		byte[] array = null;
		if (productId != null)
		{
			int byteCount = _utf8.GetByteCount(productId);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(productId, 0, productId.Length, array, 0);
			array[byteCount] = 0;
		}
		call_OpenConsoleStorePageDelegate(array);
	}

	public void OpenOnscreenKeyboard(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
	{
		byte[] array = null;
		if (initialText != null)
		{
			int byteCount = _utf8.GetByteCount(initialText);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(initialText, 0, initialText.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (descriptionText != null)
		{
			int byteCount2 = _utf8.GetByteCount(descriptionText);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(descriptionText, 0, descriptionText.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_OpenOnscreenKeyboardDelegate(array, array2, maxLength, keyboardTypeEnum);
	}

	public void OutputBenchmarkValuesToPerformanceReporter()
	{
		call_OutputBenchmarkValuesToPerformanceReporterDelegate();
	}

	public void OutputPerformanceReports()
	{
		call_OutputPerformanceReportsDelegate();
	}

	public void PairSceneNameToModuleName(string sceneName, string moduleName)
	{
		byte[] array = null;
		if (sceneName != null)
		{
			int byteCount = _utf8.GetByteCount(sceneName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (moduleName != null)
		{
			int byteCount2 = _utf8.GetByteCount(moduleName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(moduleName, 0, moduleName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_PairSceneNameToModuleNameDelegate(array, array2);
	}

	public string ProcessWindowTitle(string title)
	{
		byte[] array = null;
		if (title != null)
		{
			int byteCount = _utf8.GetByteCount(title);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(title, 0, title.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_ProcessWindowTitleDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void QuitGame()
	{
		call_QuitGameDelegate();
	}

	public int RegisterGPUAllocationGroup(string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_RegisterGPUAllocationGroupDelegate(array);
	}

	public void RegisterMeshForGPUMorph(string metaMeshName)
	{
		byte[] array = null;
		if (metaMeshName != null)
		{
			int byteCount = _utf8.GetByteCount(metaMeshName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(metaMeshName, 0, metaMeshName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RegisterMeshForGPUMorphDelegate(array);
	}

	public int SaveDataAsTexture(string path, int width, int height, float[] data)
	{
		byte[] array = null;
		if (path != null)
		{
			int byteCount = _utf8.GetByteCount(path);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(path, 0, path.Length, array, 0);
			array[byteCount] = 0;
		}
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(data);
		IntPtr pointer = pinnedArrayData.Pointer;
		int result = call_SaveDataAsTextureDelegate(array, width, height, pointer);
		pinnedArrayData.Dispose();
		return result;
	}

	public void SelectEntities(UIntPtr[] gameEntities, int entityCount)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntities);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_SelectEntitiesDelegate(pointer, entityCount);
		pinnedArrayData.Dispose();
	}

	public void SetAllocationAlwaysValidScene(UIntPtr scene)
	{
		call_SetAllocationAlwaysValidSceneDelegate(scene);
	}

	public void SetAssertionAtShaderCompile(bool value)
	{
		call_SetAssertionAtShaderCompileDelegate(value);
	}

	public void SetAssertionsAndWarningsSetExitCode(bool value)
	{
		call_SetAssertionsAndWarningsSetExitCodeDelegate(value);
	}

	public void SetBenchmarkStatus(int status, string def)
	{
		byte[] array = null;
		if (def != null)
		{
			int byteCount = _utf8.GetByteCount(def);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(def, 0, def.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetBenchmarkStatusDelegate(status, array);
	}

	public void SetCanLoadModules(bool canLoadModules)
	{
		call_SetCanLoadModulesDelegate(canLoadModules);
	}

	public void SetCoreGameState(int state)
	{
		call_SetCoreGameStateDelegate(state);
	}

	public void SetCrashOnAsserts(bool val)
	{
		call_SetCrashOnAssertsDelegate(val);
	}

	public void SetCrashOnWarnings(bool val)
	{
		call_SetCrashOnWarningsDelegate(val);
	}

	public void SetCrashReportCustomStack(string customStack)
	{
		byte[] array = null;
		if (customStack != null)
		{
			int byteCount = _utf8.GetByteCount(customStack);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(customStack, 0, customStack.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetCrashReportCustomStackDelegate(array);
	}

	public void SetCrashReportCustomString(string customString)
	{
		byte[] array = null;
		if (customString != null)
		{
			int byteCount = _utf8.GetByteCount(customString);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(customString, 0, customString.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetCrashReportCustomStringDelegate(array);
	}

	public void SetCreateDumpOnWarnings(bool val)
	{
		call_SetCreateDumpOnWarningsDelegate(val);
	}

	public void SetDisableDumpGeneration(bool value)
	{
		call_SetDisableDumpGenerationDelegate(value);
	}

	public void SetDumpFolderPath(string path)
	{
		byte[] array = null;
		if (path != null)
		{
			int byteCount = _utf8.GetByteCount(path);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(path, 0, path.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetDumpFolderPathDelegate(array);
	}

	public void SetFixedDt(bool enabled, float dt)
	{
		call_SetFixedDtDelegate(enabled, dt);
	}

	public void SetForceDrawEntityID(bool value)
	{
		call_SetForceDrawEntityIDDelegate(value);
	}

	public void SetForceVsync(bool value)
	{
		call_SetForceVsyncDelegate(value);
	}

	public void SetFrameLimiterWithSleep(bool value)
	{
		call_SetFrameLimiterWithSleepDelegate(value);
	}

	public void SetGraphicsPreset(int preset)
	{
		call_SetGraphicsPresetDelegate(preset);
	}

	public void SetLoadingScreenPercentage(float value)
	{
		call_SetLoadingScreenPercentageDelegate(value);
	}

	public void SetMessageLineRenderingState(bool value)
	{
		call_SetMessageLineRenderingStateDelegate(value);
	}

	public void SetPrintCallstackAtCrahses(bool value)
	{
		call_SetPrintCallstackAtCrahsesDelegate(value);
	}

	public void SetRenderAgents(bool value)
	{
		call_SetRenderAgentsDelegate(value);
	}

	public void SetRenderMode(int mode)
	{
		call_SetRenderModeDelegate(mode);
	}

	public void SetReportMode(bool reportMode)
	{
		call_SetReportModeDelegate(reportMode);
	}

	public void SetScreenTextRenderingState(bool value)
	{
		call_SetScreenTextRenderingStateDelegate(value);
	}

	public void SetWatchdogAutoreport(bool value)
	{
		call_SetWatchdogAutoreportDelegate(value);
	}

	public void SetWatchdogValue(string fileName, string groupName, string key, string value)
	{
		byte[] array = null;
		if (fileName != null)
		{
			int byteCount = _utf8.GetByteCount(fileName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(fileName, 0, fileName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (groupName != null)
		{
			int byteCount2 = _utf8.GetByteCount(groupName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(groupName, 0, groupName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (key != null)
		{
			int byteCount3 = _utf8.GetByteCount(key);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(key, 0, key.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		byte[] array4 = null;
		if (value != null)
		{
			int byteCount4 = _utf8.GetByteCount(value);
			array4 = ((byteCount4 < 1024) ? CallbackStringBufferManager.StringBuffer3 : new byte[byteCount4 + 1]);
			_utf8.GetBytes(value, 0, value.Length, array4, 0);
			array4[byteCount4] = 0;
		}
		call_SetWatchdogValueDelegate(array, array2, array3, array4);
	}

	public void SetWindowTitle(string title)
	{
		byte[] array = null;
		if (title != null)
		{
			int byteCount = _utf8.GetByteCount(title);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(title, 0, title.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetWindowTitleDelegate(array);
	}

	public void StartLoadingStuckCheckState(float seconds)
	{
		call_StartLoadingStuckCheckStateDelegate(seconds);
	}

	public void StartScenePerformanceReport(string folderPath)
	{
		byte[] array = null;
		if (folderPath != null)
		{
			int byteCount = _utf8.GetByteCount(folderPath);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(folderPath, 0, folderPath.Length, array, 0);
			array[byteCount] = 0;
		}
		call_StartScenePerformanceReportDelegate(array);
	}

	public void TakeScreenshotFromPlatformPath(PlatformFilePath path)
	{
		call_TakeScreenshotFromPlatformPathDelegate(path);
	}

	public void TakeScreenshotFromStringPath(string path)
	{
		byte[] array = null;
		if (path != null)
		{
			int byteCount = _utf8.GetByteCount(path);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(path, 0, path.Length, array, 0);
			array[byteCount] = 0;
		}
		call_TakeScreenshotFromStringPathDelegate(array);
	}

	public string TakeSSFromTop(string file_name)
	{
		byte[] array = null;
		if (file_name != null)
		{
			int byteCount = _utf8.GetByteCount(file_name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(file_name, 0, file_name.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_TakeSSFromTopDelegate(array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void ToggleRender()
	{
		call_ToggleRenderDelegate();
	}
}
