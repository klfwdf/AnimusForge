using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class GuardrailRuleEval
{
	public string RuleTag;

	public string MatchedSeed;

	public string MatchedIntent;

	public float RawInput;

	public float RawContext;

	public float MixedRaw;

	public float AmpScore;

	public float RerankScore;

	public float Delta;

	public float Mean;

	public float MaxOther;

	public string MaxOtherTag;

	public int Rank;

	public bool Candidate;

	public bool AbsHit;

	public bool RelHit;

	public bool HighAmpHit;

	public bool ForceHit;

	public float TopGap;

	public float IntentEvidence;

	public float IntentGate;

	public string IntentSeed;

	public bool LexicalAnchor;

	public string RejectReason;

	public string MatchMode;

	public bool Hit;
}

internal sealed class GuardrailEvalSnapshot
{
	public string Key;

	public string MatchMode = "none";

	public int IntentCount;

	public int RecallPerIntent;

	public int RerankPerIntent;

	public int ReturnCap;

	public MentionedWorldEntities MentionedEntities = new MentionedWorldEntities();

	public Dictionary<string, GuardrailRuleEval> Rules = new Dictionary<string, GuardrailRuleEval>(StringComparer.OrdinalIgnoreCase);
	internal List<GuardrailRuleEval> OrderedRules = new List<GuardrailRuleEval>();
}
