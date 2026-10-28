using System;
using System.Collections.Generic;
using MCM.Abstractions.Attributes.v1;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Abstractions;

public sealed class SettingsPropertyDefinition : ISettingsPropertyDefinition, IPropertyDefinitionBase, IPropertyDefinitionBool, IPropertyDefinitionDropdown, IPropertyDefinitionWithMinMax, IPropertyDefinitionWithEditableMinMax, IPropertyDefinitionWithFormat, IPropertyDefinitionWithCustomFormatter, IPropertyDefinitionWithId, IPropertyDefinitionText, IPropertyDefinitionGroupToggle, IPropertyGroupDefinition, IPropertyDefinitionButton
{
	public string Id { get; } = string.Empty;

	public IRef PropertyReference { get; }

	public SettingType SettingType { get; } = SettingType.NONE;

	public string DisplayName { get; } = string.Empty;

	public int Order { get; } = -1;

	public bool RequireRestart { get; } = true;

	public string HintText { get; } = string.Empty;

	public decimal MaxValue { get; }

	public decimal MinValue { get; }

	public decimal EditableMinValue { get; }

	public decimal EditableMaxValue { get; }

	public int SelectedIndex { get; }

	public string ValueFormat { get; } = string.Empty;

	public Type? CustomFormatter { get; }

	public string GroupName { get; }

	public bool IsToggle { get; }

	public int GroupOrder { get; }

	private char SubGroupDelimiter { get; }

	public string Content { get; } = string.Empty;

	public SettingsPropertyDefinition(IPropertyDefinitionBase propertyDefinition, IPropertyGroupDefinition propertyGroupDefinition, IRef propertyReference, char subGroupDelimiter)
		: this(new IPropertyDefinitionBase[1] { propertyDefinition }, propertyGroupDefinition, propertyReference, subGroupDelimiter)
	{
	}

	public SettingsPropertyDefinition(IEnumerable<IPropertyDefinitionBase> propertyDefinitions, IPropertyGroupDefinition propertyGroupDefinition, IRef propertyReference, char subGroupDelimiter)
	{
		SubGroupDelimiter = subGroupDelimiter;
		string[] value = propertyGroupDefinition.GroupName.Split(new char[1] { SubGroupDelimiter });
		GroupName = string.Join(SubGroupDelimiter.ToString(), value);
		GroupOrder = propertyGroupDefinition.GroupOrder;
		PropertyReference = propertyReference;
		if (PropertyReference is PropertyRef propertyRef)
		{
			Id = propertyRef.PropertyInfo.Name;
		}
		if (PropertyReference.Type == typeof(bool))
		{
			SettingType = SettingType.Bool;
		}
		else if (PropertyReference.Type == typeof(int))
		{
			SettingType = SettingType.Int;
		}
		else if (PropertyReference.Type == typeof(float))
		{
			SettingType = SettingType.Float;
		}
		else if (PropertyReference.Type == typeof(string))
		{
			SettingType = SettingType.String;
		}
		else if (SettingsUtils.IsForGenericDropdown(PropertyReference.Type))
		{
			SettingType = SettingType.Dropdown;
		}
		else if (PropertyReference.Type == typeof(Action))
		{
			SettingType = SettingType.Button;
		}
		foreach (IPropertyDefinitionBase propertyDefinition in propertyDefinitions)
		{
			if (propertyDefinition != null)
			{
				IPropertyDefinitionBase propertyDefinitionBase = propertyDefinition;
				if (true)
				{
					DisplayName = propertyDefinitionBase.DisplayName;
					Order = propertyDefinitionBase.Order;
					RequireRestart = propertyDefinitionBase.RequireRestart;
					HintText = propertyDefinitionBase.HintText;
				}
			}
			if (propertyDefinition is SettingPropertyAttribute settingPropertyAttribute)
			{
				MinValue = settingPropertyAttribute.MinValue;
				MaxValue = settingPropertyAttribute.MaxValue;
				EditableMinValue = settingPropertyAttribute.MinValue;
				EditableMaxValue = settingPropertyAttribute.MaxValue;
			}
			IPropertyDefinitionBool propertyDefinitionBool = propertyDefinition as IPropertyDefinitionBool;
			if (propertyDefinitionBool != null)
			{
			}
			if (propertyDefinition is IPropertyDefinitionWithMinMax propertyDefinitionWithMinMax)
			{
				MinValue = propertyDefinitionWithMinMax.MinValue;
				MaxValue = propertyDefinitionWithMinMax.MaxValue;
				EditableMinValue = propertyDefinitionWithMinMax.MinValue;
				EditableMaxValue = propertyDefinitionWithMinMax.MaxValue;
			}
			if (propertyDefinition is IPropertyDefinitionWithEditableMinMax propertyDefinitionWithEditableMinMax)
			{
				EditableMinValue = propertyDefinitionWithEditableMinMax.EditableMinValue;
				EditableMaxValue = propertyDefinitionWithEditableMinMax.EditableMaxValue;
			}
			if (propertyDefinition is IPropertyDefinitionWithFormat propertyDefinitionWithFormat)
			{
				ValueFormat = propertyDefinitionWithFormat.ValueFormat;
			}
			if (propertyDefinition is IPropertyDefinitionWithCustomFormatter propertyDefinitionWithCustomFormatter)
			{
				CustomFormatter = propertyDefinitionWithCustomFormatter.CustomFormatter;
			}
			IPropertyDefinitionWithActionFormat propertyDefinitionWithActionFormat = propertyDefinition as IPropertyDefinitionWithActionFormat;
			if (propertyDefinitionWithActionFormat != null)
			{
			}
			IPropertyDefinitionText propertyDefinitionText = propertyDefinition as IPropertyDefinitionText;
			if (propertyDefinitionText != null)
			{
			}
			if (propertyDefinition is IPropertyDefinitionDropdown propertyDefinitionDropdown)
			{
				SelectedIndex = propertyDefinitionDropdown.SelectedIndex;
			}
			if (propertyDefinition is IPropertyDefinitionWithId propertyDefinitionWithId)
			{
				Id = propertyDefinitionWithId.Id;
			}
			if (propertyDefinition is IPropertyDefinitionGroupToggle propertyDefinitionGroupToggle)
			{
				IsToggle = propertyDefinitionGroupToggle.IsToggle;
			}
			if (propertyDefinition is IPropertyDefinitionButton propertyDefinitionButton)
			{
				Content = propertyDefinitionButton.Content;
			}
		}
	}

	public override string ToString()
	{
		return "[" + GroupName + "]: " + DisplayName;
	}

	public SettingsPropertyDefinition Clone(bool keepRefs = true)
	{
		object value = PropertyReference.Value;
		IRef obj2;
		if (!keepRefs)
		{
			IRef obj = new StorageRef((value is ICloneable cloneable) ? cloneable.Clone() : value);
			obj2 = obj;
		}
		else
		{
			obj2 = PropertyReference;
		}
		IRef propertyReference = obj2;
		return new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(this), new PropertyGroupDefinitionWrapper(this), propertyReference, SubGroupDelimiter);
	}
}
