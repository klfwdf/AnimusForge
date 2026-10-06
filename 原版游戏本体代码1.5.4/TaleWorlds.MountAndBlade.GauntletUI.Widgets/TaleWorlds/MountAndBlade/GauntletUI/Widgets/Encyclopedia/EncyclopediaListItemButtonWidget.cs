using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Encyclopedia;

public class EncyclopediaListItemButtonWidget : ButtonWidget
{
	private string _listItemId;

	public TextWidget ListItemNameTextWidget { get; set; }

	public TextWidget ListComparedValueTextWidget { get; set; }

	public Brush InfoAvailableItemNameBrush { get; set; }

	public Brush InfoUnvailableItemNameBrush { get; set; }

	public bool IsInfoAvailable { get; set; }

	[Editor(false)]
	public string ListItemId
	{
		get
		{
			return _listItemId;
		}
		set
		{
			if (_listItemId != value)
			{
				_listItemId = value;
				OnPropertyChanged(value, "ListItemId");
			}
		}
	}

	public EncyclopediaListItemButtonWidget(UIContext context)
		: base(context)
	{
		base.EventManager.AddLateUpdateAction(this, OnThisLateUpdate, 1);
	}

	public void OnThisLateUpdate(float dt)
	{
		ListItemNameTextWidget.Brush = (IsInfoAvailable ? InfoAvailableItemNameBrush : InfoUnvailableItemNameBrush);
		ListComparedValueTextWidget.Brush = (IsInfoAvailable ? InfoAvailableItemNameBrush : InfoUnvailableItemNameBrush);
	}
}
