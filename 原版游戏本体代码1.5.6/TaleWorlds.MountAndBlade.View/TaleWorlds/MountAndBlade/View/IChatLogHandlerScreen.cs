using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.View;

public interface IChatLogHandlerScreen
{
	void TryUpdateChatLogLayerParameters(ref bool isTeamChatAvailable, ref bool inputEnabled, ref bool isToggleChatHintAvailable, ref bool isMouseVisible, ref InputContext inputContext);
}
