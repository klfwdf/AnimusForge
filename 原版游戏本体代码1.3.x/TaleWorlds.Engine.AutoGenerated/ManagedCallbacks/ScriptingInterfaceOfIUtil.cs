using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000030 RID: 48
	internal class ScriptingInterfaceOfIUtil : IUtil
	{
		// Token: 0x06000634 RID: 1588 RVA: 0x00019F94 File Offset: 0x00018194
		public void AddCommandLineFunction(string concatName)
		{
			byte[] array = null;
			if (concatName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(concatName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(concatName, 0, concatName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_AddCommandLineFunctionDelegate(array);
		}

		// Token: 0x06000635 RID: 1589 RVA: 0x00019FF0 File Offset: 0x000181F0
		public void AddMainThreadPerformanceQuery(string parent, string name, float seconds)
		{
			byte[] array = null;
			if (parent != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(parent);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(parent, 0, parent.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (name != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(name);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(name, 0, name.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_AddMainThreadPerformanceQueryDelegate(array, array2, seconds);
		}

		// Token: 0x06000636 RID: 1590 RVA: 0x0001A090 File Offset: 0x00018290
		public void AddPerformanceReportToken(string performance_type, string name, float loading_time)
		{
			byte[] array = null;
			if (performance_type != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(performance_type);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(performance_type, 0, performance_type.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (name != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(name);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(name, 0, name.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_AddPerformanceReportTokenDelegate(array, array2, loading_time);
		}

		// Token: 0x06000637 RID: 1591 RVA: 0x0001A130 File Offset: 0x00018330
		public void AddSceneObjectReport(string scene_name, string report_name, float report_value)
		{
			byte[] array = null;
			if (scene_name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(scene_name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(scene_name, 0, scene_name.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (report_name != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(report_name);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(report_name, 0, report_name.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_AddSceneObjectReportDelegate(array, array2, report_value);
		}

		// Token: 0x06000638 RID: 1592 RVA: 0x0001A1CE File Offset: 0x000183CE
		public void CheckIfAssetsAndSourcesAreSame()
		{
			ScriptingInterfaceOfIUtil.call_CheckIfAssetsAndSourcesAreSameDelegate();
		}

		// Token: 0x06000639 RID: 1593 RVA: 0x0001A1DA File Offset: 0x000183DA
		public bool CheckIfTerrainShaderHeaderGenerationFinished()
		{
			return ScriptingInterfaceOfIUtil.call_CheckIfTerrainShaderHeaderGenerationFinishedDelegate();
		}

		// Token: 0x0600063A RID: 1594 RVA: 0x0001A1E6 File Offset: 0x000183E6
		public void CheckResourceModifications()
		{
			ScriptingInterfaceOfIUtil.call_CheckResourceModificationsDelegate();
		}

		// Token: 0x0600063B RID: 1595 RVA: 0x0001A1F4 File Offset: 0x000183F4
		public void CheckSceneForProblems(string path)
		{
			byte[] array = null;
			if (path != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(path);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(path, 0, path.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_CheckSceneForProblemsDelegate(array);
		}

		// Token: 0x0600063C RID: 1596 RVA: 0x0001A24E File Offset: 0x0001844E
		public bool CheckShaderCompilation()
		{
			return ScriptingInterfaceOfIUtil.call_CheckShaderCompilationDelegate();
		}

		// Token: 0x0600063D RID: 1597 RVA: 0x0001A25A File Offset: 0x0001845A
		public void clear_decal_atlas(DecalAtlasGroup atlasGroup)
		{
			ScriptingInterfaceOfIUtil.call_clear_decal_atlasDelegate(atlasGroup);
		}

		// Token: 0x0600063E RID: 1598 RVA: 0x0001A267 File Offset: 0x00018467
		public void ClearOldResourcesAndObjects()
		{
			ScriptingInterfaceOfIUtil.call_ClearOldResourcesAndObjectsDelegate();
		}

		// Token: 0x0600063F RID: 1599 RVA: 0x0001A273 File Offset: 0x00018473
		public void ClearShaderMemory()
		{
			ScriptingInterfaceOfIUtil.call_ClearShaderMemoryDelegate();
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x0001A280 File Offset: 0x00018480
		public bool CommandLineArgumentExists(string str)
		{
			byte[] array = null;
			if (str != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(str);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(str, 0, str.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIUtil.call_CommandLineArgumentExistsDelegate(array);
		}

		// Token: 0x06000641 RID: 1601 RVA: 0x0001A2DC File Offset: 0x000184DC
		public void CompileAllShaders(string targetPlatform)
		{
			byte[] array = null;
			if (targetPlatform != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(targetPlatform);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(targetPlatform, 0, targetPlatform.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_CompileAllShadersDelegate(array);
		}

		// Token: 0x06000642 RID: 1602 RVA: 0x0001A338 File Offset: 0x00018538
		public void CompileTerrainShadersDist(string targetPlatform, string targetConfig, string output_path)
		{
			byte[] array = null;
			if (targetPlatform != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(targetPlatform);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(targetPlatform, 0, targetPlatform.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (targetConfig != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(targetConfig);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(targetConfig, 0, targetConfig.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			byte[] array3 = null;
			if (output_path != null)
			{
				int byteCount3 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(output_path);
				array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(output_path, 0, output_path.Length, array3, 0);
				array3[byteCount3] = 0;
			}
			ScriptingInterfaceOfIUtil.call_CompileTerrainShadersDistDelegate(array, array2, array3);
		}

		// Token: 0x06000643 RID: 1603 RVA: 0x0001A420 File Offset: 0x00018620
		public void CreateSelectionInEditor(UIntPtr[] gameEntities, int entityCount, string name)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntities, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_CreateSelectionInEditorDelegate(pointer, entityCount, array);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000644 RID: 1604 RVA: 0x0001A494 File Offset: 0x00018694
		public void DebugSetGlobalLoadingWindowState(bool s)
		{
			ScriptingInterfaceOfIUtil.call_DebugSetGlobalLoadingWindowStateDelegate(s);
		}

		// Token: 0x06000645 RID: 1605 RVA: 0x0001A4A4 File Offset: 0x000186A4
		public void DeleteEntitiesInEditorScene(UIntPtr[] gameEntities, int entityCount)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntities, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIUtil.call_DeleteEntitiesInEditorSceneDelegate(pointer, entityCount);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000646 RID: 1606 RVA: 0x0001A4D5 File Offset: 0x000186D5
		public void DetachWatchdog()
		{
			ScriptingInterfaceOfIUtil.call_DetachWatchdogDelegate();
		}

		// Token: 0x06000647 RID: 1607 RVA: 0x0001A4E1 File Offset: 0x000186E1
		public bool DidAutomatedGIBakeFinished()
		{
			return ScriptingInterfaceOfIUtil.call_DidAutomatedGIBakeFinishedDelegate();
		}

		// Token: 0x06000648 RID: 1608 RVA: 0x0001A4ED File Offset: 0x000186ED
		public void DisableCoreGame()
		{
			ScriptingInterfaceOfIUtil.call_DisableCoreGameDelegate();
		}

		// Token: 0x06000649 RID: 1609 RVA: 0x0001A4F9 File Offset: 0x000186F9
		public void DisableGlobalEditDataCacher()
		{
			ScriptingInterfaceOfIUtil.call_DisableGlobalEditDataCacherDelegate();
		}

		// Token: 0x0600064A RID: 1610 RVA: 0x0001A505 File Offset: 0x00018705
		public void DisableGlobalLoadingWindow()
		{
			ScriptingInterfaceOfIUtil.call_DisableGlobalLoadingWindowDelegate();
		}

		// Token: 0x0600064B RID: 1611 RVA: 0x0001A511 File Offset: 0x00018711
		public void DoDelayedexit(int returnCode)
		{
			ScriptingInterfaceOfIUtil.call_DoDelayedexitDelegate(returnCode);
		}

		// Token: 0x0600064C RID: 1612 RVA: 0x0001A520 File Offset: 0x00018720
		public void DoFullBakeAllLevelsAutomated(string module, string sceneName)
		{
			byte[] array = null;
			if (module != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(module);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(module, 0, module.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (sceneName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(sceneName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_DoFullBakeAllLevelsAutomatedDelegate(array, array2);
		}

		// Token: 0x0600064D RID: 1613 RVA: 0x0001A5C0 File Offset: 0x000187C0
		public void DoFullBakeSingleLevelAutomated(string module, string sceneName)
		{
			byte[] array = null;
			if (module != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(module);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(module, 0, module.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (sceneName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(sceneName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_DoFullBakeSingleLevelAutomatedDelegate(array, array2);
		}

		// Token: 0x0600064E RID: 1614 RVA: 0x0001A660 File Offset: 0x00018860
		public void DoLightOnlyBakeAllLevelsAutomated(string module, string sceneName)
		{
			byte[] array = null;
			if (module != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(module);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(module, 0, module.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (sceneName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(sceneName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_DoLightOnlyBakeAllLevelsAutomatedDelegate(array, array2);
		}

		// Token: 0x0600064F RID: 1615 RVA: 0x0001A700 File Offset: 0x00018900
		public void DoLightOnlyBakeSingleLevelAutomated(string module, string sceneName)
		{
			byte[] array = null;
			if (module != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(module);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(module, 0, module.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (sceneName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(sceneName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(sceneName, 0, sceneName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_DoLightOnlyBakeSingleLevelAutomatedDelegate(array, array2);
		}

		// Token: 0x06000650 RID: 1616 RVA: 0x0001A7A0 File Offset: 0x000189A0
		public void DumpGPUMemoryStatistics(string filePath)
		{
			byte[] array = null;
			if (filePath != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(filePath);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(filePath, 0, filePath.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_DumpGPUMemoryStatisticsDelegate(array);
		}

		// Token: 0x06000651 RID: 1617 RVA: 0x0001A7FA File Offset: 0x000189FA
		public void EnableGlobalEditDataCacher()
		{
			ScriptingInterfaceOfIUtil.call_EnableGlobalEditDataCacherDelegate();
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x0001A806 File Offset: 0x00018A06
		public void EnableGlobalLoadingWindow()
		{
			ScriptingInterfaceOfIUtil.call_EnableGlobalLoadingWindowDelegate();
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x0001A812 File Offset: 0x00018A12
		public void EnableSingleGPUQueryPerFrame()
		{
			ScriptingInterfaceOfIUtil.call_EnableSingleGPUQueryPerFrameDelegate();
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x0001A81E File Offset: 0x00018A1E
		public void EndLoadingStuckCheckState()
		{
			ScriptingInterfaceOfIUtil.call_EndLoadingStuckCheckStateDelegate();
		}

		// Token: 0x06000655 RID: 1621 RVA: 0x0001A82C File Offset: 0x00018A2C
		public string ExecuteCommandLineCommand(string command)
		{
			byte[] array = null;
			if (command != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(command);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(command, 0, command.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_ExecuteCommandLineCommandDelegate(array) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000656 RID: 1622 RVA: 0x0001A890 File Offset: 0x00018A90
		public void ExitProcess(int exitCode)
		{
			ScriptingInterfaceOfIUtil.call_ExitProcessDelegate(exitCode);
		}

		// Token: 0x06000657 RID: 1623 RVA: 0x0001A8A0 File Offset: 0x00018AA0
		public string ExportNavMeshFaceMarks(string file_name)
		{
			byte[] array = null;
			if (file_name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(file_name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(file_name, 0, file_name.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_ExportNavMeshFaceMarksDelegate(array) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000658 RID: 1624 RVA: 0x0001A904 File Offset: 0x00018B04
		public void FindMeshesWithoutLods(string module_name)
		{
			byte[] array = null;
			if (module_name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(module_name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(module_name, 0, module_name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_FindMeshesWithoutLodsDelegate(array);
		}

		// Token: 0x06000659 RID: 1625 RVA: 0x0001A95E File Offset: 0x00018B5E
		public void FlushManagedObjectsMemory()
		{
			ScriptingInterfaceOfIUtil.call_FlushManagedObjectsMemoryDelegate();
		}

		// Token: 0x0600065A RID: 1626 RVA: 0x0001A96C File Offset: 0x00018B6C
		public void GatherCoreGameReferences(string scene_names)
		{
			byte[] array = null;
			if (scene_names != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(scene_names);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(scene_names, 0, scene_names.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_GatherCoreGameReferencesDelegate(array);
		}

		// Token: 0x0600065B RID: 1627 RVA: 0x0001A9C8 File Offset: 0x00018BC8
		public void GenerateTerrainShaderHeaders(string targetPlatform, string targetConfig, string output_path)
		{
			byte[] array = null;
			if (targetPlatform != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(targetPlatform);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(targetPlatform, 0, targetPlatform.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (targetConfig != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(targetConfig);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(targetConfig, 0, targetConfig.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			byte[] array3 = null;
			if (output_path != null)
			{
				int byteCount3 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(output_path);
				array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(output_path, 0, output_path.Length, array3, 0);
				array3[byteCount3] = 0;
			}
			ScriptingInterfaceOfIUtil.call_GenerateTerrainShaderHeadersDelegate(array, array2, array3);
		}

		// Token: 0x0600065C RID: 1628 RVA: 0x0001AAB0 File Offset: 0x00018CB0
		public float GetApplicationMemory()
		{
			return ScriptingInterfaceOfIUtil.call_GetApplicationMemoryDelegate();
		}

		// Token: 0x0600065D RID: 1629 RVA: 0x0001AABC File Offset: 0x00018CBC
		public string GetApplicationMemoryStatistics()
		{
			if (ScriptingInterfaceOfIUtil.call_GetApplicationMemoryStatisticsDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600065E RID: 1630 RVA: 0x0001AAD2 File Offset: 0x00018CD2
		public string GetApplicationName()
		{
			if (ScriptingInterfaceOfIUtil.call_GetApplicationNameDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600065F RID: 1631 RVA: 0x0001AAE8 File Offset: 0x00018CE8
		public string GetAttachmentsPath()
		{
			if (ScriptingInterfaceOfIUtil.call_GetAttachmentsPathDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000660 RID: 1632 RVA: 0x0001AAFE File Offset: 0x00018CFE
		public string GetBaseDirectory()
		{
			if (ScriptingInterfaceOfIUtil.call_GetBaseDirectoryDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000661 RID: 1633 RVA: 0x0001AB14 File Offset: 0x00018D14
		public int GetBenchmarkStatus()
		{
			return ScriptingInterfaceOfIUtil.call_GetBenchmarkStatusDelegate();
		}

		// Token: 0x06000662 RID: 1634 RVA: 0x0001AB20 File Offset: 0x00018D20
		public int GetBuildNumber()
		{
			return ScriptingInterfaceOfIUtil.call_GetBuildNumberDelegate();
		}

		// Token: 0x06000663 RID: 1635 RVA: 0x0001AB2C File Offset: 0x00018D2C
		public string GetConsoleHostMachine()
		{
			if (ScriptingInterfaceOfIUtil.call_GetConsoleHostMachineDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000664 RID: 1636 RVA: 0x0001AB42 File Offset: 0x00018D42
		public int GetCoreGameState()
		{
			return ScriptingInterfaceOfIUtil.call_GetCoreGameStateDelegate();
		}

		// Token: 0x06000665 RID: 1637 RVA: 0x0001AB4E File Offset: 0x00018D4E
		public ulong GetCurrentCpuMemoryUsage()
		{
			return ScriptingInterfaceOfIUtil.call_GetCurrentCpuMemoryUsageDelegate();
		}

		// Token: 0x06000666 RID: 1638 RVA: 0x0001AB5A File Offset: 0x00018D5A
		public int GetCurrentEstimatedGPUMemoryCostMB()
		{
			return ScriptingInterfaceOfIUtil.call_GetCurrentEstimatedGPUMemoryCostMBDelegate();
		}

		// Token: 0x06000667 RID: 1639 RVA: 0x0001AB66 File Offset: 0x00018D66
		public uint GetCurrentProcessID()
		{
			return ScriptingInterfaceOfIUtil.call_GetCurrentProcessIDDelegate();
		}

		// Token: 0x06000668 RID: 1640 RVA: 0x0001AB72 File Offset: 0x00018D72
		public ulong GetCurrentThreadId()
		{
			return ScriptingInterfaceOfIUtil.call_GetCurrentThreadIdDelegate();
		}

		// Token: 0x06000669 RID: 1641 RVA: 0x0001AB7E File Offset: 0x00018D7E
		public float GetDeltaTime(int timerId)
		{
			return ScriptingInterfaceOfIUtil.call_GetDeltaTimeDelegate(timerId);
		}

		// Token: 0x0600066A RID: 1642 RVA: 0x0001AB8B File Offset: 0x00018D8B
		public void GetDetailedGPUBufferMemoryStats(ref int totalMemoryAllocated, ref int totalMemoryUsed, ref int emptyChunkCount)
		{
			ScriptingInterfaceOfIUtil.call_GetDetailedGPUBufferMemoryStatsDelegate(ref totalMemoryAllocated, ref totalMemoryUsed, ref emptyChunkCount);
		}

		// Token: 0x0600066B RID: 1643 RVA: 0x0001AB9A File Offset: 0x00018D9A
		public string GetDetailedXBOXMemoryInfo()
		{
			if (ScriptingInterfaceOfIUtil.call_GetDetailedXBOXMemoryInfoDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600066C RID: 1644 RVA: 0x0001ABB0 File Offset: 0x00018DB0
		public void GetEditorSelectedEntities(UIntPtr[] gameEntitiesTemp)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntitiesTemp, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIUtil.call_GetEditorSelectedEntitiesDelegate(pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x0600066D RID: 1645 RVA: 0x0001ABE0 File Offset: 0x00018DE0
		public int GetEditorSelectedEntityCount()
		{
			return ScriptingInterfaceOfIUtil.call_GetEditorSelectedEntityCountDelegate();
		}

		// Token: 0x0600066E RID: 1646 RVA: 0x0001ABEC File Offset: 0x00018DEC
		public int GetEngineFrameNo()
		{
			return ScriptingInterfaceOfIUtil.call_GetEngineFrameNoDelegate();
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x0001ABF8 File Offset: 0x00018DF8
		public void GetEntitiesOfSelectionSet(string name, UIntPtr[] gameEntitiesTemp)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntitiesTemp, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIUtil.call_GetEntitiesOfSelectionSetDelegate(array, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x0001AC6C File Offset: 0x00018E6C
		public int GetEntityCountOfSelectionSet(string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIUtil.call_GetEntityCountOfSelectionSetDelegate(array);
		}

		// Token: 0x06000671 RID: 1649 RVA: 0x0001ACC6 File Offset: 0x00018EC6
		public string GetExecutableWorkingDirectory()
		{
			if (ScriptingInterfaceOfIUtil.call_GetExecutableWorkingDirectoryDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000672 RID: 1650 RVA: 0x0001ACDC File Offset: 0x00018EDC
		public float GetFps()
		{
			return ScriptingInterfaceOfIUtil.call_GetFpsDelegate();
		}

		// Token: 0x06000673 RID: 1651 RVA: 0x0001ACE8 File Offset: 0x00018EE8
		public bool GetFrameLimiterWithSleep()
		{
			return ScriptingInterfaceOfIUtil.call_GetFrameLimiterWithSleepDelegate();
		}

		// Token: 0x06000674 RID: 1652 RVA: 0x0001ACF4 File Offset: 0x00018EF4
		public string GetFullCommandLineString()
		{
			if (ScriptingInterfaceOfIUtil.call_GetFullCommandLineStringDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000675 RID: 1653 RVA: 0x0001AD0C File Offset: 0x00018F0C
		public string GetFullFilePathOfScene(string sceneName)
		{
			byte[] array = null;
			if (sceneName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(sceneName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_GetFullFilePathOfSceneDelegate(array) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000676 RID: 1654 RVA: 0x0001AD70 File Offset: 0x00018F70
		public string GetFullModulePath(string moduleName)
		{
			byte[] array = null;
			if (moduleName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(moduleName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(moduleName, 0, moduleName.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_GetFullModulePathDelegate(array) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000677 RID: 1655 RVA: 0x0001ADD4 File Offset: 0x00018FD4
		public string GetFullModulePaths()
		{
			if (ScriptingInterfaceOfIUtil.call_GetFullModulePathsDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000678 RID: 1656 RVA: 0x0001ADEA File Offset: 0x00018FEA
		public int GetGPUMemoryMB()
		{
			return ScriptingInterfaceOfIUtil.call_GetGPUMemoryMBDelegate();
		}

		// Token: 0x06000679 RID: 1657 RVA: 0x0001ADF8 File Offset: 0x00018FF8
		public ulong GetGpuMemoryOfAllocationGroup(string allocationName)
		{
			byte[] array = null;
			if (allocationName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(allocationName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(allocationName, 0, allocationName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIUtil.call_GetGpuMemoryOfAllocationGroupDelegate(array);
		}

		// Token: 0x0600067A RID: 1658 RVA: 0x0001AE52 File Offset: 0x00019052
		public void GetGPUMemoryStats(ref float totalMemory, ref float renderTargetMemory, ref float depthTargetMemory, ref float srvMemory, ref float bufferMemory)
		{
			ScriptingInterfaceOfIUtil.call_GetGPUMemoryStatsDelegate(ref totalMemory, ref renderTargetMemory, ref depthTargetMemory, ref srvMemory, ref bufferMemory);
		}

		// Token: 0x0600067B RID: 1659 RVA: 0x0001AE65 File Offset: 0x00019065
		public string GetLocalOutputPath()
		{
			if (ScriptingInterfaceOfIUtil.call_GetLocalOutputPathDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600067C RID: 1660 RVA: 0x0001AE7B File Offset: 0x0001907B
		public float GetMainFps()
		{
			return ScriptingInterfaceOfIUtil.call_GetMainFpsDelegate();
		}

		// Token: 0x0600067D RID: 1661 RVA: 0x0001AE87 File Offset: 0x00019087
		public ulong GetMainThreadId()
		{
			return ScriptingInterfaceOfIUtil.call_GetMainThreadIdDelegate();
		}

		// Token: 0x0600067E RID: 1662 RVA: 0x0001AE93 File Offset: 0x00019093
		public int GetMemoryUsageOfCategory(int index)
		{
			return ScriptingInterfaceOfIUtil.call_GetMemoryUsageOfCategoryDelegate(index);
		}

		// Token: 0x0600067F RID: 1663 RVA: 0x0001AEA0 File Offset: 0x000190A0
		public string GetModulesCode()
		{
			if (ScriptingInterfaceOfIUtil.call_GetModulesCodeDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000680 RID: 1664 RVA: 0x0001AEB6 File Offset: 0x000190B6
		public string GetNativeMemoryStatistics()
		{
			if (ScriptingInterfaceOfIUtil.call_GetNativeMemoryStatisticsDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000681 RID: 1665 RVA: 0x0001AECC File Offset: 0x000190CC
		public int GetNumberOfShaderCompilationsInProgress()
		{
			return ScriptingInterfaceOfIUtil.call_GetNumberOfShaderCompilationsInProgressDelegate();
		}

		// Token: 0x06000682 RID: 1666 RVA: 0x0001AED8 File Offset: 0x000190D8
		public string GetPCInfo()
		{
			if (ScriptingInterfaceOfIUtil.call_GetPCInfoDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000683 RID: 1667 RVA: 0x0001AEEE File Offset: 0x000190EE
		public string GetPlatformModulePaths()
		{
			if (ScriptingInterfaceOfIUtil.call_GetPlatformModulePathsDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000684 RID: 1668 RVA: 0x0001AF04 File Offset: 0x00019104
		public string GetPossibleCommandLineStartingWith(string command, int index)
		{
			byte[] array = null;
			if (command != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(command);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(command, 0, command.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_GetPossibleCommandLineStartingWithDelegate(array, index) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000685 RID: 1669 RVA: 0x0001AF69 File Offset: 0x00019169
		public float GetRendererFps()
		{
			return ScriptingInterfaceOfIUtil.call_GetRendererFpsDelegate();
		}

		// Token: 0x06000686 RID: 1670 RVA: 0x0001AF75 File Offset: 0x00019175
		public int GetReturnCode()
		{
			return ScriptingInterfaceOfIUtil.call_GetReturnCodeDelegate();
		}

		// Token: 0x06000687 RID: 1671 RVA: 0x0001AF84 File Offset: 0x00019184
		public string GetSingleModuleScenesOfModule(string moduleName)
		{
			byte[] array = null;
			if (moduleName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(moduleName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(moduleName, 0, moduleName.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_GetSingleModuleScenesOfModuleDelegate(array) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000688 RID: 1672 RVA: 0x0001AFE8 File Offset: 0x000191E8
		public int GetSteamAppId()
		{
			return ScriptingInterfaceOfIUtil.call_GetSteamAppIdDelegate();
		}

		// Token: 0x06000689 RID: 1673 RVA: 0x0001AFF4 File Offset: 0x000191F4
		public string GetSystemLanguage()
		{
			if (ScriptingInterfaceOfIUtil.call_GetSystemLanguageDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600068A RID: 1674 RVA: 0x0001B00A File Offset: 0x0001920A
		public int GetVertexBufferChunkSystemMemoryUsage()
		{
			return ScriptingInterfaceOfIUtil.call_GetVertexBufferChunkSystemMemoryUsageDelegate();
		}

		// Token: 0x0600068B RID: 1675 RVA: 0x0001B016 File Offset: 0x00019216
		public string GetVisualTestsTestFilesPath()
		{
			if (ScriptingInterfaceOfIUtil.call_GetVisualTestsTestFilesPathDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600068C RID: 1676 RVA: 0x0001B02C File Offset: 0x0001922C
		public string GetVisualTestsValidatePath()
		{
			if (ScriptingInterfaceOfIUtil.call_GetVisualTestsValidatePathDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600068D RID: 1677 RVA: 0x0001B042 File Offset: 0x00019242
		public bool IsAsyncPhysicsThread()
		{
			return ScriptingInterfaceOfIUtil.call_IsAsyncPhysicsThreadDelegate();
		}

		// Token: 0x0600068E RID: 1678 RVA: 0x0001B04E File Offset: 0x0001924E
		public bool IsBenchmarkQuited()
		{
			return ScriptingInterfaceOfIUtil.call_IsBenchmarkQuitedDelegate();
		}

		// Token: 0x0600068F RID: 1679 RVA: 0x0001B05A File Offset: 0x0001925A
		public int IsDetailedSoundLogOn()
		{
			return ScriptingInterfaceOfIUtil.call_IsDetailedSoundLogOnDelegate();
		}

		// Token: 0x06000690 RID: 1680 RVA: 0x0001B066 File Offset: 0x00019266
		public bool IsDevkit()
		{
			return ScriptingInterfaceOfIUtil.call_IsDevkitDelegate();
		}

		// Token: 0x06000691 RID: 1681 RVA: 0x0001B072 File Offset: 0x00019272
		public bool IsEditModeEnabled()
		{
			return ScriptingInterfaceOfIUtil.call_IsEditModeEnabledDelegate();
		}

		// Token: 0x06000692 RID: 1682 RVA: 0x0001B07E File Offset: 0x0001927E
		public bool IsLockhartPlatform()
		{
			return ScriptingInterfaceOfIUtil.call_IsLockhartPlatformDelegate();
		}

		// Token: 0x06000693 RID: 1683 RVA: 0x0001B08A File Offset: 0x0001928A
		public bool IsSceneReportFinished()
		{
			return ScriptingInterfaceOfIUtil.call_IsSceneReportFinishedDelegate();
		}

		// Token: 0x06000694 RID: 1684 RVA: 0x0001B096 File Offset: 0x00019296
		public void LoadSkyBoxes()
		{
			ScriptingInterfaceOfIUtil.call_LoadSkyBoxesDelegate();
		}

		// Token: 0x06000695 RID: 1685 RVA: 0x0001B0A4 File Offset: 0x000192A4
		public void LoadVirtualTextureTileset(string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_LoadVirtualTextureTilesetDelegate(array);
		}

		// Token: 0x06000696 RID: 1686 RVA: 0x0001B0FE File Offset: 0x000192FE
		public void ManagedParallelFor(int fromInclusive, int toExclusive, long curKey, int grainSize)
		{
			ScriptingInterfaceOfIUtil.call_ManagedParallelForDelegate(fromInclusive, toExclusive, curKey, grainSize);
		}

		// Token: 0x06000697 RID: 1687 RVA: 0x0001B10F File Offset: 0x0001930F
		public void ManagedParallelForWithDt(int fromInclusive, int toExclusive, long curKey, int grainSize)
		{
			ScriptingInterfaceOfIUtil.call_ManagedParallelForWithDtDelegate(fromInclusive, toExclusive, curKey, grainSize);
		}

		// Token: 0x06000698 RID: 1688 RVA: 0x0001B120 File Offset: 0x00019320
		public void ManagedParallelForWithoutRenderThread(int fromInclusive, int toExclusive, long curKey, int grainSize)
		{
			ScriptingInterfaceOfIUtil.call_ManagedParallelForWithoutRenderThreadDelegate(fromInclusive, toExclusive, curKey, grainSize);
		}

		// Token: 0x06000699 RID: 1689 RVA: 0x0001B131 File Offset: 0x00019331
		public void OnLoadingWindowDisabled()
		{
			ScriptingInterfaceOfIUtil.call_OnLoadingWindowDisabledDelegate();
		}

		// Token: 0x0600069A RID: 1690 RVA: 0x0001B13D File Offset: 0x0001933D
		public void OnLoadingWindowEnabled()
		{
			ScriptingInterfaceOfIUtil.call_OnLoadingWindowEnabledDelegate();
		}

		// Token: 0x0600069B RID: 1691 RVA: 0x0001B149 File Offset: 0x00019349
		public void OpenNavalDlcPurchasePage()
		{
			ScriptingInterfaceOfIUtil.call_OpenNavalDlcPurchasePageDelegate();
		}

		// Token: 0x0600069C RID: 1692 RVA: 0x0001B158 File Offset: 0x00019358
		public void OpenOnscreenKeyboard(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
		{
			byte[] array = null;
			if (initialText != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(initialText);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(initialText, 0, initialText.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (descriptionText != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(descriptionText);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(descriptionText, 0, descriptionText.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_OpenOnscreenKeyboardDelegate(array, array2, maxLength, keyboardTypeEnum);
		}

		// Token: 0x0600069D RID: 1693 RVA: 0x0001B1F8 File Offset: 0x000193F8
		public void OutputBenchmarkValuesToPerformanceReporter()
		{
			ScriptingInterfaceOfIUtil.call_OutputBenchmarkValuesToPerformanceReporterDelegate();
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x0001B204 File Offset: 0x00019404
		public void OutputPerformanceReports()
		{
			ScriptingInterfaceOfIUtil.call_OutputPerformanceReportsDelegate();
		}

		// Token: 0x0600069F RID: 1695 RVA: 0x0001B210 File Offset: 0x00019410
		public void PairSceneNameToModuleName(string sceneName, string moduleName)
		{
			byte[] array = null;
			if (sceneName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(sceneName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (moduleName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(moduleName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(moduleName, 0, moduleName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIUtil.call_PairSceneNameToModuleNameDelegate(array, array2);
		}

		// Token: 0x060006A0 RID: 1696 RVA: 0x0001B2B0 File Offset: 0x000194B0
		public string ProcessWindowTitle(string title)
		{
			byte[] array = null;
			if (title != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(title);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(title, 0, title.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_ProcessWindowTitleDelegate(array) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x060006A1 RID: 1697 RVA: 0x0001B314 File Offset: 0x00019514
		public void QuitGame()
		{
			ScriptingInterfaceOfIUtil.call_QuitGameDelegate();
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x0001B320 File Offset: 0x00019520
		public int RegisterGPUAllocationGroup(string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIUtil.call_RegisterGPUAllocationGroupDelegate(array);
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x0001B37C File Offset: 0x0001957C
		public void RegisterMeshForGPUMorph(string metaMeshName)
		{
			byte[] array = null;
			if (metaMeshName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(metaMeshName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(metaMeshName, 0, metaMeshName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_RegisterMeshForGPUMorphDelegate(array);
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x0001B3D8 File Offset: 0x000195D8
		public int SaveDataAsTexture(string path, int width, int height, float[] data)
		{
			byte[] array = null;
			if (path != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(path);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(path, 0, path.Length, array, 0);
				array[byteCount] = 0;
			}
			PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(data, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			int result = ScriptingInterfaceOfIUtil.call_SaveDataAsTextureDelegate(array, width, height, pointer);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x060006A5 RID: 1701 RVA: 0x0001B450 File Offset: 0x00019650
		public void SelectEntities(UIntPtr[] gameEntities, int entityCount)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntities, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIUtil.call_SelectEntitiesDelegate(pointer, entityCount);
			pinnedArrayData.Dispose();
		}

		// Token: 0x060006A6 RID: 1702 RVA: 0x0001B481 File Offset: 0x00019681
		public void SetAllocationAlwaysValidScene(UIntPtr scene)
		{
			ScriptingInterfaceOfIUtil.call_SetAllocationAlwaysValidSceneDelegate(scene);
		}

		// Token: 0x060006A7 RID: 1703 RVA: 0x0001B48E File Offset: 0x0001968E
		public void SetAssertionAtShaderCompile(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetAssertionAtShaderCompileDelegate(value);
		}

		// Token: 0x060006A8 RID: 1704 RVA: 0x0001B49B File Offset: 0x0001969B
		public void SetAssertionsAndWarningsSetExitCode(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetAssertionsAndWarningsSetExitCodeDelegate(value);
		}

		// Token: 0x060006A9 RID: 1705 RVA: 0x0001B4A8 File Offset: 0x000196A8
		public void SetBenchmarkStatus(int status, string def)
		{
			byte[] array = null;
			if (def != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(def);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(def, 0, def.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_SetBenchmarkStatusDelegate(status, array);
		}

		// Token: 0x060006AA RID: 1706 RVA: 0x0001B503 File Offset: 0x00019703
		public void SetCanLoadModules(bool canLoadModules)
		{
			ScriptingInterfaceOfIUtil.call_SetCanLoadModulesDelegate(canLoadModules);
		}

		// Token: 0x060006AB RID: 1707 RVA: 0x0001B510 File Offset: 0x00019710
		public void SetCoreGameState(int state)
		{
			ScriptingInterfaceOfIUtil.call_SetCoreGameStateDelegate(state);
		}

		// Token: 0x060006AC RID: 1708 RVA: 0x0001B51D File Offset: 0x0001971D
		public void SetCrashOnAsserts(bool val)
		{
			ScriptingInterfaceOfIUtil.call_SetCrashOnAssertsDelegate(val);
		}

		// Token: 0x060006AD RID: 1709 RVA: 0x0001B52A File Offset: 0x0001972A
		public void SetCrashOnWarnings(bool val)
		{
			ScriptingInterfaceOfIUtil.call_SetCrashOnWarningsDelegate(val);
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x0001B538 File Offset: 0x00019738
		public void SetCrashReportCustomStack(string customStack)
		{
			byte[] array = null;
			if (customStack != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(customStack);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(customStack, 0, customStack.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_SetCrashReportCustomStackDelegate(array);
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x0001B594 File Offset: 0x00019794
		public void SetCrashReportCustomString(string customString)
		{
			byte[] array = null;
			if (customString != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(customString);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(customString, 0, customString.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_SetCrashReportCustomStringDelegate(array);
		}

		// Token: 0x060006B0 RID: 1712 RVA: 0x0001B5EE File Offset: 0x000197EE
		public void SetCreateDumpOnWarnings(bool val)
		{
			ScriptingInterfaceOfIUtil.call_SetCreateDumpOnWarningsDelegate(val);
		}

		// Token: 0x060006B1 RID: 1713 RVA: 0x0001B5FB File Offset: 0x000197FB
		public void SetDisableDumpGeneration(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetDisableDumpGenerationDelegate(value);
		}

		// Token: 0x060006B2 RID: 1714 RVA: 0x0001B608 File Offset: 0x00019808
		public void SetDumpFolderPath(string path)
		{
			byte[] array = null;
			if (path != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(path);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(path, 0, path.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_SetDumpFolderPathDelegate(array);
		}

		// Token: 0x060006B3 RID: 1715 RVA: 0x0001B662 File Offset: 0x00019862
		public void SetFixedDt(bool enabled, float dt)
		{
			ScriptingInterfaceOfIUtil.call_SetFixedDtDelegate(enabled, dt);
		}

		// Token: 0x060006B4 RID: 1716 RVA: 0x0001B670 File Offset: 0x00019870
		public void SetForceDrawEntityID(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetForceDrawEntityIDDelegate(value);
		}

		// Token: 0x060006B5 RID: 1717 RVA: 0x0001B67D File Offset: 0x0001987D
		public void SetForceVsync(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetForceVsyncDelegate(value);
		}

		// Token: 0x060006B6 RID: 1718 RVA: 0x0001B68A File Offset: 0x0001988A
		public void SetFrameLimiterWithSleep(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetFrameLimiterWithSleepDelegate(value);
		}

		// Token: 0x060006B7 RID: 1719 RVA: 0x0001B697 File Offset: 0x00019897
		public void SetGraphicsPreset(int preset)
		{
			ScriptingInterfaceOfIUtil.call_SetGraphicsPresetDelegate(preset);
		}

		// Token: 0x060006B8 RID: 1720 RVA: 0x0001B6A4 File Offset: 0x000198A4
		public void SetLoadingScreenPercentage(float value)
		{
			ScriptingInterfaceOfIUtil.call_SetLoadingScreenPercentageDelegate(value);
		}

		// Token: 0x060006B9 RID: 1721 RVA: 0x0001B6B1 File Offset: 0x000198B1
		public void SetMessageLineRenderingState(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetMessageLineRenderingStateDelegate(value);
		}

		// Token: 0x060006BA RID: 1722 RVA: 0x0001B6BE File Offset: 0x000198BE
		public void SetPrintCallstackAtCrahses(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetPrintCallstackAtCrahsesDelegate(value);
		}

		// Token: 0x060006BB RID: 1723 RVA: 0x0001B6CB File Offset: 0x000198CB
		public void SetRenderAgents(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetRenderAgentsDelegate(value);
		}

		// Token: 0x060006BC RID: 1724 RVA: 0x0001B6D8 File Offset: 0x000198D8
		public void SetRenderMode(int mode)
		{
			ScriptingInterfaceOfIUtil.call_SetRenderModeDelegate(mode);
		}

		// Token: 0x060006BD RID: 1725 RVA: 0x0001B6E5 File Offset: 0x000198E5
		public void SetReportMode(bool reportMode)
		{
			ScriptingInterfaceOfIUtil.call_SetReportModeDelegate(reportMode);
		}

		// Token: 0x060006BE RID: 1726 RVA: 0x0001B6F2 File Offset: 0x000198F2
		public void SetScreenTextRenderingState(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetScreenTextRenderingStateDelegate(value);
		}

		// Token: 0x060006BF RID: 1727 RVA: 0x0001B6FF File Offset: 0x000198FF
		public void SetWatchdogAutoreport(bool value)
		{
			ScriptingInterfaceOfIUtil.call_SetWatchdogAutoreportDelegate(value);
		}

		// Token: 0x060006C0 RID: 1728 RVA: 0x0001B70C File Offset: 0x0001990C
		public void SetWatchdogValue(string fileName, string groupName, string key, string value)
		{
			byte[] array = null;
			if (fileName != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(fileName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(fileName, 0, fileName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (groupName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(groupName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(groupName, 0, groupName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			byte[] array3 = null;
			if (key != null)
			{
				int byteCount3 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(key);
				array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(key, 0, key.Length, array3, 0);
				array3[byteCount3] = 0;
			}
			byte[] array4 = null;
			if (value != null)
			{
				int byteCount4 = ScriptingInterfaceOfIUtil._utf8.GetByteCount(value);
				array4 = ((byteCount4 < 1024) ? CallbackStringBufferManager.StringBuffer3 : new byte[byteCount4 + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(value, 0, value.Length, array4, 0);
				array4[byteCount4] = 0;
			}
			ScriptingInterfaceOfIUtil.call_SetWatchdogValueDelegate(array, array2, array3, array4);
		}

		// Token: 0x060006C1 RID: 1729 RVA: 0x0001B844 File Offset: 0x00019A44
		public void SetWindowTitle(string title)
		{
			byte[] array = null;
			if (title != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(title);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(title, 0, title.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_SetWindowTitleDelegate(array);
		}

		// Token: 0x060006C2 RID: 1730 RVA: 0x0001B89E File Offset: 0x00019A9E
		public void StartLoadingStuckCheckState(float seconds)
		{
			ScriptingInterfaceOfIUtil.call_StartLoadingStuckCheckStateDelegate(seconds);
		}

		// Token: 0x060006C3 RID: 1731 RVA: 0x0001B8AC File Offset: 0x00019AAC
		public void StartScenePerformanceReport(string folderPath)
		{
			byte[] array = null;
			if (folderPath != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(folderPath);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(folderPath, 0, folderPath.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_StartScenePerformanceReportDelegate(array);
		}

		// Token: 0x060006C4 RID: 1732 RVA: 0x0001B906 File Offset: 0x00019B06
		public void TakeScreenshotFromPlatformPath(PlatformFilePath path)
		{
			ScriptingInterfaceOfIUtil.call_TakeScreenshotFromPlatformPathDelegate(path);
		}

		// Token: 0x060006C5 RID: 1733 RVA: 0x0001B914 File Offset: 0x00019B14
		public void TakeScreenshotFromStringPath(string path)
		{
			byte[] array = null;
			if (path != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(path);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(path, 0, path.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIUtil.call_TakeScreenshotFromStringPathDelegate(array);
		}

		// Token: 0x060006C6 RID: 1734 RVA: 0x0001B970 File Offset: 0x00019B70
		public string TakeSSFromTop(string file_name)
		{
			byte[] array = null;
			if (file_name != null)
			{
				int byteCount = ScriptingInterfaceOfIUtil._utf8.GetByteCount(file_name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIUtil._utf8.GetBytes(file_name, 0, file_name.Length, array, 0);
				array[byteCount] = 0;
			}
			if (ScriptingInterfaceOfIUtil.call_TakeSSFromTopDelegate(array) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x060006C7 RID: 1735 RVA: 0x0001B9D4 File Offset: 0x00019BD4
		public void ToggleRender()
		{
			ScriptingInterfaceOfIUtil.call_ToggleRenderDelegate();
		}

		// Token: 0x04000581 RID: 1409
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000582 RID: 1410
		public static ScriptingInterfaceOfIUtil.AddCommandLineFunctionDelegate call_AddCommandLineFunctionDelegate;

		// Token: 0x04000583 RID: 1411
		public static ScriptingInterfaceOfIUtil.AddMainThreadPerformanceQueryDelegate call_AddMainThreadPerformanceQueryDelegate;

		// Token: 0x04000584 RID: 1412
		public static ScriptingInterfaceOfIUtil.AddPerformanceReportTokenDelegate call_AddPerformanceReportTokenDelegate;

		// Token: 0x04000585 RID: 1413
		public static ScriptingInterfaceOfIUtil.AddSceneObjectReportDelegate call_AddSceneObjectReportDelegate;

		// Token: 0x04000586 RID: 1414
		public static ScriptingInterfaceOfIUtil.CheckIfAssetsAndSourcesAreSameDelegate call_CheckIfAssetsAndSourcesAreSameDelegate;

		// Token: 0x04000587 RID: 1415
		public static ScriptingInterfaceOfIUtil.CheckIfTerrainShaderHeaderGenerationFinishedDelegate call_CheckIfTerrainShaderHeaderGenerationFinishedDelegate;

		// Token: 0x04000588 RID: 1416
		public static ScriptingInterfaceOfIUtil.CheckResourceModificationsDelegate call_CheckResourceModificationsDelegate;

		// Token: 0x04000589 RID: 1417
		public static ScriptingInterfaceOfIUtil.CheckSceneForProblemsDelegate call_CheckSceneForProblemsDelegate;

		// Token: 0x0400058A RID: 1418
		public static ScriptingInterfaceOfIUtil.CheckShaderCompilationDelegate call_CheckShaderCompilationDelegate;

		// Token: 0x0400058B RID: 1419
		public static ScriptingInterfaceOfIUtil.clear_decal_atlasDelegate call_clear_decal_atlasDelegate;

		// Token: 0x0400058C RID: 1420
		public static ScriptingInterfaceOfIUtil.ClearOldResourcesAndObjectsDelegate call_ClearOldResourcesAndObjectsDelegate;

		// Token: 0x0400058D RID: 1421
		public static ScriptingInterfaceOfIUtil.ClearShaderMemoryDelegate call_ClearShaderMemoryDelegate;

		// Token: 0x0400058E RID: 1422
		public static ScriptingInterfaceOfIUtil.CommandLineArgumentExistsDelegate call_CommandLineArgumentExistsDelegate;

		// Token: 0x0400058F RID: 1423
		public static ScriptingInterfaceOfIUtil.CompileAllShadersDelegate call_CompileAllShadersDelegate;

		// Token: 0x04000590 RID: 1424
		public static ScriptingInterfaceOfIUtil.CompileTerrainShadersDistDelegate call_CompileTerrainShadersDistDelegate;

		// Token: 0x04000591 RID: 1425
		public static ScriptingInterfaceOfIUtil.CreateSelectionInEditorDelegate call_CreateSelectionInEditorDelegate;

		// Token: 0x04000592 RID: 1426
		public static ScriptingInterfaceOfIUtil.DebugSetGlobalLoadingWindowStateDelegate call_DebugSetGlobalLoadingWindowStateDelegate;

		// Token: 0x04000593 RID: 1427
		public static ScriptingInterfaceOfIUtil.DeleteEntitiesInEditorSceneDelegate call_DeleteEntitiesInEditorSceneDelegate;

		// Token: 0x04000594 RID: 1428
		public static ScriptingInterfaceOfIUtil.DetachWatchdogDelegate call_DetachWatchdogDelegate;

		// Token: 0x04000595 RID: 1429
		public static ScriptingInterfaceOfIUtil.DidAutomatedGIBakeFinishedDelegate call_DidAutomatedGIBakeFinishedDelegate;

		// Token: 0x04000596 RID: 1430
		public static ScriptingInterfaceOfIUtil.DisableCoreGameDelegate call_DisableCoreGameDelegate;

		// Token: 0x04000597 RID: 1431
		public static ScriptingInterfaceOfIUtil.DisableGlobalEditDataCacherDelegate call_DisableGlobalEditDataCacherDelegate;

		// Token: 0x04000598 RID: 1432
		public static ScriptingInterfaceOfIUtil.DisableGlobalLoadingWindowDelegate call_DisableGlobalLoadingWindowDelegate;

		// Token: 0x04000599 RID: 1433
		public static ScriptingInterfaceOfIUtil.DoDelayedexitDelegate call_DoDelayedexitDelegate;

		// Token: 0x0400059A RID: 1434
		public static ScriptingInterfaceOfIUtil.DoFullBakeAllLevelsAutomatedDelegate call_DoFullBakeAllLevelsAutomatedDelegate;

		// Token: 0x0400059B RID: 1435
		public static ScriptingInterfaceOfIUtil.DoFullBakeSingleLevelAutomatedDelegate call_DoFullBakeSingleLevelAutomatedDelegate;

		// Token: 0x0400059C RID: 1436
		public static ScriptingInterfaceOfIUtil.DoLightOnlyBakeAllLevelsAutomatedDelegate call_DoLightOnlyBakeAllLevelsAutomatedDelegate;

		// Token: 0x0400059D RID: 1437
		public static ScriptingInterfaceOfIUtil.DoLightOnlyBakeSingleLevelAutomatedDelegate call_DoLightOnlyBakeSingleLevelAutomatedDelegate;

		// Token: 0x0400059E RID: 1438
		public static ScriptingInterfaceOfIUtil.DumpGPUMemoryStatisticsDelegate call_DumpGPUMemoryStatisticsDelegate;

		// Token: 0x0400059F RID: 1439
		public static ScriptingInterfaceOfIUtil.EnableGlobalEditDataCacherDelegate call_EnableGlobalEditDataCacherDelegate;

		// Token: 0x040005A0 RID: 1440
		public static ScriptingInterfaceOfIUtil.EnableGlobalLoadingWindowDelegate call_EnableGlobalLoadingWindowDelegate;

		// Token: 0x040005A1 RID: 1441
		public static ScriptingInterfaceOfIUtil.EnableSingleGPUQueryPerFrameDelegate call_EnableSingleGPUQueryPerFrameDelegate;

		// Token: 0x040005A2 RID: 1442
		public static ScriptingInterfaceOfIUtil.EndLoadingStuckCheckStateDelegate call_EndLoadingStuckCheckStateDelegate;

		// Token: 0x040005A3 RID: 1443
		public static ScriptingInterfaceOfIUtil.ExecuteCommandLineCommandDelegate call_ExecuteCommandLineCommandDelegate;

		// Token: 0x040005A4 RID: 1444
		public static ScriptingInterfaceOfIUtil.ExitProcessDelegate call_ExitProcessDelegate;

		// Token: 0x040005A5 RID: 1445
		public static ScriptingInterfaceOfIUtil.ExportNavMeshFaceMarksDelegate call_ExportNavMeshFaceMarksDelegate;

		// Token: 0x040005A6 RID: 1446
		public static ScriptingInterfaceOfIUtil.FindMeshesWithoutLodsDelegate call_FindMeshesWithoutLodsDelegate;

		// Token: 0x040005A7 RID: 1447
		public static ScriptingInterfaceOfIUtil.FlushManagedObjectsMemoryDelegate call_FlushManagedObjectsMemoryDelegate;

		// Token: 0x040005A8 RID: 1448
		public static ScriptingInterfaceOfIUtil.GatherCoreGameReferencesDelegate call_GatherCoreGameReferencesDelegate;

		// Token: 0x040005A9 RID: 1449
		public static ScriptingInterfaceOfIUtil.GenerateTerrainShaderHeadersDelegate call_GenerateTerrainShaderHeadersDelegate;

		// Token: 0x040005AA RID: 1450
		public static ScriptingInterfaceOfIUtil.GetApplicationMemoryDelegate call_GetApplicationMemoryDelegate;

		// Token: 0x040005AB RID: 1451
		public static ScriptingInterfaceOfIUtil.GetApplicationMemoryStatisticsDelegate call_GetApplicationMemoryStatisticsDelegate;

		// Token: 0x040005AC RID: 1452
		public static ScriptingInterfaceOfIUtil.GetApplicationNameDelegate call_GetApplicationNameDelegate;

		// Token: 0x040005AD RID: 1453
		public static ScriptingInterfaceOfIUtil.GetAttachmentsPathDelegate call_GetAttachmentsPathDelegate;

		// Token: 0x040005AE RID: 1454
		public static ScriptingInterfaceOfIUtil.GetBaseDirectoryDelegate call_GetBaseDirectoryDelegate;

		// Token: 0x040005AF RID: 1455
		public static ScriptingInterfaceOfIUtil.GetBenchmarkStatusDelegate call_GetBenchmarkStatusDelegate;

		// Token: 0x040005B0 RID: 1456
		public static ScriptingInterfaceOfIUtil.GetBuildNumberDelegate call_GetBuildNumberDelegate;

		// Token: 0x040005B1 RID: 1457
		public static ScriptingInterfaceOfIUtil.GetConsoleHostMachineDelegate call_GetConsoleHostMachineDelegate;

		// Token: 0x040005B2 RID: 1458
		public static ScriptingInterfaceOfIUtil.GetCoreGameStateDelegate call_GetCoreGameStateDelegate;

		// Token: 0x040005B3 RID: 1459
		public static ScriptingInterfaceOfIUtil.GetCurrentCpuMemoryUsageDelegate call_GetCurrentCpuMemoryUsageDelegate;

		// Token: 0x040005B4 RID: 1460
		public static ScriptingInterfaceOfIUtil.GetCurrentEstimatedGPUMemoryCostMBDelegate call_GetCurrentEstimatedGPUMemoryCostMBDelegate;

		// Token: 0x040005B5 RID: 1461
		public static ScriptingInterfaceOfIUtil.GetCurrentProcessIDDelegate call_GetCurrentProcessIDDelegate;

		// Token: 0x040005B6 RID: 1462
		public static ScriptingInterfaceOfIUtil.GetCurrentThreadIdDelegate call_GetCurrentThreadIdDelegate;

		// Token: 0x040005B7 RID: 1463
		public static ScriptingInterfaceOfIUtil.GetDeltaTimeDelegate call_GetDeltaTimeDelegate;

		// Token: 0x040005B8 RID: 1464
		public static ScriptingInterfaceOfIUtil.GetDetailedGPUBufferMemoryStatsDelegate call_GetDetailedGPUBufferMemoryStatsDelegate;

		// Token: 0x040005B9 RID: 1465
		public static ScriptingInterfaceOfIUtil.GetDetailedXBOXMemoryInfoDelegate call_GetDetailedXBOXMemoryInfoDelegate;

		// Token: 0x040005BA RID: 1466
		public static ScriptingInterfaceOfIUtil.GetEditorSelectedEntitiesDelegate call_GetEditorSelectedEntitiesDelegate;

		// Token: 0x040005BB RID: 1467
		public static ScriptingInterfaceOfIUtil.GetEditorSelectedEntityCountDelegate call_GetEditorSelectedEntityCountDelegate;

		// Token: 0x040005BC RID: 1468
		public static ScriptingInterfaceOfIUtil.GetEngineFrameNoDelegate call_GetEngineFrameNoDelegate;

		// Token: 0x040005BD RID: 1469
		public static ScriptingInterfaceOfIUtil.GetEntitiesOfSelectionSetDelegate call_GetEntitiesOfSelectionSetDelegate;

		// Token: 0x040005BE RID: 1470
		public static ScriptingInterfaceOfIUtil.GetEntityCountOfSelectionSetDelegate call_GetEntityCountOfSelectionSetDelegate;

		// Token: 0x040005BF RID: 1471
		public static ScriptingInterfaceOfIUtil.GetExecutableWorkingDirectoryDelegate call_GetExecutableWorkingDirectoryDelegate;

		// Token: 0x040005C0 RID: 1472
		public static ScriptingInterfaceOfIUtil.GetFpsDelegate call_GetFpsDelegate;

		// Token: 0x040005C1 RID: 1473
		public static ScriptingInterfaceOfIUtil.GetFrameLimiterWithSleepDelegate call_GetFrameLimiterWithSleepDelegate;

		// Token: 0x040005C2 RID: 1474
		public static ScriptingInterfaceOfIUtil.GetFullCommandLineStringDelegate call_GetFullCommandLineStringDelegate;

		// Token: 0x040005C3 RID: 1475
		public static ScriptingInterfaceOfIUtil.GetFullFilePathOfSceneDelegate call_GetFullFilePathOfSceneDelegate;

		// Token: 0x040005C4 RID: 1476
		public static ScriptingInterfaceOfIUtil.GetFullModulePathDelegate call_GetFullModulePathDelegate;

		// Token: 0x040005C5 RID: 1477
		public static ScriptingInterfaceOfIUtil.GetFullModulePathsDelegate call_GetFullModulePathsDelegate;

		// Token: 0x040005C6 RID: 1478
		public static ScriptingInterfaceOfIUtil.GetGPUMemoryMBDelegate call_GetGPUMemoryMBDelegate;

		// Token: 0x040005C7 RID: 1479
		public static ScriptingInterfaceOfIUtil.GetGpuMemoryOfAllocationGroupDelegate call_GetGpuMemoryOfAllocationGroupDelegate;

		// Token: 0x040005C8 RID: 1480
		public static ScriptingInterfaceOfIUtil.GetGPUMemoryStatsDelegate call_GetGPUMemoryStatsDelegate;

		// Token: 0x040005C9 RID: 1481
		public static ScriptingInterfaceOfIUtil.GetLocalOutputPathDelegate call_GetLocalOutputPathDelegate;

		// Token: 0x040005CA RID: 1482
		public static ScriptingInterfaceOfIUtil.GetMainFpsDelegate call_GetMainFpsDelegate;

		// Token: 0x040005CB RID: 1483
		public static ScriptingInterfaceOfIUtil.GetMainThreadIdDelegate call_GetMainThreadIdDelegate;

		// Token: 0x040005CC RID: 1484
		public static ScriptingInterfaceOfIUtil.GetMemoryUsageOfCategoryDelegate call_GetMemoryUsageOfCategoryDelegate;

		// Token: 0x040005CD RID: 1485
		public static ScriptingInterfaceOfIUtil.GetModulesCodeDelegate call_GetModulesCodeDelegate;

		// Token: 0x040005CE RID: 1486
		public static ScriptingInterfaceOfIUtil.GetNativeMemoryStatisticsDelegate call_GetNativeMemoryStatisticsDelegate;

		// Token: 0x040005CF RID: 1487
		public static ScriptingInterfaceOfIUtil.GetNumberOfShaderCompilationsInProgressDelegate call_GetNumberOfShaderCompilationsInProgressDelegate;

		// Token: 0x040005D0 RID: 1488
		public static ScriptingInterfaceOfIUtil.GetPCInfoDelegate call_GetPCInfoDelegate;

		// Token: 0x040005D1 RID: 1489
		public static ScriptingInterfaceOfIUtil.GetPlatformModulePathsDelegate call_GetPlatformModulePathsDelegate;

		// Token: 0x040005D2 RID: 1490
		public static ScriptingInterfaceOfIUtil.GetPossibleCommandLineStartingWithDelegate call_GetPossibleCommandLineStartingWithDelegate;

		// Token: 0x040005D3 RID: 1491
		public static ScriptingInterfaceOfIUtil.GetRendererFpsDelegate call_GetRendererFpsDelegate;

		// Token: 0x040005D4 RID: 1492
		public static ScriptingInterfaceOfIUtil.GetReturnCodeDelegate call_GetReturnCodeDelegate;

		// Token: 0x040005D5 RID: 1493
		public static ScriptingInterfaceOfIUtil.GetSingleModuleScenesOfModuleDelegate call_GetSingleModuleScenesOfModuleDelegate;

		// Token: 0x040005D6 RID: 1494
		public static ScriptingInterfaceOfIUtil.GetSteamAppIdDelegate call_GetSteamAppIdDelegate;

		// Token: 0x040005D7 RID: 1495
		public static ScriptingInterfaceOfIUtil.GetSystemLanguageDelegate call_GetSystemLanguageDelegate;

		// Token: 0x040005D8 RID: 1496
		public static ScriptingInterfaceOfIUtil.GetVertexBufferChunkSystemMemoryUsageDelegate call_GetVertexBufferChunkSystemMemoryUsageDelegate;

		// Token: 0x040005D9 RID: 1497
		public static ScriptingInterfaceOfIUtil.GetVisualTestsTestFilesPathDelegate call_GetVisualTestsTestFilesPathDelegate;

		// Token: 0x040005DA RID: 1498
		public static ScriptingInterfaceOfIUtil.GetVisualTestsValidatePathDelegate call_GetVisualTestsValidatePathDelegate;

		// Token: 0x040005DB RID: 1499
		public static ScriptingInterfaceOfIUtil.IsAsyncPhysicsThreadDelegate call_IsAsyncPhysicsThreadDelegate;

		// Token: 0x040005DC RID: 1500
		public static ScriptingInterfaceOfIUtil.IsBenchmarkQuitedDelegate call_IsBenchmarkQuitedDelegate;

		// Token: 0x040005DD RID: 1501
		public static ScriptingInterfaceOfIUtil.IsDetailedSoundLogOnDelegate call_IsDetailedSoundLogOnDelegate;

		// Token: 0x040005DE RID: 1502
		public static ScriptingInterfaceOfIUtil.IsDevkitDelegate call_IsDevkitDelegate;

		// Token: 0x040005DF RID: 1503
		public static ScriptingInterfaceOfIUtil.IsEditModeEnabledDelegate call_IsEditModeEnabledDelegate;

		// Token: 0x040005E0 RID: 1504
		public static ScriptingInterfaceOfIUtil.IsLockhartPlatformDelegate call_IsLockhartPlatformDelegate;

		// Token: 0x040005E1 RID: 1505
		public static ScriptingInterfaceOfIUtil.IsSceneReportFinishedDelegate call_IsSceneReportFinishedDelegate;

		// Token: 0x040005E2 RID: 1506
		public static ScriptingInterfaceOfIUtil.LoadSkyBoxesDelegate call_LoadSkyBoxesDelegate;

		// Token: 0x040005E3 RID: 1507
		public static ScriptingInterfaceOfIUtil.LoadVirtualTextureTilesetDelegate call_LoadVirtualTextureTilesetDelegate;

		// Token: 0x040005E4 RID: 1508
		public static ScriptingInterfaceOfIUtil.ManagedParallelForDelegate call_ManagedParallelForDelegate;

		// Token: 0x040005E5 RID: 1509
		public static ScriptingInterfaceOfIUtil.ManagedParallelForWithDtDelegate call_ManagedParallelForWithDtDelegate;

		// Token: 0x040005E6 RID: 1510
		public static ScriptingInterfaceOfIUtil.ManagedParallelForWithoutRenderThreadDelegate call_ManagedParallelForWithoutRenderThreadDelegate;

		// Token: 0x040005E7 RID: 1511
		public static ScriptingInterfaceOfIUtil.OnLoadingWindowDisabledDelegate call_OnLoadingWindowDisabledDelegate;

		// Token: 0x040005E8 RID: 1512
		public static ScriptingInterfaceOfIUtil.OnLoadingWindowEnabledDelegate call_OnLoadingWindowEnabledDelegate;

		// Token: 0x040005E9 RID: 1513
		public static ScriptingInterfaceOfIUtil.OpenNavalDlcPurchasePageDelegate call_OpenNavalDlcPurchasePageDelegate;

		// Token: 0x040005EA RID: 1514
		public static ScriptingInterfaceOfIUtil.OpenOnscreenKeyboardDelegate call_OpenOnscreenKeyboardDelegate;

		// Token: 0x040005EB RID: 1515
		public static ScriptingInterfaceOfIUtil.OutputBenchmarkValuesToPerformanceReporterDelegate call_OutputBenchmarkValuesToPerformanceReporterDelegate;

		// Token: 0x040005EC RID: 1516
		public static ScriptingInterfaceOfIUtil.OutputPerformanceReportsDelegate call_OutputPerformanceReportsDelegate;

		// Token: 0x040005ED RID: 1517
		public static ScriptingInterfaceOfIUtil.PairSceneNameToModuleNameDelegate call_PairSceneNameToModuleNameDelegate;

		// Token: 0x040005EE RID: 1518
		public static ScriptingInterfaceOfIUtil.ProcessWindowTitleDelegate call_ProcessWindowTitleDelegate;

		// Token: 0x040005EF RID: 1519
		public static ScriptingInterfaceOfIUtil.QuitGameDelegate call_QuitGameDelegate;

		// Token: 0x040005F0 RID: 1520
		public static ScriptingInterfaceOfIUtil.RegisterGPUAllocationGroupDelegate call_RegisterGPUAllocationGroupDelegate;

		// Token: 0x040005F1 RID: 1521
		public static ScriptingInterfaceOfIUtil.RegisterMeshForGPUMorphDelegate call_RegisterMeshForGPUMorphDelegate;

		// Token: 0x040005F2 RID: 1522
		public static ScriptingInterfaceOfIUtil.SaveDataAsTextureDelegate call_SaveDataAsTextureDelegate;

		// Token: 0x040005F3 RID: 1523
		public static ScriptingInterfaceOfIUtil.SelectEntitiesDelegate call_SelectEntitiesDelegate;

		// Token: 0x040005F4 RID: 1524
		public static ScriptingInterfaceOfIUtil.SetAllocationAlwaysValidSceneDelegate call_SetAllocationAlwaysValidSceneDelegate;

		// Token: 0x040005F5 RID: 1525
		public static ScriptingInterfaceOfIUtil.SetAssertionAtShaderCompileDelegate call_SetAssertionAtShaderCompileDelegate;

		// Token: 0x040005F6 RID: 1526
		public static ScriptingInterfaceOfIUtil.SetAssertionsAndWarningsSetExitCodeDelegate call_SetAssertionsAndWarningsSetExitCodeDelegate;

		// Token: 0x040005F7 RID: 1527
		public static ScriptingInterfaceOfIUtil.SetBenchmarkStatusDelegate call_SetBenchmarkStatusDelegate;

		// Token: 0x040005F8 RID: 1528
		public static ScriptingInterfaceOfIUtil.SetCanLoadModulesDelegate call_SetCanLoadModulesDelegate;

		// Token: 0x040005F9 RID: 1529
		public static ScriptingInterfaceOfIUtil.SetCoreGameStateDelegate call_SetCoreGameStateDelegate;

		// Token: 0x040005FA RID: 1530
		public static ScriptingInterfaceOfIUtil.SetCrashOnAssertsDelegate call_SetCrashOnAssertsDelegate;

		// Token: 0x040005FB RID: 1531
		public static ScriptingInterfaceOfIUtil.SetCrashOnWarningsDelegate call_SetCrashOnWarningsDelegate;

		// Token: 0x040005FC RID: 1532
		public static ScriptingInterfaceOfIUtil.SetCrashReportCustomStackDelegate call_SetCrashReportCustomStackDelegate;

		// Token: 0x040005FD RID: 1533
		public static ScriptingInterfaceOfIUtil.SetCrashReportCustomStringDelegate call_SetCrashReportCustomStringDelegate;

		// Token: 0x040005FE RID: 1534
		public static ScriptingInterfaceOfIUtil.SetCreateDumpOnWarningsDelegate call_SetCreateDumpOnWarningsDelegate;

		// Token: 0x040005FF RID: 1535
		public static ScriptingInterfaceOfIUtil.SetDisableDumpGenerationDelegate call_SetDisableDumpGenerationDelegate;

		// Token: 0x04000600 RID: 1536
		public static ScriptingInterfaceOfIUtil.SetDumpFolderPathDelegate call_SetDumpFolderPathDelegate;

		// Token: 0x04000601 RID: 1537
		public static ScriptingInterfaceOfIUtil.SetFixedDtDelegate call_SetFixedDtDelegate;

		// Token: 0x04000602 RID: 1538
		public static ScriptingInterfaceOfIUtil.SetForceDrawEntityIDDelegate call_SetForceDrawEntityIDDelegate;

		// Token: 0x04000603 RID: 1539
		public static ScriptingInterfaceOfIUtil.SetForceVsyncDelegate call_SetForceVsyncDelegate;

		// Token: 0x04000604 RID: 1540
		public static ScriptingInterfaceOfIUtil.SetFrameLimiterWithSleepDelegate call_SetFrameLimiterWithSleepDelegate;

		// Token: 0x04000605 RID: 1541
		public static ScriptingInterfaceOfIUtil.SetGraphicsPresetDelegate call_SetGraphicsPresetDelegate;

		// Token: 0x04000606 RID: 1542
		public static ScriptingInterfaceOfIUtil.SetLoadingScreenPercentageDelegate call_SetLoadingScreenPercentageDelegate;

		// Token: 0x04000607 RID: 1543
		public static ScriptingInterfaceOfIUtil.SetMessageLineRenderingStateDelegate call_SetMessageLineRenderingStateDelegate;

		// Token: 0x04000608 RID: 1544
		public static ScriptingInterfaceOfIUtil.SetPrintCallstackAtCrahsesDelegate call_SetPrintCallstackAtCrahsesDelegate;

		// Token: 0x04000609 RID: 1545
		public static ScriptingInterfaceOfIUtil.SetRenderAgentsDelegate call_SetRenderAgentsDelegate;

		// Token: 0x0400060A RID: 1546
		public static ScriptingInterfaceOfIUtil.SetRenderModeDelegate call_SetRenderModeDelegate;

		// Token: 0x0400060B RID: 1547
		public static ScriptingInterfaceOfIUtil.SetReportModeDelegate call_SetReportModeDelegate;

		// Token: 0x0400060C RID: 1548
		public static ScriptingInterfaceOfIUtil.SetScreenTextRenderingStateDelegate call_SetScreenTextRenderingStateDelegate;

		// Token: 0x0400060D RID: 1549
		public static ScriptingInterfaceOfIUtil.SetWatchdogAutoreportDelegate call_SetWatchdogAutoreportDelegate;

		// Token: 0x0400060E RID: 1550
		public static ScriptingInterfaceOfIUtil.SetWatchdogValueDelegate call_SetWatchdogValueDelegate;

		// Token: 0x0400060F RID: 1551
		public static ScriptingInterfaceOfIUtil.SetWindowTitleDelegate call_SetWindowTitleDelegate;

		// Token: 0x04000610 RID: 1552
		public static ScriptingInterfaceOfIUtil.StartLoadingStuckCheckStateDelegate call_StartLoadingStuckCheckStateDelegate;

		// Token: 0x04000611 RID: 1553
		public static ScriptingInterfaceOfIUtil.StartScenePerformanceReportDelegate call_StartScenePerformanceReportDelegate;

		// Token: 0x04000612 RID: 1554
		public static ScriptingInterfaceOfIUtil.TakeScreenshotFromPlatformPathDelegate call_TakeScreenshotFromPlatformPathDelegate;

		// Token: 0x04000613 RID: 1555
		public static ScriptingInterfaceOfIUtil.TakeScreenshotFromStringPathDelegate call_TakeScreenshotFromStringPathDelegate;

		// Token: 0x04000614 RID: 1556
		public static ScriptingInterfaceOfIUtil.TakeSSFromTopDelegate call_TakeSSFromTopDelegate;

		// Token: 0x04000615 RID: 1557
		public static ScriptingInterfaceOfIUtil.ToggleRenderDelegate call_ToggleRenderDelegate;

		// Token: 0x020005E0 RID: 1504
		// (Invoke) Token: 0x06001D93 RID: 7571
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddCommandLineFunctionDelegate(byte[] concatName);

		// Token: 0x020005E1 RID: 1505
		// (Invoke) Token: 0x06001D97 RID: 7575
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMainThreadPerformanceQueryDelegate(byte[] parent, byte[] name, float seconds);

		// Token: 0x020005E2 RID: 1506
		// (Invoke) Token: 0x06001D9B RID: 7579
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddPerformanceReportTokenDelegate(byte[] performance_type, byte[] name, float loading_time);

		// Token: 0x020005E3 RID: 1507
		// (Invoke) Token: 0x06001D9F RID: 7583
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddSceneObjectReportDelegate(byte[] scene_name, byte[] report_name, float report_value);

		// Token: 0x020005E4 RID: 1508
		// (Invoke) Token: 0x06001DA3 RID: 7587
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CheckIfAssetsAndSourcesAreSameDelegate();

		// Token: 0x020005E5 RID: 1509
		// (Invoke) Token: 0x06001DA7 RID: 7591
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckIfTerrainShaderHeaderGenerationFinishedDelegate();

		// Token: 0x020005E6 RID: 1510
		// (Invoke) Token: 0x06001DAB RID: 7595
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CheckResourceModificationsDelegate();

		// Token: 0x020005E7 RID: 1511
		// (Invoke) Token: 0x06001DAF RID: 7599
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CheckSceneForProblemsDelegate(byte[] path);

		// Token: 0x020005E8 RID: 1512
		// (Invoke) Token: 0x06001DB3 RID: 7603
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckShaderCompilationDelegate();

		// Token: 0x020005E9 RID: 1513
		// (Invoke) Token: 0x06001DB7 RID: 7607
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void clear_decal_atlasDelegate(DecalAtlasGroup atlasGroup);

		// Token: 0x020005EA RID: 1514
		// (Invoke) Token: 0x06001DBB RID: 7611
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearOldResourcesAndObjectsDelegate();

		// Token: 0x020005EB RID: 1515
		// (Invoke) Token: 0x06001DBF RID: 7615
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearShaderMemoryDelegate();

		// Token: 0x020005EC RID: 1516
		// (Invoke) Token: 0x06001DC3 RID: 7619
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CommandLineArgumentExistsDelegate(byte[] str);

		// Token: 0x020005ED RID: 1517
		// (Invoke) Token: 0x06001DC7 RID: 7623
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CompileAllShadersDelegate(byte[] targetPlatform);

		// Token: 0x020005EE RID: 1518
		// (Invoke) Token: 0x06001DCB RID: 7627
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CompileTerrainShadersDistDelegate(byte[] targetPlatform, byte[] targetConfig, byte[] output_path);

		// Token: 0x020005EF RID: 1519
		// (Invoke) Token: 0x06001DCF RID: 7631
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CreateSelectionInEditorDelegate(IntPtr gameEntities, int entityCount, byte[] name);

		// Token: 0x020005F0 RID: 1520
		// (Invoke) Token: 0x06001DD3 RID: 7635
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DebugSetGlobalLoadingWindowStateDelegate([MarshalAs(UnmanagedType.U1)] bool s);

		// Token: 0x020005F1 RID: 1521
		// (Invoke) Token: 0x06001DD7 RID: 7639
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeleteEntitiesInEditorSceneDelegate(IntPtr gameEntities, int entityCount);

		// Token: 0x020005F2 RID: 1522
		// (Invoke) Token: 0x06001DDB RID: 7643
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DetachWatchdogDelegate();

		// Token: 0x020005F3 RID: 1523
		// (Invoke) Token: 0x06001DDF RID: 7647
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool DidAutomatedGIBakeFinishedDelegate();

		// Token: 0x020005F4 RID: 1524
		// (Invoke) Token: 0x06001DE3 RID: 7651
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableCoreGameDelegate();

		// Token: 0x020005F5 RID: 1525
		// (Invoke) Token: 0x06001DE7 RID: 7655
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableGlobalEditDataCacherDelegate();

		// Token: 0x020005F6 RID: 1526
		// (Invoke) Token: 0x06001DEB RID: 7659
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableGlobalLoadingWindowDelegate();

		// Token: 0x020005F7 RID: 1527
		// (Invoke) Token: 0x06001DEF RID: 7663
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DoDelayedexitDelegate(int returnCode);

		// Token: 0x020005F8 RID: 1528
		// (Invoke) Token: 0x06001DF3 RID: 7667
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DoFullBakeAllLevelsAutomatedDelegate(byte[] module, byte[] sceneName);

		// Token: 0x020005F9 RID: 1529
		// (Invoke) Token: 0x06001DF7 RID: 7671
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DoFullBakeSingleLevelAutomatedDelegate(byte[] module, byte[] sceneName);

		// Token: 0x020005FA RID: 1530
		// (Invoke) Token: 0x06001DFB RID: 7675
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DoLightOnlyBakeAllLevelsAutomatedDelegate(byte[] module, byte[] sceneName);

		// Token: 0x020005FB RID: 1531
		// (Invoke) Token: 0x06001DFF RID: 7679
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DoLightOnlyBakeSingleLevelAutomatedDelegate(byte[] module, byte[] sceneName);

		// Token: 0x020005FC RID: 1532
		// (Invoke) Token: 0x06001E03 RID: 7683
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DumpGPUMemoryStatisticsDelegate(byte[] filePath);

		// Token: 0x020005FD RID: 1533
		// (Invoke) Token: 0x06001E07 RID: 7687
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnableGlobalEditDataCacherDelegate();

		// Token: 0x020005FE RID: 1534
		// (Invoke) Token: 0x06001E0B RID: 7691
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnableGlobalLoadingWindowDelegate();

		// Token: 0x020005FF RID: 1535
		// (Invoke) Token: 0x06001E0F RID: 7695
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnableSingleGPUQueryPerFrameDelegate();

		// Token: 0x02000600 RID: 1536
		// (Invoke) Token: 0x06001E13 RID: 7699
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EndLoadingStuckCheckStateDelegate();

		// Token: 0x02000601 RID: 1537
		// (Invoke) Token: 0x06001E17 RID: 7703
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int ExecuteCommandLineCommandDelegate(byte[] command);

		// Token: 0x02000602 RID: 1538
		// (Invoke) Token: 0x06001E1B RID: 7707
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ExitProcessDelegate(int exitCode);

		// Token: 0x02000603 RID: 1539
		// (Invoke) Token: 0x06001E1F RID: 7711
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int ExportNavMeshFaceMarksDelegate(byte[] file_name);

		// Token: 0x02000604 RID: 1540
		// (Invoke) Token: 0x06001E23 RID: 7715
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FindMeshesWithoutLodsDelegate(byte[] module_name);

		// Token: 0x02000605 RID: 1541
		// (Invoke) Token: 0x06001E27 RID: 7719
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FlushManagedObjectsMemoryDelegate();

		// Token: 0x02000606 RID: 1542
		// (Invoke) Token: 0x06001E2B RID: 7723
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GatherCoreGameReferencesDelegate(byte[] scene_names);

		// Token: 0x02000607 RID: 1543
		// (Invoke) Token: 0x06001E2F RID: 7727
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GenerateTerrainShaderHeadersDelegate(byte[] targetPlatform, byte[] targetConfig, byte[] output_path);

		// Token: 0x02000608 RID: 1544
		// (Invoke) Token: 0x06001E33 RID: 7731
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetApplicationMemoryDelegate();

		// Token: 0x02000609 RID: 1545
		// (Invoke) Token: 0x06001E37 RID: 7735
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetApplicationMemoryStatisticsDelegate();

		// Token: 0x0200060A RID: 1546
		// (Invoke) Token: 0x06001E3B RID: 7739
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetApplicationNameDelegate();

		// Token: 0x0200060B RID: 1547
		// (Invoke) Token: 0x06001E3F RID: 7743
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetAttachmentsPathDelegate();

		// Token: 0x0200060C RID: 1548
		// (Invoke) Token: 0x06001E43 RID: 7747
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetBaseDirectoryDelegate();

		// Token: 0x0200060D RID: 1549
		// (Invoke) Token: 0x06001E47 RID: 7751
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetBenchmarkStatusDelegate();

		// Token: 0x0200060E RID: 1550
		// (Invoke) Token: 0x06001E4B RID: 7755
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetBuildNumberDelegate();

		// Token: 0x0200060F RID: 1551
		// (Invoke) Token: 0x06001E4F RID: 7759
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetConsoleHostMachineDelegate();

		// Token: 0x02000610 RID: 1552
		// (Invoke) Token: 0x06001E53 RID: 7763
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetCoreGameStateDelegate();

		// Token: 0x02000611 RID: 1553
		// (Invoke) Token: 0x06001E57 RID: 7767
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate ulong GetCurrentCpuMemoryUsageDelegate();

		// Token: 0x02000612 RID: 1554
		// (Invoke) Token: 0x06001E5B RID: 7771
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetCurrentEstimatedGPUMemoryCostMBDelegate();

		// Token: 0x02000613 RID: 1555
		// (Invoke) Token: 0x06001E5F RID: 7775
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetCurrentProcessIDDelegate();

		// Token: 0x02000614 RID: 1556
		// (Invoke) Token: 0x06001E63 RID: 7779
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate ulong GetCurrentThreadIdDelegate();

		// Token: 0x02000615 RID: 1557
		// (Invoke) Token: 0x06001E67 RID: 7783
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetDeltaTimeDelegate(int timerId);

		// Token: 0x02000616 RID: 1558
		// (Invoke) Token: 0x06001E6B RID: 7787
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetDetailedGPUBufferMemoryStatsDelegate(ref int totalMemoryAllocated, ref int totalMemoryUsed, ref int emptyChunkCount);

		// Token: 0x02000617 RID: 1559
		// (Invoke) Token: 0x06001E6F RID: 7791
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetDetailedXBOXMemoryInfoDelegate();

		// Token: 0x02000618 RID: 1560
		// (Invoke) Token: 0x06001E73 RID: 7795
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetEditorSelectedEntitiesDelegate(IntPtr gameEntitiesTemp);

		// Token: 0x02000619 RID: 1561
		// (Invoke) Token: 0x06001E77 RID: 7799
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetEditorSelectedEntityCountDelegate();

		// Token: 0x0200061A RID: 1562
		// (Invoke) Token: 0x06001E7B RID: 7803
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetEngineFrameNoDelegate();

		// Token: 0x0200061B RID: 1563
		// (Invoke) Token: 0x06001E7F RID: 7807
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetEntitiesOfSelectionSetDelegate(byte[] name, IntPtr gameEntitiesTemp);

		// Token: 0x0200061C RID: 1564
		// (Invoke) Token: 0x06001E83 RID: 7811
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetEntityCountOfSelectionSetDelegate(byte[] name);

		// Token: 0x0200061D RID: 1565
		// (Invoke) Token: 0x06001E87 RID: 7815
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetExecutableWorkingDirectoryDelegate();

		// Token: 0x0200061E RID: 1566
		// (Invoke) Token: 0x06001E8B RID: 7819
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetFpsDelegate();

		// Token: 0x0200061F RID: 1567
		// (Invoke) Token: 0x06001E8F RID: 7823
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetFrameLimiterWithSleepDelegate();

		// Token: 0x02000620 RID: 1568
		// (Invoke) Token: 0x06001E93 RID: 7827
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetFullCommandLineStringDelegate();

		// Token: 0x02000621 RID: 1569
		// (Invoke) Token: 0x06001E97 RID: 7831
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetFullFilePathOfSceneDelegate(byte[] sceneName);

		// Token: 0x02000622 RID: 1570
		// (Invoke) Token: 0x06001E9B RID: 7835
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetFullModulePathDelegate(byte[] moduleName);

		// Token: 0x02000623 RID: 1571
		// (Invoke) Token: 0x06001E9F RID: 7839
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetFullModulePathsDelegate();

		// Token: 0x02000624 RID: 1572
		// (Invoke) Token: 0x06001EA3 RID: 7843
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetGPUMemoryMBDelegate();

		// Token: 0x02000625 RID: 1573
		// (Invoke) Token: 0x06001EA7 RID: 7847
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate ulong GetGpuMemoryOfAllocationGroupDelegate(byte[] allocationName);

		// Token: 0x02000626 RID: 1574
		// (Invoke) Token: 0x06001EAB RID: 7851
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetGPUMemoryStatsDelegate(ref float totalMemory, ref float renderTargetMemory, ref float depthTargetMemory, ref float srvMemory, ref float bufferMemory);

		// Token: 0x02000627 RID: 1575
		// (Invoke) Token: 0x06001EAF RID: 7855
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetLocalOutputPathDelegate();

		// Token: 0x02000628 RID: 1576
		// (Invoke) Token: 0x06001EB3 RID: 7859
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMainFpsDelegate();

		// Token: 0x02000629 RID: 1577
		// (Invoke) Token: 0x06001EB7 RID: 7863
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate ulong GetMainThreadIdDelegate();

		// Token: 0x0200062A RID: 1578
		// (Invoke) Token: 0x06001EBB RID: 7867
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetMemoryUsageOfCategoryDelegate(int index);

		// Token: 0x0200062B RID: 1579
		// (Invoke) Token: 0x06001EBF RID: 7871
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetModulesCodeDelegate();

		// Token: 0x0200062C RID: 1580
		// (Invoke) Token: 0x06001EC3 RID: 7875
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNativeMemoryStatisticsDelegate();

		// Token: 0x0200062D RID: 1581
		// (Invoke) Token: 0x06001EC7 RID: 7879
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNumberOfShaderCompilationsInProgressDelegate();

		// Token: 0x0200062E RID: 1582
		// (Invoke) Token: 0x06001ECB RID: 7883
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetPCInfoDelegate();

		// Token: 0x0200062F RID: 1583
		// (Invoke) Token: 0x06001ECF RID: 7887
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetPlatformModulePathsDelegate();

		// Token: 0x02000630 RID: 1584
		// (Invoke) Token: 0x06001ED3 RID: 7891
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetPossibleCommandLineStartingWithDelegate(byte[] command, int index);

		// Token: 0x02000631 RID: 1585
		// (Invoke) Token: 0x06001ED7 RID: 7895
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetRendererFpsDelegate();

		// Token: 0x02000632 RID: 1586
		// (Invoke) Token: 0x06001EDB RID: 7899
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetReturnCodeDelegate();

		// Token: 0x02000633 RID: 1587
		// (Invoke) Token: 0x06001EDF RID: 7903
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetSingleModuleScenesOfModuleDelegate(byte[] moduleName);

		// Token: 0x02000634 RID: 1588
		// (Invoke) Token: 0x06001EE3 RID: 7907
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetSteamAppIdDelegate();

		// Token: 0x02000635 RID: 1589
		// (Invoke) Token: 0x06001EE7 RID: 7911
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetSystemLanguageDelegate();

		// Token: 0x02000636 RID: 1590
		// (Invoke) Token: 0x06001EEB RID: 7915
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetVertexBufferChunkSystemMemoryUsageDelegate();

		// Token: 0x02000637 RID: 1591
		// (Invoke) Token: 0x06001EEF RID: 7919
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetVisualTestsTestFilesPathDelegate();

		// Token: 0x02000638 RID: 1592
		// (Invoke) Token: 0x06001EF3 RID: 7923
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetVisualTestsValidatePathDelegate();

		// Token: 0x02000639 RID: 1593
		// (Invoke) Token: 0x06001EF7 RID: 7927
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsAsyncPhysicsThreadDelegate();

		// Token: 0x0200063A RID: 1594
		// (Invoke) Token: 0x06001EFB RID: 7931
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsBenchmarkQuitedDelegate();

		// Token: 0x0200063B RID: 1595
		// (Invoke) Token: 0x06001EFF RID: 7935
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int IsDetailedSoundLogOnDelegate();

		// Token: 0x0200063C RID: 1596
		// (Invoke) Token: 0x06001F03 RID: 7939
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsDevkitDelegate();

		// Token: 0x0200063D RID: 1597
		// (Invoke) Token: 0x06001F07 RID: 7943
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsEditModeEnabledDelegate();

		// Token: 0x0200063E RID: 1598
		// (Invoke) Token: 0x06001F0B RID: 7947
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsLockhartPlatformDelegate();

		// Token: 0x0200063F RID: 1599
		// (Invoke) Token: 0x06001F0F RID: 7951
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsSceneReportFinishedDelegate();

		// Token: 0x02000640 RID: 1600
		// (Invoke) Token: 0x06001F13 RID: 7955
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void LoadSkyBoxesDelegate();

		// Token: 0x02000641 RID: 1601
		// (Invoke) Token: 0x06001F17 RID: 7959
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void LoadVirtualTextureTilesetDelegate(byte[] name);

		// Token: 0x02000642 RID: 1602
		// (Invoke) Token: 0x06001F1B RID: 7963
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ManagedParallelForDelegate(int fromInclusive, int toExclusive, long curKey, int grainSize);

		// Token: 0x02000643 RID: 1603
		// (Invoke) Token: 0x06001F1F RID: 7967
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ManagedParallelForWithDtDelegate(int fromInclusive, int toExclusive, long curKey, int grainSize);

		// Token: 0x02000644 RID: 1604
		// (Invoke) Token: 0x06001F23 RID: 7971
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ManagedParallelForWithoutRenderThreadDelegate(int fromInclusive, int toExclusive, long curKey, int grainSize);

		// Token: 0x02000645 RID: 1605
		// (Invoke) Token: 0x06001F27 RID: 7975
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OnLoadingWindowDisabledDelegate();

		// Token: 0x02000646 RID: 1606
		// (Invoke) Token: 0x06001F2B RID: 7979
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OnLoadingWindowEnabledDelegate();

		// Token: 0x02000647 RID: 1607
		// (Invoke) Token: 0x06001F2F RID: 7983
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OpenNavalDlcPurchasePageDelegate();

		// Token: 0x02000648 RID: 1608
		// (Invoke) Token: 0x06001F33 RID: 7987
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OpenOnscreenKeyboardDelegate(byte[] initialText, byte[] descriptionText, int maxLength, int keyboardTypeEnum);

		// Token: 0x02000649 RID: 1609
		// (Invoke) Token: 0x06001F37 RID: 7991
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OutputBenchmarkValuesToPerformanceReporterDelegate();

		// Token: 0x0200064A RID: 1610
		// (Invoke) Token: 0x06001F3B RID: 7995
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OutputPerformanceReportsDelegate();

		// Token: 0x0200064B RID: 1611
		// (Invoke) Token: 0x06001F3F RID: 7999
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PairSceneNameToModuleNameDelegate(byte[] sceneName, byte[] moduleName);

		// Token: 0x0200064C RID: 1612
		// (Invoke) Token: 0x06001F43 RID: 8003
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int ProcessWindowTitleDelegate(byte[] title);

		// Token: 0x0200064D RID: 1613
		// (Invoke) Token: 0x06001F47 RID: 8007
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void QuitGameDelegate();

		// Token: 0x0200064E RID: 1614
		// (Invoke) Token: 0x06001F4B RID: 8011
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int RegisterGPUAllocationGroupDelegate(byte[] name);

		// Token: 0x0200064F RID: 1615
		// (Invoke) Token: 0x06001F4F RID: 8015
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RegisterMeshForGPUMorphDelegate(byte[] metaMeshName);

		// Token: 0x02000650 RID: 1616
		// (Invoke) Token: 0x06001F53 RID: 8019
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int SaveDataAsTextureDelegate(byte[] path, int width, int height, IntPtr data);

		// Token: 0x02000651 RID: 1617
		// (Invoke) Token: 0x06001F57 RID: 8023
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SelectEntitiesDelegate(IntPtr gameEntities, int entityCount);

		// Token: 0x02000652 RID: 1618
		// (Invoke) Token: 0x06001F5B RID: 8027
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAllocationAlwaysValidSceneDelegate(UIntPtr scene);

		// Token: 0x02000653 RID: 1619
		// (Invoke) Token: 0x06001F5F RID: 8031
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAssertionAtShaderCompileDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000654 RID: 1620
		// (Invoke) Token: 0x06001F63 RID: 8035
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAssertionsAndWarningsSetExitCodeDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000655 RID: 1621
		// (Invoke) Token: 0x06001F67 RID: 8039
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBenchmarkStatusDelegate(int status, byte[] def);

		// Token: 0x02000656 RID: 1622
		// (Invoke) Token: 0x06001F6B RID: 8043
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCanLoadModulesDelegate([MarshalAs(UnmanagedType.U1)] bool canLoadModules);

		// Token: 0x02000657 RID: 1623
		// (Invoke) Token: 0x06001F6F RID: 8047
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCoreGameStateDelegate(int state);

		// Token: 0x02000658 RID: 1624
		// (Invoke) Token: 0x06001F73 RID: 8051
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCrashOnAssertsDelegate([MarshalAs(UnmanagedType.U1)] bool val);

		// Token: 0x02000659 RID: 1625
		// (Invoke) Token: 0x06001F77 RID: 8055
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCrashOnWarningsDelegate([MarshalAs(UnmanagedType.U1)] bool val);

		// Token: 0x0200065A RID: 1626
		// (Invoke) Token: 0x06001F7B RID: 8059
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCrashReportCustomStackDelegate(byte[] customStack);

		// Token: 0x0200065B RID: 1627
		// (Invoke) Token: 0x06001F7F RID: 8063
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCrashReportCustomStringDelegate(byte[] customString);

		// Token: 0x0200065C RID: 1628
		// (Invoke) Token: 0x06001F83 RID: 8067
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCreateDumpOnWarningsDelegate([MarshalAs(UnmanagedType.U1)] bool val);

		// Token: 0x0200065D RID: 1629
		// (Invoke) Token: 0x06001F87 RID: 8071
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDisableDumpGenerationDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200065E RID: 1630
		// (Invoke) Token: 0x06001F8B RID: 8075
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDumpFolderPathDelegate(byte[] path);

		// Token: 0x0200065F RID: 1631
		// (Invoke) Token: 0x06001F8F RID: 8079
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFixedDtDelegate([MarshalAs(UnmanagedType.U1)] bool enabled, float dt);

		// Token: 0x02000660 RID: 1632
		// (Invoke) Token: 0x06001F93 RID: 8083
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForceDrawEntityIDDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000661 RID: 1633
		// (Invoke) Token: 0x06001F97 RID: 8087
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForceVsyncDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000662 RID: 1634
		// (Invoke) Token: 0x06001F9B RID: 8091
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFrameLimiterWithSleepDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000663 RID: 1635
		// (Invoke) Token: 0x06001F9F RID: 8095
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGraphicsPresetDelegate(int preset);

		// Token: 0x02000664 RID: 1636
		// (Invoke) Token: 0x06001FA3 RID: 8099
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLoadingScreenPercentageDelegate(float value);

		// Token: 0x02000665 RID: 1637
		// (Invoke) Token: 0x06001FA7 RID: 8103
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMessageLineRenderingStateDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000666 RID: 1638
		// (Invoke) Token: 0x06001FAB RID: 8107
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPrintCallstackAtCrahsesDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000667 RID: 1639
		// (Invoke) Token: 0x06001FAF RID: 8111
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRenderAgentsDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000668 RID: 1640
		// (Invoke) Token: 0x06001FB3 RID: 8115
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRenderModeDelegate(int mode);

		// Token: 0x02000669 RID: 1641
		// (Invoke) Token: 0x06001FB7 RID: 8119
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetReportModeDelegate([MarshalAs(UnmanagedType.U1)] bool reportMode);

		// Token: 0x0200066A RID: 1642
		// (Invoke) Token: 0x06001FBB RID: 8123
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetScreenTextRenderingStateDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200066B RID: 1643
		// (Invoke) Token: 0x06001FBF RID: 8127
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWatchdogAutoreportDelegate([MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200066C RID: 1644
		// (Invoke) Token: 0x06001FC3 RID: 8131
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWatchdogValueDelegate(byte[] fileName, byte[] groupName, byte[] key, byte[] value);

		// Token: 0x0200066D RID: 1645
		// (Invoke) Token: 0x06001FC7 RID: 8135
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWindowTitleDelegate(byte[] title);

		// Token: 0x0200066E RID: 1646
		// (Invoke) Token: 0x06001FCB RID: 8139
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StartLoadingStuckCheckStateDelegate(float seconds);

		// Token: 0x0200066F RID: 1647
		// (Invoke) Token: 0x06001FCF RID: 8143
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StartScenePerformanceReportDelegate(byte[] folderPath);

		// Token: 0x02000670 RID: 1648
		// (Invoke) Token: 0x06001FD3 RID: 8147
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TakeScreenshotFromPlatformPathDelegate(PlatformFilePath path);

		// Token: 0x02000671 RID: 1649
		// (Invoke) Token: 0x06001FD7 RID: 8151
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TakeScreenshotFromStringPathDelegate(byte[] path);

		// Token: 0x02000672 RID: 1650
		// (Invoke) Token: 0x06001FDB RID: 8155
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int TakeSSFromTopDelegate(byte[] file_name);

		// Token: 0x02000673 RID: 1651
		// (Invoke) Token: 0x06001FDF RID: 8159
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ToggleRenderDelegate();
	}
}
