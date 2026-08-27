using System;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.ExpeditionParade;

internal sealed class ExpeditionParadeSession
{
	internal ExpeditionParadeSession(string settlementId, string locationId, ParadeRosterSnapshot roster)
	{
		SessionId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss.fff");
		SettlementId = settlementId ?? string.Empty;
		LocationId = locationId ?? string.Empty;
		Roster = roster;
		CreatedUtc = DateTime.UtcNow;
	}

	internal string SessionId { get; }

	internal string SettlementId { get; }

	internal string LocationId { get; }

	internal ParadeRosterSnapshot Roster { get; }

	internal DateTime CreatedUtc { get; }
}

internal static class ExpeditionParadeRuntime
{
	private static readonly TimeSpan PendingLifetime = TimeSpan.FromSeconds(30.0);
	private static ExpeditionParadeSession _pending;
	private static ExpeditionParadeSession _active;

	internal static ExpeditionParadeSession Pending => _pending;

	internal static ExpeditionParadeSession Active => _active;

	internal static void Queue(ExpeditionParadeSession session)
	{
		if (session == null)
		{
			throw new ArgumentNullException(nameof(session));
		}
		Clear("queue_replace");
		_pending = session;
	}

	internal static bool TryActivate(Mission mission, string liveSettlementId, out ExpeditionParadeSession session)
	{
		session = null;
		ExpeditionParadeSession pending = _pending;
		if (mission == null || pending == null)
		{
			return false;
		}
		if (DateTime.UtcNow - pending.CreatedUtc > PendingLifetime)
		{
			Clear("pending_expired");
			return false;
		}
		if (!string.Equals(pending.SettlementId, liveSettlementId, StringComparison.OrdinalIgnoreCase))
		{
			Clear("mission_settlement_mismatch");
			return false;
		}

		_pending = null;
		_active = pending;
		session = pending;
		return true;
	}

	internal static void Complete(ExpeditionParadeSession session, string source)
	{
		if (ReferenceEquals(_active, session))
		{
			Logger.Log("ExpeditionParade", "Session completed. id=" + session.SessionId + ", source=" + (source ?? "N/A"));
			_active = null;
		}
	}

	internal static void Clear(string source)
	{
		if (_pending != null || _active != null)
		{
			Logger.Log("ExpeditionParade", "Runtime cleared. source=" + (source ?? "N/A"));
		}
		_pending = null;
		_active = null;
	}
}
