using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetTemplate
{
	private string _type;

	private WidgetFactory _usedFactory;

	private List<WidgetTemplate> _children;

	private List<WidgetTemplate> _customTypeChildren;

	private Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>> _attributes;

	private Dictionary<string, object> _extensionData;

	public bool LogicalChildrenLocation { get; private set; }

	public string Id
	{
		get
		{
			WidgetAttributeTemplate firstAttributeIfExist = GetFirstAttributeIfExist<WidgetAttributeKeyTypeId>();
			if (firstAttributeIfExist != null)
			{
				return firstAttributeIfExist.Value;
			}
			return "";
		}
	}

	public string Type => _type;

	public int ChildCount => _children.Count;

	public Dictionary<string, WidgetAttributeTemplate> GivenParameters => GetAttributesOf<WidgetAttributeKeyTypeParameter>().ToDictionary((WidgetAttributeTemplate key) => key.Key);

	public WidgetPrefab Prefab { get; private set; }

	public WidgetTemplate RootTemplate => Prefab.RootTemplate;

	public Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>> Attributes => _attributes;

	public object Tag { get; set; }

	public IEnumerable<WidgetAttributeTemplate> AllAttributes
	{
		get
		{
			foreach (Dictionary<string, WidgetAttributeTemplate> value in _attributes.Values)
			{
				foreach (WidgetAttributeTemplate value2 in value.Values)
				{
					yield return value2;
				}
			}
		}
	}

	public WidgetTemplate(string type)
	{
		_type = type;
		_extensionData = new Dictionary<string, object>();
		Tag = Guid.NewGuid();
		_attributes = new Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>>();
		_children = new List<WidgetTemplate>();
		_customTypeChildren = new List<WidgetTemplate>();
	}

	internal void LoadAttributeCollection(WidgetAttributeContext widgetAttributeContext, XmlAttributeCollection attributes)
	{
		foreach (WidgetAttributeKeyType registeredKeyType in widgetAttributeContext.RegisteredKeyTypes)
		{
			_attributes.Add(registeredKeyType.GetType(), new Dictionary<string, WidgetAttributeTemplate>());
		}
		foreach (XmlAttribute attribute in attributes)
		{
			string name = attribute.Name;
			string value = attribute.Value;
			AddAttributeTo(widgetAttributeContext, name, value);
		}
	}

	public void AddExtensionData(string name, object data)
	{
		if (_extensionData.ContainsKey(name))
		{
			_extensionData[name] = data;
		}
		else
		{
			_extensionData.Add(name, data);
		}
	}

	public T GetExtensionData<T>(string name) where T : class
	{
		_extensionData.TryGetValue(name, out var value);
		return value as T;
	}

	public void RemoveExtensionData(string name)
	{
		_extensionData.Remove(name);
	}

	public void AddExtensionData(object data)
	{
		AddExtensionData(data.GetType().Name, data);
	}

	public T GetExtensionData<T>() where T : class
	{
		return GetExtensionData<T>(typeof(T).Name);
	}

	public void RemoveExtensionData<T>() where T : class
	{
		RemoveExtensionData(typeof(T).Name);
	}

	public IEnumerable<WidgetAttributeTemplate> GetAttributesOf<T>() where T : WidgetAttributeKeyType
	{
		return _attributes[typeof(T)].Values;
	}

	public IEnumerable<WidgetAttributeTemplate> GetAttributesOf<TKey, TValue>() where TKey : WidgetAttributeKeyType where TValue : WidgetAttributeValueType
	{
		IEnumerable<WidgetAttributeTemplate> attributesOf = GetAttributesOf<TKey>();
		foreach (WidgetAttributeTemplate item in attributesOf)
		{
			if (item.ValueType is TValue)
			{
				yield return item;
			}
		}
	}

	public WidgetAttributeTemplate GetFirstAttributeIfExist<T>() where T : WidgetAttributeKeyType
	{
		using (IEnumerator<WidgetAttributeTemplate> enumerator = GetAttributesOf<T>().GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	public void SetAttribute(WidgetAttributeTemplate attribute)
	{
		Dictionary<string, WidgetAttributeTemplate> dictionary = _attributes[attribute.KeyType.GetType()];
		if (dictionary.ContainsKey(attribute.Key))
		{
			dictionary[attribute.Key] = attribute;
		}
		else
		{
			dictionary.Add(attribute.Key, attribute);
		}
	}

	public WidgetTemplate GetChildAt(int i)
	{
		return _children[i];
	}

	public void AddChild(WidgetTemplate child)
	{
		_children.Add(child);
	}

	public void RemoveChild(WidgetTemplate child)
	{
		_children.Remove(child);
	}

	public void SwapChildren(WidgetTemplate child1, WidgetTemplate child2)
	{
		int index = _children.IndexOf(child1);
		int index2 = _children.IndexOf(child2);
		WidgetTemplate value = _children[index];
		_children[index] = _children[index2];
		_children[index2] = value;
	}

	public WidgetInstantiationResult Instantiate(WidgetCreationData widgetCreationData, Dictionary<string, WidgetAttributeTemplate> parameters)
	{
		PrefabExtensionContext prefabExtensionContext = widgetCreationData.PrefabExtensionContext;
		WidgetInstantiationResult widgetInstantiationResult = CreateWidgets(widgetCreationData);
		SetAttributes(widgetCreationData, widgetInstantiationResult, parameters);
		foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
		{
			prefabExtension.AfterAttributesSet(widgetCreationData, widgetInstantiationResult, parameters);
		}
		return widgetInstantiationResult;
	}

	private WidgetInstantiationResult CreateWidgets(WidgetCreationData widgetCreationData)
	{
		_usedFactory = widgetCreationData.WidgetFactory;
		PrefabExtensionContext prefabExtensionContext = _usedFactory.PrefabExtensionContext;
		UIContext context = widgetCreationData.Context;
		Widget widget = null;
		Widget parent = widgetCreationData.Parent;
		WidgetInstantiationResult widgetInstantiationResult = null;
		WidgetInstantiationResult widgetInstantiationResult2 = null;
		if (_usedFactory.IsCustomType(_type))
		{
			WidgetInstantiationResult widgetInstantiationResult3 = _usedFactory.GetCustomType(_type).RootTemplate.CreateWidgets(widgetCreationData);
			_customTypeChildren.AddRange(widgetInstantiationResult3.Children.Select((WidgetInstantiationResult c) => c.Template));
			widget = widgetInstantiationResult3.Widget;
			widgetInstantiationResult = new WidgetInstantiationResult(widget, this, widgetInstantiationResult3);
			widgetInstantiationResult2 = widgetInstantiationResult.GetLogicalOrDefaultChildrenLocation();
		}
		else
		{
			widget = _usedFactory.CreateBuiltinWidget(context, _type);
			widgetInstantiationResult = new WidgetInstantiationResult(widget, this);
			parent?.AddChild(widget);
			foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
			{
				prefabExtension.OnWidgetCreated(widgetCreationData, widgetInstantiationResult, ChildCount);
			}
			widgetInstantiationResult2 = widgetInstantiationResult;
		}
		widget.Tag = Tag;
		widget.Id = Id;
		foreach (WidgetTemplate child in _children)
		{
			WidgetCreationData widgetCreationData2 = new WidgetCreationData(widgetCreationData, widgetInstantiationResult2);
			WidgetInstantiationResult item = child.CreateWidgets(widgetCreationData2);
			widgetInstantiationResult2.Children.Add(item);
		}
		return widgetInstantiationResult;
	}

	public void OnRelease()
	{
		if (_usedFactory.IsCustomType(_type))
		{
			_usedFactory.OnUnload(_type);
		}
		foreach (WidgetTemplate child in _children)
		{
			child.OnRelease();
		}
		foreach (WidgetTemplate customTypeChild in _customTypeChildren)
		{
			customTypeChild.OnRelease();
		}
	}

	private void SetAttributes(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, Dictionary<string, WidgetAttributeTemplate> parameters)
	{
		BrushFactory brushFactory = widgetCreationData.BrushFactory;
		SpriteData spriteData = widgetCreationData.SpriteData;
		PrefabExtensionContext prefabExtensionContext = widgetCreationData.PrefabExtensionContext;
		Widget widget = widgetInstantiationResult.Widget;
		WidgetPrefab prefab = widgetInstantiationResult.Template.Prefab;
		foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
		{
			prefabExtension.OnAttributesSet(widgetCreationData, widgetInstantiationResult, parameters);
		}
		if (widgetInstantiationResult.CustomWidgetInstantiationData != null)
		{
			WidgetInstantiationResult customWidgetInstantiationData = widgetInstantiationResult.CustomWidgetInstantiationData;
			WidgetTemplate template = customWidgetInstantiationData.Template;
			Dictionary<string, WidgetAttributeTemplate> dictionary = new Dictionary<string, WidgetAttributeTemplate>();
			foreach (KeyValuePair<string, WidgetAttributeTemplate> givenParameter in GivenParameters)
			{
				string key = givenParameter.Key;
				WidgetAttributeTemplate widgetAttributeTemplate = givenParameter.Value;
				if (widgetAttributeTemplate.KeyType is WidgetAttributeKeyTypeParameter && widgetAttributeTemplate.ValueType is WidgetAttributeValueTypeParameter && parameters.TryGetValue(key, out var value))
				{
					widgetAttributeTemplate = value;
				}
				dictionary.Add(key, widgetAttributeTemplate);
			}
			template.SetAttributes(widgetCreationData, customWidgetInstantiationData, dictionary);
		}
		foreach (WidgetAttributeTemplate allAttribute in AllAttributes)
		{
			WidgetAttributeKeyType keyType = allAttribute.KeyType;
			WidgetAttributeValueType valueType = allAttribute.ValueType;
			string key2 = allAttribute.Key;
			string value2 = allAttribute.Value;
			if (!(keyType is WidgetAttributeKeyTypeAttribute))
			{
				continue;
			}
			if (valueType is WidgetAttributeValueTypeDefault)
			{
				WidgetExtensions.SetWidgetAttributeFromString(widget, key2, value2, brushFactory, spriteData, prefab.VisualDefinitionTemplates, prefab.Constants, parameters, prefab.CustomElements, Prefab.Parameters);
			}
			else if (valueType is WidgetAttributeValueTypeConstant)
			{
				ConstantDefinition constantValue = prefab.GetConstantValue(value2);
				if (constantValue == null)
				{
					Debug.FailedAssert("Unable to find definition of constant: " + value2, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetTemplate.cs", "SetAttributes", 383);
					return;
				}
				string value3 = constantValue.GetValue(brushFactory, spriteData, prefab.Constants, parameters, Prefab.Parameters);
				if (!string.IsNullOrEmpty(value3))
				{
					WidgetExtensions.SetWidgetAttributeFromString(widget, key2, value3, brushFactory, spriteData, prefab.VisualDefinitionTemplates, prefab.Constants, parameters, prefab.CustomElements, Prefab.Parameters);
				}
			}
			else if (valueType is WidgetAttributeValueTypeParameter)
			{
				string text = value2;
				string value4 = Prefab.GetParameterDefaultValue(text);
				if (parameters.TryGetValue(text, out var value5) && value5.ValueType is WidgetAttributeValueTypeDefault)
				{
					value4 = value5.Value;
				}
				if (!string.IsNullOrEmpty(value4))
				{
					WidgetExtensions.SetWidgetAttributeFromString(widget, key2, value4, brushFactory, spriteData, prefab.VisualDefinitionTemplates, prefab.Constants, parameters, prefab.CustomElements, Prefab.Parameters);
				}
			}
		}
		foreach (WidgetInstantiationResult child in widgetInstantiationResult.Children)
		{
			child.Template.SetAttributes(widgetCreationData, child, parameters);
		}
	}

	public static WidgetTemplate LoadFrom(PrefabExtensionContext prefabExtensionContext, WidgetAttributeContext widgetAttributeContext, XmlNode node)
	{
		WidgetTemplate widgetTemplate = new WidgetTemplate(node.Name);
		widgetTemplate.LoadAttributeCollection(widgetAttributeContext, node.Attributes);
		if (node.SelectSingleNode("LogicalChildrenLocation") != null)
		{
			widgetTemplate.LogicalChildrenLocation = true;
		}
		foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
		{
			prefabExtension.DoLoading(prefabExtensionContext, widgetAttributeContext, widgetTemplate, node);
		}
		XmlNode xmlNode = node.SelectSingleNode("Children");
		if (xmlNode != null)
		{
			foreach (XmlNode childNode in xmlNode.ChildNodes)
			{
				WidgetTemplate child = LoadFrom(prefabExtensionContext, widgetAttributeContext, childNode);
				widgetTemplate.AddChild(child);
			}
		}
		return widgetTemplate;
	}

	public void SetRootTemplate(WidgetPrefab prefab)
	{
		Prefab = prefab;
		foreach (WidgetTemplate child in _children)
		{
			child.SetRootTemplate(prefab);
		}
	}

	public void AddAttributeTo(WidgetAttributeContext widgetAttributeContext, string name, string value)
	{
		WidgetAttributeKeyType keyType = widgetAttributeContext.GetKeyType(name);
		string keyName = keyType.GetKeyName(name);
		WidgetAttributeValueType valueType = widgetAttributeContext.GetValueType(value);
		string attributeValue = valueType.GetAttributeValue(value);
		WidgetAttributeTemplate widgetAttributeTemplate = new WidgetAttributeTemplate();
		widgetAttributeTemplate.KeyType = keyType;
		widgetAttributeTemplate.ValueType = valueType;
		widgetAttributeTemplate.Key = keyName;
		widgetAttributeTemplate.Value = attributeValue;
		SetAttribute(widgetAttributeTemplate);
	}

	public void RemoveAttributeFrom(WidgetAttributeContext widgetAttributeContext, string fullName)
	{
		WidgetAttributeKeyType keyType = widgetAttributeContext.GetKeyType(fullName);
		string keyName = keyType.GetKeyName(fullName);
		RemoveAttributeFrom(keyType, keyName);
	}

	public void RemoveAttributeFrom<T>(string name) where T : WidgetAttributeKeyType
	{
		Dictionary<string, WidgetAttributeTemplate> dictionary = _attributes[typeof(T)];
		if (dictionary.ContainsKey(name))
		{
			dictionary.Remove(name);
		}
	}

	public void RemoveAttributeFrom(WidgetAttributeKeyType keyType, string name)
	{
		Dictionary<string, WidgetAttributeTemplate> dictionary = _attributes[keyType.GetType()];
		if (dictionary.ContainsKey(name))
		{
			dictionary.Remove(name);
		}
	}

	private static void AddAttributeTo(XmlNode node, string name, string value)
	{
		XmlAttribute xmlAttribute = node.OwnerDocument.CreateAttribute(name);
		xmlAttribute.InnerText = value.ToString();
		node.Attributes.Append(xmlAttribute);
	}

	public void Save(PrefabExtensionContext prefabExtensionContext, XmlNode parentNode)
	{
		XmlDocument ownerDocument = parentNode.OwnerDocument;
		XmlNode xmlNode = ownerDocument.CreateElement(_type);
		foreach (WidgetAttributeTemplate allAttribute in AllAttributes)
		{
			WidgetAttributeKeyType keyType = allAttribute.KeyType;
			WidgetAttributeValueType valueType = allAttribute.ValueType;
			string key = allAttribute.Key;
			string value = allAttribute.Value;
			string serializedKey = keyType.GetSerializedKey(key);
			string serializedValue = valueType.GetSerializedValue(value);
			AddAttributeTo(xmlNode, serializedKey, serializedValue);
		}
		foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
		{
			prefabExtension.OnSave(prefabExtensionContext, xmlNode, this);
		}
		if (_children.Count > 0)
		{
			XmlNode xmlNode2 = ownerDocument.CreateElement("Children");
			foreach (WidgetTemplate child in _children)
			{
				child.Save(prefabExtensionContext, xmlNode2);
			}
			xmlNode.AppendChild(xmlNode2);
		}
		parentNode.AppendChild(xmlNode);
	}
}
