namespace AnimusForge;

public class GuardrailRuleHit
{
	public string RuleId { get; set; } = "";

	public string Group { get; set; } = "";

	public int Priority { get; set; } = 0;

	public float Score { get; set; } = 0f;

	public string MatchedSeed { get; set; } = "";

	public string Instruction { get; set; } = "";
}
