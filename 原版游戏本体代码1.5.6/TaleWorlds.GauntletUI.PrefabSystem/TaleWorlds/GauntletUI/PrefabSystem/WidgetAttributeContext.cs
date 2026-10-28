using System.Collections.Generic;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetAttributeContext
{
	private List<WidgetAttributeKeyType> _registeredKeyTypes;

	private List<WidgetAttributeValueType> _registeredValueTypes;

	private WidgetAttributeKeyTypeAttribute _widgetAttributeKeyTypeAttribute;

	private WidgetAttributeValueTypeDefault _widgetAttributeValueTypeDefault;

	public IEnumerable<WidgetAttributeKeyType> RegisteredKeyTypes => _registeredKeyTypes;

	public IEnumerable<WidgetAttributeValueType> RegisteredValueTypes => _registeredValueTypes;

	public WidgetAttributeContext()
	{
		_registeredKeyTypes = new List<WidgetAttributeKeyType>();
		_registeredValueTypes = new List<WidgetAttributeValueType>();
		WidgetAttributeKeyTypeId keyType = new WidgetAttributeKeyTypeId();
		WidgetAttributeKeyTypeParameter keyType2 = new WidgetAttributeKeyTypeParameter();
		_widgetAttributeKeyTypeAttribute = new WidgetAttributeKeyTypeAttribute();
		RegisterKeyType(keyType);
		RegisterKeyType(keyType2);
		RegisterKeyType(_widgetAttributeKeyTypeAttribute);
		WidgetAttributeValueTypeConstant valueType = new WidgetAttributeValueTypeConstant();
		WidgetAttributeValueTypeParameter valueType2 = new WidgetAttributeValueTypeParameter();
		_widgetAttributeValueTypeDefault = new WidgetAttributeValueTypeDefault();
		RegisterValueType(valueType);
		RegisterValueType(valueType2);
		RegisterValueType(_widgetAttributeValueTypeDefault);
	}

	public void RegisterKeyType(WidgetAttributeKeyType keyType)
	{
		_registeredKeyTypes.Add(keyType);
	}

	public void RegisterValueType(WidgetAttributeValueType valueType)
	{
		_registeredValueTypes.Add(valueType);
	}

	public WidgetAttributeKeyType GetKeyType(string key)
	{
		WidgetAttributeKeyType widgetAttributeKeyType = null;
		foreach (WidgetAttributeKeyType registeredKeyType in _registeredKeyTypes)
		{
			if (!(registeredKeyType is WidgetAttributeKeyTypeAttribute) && registeredKeyType.CheckKeyType(key))
			{
				widgetAttributeKeyType = registeredKeyType;
			}
		}
		if (widgetAttributeKeyType == null)
		{
			widgetAttributeKeyType = _widgetAttributeKeyTypeAttribute;
		}
		return widgetAttributeKeyType;
	}

	public WidgetAttributeValueType GetValueType(string value)
	{
		WidgetAttributeValueType widgetAttributeValueType = null;
		foreach (WidgetAttributeValueType registeredValueType in _registeredValueTypes)
		{
			if (!(registeredValueType is WidgetAttributeValueTypeDefault) && registeredValueType.CheckValueType(value))
			{
				widgetAttributeValueType = registeredValueType;
			}
		}
		if (widgetAttributeValueType == null)
		{
			widgetAttributeValueType = _widgetAttributeValueTypeDefault;
		}
		return widgetAttributeValueType;
	}
}
