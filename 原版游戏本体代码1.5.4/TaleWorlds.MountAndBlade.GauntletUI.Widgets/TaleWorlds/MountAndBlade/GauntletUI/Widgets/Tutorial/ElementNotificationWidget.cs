using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class ElementNotificationWidget : Widget
{
	private bool _doesNotHaveElement;

	private bool _shouldSyncSize;

	private string _elementID = string.Empty;

	private Widget _elementToHighlight;

	private TutorialHighlightItemBrushWidget _tutorialFrameWidget;

	[Editor(false)]
	public string ElementID
	{
		get
		{
			return _elementID;
		}
		set
		{
			if (_elementID != value)
			{
				if (_elementID != string.Empty && value == string.Empty)
				{
					ResetHighlight();
				}
				_elementID = value;
				OnPropertyChanged(value, "ElementID");
			}
		}
	}

	[Editor(false)]
	public Widget ElementToHighlight
	{
		get
		{
			return _elementToHighlight;
		}
		set
		{
			if (_elementToHighlight != value)
			{
				_elementToHighlight = value;
				OnPropertyChanged(value, "ElementToHighlight");
			}
		}
	}

	[Editor(false)]
	public TutorialHighlightItemBrushWidget TutorialFrameWidget
	{
		get
		{
			return _tutorialFrameWidget;
		}
		set
		{
			if (_tutorialFrameWidget != value)
			{
				_tutorialFrameWidget = value;
				OnPropertyChanged(value, "TutorialFrameWidget");
				if (_tutorialFrameWidget != null)
				{
					_tutorialFrameWidget.IsVisible = false;
				}
			}
		}
	}

	public ElementNotificationWidget(UIContext context)
		: base(context)
	{
		base.IsVisible = false;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		string elementID = ElementID;
		if (elementID != null && elementID.Any() && ElementToHighlight == null && !_doesNotHaveElement)
		{
			ElementToHighlight = FindElementWithID(base.EventManager.Root, ElementID);
			_doesNotHaveElement = true;
			if (ElementToHighlight != null)
			{
				TutorialFrameWidget.IsVisible = true;
				TutorialFrameWidget.IsHighlightEnabled = true;
				TutorialFrameWidget.ParentWidget = ElementToHighlight;
				if (ElementToHighlight.HeightSizePolicy == SizePolicy.CoverChildren || ElementToHighlight.WidthSizePolicy == SizePolicy.CoverChildren)
				{
					TutorialFrameWidget.WidthSizePolicy = SizePolicy.Fixed;
					TutorialFrameWidget.HeightSizePolicy = SizePolicy.Fixed;
					_shouldSyncSize = true;
				}
				else
				{
					TutorialFrameWidget.WidthSizePolicy = SizePolicy.StretchToParent;
					TutorialFrameWidget.HeightSizePolicy = SizePolicy.StretchToParent;
					_shouldSyncSize = false;
				}
			}
		}
		if (_shouldSyncSize && ElementToHighlight != null && ElementToHighlight.Size.X > 1f && ElementToHighlight.Size.Y > 1f)
		{
			base.ScaledSuggestedWidth = ElementToHighlight.Size.X - 1f;
			base.ScaledSuggestedHeight = ElementToHighlight.Size.Y - 1f;
		}
	}

	private Widget FindElementWithID(Widget current, string ID)
	{
		if (current != null)
		{
			for (int i = 0; i < current.ChildCount; i++)
			{
				if (current.GetChild(i).Id == ID)
				{
					return current.GetChild(i);
				}
				Widget widget = FindElementWithID(current.GetChild(i), ID);
				if (widget != null)
				{
					return widget;
				}
			}
		}
		return null;
	}

	private void ResetHighlight()
	{
		if (TutorialFrameWidget != null)
		{
			TutorialFrameWidget.ParentWidget = this;
			_doesNotHaveElement = false;
			TutorialFrameWidget.IsVisible = false;
			TutorialFrameWidget.IsHighlightEnabled = false;
			ElementToHighlight = null;
		}
	}
}
