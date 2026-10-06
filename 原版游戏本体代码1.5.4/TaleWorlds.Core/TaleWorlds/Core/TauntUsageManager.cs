using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.Core;

public class TauntUsageManager
{
	private class TauntUsageFlagComparer : IComparer<TauntUsage.TauntUsageFlag>
	{
		public int Compare(TauntUsage.TauntUsageFlag x, TauntUsage.TauntUsageFlag y)
		{
			int num = (int)x;
			return num.CompareTo((int)y);
		}
	}

	public class TauntUsageSet
	{
		private MBList<TauntUsage> _tauntUsages;

		public TauntUsageSet()
		{
			_tauntUsages = new MBList<TauntUsage>();
		}

		public void AddUsage(TauntUsage usage)
		{
			_tauntUsages.Add(usage);
		}

		public MBReadOnlyList<TauntUsage> GetUsages()
		{
			return _tauntUsages;
		}
	}

	public class TauntUsage
	{
		[Flags]
		public enum TauntUsageFlag
		{
			None = 0,
			RequiresBow = 1,
			RequiresShield = 2,
			IsLeftStance = 4,
			RequiresOnFoot = 8,
			UnsuitableForTwoHanded = 0x10,
			UnsuitableForOneHanded = 0x20,
			UnsuitableForShield = 0x40,
			UnsuitableForBow = 0x80,
			UnsuitableForCrossbow = 0x100,
			UnsuitableForEmpty = 0x200
		}

		private string _actionName;

		public TauntUsageFlag UsageFlag { get; }

		public TauntUsage(TauntUsageFlag usageFlag, string actionName)
		{
			UsageFlag = usageFlag;
			_actionName = actionName;
		}

		public bool IsSuitable(bool isLeftStance, bool isOnFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
		{
			return GetIsNotSuitableReason(isLeftStance, isOnFoot, mainHandWeapon, offhandWeapon) == TauntUsageFlag.None;
		}

		public TauntUsageFlag GetIsNotSuitableReason(bool isLeftStance, bool isOnFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
		{
			TauntUsageFlag tauntUsageFlag = TauntUsageFlag.None;
			if (UsageFlag.HasAllFlags(TauntUsageFlag.RequiresBow) && (mainHandWeapon == null || !mainHandWeapon.IsBow))
			{
				tauntUsageFlag |= TauntUsageFlag.RequiresBow;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.RequiresShield) && (offhandWeapon == null || !offhandWeapon.IsShield))
			{
				tauntUsageFlag |= TauntUsageFlag.RequiresShield;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.RequiresOnFoot) && !isOnFoot)
			{
				tauntUsageFlag |= TauntUsageFlag.RequiresOnFoot;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.UnsuitableForTwoHanded) && mainHandWeapon != null && mainHandWeapon.IsTwoHanded)
			{
				tauntUsageFlag |= TauntUsageFlag.UnsuitableForTwoHanded;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.UnsuitableForOneHanded) && mainHandWeapon != null && mainHandWeapon.IsOneHanded)
			{
				tauntUsageFlag |= TauntUsageFlag.UnsuitableForOneHanded;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.UnsuitableForShield) && offhandWeapon != null && offhandWeapon.IsShield)
			{
				tauntUsageFlag |= TauntUsageFlag.UnsuitableForShield;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.UnsuitableForBow) && mainHandWeapon != null && mainHandWeapon.IsBow)
			{
				tauntUsageFlag |= TauntUsageFlag.UnsuitableForBow;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.UnsuitableForCrossbow) && mainHandWeapon != null && mainHandWeapon.IsCrossBow)
			{
				tauntUsageFlag |= TauntUsageFlag.UnsuitableForCrossbow;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.UnsuitableForEmpty) && mainHandWeapon == null && offhandWeapon == null)
			{
				tauntUsageFlag |= TauntUsageFlag.UnsuitableForEmpty;
			}
			if (UsageFlag.HasAllFlags(TauntUsageFlag.IsLeftStance) != isLeftStance)
			{
				tauntUsageFlag |= TauntUsageFlag.IsLeftStance;
			}
			return tauntUsageFlag;
		}

		public string GetAction()
		{
			return _actionName;
		}
	}

	private static TauntUsageManager _instance;

	private List<TauntUsageSet> _tauntUsageSets;

	private Dictionary<string, int> _tauntUsageSetIndexMap;

	public static TauntUsageManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Initialize();
			}
			return _instance;
		}
	}

	private TauntUsageManager()
	{
		_tauntUsageSets = new List<TauntUsageSet>();
		_tauntUsageSetIndexMap = new Dictionary<string, int>();
		Read();
	}

	public static TauntUsageManager Initialize()
	{
		if (_instance == null)
		{
			_instance = new TauntUsageManager();
		}
		return _instance;
	}

	public void Read()
	{
		foreach (XmlNode item in LoadXmlFile(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/taunt_usage_sets.xml").DocumentElement.SelectNodes("taunt_usage_set"))
		{
			string innerText = item.Attributes["id"].InnerText;
			_tauntUsageSets.Add(new TauntUsageSet());
			_tauntUsageSetIndexMap[innerText] = _tauntUsageSets.Count - 1;
			foreach (XmlNode item2 in item.SelectNodes("taunt_usage"))
			{
				TauntUsage.TauntUsageFlag tauntUsageFlag = TauntUsage.TauntUsageFlag.None;
				if (bool.Parse(item2.Attributes["requires_bow"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.RequiresBow;
				}
				if (bool.Parse(item2.Attributes["requires_on_foot"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.RequiresOnFoot;
				}
				if (bool.Parse(item2.Attributes["requires_shield"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.RequiresShield;
				}
				if (bool.Parse(item2.Attributes["is_left_stance"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.IsLeftStance;
				}
				if (bool.Parse(item2.Attributes["unsuitable_for_two_handed"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded;
				}
				if (bool.Parse(item2.Attributes["unsuitable_for_one_handed"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.UnsuitableForOneHanded;
				}
				if (bool.Parse(item2.Attributes["unsuitable_for_shield"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.UnsuitableForShield;
				}
				if (bool.Parse(item2.Attributes["unsuitable_for_bow"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.UnsuitableForBow;
				}
				if (bool.Parse(item2.Attributes["unsuitable_for_crossbow"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.UnsuitableForCrossbow;
				}
				if (bool.Parse(item2.Attributes["unsuitable_for_empty"]?.Value ?? "False"))
				{
					tauntUsageFlag |= TauntUsage.TauntUsageFlag.UnsuitableForEmpty;
				}
				string value = item2.Attributes["action"].Value;
				_tauntUsageSets.Last().AddUsage(new TauntUsage(tauntUsageFlag, value));
			}
		}
	}

	public TauntUsageSet GetUsageSet(string id)
	{
		if (_tauntUsageSetIndexMap.TryGetValue(id, out var value) && value >= 0 && value < _tauntUsageSets.Count)
		{
			return _tauntUsageSets[value];
		}
		return null;
	}

	public string GetAction(int index, bool isLeftStance, bool onFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
	{
		string result = null;
		foreach (TauntUsage usage in _tauntUsageSets[index].GetUsages())
		{
			if (usage.IsSuitable(isLeftStance, onFoot, mainHandWeapon, offhandWeapon))
			{
				result = usage.GetAction();
				break;
			}
		}
		return result;
	}

	private static TextObject GetHintTextFromReasons(List<TextObject> reasons)
	{
		TextObject textObject = null;
		for (int i = 0; i < reasons.Count; i++)
		{
			if (i >= 1)
			{
				GameTexts.SetVariable("STR1", textObject.ToString());
				GameTexts.SetVariable("STR2", reasons[i]);
				textObject = GameTexts.FindText("str_string_newline_string");
			}
			else
			{
				textObject = reasons[i];
			}
		}
		return textObject;
	}

	public static string GetActionDisabledReasonText(TauntUsage.TauntUsageFlag disabledReasonFlag)
	{
		List<TextObject> list = new List<TextObject>();
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.RequiresBow))
		{
			list.Add(new TextObject("{=2GE0in0u}Requires Bow."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.RequiresShield))
		{
			list.Add(new TextObject("{=6Tw6BLXI}Requires Shield."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.RequiresOnFoot))
		{
			list.Add(new TextObject("{=GHQMM8Df}Can't be used while mounted."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded))
		{
			list.Add(new TextObject("{=EhK4Q6S4}Can't be used with Two Handed weapons."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.UnsuitableForOneHanded))
		{
			list.Add(new TextObject("{=wJbkXP98}Can't be used with One Handed weapons."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.UnsuitableForShield))
		{
			list.Add(new TextObject("{=bJMUTZ00}Can't be used with Shields."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.UnsuitableForBow))
		{
			list.Add(new TextObject("{=B9Gp7pIf}Can't be used with Bows."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.UnsuitableForCrossbow))
		{
			list.Add(new TextObject("{=kkzKtP78}Can't be used with Crossbows."));
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.UnsuitableForEmpty))
		{
			list.Add(new TextObject("{=F59nAr9s}Can't be used without a weapon."));
		}
		if (list.Count > 0)
		{
			return GetHintTextFromReasons(list).ToString();
		}
		if (disabledReasonFlag.HasAllFlags(TauntUsage.TauntUsageFlag.IsLeftStance))
		{
			return string.Empty;
		}
		return null;
	}

	public TauntUsage.TauntUsageFlag GetIsActionNotSuitableReason(int index, bool isLeftStance, bool onFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
	{
		MBReadOnlyList<TauntUsage> usages = _tauntUsageSets[index].GetUsages();
		if (usages.Count == 0)
		{
			Debug.FailedAssert("Taunt usages are empty", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\TauntUsageManager.cs", "GetIsActionNotSuitableReason", 238);
			return TauntUsage.TauntUsageFlag.None;
		}
		TauntUsage.TauntUsageFlag[] array = new TauntUsage.TauntUsageFlag[usages.Count];
		for (int i = 0; i < usages.Count; i++)
		{
			TauntUsage.TauntUsageFlag isNotSuitableReason = usages[i].GetIsNotSuitableReason(isLeftStance, onFoot, mainHandWeapon, offhandWeapon);
			if (isNotSuitableReason == TauntUsage.TauntUsageFlag.None)
			{
				return TauntUsage.TauntUsageFlag.None;
			}
			array[i] = isNotSuitableReason;
		}
		Array.Sort(array, new TauntUsageFlagComparer());
		return array[0];
	}

	public int GetTauntItemCount()
	{
		return _tauntUsageSets.Count;
	}

	public int GetIndexOfAction(string id)
	{
		if (_tauntUsageSetIndexMap.TryGetValue(id, out var value))
		{
			return value;
		}
		return -1;
	}

	public string GetDefaultAction(int index)
	{
		return _tauntUsageSets[index].GetUsages().Last()?.GetAction();
	}

	private static XmlDocument LoadXmlFile(string path)
	{
		string xml = new StreamReader(path).ReadToEnd();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		return xmlDocument;
	}
}
