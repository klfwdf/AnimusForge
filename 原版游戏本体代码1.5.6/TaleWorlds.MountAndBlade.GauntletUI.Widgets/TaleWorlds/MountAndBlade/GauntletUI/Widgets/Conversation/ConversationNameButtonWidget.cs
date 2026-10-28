using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Conversation;

public class ConversationNameButtonWidget : ButtonWidget
{
	private bool _isRelationEnabled;

	private Widget _relationBarContainer;

	[Editor(false)]
	public bool IsRelationEnabled
	{
		get
		{
			return _isRelationEnabled;
		}
		set
		{
			if (value != _isRelationEnabled)
			{
				_isRelationEnabled = value;
				OnPropertyChanged(value, "IsRelationEnabled");
			}
		}
	}

	[Editor(false)]
	public Widget RelationBarContainer
	{
		get
		{
			return _relationBarContainer;
		}
		set
		{
			if (value != _relationBarContainer)
			{
				_relationBarContainer = value;
				OnPropertyChanged(value, "RelationBarContainer");
			}
		}
	}

	public ConversationNameButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
		RelationBarContainer.IsVisible = IsRelationEnabled;
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
		RelationBarContainer.IsVisible = false;
	}
}
