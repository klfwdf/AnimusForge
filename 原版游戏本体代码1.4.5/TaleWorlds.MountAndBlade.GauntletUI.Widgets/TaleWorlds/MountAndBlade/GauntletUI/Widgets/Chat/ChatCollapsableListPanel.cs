using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Chat;

public class ChatCollapsableListPanel : ListPanel
{
	private float _alpha;

	private Color _lineColor;

	private ChatLogWidget _parentChatLogWidget;

	public bool IsLinesVisible { get; private set; }

	[DataSourceProperty]
	public float Alpha
	{
		get
		{
			return _alpha;
		}
		set
		{
			if (value != _alpha)
			{
				_alpha = value;
				OnPropertyChanged(value, "Alpha");
				RefreshAlphaValues(value);
			}
		}
	}

	[DataSourceProperty]
	public Color LineColor
	{
		get
		{
			return _lineColor;
		}
		set
		{
			if (value != _lineColor)
			{
				_lineColor = value;
				OnPropertyChanged(value, "LineColor");
				RefreshColorValues(value);
			}
		}
	}

	[DataSourceProperty]
	public ChatLogWidget ParentChatLogWidget
	{
		get
		{
			return _parentChatLogWidget;
		}
		set
		{
			if (value != _parentChatLogWidget)
			{
				_parentChatLogWidget = value;
				OnPropertyChanged(value, "ParentChatLogWidget");
			}
		}
	}

	public ChatCollapsableListPanel(UIContext context)
		: base(context)
	{
		RefreshAlphaValues(Alpha);
	}

	private void ToggleLines(bool isVisible)
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			GetChild(i).IsVisible = i == 0 || isVisible;
		}
		IsLinesVisible = isVisible;
	}

	protected override void OnMousePressed()
	{
		base.OnMousePressed();
		ToggleLines(!IsLinesVisible);
	}

	protected override bool OnPreviewMousePressed()
	{
		return base.OnPreviewMousePressed();
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		RefreshAlphaValues(Alpha);
		ToggleLines(isVisible: true);
	}

	private void RefreshAlphaValues(float newAlpha)
	{
		this.SetGlobalAlphaRecursively(newAlpha);
		if (newAlpha > 0f)
		{
			ParentChatLogWidget?.RegisterMultiLineElement(this);
		}
		else
		{
			ParentChatLogWidget?.RemoveMultiLineElement(this);
		}
	}

	private void UpdateColorValuesOfChildren(Widget widget, Color newColor)
	{
		foreach (Widget child in widget.Children)
		{
			if (child is BrushWidget brushWidget)
			{
				brushWidget.Brush.FontColor = newColor;
			}
			else
			{
				child.Color = newColor;
			}
			UpdateColorValuesOfChildren(child, newColor);
		}
	}

	private void RefreshColorValues(Color newColor)
	{
		UpdateColorValuesOfChildren(this, newColor);
	}
}
