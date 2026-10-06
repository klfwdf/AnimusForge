using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class PollHotkeyCategory : GameKeyContext
{
	public const string CategoryId = "PollHotkeyCategory";

	public const int AcceptPoll = 108;

	public const int DeclinePoll = 109;

	public PollHotkeyCategory()
		: base("PollHotkeyCategory", 116)
	{
		RegisterGameKeys();
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(108, "AcceptPoll", "PollHotkeyCategory", InputKey.F10, InputKey.ControllerLBumper, GameKeyMainCategories.PollCategory));
		RegisterGameKey(new GameKey(109, "DeclinePoll", "PollHotkeyCategory", InputKey.F11, InputKey.ControllerRBumper, GameKeyMainCategories.PollCategory));
	}
}
