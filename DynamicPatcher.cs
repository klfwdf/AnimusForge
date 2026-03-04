using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public class DynamicPatcher
{
	private const string BuildId = "BUILD_2026-01-21_x64_v1";

	private static bool _buildInfoLogged = false;

	private static readonly List<Type> TargetClasses = new List<Type>
	{
		typeof(ConversationManager),
		typeof(GameStateManager),
		typeof(InformationManager),
		typeof(ScreenManager),
		typeof(MBObjectManager),
		typeof(Campaign)
	};

	private static readonly string[] JunkKeywords = new string[19]
	{
		"GetMouseVisibility", "GetSimplifiedTimeControlMode", "IsMouseCursorHidden", "UpdateMouseVisibility", "GetMouseInput", "GetActiveMouseKeys", "str_game_key_text", "xbox", "controller", "ps_show_profile",
		"dlc_installed", "SetCurrentFocusedObject", "LateAITick", "LateAITickAux", "SetSortedLayersDirty", "TryLoseFocus", "TrySetFocus", "OnGameWindowFocusChange", "OnLayerActiveStateChanged"
	};

	private static readonly HashSet<string> SlowLogMethods = new HashSet<string>
	{
		"FocusTick", "FocusStateCheckTick", "ClearFocus", "FocusedItemHealthTick", "RealTick", "TickMapTime", "WaitAsyncTasks", "CheckMainPartyNeedsUpdate", "PartiesThink", "HourlyTick",
		"DailyTickSettlement", "CalculateAverageDistanceBetweenClosestTwoTownsWithNavigationType", "GeneratePartyId", "IsLayerBlockedAtPosition", "ShowScreenDebugInformation", "RefreshGlobalOrder", "FindPredecessor", "CleanRequests", "DetermineTimeSpeed", "Update",
		"Tick", "LateTick", "EarlyUpdate", "LateUpdate", "OnTick", "OnFrameTick"
	};

	public static bool HasHooked = false;

	private static Dictionary<string, float> _methodLogTimers = new Dictionary<string, float>();

	public static void DoMassiveHook(Harmony harmony)
	{
		if (HasHooked)
		{
			return;
		}
		HasHooked = true;
		Logger.LogTrace("System", "\ud83d\ude08 [万能拦截器] 启动！【动作监控】已改为按 F10 触发...");
		Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.MissionViews.MissionMainAgentInteractionComponent");
		if (type != null)
		{
			TargetClasses.Add(type);
		}
		MethodInfo method = typeof(DynamicPatcher).GetMethod("UniversalPrefix", BindingFlags.Static | BindingFlags.Public);
		foreach (Type targetClass in TargetClasses)
		{
			if (targetClass == null)
			{
				continue;
			}
			List<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(targetClass);
			foreach (MethodInfo item in declaredMethods)
			{
				string name = item.Name;
				if (!name.StartsWith("get_") && !name.StartsWith("set_"))
				{
					try
					{
						harmony.Patch(item, new HarmonyMethod(method));
					}
					catch (Exception)
					{
					}
				}
			}
		}
	}

	public static void UniversalPrefix(MethodBase __originalMethod, object[] __args)
	{
		if (!TraceHelper.IsEnabled)
		{
			return;
		}
		try
		{
			if (!_buildInfoLogged)
			{
				_buildInfoLogged = true;
				string text = "";
				string text2 = "";
				try
				{
					text = typeof(DynamicPatcher).Assembly.Location;
					text2 = (File.Exists(text) ? File.GetLastWriteTime(text).ToString("yyyy-MM-dd HH:mm:ss") : "");
				}
				catch
				{
				}
				Logger.LogTrace("System", "[AnimusForge] BUILD_2026-01-21_x64_v1 Assembly=" + text + " LastWrite=" + text2);
			}
			string name = __originalMethod.Name;
			string[] junkKeywords = JunkKeywords;
			foreach (string value in junkKeywords)
			{
				if (name.Contains(value))
				{
					return;
				}
			}
			string text3 = "";
			if (__args != null && __args.Length != 0)
			{
				for (int j = 0; j < __args.Length; j++)
				{
					try
					{
						if (__args[j] != null)
						{
							string text4 = __args[j].ToString();
							string[] junkKeywords2 = JunkKeywords;
							foreach (string value2 in junkKeywords2)
							{
								if (text4.Contains(value2))
								{
									return;
								}
							}
							text3 = text3 + "[" + text4 + "] ";
						}
						else
						{
							text3 += "[null] ";
						}
					}
					catch
					{
						text3 += "[UNSAFE_OBJ] ";
					}
				}
			}
			string text5 = __originalMethod.DeclaringType?.Name ?? "Unknown";
			string key = text5 + "." + name;
			float applicationTime = Time.ApplicationTime;
			float num = 1f;
			if (SlowLogMethods.Contains(name))
			{
				num = 60f;
			}
			if (_methodLogTimers.TryGetValue(key, out var value3) && applicationTime - value3 < num)
			{
				return;
			}
			_methodLogTimers[key] = applicationTime;
			if (!(name == ".ctor"))
			{
				string text6 = "\ud83d\udd75 [逻辑截获] (BUILD_2026-01-21_x64_v1) " + text5 + "." + name + "()";
				if (!string.IsNullOrEmpty(text3))
				{
					text6 = text6 + " | 参数: " + text3;
				}
				Logger.LogTrace("DeepCode", text6);
			}
		}
		catch (Exception)
		{
		}
	}
}
