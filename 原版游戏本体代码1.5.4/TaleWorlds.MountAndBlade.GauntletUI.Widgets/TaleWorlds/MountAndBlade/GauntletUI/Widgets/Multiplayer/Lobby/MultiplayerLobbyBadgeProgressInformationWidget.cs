using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyBadgeProgressInformationWidget : Widget
{
	private int _shownBadgeCount;

	private ListPanel _activeBadgesList;

	public float CenterBadgeSize { get; set; } = 200f;

	public float OuterBadgeBaseSize { get; set; } = 175f;

	public float SizeDecayFromCenterPerElement { get; set; } = 25f;

	[Editor(false)]
	public int ShownBadgeCount
	{
		get
		{
			return _shownBadgeCount;
		}
		set
		{
			if (value != _shownBadgeCount)
			{
				_shownBadgeCount = value;
				OnPropertyChanged(value, "ShownBadgeCount");
			}
		}
	}

	[Editor(false)]
	public ListPanel ActiveBadgesList
	{
		get
		{
			return _activeBadgesList;
		}
		set
		{
			if (value != _activeBadgesList)
			{
				_activeBadgesList = value;
				OnPropertyChanged(value, "ActiveBadgesList");
			}
		}
	}

	public MultiplayerLobbyBadgeProgressInformationWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (ActiveBadgesList != null)
		{
			ArrangeChildrenSizes();
		}
	}

	private void ArrangeChildrenSizes()
	{
		ActiveBadgesList.IsVisible = ShownBadgeCount > 0;
		int centerIndex = ShownBadgeCount / 2;
		int currentIndex = 0;
		ActiveBadgesList.ApplyActionToAllChildrenRecursive(delegate(Widget widget)
		{
			if (widget is MultiplayerPlayerBadgeVisualWidget multiplayerPlayerBadgeVisualWidget)
			{
				float num = CenterBadgeSize;
				if (currentIndex != centerIndex)
				{
					float num2 = MathF.Abs(currentIndex - centerIndex);
					num = OuterBadgeBaseSize - SizeDecayFromCenterPerElement * num2;
				}
				multiplayerPlayerBadgeVisualWidget.SetForcedSize(num, num);
				currentIndex++;
			}
		});
	}
}
