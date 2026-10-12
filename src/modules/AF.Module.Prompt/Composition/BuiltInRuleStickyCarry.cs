using System.Linq;
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
    private static readonly Lazy<string[]> ShortAckTokens = new Lazy<string[]>(() =>
        PromptComposerTexts.Raw("StickyCarry_AffirmativeTokens_Cn").Split(','));
    private static readonly string[] EnglishShortAckTokens =
    {
        "yes", "ok", "okay", "sure", "agreed", "i agree", "confirmed", "confirm",
        "go ahead", "continue", "do it", "that is fine", "mercenary", "vassal",
        "i choose mercenary", "i choose vassal"
    };
    private static readonly Lazy<string[]> NegativeAckTokens = new Lazy<string[]>(() => PromptComposerTexts.Raw("StickyCarry_NegativeTokens_Cn").Split(',').Concat(new[] { "no", "not", "never", "cancel", "stop", "refuse", "decline", "don't", "do not", "disagree", "disagreed", "won't", "would not", "cannot", "can't", "reject", "rejected" }).ToArray());
    private static bool ContainsEnglishToken(string text, string token)
    {
        int offset = 0;
        while ((offset = text.IndexOf(token, offset, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int end = offset + token.Length;
            if ((offset == 0 || !char.IsLetterOrDigit(text[offset - 1]))
                && (end == text.Length || !char.IsLetterOrDigit(text[end]))) return true;
            offset = end;
        }
        return false;
    }
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
		if (string.IsNullOrWhiteSpace(text) || text.Length > 20 || text.IndexOf('?') >= 0 || text.IndexOf('？') >= 0)
		{
			return false;
		}
if (PromptComposerTexts.Raw("StickyCarry_ConditionPrefixes_Cn").Split(',').Any(cp => text.StartsWith(cp, StringComparison.Ordinal))
			|| text.StartsWith("if ", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("what if", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("suppose", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
        foreach (string negative in NegativeAckTokens.Value)
        {
            bool isEnglish = negative[0] <= 127;
            if (isEnglish ? ContainsEnglishToken(text, negative) : text.IndexOf(negative, StringComparison.Ordinal) >= 0)
                return false;
        }
        string acknowledgementText = text.TrimEnd('.', '!', ',', ' ', '。', '！', '，');
        foreach (string acknowledgement in EnglishShortAckTokens)
            if (string.Equals(acknowledgementText, acknowledgement, StringComparison.OrdinalIgnoreCase)) return true;
		for (int i = 0; i < ShortAckTokens.Value.Length; i++)
		{
			if (string.Equals(acknowledgementText, ShortAckTokens.Value[i], StringComparison.Ordinal))
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
