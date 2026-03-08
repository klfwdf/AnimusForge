using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000039 RID: 57
	[ApplicationInterfaceBase]
	internal interface IUtil
	{
		// Token: 0x06000580 RID: 1408
		[EngineMethod("output_benchmark_values_to_performance_reporter", false, null, false)]
		void OutputBenchmarkValuesToPerformanceReporter();

		// Token: 0x06000581 RID: 1409
		[EngineMethod("set_loading_screen_percentage", false, null, false)]
		void SetLoadingScreenPercentage(float value);

		// Token: 0x06000582 RID: 1410
		[EngineMethod("set_fixed_dt", false, null, false)]
		void SetFixedDt(bool enabled, float dt);

		// Token: 0x06000583 RID: 1411
		[EngineMethod("set_benchmark_status", false, null, false)]
		void SetBenchmarkStatus(int status, string def);

		// Token: 0x06000584 RID: 1412
		[EngineMethod("set_can_load_modules", false, null, false)]
		void SetCanLoadModules(bool canLoadModules);

		// Token: 0x06000585 RID: 1413
		[EngineMethod("get_benchmark_status", false, null, false)]
		int GetBenchmarkStatus();

		// Token: 0x06000586 RID: 1414
		[EngineMethod("is_benchmark_quited", false, null, false)]
		bool IsBenchmarkQuited();

		// Token: 0x06000587 RID: 1415
		[EngineMethod("get_application_memory_statistics", false, null, false)]
		string GetApplicationMemoryStatistics();

		// Token: 0x06000588 RID: 1416
		[EngineMethod("get_native_memory_statistics", false, null, false)]
		string GetNativeMemoryStatistics();

		// Token: 0x06000589 RID: 1417
		[EngineMethod("command_line_argument_exits", false, null, false)]
		bool CommandLineArgumentExists(string str);

		// Token: 0x0600058A RID: 1418
		[EngineMethod("export_nav_mesh_face_marks", false, null, false)]
		string ExportNavMeshFaceMarks(string file_name);

		// Token: 0x0600058B RID: 1419
		[EngineMethod("take_ss_from_top", false, null, false)]
		string TakeSSFromTop(string file_name);

		// Token: 0x0600058C RID: 1420
		[EngineMethod("check_if_assets_and_sources_are_same", false, null, false)]
		void CheckIfAssetsAndSourcesAreSame();

		// Token: 0x0600058D RID: 1421
		[EngineMethod("gather_core_game_references", false, null, false)]
		void GatherCoreGameReferences(string scene_names);

		// Token: 0x0600058E RID: 1422
		[EngineMethod("get_application_memory", false, null, false)]
		float GetApplicationMemory();

		// Token: 0x0600058F RID: 1423
		[EngineMethod("disable_core_game", false, null, false)]
		void DisableCoreGame();

		// Token: 0x06000590 RID: 1424
		[EngineMethod("find_meshes_without_lods", false, null, false)]
		void FindMeshesWithoutLods(string module_name);

		// Token: 0x06000591 RID: 1425
		[EngineMethod("set_print_callstack_at_crashes", false, null, false)]
		void SetPrintCallstackAtCrahses(bool value);

		// Token: 0x06000592 RID: 1426
		[EngineMethod("set_disable_dump_generation", false, null, false)]
		void SetDisableDumpGeneration(bool value);

		// Token: 0x06000593 RID: 1427
		[EngineMethod("get_modules_code", false, null, false)]
		string GetModulesCode();

		// Token: 0x06000594 RID: 1428
		[EngineMethod("get_full_module_path", false, null, false)]
		string GetFullModulePath(string moduleName);

		// Token: 0x06000595 RID: 1429
		[EngineMethod("get_full_module_paths", false, null, false)]
		string GetFullModulePaths();

		// Token: 0x06000596 RID: 1430
		[EngineMethod("get_full_file_path_of_scene", false, null, false)]
		string GetFullFilePathOfScene(string sceneName);

		// Token: 0x06000597 RID: 1431
		[EngineMethod("pair_scene_name_to_module_name", false, null, false)]
		void PairSceneNameToModuleName(string sceneName, string moduleName);

		// Token: 0x06000598 RID: 1432
		[EngineMethod("get_single_module_scenes_of_module", false, null, false)]
		string GetSingleModuleScenesOfModule(string moduleName);

		// Token: 0x06000599 RID: 1433
		[EngineMethod("get_executable_working_directory", false, null, false)]
		string GetExecutableWorkingDirectory();

		// Token: 0x0600059A RID: 1434
		[EngineMethod("add_main_thread_performance_query", false, null, false)]
		void AddMainThreadPerformanceQuery(string parent, string name, float seconds);

		// Token: 0x0600059B RID: 1435
		[EngineMethod("set_dump_folder_path", false, null, false)]
		void SetDumpFolderPath(string path);

		// Token: 0x0600059C RID: 1436
		[EngineMethod("check_scene_for_problems", false, null, false)]
		void CheckSceneForProblems(string path);

		// Token: 0x0600059D RID: 1437
		[EngineMethod("set_screen_text_rendering_state", false, null, false)]
		void SetScreenTextRenderingState(bool value);

		// Token: 0x0600059E RID: 1438
		[EngineMethod("set_message_line_rendering_state", false, null, false)]
		void SetMessageLineRenderingState(bool value);

		// Token: 0x0600059F RID: 1439
		[EngineMethod("check_shader_compilation", false, null, false)]
		bool CheckShaderCompilation();

		// Token: 0x060005A0 RID: 1440
		[EngineMethod("set_crash_on_asserts", false, null, false)]
		void SetCrashOnAsserts(bool val);

		// Token: 0x060005A1 RID: 1441
		[EngineMethod("check_if_terrain_shader_header_generation_finished", false, null, false)]
		bool CheckIfTerrainShaderHeaderGenerationFinished();

		// Token: 0x060005A2 RID: 1442
		[EngineMethod("set_crash_on_warnings", false, null, false)]
		void SetCrashOnWarnings(bool val);

		// Token: 0x060005A3 RID: 1443
		[EngineMethod("set_create_dump_on_warnings", false, null, false)]
		void SetCreateDumpOnWarnings(bool val);

		// Token: 0x060005A4 RID: 1444
		[EngineMethod("generate_terrain_shader_headers", false, null, false)]
		void GenerateTerrainShaderHeaders(string targetPlatform, string targetConfig, string output_path);

		// Token: 0x060005A5 RID: 1445
		[EngineMethod("compile_terrain_shaders_dist", false, null, false)]
		void CompileTerrainShadersDist(string targetPlatform, string targetConfig, string output_path);

		// Token: 0x060005A6 RID: 1446
		[EngineMethod("compile_all_shaders", false, null, false)]
		void CompileAllShaders(string targetPlatform);

		// Token: 0x060005A7 RID: 1447
		[EngineMethod("toggle_render", false, null, false)]
		void ToggleRender();

		// Token: 0x060005A8 RID: 1448
		[EngineMethod("set_force_draw_entity_id", false, null, false)]
		void SetForceDrawEntityID(bool value);

		// Token: 0x060005A9 RID: 1449
		[EngineMethod("set_render_agents", false, null, false)]
		void SetRenderAgents(bool value);

		// Token: 0x060005AA RID: 1450
		[EngineMethod("get_core_game_state", false, null, false)]
		int GetCoreGameState();

		// Token: 0x060005AB RID: 1451
		[EngineMethod("set_core_game_state", false, null, false)]
		void SetCoreGameState(int state);

		// Token: 0x060005AC RID: 1452
		[EngineMethod("execute_command_line_command", false, null, false)]
		string ExecuteCommandLineCommand(string command);

		// Token: 0x060005AD RID: 1453
		[EngineMethod("quit_game", false, null, false)]
		void QuitGame();

		// Token: 0x060005AE RID: 1454
		[EngineMethod("exit_process", false, null, false)]
		void ExitProcess(int exitCode);

		// Token: 0x060005AF RID: 1455
		[EngineMethod("start_scene_performance_report", false, null, false)]
		void StartScenePerformanceReport(string folderPath);

		// Token: 0x060005B0 RID: 1456
		[EngineMethod("get_base_directory", false, null, false)]
		string GetBaseDirectory();

		// Token: 0x060005B1 RID: 1457
		[EngineMethod("get_visual_tests_test_files_path", false, null, false)]
		string GetVisualTestsTestFilesPath();

		// Token: 0x060005B2 RID: 1458
		[EngineMethod("get_visual_tests_validate_path", false, null, false)]
		string GetVisualTestsValidatePath();

		// Token: 0x060005B3 RID: 1459
		[EngineMethod("get_attachments_path", false, null, false)]
		string GetAttachmentsPath();

		// Token: 0x060005B4 RID: 1460
		[EngineMethod("is_scene_performance_report_finished", false, null, false)]
		bool IsSceneReportFinished();

		// Token: 0x060005B5 RID: 1461
		[EngineMethod("flush_managed_objects_memory", false, null, false)]
		void FlushManagedObjectsMemory();

		// Token: 0x060005B6 RID: 1462
		[EngineMethod("set_render_mode", false, null, false)]
		void SetRenderMode(int mode);

		// Token: 0x060005B7 RID: 1463
		[EngineMethod("add_performance_report_token", false, null, false)]
		void AddPerformanceReportToken(string performance_type, string name, float loading_time);

		// Token: 0x060005B8 RID: 1464
		[EngineMethod("add_scene_object_report", false, null, false)]
		void AddSceneObjectReport(string scene_name, string report_name, float report_value);

		// Token: 0x060005B9 RID: 1465
		[EngineMethod("output_performance_reports", false, null, false)]
		void OutputPerformanceReports();

		// Token: 0x060005BA RID: 1466
		[EngineMethod("add_command_line_function", false, null, false)]
		void AddCommandLineFunction(string concatName);

		// Token: 0x060005BB RID: 1467
		[EngineMethod("get_number_of_shader_compilations_in_progress", false, null, false)]
		int GetNumberOfShaderCompilationsInProgress();

		// Token: 0x060005BC RID: 1468
		[EngineMethod("get_editor_selected_entity_count", false, null, false)]
		int GetEditorSelectedEntityCount();

		// Token: 0x060005BD RID: 1469
		[EngineMethod("get_entity_count_of_selection_set", false, null, false)]
		int GetEntityCountOfSelectionSet(string name);

		// Token: 0x060005BE RID: 1470
		[EngineMethod("get_build_number", false, null, false)]
		int GetBuildNumber();

		// Token: 0x060005BF RID: 1471
		[EngineMethod("get_entities_of_selection_set", false, null, false)]
		void GetEntitiesOfSelectionSet(string name, UIntPtr[] gameEntitiesTemp);

		// Token: 0x060005C0 RID: 1472
		[EngineMethod("get_editor_selected_entities", false, null, false)]
		void GetEditorSelectedEntities(UIntPtr[] gameEntitiesTemp);

		// Token: 0x060005C1 RID: 1473
		[EngineMethod("delete_entities_in_editor_scene", false, null, false)]
		void DeleteEntitiesInEditorScene(UIntPtr[] gameEntities, int entityCount);

		// Token: 0x060005C2 RID: 1474
		[EngineMethod("create_selection_set_in_editor", false, null, false)]
		void CreateSelectionInEditor(UIntPtr[] gameEntities, int entityCount, string name);

		// Token: 0x060005C3 RID: 1475
		[EngineMethod("select_entities_in_editor", false, null, false)]
		void SelectEntities(UIntPtr[] gameEntities, int entityCount);

		// Token: 0x060005C4 RID: 1476
		[EngineMethod("get_current_cpu_memory_usage", false, null, false)]
		ulong GetCurrentCpuMemoryUsage();

		// Token: 0x060005C5 RID: 1477
		[EngineMethod("get_gpu_memory_stats", false, null, false)]
		void GetGPUMemoryStats(ref float totalMemory, ref float renderTargetMemory, ref float depthTargetMemory, ref float srvMemory, ref float bufferMemory);

		// Token: 0x060005C6 RID: 1478
		[EngineMethod("get_gpu_memory_of_allocation_group", false, null, false)]
		ulong GetGpuMemoryOfAllocationGroup(string allocationName);

		// Token: 0x060005C7 RID: 1479
		[EngineMethod("get_detailed_gpu_buffer_memory_stats", false, null, false)]
		void GetDetailedGPUBufferMemoryStats(ref int totalMemoryAllocated, ref int totalMemoryUsed, ref int emptyChunkCount);

		// Token: 0x060005C8 RID: 1480
		[EngineMethod("is_detailed_soung_log_on", false, null, false)]
		int IsDetailedSoundLogOn();

		// Token: 0x060005C9 RID: 1481
		[EngineMethod("get_main_fps", false, null, false)]
		float GetMainFps();

		// Token: 0x060005CA RID: 1482
		[EngineMethod("on_loading_window_enabled", false, null, false)]
		void OnLoadingWindowEnabled();

		// Token: 0x060005CB RID: 1483
		[EngineMethod("debug_set_global_loading_window_state", false, null, false)]
		void DebugSetGlobalLoadingWindowState(bool s);

		// Token: 0x060005CC RID: 1484
		[EngineMethod("on_loading_window_disabled", false, null, false)]
		void OnLoadingWindowDisabled();

		// Token: 0x060005CD RID: 1485
		[EngineMethod("disable_global_loading_window", false, null, false)]
		void DisableGlobalLoadingWindow();

		// Token: 0x060005CE RID: 1486
		[EngineMethod("enable_global_loading_window", false, null, false)]
		void EnableGlobalLoadingWindow();

		// Token: 0x060005CF RID: 1487
		[EngineMethod("enable_global_edit_data_cacher", false, null, false)]
		void EnableGlobalEditDataCacher();

		// Token: 0x060005D0 RID: 1488
		[EngineMethod("get_renderer_fps", false, null, false)]
		float GetRendererFps();

		// Token: 0x060005D1 RID: 1489
		[EngineMethod("disable_global_edit_data_cacher", false, null, false)]
		void DisableGlobalEditDataCacher();

		// Token: 0x060005D2 RID: 1490
		[EngineMethod("enable_single_gpu_query_per_frame", false, null, false)]
		void EnableSingleGPUQueryPerFrame();

		// Token: 0x060005D3 RID: 1491
		[EngineMethod("clear_decal_atlas", false, null, false)]
		void clear_decal_atlas(DecalAtlasGroup atlasGroup);

		// Token: 0x060005D4 RID: 1492
		[EngineMethod("get_fps", false, null, false)]
		float GetFps();

		// Token: 0x060005D5 RID: 1493
		[EngineMethod("get_full_command_line_string", false, null, false)]
		string GetFullCommandLineString();

		// Token: 0x060005D6 RID: 1494
		[EngineMethod("take_screenshot_from_string_path", false, null, false)]
		void TakeScreenshotFromStringPath(string path);

		// Token: 0x060005D7 RID: 1495
		[EngineMethod("take_screenshot_from_platform_path", false, null, false)]
		void TakeScreenshotFromPlatformPath(PlatformFilePath path);

		// Token: 0x060005D8 RID: 1496
		[EngineMethod("check_resource_modifications", false, null, false)]
		void CheckResourceModifications();

		// Token: 0x060005D9 RID: 1497
		[EngineMethod("set_graphics_preset", false, null, false)]
		void SetGraphicsPreset(int preset);

		// Token: 0x060005DA RID: 1498
		[EngineMethod("clear_old_resources_and_objects", false, null, false)]
		void ClearOldResourcesAndObjects();

		// Token: 0x060005DB RID: 1499
		[EngineMethod("load_virtual_texture_tileset", false, null, false)]
		void LoadVirtualTextureTileset(string name);

		// Token: 0x060005DC RID: 1500
		[EngineMethod("get_delta_time", false, null, false)]
		float GetDeltaTime(int timerId);

		// Token: 0x060005DD RID: 1501
		[EngineMethod("load_sky_boxes", false, null, false)]
		void LoadSkyBoxes();

		// Token: 0x060005DE RID: 1502
		[EngineMethod("get_engine_frame_no", false, null, false)]
		int GetEngineFrameNo();

		// Token: 0x060005DF RID: 1503
		[EngineMethod("set_allocation_always_valid_scene", false, null, false)]
		void SetAllocationAlwaysValidScene(UIntPtr scene);

		// Token: 0x060005E0 RID: 1504
		[EngineMethod("get_console_host_machine", false, null, false)]
		string GetConsoleHostMachine();

		// Token: 0x060005E1 RID: 1505
		[EngineMethod("is_edit_mode_enabled", false, null, false)]
		bool IsEditModeEnabled();

		// Token: 0x060005E2 RID: 1506
		[EngineMethod("get_pc_info", false, null, false)]
		string GetPCInfo();

		// Token: 0x060005E3 RID: 1507
		[EngineMethod("get_gpu_memory_mb", false, null, false)]
		int GetGPUMemoryMB();

		// Token: 0x060005E4 RID: 1508
		[EngineMethod("get_current_estimated_gpu_memory_cost_mb", false, null, false)]
		int GetCurrentEstimatedGPUMemoryCostMB();

		// Token: 0x060005E5 RID: 1509
		[EngineMethod("dump_gpu_memory_statistics", false, null, false)]
		void DumpGPUMemoryStatistics(string filePath);

		// Token: 0x060005E6 RID: 1510
		[EngineMethod("save_data_as_texture", false, null, false)]
		int SaveDataAsTexture(string path, int width, int height, float[] data);

		// Token: 0x060005E7 RID: 1511
		[EngineMethod("get_application_name", false, null, false)]
		string GetApplicationName();

		// Token: 0x060005E8 RID: 1512
		[EngineMethod("open_naval_dlc_purchase_page", false, null, false)]
		void OpenNavalDlcPurchasePage();

		// Token: 0x060005E9 RID: 1513
		[EngineMethod("set_window_title", false, null, false)]
		void SetWindowTitle(string title);

		// Token: 0x060005EA RID: 1514
		[EngineMethod("process_window_title", false, null, false)]
		string ProcessWindowTitle(string title);

		// Token: 0x060005EB RID: 1515
		[EngineMethod("get_current_process_id", false, null, false)]
		uint GetCurrentProcessID();

		// Token: 0x060005EC RID: 1516
		[EngineMethod("do_delayed_exit", false, null, false)]
		void DoDelayedexit(int returnCode);

		// Token: 0x060005ED RID: 1517
		[EngineMethod("set_report_mode", false, null, false)]
		void SetReportMode(bool reportMode);

		// Token: 0x060005EE RID: 1518
		[EngineMethod("set_assertions_and_warnings_set_exit_code", false, null, false)]
		void SetAssertionsAndWarningsSetExitCode(bool value);

		// Token: 0x060005EF RID: 1519
		[EngineMethod("set_assertion_at_shader_compile", false, null, false)]
		void SetAssertionAtShaderCompile(bool value);

		// Token: 0x060005F0 RID: 1520
		[EngineMethod("did_automated_gi_bake_finished", false, null, false)]
		bool DidAutomatedGIBakeFinished();

		// Token: 0x060005F1 RID: 1521
		[EngineMethod("do_full_bake_all_levels_automated", false, null, false)]
		void DoFullBakeAllLevelsAutomated(string module, string sceneName);

		// Token: 0x060005F2 RID: 1522
		[EngineMethod("do_full_bake_single_level_automated", false, null, false)]
		void DoFullBakeSingleLevelAutomated(string module, string sceneName);

		// Token: 0x060005F3 RID: 1523
		[EngineMethod("get_return_code", false, null, false)]
		int GetReturnCode();

		// Token: 0x060005F4 RID: 1524
		[EngineMethod("do_light_only_bake_single_level_automated", false, null, false)]
		void DoLightOnlyBakeSingleLevelAutomated(string module, string sceneName);

		// Token: 0x060005F5 RID: 1525
		[EngineMethod("do_light_only_bake_all_levels_automated", false, null, false)]
		void DoLightOnlyBakeAllLevelsAutomated(string module, string sceneName);

		// Token: 0x060005F6 RID: 1526
		[EngineMethod("get_local_output_dir", false, null, false)]
		string GetLocalOutputPath();

		// Token: 0x060005F7 RID: 1527
		[EngineMethod("set_crash_report_custom_string", false, null, false)]
		void SetCrashReportCustomString(string customString);

		// Token: 0x060005F8 RID: 1528
		[EngineMethod("set_crash_report_custom_managed_stack", false, null, false)]
		void SetCrashReportCustomStack(string customStack);

		// Token: 0x060005F9 RID: 1529
		[EngineMethod("get_steam_appid", false, null, false)]
		int GetSteamAppId();

		// Token: 0x060005FA RID: 1530
		[EngineMethod("set_force_vsync", false, null, false)]
		void SetForceVsync(bool value);

		// Token: 0x060005FB RID: 1531
		[EngineMethod("get_system_language", false, null, false)]
		string GetSystemLanguage();

		// Token: 0x060005FC RID: 1532
		[EngineMethod("managed_parallel_for", false, null, false)]
		void ManagedParallelFor(int fromInclusive, int toExclusive, long curKey, int grainSize);

		// Token: 0x060005FD RID: 1533
		[EngineMethod("managed_parallel_for_without_render_thread", false, null, false)]
		void ManagedParallelForWithoutRenderThread(int fromInclusive, int toExclusive, long curKey, int grainSize);

		// Token: 0x060005FE RID: 1534
		[EngineMethod("get_main_thread_id", false, null, false)]
		ulong GetMainThreadId();

		// Token: 0x060005FF RID: 1535
		[EngineMethod("get_current_thread_id", false, null, false)]
		ulong GetCurrentThreadId();

		// Token: 0x06000600 RID: 1536
		[EngineMethod("get_platform_module_paths", false, null, false)]
		string GetPlatformModulePaths();

		// Token: 0x06000601 RID: 1537
		[EngineMethod("set_watchdog_autoreport", false, null, false)]
		void SetWatchdogAutoreport(bool value);

		// Token: 0x06000602 RID: 1538
		[EngineMethod("is_async_physics_thread", false, null, false)]
		bool IsAsyncPhysicsThread();

		// Token: 0x06000603 RID: 1539
		[EngineMethod("start_loading_stuck_check_state", false, null, false)]
		void StartLoadingStuckCheckState(float seconds);

		// Token: 0x06000604 RID: 1540
		[EngineMethod("end_loading_stuck_check_state", false, null, false)]
		void EndLoadingStuckCheckState();

		// Token: 0x06000605 RID: 1541
		[EngineMethod("set_watchdog_value", false, null, false)]
		void SetWatchdogValue(string fileName, string groupName, string key, string value);

		// Token: 0x06000606 RID: 1542
		[EngineMethod("detach_watchdog", false, null, false)]
		void DetachWatchdog();

		// Token: 0x06000607 RID: 1543
		[EngineMethod("register_mesh_for_gpu_morph", false, null, false)]
		void RegisterMeshForGPUMorph(string metaMeshName);

		// Token: 0x06000608 RID: 1544
		[EngineMethod("managed_parallel_for_with_dt", false, null, false)]
		void ManagedParallelForWithDt(int fromInclusive, int toExclusive, long curKey, int grainSize);

		// Token: 0x06000609 RID: 1545
		[EngineMethod("clear_shader_memory", false, null, false)]
		void ClearShaderMemory();

		// Token: 0x0600060A RID: 1546
		[EngineMethod("open_onscreen_keyboard", false, null, false)]
		void OpenOnscreenKeyboard(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum);

		// Token: 0x0600060B RID: 1547
		[EngineMethod("register_gpu_allocation_group", false, null, false)]
		int RegisterGPUAllocationGroup(string name);

		// Token: 0x0600060C RID: 1548
		[EngineMethod("get_memory_usage_of_category", false, null, false)]
		int GetMemoryUsageOfCategory(int index);

		// Token: 0x0600060D RID: 1549
		[EngineMethod("get_vertex_buffer_chunk_system_memory_usage", false, null, false)]
		int GetVertexBufferChunkSystemMemoryUsage();

		// Token: 0x0600060E RID: 1550
		[EngineMethod("get_detailed_xbox_memory_info", false, null, false)]
		string GetDetailedXBOXMemoryInfo();

		// Token: 0x0600060F RID: 1551
		[EngineMethod("set_frame_limiter_with_sleep", false, null, false)]
		void SetFrameLimiterWithSleep(bool value);

		// Token: 0x06000610 RID: 1552
		[EngineMethod("get_frame_limiter_with_sleep", false, null, false)]
		bool GetFrameLimiterWithSleep();

		// Token: 0x06000611 RID: 1553
		[EngineMethod("get_possible_command_line_starting_with", false, null, false)]
		string GetPossibleCommandLineStartingWith(string command, int index);

		// Token: 0x06000612 RID: 1554
		[EngineMethod("is_dev_kit", false, null, false)]
		bool IsDevkit();

		// Token: 0x06000613 RID: 1555
		[EngineMethod("is_gen9_xbox_lockhart", false, null, false)]
		bool IsLockhartPlatform();
	}
}
