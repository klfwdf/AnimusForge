using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

public sealed partial class ProactiveNpcRequestBehavior
{
	// Main-thread scan state and candidate ordering; the host performs TW reads within the frame budget.
	private sealed class ProactiveCandidateScanOwner
	{
		internal ProactiveCandidateScanState Current { get; private set; }
		internal bool IsRunning => Current != null;

		internal ProactiveCandidateScanState Start(DuelSettings settings, List<MobileParty> parties, long startedAtUtcTicks)
		{
			if (Current != null)
			{
				return null;
			}
			int batchSize = Math.Max(1, (int)Math.Ceiling(parties.Count / (double)CandidateScanTargetFrames));
			Current = new ProactiveCandidateScanState
			{
				Settings = settings,
				Parties = parties,
				BatchSize = Math.Min(CandidateScanMaxPartiesPerTick, batchSize),
				Stats = new CandidateScanStats(),
				StartedAtUtcTicks = startedAtUtcTicks
			};
			return Current;
		}

		internal void Consider(ProactiveCandidateScanState scan, ProactiveCandidate candidate, CandidateScanStats stats)
		{
			if (!ReferenceEquals(Current, scan))
			{
				return;
			}
			scan.Stats.MergeFrom(stats);
			if (IsCandidateBetter(candidate, scan.BestCandidate))
			{
				scan.BestCandidate = candidate;
			}
		}

		internal bool TryComplete(ProactiveCandidateScanState scan)
		{
			if (!ReferenceEquals(Current, scan) || scan == null)
			{
				return false;
			}
			Current = null;
			return true;
		}

		internal void Clear() => Current = null;

		internal static bool IsCandidateBetter(ProactiveCandidate candidate, ProactiveCandidate currentBest)
		{
			if (candidate == null)
			{
				return false;
			}
			if (currentBest == null)
			{
				return true;
			}
			float candidateWeightedUrgency = GetWeightedUrgency(candidate);
			float bestWeightedUrgency = GetWeightedUrgency(currentBest);
			if (Math.Abs(candidateWeightedUrgency - bestWeightedUrgency) > 0.001f) return candidateWeightedUrgency > bestWeightedUrgency;
			if (Math.Abs(candidate.NeedUrgency - currentBest.NeedUrgency) > 0.001f) return candidate.NeedUrgency > currentBest.NeedUrgency;
			if (candidate.EffectiveNotorietyAtRequest != currentBest.EffectiveNotorietyAtRequest) return candidate.EffectiveNotorietyAtRequest > currentBest.EffectiveNotorietyAtRequest;
			return candidate.Distance < currentBest.Distance;
		}

		internal static float GetWeightedUrgency(ProactiveCandidate candidate)
		{
			if (candidate == null)
			{
				return 0f;
			}
			return Clamp(candidate.NeedUrgency, 0f, 100f)
				* Clamp(candidate.NeedTypeFatigueMultiplier, 0f, 1f)
				* Clamp(candidate.NeedTypeWeightMultiplier, 0f, 1f);
		}
	}
}
