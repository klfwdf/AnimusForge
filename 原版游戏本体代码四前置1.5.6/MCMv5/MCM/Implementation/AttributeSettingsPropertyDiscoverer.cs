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

namespace MCM.Implementation;

internal sealed class AttributeSettingsPropertyDiscoverer : IAttributeSettingsPropertyDiscoverer, ISettingsPropertyDiscoverer
{
	public IEnumerable<string> DiscoveryTypes { get; } = new string[1] { "attributes" };

	public IEnumerable<ISettingsPropertyDefinition> GetProperties(BaseSettings settings)
	{
		foreach (ISettingsPropertyDefinition propertyDefinition in GetPropertiesInternal(settings))
		{
			SettingsUtils.CheckIsValid(propertyDefinition, settings);
			yield return propertyDefinition;
		}
	}

	private static IEnumerable<ISettingsPropertyDefinition> GetPropertiesInternal(BaseSettings settings)
	{
		Type type = settings.GetType();
		char subGroupDelimiter = (AccessTools2.Property(type, "SubGroupDelimiter")?.GetValue(settings) as char?) ?? '/';
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo property in properties)
		{
			if (!(property.Name == "Id") && !(property.Name == "DisplayName") && !(property.Name == "FolderName") && !(property.Name == "FormatType") && !(property.Name == "SubFolder") && !(property.Name == "UIVersion"))
			{
				List<Attribute> attributes = property.GetCustomAttributes().ToList();
				object groupAttrObj = attributes.SingleOrDefault((Attribute a) => a is IPropertyGroupDefinition);
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
				List<IPropertyDefinitionBase> propertyDefinitions = SettingsUtils.GetPropertyDefinitionWrappers(attributes).ToList();
				if (propertyDefinitions.Count > 0)
				{
					yield return new SettingsPropertyDefinition(propertyDefinitions, groupDefinition, new PropertyRef(property, settings), subGroupDelimiter);
				}
			}
		}
	}
}
