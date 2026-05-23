using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options.Gamepad;

public class OptionsGamepadVisualWidget : Widget
{
	private List<OptionsGamepadKeyLocationWidget> _allKeyLocations = new List<OptionsGamepadKeyLocationWidget>();

	private List<OptionsGamepadOptionItemListPanel> _allChildKeyItems = new List<OptionsGamepadOptionItemListPanel>();

	private bool _initalized;

	private bool _isKeysDirty;

	public Widget ParentAreaWidget { get; set; }

	private float _verticalMarginBetweenKeys => 20f;

	public OptionsGamepadVisualWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initalized)
		{
			foreach (Widget child in base.ParentWidget.Children)
			{
				if (child is OptionsGamepadKeyLocationWidget item)
				{
					_allKeyLocations.Add(item);
				}
			}
			_initalized = true;
		}
		if (!_isKeysDirty)
		{
			return;
		}
		_allKeyLocations.ForEach(delegate(OptionsGamepadKeyLocationWidget k)
		{
			k.SetKeyProperties(string.Empty, ParentAreaWidget);
		});
		foreach (Widget child2 in base.Children)
		{
			OptionsGamepadOptionItemListPanel optionItem;
			if ((optionItem = child2 as OptionsGamepadOptionItemListPanel) != null)
			{
				OptionsGamepadKeyLocationWidget optionsGamepadKeyLocationWidget = _allKeyLocations.Find((OptionsGamepadKeyLocationWidget l) => l.KeyID == optionItem.KeyId);
				if (optionsGamepadKeyLocationWidget != null)
				{
					optionItem.SetKeyProperties(optionsGamepadKeyLocationWidget, ParentAreaWidget);
				}
				else
				{
					optionItem.IsVisible = false;
				}
			}
		}
		_isKeysDirty = false;
	}

	private void OnActionTextChanged()
	{
		_isKeysDirty = true;
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		_isKeysDirty = true;
		if (child is OptionsGamepadOptionItemListPanel optionsGamepadOptionItemListPanel && !_allChildKeyItems.Contains(optionsGamepadOptionItemListPanel))
		{
			_allChildKeyItems.Add(optionsGamepadOptionItemListPanel);
			optionsGamepadOptionItemListPanel.OnActionTextChanged += OnActionTextChanged;
		}
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		base.OnBeforeChildRemoved(child);
		_isKeysDirty = true;
		if (child is OptionsGamepadOptionItemListPanel optionsGamepadOptionItemListPanel && _allChildKeyItems.Contains(optionsGamepadOptionItemListPanel))
		{
			_allChildKeyItems.Remove(optionsGamepadOptionItemListPanel);
			optionsGamepadOptionItemListPanel.OnActionTextChanged -= OnActionTextChanged;
		}
	}
}
