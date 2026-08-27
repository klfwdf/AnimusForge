using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.ExpeditionParade.Diagnostics;

internal sealed class ParadeSessionDiagnostics
{
	private const int MaximumEvents = 256;
	private const int MaximumValueLength = 512;
	private readonly Dictionary<string, string> _fields = new(StringComparer.Ordinal);
	private readonly List<string> _events = new();

	internal ParadeSessionDiagnostics(string sessionId, string settlementId)
	{
		SetField("session_id", sessionId);
		SetField("settlement_id", settlementId);
	}

	internal void SetField(string key, object value)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Diagnostic key is required.", nameof(key));
		}
		_fields[key] = Sanitize(value?.ToString());
	}

	internal void RecordEvent(string code, string detail = "")
	{
		if (_events.Count >= MaximumEvents)
		{
			return;
		}
		_events.Add(Sanitize(code) + (string.IsNullOrWhiteSpace(detail) ? string.Empty : ":" + Sanitize(detail)));
	}

	internal IReadOnlyList<string> Snapshot()
	{
		return _fields.OrderBy(pair => pair.Key, StringComparer.Ordinal)
			.Select(pair => pair.Key + "=" + pair.Value)
			.Concat(_events.Select((value, index) => "event[" + index + "]=" + value))
			.ToArray();
	}

	private static string Sanitize(string value)
	{
		string sanitized = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
		return sanitized.Length <= MaximumValueLength ? sanitized : sanitized.Substring(0, MaximumValueLength);
	}
}
