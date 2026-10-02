using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Modules;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge
{
	public partial class DiplomacyBehavior : CampaignBehaviorBase
	{
		public static DiplomacyBehavior Instance { get; private set; }

		private static bool s_globalPatchesApplied;
		[ModuleInitializer]
		internal static void ModuleInit()
		{
			ApplyGlobalPatchesOnce();
		}

		private static void ApplyGlobalPatchesOnce()
		{
			if (s_globalPatchesApplied) return;
			s_globalPatchesApplied = true;
			try
			{
				Harmony harmony = new Harmony("com.AnimusForge.diplomacy");
				harmony.Patch(
					typeof(MyBehavior).GetMethod("BuildShoutPromptContextForExternal",
						BindingFlags.Public | BindingFlags.Static),
					postfix: new HarmonyMethod(typeof(DiplomacyBehavior), nameof(Patch_BuildDiplomacyContext_Postfix)));
				Logger.Log("DiplomacyBehavior", "[Harmony] Diplomacy context injection applied.");
			}
			catch (Exception ex)
			{
				Logger.Log("DiplomacyBehavior", $"[Harmony Error] {ex.Message}");
			}
		}

		public override void RegisterEvents()
		{
			ApplyGlobalPatchesOnce();
			Instance = this;
			Logger.Log("DiplomacyBehavior", "[Lifecycle] Registered.");
		}

		public override void SyncData(IDataStore dataStore)
		{
		}

		// ════════════════════════════════════════════════════════ LLM context

		internal static string BuildDiplomacyPostprocessContext(Hero npc)
		{
			var source = new DiplomacyPostprocessContextSource(npc);
			return DiplomacyPostprocessContextApplication.Build(ref source);
		}

		// ════════════════════════════════════════════════════════ Harmony

		private static void Patch_BuildDiplomacyContext_Postfix(
			Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero,
			CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex,
			bool suppressDynamicRuleAndLore, bool usePrefetchedLoreContext, string prefetchedLoreContext,
			ref MyBehavior.ShoutPromptContext __result)
		{
			try
			{
				if (__result == null) return;
				Hero ctx = targetHero ?? targetCharacter?.HeroObject;
				if (ctx == null) return;
                string additions = DiplomacyConversationBridge.BuildDiplomacyPrompt(ctx, __result.Extras);
                if (!string.IsNullOrEmpty(additions)) __result.Extras = (__result.Extras ?? "") + additions;
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[PatchContext Error] {ex.Message}"); }
		}

		// ════════════════════════════════════════════════════════ DECLARE_WAR

	}
}
