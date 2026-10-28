using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerPollProgressionWidget : Widget
{
	private const string _activeState = "Active";

	private const string _inactiveState = "Inactive";

	private bool _hasOngoingPoll;

	private ListPanel _pollExtension;

	public bool HasOngoingPoll
	{
		get
		{
			return _hasOngoingPoll;
		}
		set
		{
			if (value != _hasOngoingPoll)
			{
				_hasOngoingPoll = value;
				OnPropertyChanged(value, "HasOngoingPoll");
				PollExtension?.SetState(value ? "Active" : "Inactive");
			}
		}
	}

	[Editor(false)]
	public ListPanel PollExtension
	{
		get
		{
			return _pollExtension;
		}
		set
		{
			if (value != _pollExtension)
			{
				_pollExtension = value;
				OnPropertyChanged(value, "PollExtension");
				_pollExtension.SetState("Inactive");
			}
		}
	}

	public MultiplayerPollProgressionWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
	}
}
