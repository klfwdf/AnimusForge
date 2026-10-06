using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyFormationDropdownWidget : DropdownWidget
{
	private DelayedStateChanger _seperatorStateChanger;

	private DelayedStateChanger _listStateChanger;

	[Editor(false)]
	public DelayedStateChanger SeperatorStateChanger
	{
		get
		{
			return _seperatorStateChanger;
		}
		set
		{
			if (_seperatorStateChanger != value)
			{
				_seperatorStateChanger = value;
				OnPropertyChanged(value, "SeperatorStateChanger");
				SeperatorStateChangerUpdated();
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger ListStateChanger
	{
		get
		{
			return _listStateChanger;
		}
		set
		{
			if (_listStateChanger != value)
			{
				_listStateChanger = value;
				OnPropertyChanged(value, "ListStateChanger");
				ListStateChangerUpdated();
			}
		}
	}

	public PartyFormationDropdownWidget(UIContext context)
		: base(context)
	{
		base.DoNotHandleDropdownListPanel = true;
	}

	private void ListStateChangerUpdated()
	{
	}

	private void SeperatorStateChangerUpdated()
	{
	}

	protected override void OpenPanel()
	{
		base.ListPanel.IsVisible = true;
		SeperatorStateChanger.IsVisible = true;
		ListStateChanger.Delay = SeperatorStateChanger.VisualDefinition.TransitionDuration;
		ListStateChanger.State = "Opened";
		ListStateChanger.Start();
		SeperatorStateChanger.Delay = 0f;
		SeperatorStateChanger.State = "Opened";
		SeperatorStateChanger.Start();
		base.Context.TwoDimensionContext.PlaySound("dropdown");
	}

	protected override void ClosePanel()
	{
		ListStateChanger.Delay = 0f;
		ListStateChanger.State = "Closed";
		ListStateChanger.Start();
		SeperatorStateChanger.Delay = ListStateChanger.TargetWidget.VisualDefinition.TransitionDuration;
		SeperatorStateChanger.State = "Closed";
		SeperatorStateChanger.Start();
		base.Context.TwoDimensionContext.PlaySound("dropdown");
	}
}
