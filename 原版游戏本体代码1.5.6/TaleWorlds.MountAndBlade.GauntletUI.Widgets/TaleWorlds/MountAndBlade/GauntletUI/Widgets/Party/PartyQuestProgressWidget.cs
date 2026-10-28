using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyQuestProgressWidget : Widget
{
	private int _itemCount;

	private ListPanel _dividerContainer;

	private Brush _dividerBrush;

	[Editor(false)]
	public int ItemCount
	{
		get
		{
			return _itemCount;
		}
		set
		{
			if (_itemCount != value)
			{
				_itemCount = value;
				OnPropertyChanged(value, "ItemCount");
				UpdateDividers();
			}
		}
	}

	[Editor(false)]
	public ListPanel DividerContainer
	{
		get
		{
			return _dividerContainer;
		}
		set
		{
			if (_dividerContainer != value)
			{
				_dividerContainer = value;
				OnPropertyChanged(value, "DividerContainer");
			}
		}
	}

	[Editor(false)]
	public Brush DividerBrush
	{
		get
		{
			return _dividerBrush;
		}
		set
		{
			if (_dividerBrush != value)
			{
				_dividerBrush = value;
				OnPropertyChanged(value, "DividerBrush");
			}
		}
	}

	public PartyQuestProgressWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateDividers()
	{
		if (DividerContainer == null || DividerBrush == null)
		{
			return;
		}
		int itemCount = ItemCount;
		if (DividerContainer.ChildCount > itemCount)
		{
			int num = DividerContainer.ChildCount - itemCount;
			for (int i = 0; i < num; i++)
			{
				DividerContainer.RemoveChild(DividerContainer.GetChild(i));
			}
		}
		else if (itemCount > DividerContainer.ChildCount)
		{
			int num2 = itemCount - DividerContainer.ChildCount;
			for (int j = 0; j < num2; j++)
			{
				DividerContainer.AddChild(CreateDivider());
			}
		}
		UpdateDividerPositions();
	}

	private Widget CreateDivider()
	{
		Widget obj = new Widget(base.Context)
		{
			WidthSizePolicy = SizePolicy.StretchToParent,
			HeightSizePolicy = SizePolicy.StretchToParent
		};
		BrushWidget brushWidget = new BrushWidget(base.Context);
		brushWidget.WidthSizePolicy = SizePolicy.Fixed;
		brushWidget.HeightSizePolicy = SizePolicy.Fixed;
		brushWidget.Brush = DividerBrush;
		brushWidget.SuggestedWidth = brushWidget.ReadOnlyBrush.Sprite.Width;
		brushWidget.SuggestedHeight = brushWidget.ReadOnlyBrush.Sprite.Height;
		brushWidget.HorizontalAlignment = HorizontalAlignment.Right;
		brushWidget.VerticalAlignment = VerticalAlignment.Center;
		brushWidget.PositionXOffset = (float)brushWidget.ReadOnlyBrush.Sprite.Width * 0.5f;
		obj.AddChild(brushWidget);
		return obj;
	}

	private void UpdateDividerPositions()
	{
		int childCount = DividerContainer.ChildCount;
		float num = DividerContainer.Size.X / (float)(childCount + 1);
		for (int i = 0; i < childCount; i++)
		{
			Widget child = DividerContainer.GetChild(i);
			child.PositionXOffset = (float)i * num - child.Size.X / 2f;
		}
	}
}
