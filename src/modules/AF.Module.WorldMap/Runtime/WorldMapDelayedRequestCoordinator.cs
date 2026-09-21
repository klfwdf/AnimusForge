using System;

namespace AnimusForge;

/// <summary>
/// Owns the process-local identity of the currently open WorldMap UI request.
/// Tickets are deliberately not persisted: an open native screen cannot survive
/// a save/runtime generation change, and a callback may only claim its own ticket.
/// </summary>
internal sealed class WorldMapDelayedRequestCoordinator
{
	private readonly object _sync = new object();
	private long _nextTicket;
	private long _activeTicket;

	internal bool HasActiveRequest
	{
		get
		{
			lock (_sync)
			{
				return _activeTicket != 0L;
			}
		}
	}

	internal bool TryBegin(out long ticket)
	{
		lock (_sync)
		{
			if (_activeTicket != 0L)
			{
				ticket = 0L;
				return false;
			}

			_nextTicket = _nextTicket == long.MaxValue ? 1L : _nextTicket + 1L;
			if (_nextTicket == 0L)
			{
				_nextTicket = 1L;
			}
			_activeTicket = _nextTicket;
			ticket = _activeTicket;
			return true;
		}
	}

	internal bool TryClaimCompletion(long ticket)
	{
		return TryReleaseOwned(ticket);
	}

	internal bool TryCancelOpen(long ticket)
	{
		return TryReleaseOwned(ticket);
	}

	private bool TryReleaseOwned(long ticket)
	{
		lock (_sync)
		{
			if (ticket == 0L || _activeTicket != ticket)
			{
				return false;
			}
			_activeTicket = 0L;
			return true;
		}
	}
}
