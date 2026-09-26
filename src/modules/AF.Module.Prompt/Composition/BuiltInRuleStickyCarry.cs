using System;

namespace AnimusForge;

/// <summary>
/// Cross-turn carry for the three built-in topics (duel / reward / loan): a short
/// acknowledgement from the player keeps the previously routed topic alive for a bounded
/// number of turns on the same target. One instance per campaign owner; reset on save load.
/// Not thread-safe by design: the host calls it inside the prompt build on one request at a time.
/// This is separate from <see cref="PromptStickyRuleStore"/>, which carries kingdom_service/marriage.
/// </summary>
internal sealed class BuiltInRuleStickyCarry
{
	private static readonly string[] ShortAckTokens =
	{
		"好", "好的", "行", "可以", "同意", "确认", "就这样", "继续", "嗯", "是",
		"对", "我选雇佣兵", "雇佣兵", "我选封臣", "封臣", "那就按这个", "那就这么办"
	};

	private string _targetKey;
	private int _duelRoundsLeft;
	private int _rewardRoundsLeft;
	private int _loanRoundsLeft;

	internal int DuelRoundsLeft => _duelRoundsLeft;
	internal int RewardRoundsLeft => _rewardRoundsLeft;
	internal int LoanRoundsLeft => _loanRoundsLeft;
	internal string TargetKey => _targetKey;

	internal static bool IsShortAck(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Length > 20)
		{
			return false;
		}
		for (int i = 0; i < ShortAckTokens.Length; i++)
		{
			if (text.IndexOf(ShortAckTokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	internal static int TurnLimit(string ruleId)
	{
		switch ((ruleId ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
		case "reward":
			return 2;
		case "loan":
			return 3;
		default:
			return 0;
		}
	}

	/// <summary>Target key = hero id, else character id, else character's hero id; lower-case trimmed.</summary>
	internal static string ResolveTargetKey(string heroStringId, string characterStringId, string characterHeroStringId)
	{
		string text = heroStringId ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			text = characterStringId ?? "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = characterHeroStringId ?? "";
		}
		return (text ?? "").Trim().ToLowerInvariant();
	}

	internal void Clear()
	{
		_targetKey = null;
		_duelRoundsLeft = 0;
		_rewardRoundsLeft = 0;
		_loanRoundsLeft = 0;
	}

	/// <summary>
	/// Consume one carry round when the same target receives a short acknowledgement.
	/// Any mismatch (target, input shape, no rounds) clears the carry, matching legacy behavior.
	/// </summary>
	internal bool TryConsume(string targetKey, string playerInput, out bool duel, out bool reward, out bool loan, out string log)
	{
		duel = false;
		reward = false;
		loan = false;
		log = null;
		string text = targetKey ?? "";
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(_targetKey) || (_duelRoundsLeft <= 0 && _rewardRoundsLeft <= 0 && _loanRoundsLeft <= 0))
		{
			Clear();
			return false;
		}
		if (!string.Equals(_targetKey, text, StringComparison.Ordinal))
		{
			Clear();
			return false;
		}
		if (!IsShortAck(playerInput))
		{
			Clear();
			return false;
		}
		if (_duelRoundsLeft > 0)
		{
			duel = true;
			_duelRoundsLeft--;
		}
		if (_rewardRoundsLeft > 0)
		{
			reward = true;
			_rewardRoundsLeft--;
		}
		if (_loanRoundsLeft > 0)
		{
			loan = true;
			_loanRoundsLeft--;
		}
		if (!duel && !reward && !loan)
		{
			Clear();
			return false;
		}
		if (_duelRoundsLeft <= 0 && _rewardRoundsLeft <= 0 && _loanRoundsLeft <= 0)
		{
			Clear();
		}
		log = $"builtin_rule_sticky_consume target={text} duel={duel} reward={reward} loan={loan} left=({_duelRoundsLeft},{_rewardRoundsLeft},{_loanRoundsLeft})";
		return true;
	}

	/// <summary>Prime the carry from live semantic hits; no hit leaves the current carry untouched.</summary>
	internal bool Prime(string targetKey, bool duel, bool reward, bool loan, out string log)
	{
		log = null;
		string text = targetKey ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			Clear();
			return false;
		}
		if (!(duel || reward || loan))
		{
			return false;
		}
		_targetKey = text;
		_duelRoundsLeft = duel ? TurnLimit("duel") : 0;
		_rewardRoundsLeft = reward ? TurnLimit("reward") : 0;
		_loanRoundsLeft = loan ? TurnLimit("loan") : 0;
		log = $"builtin_rule_sticky_prime target={text} duel={_duelRoundsLeft} reward={_rewardRoundsLeft} loan={_loanRoundsLeft}";
		return true;
	}
}
