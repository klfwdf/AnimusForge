using System;
using System.Collections.Generic;

namespace AnimusForge.ExpeditionParade.Mission;

internal enum ParadeCrowdReaction
{
	EnthusiasticCheer,
	SupportiveGesture,
	QuietWatch,
	ComplaintOrJeer,
	FearfulStepAside
}

internal enum ParadeCrowdState
{
	Ambient,
	Notice,
	StepAside,
	Watch,
	React,
	Resume,
	Released
}

internal sealed class ParadeCrowdContext
{
	internal bool IsPlayerOwner { get; set; }

	internal bool IsPlayerRuler { get; set; }

	internal int Loyalty { get; set; } = 50;

	internal int Security { get; set; } = 50;

	internal int Relation { get; set; }

	internal int RecentActionInfluence { get; set; }

	internal int SessionSeed { get; set; }
}

internal sealed class ParadeCrowdParticipant
{
	internal ParadeCrowdParticipant(string stableId, string cultureId, bool createdByParade, ParadeCrowdReaction reaction)
	{
		StableId = string.IsNullOrWhiteSpace(stableId) ? throw new ArgumentException("Stable civilian id is required.", nameof(stableId)) : stableId;
		CultureId = cultureId ?? string.Empty;
		CreatedByParade = createdByParade;
		Reaction = reaction;
		State = ParadeCrowdState.Ambient;
	}

	internal string StableId { get; }

	internal string CultureId { get; }

	internal bool CreatedByParade { get; }

	internal ParadeCrowdReaction Reaction { get; }

	internal ParadeCrowdState State { get; private set; }

	internal bool TryAdvance(ParadeCrowdState next)
	{
		bool valid = (State, next) switch
		{
			(ParadeCrowdState.Ambient, ParadeCrowdState.Notice) => true,
			(ParadeCrowdState.Notice, ParadeCrowdState.StepAside) => true,
			(ParadeCrowdState.StepAside, ParadeCrowdState.Watch) => true,
			(ParadeCrowdState.Watch, ParadeCrowdState.React) => true,
			(ParadeCrowdState.React, ParadeCrowdState.Resume) => true,
			(ParadeCrowdState.Resume, ParadeCrowdState.Released) => true,
			_ => false
		};
		if (valid)
		{
			State = next;
		}
		return valid;
	}

	internal void Release()
	{
		State = ParadeCrowdState.Released;
	}
}

internal sealed class CrowdReactionController
{
	private readonly Dictionary<string, ParadeCrowdParticipant> _participants = new(StringComparer.Ordinal);

	internal IReadOnlyCollection<ParadeCrowdParticipant> Participants => _participants.Values;

	internal bool TryRegisterCivilian(
		string stableId,
		string civilianCultureId,
		string settlementCultureId,
		bool isCivilianTemplate,
		bool createdByParade,
		ParadeCrowdContext context,
		out ParadeCrowdParticipant participant,
		out string failure)
	{
		participant = null;
		if (!isCivilianTemplate)
		{
			failure = "audience_template_is_not_civilian";
			return false;
		}
		if (createdByParade && !string.Equals(civilianCultureId, settlementCultureId, StringComparison.OrdinalIgnoreCase))
		{
			failure = "temporary_civilian_culture_mismatch";
			return false;
		}
		if (string.IsNullOrWhiteSpace(stableId) || _participants.ContainsKey(stableId))
		{
			failure = "civilian_id_missing_or_duplicate";
			return false;
		}

		ParadeCrowdReaction reaction = EvaluateReaction(context ?? throw new ArgumentNullException(nameof(context)), stableId);
		participant = new ParadeCrowdParticipant(stableId, civilianCultureId, createdByParade, reaction);
		_participants.Add(stableId, participant);
		failure = string.Empty;
		return true;
	}

	internal static ParadeCrowdReaction EvaluateReaction(ParadeCrowdContext context, string stableCivilianId)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}
		int score = (context.IsPlayerOwner ? 18 : 0)
			+ (context.IsPlayerRuler ? 10 : 0)
			+ (Clamp(context.Loyalty, 0, 100) - 50) / 2
			+ (Clamp(context.Security, 0, 100) - 50) / 3
			+ Clamp(context.Relation, -100, 100) / 4
			+ Clamp(context.RecentActionInfluence, -40, 40)
			+ StableVariance(context.SessionSeed, stableCivilianId, 31);

		if (score >= 35)
		{
			return ParadeCrowdReaction.EnthusiasticCheer;
		}
		if (score >= 12)
		{
			return ParadeCrowdReaction.SupportiveGesture;
		}
		if (score >= -10)
		{
			return ParadeCrowdReaction.QuietWatch;
		}
		return context.Security < 30 || score < -35
			? ParadeCrowdReaction.FearfulStepAside
			: ParadeCrowdReaction.ComplaintOrJeer;
	}

	internal void ReleaseAll()
	{
		foreach (ParadeCrowdParticipant participant in _participants.Values)
		{
			participant.Release();
		}
	}

	private static int StableVariance(int seed, string value, int range)
	{
		unchecked
		{
			uint hash = 2166136261u ^ (uint)seed;
			foreach (char character in value ?? string.Empty)
			{
				hash ^= character;
				hash *= 16777619u;
			}
			return (int)(hash % (uint)range) - range / 2;
		}
	}

	private static int Clamp(int value, int minimum, int maximum)
	{
		return Math.Max(minimum, Math.Min(maximum, value));
	}
}
