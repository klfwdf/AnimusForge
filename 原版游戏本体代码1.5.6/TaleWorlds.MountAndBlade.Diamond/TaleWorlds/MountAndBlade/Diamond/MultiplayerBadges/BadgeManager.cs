using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public static class BadgeManager
{
	public const string PropertyParameterName = "property";

	public const string ValueParameterName = "value";

	public const string MinValueParameterName = "min_value";

	public const string MaxValueParameterName = "max_value";

	public const string IsBestParameterName = "is_best";

	private static Dictionary<string, Badge> _badgesById;

	private static Dictionary<BadgeType, List<Badge>> _badgesByType;

	public static List<Badge> Badges { get; private set; }

	public static bool IsInitialized { get; private set; }

	public static void InitializeWithXML(string xmlPath)
	{
		Debug.Print("BadgeManager::InitializeWithXML");
		if (!IsInitialized)
		{
			LoadFromXml(xmlPath);
			IsInitialized = true;
		}
	}

	public static void OnFinalize()
	{
		Debug.Print("BadgeManager::OnFinalize");
		if (IsInitialized)
		{
			_badgesById.Clear();
			_badgesByType.Clear();
			Badges.Clear();
			_badgesById = null;
			_badgesByType = null;
			Badges = null;
			IsInitialized = false;
		}
	}

	private static void LoadFromXml(string path)
	{
		XmlDocument xmlDocument = new XmlDocument();
		using (StreamReader streamReader = new StreamReader(path))
		{
			string xml = streamReader.ReadToEnd();
			xmlDocument.LoadXml(xml);
			streamReader.Close();
		}
		_badgesById = new Dictionary<string, Badge>();
		_badgesByType = new Dictionary<BadgeType, List<Badge>>();
		Badges = new List<Badge>();
		foreach (XmlNode childNode in xmlDocument.ChildNodes)
		{
			if (!(childNode.Name == "Badges"))
			{
				continue;
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.Name == "Badge")
				{
					BadgeType result = BadgeType.Custom;
					if (!Enum.TryParse<BadgeType>(childNode2.Attributes["type"].Value, ignoreCase: true, out result))
					{
						Debug.FailedAssert("No 'type' was provided for a badge", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeManager.cs", "LoadFromXml", 82);
					}
					Badge badge = null;
					switch (result)
					{
					case BadgeType.Custom:
					case BadgeType.OnLogin:
						badge = new Badge(Badges.Count, result);
						break;
					case BadgeType.Conditional:
						badge = new ConditionalBadge(Badges.Count, result);
						break;
					}
					badge.Deserialize(childNode2);
					_badgesById[badge.StringId] = badge;
					Badges.Add(badge);
					if (!_badgesByType.TryGetValue(result, out var value))
					{
						value = new List<Badge>();
						_badgesByType.Add(result, value);
					}
					value.Add(badge);
				}
			}
		}
	}

	public static Badge GetByIndex(int index)
	{
		if (index == -1 || Badges == null || Badges.Count <= index || index < 0)
		{
			return null;
		}
		return Badges[index];
	}

	public static Badge GetById(string id)
	{
		if (id == null || !_badgesById.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static List<Badge> GetByType(BadgeType type)
	{
		if (!_badgesByType.TryGetValue(type, out var value))
		{
			value = new List<Badge>();
			_badgesByType.Add(type, value);
		}
		return value;
	}

	public static string GetBadgeConditionValue(this PlayerData playerData, BadgeCondition condition)
	{
		if (playerData == null)
		{
			Debug.FailedAssert("PlayerData is null on get value", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeManager.cs", "GetBadgeConditionValue", 143);
			return "";
		}
		if (!condition.Parameters.TryGetValue("property", out var value))
		{
			Debug.FailedAssert("Condition with type PlayerData does not have Property parameter", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeManager.cs", "GetBadgeConditionValue", 150);
			return "";
		}
		if (value == "ShownBadgeId")
		{
			return playerData.ShownBadgeId;
		}
		return "";
	}

	public static int GetBadgeConditionNumericValue(this PlayerData playerData, BadgeCondition condition)
	{
		if (playerData == null)
		{
			Debug.FailedAssert("PlayerData is null on get value", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeManager.cs", "GetBadgeConditionNumericValue", 167);
			return 0;
		}
		if (!condition.Parameters.TryGetValue("property", out var value))
		{
			Debug.FailedAssert("Condition with type PlayerDataNumeric does not have Property parameter", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeManager.cs", "GetBadgeConditionNumericValue", 174);
			return 0;
		}
		int result = 0;
		string[] array = value.Split(new char[1] { '.' });
		switch (array[0])
		{
		case "KillCount":
			result = playerData.KillCount;
			break;
		case "DeathCount":
			result = playerData.DeathCount;
			break;
		case "AssistCount":
			result = playerData.AssistCount;
			break;
		case "WinCount":
			result = playerData.WinCount;
			break;
		case "LoseCount":
			result = playerData.LoseCount;
			break;
		case "Level":
			result = playerData.Level;
			break;
		case "Playtime":
			result = playerData.Playtime;
			break;
		case "Stats":
		{
			if (array.Length != 3 || playerData.Stats == null)
			{
				break;
			}
			string text = array[1].Trim().ToLower();
			PlayerStatsBase[] stats = playerData.Stats;
			foreach (PlayerStatsBase playerStatsBase in stats)
			{
				if (playerStatsBase.GameType.Trim().ToLower() == text)
				{
					switch (array[2])
					{
					case "KillCount":
						result = playerStatsBase.KillCount;
						break;
					case "DeathCount":
						result = playerStatsBase.DeathCount;
						break;
					case "AssistCount":
						result = playerStatsBase.AssistCount;
						break;
					case "WinCount":
						result = playerStatsBase.WinCount;
						break;
					case "LoseCount":
						result = playerStatsBase.LoseCount;
						break;
					}
					break;
				}
			}
			break;
		}
		}
		return result;
	}
}
