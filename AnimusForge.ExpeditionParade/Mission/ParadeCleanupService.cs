using System;
using System.Collections.Generic;

namespace AnimusForge.ExpeditionParade.Mission;

internal sealed class ParadeCleanupService
{
	private readonly List<CleanupEntry> _entries = new();
	private readonly HashSet<string> _keys = new(StringComparer.Ordinal);
	private bool _completed;

	internal bool IsCompleted => _completed;

	internal int RegisteredCount => _entries.Count;

	internal void Register(string key, Action cleanup)
	{
		if (_completed)
		{
			throw new InvalidOperationException("Cleanup has already completed.");
		}
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Cleanup key is required.", nameof(key));
		}
		if (cleanup == null)
		{
			throw new ArgumentNullException(nameof(cleanup));
		}
		if (!_keys.Add(key))
		{
			throw new InvalidOperationException("Duplicate cleanup key: " + key);
		}
		_entries.Add(new CleanupEntry(key, cleanup));
	}

	internal IReadOnlyList<string> RunOnce()
	{
		if (_completed)
		{
			return Array.Empty<string>();
		}
		_completed = true;
		List<string> failures = new();
		for (int index = _entries.Count - 1; index >= 0; index--)
		{
			try
			{
				_entries[index].Action();
			}
			catch (Exception ex)
			{
				failures.Add(_entries[index].Key + ":" + ex.GetType().Name + ":" + ex.Message);
			}
		}
		return failures;
	}

	private sealed class CleanupEntry
	{
		internal CleanupEntry(string key, Action action)
		{
			Key = key;
			Action = action;
		}

		internal string Key { get; }

		internal Action Action { get; }
	}
}
