namespace Voxforge;

public class AIConfigModel
{
	public string GlobalSystemPrompt { get; set; } = "你是一个NPC。";

	public DuelConfig DuelSettings { get; set; } = new DuelConfig();

	public RewardConfig RewardSettings { get; set; } = new RewardConfig();
}
