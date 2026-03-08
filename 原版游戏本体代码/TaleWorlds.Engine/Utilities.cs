using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000099 RID: 153
	public static class Utilities
	{
		// Token: 0x06000D1E RID: 3358 RVA: 0x0000EAF0 File Offset: 0x0000CCF0
		public static void ConstructMainThreadJob(Delegate function, params object[] parameters)
		{
			Utilities.MainThreadJob item = new Utilities.MainThreadJob(function, parameters);
			Utilities.jobs.Enqueue(item);
		}

		// Token: 0x06000D1F RID: 3359 RVA: 0x0000EB10 File Offset: 0x0000CD10
		public static void ConstructMainThreadJob(Semaphore semaphore, Delegate function, params object[] parameters)
		{
			Utilities.MainThreadJob item = new Utilities.MainThreadJob(semaphore, function, parameters);
			Utilities.jobs.Enqueue(item);
		}

		// Token: 0x06000D20 RID: 3360 RVA: 0x0000EB34 File Offset: 0x0000CD34
		public static void RunJobs()
		{
			Utilities.MainThreadJob mainThreadJob;
			while (Utilities.jobs.TryDequeue(out mainThreadJob))
			{
				mainThreadJob.Invoke();
			}
		}

		// Token: 0x06000D21 RID: 3361 RVA: 0x0000EB57 File Offset: 0x0000CD57
		public static void WaitJobs()
		{
			while (!Utilities.jobs.IsEmpty)
			{
			}
		}

		// Token: 0x06000D22 RID: 3362 RVA: 0x0000EB65 File Offset: 0x0000CD65
		public static void OutputBenchmarkValuesToPerformanceReporter()
		{
			EngineApplicationInterface.IUtil.OutputBenchmarkValuesToPerformanceReporter();
		}

		// Token: 0x06000D23 RID: 3363 RVA: 0x0000EB71 File Offset: 0x0000CD71
		public static void SetLoadingScreenPercentage(float value)
		{
			EngineApplicationInterface.IUtil.SetLoadingScreenPercentage(value);
		}

		// Token: 0x06000D24 RID: 3364 RVA: 0x0000EB7E File Offset: 0x0000CD7E
		public static void SetFixedDt(bool enabled, float dt)
		{
			EngineApplicationInterface.IUtil.SetFixedDt(enabled, dt);
		}

		// Token: 0x06000D25 RID: 3365 RVA: 0x0000EB8C File Offset: 0x0000CD8C
		public static void SetBenchmarkStatus(int status, string def)
		{
			EngineApplicationInterface.IUtil.SetBenchmarkStatus(status, def);
		}

		// Token: 0x06000D26 RID: 3366 RVA: 0x0000EB9A File Offset: 0x0000CD9A
		public static int GetBenchmarkStatus()
		{
			return EngineApplicationInterface.IUtil.GetBenchmarkStatus();
		}

		// Token: 0x06000D27 RID: 3367 RVA: 0x0000EBA6 File Offset: 0x0000CDA6
		public static string GetApplicationMemoryStatistics()
		{
			return EngineApplicationInterface.IUtil.GetApplicationMemoryStatistics();
		}

		// Token: 0x06000D28 RID: 3368 RVA: 0x0000EBB2 File Offset: 0x0000CDB2
		public static bool IsBenchmarkQuited()
		{
			return EngineApplicationInterface.IUtil.IsBenchmarkQuited();
		}

		// Token: 0x06000D29 RID: 3369 RVA: 0x0000EBBE File Offset: 0x0000CDBE
		public static string GetNativeMemoryStatistics()
		{
			return EngineApplicationInterface.IUtil.GetNativeMemoryStatistics();
		}

		// Token: 0x06000D2A RID: 3370 RVA: 0x0000EBCA File Offset: 0x0000CDCA
		public static bool CommandLineArgumentExists(string str)
		{
			return EngineApplicationInterface.IUtil.CommandLineArgumentExists(str);
		}

		// Token: 0x06000D2B RID: 3371 RVA: 0x0000EBD7 File Offset: 0x0000CDD7
		public static string GetConsoleHostMachine()
		{
			return EngineApplicationInterface.IUtil.GetConsoleHostMachine();
		}

		// Token: 0x06000D2C RID: 3372 RVA: 0x0000EBE3 File Offset: 0x0000CDE3
		public static string ExportNavMeshFaceMarks(string file_name)
		{
			return EngineApplicationInterface.IUtil.ExportNavMeshFaceMarks(file_name);
		}

		// Token: 0x06000D2D RID: 3373 RVA: 0x0000EBF0 File Offset: 0x0000CDF0
		public static string TakeSSFromTop(string file_name)
		{
			return EngineApplicationInterface.IUtil.TakeSSFromTop(file_name);
		}

		// Token: 0x06000D2E RID: 3374 RVA: 0x0000EBFD File Offset: 0x0000CDFD
		public static void CheckIfAssetsAndSourcesAreSame()
		{
			EngineApplicationInterface.IUtil.CheckIfAssetsAndSourcesAreSame();
		}

		// Token: 0x06000D2F RID: 3375 RVA: 0x0000EC09 File Offset: 0x0000CE09
		public static void DisableCoreGame()
		{
			EngineApplicationInterface.IUtil.DisableCoreGame();
		}

		// Token: 0x06000D30 RID: 3376 RVA: 0x0000EC15 File Offset: 0x0000CE15
		public static float GetApplicationMemory()
		{
			return EngineApplicationInterface.IUtil.GetApplicationMemory();
		}

		// Token: 0x06000D31 RID: 3377 RVA: 0x0000EC21 File Offset: 0x0000CE21
		public static void GatherCoreGameReferences(string scene_names)
		{
			EngineApplicationInterface.IUtil.GatherCoreGameReferences(scene_names);
		}

		// Token: 0x06000D32 RID: 3378 RVA: 0x0000EC2E File Offset: 0x0000CE2E
		public static bool IsOnlyCoreContentEnabled()
		{
			return EngineApplicationInterface.IUtil.GetCoreGameState() != 0;
		}

		// Token: 0x06000D33 RID: 3379 RVA: 0x0000EC3D File Offset: 0x0000CE3D
		public static void FindMeshesWithoutLods(string module_name)
		{
			EngineApplicationInterface.IUtil.FindMeshesWithoutLods(module_name);
		}

		// Token: 0x06000D34 RID: 3380 RVA: 0x0000EC4A File Offset: 0x0000CE4A
		public static void SetDisableDumpGeneration(bool value)
		{
			EngineApplicationInterface.IUtil.SetDisableDumpGeneration(value);
		}

		// Token: 0x06000D35 RID: 3381 RVA: 0x0000EC57 File Offset: 0x0000CE57
		public static void SetPrintCallstackAtCrahses(bool value)
		{
			EngineApplicationInterface.IUtil.SetPrintCallstackAtCrahses(value);
		}

		// Token: 0x06000D36 RID: 3382 RVA: 0x0000EC64 File Offset: 0x0000CE64
		public static string[] GetModulesNames()
		{
			return EngineApplicationInterface.IUtil.GetModulesCode().Split(new char[] { '*' });
		}

		// Token: 0x06000D37 RID: 3383 RVA: 0x0000EC80 File Offset: 0x0000CE80
		public static string GetFullFilePathOfScene(string sceneName)
		{
			string fullFilePathOfScene = EngineApplicationInterface.IUtil.GetFullFilePathOfScene(sceneName);
			if (fullFilePathOfScene == "SCENE_NOT_FOUND")
			{
				throw new Exception("Scene '" + sceneName + "' was not found!");
			}
			return fullFilePathOfScene.Replace("$BASE/", Utilities.GetBasePath());
		}

		// Token: 0x06000D38 RID: 3384 RVA: 0x0000ECC0 File Offset: 0x0000CEC0
		public static bool TryGetFullFilePathOfScene(string sceneName, out string fullPath)
		{
			bool result;
			try
			{
				fullPath = Utilities.GetFullFilePathOfScene(sceneName);
				result = true;
			}
			catch (Exception)
			{
				fullPath = null;
				result = false;
			}
			return result;
		}

		// Token: 0x06000D39 RID: 3385 RVA: 0x0000ECF4 File Offset: 0x0000CEF4
		public static bool TryGetUniqueIdentifiersForScene(string sceneName, out UniqueSceneId identifiers)
		{
			identifiers = null;
			string xsceneFilePath;
			return Utilities.TryGetFullFilePathOfScene(sceneName, out xsceneFilePath) && Utilities.TryGetUniqueIdentifiersForSceneFile(xsceneFilePath, out identifiers);
		}

		// Token: 0x06000D3A RID: 3386 RVA: 0x0000ED18 File Offset: 0x0000CF18
		public static bool TryGetUniqueIdentifiersForSceneFile(string xsceneFilePath, out UniqueSceneId identifiers)
		{
			identifiers = null;
			using (XmlReader xmlReader = XmlReader.Create(new StreamReader(xsceneFilePath)))
			{
				string attribute;
				string attribute2;
				if (xmlReader.MoveToContent() == XmlNodeType.Element && xmlReader.Name == "scene" && (attribute = xmlReader.GetAttribute("unique_token")) != null && (attribute2 = xmlReader.GetAttribute("revision")) != null)
				{
					identifiers = new UniqueSceneId(attribute, attribute2);
				}
			}
			return identifiers != null;
		}

		// Token: 0x06000D3B RID: 3387 RVA: 0x0000ED98 File Offset: 0x0000CF98
		public static void PairSceneNameToModuleName(string sceneName, string moduleName)
		{
			EngineApplicationInterface.IUtil.PairSceneNameToModuleName(sceneName, moduleName);
		}

		// Token: 0x06000D3C RID: 3388 RVA: 0x0000EDA6 File Offset: 0x0000CFA6
		public static string[] GetSingleModuleScenesOfModule(string moduleName)
		{
			return EngineApplicationInterface.IUtil.GetSingleModuleScenesOfModule(moduleName).Split(new char[] { '*' });
		}

		// Token: 0x06000D3D RID: 3389 RVA: 0x0000EDC3 File Offset: 0x0000CFC3
		public static string GetFullCommandLineString()
		{
			return EngineApplicationInterface.IUtil.GetFullCommandLineString();
		}

		// Token: 0x06000D3E RID: 3390 RVA: 0x0000EDCF File Offset: 0x0000CFCF
		public static void SetScreenTextRenderingState(bool state)
		{
			EngineApplicationInterface.IUtil.SetScreenTextRenderingState(state);
		}

		// Token: 0x06000D3F RID: 3391 RVA: 0x0000EDDC File Offset: 0x0000CFDC
		public static void SetMessageLineRenderingState(bool state)
		{
			EngineApplicationInterface.IUtil.SetMessageLineRenderingState(state);
		}

		// Token: 0x06000D40 RID: 3392 RVA: 0x0000EDE9 File Offset: 0x0000CFE9
		public static bool CheckIfTerrainShaderHeaderGenerationFinished()
		{
			return EngineApplicationInterface.IUtil.CheckIfTerrainShaderHeaderGenerationFinished();
		}

		// Token: 0x06000D41 RID: 3393 RVA: 0x0000EDF5 File Offset: 0x0000CFF5
		public static void GenerateTerrainShaderHeaders(string targetPlatform, string targetConfig, string output_path)
		{
			EngineApplicationInterface.IUtil.GenerateTerrainShaderHeaders(targetPlatform, targetConfig, output_path);
		}

		// Token: 0x06000D42 RID: 3394 RVA: 0x0000EE04 File Offset: 0x0000D004
		public static void CompileTerrainShadersDist(string targetPlatform, string targetConfig, string output_path)
		{
			EngineApplicationInterface.IUtil.CompileTerrainShadersDist(targetPlatform, targetConfig, output_path);
		}

		// Token: 0x06000D43 RID: 3395 RVA: 0x0000EE13 File Offset: 0x0000D013
		public static void SetCrashOnAsserts(bool val)
		{
			EngineApplicationInterface.IUtil.SetCrashOnAsserts(val);
		}

		// Token: 0x06000D44 RID: 3396 RVA: 0x0000EE20 File Offset: 0x0000D020
		public static void SetCrashOnWarnings(bool val)
		{
			EngineApplicationInterface.IUtil.SetCrashOnWarnings(val);
		}

		// Token: 0x06000D45 RID: 3397 RVA: 0x0000EE2D File Offset: 0x0000D02D
		public static void SetCreateDumpOnWarnings(bool val)
		{
			EngineApplicationInterface.IUtil.SetCreateDumpOnWarnings(val);
		}

		// Token: 0x06000D46 RID: 3398 RVA: 0x0000EE3A File Offset: 0x0000D03A
		public static void ToggleRender()
		{
			EngineApplicationInterface.IUtil.ToggleRender();
		}

		// Token: 0x06000D47 RID: 3399 RVA: 0x0000EE46 File Offset: 0x0000D046
		public static void SetRenderAgents(bool value)
		{
			EngineApplicationInterface.IUtil.SetRenderAgents(value);
		}

		// Token: 0x06000D48 RID: 3400 RVA: 0x0000EE53 File Offset: 0x0000D053
		public static bool CheckShaderCompilation()
		{
			return EngineApplicationInterface.IUtil.CheckShaderCompilation();
		}

		// Token: 0x06000D49 RID: 3401 RVA: 0x0000EE5F File Offset: 0x0000D05F
		public static void CompileAllShaders(string targetPlatform)
		{
			EngineApplicationInterface.IUtil.CompileAllShaders(targetPlatform);
		}

		// Token: 0x06000D4A RID: 3402 RVA: 0x0000EE6C File Offset: 0x0000D06C
		public static string GetExecutableWorkingDirectory()
		{
			return EngineApplicationInterface.IUtil.GetExecutableWorkingDirectory();
		}

		// Token: 0x06000D4B RID: 3403 RVA: 0x0000EE78 File Offset: 0x0000D078
		public static void SetDumpFolderPath(string path)
		{
			EngineApplicationInterface.IUtil.SetDumpFolderPath(path);
		}

		// Token: 0x06000D4C RID: 3404 RVA: 0x0000EE85 File Offset: 0x0000D085
		public static void CheckSceneForProblems(string sceneName)
		{
			EngineApplicationInterface.IUtil.CheckSceneForProblems(sceneName);
		}

		// Token: 0x06000D4D RID: 3405 RVA: 0x0000EE92 File Offset: 0x0000D092
		public static void SetCoreGameState(int state)
		{
			EngineApplicationInterface.IUtil.SetCoreGameState(state);
		}

		// Token: 0x06000D4E RID: 3406 RVA: 0x0000EE9F File Offset: 0x0000D09F
		public static int GetCoreGameState()
		{
			return EngineApplicationInterface.IUtil.GetCoreGameState();
		}

		// Token: 0x06000D4F RID: 3407 RVA: 0x0000EEAB File Offset: 0x0000D0AB
		public static string ExecuteCommandLineCommand(string command)
		{
			return EngineApplicationInterface.IUtil.ExecuteCommandLineCommand(command);
		}

		// Token: 0x06000D50 RID: 3408 RVA: 0x0000EEB8 File Offset: 0x0000D0B8
		public static void QuitGame()
		{
			EngineApplicationInterface.IUtil.QuitGame();
		}

		// Token: 0x06000D51 RID: 3409 RVA: 0x0000EEC4 File Offset: 0x0000D0C4
		public static void ExitProcess(int exitCode)
		{
			EngineApplicationInterface.IUtil.ExitProcess(exitCode);
		}

		// Token: 0x06000D52 RID: 3410 RVA: 0x0000EED1 File Offset: 0x0000D0D1
		public static string GetBasePath()
		{
			return EngineApplicationInterface.IUtil.GetBaseDirectory();
		}

		// Token: 0x06000D53 RID: 3411 RVA: 0x0000EEDD File Offset: 0x0000D0DD
		public static string GetVisualTestsValidatePath()
		{
			return EngineApplicationInterface.IUtil.GetVisualTestsValidatePath();
		}

		// Token: 0x06000D54 RID: 3412 RVA: 0x0000EEE9 File Offset: 0x0000D0E9
		public static string GetVisualTestsTestFilesPath()
		{
			return EngineApplicationInterface.IUtil.GetVisualTestsTestFilesPath();
		}

		// Token: 0x06000D55 RID: 3413 RVA: 0x0000EEF5 File Offset: 0x0000D0F5
		public static string GetAttachmentsPath()
		{
			return EngineApplicationInterface.IUtil.GetAttachmentsPath();
		}

		// Token: 0x06000D56 RID: 3414 RVA: 0x0000EF01 File Offset: 0x0000D101
		public static void StartScenePerformanceReport(string folderPath)
		{
			EngineApplicationInterface.IUtil.StartScenePerformanceReport(folderPath);
		}

		// Token: 0x06000D57 RID: 3415 RVA: 0x0000EF0E File Offset: 0x0000D10E
		public static bool IsSceneReportFinished()
		{
			return EngineApplicationInterface.IUtil.IsSceneReportFinished();
		}

		// Token: 0x06000D58 RID: 3416 RVA: 0x0000EF1A File Offset: 0x0000D11A
		public static float GetFps()
		{
			return EngineApplicationInterface.IUtil.GetFps();
		}

		// Token: 0x06000D59 RID: 3417 RVA: 0x0000EF26 File Offset: 0x0000D126
		public static float GetMainFps()
		{
			return EngineApplicationInterface.IUtil.GetMainFps();
		}

		// Token: 0x06000D5A RID: 3418 RVA: 0x0000EF32 File Offset: 0x0000D132
		public static float GetRendererFps()
		{
			return EngineApplicationInterface.IUtil.GetRendererFps();
		}

		// Token: 0x06000D5B RID: 3419 RVA: 0x0000EF3E File Offset: 0x0000D13E
		public static void EnableSingleGPUQueryPerFrame()
		{
			EngineApplicationInterface.IUtil.EnableSingleGPUQueryPerFrame();
		}

		// Token: 0x06000D5C RID: 3420 RVA: 0x0000EF4A File Offset: 0x0000D14A
		public static void ClearDecalAtlas(DecalAtlasGroup atlasGroup)
		{
			EngineApplicationInterface.IUtil.clear_decal_atlas(atlasGroup);
		}

		// Token: 0x06000D5D RID: 3421 RVA: 0x0000EF57 File Offset: 0x0000D157
		public static void FlushManagedObjectsMemory()
		{
			Common.MemoryCleanupGC(false);
		}

		// Token: 0x06000D5E RID: 3422 RVA: 0x0000EF5F File Offset: 0x0000D15F
		public static void OnLoadingWindowEnabled()
		{
			EngineApplicationInterface.IUtil.OnLoadingWindowEnabled();
		}

		// Token: 0x06000D5F RID: 3423 RVA: 0x0000EF6B File Offset: 0x0000D16B
		public static void DebugSetGlobalLoadingWindowState(bool newState)
		{
			EngineApplicationInterface.IUtil.DebugSetGlobalLoadingWindowState(newState);
		}

		// Token: 0x06000D60 RID: 3424 RVA: 0x0000EF78 File Offset: 0x0000D178
		public static void OnLoadingWindowDisabled()
		{
			EngineApplicationInterface.IUtil.OnLoadingWindowDisabled();
		}

		// Token: 0x06000D61 RID: 3425 RVA: 0x0000EF84 File Offset: 0x0000D184
		public static void DisableGlobalLoadingWindow()
		{
			EngineApplicationInterface.IUtil.DisableGlobalLoadingWindow();
		}

		// Token: 0x06000D62 RID: 3426 RVA: 0x0000EF90 File Offset: 0x0000D190
		public static void EnableGlobalLoadingWindow()
		{
			EngineApplicationInterface.IUtil.EnableGlobalLoadingWindow();
		}

		// Token: 0x06000D63 RID: 3427 RVA: 0x0000EF9C File Offset: 0x0000D19C
		public static void EnableGlobalEditDataCacher()
		{
			EngineApplicationInterface.IUtil.EnableGlobalEditDataCacher();
		}

		// Token: 0x06000D64 RID: 3428 RVA: 0x0000EFA8 File Offset: 0x0000D1A8
		public static void DoFullBakeAllLevelsAutomated(string module, string scene)
		{
			EngineApplicationInterface.IUtil.DoFullBakeAllLevelsAutomated(module, scene);
		}

		// Token: 0x06000D65 RID: 3429 RVA: 0x0000EFB6 File Offset: 0x0000D1B6
		public static int GetReturnCode()
		{
			return EngineApplicationInterface.IUtil.GetReturnCode();
		}

		// Token: 0x06000D66 RID: 3430 RVA: 0x0000EFC2 File Offset: 0x0000D1C2
		public static void DisableGlobalEditDataCacher()
		{
			EngineApplicationInterface.IUtil.DisableGlobalEditDataCacher();
		}

		// Token: 0x06000D67 RID: 3431 RVA: 0x0000EFCE File Offset: 0x0000D1CE
		public static void DoFullBakeSingleLevelAutomated(string module, string scene)
		{
			EngineApplicationInterface.IUtil.DoFullBakeSingleLevelAutomated(module, scene);
		}

		// Token: 0x06000D68 RID: 3432 RVA: 0x0000EFDC File Offset: 0x0000D1DC
		public static void DoLightOnlyBakeSingleLevelAutomated(string module, string scene)
		{
			EngineApplicationInterface.IUtil.DoLightOnlyBakeSingleLevelAutomated(module, scene);
		}

		// Token: 0x06000D69 RID: 3433 RVA: 0x0000EFEA File Offset: 0x0000D1EA
		public static void DoLightOnlyBakeAllLevelsAutomated(string module, string scene)
		{
			EngineApplicationInterface.IUtil.DoLightOnlyBakeAllLevelsAutomated(module, scene);
		}

		// Token: 0x06000D6A RID: 3434 RVA: 0x0000EFF8 File Offset: 0x0000D1F8
		public static bool DidAutomatedGIBakeFinished()
		{
			return EngineApplicationInterface.IUtil.DidAutomatedGIBakeFinished();
		}

		// Token: 0x06000D6B RID: 3435 RVA: 0x0000F004 File Offset: 0x0000D204
		public static void GetSelectedEntities(ref List<GameEntity> gameEntities)
		{
			int editorSelectedEntityCount = EngineApplicationInterface.IUtil.GetEditorSelectedEntityCount();
			UIntPtr[] array = new UIntPtr[editorSelectedEntityCount];
			EngineApplicationInterface.IUtil.GetEditorSelectedEntities(array);
			for (int i = 0; i < editorSelectedEntityCount; i++)
			{
				gameEntities.Add(new GameEntity(array[i]));
			}
		}

		// Token: 0x06000D6C RID: 3436 RVA: 0x0000F04C File Offset: 0x0000D24C
		public static void DeleteEntitiesInEditorScene(List<GameEntity> gameEntities)
		{
			int count = gameEntities.Count;
			UIntPtr[] array = new UIntPtr[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = gameEntities[i].Pointer;
			}
			EngineApplicationInterface.IUtil.DeleteEntitiesInEditorScene(array, count);
		}

		// Token: 0x06000D6D RID: 3437 RVA: 0x0000F090 File Offset: 0x0000D290
		public static void CreateSelectionInEditor(List<GameEntity> gameEntities, string name)
		{
			int count = gameEntities.Count;
			UIntPtr[] array = new UIntPtr[gameEntities.Count];
			for (int i = 0; i < count; i++)
			{
				array[i] = gameEntities[i].Pointer;
			}
			EngineApplicationInterface.IUtil.CreateSelectionInEditor(array, count, name);
		}

		// Token: 0x06000D6E RID: 3438 RVA: 0x0000F0D8 File Offset: 0x0000D2D8
		public static void SelectEntities(List<GameEntity> gameEntities)
		{
			int count = gameEntities.Count;
			UIntPtr[] array = new UIntPtr[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = gameEntities[i].Pointer;
			}
			EngineApplicationInterface.IUtil.SelectEntities(array, count);
		}

		// Token: 0x06000D6F RID: 3439 RVA: 0x0000F11C File Offset: 0x0000D31C
		public static void GetEntitiesOfSelectionSet(string selectionSetName, ref List<GameEntity> gameEntities)
		{
			int entityCountOfSelectionSet = EngineApplicationInterface.IUtil.GetEntityCountOfSelectionSet(selectionSetName);
			UIntPtr[] array = new UIntPtr[entityCountOfSelectionSet];
			EngineApplicationInterface.IUtil.GetEntitiesOfSelectionSet(selectionSetName, array);
			for (int i = 0; i < entityCountOfSelectionSet; i++)
			{
				gameEntities.Add(new GameEntity(array[i]));
			}
		}

		// Token: 0x06000D70 RID: 3440 RVA: 0x0000F163 File Offset: 0x0000D363
		public static void AddCommandLineFunction(string concatName)
		{
			EngineApplicationInterface.IUtil.AddCommandLineFunction(concatName);
		}

		// Token: 0x06000D71 RID: 3441 RVA: 0x0000F170 File Offset: 0x0000D370
		public static int GetNumberOfShaderCompilationsInProgress()
		{
			return EngineApplicationInterface.IUtil.GetNumberOfShaderCompilationsInProgress();
		}

		// Token: 0x06000D72 RID: 3442 RVA: 0x0000F17C File Offset: 0x0000D37C
		public static int IsDetailedSoundLogOn()
		{
			return EngineApplicationInterface.IUtil.IsDetailedSoundLogOn();
		}

		// Token: 0x06000D73 RID: 3443 RVA: 0x0000F188 File Offset: 0x0000D388
		public static ulong GetCurrentCpuMemoryUsageMB()
		{
			return EngineApplicationInterface.IUtil.GetCurrentCpuMemoryUsage();
		}

		// Token: 0x06000D74 RID: 3444 RVA: 0x0000F194 File Offset: 0x0000D394
		public static ulong GetGpuMemoryOfAllocationGroup(string name)
		{
			return EngineApplicationInterface.IUtil.GetGpuMemoryOfAllocationGroup(name);
		}

		// Token: 0x06000D75 RID: 3445 RVA: 0x0000F1A1 File Offset: 0x0000D3A1
		public static void GetGPUMemoryStats(ref float totalMemory, ref float renderTargetMemory, ref float depthTargetMemory, ref float srvMemory, ref float bufferMemory)
		{
			EngineApplicationInterface.IUtil.GetGPUMemoryStats(ref totalMemory, ref renderTargetMemory, ref depthTargetMemory, ref srvMemory, ref bufferMemory);
		}

		// Token: 0x06000D76 RID: 3446 RVA: 0x0000F1B3 File Offset: 0x0000D3B3
		public static void GetDetailedGPUMemoryData(ref int totalMemoryAllocated, ref int totalMemoryUsed, ref int emptyChunkTotalSize)
		{
			EngineApplicationInterface.IUtil.GetDetailedGPUBufferMemoryStats(ref totalMemoryAllocated, ref totalMemoryUsed, ref emptyChunkTotalSize);
		}

		// Token: 0x06000D77 RID: 3447 RVA: 0x0000F1C2 File Offset: 0x0000D3C2
		public static void SetRenderMode(Utilities.EngineRenderDisplayMode mode)
		{
			EngineApplicationInterface.IUtil.SetRenderMode((int)mode);
		}

		// Token: 0x06000D78 RID: 3448 RVA: 0x0000F1CF File Offset: 0x0000D3CF
		public static void SetForceDrawEntityID(bool value)
		{
			EngineApplicationInterface.IUtil.SetForceDrawEntityID(value);
		}

		// Token: 0x06000D79 RID: 3449 RVA: 0x0000F1DC File Offset: 0x0000D3DC
		public static void AddPerformanceReportToken(string performance_type, string name, float loading_time)
		{
			EngineApplicationInterface.IUtil.AddPerformanceReportToken(performance_type, name, loading_time);
		}

		// Token: 0x06000D7A RID: 3450 RVA: 0x0000F1EB File Offset: 0x0000D3EB
		public static void AddSceneObjectReport(string scene_name, string report_name, float report_value)
		{
			EngineApplicationInterface.IUtil.AddSceneObjectReport(scene_name, report_name, report_value);
		}

		// Token: 0x06000D7B RID: 3451 RVA: 0x0000F1FA File Offset: 0x0000D3FA
		public static void OutputPerformanceReports()
		{
			EngineApplicationInterface.IUtil.OutputPerformanceReports();
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x06000D7C RID: 3452 RVA: 0x0000F206 File Offset: 0x0000D406
		public static int EngineFrameNo
		{
			get
			{
				return EngineApplicationInterface.IUtil.GetEngineFrameNo();
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x06000D7D RID: 3453 RVA: 0x0000F212 File Offset: 0x0000D412
		public static bool EditModeEnabled
		{
			get
			{
				return EngineApplicationInterface.IUtil.IsEditModeEnabled();
			}
		}

		// Token: 0x06000D7E RID: 3454 RVA: 0x0000F21E File Offset: 0x0000D41E
		public static void TakeScreenshot(PlatformFilePath path)
		{
			EngineApplicationInterface.IUtil.TakeScreenshotFromPlatformPath(path);
		}

		// Token: 0x06000D7F RID: 3455 RVA: 0x0000F22B File Offset: 0x0000D42B
		public static void TakeScreenshot(string path)
		{
			EngineApplicationInterface.IUtil.TakeScreenshotFromStringPath(path);
		}

		// Token: 0x06000D80 RID: 3456 RVA: 0x0000F238 File Offset: 0x0000D438
		public static void SetAllocationAlwaysValidScene(Scene scene)
		{
			EngineApplicationInterface.IUtil.SetAllocationAlwaysValidScene((scene != null) ? scene.Pointer : UIntPtr.Zero);
		}

		// Token: 0x06000D81 RID: 3457 RVA: 0x0000F25A File Offset: 0x0000D45A
		public static void CheckResourceModifications()
		{
			EngineApplicationInterface.IUtil.CheckResourceModifications();
		}

		// Token: 0x06000D82 RID: 3458 RVA: 0x0000F266 File Offset: 0x0000D466
		public static void SetGraphicsPreset(int preset)
		{
			EngineApplicationInterface.IUtil.SetGraphicsPreset(preset);
		}

		// Token: 0x06000D83 RID: 3459 RVA: 0x0000F273 File Offset: 0x0000D473
		public static string GetLocalOutputPath()
		{
			return EngineApplicationInterface.IUtil.GetLocalOutputPath();
		}

		// Token: 0x06000D84 RID: 3460 RVA: 0x0000F27F File Offset: 0x0000D47F
		public static string GetPCInfo()
		{
			return EngineApplicationInterface.IUtil.GetPCInfo();
		}

		// Token: 0x06000D85 RID: 3461 RVA: 0x0000F28B File Offset: 0x0000D48B
		public static int GetGPUMemoryMB()
		{
			return EngineApplicationInterface.IUtil.GetGPUMemoryMB();
		}

		// Token: 0x06000D86 RID: 3462 RVA: 0x0000F297 File Offset: 0x0000D497
		public static int GetCurrentEstimatedGPUMemoryCostMB()
		{
			return EngineApplicationInterface.IUtil.GetCurrentEstimatedGPUMemoryCostMB();
		}

		// Token: 0x06000D87 RID: 3463 RVA: 0x0000F2A3 File Offset: 0x0000D4A3
		public static void DumpGPUMemoryStatistics(string filePath)
		{
			EngineApplicationInterface.IUtil.DumpGPUMemoryStatistics(filePath);
		}

		// Token: 0x06000D88 RID: 3464 RVA: 0x0000F2B0 File Offset: 0x0000D4B0
		public static int SaveDataAsTexture(string path, int width, int height, float[] data)
		{
			return EngineApplicationInterface.IUtil.SaveDataAsTexture(path, width, height, data);
		}

		// Token: 0x06000D89 RID: 3465 RVA: 0x0000F2C0 File Offset: 0x0000D4C0
		public static void ClearOldResourcesAndObjects()
		{
			EngineApplicationInterface.IUtil.ClearOldResourcesAndObjects();
		}

		// Token: 0x06000D8A RID: 3466 RVA: 0x0000F2CC File Offset: 0x0000D4CC
		public static void LoadVirtualTextureTileset(string name)
		{
			EngineApplicationInterface.IUtil.LoadVirtualTextureTileset(name);
		}

		// Token: 0x06000D8B RID: 3467 RVA: 0x0000F2D9 File Offset: 0x0000D4D9
		public static float GetDeltaTime(int timerId)
		{
			return EngineApplicationInterface.IUtil.GetDeltaTime(timerId);
		}

		// Token: 0x06000D8C RID: 3468 RVA: 0x0000F2E6 File Offset: 0x0000D4E6
		public static void LoadSkyBoxes()
		{
			EngineApplicationInterface.IUtil.LoadSkyBoxes();
		}

		// Token: 0x06000D8D RID: 3469 RVA: 0x0000F2F2 File Offset: 0x0000D4F2
		public static string GetApplicationName()
		{
			return EngineApplicationInterface.IUtil.GetApplicationName();
		}

		// Token: 0x06000D8E RID: 3470 RVA: 0x0000F2FE File Offset: 0x0000D4FE
		public static void OpenNavalDlcPurchasePage()
		{
			EngineApplicationInterface.IUtil.OpenNavalDlcPurchasePage();
		}

		// Token: 0x06000D8F RID: 3471 RVA: 0x0000F30A File Offset: 0x0000D50A
		public static void SetWindowTitle(string title)
		{
			EngineApplicationInterface.IUtil.SetWindowTitle(title);
		}

		// Token: 0x06000D90 RID: 3472 RVA: 0x0000F317 File Offset: 0x0000D517
		public static string ProcessWindowTitle(string title)
		{
			return EngineApplicationInterface.IUtil.ProcessWindowTitle(title);
		}

		// Token: 0x06000D91 RID: 3473 RVA: 0x0000F324 File Offset: 0x0000D524
		public static uint GetCurrentProcessID()
		{
			return EngineApplicationInterface.IUtil.GetCurrentProcessID();
		}

		// Token: 0x06000D92 RID: 3474 RVA: 0x0000F330 File Offset: 0x0000D530
		public static void DoDelayedexit(int returnCode)
		{
			EngineApplicationInterface.IUtil.DoDelayedexit(returnCode);
		}

		// Token: 0x06000D93 RID: 3475 RVA: 0x0000F33D File Offset: 0x0000D53D
		public static void SetAssertionsAndWarningsSetExitCode(bool value)
		{
			EngineApplicationInterface.IUtil.SetAssertionsAndWarningsSetExitCode(value);
		}

		// Token: 0x06000D94 RID: 3476 RVA: 0x0000F34A File Offset: 0x0000D54A
		public static void SetReportMode(bool reportMode)
		{
			EngineApplicationInterface.IUtil.SetReportMode(reportMode);
		}

		// Token: 0x06000D95 RID: 3477 RVA: 0x0000F357 File Offset: 0x0000D557
		public static void SetAssertionAtShaderCompile(bool value)
		{
			EngineApplicationInterface.IUtil.SetAssertionAtShaderCompile(value);
		}

		// Token: 0x06000D96 RID: 3478 RVA: 0x0000F364 File Offset: 0x0000D564
		public static void SetCrashReportCustomString(string customString)
		{
			EngineApplicationInterface.IUtil.SetCrashReportCustomString(customString);
		}

		// Token: 0x06000D97 RID: 3479 RVA: 0x0000F371 File Offset: 0x0000D571
		public static void SetCrashReportCustomStack(string customStack)
		{
			EngineApplicationInterface.IUtil.SetCrashReportCustomStack(customStack);
		}

		// Token: 0x06000D98 RID: 3480 RVA: 0x0000F37E File Offset: 0x0000D57E
		public static int GetSteamAppId()
		{
			return EngineApplicationInterface.IUtil.GetSteamAppId();
		}

		// Token: 0x06000D99 RID: 3481 RVA: 0x0000F38A File Offset: 0x0000D58A
		public static void SetForceVsync(bool value)
		{
			Debug.Print("Force VSync State is now " + (value ? "ACTIVE" : "DEACTIVATED"), 0, Debug.DebugColor.DarkBlue, 17592186044416UL);
			EngineApplicationInterface.IUtil.SetForceVsync(value);
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x06000D9A RID: 3482 RVA: 0x0000F3C0 File Offset: 0x0000D5C0
		private static PlatformFilePath DefaultBannerlordConfigFullPath
		{
			get
			{
				return new PlatformFilePath(EngineFilePaths.ConfigsPath, "BannerlordConfig.txt");
			}
		}

		// Token: 0x06000D9B RID: 3483 RVA: 0x0000F3D4 File Offset: 0x0000D5D4
		public static string LoadBannerlordConfigFile()
		{
			PlatformFilePath defaultBannerlordConfigFullPath = Utilities.DefaultBannerlordConfigFullPath;
			if (!FileHelper.FileExists(defaultBannerlordConfigFullPath))
			{
				return "";
			}
			return FileHelper.GetFileContentString(defaultBannerlordConfigFullPath);
		}

		// Token: 0x06000D9C RID: 3484 RVA: 0x0000F3FC File Offset: 0x0000D5FC
		public static SaveResult SaveConfigFile(string configProperties)
		{
			PlatformFilePath defaultBannerlordConfigFullPath = Utilities.DefaultBannerlordConfigFullPath;
			SaveResult result;
			try
			{
				string data = configProperties.Substring(0, configProperties.Length - 1);
				FileHelper.SaveFileString(defaultBannerlordConfigFullPath, data);
				result = SaveResult.Success;
			}
			catch
			{
				Debug.Print("Could not create Bannerlord Config file", 0, Debug.DebugColor.White, 17592186044416UL);
				result = SaveResult.ConfigFileFailure;
			}
			return result;
		}

		// Token: 0x06000D9D RID: 3485 RVA: 0x0000F458 File Offset: 0x0000D658
		public static void OpenOnscreenKeyboard(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
		{
			EngineApplicationInterface.IUtil.OpenOnscreenKeyboard(initialText, descriptionText, maxLength, keyboardTypeEnum);
		}

		// Token: 0x06000D9E RID: 3486 RVA: 0x0000F468 File Offset: 0x0000D668
		public static string GetSystemLanguage()
		{
			return EngineApplicationInterface.IUtil.GetSystemLanguage();
		}

		// Token: 0x06000D9F RID: 3487 RVA: 0x0000F474 File Offset: 0x0000D674
		public static int RegisterGPUAllocationGroup(string name)
		{
			return EngineApplicationInterface.IUtil.RegisterGPUAllocationGroup(name);
		}

		// Token: 0x06000DA0 RID: 3488 RVA: 0x0000F481 File Offset: 0x0000D681
		public static int GetMemoryUsageOfCategory(int category)
		{
			return EngineApplicationInterface.IUtil.GetMemoryUsageOfCategory(category);
		}

		// Token: 0x06000DA1 RID: 3489 RVA: 0x0000F48E File Offset: 0x0000D68E
		public static string GetDetailedXBOXMemoryInfo()
		{
			return EngineApplicationInterface.IUtil.GetDetailedXBOXMemoryInfo();
		}

		// Token: 0x06000DA2 RID: 3490 RVA: 0x0000F49A File Offset: 0x0000D69A
		public static void SetFrameLimiterWithSleep(bool value)
		{
			EngineApplicationInterface.IUtil.SetFrameLimiterWithSleep(value);
		}

		// Token: 0x06000DA3 RID: 3491 RVA: 0x0000F4A7 File Offset: 0x0000D6A7
		public static bool GetFrameLimiterWithSleep()
		{
			return EngineApplicationInterface.IUtil.GetFrameLimiterWithSleep();
		}

		// Token: 0x06000DA4 RID: 3492 RVA: 0x0000F4B3 File Offset: 0x0000D6B3
		public static string GetPossibleCommandLineStartingWith(string command, int index)
		{
			return EngineApplicationInterface.IUtil.GetPossibleCommandLineStartingWith(command, index);
		}

		// Token: 0x06000DA5 RID: 3493 RVA: 0x0000F4C1 File Offset: 0x0000D6C1
		public static bool IsDevkit()
		{
			return EngineApplicationInterface.IUtil.IsDevkit();
		}

		// Token: 0x06000DA6 RID: 3494 RVA: 0x0000F4CD File Offset: 0x0000D6CD
		public static bool IsLockhartPlatform()
		{
			return EngineApplicationInterface.IUtil.IsLockhartPlatform();
		}

		// Token: 0x06000DA7 RID: 3495 RVA: 0x0000F4D9 File Offset: 0x0000D6D9
		public static int GetVertexBufferChunkSystemMemoryUsage()
		{
			return EngineApplicationInterface.IUtil.GetVertexBufferChunkSystemMemoryUsage();
		}

		// Token: 0x06000DA8 RID: 3496 RVA: 0x0000F4E5 File Offset: 0x0000D6E5
		public static int GetBuildNumber()
		{
			return EngineApplicationInterface.IUtil.GetBuildNumber();
		}

		// Token: 0x06000DA9 RID: 3497 RVA: 0x0000F4F1 File Offset: 0x0000D6F1
		public static ApplicationVersion GetApplicationVersionWithBuildNumber()
		{
			return ApplicationVersion.FromParametersFile(null);
		}

		// Token: 0x06000DAA RID: 3498 RVA: 0x0000F4F9 File Offset: 0x0000D6F9
		public static void ParallelFor(int startIndex, int endIndex, long curKey, int grainSize)
		{
			EngineApplicationInterface.IUtil.ManagedParallelFor(startIndex, endIndex, curKey, grainSize);
		}

		// Token: 0x06000DAB RID: 3499 RVA: 0x0000F509 File Offset: 0x0000D709
		public static void ParallelForWithoutRenderThread(int startIndex, int endIndex, long curKey, int grainSize)
		{
			EngineApplicationInterface.IUtil.ManagedParallelForWithoutRenderThread(startIndex, endIndex, curKey, grainSize);
		}

		// Token: 0x06000DAC RID: 3500 RVA: 0x0000F519 File Offset: 0x0000D719
		public static void ClearShaderMemory()
		{
			EngineApplicationInterface.IUtil.ClearShaderMemory();
		}

		// Token: 0x06000DAD RID: 3501 RVA: 0x0000F525 File Offset: 0x0000D725
		public static void RegisterMeshForGPUMorph(string metaMeshName)
		{
			EngineApplicationInterface.IUtil.RegisterMeshForGPUMorph(metaMeshName);
		}

		// Token: 0x06000DAE RID: 3502 RVA: 0x0000F532 File Offset: 0x0000D732
		public static void ParallelForWithDt(int startIndex, int endIndex, long curKey, int grainSize)
		{
			EngineApplicationInterface.IUtil.ManagedParallelForWithDt(startIndex, endIndex, curKey, grainSize);
		}

		// Token: 0x06000DAF RID: 3503 RVA: 0x0000F542 File Offset: 0x0000D742
		public static ulong GetMainThreadId()
		{
			return EngineApplicationInterface.IUtil.GetMainThreadId();
		}

		// Token: 0x06000DB0 RID: 3504 RVA: 0x0000F54E File Offset: 0x0000D74E
		public static ulong GetCurrentThreadId()
		{
			return EngineApplicationInterface.IUtil.GetCurrentThreadId();
		}

		// Token: 0x06000DB1 RID: 3505 RVA: 0x0000F55A File Offset: 0x0000D75A
		public static void SetWatchdogValue(string fileName, string groupName, string key, string value)
		{
			EngineApplicationInterface.IUtil.SetWatchdogValue(fileName, groupName, key, value);
		}

		// Token: 0x06000DB2 RID: 3506 RVA: 0x0000F56A File Offset: 0x0000D76A
		public static void SetWatchdogAutoreport(bool enabled)
		{
			EngineApplicationInterface.IUtil.SetWatchdogAutoreport(enabled);
		}

		// Token: 0x06000DB3 RID: 3507 RVA: 0x0000F577 File Offset: 0x0000D777
		public static void DetachWatchdog()
		{
			EngineApplicationInterface.IUtil.DetachWatchdog();
		}

		// Token: 0x06000DB4 RID: 3508 RVA: 0x0000F583 File Offset: 0x0000D783
		public static string GetPlatformModulePaths()
		{
			return EngineApplicationInterface.IUtil.GetPlatformModulePaths();
		}

		// Token: 0x06000DB5 RID: 3509 RVA: 0x0000F58F File Offset: 0x0000D78F
		public static bool IsAsyncPhysicsThread()
		{
			return EngineApplicationInterface.IUtil.IsAsyncPhysicsThread();
		}

		// Token: 0x06000DB6 RID: 3510 RVA: 0x0000F59B File Offset: 0x0000D79B
		public static void StartLoadingStuckCheckState(float timeoutThresholdSeconds)
		{
			EngineApplicationInterface.IUtil.StartLoadingStuckCheckState(timeoutThresholdSeconds);
		}

		// Token: 0x06000DB7 RID: 3511 RVA: 0x0000F5A8 File Offset: 0x0000D7A8
		public static void EndLoadingStuckCheckState()
		{
			EngineApplicationInterface.IUtil.EndLoadingStuckCheckState();
		}

		// Token: 0x040001FF RID: 511
		private static ConcurrentQueue<Utilities.MainThreadJob> jobs = new ConcurrentQueue<Utilities.MainThreadJob>();

		// Token: 0x04000200 RID: 512
		public static bool renderingActive = true;

		// Token: 0x020000D3 RID: 211
		public enum EngineRenderDisplayMode
		{
			// Token: 0x04000449 RID: 1097
			ShowNone,
			// Token: 0x0400044A RID: 1098
			ShowAlbedo,
			// Token: 0x0400044B RID: 1099
			ShowNormals,
			// Token: 0x0400044C RID: 1100
			ShowVertexNormals,
			// Token: 0x0400044D RID: 1101
			ShowSpecular,
			// Token: 0x0400044E RID: 1102
			ShowGloss,
			// Token: 0x0400044F RID: 1103
			ShowOcclusion,
			// Token: 0x04000450 RID: 1104
			ShowGbufferShadowMask,
			// Token: 0x04000451 RID: 1105
			ShowTranslucency,
			// Token: 0x04000452 RID: 1106
			ShowMotionVector,
			// Token: 0x04000453 RID: 1107
			ShowVertexColor,
			// Token: 0x04000454 RID: 1108
			ShowDepth,
			// Token: 0x04000455 RID: 1109
			ShowTiledLightOverdraw,
			// Token: 0x04000456 RID: 1110
			ShowTiledDecalOverdraw,
			// Token: 0x04000457 RID: 1111
			ShowMeshId,
			// Token: 0x04000458 RID: 1112
			ShowDisableSunLighting,
			// Token: 0x04000459 RID: 1113
			ShowDebugTexture,
			// Token: 0x0400045A RID: 1114
			ShowTextureDensity,
			// Token: 0x0400045B RID: 1115
			ShowOverdraw,
			// Token: 0x0400045C RID: 1116
			ShowVsComplexity,
			// Token: 0x0400045D RID: 1117
			ShowPsComplexity,
			// Token: 0x0400045E RID: 1118
			ShowDisableAmbientLighting,
			// Token: 0x0400045F RID: 1119
			ShowEntityId,
			// Token: 0x04000460 RID: 1120
			ShowPrtDiffuseAmbient,
			// Token: 0x04000461 RID: 1121
			ShowLightDebugMode,
			// Token: 0x04000462 RID: 1122
			ShowParticleShadingAtlas,
			// Token: 0x04000463 RID: 1123
			ShowTerrainAngle,
			// Token: 0x04000464 RID: 1124
			ShowParallaxDebug,
			// Token: 0x04000465 RID: 1125
			ShowAlbedoValidation,
			// Token: 0x04000466 RID: 1126
			NumDebugModes
		}

		// Token: 0x020000D4 RID: 212
		private class MainThreadJob
		{
			// Token: 0x06001006 RID: 4102 RVA: 0x000149EB File Offset: 0x00012BEB
			internal MainThreadJob(Delegate function, object[] parameters)
			{
				this._function = function;
				this._parameters = parameters;
				this.wait_handle = null;
			}

			// Token: 0x06001007 RID: 4103 RVA: 0x00014A08 File Offset: 0x00012C08
			internal MainThreadJob(Semaphore sema, Delegate function, object[] parameters)
			{
				this._function = function;
				this._parameters = parameters;
				this.wait_handle = sema;
			}

			// Token: 0x06001008 RID: 4104 RVA: 0x00014A25 File Offset: 0x00012C25
			internal void Invoke()
			{
				this._function.DynamicInvoke(this._parameters);
				if (this.wait_handle != null)
				{
					this.wait_handle.Release();
				}
			}

			// Token: 0x04000467 RID: 1127
			private Delegate _function;

			// Token: 0x04000468 RID: 1128
			private object[] _parameters;

			// Token: 0x04000469 RID: 1129
			private Semaphore wait_handle;
		}

		// Token: 0x020000D5 RID: 213
		public class MainThreadPerformanceQuery : IDisposable
		{
			// Token: 0x06001009 RID: 4105 RVA: 0x00014A4D File Offset: 0x00012C4D
			public MainThreadPerformanceQuery(string parent, string name)
			{
				this._name = name;
				this._parent = parent;
				this._stopWatch = new Stopwatch();
				this._stopWatch.Start();
			}

			// Token: 0x0600100A RID: 4106 RVA: 0x00014A7C File Offset: 0x00012C7C
			public void Dispose()
			{
				this._stopWatch.Stop();
				float num = (float)this._stopWatch.Elapsed.TotalMilliseconds;
				num /= 1000f;
				EngineApplicationInterface.IUtil.AddMainThreadPerformanceQuery(this._parent, this._name, num);
			}

			// Token: 0x0400046A RID: 1130
			private string _name;

			// Token: 0x0400046B RID: 1131
			private string _parent;

			// Token: 0x0400046C RID: 1132
			private Stopwatch _stopWatch;
		}
	}
}
