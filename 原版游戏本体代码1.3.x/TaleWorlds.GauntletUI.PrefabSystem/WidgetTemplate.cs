using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200001D RID: 29
	public class WidgetTemplate
	{
		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000E7 RID: 231 RVA: 0x00004F21 File Offset: 0x00003121
		// (set) Token: 0x060000E8 RID: 232 RVA: 0x00004F29 File Offset: 0x00003129
		public bool LogicalChildrenLocation { get; private set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x00004F34 File Offset: 0x00003134
		public string Id
		{
			get
			{
				WidgetAttributeTemplate firstAttributeIfExist = this.GetFirstAttributeIfExist<WidgetAttributeKeyTypeId>();
				if (firstAttributeIfExist != null)
				{
					return firstAttributeIfExist.Value;
				}
				return "";
			}
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000EA RID: 234 RVA: 0x00004F57 File Offset: 0x00003157
		public string Type
		{
			get
			{
				return this._type;
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000EB RID: 235 RVA: 0x00004F5F File Offset: 0x0000315F
		public int ChildCount
		{
			get
			{
				return this._children.Count;
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000EC RID: 236 RVA: 0x00004F6C File Offset: 0x0000316C
		public Dictionary<string, WidgetAttributeTemplate> GivenParameters
		{
			get
			{
				return this.GetAttributesOf<WidgetAttributeKeyTypeParameter>().ToDictionary((WidgetAttributeTemplate key) => key.Key);
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000ED RID: 237 RVA: 0x00004F98 File Offset: 0x00003198
		// (set) Token: 0x060000EE RID: 238 RVA: 0x00004FA0 File Offset: 0x000031A0
		public WidgetPrefab Prefab { get; private set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000EF RID: 239 RVA: 0x00004FA9 File Offset: 0x000031A9
		public WidgetTemplate RootTemplate
		{
			get
			{
				return this.Prefab.RootTemplate;
			}
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000F0 RID: 240 RVA: 0x00004FB6 File Offset: 0x000031B6
		public Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>> Attributes
		{
			get
			{
				return this._attributes;
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x00004FBE File Offset: 0x000031BE
		// (set) Token: 0x060000F2 RID: 242 RVA: 0x00004FC6 File Offset: 0x000031C6
		public object Tag { get; set; }

		// Token: 0x060000F3 RID: 243 RVA: 0x00004FD0 File Offset: 0x000031D0
		public WidgetTemplate(string type)
		{
			this._type = type;
			this._extensionData = new Dictionary<string, object>();
			this.Tag = Guid.NewGuid();
			this._attributes = new Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>>();
			this._children = new List<WidgetTemplate>();
			this._customTypeChildren = new List<WidgetTemplate>();
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00005028 File Offset: 0x00003228
		internal void LoadAttributeCollection(WidgetAttributeContext widgetAttributeContext, XmlAttributeCollection attributes)
		{
			foreach (WidgetAttributeKeyType widgetAttributeKeyType in widgetAttributeContext.RegisteredKeyTypes)
			{
				this._attributes.Add(widgetAttributeKeyType.GetType(), new Dictionary<string, WidgetAttributeTemplate>());
			}
			foreach (object obj in attributes)
			{
				XmlAttribute xmlAttribute = (XmlAttribute)obj;
				string name = xmlAttribute.Name;
				string value = xmlAttribute.Value;
				this.AddAttributeTo(widgetAttributeContext, name, value);
			}
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x000050DC File Offset: 0x000032DC
		public void AddExtensionData(string name, object data)
		{
			if (this._extensionData.ContainsKey(name))
			{
				this._extensionData[name] = data;
				return;
			}
			this._extensionData.Add(name, data);
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00005108 File Offset: 0x00003308
		public T GetExtensionData<T>(string name) where T : class
		{
			object obj;
			this._extensionData.TryGetValue(name, out obj);
			return obj as T;
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000512F File Offset: 0x0000332F
		public void RemoveExtensionData(string name)
		{
			this._extensionData.Remove(name);
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000513E File Offset: 0x0000333E
		public void AddExtensionData(object data)
		{
			this.AddExtensionData(data.GetType().Name, data);
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00005152 File Offset: 0x00003352
		public T GetExtensionData<T>() where T : class
		{
			return this.GetExtensionData<T>(typeof(T).Name);
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00005169 File Offset: 0x00003369
		public void RemoveExtensionData<T>() where T : class
		{
			this.RemoveExtensionData(typeof(T).Name);
		}

		// Token: 0x060000FB RID: 251 RVA: 0x00005180 File Offset: 0x00003380
		public IEnumerable<WidgetAttributeTemplate> GetAttributesOf<T>() where T : WidgetAttributeKeyType
		{
			return this._attributes[typeof(T)].Values;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000519C File Offset: 0x0000339C
		public IEnumerable<WidgetAttributeTemplate> GetAttributesOf<TKey, TValue>() where TKey : WidgetAttributeKeyType where TValue : WidgetAttributeValueType
		{
			IEnumerable<WidgetAttributeTemplate> attributesOf = this.GetAttributesOf<TKey>();
			foreach (WidgetAttributeTemplate widgetAttributeTemplate in attributesOf)
			{
				if (widgetAttributeTemplate.ValueType is TValue)
				{
					yield return widgetAttributeTemplate;
				}
			}
			IEnumerator<WidgetAttributeTemplate> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000FD RID: 253 RVA: 0x000051AC File Offset: 0x000033AC
		public IEnumerable<WidgetAttributeTemplate> AllAttributes
		{
			get
			{
				foreach (Dictionary<string, WidgetAttributeTemplate> dictionary in this._attributes.Values)
				{
					foreach (WidgetAttributeTemplate widgetAttributeTemplate in dictionary.Values)
					{
						yield return widgetAttributeTemplate;
					}
					Dictionary<string, WidgetAttributeTemplate>.ValueCollection.Enumerator enumerator2 = default(Dictionary<string, WidgetAttributeTemplate>.ValueCollection.Enumerator);
				}
				Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>>.ValueCollection.Enumerator enumerator = default(Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>>.ValueCollection.Enumerator);
				yield break;
				yield break;
			}
		}

		// Token: 0x060000FE RID: 254 RVA: 0x000051BC File Offset: 0x000033BC
		public WidgetAttributeTemplate GetFirstAttributeIfExist<T>() where T : WidgetAttributeKeyType
		{
			using (IEnumerator<WidgetAttributeTemplate> enumerator = this.GetAttributesOf<T>().GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}
			return null;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00005208 File Offset: 0x00003408
		public void SetAttribute(WidgetAttributeTemplate attribute)
		{
			Dictionary<string, WidgetAttributeTemplate> dictionary = this._attributes[attribute.KeyType.GetType()];
			if (dictionary.ContainsKey(attribute.Key))
			{
				dictionary[attribute.Key] = attribute;
				return;
			}
			dictionary.Add(attribute.Key, attribute);
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00005255 File Offset: 0x00003455
		public WidgetTemplate GetChildAt(int i)
		{
			return this._children[i];
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00005263 File Offset: 0x00003463
		public void AddChild(WidgetTemplate child)
		{
			this._children.Add(child);
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00005271 File Offset: 0x00003471
		public void RemoveChild(WidgetTemplate child)
		{
			this._children.Remove(child);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00005280 File Offset: 0x00003480
		public void SwapChildren(WidgetTemplate child1, WidgetTemplate child2)
		{
			int index = this._children.IndexOf(child1);
			int index2 = this._children.IndexOf(child2);
			WidgetTemplate value = this._children[index];
			this._children[index] = this._children[index2];
			this._children[index2] = value;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x000052DC File Offset: 0x000034DC
		public WidgetInstantiationResult Instantiate(WidgetCreationData widgetCreationData, Dictionary<string, WidgetAttributeTemplate> parameters)
		{
			PrefabExtensionContext prefabExtensionContext = widgetCreationData.PrefabExtensionContext;
			WidgetInstantiationResult widgetInstantiationResult = this.CreateWidgets(widgetCreationData);
			this.SetAttributes(widgetCreationData, widgetInstantiationResult, parameters);
			foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
			{
				prefabExtension.AfterAttributesSet(widgetCreationData, widgetInstantiationResult, parameters);
			}
			return widgetInstantiationResult;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00005340 File Offset: 0x00003540
		private WidgetInstantiationResult CreateWidgets(WidgetCreationData widgetCreationData)
		{
			this._usedFactory = widgetCreationData.WidgetFactory;
			PrefabExtensionContext prefabExtensionContext = this._usedFactory.PrefabExtensionContext;
			UIContext context = widgetCreationData.Context;
			Widget widget = null;
			Widget parent = widgetCreationData.Parent;
			WidgetInstantiationResult widgetInstantiationResult = null;
			WidgetInstantiationResult widgetInstantiationResult3;
			if (this._usedFactory.IsCustomType(this._type))
			{
				WidgetInstantiationResult widgetInstantiationResult2 = this._usedFactory.GetCustomType(this._type).RootTemplate.CreateWidgets(widgetCreationData);
				this._customTypeChildren.AddRange(from c in widgetInstantiationResult2.Children
					select c.Template);
				widget = widgetInstantiationResult2.Widget;
				widgetInstantiationResult = new WidgetInstantiationResult(widget, this, widgetInstantiationResult2);
				widgetInstantiationResult3 = widgetInstantiationResult.GetLogicalOrDefaultChildrenLocation();
			}
			else
			{
				widget = this._usedFactory.CreateBuiltinWidget(context, this._type);
				widgetInstantiationResult = new WidgetInstantiationResult(widget, this);
				if (parent != null)
				{
					parent.AddChild(widget);
				}
				foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
				{
					prefabExtension.OnWidgetCreated(widgetCreationData, widgetInstantiationResult, this.ChildCount);
				}
				widgetInstantiationResult3 = widgetInstantiationResult;
			}
			widget.Tag = this.Tag;
			widget.Id = this.Id;
			foreach (WidgetTemplate widgetTemplate in this._children)
			{
				WidgetCreationData widgetCreationData2 = new WidgetCreationData(widgetCreationData, widgetInstantiationResult3);
				WidgetInstantiationResult item = widgetTemplate.CreateWidgets(widgetCreationData2);
				widgetInstantiationResult3.Children.Add(item);
			}
			return widgetInstantiationResult;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x000054EC File Offset: 0x000036EC
		public void OnRelease()
		{
			if (this._usedFactory.IsCustomType(this._type))
			{
				this._usedFactory.OnUnload(this._type);
			}
			foreach (WidgetTemplate widgetTemplate in this._children)
			{
				widgetTemplate.OnRelease();
			}
			foreach (WidgetTemplate widgetTemplate2 in this._customTypeChildren)
			{
				widgetTemplate2.OnRelease();
			}
		}

		// Token: 0x06000107 RID: 263 RVA: 0x000055A0 File Offset: 0x000037A0
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
				foreach (KeyValuePair<string, WidgetAttributeTemplate> keyValuePair in this.GivenParameters)
				{
					string key = keyValuePair.Key;
					WidgetAttributeTemplate widgetAttributeTemplate = keyValuePair.Value;
					WidgetAttributeTemplate widgetAttributeTemplate2;
					if (widgetAttributeTemplate.KeyType is WidgetAttributeKeyTypeParameter && widgetAttributeTemplate.ValueType is WidgetAttributeValueTypeParameter && parameters.TryGetValue(key, out widgetAttributeTemplate2))
					{
						widgetAttributeTemplate = widgetAttributeTemplate2;
					}
					dictionary.Add(key, widgetAttributeTemplate);
				}
				template.SetAttributes(widgetCreationData, customWidgetInstantiationData, dictionary);
			}
			foreach (WidgetAttributeTemplate widgetAttributeTemplate3 in this.AllAttributes)
			{
				WidgetAttributeKeyType keyType = widgetAttributeTemplate3.KeyType;
				WidgetAttributeValueType valueType = widgetAttributeTemplate3.ValueType;
				string key2 = widgetAttributeTemplate3.Key;
				string value = widgetAttributeTemplate3.Value;
				if (keyType is WidgetAttributeKeyTypeAttribute)
				{
					if (valueType is WidgetAttributeValueTypeDefault)
					{
						WidgetExtensions.SetWidgetAttributeFromString(widget, key2, value, brushFactory, spriteData, prefab.VisualDefinitionTemplates, prefab.Constants, parameters, prefab.CustomElements, this.Prefab.Parameters);
					}
					else if (valueType is WidgetAttributeValueTypeConstant)
					{
						ConstantDefinition constantValue = prefab.GetConstantValue(value);
						if (constantValue == null)
						{
							Debug.FailedAssert("Unable to find definition of constant: " + value, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetTemplate.cs", "SetAttributes", 383);
							return;
						}
						string value2 = constantValue.GetValue(brushFactory, spriteData, prefab.Constants, parameters, this.Prefab.Parameters);
						if (!string.IsNullOrEmpty(value2))
						{
							WidgetExtensions.SetWidgetAttributeFromString(widget, key2, value2, brushFactory, spriteData, prefab.VisualDefinitionTemplates, prefab.Constants, parameters, prefab.CustomElements, this.Prefab.Parameters);
						}
					}
					else if (valueType is WidgetAttributeValueTypeParameter)
					{
						string text = value;
						string value3 = this.Prefab.GetParameterDefaultValue(text);
						WidgetAttributeTemplate widgetAttributeTemplate4;
						if (parameters.TryGetValue(text, out widgetAttributeTemplate4) && widgetAttributeTemplate4.ValueType is WidgetAttributeValueTypeDefault)
						{
							value3 = widgetAttributeTemplate4.Value;
						}
						if (!string.IsNullOrEmpty(value3))
						{
							WidgetExtensions.SetWidgetAttributeFromString(widget, key2, value3, brushFactory, spriteData, prefab.VisualDefinitionTemplates, prefab.Constants, parameters, prefab.CustomElements, this.Prefab.Parameters);
						}
					}
				}
			}
			foreach (WidgetInstantiationResult widgetInstantiationResult2 in widgetInstantiationResult.Children)
			{
				widgetInstantiationResult2.Template.SetAttributes(widgetCreationData, widgetInstantiationResult2, parameters);
			}
		}

		// Token: 0x06000108 RID: 264 RVA: 0x000058F4 File Offset: 0x00003AF4
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
				foreach (object obj in xmlNode.ChildNodes)
				{
					XmlNode node2 = (XmlNode)obj;
					WidgetTemplate child = WidgetTemplate.LoadFrom(prefabExtensionContext, widgetAttributeContext, node2);
					widgetTemplate.AddChild(child);
				}
			}
			return widgetTemplate;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x000059D8 File Offset: 0x00003BD8
		public void SetRootTemplate(WidgetPrefab prefab)
		{
			this.Prefab = prefab;
			foreach (WidgetTemplate widgetTemplate in this._children)
			{
				widgetTemplate.SetRootTemplate(prefab);
			}
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00005A30 File Offset: 0x00003C30
		public void AddAttributeTo(WidgetAttributeContext widgetAttributeContext, string name, string value)
		{
			WidgetAttributeKeyType keyType = widgetAttributeContext.GetKeyType(name);
			string keyName = keyType.GetKeyName(name);
			WidgetAttributeValueType valueType = widgetAttributeContext.GetValueType(value);
			string attributeValue = valueType.GetAttributeValue(value);
			this.SetAttribute(new WidgetAttributeTemplate
			{
				KeyType = keyType,
				ValueType = valueType,
				Key = keyName,
				Value = attributeValue
			});
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00005A8C File Offset: 0x00003C8C
		public void RemoveAttributeFrom(WidgetAttributeContext widgetAttributeContext, string fullName)
		{
			WidgetAttributeKeyType keyType = widgetAttributeContext.GetKeyType(fullName);
			string keyName = keyType.GetKeyName(fullName);
			this.RemoveAttributeFrom(keyType, keyName);
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00005AB4 File Offset: 0x00003CB4
		public void RemoveAttributeFrom<T>(string name) where T : WidgetAttributeKeyType
		{
			Dictionary<string, WidgetAttributeTemplate> dictionary = this._attributes[typeof(T)];
			if (dictionary.ContainsKey(name))
			{
				dictionary.Remove(name);
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00005AE8 File Offset: 0x00003CE8
		public void RemoveAttributeFrom(WidgetAttributeKeyType keyType, string name)
		{
			Dictionary<string, WidgetAttributeTemplate> dictionary = this._attributes[keyType.GetType()];
			if (dictionary.ContainsKey(name))
			{
				dictionary.Remove(name);
			}
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00005B18 File Offset: 0x00003D18
		private static void AddAttributeTo(XmlNode node, string name, string value)
		{
			XmlAttribute xmlAttribute = node.OwnerDocument.CreateAttribute(name);
			xmlAttribute.InnerText = value.ToString();
			node.Attributes.Append(xmlAttribute);
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00005B4C File Offset: 0x00003D4C
		public void Save(PrefabExtensionContext prefabExtensionContext, XmlNode parentNode)
		{
			XmlDocument ownerDocument = parentNode.OwnerDocument;
			XmlNode xmlNode = ownerDocument.CreateElement(this._type);
			foreach (WidgetAttributeTemplate widgetAttributeTemplate in this.AllAttributes)
			{
				WidgetAttributeKeyType keyType = widgetAttributeTemplate.KeyType;
				WidgetAttributeValueType valueType = widgetAttributeTemplate.ValueType;
				string key = widgetAttributeTemplate.Key;
				string value = widgetAttributeTemplate.Value;
				string serializedKey = keyType.GetSerializedKey(key);
				string serializedValue = valueType.GetSerializedValue(value);
				WidgetTemplate.AddAttributeTo(xmlNode, serializedKey, serializedValue);
			}
			foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
			{
				prefabExtension.OnSave(prefabExtensionContext, xmlNode, this);
			}
			if (this._children.Count > 0)
			{
				XmlNode xmlNode2 = ownerDocument.CreateElement("Children");
				foreach (WidgetTemplate widgetTemplate in this._children)
				{
					widgetTemplate.Save(prefabExtensionContext, xmlNode2);
				}
				xmlNode.AppendChild(xmlNode2);
			}
			parentNode.AppendChild(xmlNode);
		}

		// Token: 0x0400004B RID: 75
		private string _type;

		// Token: 0x0400004C RID: 76
		private WidgetFactory _usedFactory;

		// Token: 0x0400004D RID: 77
		private List<WidgetTemplate> _children;

		// Token: 0x0400004E RID: 78
		private List<WidgetTemplate> _customTypeChildren;

		// Token: 0x0400004F RID: 79
		private Dictionary<Type, Dictionary<string, WidgetAttributeTemplate>> _attributes;

		// Token: 0x04000052 RID: 82
		private Dictionary<string, object> _extensionData;
	}
}
