using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tournament;

public class TournamentMatchWidget : Widget
{
	private int _state;

	[Editor(false)]
	public int State
	{
		get
		{
			return _state;
		}
		set
		{
			if (_state == value)
			{
				return;
			}
			_state = value;
			List<Widget> allChildrenRecursive = GetAllChildrenRecursive();
			for (int i = 0; i < allChildrenRecursive.Count; i++)
			{
				if (allChildrenRecursive[i] is TournamentParticipantBrushWidget tournamentParticipantBrushWidget)
				{
					tournamentParticipantBrushWidget.MatchState = State;
				}
			}
		}
	}

	public TournamentMatchWidget(UIContext context)
		: base(context)
	{
	}
}
