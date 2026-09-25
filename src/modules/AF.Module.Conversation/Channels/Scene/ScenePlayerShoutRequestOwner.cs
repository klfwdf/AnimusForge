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
	public ShoutTargetingContext TargetingContext;
}

/// <summary>
/// Owns Scene player-input request sequencing and one-shot claim state. The host supplies
/// current Mission/runtime values on the game thread; the owner does not read global game state.
/// </summary>
internal sealed class ScenePlayerShoutRequestOwner
{
	private long _inputSequence;

	internal void InvalidateCurrent()
	{
		Interlocked.Increment(ref _inputSequence);
	}

	internal ScenePlayerShoutContext CaptureContext(
		ShoutBehavior owner,
		Mission mission,
		Agent player,
		long runtimeGeneration,
		int sceneSessionId,
		int conversationEpoch,
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
		long nextSequence = context.InputSequence + 1;
		if (Interlocked.CompareExchange(ref _inputSequence, nextSequence, context.InputSequence) != context.InputSequence)
			return false;
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

	internal ScenePlayerShoutRequest Capture(
		ShoutBehavior owner,
		Mission mission,
		Agent player,
		long runtimeGeneration,
		int sceneSessionId,
		int conversationEpoch,
		ShoutTargetingContext targetingContext)
	{
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
