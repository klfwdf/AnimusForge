using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Bannerlord.BUTR.Shared.Extensions;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.UIExtenderEx.Prefabs;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.Utils;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace Bannerlord.UIExtenderEx.Components;

internal class PrefabComponent
{
	internal sealed record PrefabPatch(Type Type, Action<XmlDocument> Patcher);

	private delegate Dictionary<string, string> GetPrefabNamesAndPathsFromCurrentPathDelegate(object instance);

	private delegate string StringSignature();

	private delegate XmlNode XmlNodeSignature();

	private delegate XmlDocument XmlDocumentSignature();

	private delegate IEnumerable<XmlNode> IEnumerableXmlNodeSignature();

	private static readonly GetPrefabNamesAndPathsFromCurrentPathDelegate? PrefabNamesMethod = AccessTools2.GetDeclaredDelegate<GetPrefabNamesAndPathsFromCurrentPathDelegate>(typeof(WidgetFactory), "GetPrefabNamesAndPathsFromCurrentPath");

	private readonly string _moduleName;

	internal readonly ConcurrentDictionary<string, List<PrefabPatch>> MoviePatches = new ConcurrentDictionary<string, List<PrefabPatch>>();

	private readonly ConcurrentDictionary<Type, bool> _enabledPatches = new ConcurrentDictionary<Type, bool>();

	private readonly Lazy<IReadOnlyList<Type>> _contentAttributeTypes = new Lazy<IReadOnlyList<Type>>(delegate
	{
		Type contentAttributeType = typeof(Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionContentAttribute);
		return (from t in contentAttributeType.Assembly.GetTypes()
			where !t.IsAbstract && contentAttributeType.IsAssignableFrom(t)
			select t).ToList();
	});

	public PrefabComponent(string moduleName)
	{
		_moduleName = moduleName;
	}

	public IEnumerable<string> GetMoviesToPatch()
	{
		foreach (var (text2, source) in MoviePatches)
		{
			if (source.Any((PrefabPatch x) => _enabledPatches.TryGetValue(x.Type, out var value) & value))
			{
				yield return text2;
			}
		}
	}

	public void Enable()
	{
		foreach (Type key in _enabledPatches.Keys)
		{
			_enabledPatches[key] = true;
		}
	}

	public void Disable()
	{
		foreach (Type key in _enabledPatches.Keys)
		{
			_enabledPatches[key] = false;
		}
	}

	public void Enable(Type prefabType)
	{
		if (_enabledPatches.ContainsKey(prefabType))
		{
			_enabledPatches[prefabType] = true;
		}
	}

	public void Disable(Type prefabType)
	{
		if (_enabledPatches.ContainsKey(prefabType))
		{
			_enabledPatches[prefabType] = false;
		}
	}

	public void RegisterPatch(string movie, Type prefabType, Action<XmlDocument> patcher)
	{
		if (string.IsNullOrEmpty(movie))
		{
			MessageUtils.Fail("Invalid movie name!");
			return;
		}
		MoviePatches.GetOrAdd(movie, (string _) => new List<PrefabPatch>()).Add(new PrefabPatch(prefabType, patcher));
		_enabledPatches[prefabType] = false;
	}

	public void RegisterPatch(string movie, Type prefabType, Action<XmlNode> patcher)
	{
		if (string.IsNullOrEmpty(movie))
		{
			MessageUtils.Fail("Invalid movie name!");
			return;
		}
		MoviePatches.GetOrAdd(movie, (string _) => new List<PrefabPatch>()).Add(new PrefabPatch(prefabType, patcher));
		_enabledPatches[prefabType] = false;
	}

	public void RegisterPatch(string movie, string? xpath, Type prefabType, Action<XmlNode> patcher)
	{
		RegisterPatch(movie, prefabType, delegate(XmlNode node)
		{
			XmlNode xmlNode = node.SelectSingleNode(xpath ?? string.Empty);
			if (xmlNode == null)
			{
				MessageUtils.DisplayUserError("Failed to apply extension to " + movie + ": node at " + xpath + " not found.");
			}
			else
			{
				patcher(xmlNode);
			}
		});
	}

	public void Deregister()
	{
		MoviePatches.Clear();
		_enabledPatches.Clear();
	}

	private static bool TryRemoveComments(XmlNode? node)
	{
		if (string.Equals(node?.Name, "#comment"))
		{
			return false;
		}
		XmlNodeList xmlNodeList = node?.SelectNodes("//comment()");
		if (xmlNodeList == null)
		{
			return false;
		}
		foreach (XmlNode item in xmlNodeList)
		{
			item.ParentNode.RemoveChild(item);
		}
		return true;
	}

	private static string? PathForMovie(string movie)
	{
		Dictionary<string, string> dictionary = PrefabNamesMethod?.Invoke(UIResourceManager.WidgetFactory);
		if (dictionary != null)
		{
			return dictionary[movie];
		}
		MessageUtils.DisplayUserError("UIExtenderEx could not find WidgetFactory.GetPrefabNamesAndPathsFromCurrentPath!");
		return null;
	}

	public void ProcessMovieIfNeeded(string movie, XmlDocument document)
	{
		if (!MoviePatches.TryGetValue(movie, out List<PrefabPatch> value) || _enabledPatches.Values.All((bool x) => !x))
		{
			return;
		}
		foreach (var (key, action2) in value)
		{
			if (_enabledPatches.TryGetValue(key, out var value2) && value2)
			{
				action2(document);
			}
		}
		if (UIExtenderExSettings.Instance.DumpXML)
		{
			DumpXml(_moduleName, movie, document);
		}
	}

	private static void DumpXml(string moduleName, string movie, XmlDocument document)
	{
		ModuleInfoExtendedHelper moduleByType = ModuleInfoHelper.GetModuleByType(typeof(SubModule));
		if ((object)moduleByType == null)
		{
			return;
		}
		string fileName = Path.Combine(moduleByType.Path, "Dumps", movie + "_" + moduleName + ".xml");
		FileInfo fileInfo = new FileInfo(fileName);
		fileInfo.Directory?.Create();
		using FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);
		fileStream.SetLength(0L);
		using StreamWriter output = new StreamWriter(fileStream);
		using XmlWriter w = XmlWriter.Create(output, new XmlWriterSettings
		{
			Indent = true,
			IndentChars = "  ",
			NewLineChars = Environment.NewLine,
			NewLineHandling = NewLineHandling.Replace
		});
		document.Save(w);
	}

	[Obsolete("Use Prefabs2.PrefabExtensionInsertPatch instead.")]
	public void RegisterPatch(string movie, string? xpath, Bannerlord.UIExtenderEx.Prefabs.PrefabExtensionInsertPatch patch)
	{
		RegisterPatch(movie, xpath, patch.GetType(), delegate(XmlNode node)
		{
			XmlDocument ownerDocument = node.OwnerDocument;
			if (ownerDocument == null)
			{
				MessageUtils.Fail("XML original document for " + movie + " is null!");
			}
			else
			{
				XmlElement documentElement = patch.GetPrefabExtension().DocumentElement;
				if (documentElement == null)
				{
					MessageUtils.Fail("XML patch document for " + movie + " is null!");
				}
				else if (!TryRemoveComments(documentElement))
				{
					MessageUtils.Fail("XML patch document's root node was a comment.");
				}
				else
				{
					XmlNode newChild = ownerDocument.ImportNode(documentElement, deep: true);
					int val = Math.Min(patch.Position, node.ChildNodes.Count - 1);
					val = Math.Max(val, 0);
					if (val >= node.ChildNodes.Count)
					{
						MessageUtils.Fail($"Invalid position ({val}) for insert (patching in {patch.Id})");
					}
					else
					{
						node.InsertAfter(newChild, node.ChildNodes[val]);
					}
				}
			}
		});
	}

	public void RegisterPatch(string movie, string? xpath, Bannerlord.UIExtenderEx.Prefabs.PrefabExtensionSetAttributePatch patch)
	{
		RegisterPatch(movie, xpath, patch.GetType(), delegate(XmlNode node)
		{
			XmlDocument ownerDocument = node.OwnerDocument;
			if (ownerDocument != null && node.NodeType == XmlNodeType.Element)
			{
				if (node.Attributes[patch.Attribute] == null)
				{
					XmlAttribute node2 = ownerDocument.CreateAttribute(patch.Attribute);
					node.Attributes.Append(node2);
				}
				node.Attributes[patch.Attribute].Value = patch.Value;
			}
		});
	}

	public void RegisterPatch(string movie, string? xpath, PrefabExtensionReplacePatch patch)
	{
		RegisterPatch(movie, xpath, patch.GetType(), delegate(XmlNode node)
		{
			XmlDocument ownerDocument = node.OwnerDocument;
			if (ownerDocument == null)
			{
				MessageUtils.Fail("XML original document for " + movie + " is null!");
			}
			else if (node.ParentNode == null)
			{
				MessageUtils.Fail("XML original document parent node for " + movie + " is null!");
			}
			else
			{
				XmlElement documentElement = patch.GetPrefabExtension().DocumentElement;
				if (documentElement == null)
				{
					MessageUtils.Fail("XML patch document for " + movie + " is null!");
				}
				else if (!TryRemoveComments(documentElement))
				{
					MessageUtils.Fail("XML patch document's root node was a comment.");
				}
				else
				{
					XmlNode newChild = ownerDocument.ImportNode(documentElement, deep: true);
					node.ParentNode.ReplaceChild(newChild, node);
				}
			}
		});
	}

	public void RegisterPatch(string movie, string? xpath, PrefabExtensionInsertAsSiblingPatch patch)
	{
		RegisterPatch(movie, xpath, patch.GetType(), delegate(XmlNode node)
		{
			XmlDocument ownerDocument = node.OwnerDocument;
			if (ownerDocument == null)
			{
				MessageUtils.Fail("XML original document for " + movie + " is null!");
			}
			else if (node.ParentNode == null)
			{
				MessageUtils.Fail("XML original document parent node for " + movie + " is null!");
			}
			else
			{
				XmlElement documentElement = patch.GetPrefabExtension().DocumentElement;
				if (documentElement == null)
				{
					MessageUtils.Fail("XML patch document for " + movie + " is null!");
				}
				else if (!TryRemoveComments(documentElement))
				{
					MessageUtils.Fail("XML patch document's root node was a comment.");
				}
				else
				{
					XmlNode newChild = ownerDocument.ImportNode(documentElement, deep: true);
					switch (patch.Type)
					{
					case PrefabExtensionInsertAsSiblingPatch.InsertType.Append:
						node.ParentNode.InsertAfter(newChild, node);
						break;
					case PrefabExtensionInsertAsSiblingPatch.InsertType.Prepend:
						node.ParentNode.InsertBefore(newChild, node);
						break;
					}
				}
			}
		});
	}

	public void RegisterPatch(string movie, string? xpath, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch patch)
	{
		RegisterPatch(movie, xpath, patch.GetType(), delegate(XmlNode node)
		{
			XmlDocument ownerDocument = node.OwnerDocument;
			IEnumerable<XmlNode> nodes;
			string errorMessage;
			if (ownerDocument == null)
			{
				MessageUtils.Fail("XML original document for " + movie + " is null!");
			}
			else if (!TryGetNodes(patch, out nodes, out errorMessage))
			{
				MessageUtils.Fail(errorMessage);
			}
			else if (patch.Type != InsertType.Child && node.ParentNode == null)
			{
				MessageUtils.Fail("Trying to place multiple root nodes into " + movie + "!");
			}
			else
			{
				XmlNode xmlNode = null;
				XmlNodeList oldChildNodes = null;
				XmlNode[] array = nodes.ToArray();
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					XmlNode node2 = array[i];
					if (TryRemoveComments(node2))
					{
						XmlNode xmlNode2 = ownerDocument.ImportNode(node2, deep: true);
						if (!flag)
						{
							flag = true;
							xmlNode = patch.Type switch
							{
								InsertType.Prepend => node.ParentNode.InsertBefore(xmlNode2, node), 
								InsertType.ReplaceKeepChildren => ReplaceKeepChildren(node, xmlNode2, patch.Index == 0 || array.Length == 1, out oldChildNodes), 
								InsertType.Replace => ReplaceNode(node, xmlNode2), 
								InsertType.Child => InsertAsChild(node, xmlNode2, patch.Index), 
								InsertType.Append => node.ParentNode.InsertAfter(xmlNode2, node), 
								InsertType.Remove => node.ParentNode.RemoveChild(node), 
								_ => throw new ArgumentOutOfRangeException(), 
							};
						}
						else
						{
							XmlNode xmlNode3 = xmlNode.ParentNode.InsertAfter(xmlNode2, xmlNode);
							if (patch.Type == InsertType.ReplaceKeepChildren && oldChildNodes != null && patch.Index == i)
							{
								foreach (XmlNode item in oldChildNodes)
								{
									xmlNode3.AppendChild(item);
								}
							}
							xmlNode = xmlNode3;
						}
					}
				}
			}
		});
	}

	private static XmlNode ReplaceNode(XmlNode targetNode, XmlNode importedNode)
	{
		targetNode.ParentNode.ReplaceChild(importedNode, targetNode);
		return importedNode;
	}

	private static XmlNode ReplaceKeepChildren(XmlNode targetNode, XmlNode importedNode, bool appendChildren, out XmlNodeList oldChildNodes)
	{
		oldChildNodes = targetNode.ChildNodes;
		targetNode.ParentNode.ReplaceChild(importedNode, targetNode);
		if (appendChildren)
		{
			while (oldChildNodes.Count > 0)
			{
				importedNode.AppendChild(oldChildNodes.Item(0));
			}
		}
		return importedNode;
	}

	private static XmlNode InsertAsChild(XmlNode targetNode, XmlNode importedNode, int index)
	{
		if (targetNode.ChildNodes.Count == 0)
		{
			return targetNode.AppendChild(importedNode);
		}
		if (index >= targetNode.ChildNodes.Count)
		{
			return targetNode.InsertAfter(importedNode, targetNode.ChildNodes[targetNode.ChildNodes.Count - 1]);
		}
		return targetNode.InsertBefore(importedNode, targetNode.ChildNodes[Math.Max(0, index)]);
	}

	private bool TryGetNodes(Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch patch, out IEnumerable<XmlNode>? nodes, out string errorMessage)
	{
		nodes = null;
		Type type = patch.GetType();
		MemberInfo[] contentMembers = (from m in type.GetMembers()
			where _contentAttributeTypes.Value.Any((Type t) => Attribute.GetCustomAttribute(m, t) != null)
			select m).ToArray();
		if (contentMembers.Length != 1)
		{
			errorMessage = $"{patch.GetType().Name} contains {contentMembers.Length} members with Content Attributes. " + "Insertion Patches must contain a single property or method with a PrefabExtensionContentAttribute.";
			return false;
		}
		Attribute[] array = (from t in _contentAttributeTypes.Value
			select Attribute.GetCustomAttribute(contentMembers[0], t) into a
			where a != null
			select a).ToArray();
		if (array.Length != 1)
		{
			errorMessage = $"{contentMembers[0].Name} in {patch.GetType().Name} contains {array.Length} attributes of type " + "PrefabExtensionContentAttribute. Should only have a single Content attribute.";
			return false;
		}
		errorMessage = contentMembers[0].Name + " in " + patch.GetType().Name + " ";
		Attribute attribute = array[0];
		IEnumerable<XmlNode> nodes2;
		if (!(attribute is Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionXmlDocumentAttribute attribute2))
		{
			if (!(attribute is Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionXmlNodeAttribute attribute3))
			{
				if (!(attribute is Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionXmlNodesAttribute attribute4))
				{
					if (!(attribute is Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionTextAttribute attribute5))
					{
						if (!(attribute is Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionFileNameAttribute attribute6))
						{
							throw new ArgumentOutOfRangeException("contentAttributes", array[0], null);
						}
						nodes2 = GetNodes(contentMembers[0], attribute6, patch, ref errorMessage);
					}
					else
					{
						nodes2 = GetNodes(contentMembers[0], attribute5, patch, ref errorMessage);
					}
				}
				else
				{
					nodes2 = GetNodes(contentMembers[0], attribute4, patch, ref errorMessage);
				}
			}
			else
			{
				nodes2 = GetNodes(contentMembers[0], attribute3, patch, ref errorMessage);
			}
		}
		else
		{
			nodes2 = GetNodes(contentMembers[0], attribute2, patch, ref errorMessage);
		}
		nodes = nodes2;
		if (nodes == null)
		{
			return false;
		}
		errorMessage = "";
		return true;
	}

	private static IEnumerable<XmlNode>? GetNodes(MemberInfo contentMemberInfo, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionXmlDocumentAttribute attribute, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch instance, ref string errorMessage)
	{
		if (!TryGetContent<XmlDocument>(contentMemberInfo, instance, ref errorMessage, out XmlDocument output) || output == null)
		{
			return null;
		}
		if (!attribute.RemoveRootNode)
		{
			return new List<XmlNode> { output.DocumentElement };
		}
		return output.DocumentElement.ChildNodes.Cast<XmlNode>();
	}

	private static IEnumerable<XmlNode>? GetNodes(MemberInfo contentMemberInfo, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionXmlNodeAttribute attribute, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch instance, ref string errorMessage)
	{
		if (!TryGetContent<XmlNode>(contentMemberInfo, instance, ref errorMessage, out XmlNode output) || output == null)
		{
			return null;
		}
		if (output is XmlDocument xmlDocument)
		{
			output = xmlDocument.DocumentElement;
		}
		if (!attribute.RemoveRootNode)
		{
			return new List<XmlNode> { output };
		}
		return output.ChildNodes.Cast<XmlNode>();
	}

	private static IEnumerable<XmlNode>? GetNodes(MemberInfo contentMemberInfo, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionXmlNodesAttribute attribute, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch instance, ref string errorMessage)
	{
		XmlNode[] array = ((!TryGetContent<IEnumerable<XmlNode>>(contentMemberInfo, instance, ref errorMessage, out IEnumerable<XmlNode> output)) ? null : output.ToArray());
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] is XmlDocument xmlDocument)
				{
					array[i] = xmlDocument.DocumentElement;
				}
			}
		}
		return array;
	}

	private static IEnumerable<XmlNode>? GetNodes(MemberInfo contentMemberInfo, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionTextAttribute attribute, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch instance, ref string errorMessage)
	{
		if (!TryGetContent<string>(contentMemberInfo, instance, ref errorMessage, out string output) || output == null)
		{
			return null;
		}
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			xmlDocument.LoadXml(output);
		}
		catch (XmlException arg)
		{
			errorMessage += $"failed to load or parse. Exception: {arg}";
			return null;
		}
		if (!attribute.RemoveRootNode)
		{
			return new List<XmlNode> { xmlDocument.DocumentElement };
		}
		return xmlDocument.DocumentElement.ChildNodes.Cast<XmlNode>();
	}

	private IEnumerable<XmlNode>? GetNodes(MemberInfo contentMemberInfo, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch.PrefabExtensionFileNameAttribute attribute, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch instance, ref string errorMessage)
	{
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			if (!TryGetContent<string>(contentMemberInfo, instance, ref errorMessage, out string fileName) || fileName == null)
			{
				return null;
			}
			fileName = Path.GetFileNameWithoutExtension(fileName);
			ModuleInfoExtendedHelper moduleInfoExtendedHelper = ModuleInfoHelper.LoadFromId(_moduleName);
			if ((object)moduleInfoExtendedHelper == null)
			{
				errorMessage = errorMessage + "Module " + _moduleName + " is not found.";
				return null;
			}
			string path = Path.Combine(ModuleInfoHelper.GetModulePath(moduleInfoExtendedHelper), "GUI");
			string[] files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);
			files = files.Where((string x) => string.Equals(Path.GetFileNameWithoutExtension(x), fileName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			if (files.Length != 1)
			{
				errorMessage += $"Found {files.Length} files matching {fileName}.";
				return null;
			}
			xmlDocument.Load(files[0]);
		}
		catch (Exception arg)
		{
			errorMessage += $"exception was thrown while loading the document. Exception: {arg}";
			return null;
		}
		if (!attribute.RemoveRootNode)
		{
			return new List<XmlNode> { xmlDocument.DocumentElement };
		}
		return xmlDocument.DocumentElement.ChildNodes.Cast<XmlNode>();
	}

	private static bool TryGetContent<T>(MemberInfo memberInfo, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch instance, ref string errorMessage, out T? output)
	{
		output = default(T);
		object obj = GetFunction(typeof(T), instance, memberInfo)();
		if (obj == null)
		{
			Type type = ((memberInfo is PropertyInfo propertyInfo) ? propertyInfo.PropertyType : ((MethodInfo)memberInfo).ReturnType);
			errorMessage = errorMessage + "is of type: " + type.Name + ". A Member flagged with a Content attribute must be of one of the types listed in PrefabExtensionContentAttribute";
			return false;
		}
		if (!(obj is T val))
		{
			Type type2 = ((memberInfo is PropertyInfo propertyInfo2) ? propertyInfo2.PropertyType : ((MethodInfo)memberInfo).ReturnType);
			errorMessage = errorMessage + "is of type: " + type2.Name + ", while its attribute type expects a " + typeof(T).Name + ". See PrefabExtensionContentAttribute for more information.";
			return false;
		}
		errorMessage = "";
		output = val;
		return true;
	}

	public void RegisterPatch(string movie, string? xpath, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch patch)
	{
		RegisterPatch(movie, xpath, patch.GetType(), delegate(XmlNode node)
		{
			XmlDocument ownerDocument = node.OwnerDocument;
			if (ownerDocument == null || node.NodeType != XmlNodeType.Element)
			{
				return;
			}
			foreach (Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute attribute in patch.Attributes)
			{
				if (node.Attributes[attribute.Name] == null)
				{
					XmlAttribute node2 = ownerDocument.CreateAttribute(attribute.Name);
					node.Attributes.Append(node2);
				}
				node.Attributes[attribute.Name].Value = attribute.Value;
			}
		});
	}

	private static Func<object?> GetFunction(Type returnType, Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch instance, MemberInfo memberInfo)
	{
		MethodInfo methodInfo = ((memberInfo is PropertyInfo propertyInfo) ? propertyInfo.GetMethod : ((!(memberInfo is MethodInfo methodInfo2)) ? null : methodInfo2));
		MethodInfo methodInfo3 = methodInfo;
		if ((object)methodInfo3 == null)
		{
			return () => (object?)null;
		}
		if (returnType == typeof(string))
		{
			StringSignature @delegate = Delegate.CreateDelegate(typeof(StringSignature), instance, methodInfo3) as StringSignature;
			return () => @delegate?.Invoke();
		}
		if (returnType == typeof(XmlNode))
		{
			XmlNodeSignature delegate2 = Delegate.CreateDelegate(typeof(XmlNodeSignature), instance, methodInfo3) as XmlNodeSignature;
			return () => delegate2?.Invoke();
		}
		if (returnType == typeof(XmlDocument))
		{
			XmlDocumentSignature delegate3 = Delegate.CreateDelegate(typeof(XmlDocumentSignature), instance, methodInfo3) as XmlDocumentSignature;
			return () => delegate3?.Invoke();
		}
		if (returnType == typeof(IEnumerable<XmlNode>))
		{
			IEnumerableXmlNodeSignature delegate4 = Delegate.CreateDelegate(typeof(IEnumerableXmlNodeSignature), instance, methodInfo3) as IEnumerableXmlNodeSignature;
			return () => delegate4?.Invoke();
		}
		return () => (object?)null;
	}
}
