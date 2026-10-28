using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Clan;

public class MultiplayerLobbyClanMemberRankVisualBrushWidget(UIContext context) : BrushWidget(context)
{
	private int _type = -1;

	[Editor(false)]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				_type = value;
				OnPropertyChanged(value, "Type");
				UpdateTypeVisual();
			}
		}
	}

	private void UpdateTypeVisual()
	{
		if (Type == 0)
		{
			SetState("Member");
		}
		else if (Type == 1)
		{
			SetState("Officer");
		}
		else if (Type == 2)
		{
			SetState("Leader");
		}
		else
		{
			Debug.FailedAssert("This member type is not defined in widget", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Multiplayer\\Lobby\\Clan\\MultiplayerLobbyClanMemberRankVisualBrushWidget.cs", "UpdateTypeVisual", 28);
		}
	}
}
