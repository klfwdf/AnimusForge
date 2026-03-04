using System.Collections.Generic;

namespace AnimusForge;

public class GuardrailConfigModel
{
	public string GlobalPrompt { get; set; } = "你是一个NPC。";

	public string GlobalGuardrail { get; set; } = "";

	public DuelConfig Duel { get; set; } = new DuelConfig();

	public RewardConfig Reward { get; set; } = new RewardConfig();

	public LoanConfig Loan { get; set; } = new LoanConfig();

	public SurroundingsConfig Surroundings { get; set; } = new SurroundingsConfig();

	public List<GuardrailRulePromptConfig> RulePrompts { get; set; } = new List<GuardrailRulePromptConfig>();

	public DuelStakeConfig DuelStake { get; set; } = new DuelStakeConfig();

	public KnowledgeRetrievalConfig KnowledgeRetrieval { get; set; } = new KnowledgeRetrievalConfig();
}
