using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class PropertyBasedTooltipVM : TooltipBaseVM
{
	public enum TooltipMode
	{
		DefaultGame,
		DefaultCampaign,
		Ally,
		Enemy,
		War
	}

	private static Dictionary<string, Func<string>> _keyTextGetters = new Dictionary<string, Func<string>>();

	private MBBindingList<TooltipProperty> _tooltipPropertyList;

	private int _mode;

	[DataSourceProperty]
	public MBBindingList<TooltipProperty> TooltipPropertyList
	{
		get
		{
			return _tooltipPropertyList;
		}
		set
		{
			if (value != _tooltipPropertyList)
			{
				_tooltipPropertyList = value;
				OnPropertyChangedWithValue(value, "TooltipPropertyList");
			}
		}
	}

	[DataSourceProperty]
	public int Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			if (value != _mode)
			{
				_mode = value;
				OnPropertyChangedWithValue(value, "Mode");
			}
		}
	}

	public PropertyBasedTooltipVM(Type invokedType, object[] invokedArgs)
		: base(invokedType, invokedArgs)
	{
		TooltipPropertyList = new MBBindingList<TooltipProperty>();
		_isPeriodicRefreshEnabled = true;
		_periodicRefreshDelay = 2f;
		Refresh();
	}

	protected override void OnFinalizeInternal()
	{
		base.IsActive = false;
		TooltipPropertyList.Clear();
	}

	public static void AddKeyType(string keyID, Func<string> getKeyText)
	{
		_keyTextGetters.Add(keyID, getKeyText);
	}

	public string GetKeyText(string keyID)
	{
		if (_keyTextGetters.ContainsKey(keyID))
		{
			return _keyTextGetters[keyID]();
		}
		return "";
	}

	protected override void OnPeriodicRefresh()
	{
		base.OnPeriodicRefresh();
		foreach (TooltipProperty tooltipProperty in TooltipPropertyList)
		{
			tooltipProperty.RefreshDefinition();
			tooltipProperty.RefreshValue();
		}
	}

	protected override void OnIsExtendedChanged()
	{
		if (base.IsActive)
		{
			base.IsActive = false;
			TooltipPropertyList.Clear();
			Refresh();
		}
	}

	private void Refresh()
	{
		InvokeRefreshData(this);
		if (TooltipPropertyList.Count > 0)
		{
			base.IsActive = true;
		}
	}

	public static void RefreshGenericPropertyBasedTooltip(PropertyBasedTooltipVM propertyBasedTooltip, object[] args)
	{
		List<TooltipProperty> source = args[0] as List<TooltipProperty>;
		propertyBasedTooltip.Mode = 0;
		foreach (TooltipProperty item in source.Where((TooltipProperty p) => (p.OnlyShowWhenExtended && propertyBasedTooltip.IsExtended) || (!p.OnlyShowWhenExtended && p.OnlyShowWhenNotExtended && !propertyBasedTooltip.IsExtended) || (!p.OnlyShowWhenExtended && !p.OnlyShowWhenNotExtended)))
		{
			propertyBasedTooltip.AddPropertyDuplicate(item);
		}
	}

	public void AddProperty(string definition, string value, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		TooltipProperty item = new TooltipProperty(definition, value, textHeight, onlyShowWhenExtended: false, propertyFlags);
		TooltipPropertyList.Add(item);
	}

	public void AddModifierProperty(string definition, int modifierValue, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		string value = ((modifierValue > 0) ? ("+" + modifierValue) : modifierValue.ToString());
		TooltipProperty item = new TooltipProperty(definition, value, textHeight, onlyShowWhenExtended: false, propertyFlags);
		TooltipPropertyList.Add(item);
	}

	public void AddProperty(string definition, Func<string> value, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		TooltipProperty item = new TooltipProperty(definition, value, textHeight, onlyShowWhenExtended: false, propertyFlags);
		TooltipPropertyList.Add(item);
	}

	public void AddProperty(Func<string> definition, Func<string> value, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		TooltipProperty item = new TooltipProperty(definition, value, textHeight, onlyShowWhenExtended: false, propertyFlags);
		TooltipPropertyList.Add(item);
	}

	public void AddColoredProperty(string definition, string value, Color color, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		if (color == Colors.Black)
		{
			AddProperty(definition, value, textHeight);
			return;
		}
		TooltipProperty item = new TooltipProperty(definition, value, textHeight, color, onlyShowWhenExtended: false, propertyFlags);
		TooltipPropertyList.Add(item);
	}

	public void AddColoredProperty(string definition, Func<string> value, Color color, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		if (color == Colors.Black)
		{
			AddProperty(definition, value, textHeight);
			return;
		}
		TooltipProperty item = new TooltipProperty(definition, value, textHeight, color, onlyShowWhenExtended: false, propertyFlags);
		TooltipPropertyList.Add(item);
	}

	public void AddColoredProperty(Func<string> definition, Func<string> value, Color color, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		if (color == Colors.Black)
		{
			AddProperty(definition, value, textHeight);
			return;
		}
		TooltipProperty item = new TooltipProperty(definition, value, textHeight, color, onlyShowWhenExtended: false, propertyFlags);
		TooltipPropertyList.Add(item);
	}

	private void AddPropertyDuplicate(TooltipProperty property)
	{
		TooltipProperty item = new TooltipProperty(property);
		TooltipPropertyList.Add(item);
	}
}
