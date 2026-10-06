using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation.Persuasion;

public class PersuasionTask
{
	public readonly MBList<PersuasionOptionArgs> Options;

	public TextObject SpokenLine;

	public TextObject ImmediateFailLine;

	public TextObject FinalFailLine;

	public TextObject TryLaterLine;

	public readonly int ReservationType;

	public PersuasionTask(int reservationType)
	{
		Options = new MBList<PersuasionOptionArgs>();
		ReservationType = reservationType;
	}

	public void AddOptionToTask(PersuasionOptionArgs option)
	{
		Options.Add(option);
	}

	public void BlockAllOptions()
	{
		foreach (PersuasionOptionArgs option in Options)
		{
			option.BlockTheOption(isBlocked: true);
		}
	}

	public void UnblockAllOptions()
	{
		foreach (PersuasionOptionArgs option in Options)
		{
			option.BlockTheOption(isBlocked: false);
		}
	}

	public void ApplyEffects(float moveToNextStageChance, float blockRandomOptionChance)
	{
		if (moveToNextStageChance > MBRandom.RandomFloat)
		{
			BlockAllOptions();
		}
		else if (blockRandomOptionChance > MBRandom.RandomFloat)
		{
			Options.GetRandomElementWithPredicate((PersuasionOptionArgs x) => !x.IsBlocked)?.BlockTheOption(isBlocked: true);
		}
	}
}
