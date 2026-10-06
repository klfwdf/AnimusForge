using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Data;

public class PrefabDatabindingExtension : PrefabExtension
{
	private static WidgetAttributeTemplate GetDataSource(WidgetTemplate widgetTemplate)
	{
		return widgetTemplate.GetFirstAttributeIfExist<WidgetAttributeKeyTypeDataSource>();
	}

	protected override void RegisterAttributeTypes(WidgetAttributeContext widgetAttributeContext)
	{
		WidgetAttributeKeyTypeDataSource keyType = new WidgetAttributeKeyTypeDataSource();
		WidgetAttributeKeyTypeCommand keyType2 = new WidgetAttributeKeyTypeCommand();
		WidgetAttributeKeyTypeCommandParameter keyType3 = new WidgetAttributeKeyTypeCommandParameter();
		widgetAttributeContext.RegisterKeyType(keyType);
		widgetAttributeContext.RegisterKeyType(keyType2);
		widgetAttributeContext.RegisterKeyType(keyType3);
		WidgetAttributeValueTypeBindingPath valueType = new WidgetAttributeValueTypeBindingPath();
		WidgetAttributeValueTypeBinding valueType2 = new WidgetAttributeValueTypeBinding();
		widgetAttributeContext.RegisterValueType(valueType);
		widgetAttributeContext.RegisterValueType(valueType2);
	}

	protected override void OnWidgetCreated(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, int childCount)
	{
		GauntletMovie extensionData = widgetCreationData.GetExtensionData<GauntletMovie>();
		if (extensionData != null)
		{
			WidgetTemplate template = widgetInstantiationResult.Template;
			Widget widget = widgetInstantiationResult.Widget;
			GauntletView gauntletView = widgetCreationData.Parent?.GetComponent<GauntletView>();
			GauntletView gauntletView2 = new GauntletView(extensionData, gauntletView, widget, childCount);
			widget.AddComponent(gauntletView2);
			widgetInstantiationResult.AddExtensionData(gauntletView2, passToChildWidgetCreation: true);
			ItemTemplateUsage extensionData2 = template.GetExtensionData<ItemTemplateUsage>();
			if (extensionData2 != null)
			{
				gauntletView2.ItemTemplateUsageWithData = new ItemTemplateUsageWithData(extensionData2);
			}
			gauntletView?.AddChild(gauntletView2);
		}
	}

	protected override void OnSave(PrefabExtensionContext prefabExtensionContext, XmlNode node, WidgetTemplate widgetTemplate)
	{
		ItemTemplateUsage extensionData = widgetTemplate.GetExtensionData<ItemTemplateUsage>();
		if (extensionData != null)
		{
			XmlDocument ownerDocument = node.OwnerDocument;
			XmlNode xmlNode = ownerDocument.CreateElement("ItemTemplate");
			extensionData.DefaultItemTemplate.Save(prefabExtensionContext, xmlNode);
			node.AppendChild(xmlNode);
			if (extensionData.FirstItemTemplate != null)
			{
				XmlNode xmlNode2 = ownerDocument.CreateElement("ItemTemplate");
				XmlAttribute xmlAttribute = ownerDocument.CreateAttribute("Type");
				xmlAttribute.InnerText = "First";
				xmlNode2.Attributes.Append(xmlAttribute);
				extensionData.DefaultItemTemplate.Save(prefabExtensionContext, xmlNode2);
				node.AppendChild(xmlNode2);
			}
			if (extensionData.LastItemTemplate != null)
			{
				XmlNode xmlNode3 = ownerDocument.CreateElement("ItemTemplate");
				XmlAttribute xmlAttribute2 = ownerDocument.CreateAttribute("Type");
				xmlAttribute2.InnerText = "Last";
				xmlNode3.Attributes.Append(xmlAttribute2);
				extensionData.DefaultItemTemplate.Save(prefabExtensionContext, xmlNode3);
				node.AppendChild(xmlNode3);
			}
		}
	}

	protected override void OnAttributesSet(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, Dictionary<string, WidgetAttributeTemplate> parameters)
	{
		if (widgetInstantiationResult.Template.GetExtensionData<ItemTemplateUsage>() == null)
		{
			return;
		}
		ItemTemplateUsageWithData itemTemplateUsageWithData = widgetInstantiationResult.GetGauntletView().ItemTemplateUsageWithData;
		foreach (KeyValuePair<string, WidgetAttributeTemplate> parameter in parameters)
		{
			string key = parameter.Key;
			WidgetAttributeTemplate value = parameter.Value;
			itemTemplateUsageWithData.GivenParameters.Add(key, value);
		}
	}

	protected override void DoLoading(PrefabExtensionContext prefabExtensionContext, WidgetAttributeContext widgetAttributeContext, WidgetTemplate template, XmlNode node)
	{
		XmlNodeList xmlNodeList = node.SelectNodes("ItemTemplate");
		ItemTemplateUsage itemTemplateUsage = null;
		foreach (XmlNode item in xmlNodeList)
		{
			XmlAttribute xmlAttribute = item.Attributes["Type"];
			if (xmlAttribute == null || xmlAttribute.Value == "Default")
			{
				XmlNode firstChild = item.FirstChild;
				itemTemplateUsage = new ItemTemplateUsage(WidgetTemplate.LoadFrom(prefabExtensionContext, widgetAttributeContext, firstChild));
				template.AddExtensionData(itemTemplateUsage);
			}
		}
		if (itemTemplateUsage == null)
		{
			return;
		}
		foreach (XmlNode item2 in xmlNodeList)
		{
			XmlAttribute xmlAttribute2 = item2.Attributes["Type"];
			if (xmlAttribute2 != null && !(xmlAttribute2.Value == "Default"))
			{
				XmlNode firstChild2 = item2.FirstChild;
				WidgetTemplate widgetTemplate = WidgetTemplate.LoadFrom(prefabExtensionContext, widgetAttributeContext, firstChild2);
				if (xmlAttribute2.Value == "First")
				{
					itemTemplateUsage.FirstItemTemplate = widgetTemplate;
				}
				else if (xmlAttribute2.Value == "Last")
				{
					itemTemplateUsage.LastItemTemplate = widgetTemplate;
				}
			}
		}
	}

	protected override void OnLoadingFinished(WidgetPrefab widgetPrefab)
	{
		SetRootTemplate(widgetPrefab.RootTemplate, widgetPrefab);
	}

	private void SetRootTemplate(WidgetTemplate widgetTemplate, WidgetPrefab prefab)
	{
		ItemTemplateUsage extensionData = widgetTemplate.GetExtensionData<ItemTemplateUsage>();
		if (extensionData != null)
		{
			if (extensionData.FirstItemTemplate != null)
			{
				extensionData.FirstItemTemplate.SetRootTemplate(prefab);
				SetRootTemplate(extensionData.FirstItemTemplate, prefab);
			}
			if (extensionData.LastItemTemplate != null)
			{
				extensionData.LastItemTemplate.SetRootTemplate(prefab);
				SetRootTemplate(extensionData.LastItemTemplate, prefab);
			}
			if (extensionData.DefaultItemTemplate != null)
			{
				extensionData.DefaultItemTemplate.SetRootTemplate(prefab);
				SetRootTemplate(extensionData.DefaultItemTemplate, prefab);
			}
		}
		for (int i = 0; i < widgetTemplate.ChildCount; i++)
		{
			WidgetTemplate childAt = widgetTemplate.GetChildAt(i);
			SetRootTemplate(childAt, prefab);
		}
	}

	protected override void AfterAttributesSet(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, Dictionary<string, WidgetAttributeTemplate> parameters)
	{
		WidgetTemplate template = widgetInstantiationResult.Template;
		GauntletView gauntletView = widgetInstantiationResult.GetGauntletView();
		bool flag = gauntletView.Parent == null;
		WidgetAttributeTemplate dataSource = GetDataSource(template);
		WidgetPrefab prefab = template.Prefab;
		BrushFactory brushFactory = widgetCreationData.BrushFactory;
		SpriteData spriteData = widgetCreationData.SpriteData;
		BindingPath bindingPath = null;
		if (flag)
		{
			bindingPath = new BindingPath("Root");
		}
		else if (dataSource != null)
		{
			if (dataSource.ValueType is WidgetAttributeValueTypeBindingPath)
			{
				bindingPath = new BindingPath(dataSource.Value);
			}
			else if (dataSource.ValueType is WidgetAttributeValueTypeParameter)
			{
				string value = dataSource.Value;
				string path = prefab.GetParameterDefaultValue(value);
				if (parameters.ContainsKey(value) && parameters[value].ValueType is WidgetAttributeValueTypeBindingPath)
				{
					path = parameters[value].Value;
				}
				if (!string.IsNullOrEmpty(value))
				{
					bindingPath = new BindingPath(path);
				}
			}
			else if (dataSource.ValueType is WidgetAttributeValueTypeConstant)
			{
				string value2 = dataSource.Value;
				string value3 = prefab.GetConstantValue(value2).GetValue(brushFactory, spriteData, prefab.Constants, parameters, prefab.Parameters);
				if (!string.IsNullOrEmpty(value3))
				{
					bindingPath = new BindingPath(value3);
				}
			}
		}
		Dictionary<string, WidgetAttributeTemplate> dictionary = template.GetAttributesOf<WidgetAttributeKeyTypeParameter>().ToDictionary((WidgetAttributeTemplate widgetAttributeTemplate3) => widgetAttributeTemplate3.Key);
		foreach (KeyValuePair<string, WidgetAttributeTemplate> parameter in parameters)
		{
			if (!dictionary.ContainsKey(parameter.Key))
			{
				dictionary.Add(parameter.Key, parameter.Value);
			}
		}
		if (widgetInstantiationResult.CustomWidgetInstantiationData != null)
		{
			WidgetInstantiationResult customWidgetInstantiationData = widgetInstantiationResult.CustomWidgetInstantiationData;
			AfterAttributesSet(widgetCreationData, customWidgetInstantiationData, dictionary);
		}
		if (bindingPath != null)
		{
			gauntletView.InitializeViewModelPath(bindingPath);
		}
		IEnumerable<WidgetAttributeTemplate> attributesOf = template.GetAttributesOf<WidgetAttributeKeyTypeCommand>();
		Dictionary<string, WidgetAttributeTemplate> dictionary2 = template.GetAttributesOf<WidgetAttributeKeyTypeCommandParameter>().ToDictionary((WidgetAttributeTemplate widgetAttributeTemplate3) => widgetAttributeTemplate3.Key);
		foreach (WidgetAttributeTemplate item in attributesOf)
		{
			string key = item.Key;
			string value4 = item.Value;
			if (dictionary2.ContainsKey(key))
			{
				WidgetAttributeTemplate widgetAttributeTemplate = dictionary2[key];
				string text = null;
				if (widgetAttributeTemplate.ValueType is WidgetAttributeValueTypeDefault)
				{
					text = widgetAttributeTemplate.Value;
				}
				else if (parameters.ContainsKey(widgetAttributeTemplate.Value))
				{
					text = parameters[widgetAttributeTemplate.Value].Value;
				}
				if (!string.IsNullOrEmpty(text))
				{
					gauntletView.BindCommand(key, new BindingPath(value4), text);
				}
				else
				{
					gauntletView.BindCommand(key, new BindingPath(value4));
				}
			}
			else
			{
				string path2 = value4;
				if (item.ValueType is WidgetAttributeValueTypeParameter && parameters.ContainsKey(value4))
				{
					path2 = parameters[value4].Value;
				}
				gauntletView.BindCommand(key, new BindingPath(path2));
			}
		}
		foreach (WidgetAttributeTemplate allAttribute in template.AllAttributes)
		{
			WidgetAttributeKeyType keyType = allAttribute.KeyType;
			WidgetAttributeValueType valueType = allAttribute.ValueType;
			string key2 = allAttribute.Key;
			string value5 = allAttribute.Value;
			if (!(keyType is WidgetAttributeKeyTypeAttribute) && !(keyType is WidgetAttributeKeyTypeId))
			{
				continue;
			}
			if (valueType is WidgetAttributeValueTypeBinding)
			{
				gauntletView.BindData(key2, new BindingPath(value5));
			}
			else
			{
				if (!(valueType is WidgetAttributeValueTypeParameter))
				{
					continue;
				}
				string key3 = value5;
				if (!parameters.ContainsKey(key3))
				{
					continue;
				}
				WidgetAttributeTemplate widgetAttributeTemplate2 = parameters[key3];
				if (widgetAttributeTemplate2.ValueType is WidgetAttributeValueTypeBinding)
				{
					string value6 = widgetAttributeTemplate2.Value;
					if (!string.IsNullOrEmpty(value6))
					{
						gauntletView.BindData(key2, new BindingPath(value6));
					}
				}
			}
		}
		foreach (WidgetInstantiationResult child in widgetInstantiationResult.Children)
		{
			AfterAttributesSet(widgetCreationData, child, parameters);
		}
	}
}
