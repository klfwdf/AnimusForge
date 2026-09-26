using System;

namespace AnimusForge;

public sealed partial class ProactiveNpcRequestBehavior
{
	// Runtime-only opening authority; the saved session and game-object adapters stay in the host.
	private sealed class ProactiveOpeningOwner
	{
		private PendingOpeningFact _native;
		private PendingOpeningFact _scene;

		internal void Open(bool native, string sessionId, string heroId, string extraFact, string promptText, float nowHours)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(heroId))
			{
				return;
			}
			var pending = new PendingOpeningFact
			{
				SessionId = sessionId,
				HeroId = heroId,
				ExtraFact = extraFact,
				PromptText = promptText,
				CreatedAtHours = nowHours
			};
			if (native)
			{
				_native = pending;
				_scene = null;
			}
			else
			{
				_scene = pending;
				_native = null;
			}
		}

		internal bool Matches(bool native, string sessionId, string heroId)
		{
			var pending = native ? _native : _scene;
			return pending != null
				&& !string.IsNullOrWhiteSpace(sessionId)
				&& !string.IsNullOrWhiteSpace(heroId)
				&& string.Equals(pending.SessionId, sessionId, StringComparison.Ordinal)
				&& string.Equals(pending.HeroId, heroId, StringComparison.OrdinalIgnoreCase);
		}

		internal bool TryPeek(bool native, string sessionId, out string heroId, out string extraFact, out string promptText)
		{
			heroId = "";
			extraFact = "";
			promptText = "";
			var pending = native ? _native : _scene;
			if (pending == null || string.IsNullOrWhiteSpace(sessionId)
				|| !string.Equals(pending.SessionId, sessionId, StringComparison.Ordinal))
			{
				return false;
			}
			heroId = pending.HeroId ?? "";
			extraFact = pending.ExtraFact ?? "";
			promptText = pending.PromptText ?? "";
			return !string.IsNullOrWhiteSpace(heroId);
		}

		internal bool TryConsume(bool native, string sessionId, string heroId, out string extraFact, out string promptText)
		{
			extraFact = "";
			promptText = "";
			if (!Matches(native, sessionId, heroId))
			{
				return false;
			}
			var pending = native ? _native : _scene;
			extraFact = pending.ExtraFact ?? "";
			promptText = pending.PromptText ?? "";
			if (native)
			{
				_native = null;
			}
			else
			{
				_scene = null;
			}
			return true;
		}

		internal void Clear()
		{
			_native = null;
			_scene = null;
		}
	}
}
