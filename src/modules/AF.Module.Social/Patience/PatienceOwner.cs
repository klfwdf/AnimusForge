using System;
using System.Collections.Generic;

namespace AnimusForge;
internal class PatienceRecord
{
	public float Value;
	public float LastDay;
	public int NoInterestRounds;
	public int ExhaustedRefusalCount;
}

internal readonly struct PatienceChange
{
	internal readonly int Before;
	internal readonly int After;
	internal readonly int Delta;
	internal bool BecameExhausted => Before > 0 && After <= 0;

	internal PatienceChange(int before, int after, int delta)
	{
		Before = before;
		After = after;
		Delta = delta;
	}
}

// One keyed state authority; no daily/tick scan. Save snapshots are detached, not live state.
internal sealed class PatienceOwner<TState>
	where TState : PatienceRecord, new()
{
	private readonly object _gate = new object ();
	private readonly Dictionary<string, TState> _states = new Dictionary<string, TState>();
	private TState Get(string key, int max, float now)
	{
		if (!_states.TryGetValue(key, out var state) || state == null)
		{
			state = new TState
			{
				Value = max,
				LastDay = now
			};
			_states[key] = state;
		}

		return state;
	}

	private static void Recover(TState state, int max, float now)
	{
		if (now > state.LastDay)
		{
			float elapsed = now - state.LastDay;
			state.Value += elapsed * 4f;
			state.LastDay = now;
			if (state.NoInterestRounds > 0)
			{
				int days = (int)Math.Floor(elapsed);
				if (days > 0)
					state.NoInterestRounds = Math.Max(0, state.NoInterestRounds - days);
			}
		}

		state.Value = Math.Max(0f, Math.Min(max, state.Value));
		if (state.Value > .01f && state.ExhaustedRefusalCount > 0)
			state.ExhaustedRefusalCount = 0;
	}

	internal float Snapshot(string key, int max, float now)
	{
		if (string.IsNullOrWhiteSpace(key))
			return max;
		lock (_gate)
		{
			var state = Get(key, max, now);
			Recover(state, max, now);
			return state.Value;
		}
	}

	internal PatienceChange Apply(string key, int max, float now, PatienceMood mood)
	{
		if (string.IsNullOrWhiteSpace(key) || max <= 0)
			return default;
		lock (_gate)
		{
			var state = Get(key, max, now);
			Recover(state, max, now);
			int before = (int)Math.Round(state.Value);
			int delta = PatienceRules.ComputePatienceDelta(mood, ref state.NoInterestRounds);
			state.Value = Math.Max(0f, Math.Min(max, state.Value + delta));
			state.LastDay = now;
			return new PatienceChange(before, (int)Math.Round(state.Value), delta);
		}
	}

	internal PatienceChange OverrideNeutral(string key, int max, float now, PatienceMood mood)
	{
		if (string.IsNullOrWhiteSpace(key) || max <= 0 || mood == PatienceMood.Neutral)
			return default;
		lock (_gate)
		{
			var state = Get(key, max, now);
			Recover(state, max, now);
			int before = (int)Math.Round(state.Value);
			int baseline = Math.Max(0, state.NoInterestRounds - 1);
			int neutralRounds = baseline;
			int moodRounds = baseline;
			int neutral = PatienceRules.ComputePatienceDelta(PatienceMood.Neutral, ref neutralRounds);
			int delta = PatienceRules.ComputePatienceDelta(mood, ref moodRounds) - neutral;
			state.Value = Math.Max(0f, Math.Min(max, state.Value + delta));
			state.NoInterestRounds = moodRounds;
			state.LastDay = now;
			return new PatienceChange(before, (int)Math.Round(state.Value), delta);
		}
	}

	internal Dictionary<string, TState> SaveSnapshot()
	{
		lock (_gate)
		{
			var result = new Dictionary<string, TState>();
			foreach (var pair in _states)
			{
				var state = pair.Value;
				if (state != null)
				{
					result[pair.Key] = new TState
					{
						Value = state.Value,
						LastDay = state.LastDay,
						NoInterestRounds = state.NoInterestRounds,
						ExhaustedRefusalCount = state.ExhaustedRefusalCount
					};
				}
			}

			return result;
		}
	}

	internal void Replace(IEnumerable<KeyValuePair<string, TState>> loaded)
	{
		lock (_gate)
		{
			_states.Clear();
			if (loaded == null)
				return;
			foreach (var pair in loaded)
			{
				if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
					_states[pair.Key] = pair.Value;
			}
		}
	}

	internal void Clear()
	{
		lock (_gate)
			_states.Clear();
	}
}
