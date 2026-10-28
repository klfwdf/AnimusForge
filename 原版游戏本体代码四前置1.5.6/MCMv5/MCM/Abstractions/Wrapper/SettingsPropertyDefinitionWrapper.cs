using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib.BUTR.Extensions;
using MCM.Common;

namespace MCM.Abstractions.Wrapper;

public sealed class SettingsPropertyDefinitionWrapper : ISettingsPropertyDefinition, IPropertyDefinitionBase, IPropertyDefinitionBool, IPropertyDefinitionDropdown, IPropertyDefinitionWithMinMax, IPropertyDefinitionWithEditableMinMax, IPropertyDefinitionWithFormat, IPropertyDefinitionWithCustomFormatter, IPropertyDefinitionWithId, IPropertyDefinitionText, IPropertyDefinitionGroupToggle, IPropertyGroupDefinition, IPropertyDefinitionButton
{
	public IRef PropertyReference { get; }

	public SettingType SettingType { get; }

	public string DisplayName { get; }

	public int Order { get; }

	public bool RequireRestart { get; }

	public string HintText { get; }

	public decimal MaxValue { get; }

	public decimal MinValue { get; }

	public decimal EditableMinValue { get; }

	public decimal EditableMaxValue { get; }

	public int SelectedIndex { get; }

	public string ValueFormat { get; }

	public Type? CustomFormatter { get; }

	public string GroupName { get; }

	public int GroupOrder { get; }

	public string Id { get; }

	public bool IsToggle { get; }

	private char SubGroupDelimiter { get; }

	public string Content { get; }

	public SettingsPropertyDefinitionWrapper(object @object)
	{
		Type type = @object.GetType();
		PropertyInfo propertyInfo = AccessTools2.Property(type, "SettingType");
		PropertyInfo propertyInfo2 = AccessTools2.Property(type, "PropertyReference");
		PropertyInfo propertyInfo3 = AccessTools2.Property(type, "DisplayName");
		PropertyInfo propertyInfo4 = AccessTools2.Property(type, "HintText");
		PropertyInfo propertyInfo5 = AccessTools2.Property(type, "Order");
		PropertyInfo propertyInfo6 = AccessTools2.Property(type, "RequireRestart");
		PropertyInfo propertyInfo7 = AccessTools2.Property(type, "GroupName");
		PropertyInfo propertyInfo8 = AccessTools2.Property(type, "GroupOrder");
		PropertyInfo propertyInfo9 = AccessTools2.Property(type, "MinValue");
		PropertyInfo propertyInfo10 = AccessTools2.Property(type, "MaxValue");
		PropertyInfo propertyInfo11 = AccessTools2.Property(type, "EditableMinValue");
		PropertyInfo propertyInfo12 = AccessTools2.Property(type, "EditableMaxValue");
		PropertyInfo propertyInfo13 = AccessTools2.Property(type, "SelectedIndex");
		PropertyInfo propertyInfo14 = AccessTools2.Property(type, "ValueFormat");
		PropertyInfo propertyInfo15 = AccessTools2.Property(type, "CustomFormatter");
		PropertyInfo propertyInfo16 = AccessTools2.Property(type, "Id");
		PropertyInfo propertyInfo17 = AccessTools2.Property(type, "IsToggle");
		PropertyInfo propertyInfo18 = AccessTools2.Property(type, "SubGroupDelimiter");
		PropertyInfo propertyInfo19 = AccessTools2.Property(type, "Content");
		object obj = propertyInfo?.GetValue(@object);
		SettingType = ((obj == null) ? SettingType.NONE : (Enum.TryParse<SettingType>(obj.ToString(), out var result) ? result : SettingType.NONE));
		object obj2 = propertyInfo2?.GetValue(@object);
		IRef obj4;
		if (obj2 == null)
		{
			IRef obj3 = new ProxyRef<object>(() => (object?)null, delegate
			{
			});
			obj4 = obj3;
		}
		else if (!(obj2 is IRef obj5))
		{
			IRef obj3 = new RefWrapper(obj2);
			obj4 = obj3;
		}
		else
		{
			obj4 = obj5;
		}
		PropertyReference = obj4;
		object obj6 = propertyInfo3?.GetValue(@object);
		if (1 == 0)
		{
		}
		string text = ((obj6 is string text2) ? text2 : ((obj6 == null) ? "ERROR" : obj6.ToString()));
		if (1 == 0)
		{
		}
		DisplayName = text;
		object obj7 = propertyInfo4?.GetValue(@object);
		if (1 == 0)
		{
		}
		text = ((obj7 is string text3) ? text3 : ((obj7 == null) ? "ERROR" : obj7.ToString()));
		if (1 == 0)
		{
		}
		HintText = text;
		Order = (propertyInfo5?.GetValue(@object) as int?) ?? (-1);
		RequireRestart = (propertyInfo6?.GetValue(@object) as bool?) ?? true;
		GroupName = (propertyInfo7?.GetValue(@object) as string) ?? string.Empty;
		GroupOrder = (propertyInfo8?.GetValue(@object) as int?) ?? (-1);
		object obj8 = propertyInfo9?.GetValue(@object);
		MinValue = ((obj8 != null) ? (obj8 as decimal?).GetValueOrDefault() : 0m);
		object obj9 = propertyInfo10?.GetValue(@object);
		MaxValue = ((obj9 != null) ? (obj9 as decimal?).GetValueOrDefault() : 0m);
		object obj10 = propertyInfo11?.GetValue(@object);
		EditableMinValue = ((obj10 != null) ? (obj10 as decimal?).GetValueOrDefault() : 0m);
		object obj11 = propertyInfo12?.GetValue(@object);
		EditableMaxValue = ((obj11 != null) ? (obj11 as decimal?).GetValueOrDefault() : 0m);
		SelectedIndex = (propertyInfo13?.GetValue(@object) as int?).GetValueOrDefault();
		string text4 = propertyInfo14?.GetValue(@object) as string;
		if (text4 == null)
		{
			SettingType settingType = SettingType;
			if (1 == 0)
			{
			}
			text = settingType switch
			{
				SettingType.Int => "0", 
				SettingType.Float => "0.00", 
				_ => string.Empty, 
			};
			if (1 == 0)
			{
			}
			text4 = text;
		}
		ValueFormat = text4;
		CustomFormatter = propertyInfo15?.GetValue(@object) as Type;
		Id = (propertyInfo16?.GetValue(@object) as string) ?? string.Empty;
		IsToggle = propertyInfo17?.GetValue(@object) as bool? == true;
		SubGroupDelimiter = (propertyInfo18?.GetValue(@object) as char?) ?? '/';
		Content = (propertyInfo19?.GetValue(@object) as string) ?? string.Empty;
	}

	public SettingsPropertyDefinition Clone(bool keepRefs = true)
	{
		object value = PropertyReference.Value;
		IEnumerable<IPropertyDefinitionBase> propertyDefinitionWrappers = SettingsUtils.GetPropertyDefinitionWrappers(this);
		PropertyGroupDefinitionWrapper propertyGroupDefinition = new PropertyGroupDefinitionWrapper(this);
		IRef propertyReference;
		if (!keepRefs)
		{
			IRef obj = new StorageRef(value);
			propertyReference = obj;
		}
		else
		{
			propertyReference = PropertyReference;
		}
		return new SettingsPropertyDefinition(propertyDefinitionWrappers, propertyGroupDefinition, propertyReference, SubGroupDelimiter);
	}
}
