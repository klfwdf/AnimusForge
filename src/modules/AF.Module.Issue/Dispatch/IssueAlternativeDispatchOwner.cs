using System;

namespace AnimusForge;

internal static partial class VanillaIssueOfferBridge
{
	// Main-thread, runtime-only identity for the one open vanilla issue troop screen.
	private sealed class IssueAlternativeDispatchOwner
	{
		private PendingAlternativeDispatch _pending;

		internal bool HasPending => _pending != null;

		internal bool TryBegin(PendingAlternativeDispatch pending)
		{
			if (pending == null || _pending != null)
			{
				return false;
			}
			_pending = pending;
			return true;
		}

		internal bool IsCurrent(PendingAlternativeDispatch pending)
		{
			return pending != null && ReferenceEquals(_pending, pending);
		}

		internal bool TryTake(PendingAlternativeDispatch expected, out PendingAlternativeDispatch pending)
		{
			pending = null;
			if (!IsCurrent(expected))
			{
				return false;
			}
			pending = _pending;
			_pending = null;
			return true;
		}

		internal void Clear()
		{
			_pending = null;
		}
	}
}
