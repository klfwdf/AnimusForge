using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class MultiplayerTeamStatsSideRowWidget : ButtonWidget
{
	private bool _isFollowed;

	[Editor(false)]
	public bool IsFollowed
	{
		get
		{
			return _isFollowed;
		}
		set
		{
			if (_isFollowed != value)
			{
				_isFollowed = value;
				OnPropertyChanged(value, "IsFollowed");
				base.IsSelected = value;
			}
		}
	}

	public MultiplayerTeamStatsSideRowWidget(UIContext context)
		: base(context)
	{
	}
}
