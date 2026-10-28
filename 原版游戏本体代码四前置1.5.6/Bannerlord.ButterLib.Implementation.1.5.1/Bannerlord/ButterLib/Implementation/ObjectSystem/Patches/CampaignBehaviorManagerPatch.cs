using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.ObjectSystem;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.CampaignSystem;

namespace Bannerlord.ButterLib.Implementation.ObjectSystem.Patches;

internal sealed class CampaignBehaviorManagerPatch
{
	private delegate void SaveBehaviorDataDelegate(CampaignBehaviorBase campaignBehavior);

	private delegate void LoadBehaviorDataDelegate(CampaignBehaviorBase campaignBehavior);

	private static readonly MethodInfo? OnGameLoadedTargetMI = AccessTools2.Method("TaleWorlds.CampaignSystem.CampaignBehaviors.CampaignBehaviorManager:OnGameLoaded") ?? AccessTools2.Method("TaleWorlds.CampaignSystem.CampaignBehaviors.CampaignBehaviorManager:LoadBehaviorData");

	private static readonly MethodInfo? OnBeforeSaveTargetMI = AccessTools2.Method("TaleWorlds.CampaignSystem.CampaignBehaviors.CampaignBehaviorManager:OnBeforeSave");

	private static readonly MethodInfo? OnGameLoadedPatchMI = SymbolExtensions2.GetMethodInfo((object x) => OnGameLoadedPrefix(x));

	private static readonly MethodInfo? OnBeforeSavePatchMI = SymbolExtensions2.GetMethodInfo((object x) => OnBeforeSavePostfix(x));

	internal static void Enable(Harmony harmony)
	{
		ILogger<CampaignBehaviorManagerPatch> logger = (ButterLibSubModule.Instance?.GetServiceProvider() ?? ButterLibSubModule.Instance?.GetTempServiceProvider())?.GetService<ILogger<CampaignBehaviorManagerPatch>>() ?? NullLogger<CampaignBehaviorManagerPatch>.Instance;
		if ((object)OnGameLoadedTargetMI == null)
		{
			logger.LogError("{Method} is null", "OnGameLoadedTargetMI");
		}
		if ((object)OnBeforeSaveTargetMI == null)
		{
			logger.LogError("{Method} is null", "OnBeforeSaveTargetMI");
		}
		if ((object)OnGameLoadedPatchMI == null)
		{
			logger.LogError("{Method} is null", "OnGameLoadedPatchMI");
		}
		if ((object)OnBeforeSavePatchMI == null)
		{
			logger.LogError("{Method} is null", "OnBeforeSavePatchMI");
		}
		if ((object)OnGameLoadedTargetMI != null && (object)OnBeforeSaveTargetMI != null && (object)OnGameLoadedPatchMI != null && (object)OnBeforeSavePatchMI != null)
		{
			harmony.Patch(OnGameLoadedTargetMI, new HarmonyMethod(OnGameLoadedPatchMI));
			harmony.Patch(OnBeforeSaveTargetMI, null, new HarmonyMethod(OnBeforeSavePatchMI));
		}
	}

	internal static void Disable(Harmony harmony)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnGameLoadedPrefix(object? ____campaignBehaviorDataStore)
	{
		IServiceProvider serviceProvider = ButterLibSubModule.Instance?.GetServiceProvider();
		ILogger<CampaignBehaviorManagerPatch> logger = serviceProvider?.GetService<ILogger<CampaignBehaviorManagerPatch>>() ?? NullLogger<CampaignBehaviorManagerPatch>.Instance;
		if (____campaignBehaviorDataStore == null)
		{
			logger.LogError("{Method}: {Variable} is null", "OnGameLoadedPrefix", "____campaignBehaviorDataStore");
			return;
		}
		IMBObjectExtensionDataStore iMBObjectExtensionDataStore = serviceProvider?.GetService<IMBObjectExtensionDataStore>();
		if (iMBObjectExtensionDataStore == null)
		{
			logger.LogError("{Method}: {Variable} is null", "OnGameLoadedPrefix", "mbObjectVariableStorage");
			return;
		}
		if (!(iMBObjectExtensionDataStore is CampaignBehaviorBase campaignBehavior))
		{
			logger.LogError("{Method}: {Variable} is not a CampaignBehaviorBase", "OnGameLoadedPrefix", "mbObjectVariableStorage");
			return;
		}
		LoadBehaviorDataDelegate loadBehaviorDataDelegate = AccessTools2.GetDelegate<LoadBehaviorDataDelegate>(____campaignBehaviorDataStore, ____campaignBehaviorDataStore.GetType(), "LoadBehaviorData");
		if (loadBehaviorDataDelegate == null)
		{
			logger.LogError("{Method}: {Variable} is not a LoadBehaviorDataDelegate", "OnGameLoadedPrefix", "loadBehaviorData");
			return;
		}
		try
		{
			loadBehaviorDataDelegate(campaignBehavior);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "{Method}", "OnGameLoadedPrefix");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnBeforeSavePostfix(object? ____campaignBehaviorDataStore)
	{
		IServiceProvider serviceProvider = ButterLibSubModule.Instance?.GetServiceProvider();
		ILogger<CampaignBehaviorManagerPatch> logger = serviceProvider?.GetService<ILogger<CampaignBehaviorManagerPatch>>() ?? NullLogger<CampaignBehaviorManagerPatch>.Instance;
		if (____campaignBehaviorDataStore == null)
		{
			logger.LogError("{Method}: {Variable} is null", "OnBeforeSavePostfix", "____campaignBehaviorDataStore");
			return;
		}
		IMBObjectExtensionDataStore iMBObjectExtensionDataStore = serviceProvider?.GetService<IMBObjectExtensionDataStore>();
		if (iMBObjectExtensionDataStore == null)
		{
			logger.LogError("{Method}: {Variable} is null", "OnBeforeSavePostfix", "mbObjectVariableStorage");
			return;
		}
		if (!(iMBObjectExtensionDataStore is CampaignBehaviorBase campaignBehavior))
		{
			logger.LogError("{Method}: {Variable} is not a CampaignBehaviorBase", "OnBeforeSavePostfix", "mbObjectVariableStorage");
			return;
		}
		SaveBehaviorDataDelegate saveBehaviorDataDelegate = AccessTools2.GetDelegate<SaveBehaviorDataDelegate>(____campaignBehaviorDataStore, ____campaignBehaviorDataStore.GetType(), "SaveBehaviorData");
		if (saveBehaviorDataDelegate == null)
		{
			logger.LogError("{Method}: {Variable} is not a SaveBehaviorDataDelegate", "OnBeforeSavePostfix", "saveBehaviorData");
			return;
		}
		try
		{
			saveBehaviorDataDelegate(campaignBehavior);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "{Method}", "OnBeforeSavePostfix");
		}
	}
}
