using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
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

	private static readonly ConcurrentDictionary<MethodBase, int> _methodLogTimers = new ConcurrentDictionary<MethodBase, int>();

	public static void DoMassiveHook(Harmony harmony)
	{
		if (HasHooked)
		{
			return;
		}
		HasHooked = true;
		Logger.LogTrace("System", "\ud83d\ude08 [Dynamic interceptor] Started; action monitoring is now triggered by F10.");
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

    internal static bool TryAdmitDiagnosticLog(MethodBase method, int now)
    {
        if (method == null) return false;
        uint interval = SlowLogMethods.Contains(method.Name) ? 60000u : 1000u;
        if (_methodLogTimers.TryGetValue(method, out int last))
            return unchecked((uint)(now - last)) >= interval && _methodLogTimers.TryUpdate(method, now, last);
        return _methodLogTimers.TryAdd(method, now);
    }

    internal static string FormatDiagnosticArgument(object value)
    {
        if (value == null) return "null";
        if (value is string text) return text.Length <= 512 ? text : text.Substring(0, 512) + "...";
        Type type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is decimal || value is Guid || value is DateTime || value is DateTimeOffset || value is TimeSpan)
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        // Calling ToString on PartyBase (or any custom game object) can touch
        // invalid native/game state. Type metadata never dereferences that state.
        return "<" + (type.FullName ?? type.Name) + ">";
    }

    public static void UniversalPrefix(MethodBase __originalMethod, object[] __args)
    {
        if (!TraceHelper.IsEnabled || __originalMethod == null) return;
        try
        {
            string name = __originalMethod.Name;
            if (name == ".ctor") return;
            foreach (string keyword in JunkKeywords) if (name.Contains(keyword)) return;
            // Suppressed calls do not format arguments, allocate method keys or call
            // the engine clock. One atomic winner per method and interval across threads.
            if (!TryAdmitDiagnosticLog(__originalMethod, Environment.TickCount)) return;
            if (!_buildInfoLogged)
            {
                _buildInfoLogged = true;
                string path = typeof(DynamicPatcher).Assembly.Location;
                string modified = File.Exists(path) ? File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss") : "";
                Logger.LogTrace("System", "[AnimusForge] " + BuildId + " Assembly=" + path + " LastWrite=" + modified);
            }
            var arguments = new StringBuilder();
            if (__args != null)
            {
                int count = Math.Min(12, __args.Length);
                for (int i = 0; i < count; i++)
                {
                    string text = FormatDiagnosticArgument(__args[i]);
                    foreach (string keyword in JunkKeywords) if (text.Contains(keyword)) return;
                    arguments.Append('[').Append(text).Append("] ");
                }
                if (count < __args.Length) arguments.Append("[remaining=").Append(__args.Length - count).Append("] ");
            }
            string owner = __originalMethod.DeclaringType?.Name ?? "Unknown";
            Logger.LogTrace("DeepCode", "[Logic interception] (" + BuildId + ") " + owner + "." + name + "()" + (arguments.Length == 0 ? "" : " | Arguments: " + arguments));
        }
        catch (Exception) { }
    }
}
