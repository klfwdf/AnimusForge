using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options.Gamepad;

public class OptionsGamepadKeyLocationWidget : Widget
{
	private bool _valuesInitialized;

	private string _actionText;

	private Widget _parentAreaWidget;

	private List<TextWidget> _keyNameTextWidgets = new List<TextWidget>();

	private InputKeyVisualWidget _keyVisualWidget;

	public bool ForceVisible { get; set; }

	public int KeyID { get; set; }

	public int NormalPositionXOffset { get; set; }

	public int NormalPositionYOffset { get; set; }

	public int NormalSizeXOfImage { get; private set; } = -1;

	public int NormalSizeYOfImage { get; private set; } = -1;

	public int CurrentSizeXOfImage { get; private set; } = -1;

	public int CurrentSizeYOfImage { get; private set; } = -1;

	public bool IsKeyToTheLeftOfTheGamepad { get; private set; }

	public OptionsGamepadKeyLocationWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_valuesInitialized)
		{
			NormalSizeXOfImage = base.ParentWidget.Sprite.Width;
			NormalSizeYOfImage = base.ParentWidget.Sprite.Height;
			CurrentSizeXOfImage = (int)(base.ParentWidget.SuggestedWidth * base._scaleToUse);
			CurrentSizeYOfImage = (int)(base.ParentWidget.SuggestedHeight * base._scaleToUse);
			_keyVisualWidget = null;
			_keyNameTextWidgets.Clear();
			List<Widget> allChildrenRecursive = GetAllChildrenRecursive();
			for (int i = 0; i < allChildrenRecursive.Count; i++)
			{
				if (allChildrenRecursive[i] is TextWidget item)
				{
					_keyNameTextWidgets.Add(item);
				}
				if (_keyVisualWidget == null && allChildrenRecursive[i] is InputKeyVisualWidget keyVisualWidget)
				{
					_keyVisualWidget = keyVisualWidget;
				}
			}
			_valuesInitialized = true;
			IsKeyToTheLeftOfTheGamepad = (float)NormalPositionXOffset < (float)NormalSizeXOfImage / 2f;
		}
		float num = base.ParentWidget.SuggestedWidth / (float)NormalSizeXOfImage;
		float num2 = base.ParentWidget.SuggestedHeight / (float)NormalSizeYOfImage;
		base.PositionXOffset = (float)NormalPositionXOffset * num;
		base.PositionYOffset = (float)NormalPositionYOffset * num2;
		List<TextWidget> keyNameTextWidgets = _keyNameTextWidgets;
		if (keyNameTextWidgets != null && keyNameTextWidgets.Count == 1)
		{
			_keyNameTextWidgets[0].Text = _actionText;
		}
		base.IsVisible = !string.IsNullOrEmpty(_actionText) || ForceVisible;
		if (!_valuesInitialized)
		{
			return;
		}
		if (IsKeyToTheLeftOfTheGamepad)
		{
			_keyNameTextWidgets.ForEach(delegate(TextWidget t)
			{
				t.ScaledSuggestedWidth = MathF.Abs(_parentAreaWidget.GlobalPosition.X - _keyVisualWidget.GlobalPosition.X);
				t.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Right;
			});
		}
		else
		{
			_keyNameTextWidgets.ForEach(delegate(TextWidget t)
			{
				t.ScaledSuggestedWidth = _parentAreaWidget.GlobalPosition.X + _parentAreaWidget.Size.X - (_keyVisualWidget.GlobalPosition.X + _keyVisualWidget.Size.X);
				t.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
			});
		}
	}

	internal void SetKeyProperties(string actionText, Widget parentAreaWidget)
	{
		_actionText = actionText;
		List<TextWidget> keyNameTextWidgets = _keyNameTextWidgets;
		if (keyNameTextWidgets != null && keyNameTextWidgets.Count == 1)
		{
			_keyNameTextWidgets[0].Text = _actionText;
		}
		_parentAreaWidget = parentAreaWidget;
		_valuesInitialized = false;
	}
}
