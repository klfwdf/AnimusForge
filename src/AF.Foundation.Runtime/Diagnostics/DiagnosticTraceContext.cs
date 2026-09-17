using System;
using System.Threading;

namespace AnimusForge;

internal sealed class DiagnosticTraceContext
{
	internal sealed class ScopeState
	{
		internal string TraceId;
		internal string Channel;
		internal string HeroId;
		internal string NpcName;
		internal long StartUtcTicks;
		internal ScopeState Parent;
	}

	private readonly AsyncLocal<ScopeState> _current = new AsyncLocal<ScopeState>();
	private long _traceSeed;

	internal ScopeState Current => _current.Value;
	internal string CurrentTraceId => _current.Value?.TraceId ?? "";
	internal string CurrentChannel => _current.Value?.Channel ?? "";

	internal ScopeState Push(string channel, string heroId, string npcName, string traceId)
	{
		ScopeState previous = _current.Value;
		string id = (traceId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			id = previous == null || string.IsNullOrWhiteSpace(previous.TraceId) ? NewTraceId() : previous.TraceId;
		}
		ScopeState state = new ScopeState
		{
			TraceId = id,
			Channel = (channel ?? previous?.Channel ?? "").Trim(),
			HeroId = (heroId ?? previous?.HeroId ?? "").Trim(),
			NpcName = (npcName ?? previous?.NpcName ?? "").Trim(),
			StartUtcTicks = DateTime.UtcNow.Ticks,
			Parent = previous
		};
		_current.Value = state;
		return state;
	}

	internal void Restore(ScopeState previous) => _current.Value = previous;

	private string NewTraceId()
	{
		long sequence = Interlocked.Increment(ref _traceSeed);
		string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
		return "vf-" + timestamp + "-" + sequence.ToString("x");
	}
}
