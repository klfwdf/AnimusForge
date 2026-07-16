using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

internal static class CampaignTickDiagnosticsPatch
{
	private const string LogSource = "CampaignTickDiag";
	private const string CheckpointFileName = "CampaignTick_LastCheckpoint.txt";
	private const int MaxCheckpointLines = 12;
	private const int CheckpointWriteIntervalMs = 1000;
	private const int PriorCrashSuspectSkipCount = 18;

	private static readonly object StateLock = new object();
	private static readonly Dictionary<string, long> EntryCounts = new Dictionary<string, long>(StringComparer.Ordinal);
	private static readonly Dictionary<string, int> PriorCrashSuspectPartySkips = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private static readonly List<string> LastCheckpointLines = new List<string>();
	private static readonly Regex PartialHourlyAiPartyRegex = new Regex("args=\\[MobileParty\\{id=([^,\\]}]+)", RegexOptions.Compiled);

	private static bool _patched;
	private static long _sequence;
	private static long _nextCheckpointWriteUtcTicks;
	private static int _checkpointWriteDue;
	private static string _lastContext = "";

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		_patched = true;
		int patched = 0;
		try
		{
			LoadPriorCrashSuspectFromCheckpoint();
			Type dispatcherType = typeof(CampaignEventDispatcher);
			PatchMethod(harmony, dispatcherType, "Tick", new[] { typeof(float) }, ref patched);
			PatchMethod(harmony, dispatcherType, "TickPartialHourlyAi", new[] { typeof(MobileParty) }, ref patched);
			PatchMethod(harmony, dispatcherType, "QuarterDailyPartyTick", new[] { typeof(MobileParty) }, ref patched);
			PatchMethod(harmony, dispatcherType, "AiHourlyTick", new[] { typeof(MobileParty), typeof(PartyThinkParams) }, ref patched);
			PatchMethod(harmony, dispatcherType, "HourlyTick", Type.EmptyTypes, ref patched);
			PatchMethod(harmony, dispatcherType, "QuarterHourlyTick", Type.EmptyTypes, ref patched);
			PatchMethod(harmony, dispatcherType, "HourlyTickParty", new[] { typeof(MobileParty) }, ref patched);
			PatchMethod(harmony, dispatcherType, "HourlyTickSettlement", new[] { typeof(Settlement) }, ref patched);
			PatchMethod(harmony, dispatcherType, "HourlyTickClan", new[] { typeof(Clan) }, ref patched);
			PatchMethod(harmony, dispatcherType, "DailyTick", Type.EmptyTypes, ref patched);
			PatchMethod(harmony, dispatcherType, "DailyTickParty", new[] { typeof(MobileParty) }, ref patched);
			PatchMethod(harmony, dispatcherType, "DailyTickTown", new[] { typeof(Town) }, ref patched);
			PatchMethod(harmony, dispatcherType, "DailyTickSettlement", new[] { typeof(Settlement) }, ref patched);
			PatchMethod(harmony, dispatcherType, "DailyTickHero", new[] { typeof(Hero) }, ref patched);
			PatchMethod(harmony, dispatcherType, "DailyTickClan", new[] { typeof(Clan) }, ref patched);
			PatchMethod(harmony, dispatcherType, "WeeklyTick", Type.EmptyTypes, ref patched);

			Type periodicType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignPeriodicEventManager");
			if (periodicType != null)
			{
				PatchMethod(harmony, periodicType, "TickPeriodicEvents", Type.EmptyTypes, ref patched);
				PatchMethod(harmony, periodicType, "MobilePartyHourlyTick", Type.EmptyTypes, ref patched);
				PatchMethod(harmony, periodicType, "TickPartialHourlyAi", Type.EmptyTypes, ref patched);
				PatchMethod(harmony, periodicType, "PeriodicHourlyTick", Type.EmptyTypes, ref patched);
				PatchMethod(harmony, periodicType, "PeriodicDailyTick", Type.EmptyTypes, ref patched);
				PatchMethod(harmony, periodicType, "PeriodicQuarterDailyTick", Type.EmptyTypes, ref patched);
			}
			else
			{
				Logger.Log(LogSource, "CampaignPeriodicEventManager not found; periodic phase diagnostics skipped.");
			}
			Logger.Log(LogSource, "Campaign tick diagnostics patch applied. methods=" + patched + ", checkpoint=" + CheckpointFileName + ", writeIntervalMs=" + CheckpointWriteIntervalMs);
		}
		catch (Exception ex)
		{
			Logger.LogImmediate(LogSource, "Failed to apply campaign tick diagnostics patch: " + ex);
		}
	}

	public static void Enter(MethodBase __originalMethod)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod);
	}

	public static void Enter(MethodBase __originalMethod, float __0)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod, __0);
	}

	public static void Enter(MethodBase __originalMethod, MobileParty __0)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod, __0);
	}

	public static void Enter(MethodBase __originalMethod, MobileParty __0, PartyThinkParams __1)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod, __0, __1);
	}

	public static void Enter(MethodBase __originalMethod, Settlement __0)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod, __0);
	}

	public static void Enter(MethodBase __originalMethod, Clan __0)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod, __0);
	}

	public static void Enter(MethodBase __originalMethod, Hero __0)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod, __0);
	}

	public static void Enter(MethodBase __originalMethod, Town __0)
	{
		CaptureRegularCheckpoint("ENTER", __originalMethod, __0);
	}

	public static void Exit(MethodBase __originalMethod)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod);
	}

	public static void Exit(MethodBase __originalMethod, float __0)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod, __0);
	}

	public static void Exit(MethodBase __originalMethod, MobileParty __0)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod, __0);
	}

	public static void Exit(MethodBase __originalMethod, MobileParty __0, PartyThinkParams __1)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod, __0, __1);
	}

	public static void Exit(MethodBase __originalMethod, Settlement __0)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod, __0);
	}

	public static void Exit(MethodBase __originalMethod, Clan __0)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod, __0);
	}

	public static void Exit(MethodBase __originalMethod, Hero __0)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod, __0);
	}

	public static void Exit(MethodBase __originalMethod, Town __0)
	{
		CaptureRegularCheckpoint("EXIT", __originalMethod, __0);
	}

	public static void RefreshCheckpointWriteBudget()
	{
		try
		{
			long nowTicks = DateTime.UtcNow.Ticks;
			long nextTicks = Interlocked.Read(ref _nextCheckpointWriteUtcTicks);
			if (nowTicks < nextTicks)
			{
				return;
			}
			long nextAllowedTicks = nowTicks + TimeSpan.FromMilliseconds(CheckpointWriteIntervalMs).Ticks;
			if (Interlocked.CompareExchange(ref _nextCheckpointWriteUtcTicks, nextAllowedTicks, nextTicks) == nextTicks)
			{
				Volatile.Write(ref _checkpointWriteDue, 1);
			}
		}
		catch
		{
		}
	}

	private static bool TryReserveCheckpointWrite()
	{
		try
		{
			if (Volatile.Read(ref _checkpointWriteDue) == 0)
			{
				return false;
			}
			return Interlocked.CompareExchange(ref _checkpointWriteDue, 0, 1) == 1;
		}
		catch
		{
			return false;
		}
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod);
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, float __0)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod, __0);
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, MobileParty __0)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod, __0);
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, MobileParty __0, PartyThinkParams __1)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod, __0, __1);
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, Settlement __0)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod, __0);
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, Clan __0)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod, __0);
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, Hero __0)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod, __0);
	}

	public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, Town __0)
	{
		return CaptureExceptionCheckpoint(__exception, __originalMethod, __0);
	}

	private static void CaptureRegularCheckpoint(string phase, MethodBase method)
	{
		try
		{
			if (!TryReserveCheckpointWrite())
			{
				return;
			}
			StoreCheckpoint(BuildContext(phase, method, null), forceWrite: false, checkpointReserved: true);
		}
		catch
		{
			Volatile.Write(ref _checkpointWriteDue, 1);
		}
	}

	private static void CaptureRegularCheckpoint<TArgument>(string phase, MethodBase method, TArgument argument)
	{
		try
		{
			if (!TryReserveCheckpointWrite())
			{
				return;
			}
			StoreCheckpoint(BuildContext(phase, method, new object[] { argument }), forceWrite: false, checkpointReserved: true);
		}
		catch
		{
			Volatile.Write(ref _checkpointWriteDue, 1);
		}
	}

	private static void CaptureRegularCheckpoint<TFirst, TSecond>(string phase, MethodBase method, TFirst first, TSecond second)
	{
		try
		{
			if (!TryReserveCheckpointWrite())
			{
				return;
			}
			StoreCheckpoint(BuildContext(phase, method, new object[] { first, second }), forceWrite: false, checkpointReserved: true);
		}
		catch
		{
			Volatile.Write(ref _checkpointWriteDue, 1);
		}
	}

	private static Exception CaptureExceptionCheckpoint(Exception exception, MethodBase method)
	{
		if (exception == null)
		{
			return null;
		}
		return StoreExceptionCheckpoint(exception, method, null);
	}

	private static Exception CaptureExceptionCheckpoint<TArgument>(Exception exception, MethodBase method, TArgument argument)
	{
		if (exception == null)
		{
			return null;
		}
		return StoreExceptionCheckpoint(exception, method, new object[] { argument });
	}

	private static Exception CaptureExceptionCheckpoint<TFirst, TSecond>(Exception exception, MethodBase method, TFirst first, TSecond second)
	{
		if (exception == null)
		{
			return null;
		}
		return StoreExceptionCheckpoint(exception, method, new object[] { first, second });
	}

	private static Exception StoreExceptionCheckpoint(Exception exception, MethodBase method, object[] args)
	{
		try
		{
			string context = BuildContext("EXCEPTION", method, args);
			StoreCheckpoint(context, forceWrite: true, checkpointReserved: false);
			Logger.LogImmediate(LogSource, "[tick-exception] " + context + "\n" + exception);
		}
		catch
		{
		}
		return exception;
	}

	public static string GetLastContextSummary()
	{
		try
		{
			lock (StateLock)
			{
				return _lastContext ?? "";
			}
		}
		catch
		{
			return "";
		}
	}

	public static bool ConsumePriorCrashSuspectPartySkip(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			string partyId = party?.StringId ?? "";
			if (string.IsNullOrWhiteSpace(partyId))
			{
				return false;
			}
			lock (StateLock)
			{
				if (!PriorCrashSuspectPartySkips.TryGetValue(partyId, out int remaining) || remaining <= 0)
				{
					PriorCrashSuspectPartySkips.Remove(partyId);
					return false;
				}
				remaining--;
				if (remaining <= 0)
				{
					PriorCrashSuspectPartySkips.Remove(partyId);
				}
				else
				{
					PriorCrashSuspectPartySkips[partyId] = remaining;
				}
				reason = "prior_crash_partial_hourly_ai remaining=" + remaining;
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	private static void PatchMethod(Harmony harmony, Type type, string methodName, Type[] parameterTypes, ref int patched)
	{
		MethodInfo target = AccessTools.Method(type, methodName, parameterTypes);
		if (target == null)
		{
			Logger.Log(LogSource, type.FullName + "." + methodName + " not found; diagnostics skipped.");
			return;
		}
		HarmonyMethod prefix = CreateHookMethod(nameof(Enter), parameterTypes, isFinalizer: false);
		HarmonyMethod postfix = CreateHookMethod(nameof(Exit), parameterTypes, isFinalizer: false);
		HarmonyMethod finalizer = CreateHookMethod(nameof(Finalizer), parameterTypes, isFinalizer: true);
		if (prefix == null || postfix == null || finalizer == null)
		{
			Logger.Log(LogSource, type.FullName + "." + methodName + " diagnostics hook signature unavailable; skipped.");
			return;
		}
		harmony.Patch(target, prefix: prefix, postfix: postfix, finalizer: finalizer);
		patched++;
	}

	private static HarmonyMethod CreateHookMethod(string hookName, Type[] targetParameterTypes, bool isFinalizer)
	{
		int prefixLength = isFinalizer ? 2 : 1;
		int targetLength = targetParameterTypes?.Length ?? 0;
		Type[] hookParameterTypes = new Type[prefixLength + targetLength];
		int index = 0;
		if (isFinalizer)
		{
			hookParameterTypes[index++] = typeof(Exception);
		}
		hookParameterTypes[index++] = typeof(MethodBase);
		for (int i = 0; i < targetLength; i++)
		{
			hookParameterTypes[index + i] = targetParameterTypes[i];
		}
		MethodInfo method = AccessTools.Method(typeof(CampaignTickDiagnosticsPatch), hookName, hookParameterTypes);
		return method == null ? null : new HarmonyMethod(method);
	}

	private static void LoadPriorCrashSuspectFromCheckpoint()
	{
		try
		{
			string path = AnimusForgeModulePaths.GetLogFilePath(CheckpointFileName);
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				return;
			}
			string[] lines = File.ReadAllLines(path, Encoding.UTF8);
			string lastLine = "";
			for (int i = 0; i < lines.Length; i++)
			{
				if ((lines[i] ?? "").StartsWith("last=", StringComparison.OrdinalIgnoreCase))
				{
					lastLine = lines[i] ?? "";
					break;
				}
			}
			if (string.IsNullOrWhiteSpace(lastLine)
				|| !lastLine.Contains("CampaignEventDispatcher.TickPartialHourlyAi")
				|| lastLine.Contains("EXIT "))
			{
				return;
			}
			Match match = PartialHourlyAiPartyRegex.Match(lastLine);
			string partyId = match.Success ? (match.Groups[1].Value ?? "").Trim() : "";
			if (string.IsNullOrWhiteSpace(partyId) || string.Equals(partyId, "player_party", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			lock (StateLock)
			{
				PriorCrashSuspectPartySkips[partyId] = PriorCrashSuspectSkipCount;
			}
			Logger.LogImmediate(LogSource, "prior crash partial hourly AI suspect loaded party=" + partyId + " skipCount=" + PriorCrashSuspectSkipCount + " checkpoint=" + Sanitize(lastLine));
		}
		catch (Exception ex)
		{
			Logger.LogImmediate(LogSource, "prior crash suspect load failed: " + ex);
		}
	}

	private static void StoreCheckpoint(string context, bool forceWrite, bool checkpointReserved)
	{
		string snapshot = "";
		try
		{
			long nowTicks = DateTime.UtcNow.Ticks;
			lock (StateLock)
			{
				_lastContext = context ?? "";
				LastCheckpointLines.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " " + (_lastContext ?? ""));
				while (LastCheckpointLines.Count > MaxCheckpointLines)
				{
					LastCheckpointLines.RemoveAt(0);
				}
				if (!checkpointReserved)
				{
					if (!forceWrite && nowTicks < Interlocked.Read(ref _nextCheckpointWriteUtcTicks))
					{
						return;
					}
					Interlocked.Exchange(ref _nextCheckpointWriteUtcTicks, nowTicks + TimeSpan.FromMilliseconds(CheckpointWriteIntervalMs).Ticks);
					Volatile.Write(ref _checkpointWriteDue, 0);
				}
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("Campaign tick last checkpoint");
				sb.AppendLine("updated=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				sb.AppendLine("last=" + (_lastContext ?? ""));
				sb.AppendLine("writeIntervalMs=" + CheckpointWriteIntervalMs);
				sb.AppendLine();
				for (int i = 0; i < LastCheckpointLines.Count; i++)
				{
					sb.AppendLine(LastCheckpointLines[i]);
				}
				snapshot = sb.ToString();
			}
			Logger.WriteLogSnapshotImmediate(CheckpointFileName, snapshot);
		}
		catch
		{
		}
	}

	private static string BuildContext(string phase, MethodBase method, object[] args)
	{
		long seq = ++_sequence;
		string methodName = (method?.DeclaringType?.FullName ?? "UnknownType") + "." + (method?.Name ?? "UnknownMethod");
		long count = IncrementEntryCount(methodName);
		StringBuilder sb = new StringBuilder();
		sb.Append((phase ?? "").Trim());
		sb.Append(" ");
		sb.Append("seq=").Append(seq);
		sb.Append(" count=").Append(count);
		sb.Append(" method=").Append(methodName);
		sb.Append(" campaignTime=").Append(SafeCampaignTime());
		sb.Append(" mainParty=").Append(DescribeMobileParty(SafeMainParty()));
		if (args != null && args.Length > 0)
		{
			sb.Append(" args=[");
			for (int i = 0; i < args.Length; i++)
			{
				if (i > 0)
				{
					sb.Append("; ");
				}
				sb.Append(DescribeArg(args[i]));
			}
			sb.Append("]");
		}
		return sb.ToString();
	}

	private static long IncrementEntryCount(string methodName)
	{
		try
		{
			lock (StateLock)
			{
				if (!EntryCounts.TryGetValue(methodName, out long count))
				{
					count = 0L;
				}
				count++;
				EntryCounts[methodName] = count;
				return count;
			}
		}
		catch
		{
			return 0L;
		}
	}

	private static MobileParty SafeMainParty()
	{
		try
		{
			return MobileParty.MainParty;
		}
		catch
		{
			return null;
		}
	}

	private static string SafeCampaignTime()
	{
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign == null)
			{
				return "no_campaign";
			}
			CampaignTime now = CampaignTime.Now;
			return SafeString(() => now.ToString(), "?") + "/day=" + SafeString(() => Math.Floor(now.ToDays).ToString("0"), "?") + "/hour=" + SafeString(() => now.GetHourOfDay.ToString(), "?");
		}
		catch (Exception ex)
		{
			return "time_error:" + ex.GetType().Name;
		}
	}

	private static string DescribeArg(object value)
	{
		if (value == null)
		{
			return "null";
		}
		if (value is MobileParty party)
		{
			return DescribeMobileParty(party);
		}
		if (value is Settlement settlement)
		{
			return DescribeSettlement(settlement);
		}
		if (value is Town town)
		{
			return DescribeTown(town);
		}
		if (value is Hero hero)
		{
			return DescribeHero(hero);
		}
		if (value is Clan clan)
		{
			return DescribeClan(clan);
		}
		if (value is float number)
		{
			return "float:" + number.ToString("0.###");
		}
		return value.GetType().FullName ?? value.GetType().Name;
	}

	private static string DescribeMobileParty(MobileParty party)
	{
		if (party == null)
		{
			return "MobileParty:null";
		}
		return "MobileParty{id=" + SafeString(() => party.StringId, "?")
			+ ",name=" + SafeString(() => party.Name?.ToString(), "?")
			+ ",active=" + SafeString(() => party.IsActive.ToString(), "?")
			+ ",leader=" + SafeString(() => party.LeaderHero?.StringId, "null")
			+ ",owner=" + SafeString(() => party.Party?.Owner?.StringId, "null")
			+ ",clan=" + SafeString(() => party.ActualClan?.StringId, "null")
			+ ",mapFaction=" + SafeString(() => GetObjectStableId(party.MapFaction), "null")
			+ ",settlement=" + SafeString(() => party.CurrentSettlement?.StringId, "null")
			+ ",lastSettlement=" + SafeString(() => party.LastVisitedSettlement?.StringId, "null")
			+ ",targetSettlement=" + SafeString(() => party.TargetSettlement?.StringId, "null")
			+ ",targetParty=" + SafeString(() => party.TargetParty?.StringId, "null")
			+ ",default=" + SafeString(() => party.DefaultBehavior.ToString(), "?")
			+ ",short=" + SafeString(() => party.ShortTermBehavior.ToString(), "?")
			+ ",atSea=" + SafeString(() => party.IsCurrentlyAtSea.ToString(), "?")
			+ ",mapEvent=" + SafeString(() => party.MapEvent == null ? "null" : "ok", "?")
			+ ",army=" + SafeString(() => party.Army?.Name?.ToString(), "null")
			+ ",component=" + SafeString(() => party.PartyComponent?.GetType().FullName, "null")
			+ ",ai=" + DescribePartyAi(party)
			+ "}";
	}

	private static string DescribePartyAi(MobileParty party)
	{
		try
		{
			if (party?.Ai == null)
			{
				return "null";
			}
			return "disabled=" + party.Ai.IsDisabled
				+ ",noNew=" + party.Ai.DoNotMakeNewDecisions
				+ ",rethink=" + party.Ai.RethinkAtNextHourlyTick
				+ ",hour=" + party.Ai.HourCounter;
		}
		catch (Exception ex)
		{
			return "error:" + ex.GetType().Name;
		}
	}

	private static string DescribeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return "Settlement:null";
		}
		return "Settlement{id=" + SafeString(() => settlement.StringId, "?")
			+ ",name=" + SafeString(() => settlement.Name?.ToString(), "?")
			+ ",ownerClan=" + SafeString(() => settlement.OwnerClan?.StringId, "null")
			+ "}";
	}

	private static string DescribeTown(Town town)
	{
		if (town == null)
		{
			return "Town:null";
		}
		return "Town{settlement=" + SafeString(() => town.Settlement?.StringId, "?")
			+ ",name=" + SafeString(() => town.Name?.ToString(), "?")
			+ ",owner=" + SafeString(() => GetObjectStableId(town.Owner), "null")
			+ "}";
	}

	private static string DescribeHero(Hero hero)
	{
		if (hero == null)
		{
			return "Hero:null";
		}
		return "Hero{id=" + SafeString(() => hero.StringId, "?")
			+ ",name=" + SafeString(() => hero.Name?.ToString(), "?")
			+ ",clan=" + SafeString(() => hero.Clan?.StringId, "null")
			+ ",party=" + SafeString(() => hero.PartyBelongedTo?.StringId, "null")
			+ ",settlement=" + SafeString(() => hero.CurrentSettlement?.StringId, "null")
			+ "}";
	}

	private static string DescribeClan(Clan clan)
	{
		if (clan == null)
		{
			return "Clan:null";
		}
		return "Clan{id=" + SafeString(() => clan.StringId, "?")
			+ ",name=" + SafeString(() => clan.Name?.ToString(), "?")
			+ ",kingdom=" + SafeString(() => clan.Kingdom?.StringId, "null")
			+ ",leader=" + SafeString(() => clan.Leader?.StringId, "null")
			+ "}";
	}

	private static string SafeString(Func<string> getter, string fallback)
	{
		try
		{
			string value = getter?.Invoke();
			return string.IsNullOrWhiteSpace(value) ? fallback : Sanitize(value);
		}
		catch (Exception ex)
		{
			return "error:" + ex.GetType().Name;
		}
	}

	private static string GetObjectStableId(object value)
	{
		if (value == null)
		{
			return "null";
		}
		try
		{
			PropertyInfo property = value.GetType().GetProperty("StringId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			object id = property?.GetValue(value, null);
			if (id != null)
			{
				return id.ToString();
			}
		}
		catch
		{
		}
		try
		{
			return value.GetType().Name;
		}
		catch
		{
			return "unknown";
		}
	}

	private static string Sanitize(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}
		string text = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
		return text.Length <= 120 ? text : text.Substring(0, 120);
	}
}
