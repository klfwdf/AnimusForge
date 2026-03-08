using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000008 RID: 8
	public static class HotKeyManager
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600009F RID: 159 RVA: 0x00003358 File Offset: 0x00001558
		// (remove) Token: 0x060000A0 RID: 160 RVA: 0x0000338C File Offset: 0x0000158C
		public static event HotKeyManager.OnKeybindsChangedEvent OnKeybindsChanged;

		// Token: 0x060000A1 RID: 161 RVA: 0x000033C0 File Offset: 0x000015C0
		public static string GetHotKeyId(string categoryName, string hotKeyId)
		{
			GameKeyContext gameKeyContext;
			if (HotKeyManager._categories.TryGetValue(categoryName, out gameKeyContext))
			{
				return gameKeyContext.GetHotKeyId(hotKeyId);
			}
			Debug.FailedAssert("Key category with id \"" + categoryName + "\" doesn't exsist.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "GetHotKeyId", 29);
			return "";
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x0000340C File Offset: 0x0000160C
		public static string GetHotKeyId(string categoryName, int hotKeyId)
		{
			GameKeyContext gameKeyContext;
			if (HotKeyManager._categories.TryGetValue(categoryName, out gameKeyContext))
			{
				return gameKeyContext.GetHotKeyId(hotKeyId);
			}
			Debug.FailedAssert("Key category with id \"" + categoryName + "\" doesn't exsist.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "GetHotKeyId", 40);
			return "invalid";
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00003456 File Offset: 0x00001656
		public static GameKeyContext GetCategory(string categoryName)
		{
			return HotKeyManager._categories[categoryName];
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00003463 File Offset: 0x00001663
		public static Dictionary<string, GameKeyContext>.ValueCollection GetAllCategories()
		{
			return HotKeyManager._categories.Values;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x0000346F File Offset: 0x0000166F
		public static void Tick(float dt)
		{
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00003471 File Offset: 0x00001671
		public static void Initialize(PlatformFilePath savePath, bool isRDownSwappedWithRRight)
		{
			GameKeyContext.SetIsRDownSwappedWithRRight(isRDownSwappedWithRRight);
			HotKeyManager._savePath = savePath;
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00003480 File Offset: 0x00001680
		public static void RegisterInitialContexts(IEnumerable<GameKeyContext> contexts, bool loadKeys)
		{
			HotKeyManager._categories.Clear();
			foreach (GameKeyContext gameKeyContext in contexts)
			{
				HotKeyManager.RegisterContext(gameKeyContext, gameKeyContext.Type == GameKeyContext.GameKeyContextType.AuxiliaryNotSerialized, false);
			}
			if (loadKeys)
			{
				HotKeyManager.LoadAsync();
			}
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000034E4 File Offset: 0x000016E4
		public static void RegisterContext(GameKeyContext context, bool ignoreSerialize = false, bool loadKeys = false)
		{
			if (!HotKeyManager._categories.ContainsKey(context.GameKeyCategoryId))
			{
				HotKeyManager._categories.Add(context.GameKeyCategoryId, context);
			}
			if (ignoreSerialize && !HotKeyManager._serializeIgnoredCategories.Contains(context.GameKeyCategoryId))
			{
				HotKeyManager._serializeIgnoredCategories.Add(context.GameKeyCategoryId);
			}
			if (loadKeys)
			{
				HotKeyManager.LoadAsync();
			}
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00003541 File Offset: 0x00001741
		public static bool ShouldNotifyDocumentVersionDifferent()
		{
			bool notifyDocumentVersionDifferent = HotKeyManager._notifyDocumentVersionDifferent;
			HotKeyManager._notifyDocumentVersionDifferent = false;
			return notifyDocumentVersionDifferent;
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00003550 File Offset: 0x00001750
		public static void Reset()
		{
			foreach (GameKeyContext gameKeyContext in HotKeyManager._categories.Values)
			{
				foreach (GameKey gameKey in gameKeyContext.RegisteredGameKeys)
				{
					if (gameKey != null)
					{
						Key controllerKey = gameKey.ControllerKey;
						if (controllerKey != null)
						{
							Key defaultControllerKey = gameKey.DefaultControllerKey;
							controllerKey.ChangeKey((defaultControllerKey != null) ? defaultControllerKey.InputKey : InputKey.Invalid);
						}
						Key keyboardKey = gameKey.KeyboardKey;
						if (keyboardKey != null)
						{
							Key defaultKeyboardKey = gameKey.DefaultKeyboardKey;
							keyboardKey.ChangeKey((defaultKeyboardKey != null) ? defaultKeyboardKey.InputKey : InputKey.Invalid);
						}
					}
				}
				foreach (HotKey hotKey in gameKeyContext.RegisteredHotKeys)
				{
					if (hotKey != null)
					{
						hotKey.Keys.Clear();
						foreach (Key key in hotKey.DefaultKeys)
						{
							hotKey.Keys.Add(new Key(key.InputKey));
						}
					}
				}
				foreach (GameAxisKey gameAxisKey in gameKeyContext.RegisteredGameAxisKeys)
				{
					gameAxisKey.AxisKey.ChangeKey(gameAxisKey.DefaultAxisKey.InputKey);
				}
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00003760 File Offset: 0x00001960
		public static async void LoadAsync()
		{
			if (FileHelper.FileExists(HotKeyManager._savePath))
			{
				try
				{
					XmlDocument document = new XmlDocument();
					await document.LoadAsync(HotKeyManager._savePath);
					XmlElement documentElement = document.DocumentElement;
					float num = 0f;
					if (documentElement.HasAttribute("hotkeyEditEnabled"))
					{
						HotKeyManager._hotkeyEditEnabled = Convert.ToBoolean(documentElement.GetAttribute("hotkeyEditEnabled"));
					}
					float num2;
					if (documentElement.HasAttribute("version") && float.TryParse(documentElement.GetAttribute("version"), out num2))
					{
						num = num2;
					}
					if (num != HotKeyManager._versionOfHotkeys)
					{
						HotKeyManager._notifyDocumentVersionDifferent = true;
						HotKeyManager.SaveAsync(false);
					}
					else
					{
						foreach (object obj in documentElement.ChildNodes)
						{
							XmlElement xmlElement = (XmlElement)((XmlNode)obj);
							string attribute = xmlElement.GetAttribute("name");
							GameKeyContext gameKeyContext;
							if (HotKeyManager._categories.TryGetValue(attribute, out gameKeyContext))
							{
								foreach (object obj2 in xmlElement.ChildNodes)
								{
									XmlNode xmlNode = (XmlNode)obj2;
									string name = ((XmlElement)xmlNode).Name;
									if (name == "GameKey")
									{
										string innerText = xmlNode["Id"].InnerText;
										GameKey gameKey = gameKeyContext.GetGameKey(innerText);
										if (gameKey != null)
										{
											XmlElement xmlElement2 = xmlNode["Keys"]["KeyboardKey"];
											if (xmlElement2 != null)
											{
												InputKey key;
												if (Enum.TryParse<InputKey>(xmlElement2.InnerText, out key))
												{
													if (gameKey.KeyboardKey != null)
													{
														gameKey.KeyboardKey.ChangeKey(key);
													}
													else
													{
														gameKey.KeyboardKey = new Key(key);
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
									}
									else if (HotKeyManager._hotkeyEditEnabled || gameKeyContext.Type == GameKeyContext.GameKeyContextType.AuxiliarySerializedAndShownInOptions)
									{
										if (name == "GameAxisKey")
										{
											string innerText2 = xmlNode["Id"].InnerText;
											GameAxisKey gameAxisKey = gameKeyContext.GetGameAxisKey(innerText2);
											if (gameAxisKey != null)
											{
												XmlElement xmlElement3 = xmlNode["Keys"];
												if (!gameAxisKey.IsBinded)
												{
													XmlElement xmlElement4 = xmlElement3["PositiveKey"];
													if (xmlElement4 != null)
													{
														if (xmlElement4.InnerText != "None")
														{
															InputKey defaultKeyboardKey;
															if (Enum.TryParse<InputKey>(xmlElement4.InnerText, out defaultKeyboardKey))
															{
																gameAxisKey.PositiveKey = new GameKey(-1, gameAxisKey.Id + "_p", attribute, defaultKeyboardKey, "");
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
															InputKey defaultKeyboardKey2;
															if (Enum.TryParse<InputKey>(xmlElement5.InnerText, out defaultKeyboardKey2))
															{
																gameAxisKey.NegativeKey = new GameKey(-1, gameAxisKey.Id + "_n", attribute, defaultKeyboardKey2, "");
															}
														}
														else
														{
															gameAxisKey.NegativeKey = null;
														}
													}
												}
												XmlElement xmlElement6 = xmlElement3["AxisKey"];
												if (xmlElement6 != null)
												{
													if (xmlElement6.InnerText != "None")
													{
														InputKey key2;
														if (Enum.TryParse<InputKey>(xmlElement6.InnerText, out key2))
														{
															gameAxisKey.AxisKey = new Key(key2);
														}
													}
													else
													{
														gameAxisKey.AxisKey = null;
													}
												}
											}
										}
										else if (name == "HotKey")
										{
											string innerText3 = xmlNode["Id"].InnerText;
											HotKey hotKey = gameKeyContext.GetHotKey(innerText3);
											if (hotKey != null)
											{
												new List<HotKey>();
												XmlElement xmlElement7 = xmlNode["Keys"];
												hotKey.Keys = new List<Key>();
												for (int i = 0; i < xmlElement7.ChildNodes.Count; i++)
												{
													InputKey key3;
													if (Enum.TryParse<InputKey>(xmlElement7.ChildNodes[i].InnerText, out key3))
													{
														hotKey.Keys.Add(new Key(key3));
													}
												}
											}
										}
									}
								}
							}
						}
						document = null;
					}
				}
				catch (Exception ex)
				{
					Debug.FailedAssert("Couldn't load key bindings: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "LoadAsync", 354);
				}
			}
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00003794 File Offset: 0x00001994
		public static async void SaveAsync(bool throwEvent)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				XmlDeclaration newChild = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
				XmlElement documentElement = xmlDocument.DocumentElement;
				xmlDocument.InsertBefore(newChild, documentElement);
				XmlComment newChild2 = xmlDocument.CreateComment("To override values other than GameKeys, change hotkeyEditEnabled to True.");
				xmlDocument.InsertBefore(newChild2, documentElement);
				XmlElement xmlElement = xmlDocument.CreateElement("HotKeyCategories");
				xmlElement.SetAttribute("hotkeyEditEnabled", HotKeyManager._hotkeyEditEnabled.ToString());
				xmlElement.SetAttribute("version", HotKeyManager._versionOfHotkeys.ToString());
				xmlDocument.AppendChild(xmlElement);
				foreach (KeyValuePair<string, GameKeyContext> keyValuePair in HotKeyManager._categories)
				{
					if (!HotKeyManager._serializeIgnoredCategories.Contains(keyValuePair.Key))
					{
						XmlElement xmlElement2 = xmlDocument.CreateElement("HotKeyCategory");
						xmlElement.AppendChild(xmlElement2);
						xmlElement2.SetAttribute("name", keyValuePair.Key);
						foreach (GameKey gameKey in keyValuePair.Value.RegisteredGameKeys)
						{
							if (gameKey != null)
							{
								XmlElement xmlElement3 = xmlDocument.CreateElement("GameKey");
								xmlElement2.AppendChild(xmlElement3);
								XmlElement xmlElement4 = xmlDocument.CreateElement("Id");
								xmlElement3.AppendChild(xmlElement4);
								xmlElement4.InnerText = gameKey.StringId;
								XmlElement xmlElement5 = xmlDocument.CreateElement("Keys");
								xmlElement3.AppendChild(xmlElement5);
								XmlElement xmlElement6 = xmlDocument.CreateElement("KeyboardKey");
								xmlElement5.AppendChild(xmlElement6);
								xmlElement6.InnerText = ((gameKey.KeyboardKey != null) ? gameKey.KeyboardKey.InputKey.ToString() : "None");
								XmlElement xmlElement7 = xmlDocument.CreateElement("ControllerKey");
								xmlElement5.AppendChild(xmlElement7);
								xmlElement7.InnerText = ((gameKey.ControllerKey != null) ? gameKey.ControllerKey.InputKey.ToString() : "None");
							}
						}
						foreach (GameAxisKey gameAxisKey in keyValuePair.Value.RegisteredGameAxisKeys)
						{
							XmlElement xmlElement8 = xmlDocument.CreateElement("GameAxisKey");
							xmlElement2.AppendChild(xmlElement8);
							XmlElement xmlElement9 = xmlDocument.CreateElement("Id");
							xmlElement8.AppendChild(xmlElement9);
							xmlElement9.InnerText = gameAxisKey.Id;
							XmlElement xmlElement10 = xmlDocument.CreateElement("Keys");
							xmlElement8.AppendChild(xmlElement10);
							XmlElement xmlElement11 = xmlDocument.CreateElement("PositiveKey");
							xmlElement10.AppendChild(xmlElement11);
							xmlElement11.InnerText = ((gameAxisKey.PositiveKey != null) ? gameAxisKey.PositiveKey.KeyboardKey.InputKey.ToString() : "None");
							XmlElement xmlElement12 = xmlDocument.CreateElement("NegativeKey");
							xmlElement10.AppendChild(xmlElement12);
							xmlElement12.InnerText = ((gameAxisKey.NegativeKey != null) ? gameAxisKey.NegativeKey.KeyboardKey.InputKey.ToString() : "None");
							XmlElement xmlElement13 = xmlDocument.CreateElement("AxisKey");
							xmlElement10.AppendChild(xmlElement13);
							xmlElement13.InnerText = ((gameAxisKey.AxisKey != null) ? gameAxisKey.AxisKey.InputKey.ToString() : "None");
						}
						foreach (HotKey hotKey in keyValuePair.Value.RegisteredHotKeys)
						{
							XmlElement xmlElement14 = xmlDocument.CreateElement("HotKey");
							xmlElement2.AppendChild(xmlElement14);
							XmlElement xmlElement15 = xmlDocument.CreateElement("Id");
							xmlElement14.AppendChild(xmlElement15);
							xmlElement15.InnerText = hotKey.Id;
							XmlElement xmlElement16 = xmlDocument.CreateElement("Keys");
							xmlElement14.AppendChild(xmlElement16);
							foreach (Key key in hotKey.Keys)
							{
								XmlElement xmlElement17 = xmlDocument.CreateElement("Key");
								xmlElement16.AppendChild(xmlElement17);
								xmlElement17.InnerText = key.InputKey.ToString();
							}
						}
					}
				}
				await xmlDocument.SaveAsync(HotKeyManager._savePath);
				if (throwEvent)
				{
					HotKeyManager.OnKeybindsChangedEvent onKeybindsChanged = HotKeyManager.OnKeybindsChanged;
					if (onKeybindsChanged != null)
					{
						onKeybindsChanged();
					}
				}
			}
			catch
			{
				Debug.FailedAssert("Couldn't save key bindings.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "SaveAsync", 466);
			}
		}

		// Token: 0x0400001E RID: 30
		private static readonly Dictionary<string, GameKeyContext> _categories = new Dictionary<string, GameKeyContext>();

		// Token: 0x0400001F RID: 31
		private static readonly List<string> _serializeIgnoredCategories = new List<string>();

		// Token: 0x04000020 RID: 32
		private static readonly float _versionOfHotkeys = 5.1f;

		// Token: 0x04000021 RID: 33
		private static bool _hotkeyEditEnabled = false;

		// Token: 0x04000022 RID: 34
		private static bool _notifyDocumentVersionDifferent = false;

		// Token: 0x04000023 RID: 35
		private static PlatformFilePath _savePath;

		// Token: 0x02000014 RID: 20
		// (Invoke) Token: 0x060001A5 RID: 421
		public delegate void OnKeybindsChangedEvent();
	}
}
