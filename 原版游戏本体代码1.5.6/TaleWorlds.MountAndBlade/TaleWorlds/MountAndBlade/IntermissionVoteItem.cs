namespace TaleWorlds.MountAndBlade;

public class IntermissionVoteItem
{
	public readonly string Id;

	public readonly int Index;

	public int VoteCount { get; private set; }

	public IntermissionVoteItem(string id, int index)
	{
		Id = id;
		Index = index;
		VoteCount = 0;
	}

	public void SetVoteCount(int voteCount)
	{
		VoteCount = voteCount;
	}

	public void IncreaseVoteCount(int incrementAmount)
	{
		VoteCount += incrementAmount;
	}
}
