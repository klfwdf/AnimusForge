using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Frozen UI targeting input. Live Agent validity is still checked by the game-thread host
/// before the audience scope is created; this object only preserves the originally framed set.
/// </summary>
internal sealed class ShoutTargetingContext
{
	public float RangeMeters;

	public float HalfAngleRadians;

	public int PrimaryAgentIndex = -1;

	public List<int> CandidateAgentIndices = new List<int>();

	public Dictionary<int, float> CandidatePlayerDistancesMeters = new Dictionary<int, float>();

	public List<Agent> PreviewCandidateAgents = new List<Agent>();
}

/// <summary>
/// Identity captured before a player-driven Scene request may wait for the previous
/// postprocess gate. It is transient and never persisted.
/// </summary>
internal sealed class ScenePlayerShoutRequest
{
	public ShoutBehavior Owner;

	public Mission Mission;

	public Agent Player;

	public long RuntimeGeneration;

	public int SceneSessionId;

	public int ConversationEpoch;

	public long InputSequence;

	public ShoutTargetingContext TargetingContext;

	public int Started;
}

/// <summary>
/// A read-only pre-claim view. Issuing it must not invalidate an existing Scene input.
/// The host also validates its mutable UI targeting source before claiming it.
/// </summary>
internal sealed class ScenePlayerShoutContext
{
	public ShoutBehavior Owner;
	public Mission Mission;
	public Agent Player;
	public long RuntimeGeneration;
	public int SceneSessionId;
	public int ConversationEpoch;
	public long InputSequence;
	public ShoutTargetingContext SourceTargetingContext;
	public ShoutTargetingContext TargetingContext;
}

/// <summary>
/// Owns Scene player-input request sequencing and one-shot claim state. The host supplies
/// current Mission/runtime values on the game thread; the owner does not read global game state.
/// </summary>
internal sealed class ScenePlayerShoutRequestOwner
{
	internal const int MaximumModuleTickets = 128;
	private sealed class ModuleTicket
	{
		internal string ClientId;
		internal ScenePlayerShoutContext Context;
	}

	private readonly object _inputGate = new object();
	private readonly Dictionary<string, ModuleTicket> _moduleTickets = new Dictionary<string, ModuleTicket>(StringComparer.Ordinal);
	private long _inputSequence;
	private long _moduleClaimedSequence = -1;

	internal void InvalidateCurrent()
	{
		lock (_inputGate)
		{
			Interlocked.Increment(ref _inputSequence);
			_moduleClaimedSequence = -1;
			_moduleTickets.Clear();
		}
	}

	internal string IssueModuleTicket(string clientId, ScenePlayerShoutContext context)
	{
		if (string.IsNullOrEmpty(clientId) || context == null)
			return null;
		lock (_inputGate)
		{
			if (context.InputSequence != _inputSequence || _moduleTickets.Count >= MaximumModuleTickets)
				return null;
			string id;
			do { id = Guid.NewGuid().ToString("N"); } while (_moduleTickets.ContainsKey(id));
			_moduleTickets.Add(id, new ModuleTicket { ClientId = clientId, Context = context });
			return id;
		}
	}

	internal bool TryTakeModuleTicket(string clientId, string id, out ScenePlayerShoutContext context)
	{
		context = null;
		if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(id))
			return false;
		lock (_inputGate)
		{
			if (!_moduleTickets.TryGetValue(id, out ModuleTicket ticket)
				|| !string.Equals(ticket.ClientId, clientId, StringComparison.Ordinal))
				return false;
			_moduleTickets.Remove(id);
			context = ticket.Context;
			return true;
		}
	}

	internal void InvalidateModuleTickets()
	{
		lock (_inputGate) _moduleTickets.Clear();
	}

	internal void RevokeClientModuleTickets(string clientId)
	{
		if (string.IsNullOrEmpty(clientId)) return;
		lock (_inputGate)
		{
			List<string> revoke = new List<string>();
			foreach (KeyValuePair<string, ModuleTicket> entry in _moduleTickets)
				if (string.Equals(entry.Value.ClientId, clientId, StringComparison.Ordinal))
					revoke.Add(entry.Key);
			foreach (string id in revoke) _moduleTickets.Remove(id);
		}
	}

	internal ScenePlayerShoutContext CaptureContext(
		ShoutBehavior owner,
		Mission mission,
		Agent player,
		long runtimeGeneration,
		int sceneSessionId,
		int conversationEpoch,
		ShoutTargetingContext sourceTargetingContext,
		ShoutTargetingContext targetingContext)
	{
		return new ScenePlayerShoutContext
		{
			Owner = owner,
			Mission = mission,
			Player = player,
			RuntimeGeneration = runtimeGeneration,
			SceneSessionId = sceneSessionId,
			ConversationEpoch = conversationEpoch,
			InputSequence = Interlocked.Read(ref _inputSequence),
			SourceTargetingContext = sourceTargetingContext,
			TargetingContext = targetingContext
		};
	}

	internal bool TryClaimContext(
		ScenePlayerShoutContext context,
		ShoutBehavior owner,
		Mission currentMission,
		Agent currentPlayer,
		bool runtimeGenerationIsCurrent,
		int currentSceneSessionId,
		int currentConversationEpoch,
		out ScenePlayerShoutRequest request)
	{
		request = null;
		if (context == null || !ReferenceEquals(context.Owner, owner)
			|| context.Mission == null || !ReferenceEquals(context.Mission, currentMission)
			|| context.Player == null || !ReferenceEquals(context.Player, currentPlayer)
			|| !runtimeGenerationIsCurrent || context.SceneSessionId != currentSceneSessionId
			|| context.ConversationEpoch != currentConversationEpoch
			|| context.TargetingContext == null || context.InputSequence == long.MaxValue)
			return false;
		lock (_inputGate)
		{
			long nextSequence = context.InputSequence + 1;
			if (Interlocked.CompareExchange(ref _inputSequence, nextSequence, context.InputSequence) != context.InputSequence)
				return false;
			_moduleClaimedSequence = nextSequence;
			request = new ScenePlayerShoutRequest
			{
				Owner = owner,
				Mission = currentMission,
				Player = currentPlayer,
				RuntimeGeneration = context.RuntimeGeneration,
				SceneSessionId = context.SceneSessionId,
				ConversationEpoch = context.ConversationEpoch,
				InputSequence = nextSequence,
				TargetingContext = context.TargetingContext
			};
			return true;
		}
	}

	internal ScenePlayerShoutRequest Capture(
		ShoutBehavior owner,
		Mission mission,
		Agent player,
		long runtimeGeneration,
		int sceneSessionId,
		int conversationEpoch,
		ShoutTargetingContext targetingContext)
	{
		lock (_inputGate)
		{
			// A module claim owns this UI input until the next processing session.
			if (_moduleClaimedSequence == Interlocked.Read(ref _inputSequence))
				return null;
			return new ScenePlayerShoutRequest
			{
				Owner = owner,
				Mission = mission,
				Player = player,
				RuntimeGeneration = runtimeGeneration,
				SceneSessionId = sceneSessionId,
				ConversationEpoch = conversationEpoch,
				InputSequence = Interlocked.Increment(ref _inputSequence),
				TargetingContext = targetingContext
			};
		}
	}

	internal bool IsCurrent(
		ScenePlayerShoutRequest request,
		ShoutBehavior owner,
		Mission currentMission,
		Agent currentPlayer,
		bool runtimeGenerationIsCurrent,
		int currentSceneSessionId,
		int currentConversationEpoch)
	{
		return request != null
			&& ReferenceEquals(request.Owner, owner)
			&& request.Mission != null
			&& ReferenceEquals(request.Mission, currentMission)
			&& request.Player != null
			&& ReferenceEquals(request.Player, currentPlayer)
			&& runtimeGenerationIsCurrent
			&& request.SceneSessionId == currentSceneSessionId
			&& request.ConversationEpoch == currentConversationEpoch
			&& request.InputSequence == Interlocked.Read(ref _inputSequence);
	}

	internal bool IsUnclaimed(ScenePlayerShoutRequest request)
	{
		return request != null && Volatile.Read(ref request.Started) == 0;
	}

	internal bool TryClaim(ScenePlayerShoutRequest request)
	{
		return request != null && Interlocked.CompareExchange(ref request.Started, 1, 0) == 0;
	}

	internal void MarkStarted(ScenePlayerShoutRequest request)
	{
		if (request != null)
		{
			Interlocked.Exchange(ref request.Started, 1);
		}
	}
}
