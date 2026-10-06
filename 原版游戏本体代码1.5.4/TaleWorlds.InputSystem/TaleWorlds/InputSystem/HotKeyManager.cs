using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public static class HotKeyManager
{
	public delegate void OnKeybindsChangedEvent();

	private static readonly Dictionary<string, GameKeyContext> _categories = new Dictionary<string, GameKeyContext>();

	private static readonly List<string> _serializeIgnoredCategories = new List<string>();

	private static readonly float _versionOfHotkeys = 5.1f;

	private static XmlNodeList _loadedCategories;

	private static bool _hotkeyEditEnabled = false;

	private static bool _notifyDocumentVersionDifferent = false;

	private static PlatformFilePath _savePath;

	private static bool _needsLoading;

	private static bool _needsSaving;

	private static bool _needsKeybindsChangedEvent;

	private static bool _isSaveLoadInProgress;

	public static event OnKeybindsChangedEvent OnKeybindsChanged;

	public static string GetHotKeyId(string categoryName, string hotKeyId)
	{
		if (_categories.TryGetValue(categoryName, out var value))
		{
			return value.GetHotKeyId(hotKeyId);
		}
		Debug.FailedAssert("Key category with id \"" + categoryName + "\" doesn't exsist.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "GetHotKeyId", 36);
		return "";
	}

	public static string GetHotKeyId(string categoryName, int hotKeyId)
	{
		if (_categories.TryGetValue(categoryName, out var value))
		{
			return value.GetHotKeyId(hotKeyId);
		}
		Debug.FailedAssert("Key category with id \"" + categoryName + "\" doesn't exsist.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "GetHotKeyId", 47);
		return "invalid";
	}

	public static GameKeyContext GetCategory(string categoryName)
	{
		return _categories[categoryName];
	}

	public static Dictionary<string, GameKeyContext>.ValueCollection GetAllCategories()
	{
		return _categories.Values;
	}

	public static void Tick(float dt)
	{
		if (!_isSaveLoadInProgress)
		{
			HandleSaveLoad();
		}
		if (!_isSaveLoadInProgress && _needsKeybindsChangedEvent)
		{
			HotKeyManager.OnKeybindsChanged?.Invoke();
			_needsKeybindsChangedEvent = false;
		}
	}

	private static async void HandleSaveLoad()
	{
		if (_needsLoading || _needsSaving)
		{
			_isSaveLoadInProgress = true;
			if (_needsLoading)
			{
				await LoadAsync();
				_needsLoading = false;
			}
			if (_needsSaving)
			{
				await SaveAsync();
				_needsSaving = false;
			}
			_isSaveLoadInProgress = false;
		}
	}

	public static void Initialize(PlatformFilePath savePath, bool isRDownSwappedWithRRight)
	{
		GameKeyContext.SetIsRDownSwappedWithRRight(isRDownSwappedWithRRight);
		_savePath = savePath;
	}

	public static void RegisterInitialContexts(IEnumerable<GameKeyContext> contexts)
	{
		_categories.Clear();
		foreach (GameKeyContext context in contexts)
		{
			RegisterContext(context, context.Type == GameKeyContext.GameKeyContextType.AuxiliaryNotSerialized);
		}
	}

	public static void RegisterContext(GameKeyContext context, bool ignoreSerialize = false)
	{
		if (!_categories.ContainsKey(context.GameKeyCategoryId))
		{
			_categories.Add(context.GameKeyCategoryId, context);
			_needsLoading = true;
		}
		if (ignoreSerialize && !_serializeIgnoredCategories.Contains(context.GameKeyCategoryId))
		{
			_serializeIgnoredCategories.Add(context.GameKeyCategoryId);
			_needsLoading = true;
		}
	}

	public static bool ShouldNotifyDocumentVersionDifferent()
	{
		bool notifyDocumentVersionDifferent = _notifyDocumentVersionDifferent;
		_notifyDocumentVersionDifferent = false;
		return notifyDocumentVersionDifferent;
	}

	public static void Reset()
	{
		foreach (GameKeyContext value in _categories.Values)
		{
			foreach (GameKey registeredGameKey in value.RegisteredGameKeys)
			{
				if (registeredGameKey != null)
				{
					registeredGameKey.ControllerKey?.ChangeKey(registeredGameKey.DefaultControllerKey?.InputKey ?? InputKey.Invalid);
					registeredGameKey.KeyboardKey?.ChangeKey(registeredGameKey.DefaultKeyboardKey?.InputKey ?? InputKey.Invalid);
				}
			}
			foreach (HotKey registeredHotKey in value.RegisteredHotKeys)
			{
				if (registeredHotKey == null)
				{
					continue;
				}
				registeredHotKey.Keys.Clear();
				foreach (Key defaultKey in registeredHotKey.DefaultKeys)
				{
					registeredHotKey.Keys.Add(new Key(defaultKey.InputKey));
				}
			}
			foreach (GameAxisKey registeredGameAxisKey in value.RegisteredGameAxisKeys)
			{
				registeredGameAxisKey.AxisKey.ChangeKey(registeredGameAxisKey.DefaultAxisKey.InputKey);
			}
		}
	}

	private static async Task LoadAsync()
	{
		_loadedCategories = null;
		if (!FileHelper.FileExists(_savePath))
		{
			return;
		}
		try
		{
			XmlDocument document = new XmlDocument();
			await document.LoadAsync(_savePath);
			XmlElement documentElement = document.DocumentElement;
			float num = 0f;
			if (documentElement.HasAttribute("hotkeyEditEnabled"))
			{
				_hotkeyEditEnabled = Convert.ToBoolean(documentElement.GetAttribute("hotkeyEditEnabled"));
			}
			if (documentElement.HasAttribute("version") && float.TryParse(documentElement.GetAttribute("version"), out var result))
			{
				num = result;
			}
			if (num != _versionOfHotkeys)
			{
				_notifyDocumentVersionDifferent = true;
				await SaveAsync();
				return;
			}
			_loadedCategories = documentElement.ChildNodes;
			foreach (XmlNode childNode in documentElement.ChildNodes)
			{
				string attribute = ((XmlElement)childNode).GetAttribute("name");
				if (!_categories.TryGetValue(attribute, out var value))
				{
					continue;
				}
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					string name = ((XmlElement)childNode2).Name;
					if (name == "GameKey")
					{
						string innerText = childNode2["Id"].InnerText;
						GameKey gameKey = value.GetGameKey(innerText);
						if (gameKey == null)
						{
							continue;
						}
						XmlElement xmlElement2 = childNode2["Keys"]["KeyboardKey"];
						if (xmlElement2 != null)
						{
							if (Enum.TryParse<InputKey>(xmlElement2.InnerText, out var result2))
							{
								if (gameKey.KeyboardKey != null)
								{
									gameKey.KeyboardKey.ChangeKey(result2);
								}
								else
								{
									gameKey.KeyboardKey = new Key(result2);
								}
							}
						}
						else if (gameKey.DefaultKeyboardKey != null && gameKey.DefaultKeyboardKey.InputKey != InputKey.Invalid)
						{
							gameKey.KeyboardKey = new Key(gameKey.DefaultKeyboardKey.InputKey);
						}
						else
						{
							gameKey.KeyboardKey = new Key(InputKey.Invalid);
						}
					}
					else
					{
						if (!_hotkeyEditEnabled && value.Type != GameKeyContext.GameKeyContextType.AuxiliarySerializedAndShownInOptions)
						{
							continue;
						}
						if (name == "GameAxisKey")
						{
							string innerText2 = childNode2["Id"].InnerText;
							GameAxisKey gameAxisKey = value.GetGameAxisKey(innerText2);
							if (gameAxisKey == null)
							{
								continue;
							}
							XmlElement xmlElement3 = childNode2["Keys"];
							if (!gameAxisKey.IsBinded)
							{
								XmlElement xmlElement4 = xmlElement3["PositiveKey"];
								if (xmlElement4 != null)
								{
									if (xmlElement4.InnerText != "None")
									{
										if (Enum.TryParse<InputKey>(xmlElement4.InnerText, out var result3))
										{
											gameAxisKey.PositiveKey = new GameKey(-1, gameAxisKey.Id + "_p", attribute, result3);
										}
									}
									else
									{
										gameAxisKey.PositiveKey = null;
									}
								}
								XmlElement xmlElement5 = xmlElement3["NegativeKey"];
								if (xmlElement5 != null)
								{
									if (xmlElement5.InnerText != "None")
									{
										if (Enum.TryParse<InputKey>(xmlElement5.InnerText, out var result4))
										{
											gameAxisKey.NegativeKey = new GameKey(-1, gameAxisKey.Id + "_n", attribute, result4);
										}
									}
									else
									{
										gameAxisKey.NegativeKey = null;
									}
								}
							}
							XmlElement xmlElement6 = xmlElement3["AxisKey"];
							if (xmlElement6 == null)
							{
								continue;
							}
							if (xmlElement6.InnerText != "None")
							{
								if (Enum.TryParse<InputKey>(xmlElement6.InnerText, out var result5))
								{
									gameAxisKey.AxisKey = new Key(result5);
								}
							}
							else
							{
								gameAxisKey.AxisKey = null;
							}
						}
						else
						{
							if (!(name == "HotKey"))
							{
								continue;
							}
							string innerText3 = childNode2["Id"].InnerText;
							HotKey hotKey = value.GetHotKey(innerText3);
							if (hotKey == null)
							{
								continue;
							}
							new List<HotKey>();
							XmlElement xmlElement7 = childNode2["Keys"];
							hotKey.Keys = new List<Key>();
							for (int i = 0; i < xmlElement7.ChildNodes.Count; i++)
							{
								if (Enum.TryParse<InputKey>(xmlElement7.ChildNodes[i].InnerText, out var result6))
								{
									hotKey.Keys.Add(new Key(result6));
								}
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Couldn't load key bindings: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "LoadAsync", 390);
			_loadedCategories = null;
		}
	}

	public static void MarkForSave()
	{
		_needsSaving = true;
	}

	private static async Task SaveAsync()
	{
		try
		{
			XmlDocument document = new XmlDocument();
			XmlDeclaration newChild = document.CreateXmlDeclaration("1.0", "UTF-8", null);
			XmlElement documentElement = document.DocumentElement;
			document.InsertBefore(newChild, documentElement);
			XmlComment newChild2 = document.CreateComment("To override values other than GameKeys, change hotkeyEditEnabled to True.");
			document.InsertBefore(newChild2, documentElement);
			XmlElement xmlElement = document.CreateElement("HotKeyCategories");
			xmlElement.SetAttribute("hotkeyEditEnabled", _hotkeyEditEnabled.ToString());
			float versionOfHotkeys = _versionOfHotkeys;
			xmlElement.SetAttribute("version", versionOfHotkeys.ToString());
			document.AppendChild(xmlElement);
			foreach (KeyValuePair<string, GameKeyContext> category in _categories)
			{
				if (!_serializeIgnoredCategories.Contains(category.Key))
				{
					XmlElement newChild3 = CreateGameKeyContextNode(category.Key, category.Value, ref document);
					xmlElement.AppendChild(newChild3);
				}
			}
			if (_loadedCategories != null)
			{
				foreach (XmlElement loadedCategory in _loadedCategories)
				{
					if (!_categories.TryGetValue(loadedCategory.GetAttribute("name"), out var _))
					{
						xmlElement.AppendChild(document.ImportNode(loadedCategory, deep: true));
					}
				}
			}
			await document.SaveAsync(_savePath);
			_needsKeybindsChangedEvent = true;
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Couldn't save key bindings: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "SaveAsync", 447);
		}
	}

	private static XmlElement CreateGameKeyContextNode(string name, GameKeyContext context, ref XmlDocument document)
	{
		XmlElement xmlElement = document.CreateElement("HotKeyCategory");
		xmlElement.SetAttribute("name", name);
		foreach (GameKey registeredGameKey in context.RegisteredGameKeys)
		{
			if (registeredGameKey != null)
			{
				XmlElement xmlElement2 = document.CreateElement("GameKey");
				xmlElement.AppendChild(xmlElement2);
				XmlElement xmlElement3 = document.CreateElement("Id");
				xmlElement2.AppendChild(xmlElement3);
				xmlElement3.InnerText = registeredGameKey.StringId;
				XmlElement xmlElement4 = document.CreateElement("Keys");
				xmlElement2.AppendChild(xmlElement4);
				XmlElement xmlElement5 = document.CreateElement("KeyboardKey");
				xmlElement4.AppendChild(xmlElement5);
				xmlElement5.InnerText = ((registeredGameKey.KeyboardKey != null) ? registeredGameKey.KeyboardKey.InputKey.ToString() : "None");
				XmlElement xmlElement6 = document.CreateElement("ControllerKey");
				xmlElement4.AppendChild(xmlElement6);
				xmlElement6.InnerText = ((registeredGameKey.ControllerKey != null) ? registeredGameKey.ControllerKey.InputKey.ToString() : "None");
			}
		}
		foreach (GameAxisKey registeredGameAxisKey in context.RegisteredGameAxisKeys)
		{
			XmlElement xmlElement7 = document.CreateElement("GameAxisKey");
			xmlElement.AppendChild(xmlElement7);
			XmlElement xmlElement8 = document.CreateElement("Id");
			xmlElement7.AppendChild(xmlElement8);
			xmlElement8.InnerText = registeredGameAxisKey.Id;
			XmlElement xmlElement9 = document.CreateElement("Keys");
			xmlElement7.AppendChild(xmlElement9);
			XmlElement xmlElement10 = document.CreateElement("PositiveKey");
			xmlElement9.AppendChild(xmlElement10);
			xmlElement10.InnerText = ((registeredGameAxisKey.PositiveKey != null) ? registeredGameAxisKey.PositiveKey.KeyboardKey.InputKey.ToString() : "None");
			XmlElement xmlElement11 = document.CreateElement("NegativeKey");
			xmlElement9.AppendChild(xmlElement11);
			xmlElement11.InnerText = ((registeredGameAxisKey.NegativeKey != null) ? registeredGameAxisKey.NegativeKey.KeyboardKey.InputKey.ToString() : "None");
			XmlElement xmlElement12 = document.CreateElement("AxisKey");
			xmlElement9.AppendChild(xmlElement12);
			xmlElement12.InnerText = ((registeredGameAxisKey.AxisKey != null) ? registeredGameAxisKey.AxisKey.InputKey.ToString() : "None");
		}
		foreach (HotKey registeredHotKey in context.RegisteredHotKeys)
		{
			XmlElement xmlElement13 = document.CreateElement("HotKey");
			xmlElement.AppendChild(xmlElement13);
			XmlElement xmlElement14 = document.CreateElement("Id");
			xmlElement13.AppendChild(xmlElement14);
			xmlElement14.InnerText = registeredHotKey.Id;
			XmlElement xmlElement15 = document.CreateElement("Keys");
			xmlElement13.AppendChild(xmlElement15);
			foreach (Key key in registeredHotKey.Keys)
			{
				XmlElement xmlElement16 = document.CreateElement("Key");
				xmlElement15.AppendChild(xmlElement16);
				xmlElement16.InnerText = key.InputKey.ToString();
			}
		}
		return xmlElement;
	}
}
