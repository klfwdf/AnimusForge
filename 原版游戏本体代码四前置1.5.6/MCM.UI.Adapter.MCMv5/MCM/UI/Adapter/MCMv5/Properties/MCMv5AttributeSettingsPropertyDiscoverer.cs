using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.UI.Adapter.MCMv5.Properties;

public sealed class MCMv5AttributeSettingsPropertyDiscoverer : IAttributeSettingsPropertyDiscoverer, ISettingsPropertyDiscoverer
{
	public IEnumerable<string> DiscoveryTypes { get; } = new string[1] { "mcm_v5_attributes" };

	public IEnumerable<ISettingsPropertyDefinition> GetProperties(BaseSettings settings)
	{
		if (1 == 0)
		{
		}
		object obj = ((!(settings is IWrapper wrapper)) ? settings : wrapper.Object);
		if (1 == 0)
		{
		}
		object obj2 = obj;
		foreach (ISettingsPropertyDefinition propertyDefinition in GetPropertiesInternal(obj2))
		{
			SettingsUtils.CheckIsValid(propertyDefinition, obj2);
			yield return propertyDefinition;
		}
	}

	private static IEnumerable<ISettingsPropertyDefinition> GetPropertiesInternal(object? @object)
	{
		if (@object == null)
		{
			yield break;
		}
		Type type = @object.GetType();
		char subGroupDelimiter = (HarmonyLib.BUTR.Extensions.AccessTools2.Property(type, "SubGroupDelimiter")?.GetValue(@object) as char?) ?? '/';
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo property in properties)
		{
			if (!(property.Name == "Id") && !(property.Name == "DisplayName") && !(property.Name == "FolderName") && !(property.Name == "FormatType") && !(property.Name == "SubFolder") && !(property.Name == "UIVersion"))
			{
				List<Attribute> attributes = property.GetCustomAttributes().ToList();
				object groupAttrObj = attributes.SingleOrDefault((Attribute a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyGroupDefinition"));
				IPropertyGroupDefinition propertyGroupDefinition;
				if (groupAttrObj == null)
				{
					propertyGroupDefinition = SettingPropertyGroupAttribute.Default;
				}
				else
				{
					IPropertyGroupDefinition propertyGroupDefinition2 = new PropertyGroupDefinitionWrapper(groupAttrObj);
					propertyGroupDefinition = propertyGroupDefinition2;
				}
				IPropertyGroupDefinition groupDefinition = propertyGroupDefinition;
				List<IPropertyDefinitionBase> propertyDefinitions = GetPropertyDefinitionWrappers(attributes).ToList();
				if (propertyDefinitions.Count > 0)
				{
					yield return new SettingsPropertyDefinition(propertyDefinitions, groupDefinition, new PropertyRef(property, @object), subGroupDelimiter);
				}
			}
		}
	}

	private static IEnumerable<IPropertyDefinitionBase> GetPropertyDefinitionWrappers(IReadOnlyCollection<object> properties)
	{
		object propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionBool"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionBoolWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionDropdown"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionDropdownWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionGroupToggle"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionGroupToggleWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionText"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionTextWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionWithActionFormat"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithActionFormatWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionWithCustomFormatter"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithCustomFormatterWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionWithEditableMinMax"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithEditableMinMaxWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionWithFormat"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithFormatWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionWithId"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithIdWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionWithMinMax"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithMinMaxWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => ReflectionUtils.ImplementsEquivalentInterface(a.GetType(), "MCM.Abstractions.IPropertyDefinitionButton"));
		if (propAttr != null)
		{
			yield return new PropertyDefinitionButtonWrapper(propAttr);
		}
	}
}
