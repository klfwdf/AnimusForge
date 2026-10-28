using System;
using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal static class CoreCallbacksGenerated
{
	internal delegate float Agent_DebugGetHealth_delegate(int thisPointer);

	internal delegate int Agent_GetFormationUnitSpacing_delegate(int thisPointer);

	internal delegate float Agent_GetMissileRangeWithHeightDifferenceAux_delegate(int thisPointer, float targetZ);

	internal delegate UIntPtr Agent_GetSoundAndCollisionInfoClassName_delegate(int thisPointer);

	internal delegate float Agent_GetWeaponInaccuracy_delegate(int thisPointer, EquipmentIndex weaponSlotIndex, int weaponUsageIndex);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool Agent_IsInSameFormationWith_delegate(int thisPointer, int otherAgent);

	internal delegate void Agent_OnAgentAlarmedStateChanged_delegate(int thisPointer, Agent.AIStateFlag flag);

	internal delegate void Agent_OnAIInputSet_delegate(int thisPointer, ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector);

	internal delegate void Agent_OnDismount_delegate(int thisPointer, int mount);

	internal delegate void Agent_OnMount_delegate(int thisPointer, int mount);

	internal delegate void Agent_OnRemoveWeapon_delegate(int thisPointer, EquipmentIndex slotIndex);

	internal delegate void Agent_OnRetreating_delegate(int thisPointer);

	internal delegate void Agent_OnShieldDamaged_delegate(int thisPointer, EquipmentIndex slotIndex, int inflictedDamage);

	internal delegate void Agent_OnWeaponAmmoConsume_delegate(int thisPointer, EquipmentIndex slotIndex, short totalAmmo);

	internal delegate void Agent_OnWeaponAmmoReload_delegate(int thisPointer, EquipmentIndex slotIndex, EquipmentIndex ammoSlotIndex, short totalAmmo);

	internal delegate void Agent_OnWeaponAmmoRemoved_delegate(int thisPointer, EquipmentIndex slotIndex);

	internal delegate void Agent_OnWeaponAmountChange_delegate(int thisPointer, EquipmentIndex slotIndex, short amount);

	internal delegate void Agent_OnWeaponReloadPhaseChange_delegate(int thisPointer, EquipmentIndex slotIndex, short reloadPhase);

	internal delegate void Agent_OnWeaponSwitchingToAlternativeStart_delegate(int thisPointer, EquipmentIndex slotIndex, int usageIndex);

	internal delegate void Agent_OnWeaponUsageIndexChange_delegate(int thisPointer, EquipmentIndex slotIndex, int usageIndex);

	internal delegate void Agent_OnWieldedItemIndexChange_delegate(int thisPointer, [MarshalAs(UnmanagedType.U1)] bool isOffHand, [MarshalAs(UnmanagedType.U1)] bool isWieldedInstantly, [MarshalAs(UnmanagedType.U1)] bool isWieldedOnSpawn);

	internal delegate void Agent_SetAgentAIPerformingRetreatBehavior_delegate(int thisPointer, [MarshalAs(UnmanagedType.U1)] bool isAgentAIPerformingRetreatBehavior);

	internal delegate void Agent_UpdateAgentStats_delegate(int thisPointer);

	internal delegate void Agent_UpdateMountAgentCache_delegate(int thisPointer, int newMountAgent);

	internal delegate void Agent_UpdateRiderAgentCache_delegate(int thisPointer, int newRiderAgent);

	internal delegate void BannerlordTableauManager_RegisterCharacterTableauScene_delegate(NativeObjectPointer scene, int type);

	internal delegate void BannerlordTableauManager_RequestCharacterTableauSetup_delegate(int characterCodeId, NativeObjectPointer scene, NativeObjectPointer poseEntity);

	internal delegate void CoreManaged_CheckSharedStructureSizes_delegate();

	internal delegate void CoreManaged_EngineApiMethodInterfaceInitializer_delegate(int id, IntPtr pointer);

	internal delegate void CoreManaged_FillEngineApiPointers_delegate();

	internal delegate void CoreManaged_Finalize_delegate();

	internal delegate void CoreManaged_OnLoadCommonFinished_delegate();

	internal delegate void CoreManaged_Start_delegate();

	internal delegate void GameNetwork_HandleConsoleCommand_delegate(IntPtr command);

	internal delegate void GameNetwork_HandleDisconnect_delegate();

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool GameNetwork_HandleNetworkPacketAsClient_delegate();

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool GameNetwork_HandleNetworkPacketAsServer_delegate(int networkPeer);

	internal delegate void GameNetwork_HandleRemovePlayer_delegate(int peer, [MarshalAs(UnmanagedType.U1)] bool isTimedOut);

	internal delegate void GameNetwork_SyncRelevantGameOptionsToServer_delegate();

	internal delegate int ManagedOptions_GetConfigCount_delegate();

	internal delegate float ManagedOptions_GetConfigValue_delegate(int type);

	internal delegate void MBEditor_CloseEditorScene_delegate();

	internal delegate void MBEditor_DestroyEditor_delegate(NativeObjectPointer scene);

	internal delegate void MBEditor_SetEditorScene_delegate(NativeObjectPointer scene);

	internal delegate int MBMultiplayerData_GetCurrentPlayerCount_delegate();

	internal delegate UIntPtr MBMultiplayerData_GetGameModule_delegate();

	internal delegate UIntPtr MBMultiplayerData_GetGameType_delegate();

	internal delegate UIntPtr MBMultiplayerData_GetMap_delegate();

	internal delegate int MBMultiplayerData_GetPlayerCountLimit_delegate();

	internal delegate UIntPtr MBMultiplayerData_GetServerId_delegate();

	internal delegate UIntPtr MBMultiplayerData_GetServerName_delegate();

	internal delegate void MBMultiplayerData_UpdateGameServerInfo_delegate(IntPtr id, IntPtr gameServer, IntPtr gameModule, IntPtr gameType, IntPtr map, int currentPlayerCount, int maxPlayerCount, IntPtr address, int port);

	internal delegate void Mission_ApplySkeletonScaleToAllEquippedItems_delegate(int thisPointer, IntPtr itemName);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool Mission_CanPhysicsCollideBetweenTwoEntities_delegate(int thisPointer, UIntPtr entity0Ptr, UIntPtr entity1Ptr);

	internal delegate void Mission_ChargeDamageCallback_delegate(int thisPointer, ref AttackCollisionData collisionData, Blow blow, int attacker, int victim);

	internal delegate void Mission_DebugLogNativeMissionNetworkEvent_delegate(int eventEnum, IntPtr eventName, int bitCount);

	internal delegate void Mission_EndMission_delegate(int thisPointer);

	internal delegate void Mission_FallDamageCallback_delegate(int thisPointer, ref AttackCollisionData collisionData, Blow b, int attacker, int victim);

	internal delegate AgentState Mission_GetAgentState_delegate(int thisPointer, int affectorAgent, int agent, DamageTypes damageType, WeaponFlags weaponFlags);

	internal delegate WorldPosition Mission_GetClosestFleePositionForAgent_delegate(int thisPointer, int agent);

	internal delegate void Mission_GetDefendCollisionResults_delegate(int thisPointer, int attackerAgent, int defenderAgent, CombatCollisionResult collisionResult, int attackerWeaponSlotIndex, [MarshalAs(UnmanagedType.U1)] bool isAlternativeAttack, StrikeType strikeType, Agent.UsageDirection attackDirection, float collisionDistanceOnWeapon, float attackProgress, [MarshalAs(UnmanagedType.U1)] bool attackIsParried, [MarshalAs(UnmanagedType.U1)] bool isPassiveUsageHit, [MarshalAs(UnmanagedType.U1)] bool isHeavyAttack, ref float defenderStunPeriod, ref float attackerStunPeriod, [MarshalAs(UnmanagedType.U1)] ref bool crushedThrough);

	internal delegate void Mission_MeleeHitCallback_delegate(int thisPointer, ref AttackCollisionData collisionData, int attacker, int victim, NativeObjectPointer realHitEntity, ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction, CrushThroughState crushThroughState, Vec3 blowDir, Vec3 swingDir, ref HitParticleResultData hitParticleResultData, [MarshalAs(UnmanagedType.U1)] bool crushedThroughWithoutAgentCollision);

	internal delegate void Mission_MissileAreaDamageCallback_delegate(int thisPointer, ref AttackCollisionData collisionDataInput, ref Blow blowInput, int alreadyDamagedAgent, int shooterAgent, [MarshalAs(UnmanagedType.U1)] bool isBigExplosion);

	internal delegate void Mission_MissileCalculatePassbySoundParametersCallbackMT_delegate(int thisPointer, int missileIndex, ref SoundEventParameter soundEventParameter);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool Mission_MissileHitCallback_delegate(int thisPointer, out int extraHitParticleIndex, ref AttackCollisionData collisionData, Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity, MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, int attacker, int victim, NativeObjectPointer hitEntity);

	internal delegate void Mission_OnAgentAddedAsCorpse_delegate(int thisPointer, int affectedAgent, int corpsesToFadeIndex);

	internal delegate void Mission_OnAgentDeleted_delegate(int thisPointer, int affectedAgent);

	internal delegate float Mission_OnAgentHitBlocked_delegate(int thisPointer, int affectedAgent, int affectorAgent, ref AttackCollisionData collisionData, Vec3 blowDirection, Vec3 swingDirection, [MarshalAs(UnmanagedType.U1)] bool isMissile);

	internal delegate void Mission_OnAgentRemoved_delegate(int thisPointer, int affectedAgent, int affectorAgent, AgentState agentState, KillingBlow killingBlow);

	internal delegate void Mission_OnAgentShootMissile_delegate(int thisPointer, int shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, [MarshalAs(UnmanagedType.U1)] bool hasRigidBody, [MarshalAs(UnmanagedType.U1)] bool isPrimaryWeaponShot, int forcedMissileIndex);

	internal delegate void Mission_OnFixedTick_delegate(int thisPointer, float fixedDt);

	internal delegate void Mission_OnMissileRemoved_delegate(int thisPointer, int missileIndex);

	internal delegate void Mission_OnPreTick_delegate(int thisPointer, float dt);

	internal delegate void Mission_OnSceneCreated_delegate(int thisPointer, NativeObjectPointer scene);

	internal delegate void Mission_PauseMission_delegate(int thisPointer);

	internal delegate void Mission_ResetMission_delegate(int thisPointer);

	internal delegate void Mission_SpawnWeaponAsDropFromAgent_delegate(int thisPointer, int agent, EquipmentIndex equipmentIndex, ref Vec3 globalVelocity, ref Vec3 globalAngularVelocity, Mission.WeaponSpawnFlags spawnFlags);

	internal delegate void Mission_TickAgentsAndTeams_delegate(int thisPointer, float dt, [MarshalAs(UnmanagedType.U1)] bool tickPaused);

	internal delegate void Mission_UpdateMissionTimeCache_delegate(int thisPointer, float curTime);

	internal delegate UIntPtr Module_CreateProcessedActionSetsXMLForNative_delegate();

	internal delegate UIntPtr Module_CreateProcessedActionTypesXMLForNative_delegate();

	internal delegate UIntPtr Module_CreateProcessedAnimationsXMLForNative_delegate(out string animationsXmlPaths);

	internal delegate UIntPtr Module_CreateProcessedItemHolstersXMLForNative_delegate(out string baseItemHolstersPath);

	internal delegate UIntPtr Module_CreateProcessedModuleDataXMLForNative_delegate(IntPtr xmlType);

	internal delegate UIntPtr Module_CreateProcessedSkinsXMLForNative_delegate(out string baseSkinsXmlPath);

	internal delegate UIntPtr Module_CreateProcessedSoundEventDataXMLForNative_delegate();

	internal delegate UIntPtr Module_CreateProcessedSoundParamsXMLForNative_delegate();

	internal delegate UIntPtr Module_CreateProcessedVoiceDefinitionsXMLForNative_delegate();

	internal delegate UIntPtr Module_GetGameStatus_delegate();

	internal delegate UIntPtr Module_GetHorseMaterialNames_delegate(int thisPointer);

	internal delegate int Module_GetInstance_delegate();

	internal delegate UIntPtr Module_GetItemMeshNames_delegate(int thisPointer);

	internal delegate UIntPtr Module_GetMetaMeshPackageMapping_delegate(int thisPointer);

	internal delegate UIntPtr Module_GetMissionControllerClassNames_delegate(int thisPointer);

	internal delegate void Module_Initialize_delegate(int thisPointer);

	internal delegate void Module_LoadSingleModule_delegate(int thisPointer, IntPtr modulePath);

	internal delegate void Module_MBThrowException_delegate();

	internal delegate void Module_OnCloseSceneEditorPresentation_delegate(int thisPointer);

	internal delegate void Module_OnDumpCreated_delegate(int thisPointer);

	internal delegate void Module_OnDumpCreationStarted_delegate(int thisPointer);

	internal delegate void Module_OnEnterEditMode_delegate(int thisPointer, [MarshalAs(UnmanagedType.U1)] bool isFirstTime);

	internal delegate void Module_OnImguiProfilerTick_delegate(int thisPointer);

	internal delegate void Module_OnSceneEditorModeOver_delegate(int thisPointer);

	internal delegate void Module_OnSkinsXMLHasChanged_delegate(int thisPointer);

	internal delegate void Module_RunTest_delegate(int thisPointer, IntPtr commandLine);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool Module_SetEditorScreenAsRootScreen_delegate(int thisPointer);

	internal delegate void Module_SetLoadingFinished_delegate(int thisPointer);

	internal delegate void Module_StartMissionForEditor_delegate(int thisPointer, IntPtr missionName, IntPtr sceneName, IntPtr levels);

	internal delegate void Module_StartMissionForReplayEditor_delegate(int thisPointer, IntPtr missionName, IntPtr sceneName, IntPtr levels, IntPtr fileName, [MarshalAs(UnmanagedType.U1)] bool record, float startTime, float endTime);

	internal delegate void Module_TickTest_delegate(int thisPointer, float dt);

	internal delegate Vec3 WeaponComponentMissionExtensions_CalculateCenterOfMass_delegate(NativeObjectPointer body);

	internal static Delegate[] Delegates { get; private set; }

	public static void Initialize()
	{
		Delegates = new Delegate[111];
		Delegates[0] = new Agent_DebugGetHealth_delegate(Agent_DebugGetHealth);
		Delegates[1] = new Agent_GetFormationUnitSpacing_delegate(Agent_GetFormationUnitSpacing);
		Delegates[2] = new Agent_GetMissileRangeWithHeightDifferenceAux_delegate(Agent_GetMissileRangeWithHeightDifferenceAux);
		Delegates[3] = new Agent_GetSoundAndCollisionInfoClassName_delegate(Agent_GetSoundAndCollisionInfoClassName);
		Delegates[4] = new Agent_GetWeaponInaccuracy_delegate(Agent_GetWeaponInaccuracy);
		Delegates[5] = new Agent_IsInSameFormationWith_delegate(Agent_IsInSameFormationWith);
		Delegates[6] = new Agent_OnAgentAlarmedStateChanged_delegate(Agent_OnAgentAlarmedStateChanged);
		Delegates[7] = new Agent_OnAIInputSet_delegate(Agent_OnAIInputSet);
		Delegates[8] = new Agent_OnDismount_delegate(Agent_OnDismount);
		Delegates[9] = new Agent_OnMount_delegate(Agent_OnMount);
		Delegates[10] = new Agent_OnRemoveWeapon_delegate(Agent_OnRemoveWeapon);
		Delegates[11] = new Agent_OnRetreating_delegate(Agent_OnRetreating);
		Delegates[12] = new Agent_OnShieldDamaged_delegate(Agent_OnShieldDamaged);
		Delegates[13] = new Agent_OnWeaponAmmoConsume_delegate(Agent_OnWeaponAmmoConsume);
		Delegates[14] = new Agent_OnWeaponAmmoReload_delegate(Agent_OnWeaponAmmoReload);
		Delegates[15] = new Agent_OnWeaponAmmoRemoved_delegate(Agent_OnWeaponAmmoRemoved);
		Delegates[16] = new Agent_OnWeaponAmountChange_delegate(Agent_OnWeaponAmountChange);
		Delegates[17] = new Agent_OnWeaponReloadPhaseChange_delegate(Agent_OnWeaponReloadPhaseChange);
		Delegates[18] = new Agent_OnWeaponSwitchingToAlternativeStart_delegate(Agent_OnWeaponSwitchingToAlternativeStart);
		Delegates[19] = new Agent_OnWeaponUsageIndexChange_delegate(Agent_OnWeaponUsageIndexChange);
		Delegates[20] = new Agent_OnWieldedItemIndexChange_delegate(Agent_OnWieldedItemIndexChange);
		Delegates[21] = new Agent_SetAgentAIPerformingRetreatBehavior_delegate(Agent_SetAgentAIPerformingRetreatBehavior);
		Delegates[22] = new Agent_UpdateAgentStats_delegate(Agent_UpdateAgentStats);
		Delegates[23] = new Agent_UpdateMountAgentCache_delegate(Agent_UpdateMountAgentCache);
		Delegates[24] = new Agent_UpdateRiderAgentCache_delegate(Agent_UpdateRiderAgentCache);
		Delegates[25] = new BannerlordTableauManager_RegisterCharacterTableauScene_delegate(BannerlordTableauManager_RegisterCharacterTableauScene);
		Delegates[26] = new BannerlordTableauManager_RequestCharacterTableauSetup_delegate(BannerlordTableauManager_RequestCharacterTableauSetup);
		Delegates[27] = new CoreManaged_CheckSharedStructureSizes_delegate(CoreManaged_CheckSharedStructureSizes);
		Delegates[28] = new CoreManaged_EngineApiMethodInterfaceInitializer_delegate(CoreManaged_EngineApiMethodInterfaceInitializer);
		Delegates[29] = new CoreManaged_FillEngineApiPointers_delegate(CoreManaged_FillEngineApiPointers);
		Delegates[30] = new CoreManaged_Finalize_delegate(CoreManaged_Finalize);
		Delegates[31] = new CoreManaged_OnLoadCommonFinished_delegate(CoreManaged_OnLoadCommonFinished);
		Delegates[32] = new CoreManaged_Start_delegate(CoreManaged_Start);
		Delegates[33] = new GameNetwork_HandleConsoleCommand_delegate(GameNetwork_HandleConsoleCommand);
		Delegates[34] = new GameNetwork_HandleDisconnect_delegate(GameNetwork_HandleDisconnect);
		Delegates[35] = new GameNetwork_HandleNetworkPacketAsClient_delegate(GameNetwork_HandleNetworkPacketAsClient);
		Delegates[36] = new GameNetwork_HandleNetworkPacketAsServer_delegate(GameNetwork_HandleNetworkPacketAsServer);
		Delegates[37] = new GameNetwork_HandleRemovePlayer_delegate(GameNetwork_HandleRemovePlayer);
		Delegates[38] = new GameNetwork_SyncRelevantGameOptionsToServer_delegate(GameNetwork_SyncRelevantGameOptionsToServer);
		Delegates[39] = new ManagedOptions_GetConfigCount_delegate(ManagedOptions_GetConfigCount);
		Delegates[40] = new ManagedOptions_GetConfigValue_delegate(ManagedOptions_GetConfigValue);
		Delegates[41] = new MBEditor_CloseEditorScene_delegate(MBEditor_CloseEditorScene);
		Delegates[42] = new MBEditor_DestroyEditor_delegate(MBEditor_DestroyEditor);
		Delegates[43] = new MBEditor_SetEditorScene_delegate(MBEditor_SetEditorScene);
		Delegates[44] = new MBMultiplayerData_GetCurrentPlayerCount_delegate(MBMultiplayerData_GetCurrentPlayerCount);
		Delegates[45] = new MBMultiplayerData_GetGameModule_delegate(MBMultiplayerData_GetGameModule);
		Delegates[46] = new MBMultiplayerData_GetGameType_delegate(MBMultiplayerData_GetGameType);
		Delegates[47] = new MBMultiplayerData_GetMap_delegate(MBMultiplayerData_GetMap);
		Delegates[48] = new MBMultiplayerData_GetPlayerCountLimit_delegate(MBMultiplayerData_GetPlayerCountLimit);
		Delegates[49] = new MBMultiplayerData_GetServerId_delegate(MBMultiplayerData_GetServerId);
		Delegates[50] = new MBMultiplayerData_GetServerName_delegate(MBMultiplayerData_GetServerName);
		Delegates[51] = new MBMultiplayerData_UpdateGameServerInfo_delegate(MBMultiplayerData_UpdateGameServerInfo);
		Delegates[52] = new Mission_ApplySkeletonScaleToAllEquippedItems_delegate(Mission_ApplySkeletonScaleToAllEquippedItems);
		Delegates[53] = new Mission_CanPhysicsCollideBetweenTwoEntities_delegate(Mission_CanPhysicsCollideBetweenTwoEntities);
		Delegates[54] = new Mission_ChargeDamageCallback_delegate(Mission_ChargeDamageCallback);
		Delegates[55] = new Mission_DebugLogNativeMissionNetworkEvent_delegate(Mission_DebugLogNativeMissionNetworkEvent);
		Delegates[56] = new Mission_EndMission_delegate(Mission_EndMission);
		Delegates[57] = new Mission_FallDamageCallback_delegate(Mission_FallDamageCallback);
		Delegates[58] = new Mission_GetAgentState_delegate(Mission_GetAgentState);
		Delegates[59] = new Mission_GetClosestFleePositionForAgent_delegate(Mission_GetClosestFleePositionForAgent);
		Delegates[60] = new Mission_GetDefendCollisionResults_delegate(Mission_GetDefendCollisionResults);
		Delegates[61] = new Mission_MeleeHitCallback_delegate(Mission_MeleeHitCallback);
		Delegates[62] = new Mission_MissileAreaDamageCallback_delegate(Mission_MissileAreaDamageCallback);
		Delegates[63] = new Mission_MissileCalculatePassbySoundParametersCallbackMT_delegate(Mission_MissileCalculatePassbySoundParametersCallbackMT);
		Delegates[64] = new Mission_MissileHitCallback_delegate(Mission_MissileHitCallback);
		Delegates[65] = new Mission_OnAgentAddedAsCorpse_delegate(Mission_OnAgentAddedAsCorpse);
		Delegates[66] = new Mission_OnAgentDeleted_delegate(Mission_OnAgentDeleted);
		Delegates[67] = new Mission_OnAgentHitBlocked_delegate(Mission_OnAgentHitBlocked);
		Delegates[68] = new Mission_OnAgentRemoved_delegate(Mission_OnAgentRemoved);
		Delegates[69] = new Mission_OnAgentShootMissile_delegate(Mission_OnAgentShootMissile);
		Delegates[70] = new Mission_OnFixedTick_delegate(Mission_OnFixedTick);
		Delegates[71] = new Mission_OnMissileRemoved_delegate(Mission_OnMissileRemoved);
		Delegates[72] = new Mission_OnPreTick_delegate(Mission_OnPreTick);
		Delegates[73] = new Mission_OnSceneCreated_delegate(Mission_OnSceneCreated);
		Delegates[74] = new Mission_PauseMission_delegate(Mission_PauseMission);
		Delegates[75] = new Mission_ResetMission_delegate(Mission_ResetMission);
		Delegates[76] = new Mission_SpawnWeaponAsDropFromAgent_delegate(Mission_SpawnWeaponAsDropFromAgent);
		Delegates[77] = new Mission_TickAgentsAndTeams_delegate(Mission_TickAgentsAndTeams);
		Delegates[78] = new Mission_UpdateMissionTimeCache_delegate(Mission_UpdateMissionTimeCache);
		Delegates[79] = new Module_CreateProcessedActionSetsXMLForNative_delegate(Module_CreateProcessedActionSetsXMLForNative);
		Delegates[80] = new Module_CreateProcessedActionTypesXMLForNative_delegate(Module_CreateProcessedActionTypesXMLForNative);
		Delegates[81] = new Module_CreateProcessedAnimationsXMLForNative_delegate(Module_CreateProcessedAnimationsXMLForNative);
		Delegates[82] = new Module_CreateProcessedItemHolstersXMLForNative_delegate(Module_CreateProcessedItemHolstersXMLForNative);
		Delegates[83] = new Module_CreateProcessedModuleDataXMLForNative_delegate(Module_CreateProcessedModuleDataXMLForNative);
		Delegates[84] = new Module_CreateProcessedSkinsXMLForNative_delegate(Module_CreateProcessedSkinsXMLForNative);
		Delegates[85] = new Module_CreateProcessedSoundEventDataXMLForNative_delegate(Module_CreateProcessedSoundEventDataXMLForNative);
		Delegates[86] = new Module_CreateProcessedSoundParamsXMLForNative_delegate(Module_CreateProcessedSoundParamsXMLForNative);
		Delegates[87] = new Module_CreateProcessedVoiceDefinitionsXMLForNative_delegate(Module_CreateProcessedVoiceDefinitionsXMLForNative);
		Delegates[88] = new Module_GetGameStatus_delegate(Module_GetGameStatus);
		Delegates[89] = new Module_GetHorseMaterialNames_delegate(Module_GetHorseMaterialNames);
		Delegates[90] = new Module_GetInstance_delegate(Module_GetInstance);
		Delegates[91] = new Module_GetItemMeshNames_delegate(Module_GetItemMeshNames);
		Delegates[92] = new Module_GetMetaMeshPackageMapping_delegate(Module_GetMetaMeshPackageMapping);
		Delegates[93] = new Module_GetMissionControllerClassNames_delegate(Module_GetMissionControllerClassNames);
		Delegates[94] = new Module_Initialize_delegate(Module_Initialize);
		Delegates[95] = new Module_LoadSingleModule_delegate(Module_LoadSingleModule);
		Delegates[96] = new Module_MBThrowException_delegate(Module_MBThrowException);
		Delegates[97] = new Module_OnCloseSceneEditorPresentation_delegate(Module_OnCloseSceneEditorPresentation);
		Delegates[98] = new Module_OnDumpCreated_delegate(Module_OnDumpCreated);
		Delegates[99] = new Module_OnDumpCreationStarted_delegate(Module_OnDumpCreationStarted);
		Delegates[100] = new Module_OnEnterEditMode_delegate(Module_OnEnterEditMode);
		Delegates[101] = new Module_OnImguiProfilerTick_delegate(Module_OnImguiProfilerTick);
		Delegates[102] = new Module_OnSceneEditorModeOver_delegate(Module_OnSceneEditorModeOver);
		Delegates[103] = new Module_OnSkinsXMLHasChanged_delegate(Module_OnSkinsXMLHasChanged);
		Delegates[104] = new Module_RunTest_delegate(Module_RunTest);
		Delegates[105] = new Module_SetEditorScreenAsRootScreen_delegate(Module_SetEditorScreenAsRootScreen);
		Delegates[106] = new Module_SetLoadingFinished_delegate(Module_SetLoadingFinished);
		Delegates[107] = new Module_StartMissionForEditor_delegate(Module_StartMissionForEditor);
		Delegates[108] = new Module_StartMissionForReplayEditor_delegate(Module_StartMissionForReplayEditor);
		Delegates[109] = new Module_TickTest_delegate(Module_TickTest);
		Delegates[110] = new WeaponComponentMissionExtensions_CalculateCenterOfMass_delegate(WeaponComponentMissionExtensions_CalculateCenterOfMass);
	}

	[MonoPInvokeCallback(typeof(Agent_DebugGetHealth_delegate))]
	internal static float Agent_DebugGetHealth(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).DebugGetHealth();
	}

	[MonoPInvokeCallback(typeof(Agent_GetFormationUnitSpacing_delegate))]
	internal static int Agent_GetFormationUnitSpacing(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).GetFormationUnitSpacing();
	}

	[MonoPInvokeCallback(typeof(Agent_GetMissileRangeWithHeightDifferenceAux_delegate))]
	internal static float Agent_GetMissileRangeWithHeightDifferenceAux(int thisPointer, float targetZ)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).GetMissileRangeWithHeightDifferenceAux(targetZ);
	}

	[MonoPInvokeCallback(typeof(Agent_GetSoundAndCollisionInfoClassName_delegate))]
	internal static UIntPtr Agent_GetSoundAndCollisionInfoClassName(int thisPointer)
	{
		string soundAndCollisionInfoClassName = (DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).GetSoundAndCollisionInfoClassName();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, soundAndCollisionInfoClassName);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Agent_GetWeaponInaccuracy_delegate))]
	internal static float Agent_GetWeaponInaccuracy(int thisPointer, EquipmentIndex weaponSlotIndex, int weaponUsageIndex)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).GetWeaponInaccuracy(weaponSlotIndex, weaponUsageIndex);
	}

	[MonoPInvokeCallback(typeof(Agent_IsInSameFormationWith_delegate))]
	internal static bool Agent_IsInSameFormationWith(int thisPointer, int otherAgent)
	{
		Agent obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Agent;
		Agent otherAgent2 = DotNetObject.GetManagedObjectWithId(otherAgent) as Agent;
		return obj.IsInSameFormationWith(otherAgent2);
	}

	[MonoPInvokeCallback(typeof(Agent_OnAgentAlarmedStateChanged_delegate))]
	internal static void Agent_OnAgentAlarmedStateChanged(int thisPointer, Agent.AIStateFlag flag)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnAgentAlarmedStateChanged(flag);
	}

	[MonoPInvokeCallback(typeof(Agent_OnAIInputSet_delegate))]
	internal static void Agent_OnAIInputSet(int thisPointer, ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnAIInputSet(ref eventFlag, ref movementFlag, ref inputVector);
	}

	[MonoPInvokeCallback(typeof(Agent_OnDismount_delegate))]
	internal static void Agent_OnDismount(int thisPointer, int mount)
	{
		Agent obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Agent;
		Agent mount2 = DotNetObject.GetManagedObjectWithId(mount) as Agent;
		obj.OnDismount(mount2);
	}

	[MonoPInvokeCallback(typeof(Agent_OnMount_delegate))]
	internal static void Agent_OnMount(int thisPointer, int mount)
	{
		Agent obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Agent;
		Agent mount2 = DotNetObject.GetManagedObjectWithId(mount) as Agent;
		obj.OnMount(mount2);
	}

	[MonoPInvokeCallback(typeof(Agent_OnRemoveWeapon_delegate))]
	internal static void Agent_OnRemoveWeapon(int thisPointer, EquipmentIndex slotIndex)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnRemoveWeapon(slotIndex);
	}

	[MonoPInvokeCallback(typeof(Agent_OnRetreating_delegate))]
	internal static void Agent_OnRetreating(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnRetreating();
	}

	[MonoPInvokeCallback(typeof(Agent_OnShieldDamaged_delegate))]
	internal static void Agent_OnShieldDamaged(int thisPointer, EquipmentIndex slotIndex, int inflictedDamage)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnShieldDamaged(slotIndex, inflictedDamage);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWeaponAmmoConsume_delegate))]
	internal static void Agent_OnWeaponAmmoConsume(int thisPointer, EquipmentIndex slotIndex, short totalAmmo)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWeaponAmmoConsume(slotIndex, totalAmmo);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWeaponAmmoReload_delegate))]
	internal static void Agent_OnWeaponAmmoReload(int thisPointer, EquipmentIndex slotIndex, EquipmentIndex ammoSlotIndex, short totalAmmo)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWeaponAmmoReload(slotIndex, ammoSlotIndex, totalAmmo);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWeaponAmmoRemoved_delegate))]
	internal static void Agent_OnWeaponAmmoRemoved(int thisPointer, EquipmentIndex slotIndex)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWeaponAmmoRemoved(slotIndex);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWeaponAmountChange_delegate))]
	internal static void Agent_OnWeaponAmountChange(int thisPointer, EquipmentIndex slotIndex, short amount)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWeaponAmountChange(slotIndex, amount);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWeaponReloadPhaseChange_delegate))]
	internal static void Agent_OnWeaponReloadPhaseChange(int thisPointer, EquipmentIndex slotIndex, short reloadPhase)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWeaponReloadPhaseChange(slotIndex, reloadPhase);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWeaponSwitchingToAlternativeStart_delegate))]
	internal static void Agent_OnWeaponSwitchingToAlternativeStart(int thisPointer, EquipmentIndex slotIndex, int usageIndex)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWeaponSwitchingToAlternativeStart(slotIndex, usageIndex);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWeaponUsageIndexChange_delegate))]
	internal static void Agent_OnWeaponUsageIndexChange(int thisPointer, EquipmentIndex slotIndex, int usageIndex)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWeaponUsageIndexChange(slotIndex, usageIndex);
	}

	[MonoPInvokeCallback(typeof(Agent_OnWieldedItemIndexChange_delegate))]
	internal static void Agent_OnWieldedItemIndexChange(int thisPointer, bool isOffHand, bool isWieldedInstantly, bool isWieldedOnSpawn)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).OnWieldedItemIndexChange(isOffHand, isWieldedInstantly, isWieldedOnSpawn);
	}

	[MonoPInvokeCallback(typeof(Agent_SetAgentAIPerformingRetreatBehavior_delegate))]
	internal static void Agent_SetAgentAIPerformingRetreatBehavior(int thisPointer, bool isAgentAIPerformingRetreatBehavior)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).SetAgentAIPerformingRetreatBehavior(isAgentAIPerformingRetreatBehavior);
	}

	[MonoPInvokeCallback(typeof(Agent_UpdateAgentStats_delegate))]
	internal static void Agent_UpdateAgentStats(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Agent).UpdateAgentStats();
	}

	[MonoPInvokeCallback(typeof(Agent_UpdateMountAgentCache_delegate))]
	internal static void Agent_UpdateMountAgentCache(int thisPointer, int newMountAgent)
	{
		Agent obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Agent;
		Agent newMountAgent2 = DotNetObject.GetManagedObjectWithId(newMountAgent) as Agent;
		obj.UpdateMountAgentCache(newMountAgent2);
	}

	[MonoPInvokeCallback(typeof(Agent_UpdateRiderAgentCache_delegate))]
	internal static void Agent_UpdateRiderAgentCache(int thisPointer, int newRiderAgent)
	{
		Agent obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Agent;
		Agent newRiderAgent2 = DotNetObject.GetManagedObjectWithId(newRiderAgent) as Agent;
		obj.UpdateRiderAgentCache(newRiderAgent2);
	}

	[MonoPInvokeCallback(typeof(BannerlordTableauManager_RegisterCharacterTableauScene_delegate))]
	internal static void BannerlordTableauManager_RegisterCharacterTableauScene(NativeObjectPointer scene, int type)
	{
		Scene scene2 = null;
		if (scene.Pointer != UIntPtr.Zero)
		{
			scene2 = new Scene(scene.Pointer);
		}
		BannerlordTableauManager.RegisterCharacterTableauScene(scene2, type);
	}

	[MonoPInvokeCallback(typeof(BannerlordTableauManager_RequestCharacterTableauSetup_delegate))]
	internal static void BannerlordTableauManager_RequestCharacterTableauSetup(int characterCodeId, NativeObjectPointer scene, NativeObjectPointer poseEntity)
	{
		Scene scene2 = null;
		if (scene.Pointer != UIntPtr.Zero)
		{
			scene2 = new Scene(scene.Pointer);
		}
		GameEntity poseEntity2 = null;
		if (poseEntity.Pointer != UIntPtr.Zero)
		{
			poseEntity2 = new GameEntity(poseEntity.Pointer);
		}
		BannerlordTableauManager.RequestCharacterTableauSetup(characterCodeId, scene2, poseEntity2);
	}

	[MonoPInvokeCallback(typeof(CoreManaged_CheckSharedStructureSizes_delegate))]
	internal static void CoreManaged_CheckSharedStructureSizes()
	{
		CoreManaged.CheckSharedStructureSizes();
	}

	[MonoPInvokeCallback(typeof(CoreManaged_EngineApiMethodInterfaceInitializer_delegate))]
	internal static void CoreManaged_EngineApiMethodInterfaceInitializer(int id, IntPtr pointer)
	{
		CoreManaged.EngineApiMethodInterfaceInitializer(id, pointer);
	}

	[MonoPInvokeCallback(typeof(CoreManaged_FillEngineApiPointers_delegate))]
	internal static void CoreManaged_FillEngineApiPointers()
	{
		CoreManaged.FillEngineApiPointers();
	}

	[MonoPInvokeCallback(typeof(CoreManaged_Finalize_delegate))]
	internal static void CoreManaged_Finalize()
	{
		CoreManaged.Finalize();
	}

	[MonoPInvokeCallback(typeof(CoreManaged_OnLoadCommonFinished_delegate))]
	internal static void CoreManaged_OnLoadCommonFinished()
	{
		CoreManaged.OnLoadCommonFinished();
	}

	[MonoPInvokeCallback(typeof(CoreManaged_Start_delegate))]
	internal static void CoreManaged_Start()
	{
		CoreManaged.Start();
	}

	[MonoPInvokeCallback(typeof(GameNetwork_HandleConsoleCommand_delegate))]
	internal static void GameNetwork_HandleConsoleCommand(IntPtr command)
	{
		GameNetwork.HandleConsoleCommand(Marshal.PtrToStringAnsi(command));
	}

	[MonoPInvokeCallback(typeof(GameNetwork_HandleDisconnect_delegate))]
	internal static void GameNetwork_HandleDisconnect()
	{
		GameNetwork.HandleDisconnect();
	}

	[MonoPInvokeCallback(typeof(GameNetwork_HandleNetworkPacketAsClient_delegate))]
	internal static bool GameNetwork_HandleNetworkPacketAsClient()
	{
		return GameNetwork.HandleNetworkPacketAsClient();
	}

	[MonoPInvokeCallback(typeof(GameNetwork_HandleNetworkPacketAsServer_delegate))]
	internal static bool GameNetwork_HandleNetworkPacketAsServer(int networkPeer)
	{
		return GameNetwork.HandleNetworkPacketAsServer(DotNetObject.GetManagedObjectWithId(networkPeer) as MBNetworkPeer);
	}

	[MonoPInvokeCallback(typeof(GameNetwork_HandleRemovePlayer_delegate))]
	internal static void GameNetwork_HandleRemovePlayer(int peer, bool isTimedOut)
	{
		GameNetwork.HandleRemovePlayer(DotNetObject.GetManagedObjectWithId(peer) as MBNetworkPeer, isTimedOut);
	}

	[MonoPInvokeCallback(typeof(GameNetwork_SyncRelevantGameOptionsToServer_delegate))]
	internal static void GameNetwork_SyncRelevantGameOptionsToServer()
	{
		GameNetwork.SyncRelevantGameOptionsToServer();
	}

	[MonoPInvokeCallback(typeof(ManagedOptions_GetConfigCount_delegate))]
	internal static int ManagedOptions_GetConfigCount()
	{
		return ManagedOptions.GetConfigCount();
	}

	[MonoPInvokeCallback(typeof(ManagedOptions_GetConfigValue_delegate))]
	internal static float ManagedOptions_GetConfigValue(int type)
	{
		return ManagedOptions.GetConfigValue(type);
	}

	[MonoPInvokeCallback(typeof(MBEditor_CloseEditorScene_delegate))]
	internal static void MBEditor_CloseEditorScene()
	{
		MBEditor.CloseEditorScene();
	}

	[MonoPInvokeCallback(typeof(MBEditor_DestroyEditor_delegate))]
	internal static void MBEditor_DestroyEditor(NativeObjectPointer scene)
	{
		Scene scene2 = null;
		if (scene.Pointer != UIntPtr.Zero)
		{
			scene2 = new Scene(scene.Pointer);
		}
		MBEditor.DestroyEditor(scene2);
	}

	[MonoPInvokeCallback(typeof(MBEditor_SetEditorScene_delegate))]
	internal static void MBEditor_SetEditorScene(NativeObjectPointer scene)
	{
		Scene editorScene = null;
		if (scene.Pointer != UIntPtr.Zero)
		{
			editorScene = new Scene(scene.Pointer);
		}
		MBEditor.SetEditorScene(editorScene);
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_GetCurrentPlayerCount_delegate))]
	internal static int MBMultiplayerData_GetCurrentPlayerCount()
	{
		return MBMultiplayerData.GetCurrentPlayerCount();
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_GetGameModule_delegate))]
	internal static UIntPtr MBMultiplayerData_GetGameModule()
	{
		string gameModule = MBMultiplayerData.GetGameModule();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, gameModule);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_GetGameType_delegate))]
	internal static UIntPtr MBMultiplayerData_GetGameType()
	{
		string gameType = MBMultiplayerData.GetGameType();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, gameType);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_GetMap_delegate))]
	internal static UIntPtr MBMultiplayerData_GetMap()
	{
		string map = MBMultiplayerData.GetMap();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, map);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_GetPlayerCountLimit_delegate))]
	internal static int MBMultiplayerData_GetPlayerCountLimit()
	{
		return MBMultiplayerData.GetPlayerCountLimit();
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_GetServerId_delegate))]
	internal static UIntPtr MBMultiplayerData_GetServerId()
	{
		string serverId = MBMultiplayerData.GetServerId();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, serverId);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_GetServerName_delegate))]
	internal static UIntPtr MBMultiplayerData_GetServerName()
	{
		string serverName = MBMultiplayerData.GetServerName();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, serverName);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(MBMultiplayerData_UpdateGameServerInfo_delegate))]
	internal static void MBMultiplayerData_UpdateGameServerInfo(IntPtr id, IntPtr gameServer, IntPtr gameModule, IntPtr gameType, IntPtr map, int currentPlayerCount, int maxPlayerCount, IntPtr address, int port)
	{
		string? id2 = Marshal.PtrToStringAnsi(id);
		string gameServer2 = Marshal.PtrToStringAnsi(gameServer);
		string gameModule2 = Marshal.PtrToStringAnsi(gameModule);
		string gameType2 = Marshal.PtrToStringAnsi(gameType);
		string map2 = Marshal.PtrToStringAnsi(map);
		string address2 = Marshal.PtrToStringAnsi(address);
		MBMultiplayerData.UpdateGameServerInfo(id2, gameServer2, gameModule2, gameType2, map2, currentPlayerCount, maxPlayerCount, address2, port);
	}

	[MonoPInvokeCallback(typeof(Mission_ApplySkeletonScaleToAllEquippedItems_delegate))]
	internal static void Mission_ApplySkeletonScaleToAllEquippedItems(int thisPointer, IntPtr itemName)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		string itemName2 = Marshal.PtrToStringAnsi(itemName);
		obj.ApplySkeletonScaleToAllEquippedItems(itemName2);
	}

	[MonoPInvokeCallback(typeof(Mission_CanPhysicsCollideBetweenTwoEntities_delegate))]
	internal static bool Mission_CanPhysicsCollideBetweenTwoEntities(int thisPointer, UIntPtr entity0Ptr, UIntPtr entity1Ptr)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).CanPhysicsCollideBetweenTwoEntities(entity0Ptr, entity1Ptr);
	}

	[MonoPInvokeCallback(typeof(Mission_ChargeDamageCallback_delegate))]
	internal static void Mission_ChargeDamageCallback(int thisPointer, ref AttackCollisionData collisionData, Blow blow, int attacker, int victim)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent attacker2 = DotNetObject.GetManagedObjectWithId(attacker) as Agent;
		Agent victim2 = DotNetObject.GetManagedObjectWithId(victim) as Agent;
		obj.ChargeDamageCallback(ref collisionData, blow, attacker2, victim2);
	}

	[MonoPInvokeCallback(typeof(Mission_DebugLogNativeMissionNetworkEvent_delegate))]
	internal static void Mission_DebugLogNativeMissionNetworkEvent(int eventEnum, IntPtr eventName, int bitCount)
	{
		string eventName2 = Marshal.PtrToStringAnsi(eventName);
		Mission.DebugLogNativeMissionNetworkEvent(eventEnum, eventName2, bitCount);
	}

	[MonoPInvokeCallback(typeof(Mission_EndMission_delegate))]
	internal static void Mission_EndMission(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).EndMission();
	}

	[MonoPInvokeCallback(typeof(Mission_FallDamageCallback_delegate))]
	internal static void Mission_FallDamageCallback(int thisPointer, ref AttackCollisionData collisionData, Blow b, int attacker, int victim)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent attacker2 = DotNetObject.GetManagedObjectWithId(attacker) as Agent;
		Agent victim2 = DotNetObject.GetManagedObjectWithId(victim) as Agent;
		obj.FallDamageCallback(ref collisionData, b, attacker2, victim2);
	}

	[MonoPInvokeCallback(typeof(Mission_GetAgentState_delegate))]
	internal static AgentState Mission_GetAgentState(int thisPointer, int affectorAgent, int agent, DamageTypes damageType, WeaponFlags weaponFlags)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent affectorAgent2 = DotNetObject.GetManagedObjectWithId(affectorAgent) as Agent;
		Agent agent2 = DotNetObject.GetManagedObjectWithId(agent) as Agent;
		return obj.GetAgentState(affectorAgent2, agent2, damageType, weaponFlags);
	}

	[MonoPInvokeCallback(typeof(Mission_GetClosestFleePositionForAgent_delegate))]
	internal static WorldPosition Mission_GetClosestFleePositionForAgent(int thisPointer, int agent)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent agent2 = DotNetObject.GetManagedObjectWithId(agent) as Agent;
		return obj.GetClosestFleePositionForAgent(agent2);
	}

	[MonoPInvokeCallback(typeof(Mission_GetDefendCollisionResults_delegate))]
	internal static void Mission_GetDefendCollisionResults(int thisPointer, int attackerAgent, int defenderAgent, CombatCollisionResult collisionResult, int attackerWeaponSlotIndex, bool isAlternativeAttack, StrikeType strikeType, Agent.UsageDirection attackDirection, float collisionDistanceOnWeapon, float attackProgress, bool attackIsParried, bool isPassiveUsageHit, bool isHeavyAttack, ref float defenderStunPeriod, ref float attackerStunPeriod, ref bool crushedThrough)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent attackerAgent2 = DotNetObject.GetManagedObjectWithId(attackerAgent) as Agent;
		Agent defenderAgent2 = DotNetObject.GetManagedObjectWithId(defenderAgent) as Agent;
		obj.GetDefendCollisionResults(attackerAgent2, defenderAgent2, collisionResult, attackerWeaponSlotIndex, isAlternativeAttack, strikeType, attackDirection, collisionDistanceOnWeapon, attackProgress, attackIsParried, isPassiveUsageHit, isHeavyAttack, ref defenderStunPeriod, ref attackerStunPeriod, ref crushedThrough);
	}

	[MonoPInvokeCallback(typeof(Mission_MeleeHitCallback_delegate))]
	internal static void Mission_MeleeHitCallback(int thisPointer, ref AttackCollisionData collisionData, int attacker, int victim, NativeObjectPointer realHitEntity, ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction, CrushThroughState crushThroughState, Vec3 blowDir, Vec3 swingDir, ref HitParticleResultData hitParticleResultData, bool crushedThroughWithoutAgentCollision)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent attacker2 = DotNetObject.GetManagedObjectWithId(attacker) as Agent;
		Agent victim2 = DotNetObject.GetManagedObjectWithId(victim) as Agent;
		GameEntity realHitEntity2 = null;
		if (realHitEntity.Pointer != UIntPtr.Zero)
		{
			realHitEntity2 = new GameEntity(realHitEntity.Pointer);
		}
		obj.MeleeHitCallback(ref collisionData, attacker2, victim2, realHitEntity2, ref inOutMomentumRemaining, ref colReaction, crushThroughState, blowDir, swingDir, ref hitParticleResultData, crushedThroughWithoutAgentCollision);
	}

	[MonoPInvokeCallback(typeof(Mission_MissileAreaDamageCallback_delegate))]
	internal static void Mission_MissileAreaDamageCallback(int thisPointer, ref AttackCollisionData collisionDataInput, ref Blow blowInput, int alreadyDamagedAgent, int shooterAgent, bool isBigExplosion)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent alreadyDamagedAgent2 = DotNetObject.GetManagedObjectWithId(alreadyDamagedAgent) as Agent;
		Agent shooterAgent2 = DotNetObject.GetManagedObjectWithId(shooterAgent) as Agent;
		obj.MissileAreaDamageCallback(ref collisionDataInput, ref blowInput, alreadyDamagedAgent2, shooterAgent2, isBigExplosion);
	}

	[MonoPInvokeCallback(typeof(Mission_MissileCalculatePassbySoundParametersCallbackMT_delegate))]
	internal static void Mission_MissileCalculatePassbySoundParametersCallbackMT(int thisPointer, int missileIndex, ref SoundEventParameter soundEventParameter)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).MissileCalculatePassbySoundParametersCallbackMT(missileIndex, ref soundEventParameter);
	}

	[MonoPInvokeCallback(typeof(Mission_MissileHitCallback_delegate))]
	internal static bool Mission_MissileHitCallback(int thisPointer, out int extraHitParticleIndex, ref AttackCollisionData collisionData, Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity, MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, int attacker, int victim, NativeObjectPointer hitEntity)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent attacker2 = DotNetObject.GetManagedObjectWithId(attacker) as Agent;
		Agent victim2 = DotNetObject.GetManagedObjectWithId(victim) as Agent;
		GameEntity hitEntity2 = null;
		if (hitEntity.Pointer != UIntPtr.Zero)
		{
			hitEntity2 = new GameEntity(hitEntity.Pointer);
		}
		return obj.MissileHitCallback(out extraHitParticleIndex, ref collisionData, missileStartingPosition, missilePosition, missileAngularVelocity, movementVelocity, attachGlobalFrame, affectedShieldGlobalFrame, numDamagedAgents, attacker2, victim2, hitEntity2);
	}

	[MonoPInvokeCallback(typeof(Mission_OnAgentAddedAsCorpse_delegate))]
	internal static void Mission_OnAgentAddedAsCorpse(int thisPointer, int affectedAgent, int corpsesToFadeIndex)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent affectedAgent2 = DotNetObject.GetManagedObjectWithId(affectedAgent) as Agent;
		obj.OnAgentAddedAsCorpse(affectedAgent2, corpsesToFadeIndex);
	}

	[MonoPInvokeCallback(typeof(Mission_OnAgentDeleted_delegate))]
	internal static void Mission_OnAgentDeleted(int thisPointer, int affectedAgent)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent affectedAgent2 = DotNetObject.GetManagedObjectWithId(affectedAgent) as Agent;
		obj.OnAgentDeleted(affectedAgent2);
	}

	[MonoPInvokeCallback(typeof(Mission_OnAgentHitBlocked_delegate))]
	internal static float Mission_OnAgentHitBlocked(int thisPointer, int affectedAgent, int affectorAgent, ref AttackCollisionData collisionData, Vec3 blowDirection, Vec3 swingDirection, bool isMissile)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent affectedAgent2 = DotNetObject.GetManagedObjectWithId(affectedAgent) as Agent;
		Agent affectorAgent2 = DotNetObject.GetManagedObjectWithId(affectorAgent) as Agent;
		return obj.OnAgentHitBlocked(affectedAgent2, affectorAgent2, ref collisionData, blowDirection, swingDirection, isMissile);
	}

	[MonoPInvokeCallback(typeof(Mission_OnAgentRemoved_delegate))]
	internal static void Mission_OnAgentRemoved(int thisPointer, int affectedAgent, int affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent affectedAgent2 = DotNetObject.GetManagedObjectWithId(affectedAgent) as Agent;
		Agent affectorAgent2 = DotNetObject.GetManagedObjectWithId(affectorAgent) as Agent;
		obj.OnAgentRemoved(affectedAgent2, affectorAgent2, agentState, killingBlow);
	}

	[MonoPInvokeCallback(typeof(Mission_OnAgentShootMissile_delegate))]
	internal static void Mission_OnAgentShootMissile(int thisPointer, int shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, bool isPrimaryWeaponShot, int forcedMissileIndex)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent shooterAgent2 = DotNetObject.GetManagedObjectWithId(shooterAgent) as Agent;
		obj.OnAgentShootMissile(shooterAgent2, weaponIndex, position, velocity, orientation, hasRigidBody, isPrimaryWeaponShot, forcedMissileIndex);
	}

	[MonoPInvokeCallback(typeof(Mission_OnFixedTick_delegate))]
	internal static void Mission_OnFixedTick(int thisPointer, float fixedDt)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).OnFixedTick(fixedDt);
	}

	[MonoPInvokeCallback(typeof(Mission_OnMissileRemoved_delegate))]
	internal static void Mission_OnMissileRemoved(int thisPointer, int missileIndex)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).OnMissileRemoved(missileIndex);
	}

	[MonoPInvokeCallback(typeof(Mission_OnPreTick_delegate))]
	internal static void Mission_OnPreTick(int thisPointer, float dt)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).OnPreTick(dt);
	}

	[MonoPInvokeCallback(typeof(Mission_OnSceneCreated_delegate))]
	internal static void Mission_OnSceneCreated(int thisPointer, NativeObjectPointer scene)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Scene scene2 = null;
		if (scene.Pointer != UIntPtr.Zero)
		{
			scene2 = new Scene(scene.Pointer);
		}
		obj.OnSceneCreated(scene2);
	}

	[MonoPInvokeCallback(typeof(Mission_PauseMission_delegate))]
	internal static void Mission_PauseMission(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).PauseMission();
	}

	[MonoPInvokeCallback(typeof(Mission_ResetMission_delegate))]
	internal static void Mission_ResetMission(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).ResetMission();
	}

	[MonoPInvokeCallback(typeof(Mission_SpawnWeaponAsDropFromAgent_delegate))]
	internal static void Mission_SpawnWeaponAsDropFromAgent(int thisPointer, int agent, EquipmentIndex equipmentIndex, ref Vec3 globalVelocity, ref Vec3 globalAngularVelocity, Mission.WeaponSpawnFlags spawnFlags)
	{
		Mission obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Mission;
		Agent agent2 = DotNetObject.GetManagedObjectWithId(agent) as Agent;
		obj.SpawnWeaponAsDropFromAgent(agent2, equipmentIndex, ref globalVelocity, ref globalAngularVelocity, spawnFlags);
	}

	[MonoPInvokeCallback(typeof(Mission_TickAgentsAndTeams_delegate))]
	internal static void Mission_TickAgentsAndTeams(int thisPointer, float dt, bool tickPaused)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).TickAgentsAndTeams(dt, tickPaused);
	}

	[MonoPInvokeCallback(typeof(Mission_UpdateMissionTimeCache_delegate))]
	internal static void Mission_UpdateMissionTimeCache(int thisPointer, float curTime)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Mission).UpdateMissionTimeCache(curTime);
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedActionSetsXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedActionSetsXMLForNative()
	{
		string text = Module.CreateProcessedActionSetsXMLForNative();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedActionTypesXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedActionTypesXMLForNative()
	{
		string text = Module.CreateProcessedActionTypesXMLForNative();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedAnimationsXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedAnimationsXMLForNative(out string animationsXmlPaths)
	{
		string text = Module.CreateProcessedAnimationsXMLForNative(out animationsXmlPaths);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedItemHolstersXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedItemHolstersXMLForNative(out string baseItemHolstersPath)
	{
		string text = Module.CreateProcessedItemHolstersXMLForNative(out baseItemHolstersPath);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedModuleDataXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedModuleDataXMLForNative(IntPtr xmlType)
	{
		string text = Module.CreateProcessedModuleDataXMLForNative(Marshal.PtrToStringAnsi(xmlType));
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedSkinsXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedSkinsXMLForNative(out string baseSkinsXmlPath)
	{
		string text = Module.CreateProcessedSkinsXMLForNative(out baseSkinsXmlPath);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedSoundEventDataXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedSoundEventDataXMLForNative()
	{
		string text = Module.CreateProcessedSoundEventDataXMLForNative();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedSoundParamsXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedSoundParamsXMLForNative()
	{
		string text = Module.CreateProcessedSoundParamsXMLForNative();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_CreateProcessedVoiceDefinitionsXMLForNative_delegate))]
	internal static UIntPtr Module_CreateProcessedVoiceDefinitionsXMLForNative()
	{
		string text = Module.CreateProcessedVoiceDefinitionsXMLForNative();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_GetGameStatus_delegate))]
	internal static UIntPtr Module_GetGameStatus()
	{
		string gameStatus = Module.GetGameStatus();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, gameStatus);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_GetHorseMaterialNames_delegate))]
	internal static UIntPtr Module_GetHorseMaterialNames(int thisPointer)
	{
		string horseMaterialNames = (DotNetObject.GetManagedObjectWithId(thisPointer) as Module).GetHorseMaterialNames();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, horseMaterialNames);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_GetInstance_delegate))]
	internal static int Module_GetInstance()
	{
		return Module.GetInstance().GetManagedId();
	}

	[MonoPInvokeCallback(typeof(Module_GetItemMeshNames_delegate))]
	internal static UIntPtr Module_GetItemMeshNames(int thisPointer)
	{
		string itemMeshNames = (DotNetObject.GetManagedObjectWithId(thisPointer) as Module).GetItemMeshNames();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, itemMeshNames);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_GetMetaMeshPackageMapping_delegate))]
	internal static UIntPtr Module_GetMetaMeshPackageMapping(int thisPointer)
	{
		string metaMeshPackageMapping = (DotNetObject.GetManagedObjectWithId(thisPointer) as Module).GetMetaMeshPackageMapping();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, metaMeshPackageMapping);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_GetMissionControllerClassNames_delegate))]
	internal static UIntPtr Module_GetMissionControllerClassNames(int thisPointer)
	{
		string missionControllerClassNames = (DotNetObject.GetManagedObjectWithId(thisPointer) as Module).GetMissionControllerClassNames();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, missionControllerClassNames);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Module_Initialize_delegate))]
	internal static void Module_Initialize(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).Initialize();
	}

	[MonoPInvokeCallback(typeof(Module_LoadSingleModule_delegate))]
	internal static void Module_LoadSingleModule(int thisPointer, IntPtr modulePath)
	{
		Module obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Module;
		string modulePath2 = Marshal.PtrToStringAnsi(modulePath);
		obj.LoadSingleModule(modulePath2);
	}

	[MonoPInvokeCallback(typeof(Module_MBThrowException_delegate))]
	internal static void Module_MBThrowException()
	{
		Module.MBThrowException();
	}

	[MonoPInvokeCallback(typeof(Module_OnCloseSceneEditorPresentation_delegate))]
	internal static void Module_OnCloseSceneEditorPresentation(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).OnCloseSceneEditorPresentation();
	}

	[MonoPInvokeCallback(typeof(Module_OnDumpCreated_delegate))]
	internal static void Module_OnDumpCreated(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).OnDumpCreated();
	}

	[MonoPInvokeCallback(typeof(Module_OnDumpCreationStarted_delegate))]
	internal static void Module_OnDumpCreationStarted(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).OnDumpCreationStarted();
	}

	[MonoPInvokeCallback(typeof(Module_OnEnterEditMode_delegate))]
	internal static void Module_OnEnterEditMode(int thisPointer, bool isFirstTime)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).OnEnterEditMode(isFirstTime);
	}

	[MonoPInvokeCallback(typeof(Module_OnImguiProfilerTick_delegate))]
	internal static void Module_OnImguiProfilerTick(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).OnImguiProfilerTick();
	}

	[MonoPInvokeCallback(typeof(Module_OnSceneEditorModeOver_delegate))]
	internal static void Module_OnSceneEditorModeOver(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).OnSceneEditorModeOver();
	}

	[MonoPInvokeCallback(typeof(Module_OnSkinsXMLHasChanged_delegate))]
	internal static void Module_OnSkinsXMLHasChanged(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).OnSkinsXMLHasChanged();
	}

	[MonoPInvokeCallback(typeof(Module_RunTest_delegate))]
	internal static void Module_RunTest(int thisPointer, IntPtr commandLine)
	{
		Module obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Module;
		string commandLine2 = Marshal.PtrToStringAnsi(commandLine);
		obj.RunTest(commandLine2);
	}

	[MonoPInvokeCallback(typeof(Module_SetEditorScreenAsRootScreen_delegate))]
	internal static bool Module_SetEditorScreenAsRootScreen(int thisPointer)
	{
		return (DotNetObject.GetManagedObjectWithId(thisPointer) as Module).SetEditorScreenAsRootScreen();
	}

	[MonoPInvokeCallback(typeof(Module_SetLoadingFinished_delegate))]
	internal static void Module_SetLoadingFinished(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).SetLoadingFinished();
	}

	[MonoPInvokeCallback(typeof(Module_StartMissionForEditor_delegate))]
	internal static void Module_StartMissionForEditor(int thisPointer, IntPtr missionName, IntPtr sceneName, IntPtr levels)
	{
		Module obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Module;
		string missionName2 = Marshal.PtrToStringAnsi(missionName);
		string sceneName2 = Marshal.PtrToStringAnsi(sceneName);
		string levels2 = Marshal.PtrToStringAnsi(levels);
		obj.StartMissionForEditor(missionName2, sceneName2, levels2);
	}

	[MonoPInvokeCallback(typeof(Module_StartMissionForReplayEditor_delegate))]
	internal static void Module_StartMissionForReplayEditor(int thisPointer, IntPtr missionName, IntPtr sceneName, IntPtr levels, IntPtr fileName, bool record, float startTime, float endTime)
	{
		Module obj = DotNetObject.GetManagedObjectWithId(thisPointer) as Module;
		string missionName2 = Marshal.PtrToStringAnsi(missionName);
		string sceneName2 = Marshal.PtrToStringAnsi(sceneName);
		string levels2 = Marshal.PtrToStringAnsi(levels);
		string fileName2 = Marshal.PtrToStringAnsi(fileName);
		obj.StartMissionForReplayEditor(missionName2, sceneName2, levels2, fileName2, record, startTime, endTime);
	}

	[MonoPInvokeCallback(typeof(Module_TickTest_delegate))]
	internal static void Module_TickTest(int thisPointer, float dt)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as Module).TickTest(dt);
	}

	[MonoPInvokeCallback(typeof(WeaponComponentMissionExtensions_CalculateCenterOfMass_delegate))]
	internal static Vec3 WeaponComponentMissionExtensions_CalculateCenterOfMass(NativeObjectPointer body)
	{
		PhysicsShape body2 = null;
		if (body.Pointer != UIntPtr.Zero)
		{
			body2 = new PhysicsShape(body.Pointer);
		}
		return WeaponComponentMissionExtensions.CalculateCenterOfMass(body2);
	}
}
