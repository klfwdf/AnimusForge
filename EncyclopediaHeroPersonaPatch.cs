using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

namespace AnimusForge;

public static class EncyclopediaHeroPersonaPatch
{
	private static readonly object SyncRoot = new object();
	private static readonly HashSet<string> GenerationRequests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Queue<PendingRefresh> PendingRefreshes = new Queue<PendingRefresh>();
	private static bool _patched;

	private sealed class PendingRefresh
	{
		public WeakReference ViewModel;
		public string HeroId;
	}

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		lock (SyncRoot)
		{
			if (_patched)
			{
				return;
			}
			MethodInfo refreshValues = AccessTools.Method(typeof(EncyclopediaHeroPageVM), nameof(EncyclopediaHeroPageVM.RefreshValues));
			if (refreshValues == null)
			{
				Logger.Log("EncyclopediaPersona", "[WARN] 未找到 EncyclopediaHeroPageVM.RefreshValues，跳过 Hero 百科个性背景覆盖。");
				return;
			}
			Harmony activeHarmony = harmony ?? new Harmony("AnimusForge.encyclopedia.hero.persona");
			activeHarmony.Patch(refreshValues, postfix: new HarmonyMethod(typeof(EncyclopediaHeroPersonaPatch), nameof(RefreshValuesPostfix)));
			_patched = true;
			Logger.Log("EncyclopediaPersona", "[INFO] 已启用 Hero 百科个性背景覆盖。");
		}
	}

	public static void OnApplicationTick()
	{
		List<PendingRefresh> list = null;
		lock (SyncRoot)
		{
			if (PendingRefreshes.Count <= 0)
			{
				return;
			}
			list = new List<PendingRefresh>(PendingRefreshes);
			PendingRefreshes.Clear();
		}
		foreach (PendingRefresh pendingRefresh in list)
		{
			try
			{
				EncyclopediaHeroPageVM vm = pendingRefresh?.ViewModel?.Target as EncyclopediaHeroPageVM;
				if (vm == null)
				{
					continue;
				}
				Hero hero = ResolveHero(vm);
				if (!string.Equals(hero?.StringId ?? "", pendingRefresh.HeroId ?? "", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				ApplyPersonaText(vm, hero, triggerGeneration: false);
			}
			catch (Exception ex)
			{
				Logger.Log("EncyclopediaPersona", "[WARN] 刷新 Hero 百科人设文本失败: " + ex.Message);
			}
		}
	}

	public static void RefreshValuesPostfix(EncyclopediaHeroPageVM __instance)
	{
		try
		{
			Hero hero = ResolveHero(__instance);
			ApplyPersonaText(__instance, hero, triggerGeneration: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EncyclopediaPersona", "[WARN] 覆盖 Hero 百科人设文本失败: " + ex.Message);
		}
	}

	private static Hero ResolveHero(EncyclopediaHeroPageVM vm)
	{
		if (vm == null)
		{
			return null;
		}
		return AccessTools.Field(typeof(EncyclopediaHeroPageVM), "_hero")?.GetValue(vm) as Hero;
	}

	private static void ApplyPersonaText(EncyclopediaHeroPageVM vm, Hero hero, bool triggerGeneration)
	{
		if (!ShouldOverride(hero, vm))
		{
			return;
		}
		MyBehavior.GetNpcPersonaForExternal(hero, out var personality, out var background);
		string text = BuildPersonaInformationText(personality, background);
		if (!string.IsNullOrWhiteSpace(text))
		{
			vm.InformationText = text;
			if (triggerGeneration && (string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background)))
			{
				RequestGeneration(hero, vm);
			}
			return;
		}
		if (!triggerGeneration)
		{
			return;
		}
		vm.InformationText = MyBehavior.BuildNpcPersonaGenerationHintForExternal(hero);
		RequestGeneration(hero, vm);
	}

	private static bool ShouldOverride(Hero hero, EncyclopediaHeroPageVM vm)
	{
		if (hero == null || vm == null || hero == Hero.MainHero)
		{
			return false;
		}
		if (vm.IsInformationHidden)
		{
			return false;
		}
		return hero.CharacterObject?.IsHero == true;
	}

	private static string BuildPersonaInformationText(string personality, string background)
	{
		string text = (personality ?? "").Trim();
		string text2 = (background ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		List<string> parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(text))
		{
			parts.Add("【个性】\n" + text);
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			parts.Add("【历史背景】\n" + text2);
		}
		return string.Join("\n\n", parts).Trim();
	}

	private static void RequestGeneration(Hero hero, EncyclopediaHeroPageVM vm)
	{
		if (hero == null || vm == null)
		{
			return;
		}
		string heroId = (hero.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return;
		}
		if (MyBehavior.TryGetNpcPersonaGenerationStatusForExternal(hero, out var needsGeneration, out var inFlight) && (!needsGeneration || inFlight))
		{
			return;
		}
		lock (SyncRoot)
		{
			if (!GenerationRequests.Add(heroId))
			{
				return;
			}
		}
		_ = GenerateAndQueueRefreshAsync(hero, heroId, vm);
	}

	private static async Task GenerateAndQueueRefreshAsync(Hero hero, string heroId, EncyclopediaHeroPageVM vm)
	{
		try
		{
			await MyBehavior.EnsureNpcPersonaGeneratedForExternalAsync(hero);
			lock (SyncRoot)
			{
				PendingRefreshes.Enqueue(new PendingRefresh
				{
					ViewModel = new WeakReference(vm),
					HeroId = heroId
				});
			}
		}
		catch (Exception ex)
		{
			Logger.Log("EncyclopediaPersona", "[WARN] Hero 百科触发人设生成失败: " + ex.Message);
		}
		finally
		{
			lock (SyncRoot)
			{
				GenerationRequests.Remove(heroId);
			}
		}
	}
}
