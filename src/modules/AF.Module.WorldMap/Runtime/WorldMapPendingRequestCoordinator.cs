using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// Owns process-local identities for delayed WorldMap requests keyed by actor.
/// Payload remains in the Campaign host; each ticket is single-claim and cannot
/// be used by an old request to remove a newer request for the same actor.
/// </summary>
internal sealed class WorldMapPendingRequestCoordinator
{
	private readonly object _sync = new object();
	private readonly Dictionary<string, long> _ticketByOwner = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
	private long _nextTicket;

	internal long GetOrCreate(string ownerId)
	{
		string key = Normalize(ownerId);
		if (key.Length == 0) return 0L;
		lock (_sync)
		{
			if (_ticketByOwner.TryGetValue(key, out long existing)) return existing;
			_nextTicket = _nextTicket == long.MaxValue ? 1L : _nextTicket + 1L;
			if (_nextTicket == 0L) _nextTicket = 1L;
			_ticketByOwner[key] = _nextTicket;
			return _nextTicket;
		}
	}

	internal bool IsCurrent(string ownerId, long ticket)
	{
		string key = Normalize(ownerId);
		lock (_sync)
		{
			return ticket > 0L && _ticketByOwner.TryGetValue(key, out long current) && current == ticket;
		}
	}

	internal bool TryClaim(string ownerId, long ticket)
	{
		string key = Normalize(ownerId);
		lock (_sync)
		{
			if (ticket <= 0L || !_ticketByOwner.TryGetValue(key, out long current) || current != ticket)
			{
				return false;
			}
			_ticketByOwner.Remove(key);
			return true;
		}
	}

	internal bool TryCancel(string ownerId, long ticket) => TryClaim(ownerId, ticket);

	internal void Reset()
	{
		lock (_sync)
		{
			_ticketByOwner.Clear();
		}
	}

	private static string Normalize(string ownerId) => (ownerId ?? "").Trim();
}
