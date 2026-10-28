using TaleWorlds.MountAndBlade.Diamond;

public interface IChatHandler
{
	void ReceiveChatMessage(ChatChannelType channel, string sender, string message);
}
