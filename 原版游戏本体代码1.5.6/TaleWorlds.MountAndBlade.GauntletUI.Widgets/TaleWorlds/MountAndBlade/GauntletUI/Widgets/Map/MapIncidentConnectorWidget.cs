using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map;

public class MapIncidentConnectorWidget : Widget
{
	private Widget _previousActiveOption;

	private float _optionsLineTop;

	private bool _isLerpingOptionsLine;

	public Widget OptionsList { get; set; }

	public Widget OptionsPanel { get; set; }

	public Widget OptionsClipRect { get; set; }

	public Widget ConsequencePanel { get; set; }

	public Widget OptionsLineClipRect { get; set; }

	public BrushWidget OptionsLine { get; set; }

	public BrushWidget VerticalLine { get; set; }

	public BrushWidget ConsequencesLine { get; set; }

	public float LineThickness { get; set; } = 3f;

	public float LineAnimationSpeed { get; set; } = 15f;

	public float LineMargin { get; set; } = 2f;

	public MapIncidentConnectorWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (OptionsList == null || OptionsPanel == null || OptionsClipRect == null || ConsequencePanel == null || OptionsLineClipRect == null || OptionsLine == null || VerticalLine == null || ConsequencesLine == null)
		{
			Debug.FailedAssert("A required widget is null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Map\\MapIncidentConnectorWidget.cs", "OnUpdate", 35);
			return;
		}
		Widget widget = (base.IsVisible ? GetActiveOption() : null);
		if (widget == null)
		{
			_previousActiveOption = null;
			_isLerpingOptionsLine = false;
			return;
		}
		if (_previousActiveOption != widget)
		{
			_isLerpingOptionsLine = _previousActiveOption != null;
			_previousActiveOption = widget;
		}
		OptionsLine.SetState(widget.CurrentState);
		VerticalLine.SetState(widget.CurrentState);
		ConsequencesLine.SetState(widget.CurrentState);
		OptionsLine.SuggestedHeight = LineThickness;
		VerticalLine.SuggestedWidth = LineThickness;
		ConsequencesLine.SuggestedHeight = LineThickness;
		SimpleRectangle boundingBox = AreaRect.GetBoundingBox();
		SimpleRectangle boundingBox2 = OptionsClipRect.AreaRect.GetBoundingBox();
		SimpleRectangle boundingBox3 = ConsequencePanel.AreaRect.GetBoundingBox();
		float num = OptionsPanel.AreaRect.GetBoundingBox().X2 - boundingBox.X;
		float num2 = boundingBox2.Y - boundingBox.Y;
		float num3 = boundingBox2.Y2 - boundingBox.Y;
		float num4 = boundingBox3.X - boundingBox.X;
		float num5 = (num + num4 - VerticalLine.ScaledSuggestedWidth) * 0.5f;
		float num6 = num5 + VerticalLine.ScaledSuggestedWidth;
		float num7 = boundingBox3.GetCenter().Y - boundingBox.Y - ConsequencesLine.ScaledSuggestedHeight * 0.5f;
		ConsequencesLine.ScaledSuggestedWidth = num4 - num5 - LineMargin * base._scaleToUse;
		ConsequencesLine.ScaledPositionXOffset = num5;
		ConsequencesLine.ScaledPositionYOffset = num7;
		OptionsLineClipRect.ScaledSuggestedWidth = num4 - num;
		OptionsLineClipRect.ScaledSuggestedHeight = num3 - num2;
		OptionsLineClipRect.ScaledPositionXOffset = num;
		OptionsLineClipRect.ScaledPositionYOffset = num2;
		float num8 = widget.AreaRect.GetCenter().Y - boundingBox.Y - OptionsLine.ScaledSuggestedHeight * 0.5f;
		if (MathF.Abs(num8 - num7) <= 2f * LineThickness)
		{
			num8 = num7;
		}
		if (_isLerpingOptionsLine)
		{
			_optionsLineTop = MathF.Lerp(_optionsLineTop, num8, MathF.Min(LineAnimationSpeed * dt, 1f), 0.01f);
			_isLerpingOptionsLine = _optionsLineTop != num8;
		}
		else
		{
			_optionsLineTop = num8;
		}
		OptionsLine.ScaledSuggestedWidth = num6 - num - LineMargin * base._scaleToUse;
		OptionsLine.ScaledPositionXOffset = LineMargin * base._scaleToUse;
		OptionsLine.ScaledPositionYOffset = _optionsLineTop - num2;
		float num9 = MathF.Min(MathF.Clamp(_optionsLineTop, num2, num3), num7);
		float num10 = MathF.Max(MathF.Clamp(_optionsLineTop + OptionsLine.ScaledSuggestedHeight, num2, num3), num7 + ConsequencesLine.ScaledSuggestedHeight);
		VerticalLine.ScaledSuggestedHeight = num10 - num9;
		VerticalLine.ScaledPositionXOffset = num5;
		VerticalLine.ScaledPositionYOffset = num9;
	}

	private Widget GetActiveOption()
	{
		Widget result = null;
		for (int i = 0; i < OptionsList.ChildCount; i++)
		{
			Widget child = OptionsList.GetChild(i);
			if (child.IsHovered)
			{
				return child;
			}
			if (child is ButtonWidget { IsSelected: not false })
			{
				result = child;
			}
		}
		return result;
	}
}
