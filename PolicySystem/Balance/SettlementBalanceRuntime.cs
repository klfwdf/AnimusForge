using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

internal static class SettlementBalanceRuntime
{
	private const string HarmonyId = "com.AnimusForge.settlementbalance";
	private static bool _dailyInstalled;
	private static bool _foodInstalled;
	private static bool _reportedFoodFailure;
	private static bool _reportedDailyFailure;
	private static readonly HashSet<MethodBase> ModelMethods = new HashSet<MethodBase>();
	private static readonly TextObject CapExplanation = new TextObject("玩家设置的总量瓶颈");
	[ThreadStatic] private static DailyContext _day;
	[ThreadStatic] private static bool _readingProsperityFood;

	internal struct DailyContext
	{
		internal Town Town;
		internal Village Village;
		internal Settlement Settlement;
		internal float StartingFood;
		internal SettlementBalanceSnapshot Settings;
	}

	internal static void Install()
	{
		if (Campaign.Current?.Models == null) return;
		SettlementBalanceSettings.Initialize();
		var harmony = new Harmony(HarmonyId);
		if (!_dailyInstalled)
		{
			var targets = new[]
			{
				AccessTools.Method(typeof(Town), "DailyTick"),
				AccessTools.Method(typeof(Village), "DailyTick"),
				AccessTools.PropertySetter(typeof(Town), nameof(Town.Prosperity)),
				AccessTools.PropertySetter(typeof(Village), nameof(Village.Hearth)),
				AccessTools.PropertySetter(typeof(Settlement), nameof(Settlement.Militia)),
				AccessTools.PropertySetter(typeof(Fief), nameof(Fief.FoodStocks)),
				AccessTools.Method(typeof(Town), nameof(Town.FoodStocksUpperLimit))
			};
			try
			{
				if (targets.Any(target => target == null)) throw new MissingMethodException("Settlement balance daily adapter target missing.");
				PreserveExistingDailyFood(PatchProcessor.GetOriginalInstructions(targets[0]));
				harmony.Patch(targets[0], prefix: Method(nameof(TownDailyPrefix)), transpiler: Method(nameof(PreserveExistingDailyFood)), finalizer: Method(nameof(DailyFinalizer)));
				Patch(harmony, targets[1], nameof(VillageDailyPrefix), null, nameof(DailyFinalizer));
				Patch(harmony, targets[2], nameof(ProsperityWritePrefix));
				Patch(harmony, targets[3], nameof(HearthWritePrefix));
				Patch(harmony, targets[4], nameof(MilitiaWritePrefix));
				Patch(harmony, targets[5], nameof(FoodWritePrefix));
				Patch(harmony, targets[6], null, nameof(FoodCapacityPostfix));
				_dailyInstalled = true;
			}
			catch (Exception ex)
			{
				foreach (var target in targets.Where(target => target != null))
				{
					try { harmony.Unpatch(target, HarmonyPatchType.All, HarmonyId); }
					catch (Exception rollback) { PolicySystemLog.Failure("Balance", "daily-adapter-rollback-failed", target.Name, rollback.ToString()); }
				}
				PolicySystemLog.Failure("Balance", "daily-adapter-failed", ex.Message, ex.ToString());
				if (!_reportedDailyFailure)
				{
					_reportedDailyFailure = true;
					try { InformationManager.DisplayMessage(new InformationMessage("总量瓶颈未启用：日结补丁安装失败，详见政策日志。")); } catch { }
				}
			}
		}
		var models = Campaign.Current.Models;
		InstallModel(harmony, models.SettlementProsperityModel, "CalculateProsperityChange", new[] { typeof(Town), typeof(bool) }, nameof(ProsperityModelPostfix), true);
		InstallModel(harmony, models.SettlementProsperityModel, "CalculateHearthChange", new[] { typeof(Village), typeof(bool) }, nameof(HearthModelPostfix));
		InstallModel(harmony, models.SettlementMilitiaModel, "CalculateMilitiaChange", new[] { typeof(Settlement), typeof(bool) }, nameof(MilitiaModelPostfix));
		InstallModel(harmony, models.SettlementFoodModel, "CalculateTownFoodStocksChange", new[] { typeof(Town), typeof(bool), typeof(bool) }, nameof(FoodModelPostfix));
		if (!_foodInstalled)
		{
			try
			{
				// Only this private vanilla calculation reads zero prosperity. Real game state is never changed.
				MethodInfo food = AccessTools.Method(typeof(DefaultSettlementFoodModel), "CalculateTownFoodChangeInternal", new[] { typeof(Town), typeof(bool), typeof(bool) });
				if (food == null) throw new MissingMethodException("CalculateTownFoodChangeInternal");
				RemoveProsperityFood(PatchProcessor.GetOriginalInstructions(food)); // Validate before installing.
				harmony.Patch(food, transpiler: Method(nameof(RemoveProsperityFood)));
				_foodInstalled = true;
			}
			catch (Exception ex)
			{
				PolicySystemLog.Failure("Balance", "prosperity-food-removal-failed", ex.Message, ex.ToString());
				ReportFoodFailure("无法取消繁荣耗粮：原版补丁结构不匹配；未应用估算补偿。");
			}
		}
		if (_foodInstalled && !_reportedFoodFailure && !(models.SettlementFoodModel is DefaultSettlementFoodModel))
		{
			PolicySystemLog.Failure("Balance", "custom-food-model", "当前第三方粮食模型不继承原版模型。", "Prosperity food removal is only verified for the vanilla calculation; no guessed compensation applied.");
			ReportFoodFailure("第三方粮食模型未使用原版计算，无法确认繁荣耗粮已取消。");
		}
	}
	private static void ReportFoodFailure(string message)
	{
		if (_reportedFoodFailure) return;
		_reportedFoodFailure = true;
		try { InformationManager.DisplayMessage(new InformationMessage(message)); } catch { }
	}

	private static HarmonyMethod Method(string name) => new HarmonyMethod(typeof(SettlementBalanceRuntime), name)
	{
		priority = Priority.Last,
		after = new[] { "com.AnimusForge.custompolicy.settlementmodels" }
	};
	private static void Patch(Harmony harmony, MethodInfo target, string prefix = null, string postfix = null, string finalizer = null)
	{
		if (target == null) throw new MissingMethodException("Settlement balance patch target missing.");
		harmony.Patch(target, prefix == null ? null : Method(prefix), postfix == null ? null : Method(postfix), null, finalizer == null ? null : Method(finalizer));
	}
	private static void InstallModel(Harmony harmony, object model, string name, Type[] args, string postfix, bool prosperity = false)
	{
		try
		{
			var target = AccessTools.Method(model.GetType(), name, args)?.GetDeclaredMember();
			if (target == null) throw new MissingMethodException(model.GetType().FullName, name);
			if (ModelMethods.Contains(target)) return;
			Patch(harmony, target, prosperity ? nameof(ProsperityFoodPrefix) : null, postfix, prosperity ? nameof(ProsperityFoodFinalizer) : null);
			ModelMethods.Add(target);
		}
		catch (Exception ex) { PolicySystemLog.Failure("Balance", "model-adapter-failed", name, ex.ToString()); }
	}

	internal static IEnumerable<CodeInstruction> RemoveProsperityFood(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var getter = AccessTools.PropertyGetter(typeof(Town), nameof(Town.Prosperity));
		var divisor = AccessTools.PropertyGetter(typeof(TaleWorlds.CampaignSystem.ComponentInterfaces.SettlementFoodModel), "NumberOfProsperityToEatOneFood");
		var matches = codes.Select((code, index) => new { code, index }).Where(item => item.code.Calls(getter)).ToArray();
		if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one prosperity food term.");
		int position = matches[0].index;
		if (!codes.Skip(position + 1).Take(5).Any(code => code.Calls(divisor)) || !codes.Skip(position + 1).Take(5).Any(code => code.opcode == OpCodes.Div))
			throw new InvalidOperationException("Prosperity food divisor layout changed.");
		matches[0].code.opcode = OpCodes.Call;
		matches[0].code.operand = AccessTools.Method(typeof(SettlementBalanceRuntime), nameof(ZeroProsperityForFood));
		return codes;
	}
	private static float ZeroProsperityForFood(Town town) => 0f;

	internal static IEnumerable<CodeInstruction> PreserveExistingDailyFood(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var capacity = AccessTools.Method(typeof(Town), nameof(Town.FoodStocksUpperLimit));
		var write = AccessTools.PropertySetter(typeof(Fief), nameof(Fief.FoodStocks));
		var matches = codes.Select((code, index) => new { code, index }).Where(item => item.code.Calls(capacity)).ToArray();
		if (matches.Length != 2 || !codes.Skip(matches[1].index + 1).Take(3).Any(code => code.Calls(write)))
			throw new InvalidOperationException("Native daily food capacity clipping layout changed.");
		foreach (var match in matches)
		{
			match.code.opcode = OpCodes.Call;
			match.code.operand = AccessTools.Method(typeof(SettlementBalanceRuntime), nameof(FoodCapacityForDailyClip));
		}
		return codes;
	}
	private static int FoodCapacityForDailyClip(Town town)
	{
		int capacity = town.FoodStocksUpperLimit();
		var limit = Settings.Get(SettlementBalanceMetric.Food);
		return _dailyInstalled && limit.Enabled && ReferenceEquals(_day.Town, town)
			? SettlementBalanceRules.DailyFoodCapacity(capacity, _day.StartingFood, true) : capacity;
	}

	internal static void TownDailyPrefix(Town __instance, out DailyContext __state)
	{
		__state = _day;
		if (!_dailyInstalled) return;
		_day = new DailyContext { Town = __instance, Settlement = __instance.Settlement, StartingFood = __instance.FoodStocks, Settings = SettlementBalanceSettings.Current };
	}
	internal static void VillageDailyPrefix(Village __instance, out DailyContext __state)
	{
		__state = _day;
		if (!_dailyInstalled) return;
		_day = new DailyContext { Village = __instance, Settlement = __instance.Settlement, Settings = SettlementBalanceSettings.Current };
	}
	internal static Exception DailyFinalizer(DailyContext __state, Exception __exception)
	{
		_day = __state;
		return __exception;
	}
	private static void ProsperityFoodPrefix(out bool __state) { __state = _readingProsperityFood; _readingProsperityFood = true; }
	private static Exception ProsperityFoodFinalizer(bool __state, Exception __exception) { _readingProsperityFood = __state; return __exception; }
	private static SettlementBalanceSnapshot Settings => _day.Settings ?? SettlementBalanceSettings.Current;
	private static SettlementBalanceMetric ProsperityMetric(Town town) => town.IsCastle ? SettlementBalanceMetric.CastleProsperity : SettlementBalanceMetric.CityProsperity;
	private static SettlementBalanceMetric MilitiaMetric(Settlement settlement) => settlement.IsVillage ? SettlementBalanceMetric.VillageMilitia : settlement.IsCastle ? SettlementBalanceMetric.CastleMilitia : SettlementBalanceMetric.CityMilitia;
	private static float Write(float current, float proposed, SettlementBalanceMetric metric)
	{
		if (!_dailyInstalled) return proposed;
		var limit = Settings.Get(metric);
		return SettlementBalanceRules.LimitWrite(current, proposed, limit.Enabled, limit.Value);
	}
	internal static void ProsperityWritePrefix(Town __instance, ref float __0)
	{
		if (ReferenceEquals(_day.Town, __instance)) __0 = Write(__instance.Prosperity, __0, ProsperityMetric(__instance));
	}
	internal static void HearthWritePrefix(Village __instance, ref float __0)
	{
		if (ReferenceEquals(_day.Village, __instance)) __0 = Write(__instance.Hearth, __0, SettlementBalanceMetric.VillageHearth);
	}
	internal static void MilitiaWritePrefix(Settlement __instance, ref float __0)
	{
		if (ReferenceEquals(_day.Settlement, __instance)) __0 = Write(__instance.Militia, __0, MilitiaMetric(__instance));
	}
	internal static void FoodWritePrefix(Fief __instance, ref float __0)
	{
		if (ReferenceEquals(_day.Town, __instance)) __0 = Write(__instance.FoodStocks, __0, SettlementBalanceMetric.Food);
	}
	internal static void FoodCapacityPostfix(Town __instance, ref int __result)
	{
		var limit = Settings.Get(SettlementBalanceMetric.Food);
		if (!limit.Enabled || !_dailyInstalled) return;
		__result = limit.Value;
	}
	private static void LimitModel(float current, SettlementBalanceMetric metric, bool includeDescriptions, ref ExplainedNumber result)
	{
		if (!_dailyInstalled) return;
		var limit = Settings.Get(metric);
		float original = result.ResultNumber;
		float adjusted = SettlementBalanceRules.LimitDelta(current, original, limit.Enabled, limit.Value);
		if (adjusted < original)
			result.LimitMax(Math.Min(result.LimitMaxValue, adjusted), includeDescriptions ? CapExplanation : null);
	}
	private static void ProsperityModelPostfix(Town __0, bool __1, ref ExplainedNumber __result)
	{
		if (__0 != null) LimitModel(__0.Prosperity, ProsperityMetric(__0), __1, ref __result);
	}
	private static void HearthModelPostfix(Village __0, bool __1, ref ExplainedNumber __result)
	{
		if (__0 != null) LimitModel(__0.Hearth, SettlementBalanceMetric.VillageHearth, __1, ref __result);
	}
	private static void MilitiaModelPostfix(Settlement __0, bool __1, ref ExplainedNumber __result)
	{
		if (__0 != null && (__0.IsTown || __0.IsCastle || __0.IsVillage)) LimitModel(__0.Militia, MilitiaMetric(__0), __1, ref __result);
	}
	private static void FoodModelPostfix(Town __0, bool __1, bool __2, ref ExplainedNumber __result)
	{
		// The original prosperity model needs raw food surplus, not capped inventory growth.
		if (__0 != null && !_readingProsperityFood) LimitModel(__0.FoodStocks, SettlementBalanceMetric.Food, __2, ref __result);
	}
}
