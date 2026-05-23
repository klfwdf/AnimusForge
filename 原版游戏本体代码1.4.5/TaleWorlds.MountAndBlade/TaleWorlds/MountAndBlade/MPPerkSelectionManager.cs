using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class MPPerkSelectionManager
{
	public struct MPPerkSelection
	{
		public readonly int Index;

		public readonly int ListIndex;

		public MPPerkSelection(int index, int listIndex)
		{
			Index = index;
			ListIndex = listIndex;
		}
	}

	private static MPPerkSelectionManager _instance;

	public Action OnAfterResetPendingChanges;

	private Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> _selections;

	private Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> _pendingChanges;

	private PlatformFilePath _xmlPath;

	private PlayerId _playerIdOfSelectionsOwner;

	public static MPPerkSelectionManager Instance => _instance ?? (_instance = new MPPerkSelectionManager());

	public static void FreeInstance()
	{
		if (_instance != null)
		{
			_instance._selections?.Clear();
			_instance._pendingChanges?.Clear();
			_instance = null;
		}
	}

	public void InitializeForUser(string username, PlayerId playerId)
	{
		if (!(_playerIdOfSelectionsOwner != playerId))
		{
			return;
		}
		_selections?.Clear();
		_playerIdOfSelectionsOwner = playerId;
		_xmlPath = new PlatformFilePath(EngineFilePaths.ConfigsPath, string.Concat("MPDefaultPerks_", playerId, ".xml"));
		try
		{
			PlatformFilePath platformFilePath = new PlatformFilePath(EngineFilePaths.ConfigsPath, "MPDefaultPerks_" + username + ".xml");
			if (FileHelper.FileExists(platformFilePath))
			{
				FileHelper.CopyFile(platformFilePath, _xmlPath);
				FileHelper.DeleteFile(platformFilePath);
			}
		}
		catch (Exception)
		{
		}
		Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> dictionary = LoadSelectionsForUserFromXML();
		_selections = dictionary ?? new Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>>();
	}

	public void ResetPendingChanges()
	{
		_pendingChanges?.Clear();
		OnAfterResetPendingChanges?.Invoke();
	}

	public void TryToApplyAndSavePendingChanges()
	{
		if (_pendingChanges == null)
		{
			return;
		}
		foreach (KeyValuePair<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> pendingChange in _pendingChanges)
		{
			if (_selections.ContainsKey(pendingChange.Key))
			{
				_selections.Remove(pendingChange.Key);
			}
			_selections.Add(pendingChange.Key, pendingChange.Value);
		}
		_pendingChanges.Clear();
		List<KeyValuePair<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>>> selections = new List<KeyValuePair<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>>>();
		foreach (KeyValuePair<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> selection in _selections)
		{
			selections.Add(new KeyValuePair<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>>(selection.Key, selection.Value));
		}
		((ITask)AsyncTask.CreateWithDelegate(new ManagedDelegate
		{
			Instance = delegate
			{
				lock (Instance)
				{
					SaveAsXML(selections);
				}
			}
		}, isBackground: true)).Invoke();
	}

	public List<MPPerkSelection> GetSelectionsForHeroClass(MultiplayerClassDivisions.MPHeroClass currentHeroClass)
	{
		List<MPPerkSelection> value = new List<MPPerkSelection>();
		if ((_pendingChanges == null || !_pendingChanges.TryGetValue(currentHeroClass, out value)) && _selections != null)
		{
			_selections.TryGetValue(currentHeroClass, out value);
		}
		return value;
	}

	public void SetSelectionsForHeroClassTemporarily(MultiplayerClassDivisions.MPHeroClass currentHeroClass, List<MPPerkSelection> perkChoices)
	{
		if (_pendingChanges == null)
		{
			_pendingChanges = new Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>>();
		}
		if (!_pendingChanges.TryGetValue(currentHeroClass, out var value))
		{
			value = new List<MPPerkSelection>();
			_pendingChanges.Add(currentHeroClass, value);
		}
		else
		{
			value.Clear();
		}
		int count = perkChoices.Count;
		for (int i = 0; i < count; i++)
		{
			value.Add(perkChoices[i]);
		}
	}

	private Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> LoadSelectionsForUserFromXML()
	{
		Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> dictionary = null;
		lock (Instance)
		{
			bool flag = FileHelper.FileExists(_xmlPath);
			if (flag)
			{
				dictionary = new Dictionary<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>>();
				try
				{
					MBReadOnlyList<MultiplayerClassDivisions.MPHeroClass> mPHeroClasses = MultiplayerClassDivisions.GetMPHeroClasses();
					int count = mPHeroClasses.Count;
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.Load(_xmlPath);
					foreach (XmlNode childNode in xmlDocument.DocumentElement.ChildNodes)
					{
						XmlAttribute? xmlAttribute = childNode.Attributes["id"];
						MultiplayerClassDivisions.MPHeroClass mPHeroClass = null;
						string value = xmlAttribute.Value;
						for (int i = 0; i < count; i++)
						{
							if (mPHeroClasses[i].StringId == value)
							{
								mPHeroClass = mPHeroClasses[i];
								break;
							}
						}
						if (mPHeroClass != null)
						{
							List<MPPerkSelection> list = new List<MPPerkSelection>(2);
							foreach (XmlNode childNode2 in childNode.ChildNodes)
							{
								XmlAttribute xmlAttribute2 = childNode2.Attributes["index"];
								XmlAttribute xmlAttribute3 = childNode2.Attributes["listIndex"];
								if (xmlAttribute2 != null && xmlAttribute3 != null)
								{
									int index = Convert.ToInt32(xmlAttribute2.Value);
									int listIndex = Convert.ToInt32(xmlAttribute3.Value);
									list.Add(new MPPerkSelection(index, listIndex));
								}
								else
								{
									flag = false;
								}
							}
							dictionary.Add(mPHeroClass, list);
						}
						else
						{
							flag = false;
						}
					}
				}
				catch
				{
					flag = false;
				}
			}
			if (!flag)
			{
				dictionary = null;
			}
		}
		return dictionary;
	}

	private bool SaveAsXML(List<KeyValuePair<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>>> selections)
	{
		bool result = true;
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.InsertBefore(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null), xmlDocument.DocumentElement);
			XmlElement xmlElement = xmlDocument.CreateElement("HeroClasses");
			xmlDocument.AppendChild(xmlElement);
			foreach (KeyValuePair<MultiplayerClassDivisions.MPHeroClass, List<MPPerkSelection>> selection in selections)
			{
				MultiplayerClassDivisions.MPHeroClass key = selection.Key;
				List<MPPerkSelection> value = selection.Value;
				XmlElement xmlElement2 = xmlDocument.CreateElement("HeroClass");
				xmlElement2.SetAttribute("id", key.StringId);
				xmlElement.AppendChild(xmlElement2);
				foreach (MPPerkSelection item in value)
				{
					XmlElement xmlElement3 = xmlDocument.CreateElement("PerkSelection");
					int index = item.Index;
					xmlElement3.SetAttribute("index", index.ToString());
					index = item.ListIndex;
					xmlElement3.SetAttribute("listIndex", index.ToString());
					xmlElement2.AppendChild(xmlElement3);
				}
			}
			xmlDocument.Save(_xmlPath);
		}
		catch
		{
			result = false;
		}
		return result;
	}
}
