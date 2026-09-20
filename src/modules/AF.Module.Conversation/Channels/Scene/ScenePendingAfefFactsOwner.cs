using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

/// <summary>
/// Owns the bounded, process-local AFEF facts waiting for the next Scene prompt.
/// Message construction remains with the game-thread host; this owner only stores
/// already detached messages and transfers each agent's batch at most once.
/// </summary>
internal sealed class ScenePendingAfefFactsOwner
{
	internal const int MaxPendingFactsPerAgent = 12;

	private readonly object _gate = new object();

	private readonly Dictionary<int, List<ConversationMessage>> _factsByAgent =
		new Dictionary<int, List<ConversationMessage>>();

	internal void Clear()
	{
		lock (_gate)
		{
			_factsByAgent.Clear();
		}
	}

	internal void Queue(int targetAgentIndex, ConversationMessage message)
	{
		if (targetAgentIndex < 0 || message == null)
		{
			return;
		}

		lock (_gate)
		{
			if (!_factsByAgent.TryGetValue(targetAgentIndex, out List<ConversationMessage> facts)
				|| facts == null)
			{
				facts = new List<ConversationMessage>();
				_factsByAgent[targetAgentIndex] = facts;
			}

			facts.Add(message);
			while (facts.Count > MaxPendingFactsPerAgent)
			{
				facts.RemoveAt(0);
			}
		}
	}

	internal List<ConversationMessage> Consume(int targetAgentIndex)
	{
		if (targetAgentIndex < 0)
		{
			return new List<ConversationMessage>();
		}

		lock (_gate)
		{
			if (!_factsByAgent.TryGetValue(targetAgentIndex, out List<ConversationMessage> facts)
				|| facts == null
				|| facts.Count == 0)
			{
				return new List<ConversationMessage>();
			}

			_factsByAgent.Remove(targetAgentIndex);
			return facts.Where(message => message != null).ToList();
		}
	}
}
