using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.Core
{
	// Token: 0x020000D2 RID: 210
	public class TauntUsageManager
	{
		// Token: 0x170003D3 RID: 979
		// (get) Token: 0x06000B16 RID: 2838 RVA: 0x00023C94 File Offset: 0x00021E94
		public static TauntUsageManager Instance
		{
			get
			{
				if (TauntUsageManager._instance == null)
				{
					TauntUsageManager._instance = TauntUsageManager.Initialize();
				}
				return TauntUsageManager._instance;
			}
		}

		// Token: 0x06000B17 RID: 2839 RVA: 0x00023CAC File Offset: 0x00021EAC
		private TauntUsageManager()
		{
			this._tauntUsageSets = new List<TauntUsageManager.TauntUsageSet>();
			this._tauntUsageSetIndexMap = new Dictionary<string, int>();
			this.Read();
		}

		// Token: 0x06000B18 RID: 2840 RVA: 0x00023CD0 File Offset: 0x00021ED0
		public static TauntUsageManager Initialize()
		{
			if (TauntUsageManager._instance == null)
			{
				TauntUsageManager._instance = new TauntUsageManager();
			}
			return TauntUsageManager._instance;
		}

		// Token: 0x06000B19 RID: 2841 RVA: 0x00023CE8 File Offset: 0x00021EE8
		public void Read()
		{
			foreach (object obj in TauntUsageManager.LoadXmlFile(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/taunt_usage_sets.xml").DocumentElement.SelectNodes("taunt_usage_set"))
			{
				XmlNode xmlNode = (XmlNode)obj;
				string innerText = xmlNode.Attributes["id"].InnerText;
				this._tauntUsageSets.Add(new TauntUsageManager.TauntUsageSet());
				this._tauntUsageSetIndexMap[innerText] = this._tauntUsageSets.Count - 1;
				foreach (object obj2 in xmlNode.SelectNodes("taunt_usage"))
				{
					XmlNode xmlNode2 = (XmlNode)obj2;
					TauntUsageManager.TauntUsage.TauntUsageFlag tauntUsageFlag = TauntUsageManager.TauntUsage.TauntUsageFlag.None;
					XmlAttribute xmlAttribute = xmlNode2.Attributes["requires_bow"];
					if (bool.Parse(((xmlAttribute != null) ? xmlAttribute.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresBow;
					}
					XmlAttribute xmlAttribute2 = xmlNode2.Attributes["requires_on_foot"];
					if (bool.Parse(((xmlAttribute2 != null) ? xmlAttribute2.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresOnFoot;
					}
					XmlAttribute xmlAttribute3 = xmlNode2.Attributes["requires_shield"];
					if (bool.Parse(((xmlAttribute3 != null) ? xmlAttribute3.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresShield;
					}
					XmlAttribute xmlAttribute4 = xmlNode2.Attributes["is_left_stance"];
					if (bool.Parse(((xmlAttribute4 != null) ? xmlAttribute4.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.IsLeftStance;
					}
					XmlAttribute xmlAttribute5 = xmlNode2.Attributes["unsuitable_for_two_handed"];
					if (bool.Parse(((xmlAttribute5 != null) ? xmlAttribute5.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded;
					}
					XmlAttribute xmlAttribute6 = xmlNode2.Attributes["unsuitable_for_one_handed"];
					if (bool.Parse(((xmlAttribute6 != null) ? xmlAttribute6.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded;
					}
					XmlAttribute xmlAttribute7 = xmlNode2.Attributes["unsuitable_for_shield"];
					if (bool.Parse(((xmlAttribute7 != null) ? xmlAttribute7.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForShield;
					}
					XmlAttribute xmlAttribute8 = xmlNode2.Attributes["unsuitable_for_bow"];
					if (bool.Parse(((xmlAttribute8 != null) ? xmlAttribute8.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForBow;
					}
					XmlAttribute xmlAttribute9 = xmlNode2.Attributes["unsuitable_for_crossbow"];
					if (bool.Parse(((xmlAttribute9 != null) ? xmlAttribute9.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForCrossbow;
					}
					XmlAttribute xmlAttribute10 = xmlNode2.Attributes["unsuitable_for_empty"];
					if (bool.Parse(((xmlAttribute10 != null) ? xmlAttribute10.Value : null) ?? "False"))
					{
						tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForEmpty;
					}
					string value = xmlNode2.Attributes["action"].Value;
					this._tauntUsageSets.Last<TauntUsageManager.TauntUsageSet>().AddUsage(new TauntUsageManager.TauntUsage(tauntUsageFlag, value));
				}
			}
		}

		// Token: 0x06000B1A RID: 2842 RVA: 0x00024028 File Offset: 0x00022228
		public TauntUsageManager.TauntUsageSet GetUsageSet(string id)
		{
			int num;
			if (this._tauntUsageSetIndexMap.TryGetValue(id, out num) && num >= 0 && num < this._tauntUsageSets.Count)
			{
				return this._tauntUsageSets[num];
			}
			return null;
		}

		// Token: 0x06000B1B RID: 2843 RVA: 0x00024068 File Offset: 0x00022268
		public string GetAction(int index, bool isLeftStance, bool onFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
		{
			string result = null;
			foreach (TauntUsageManager.TauntUsage tauntUsage in this._tauntUsageSets[index].GetUsages())
			{
				if (tauntUsage.IsSuitable(isLeftStance, onFoot, mainHandWeapon, offhandWeapon))
				{
					result = tauntUsage.GetAction();
					break;
				}
			}
			return result;
		}

		// Token: 0x06000B1C RID: 2844 RVA: 0x000240DC File Offset: 0x000222DC
		private static TextObject GetHintTextFromReasons(List<TextObject> reasons)
		{
			TextObject textObject = null;
			for (int i = 0; i < reasons.Count; i++)
			{
				if (i >= 1)
				{
					GameTexts.SetVariable("STR1", textObject.ToString());
					GameTexts.SetVariable("STR2", reasons[i]);
					textObject = GameTexts.FindText("str_string_newline_string", null);
				}
				else
				{
					textObject = reasons[i];
				}
			}
			return textObject;
		}

		// Token: 0x06000B1D RID: 2845 RVA: 0x00024138 File Offset: 0x00022338
		public static string GetActionDisabledReasonText(TauntUsageManager.TauntUsage.TauntUsageFlag disabledReasonFlag)
		{
			List<TextObject> list = new List<TextObject>();
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresBow))
			{
				list.Add(new TextObject("{=2GE0in0u}Requires Bow.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresShield))
			{
				list.Add(new TextObject("{=6Tw6BLXI}Requires Shield.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresOnFoot))
			{
				list.Add(new TextObject("{=GHQMM8Df}Can't be used while mounted.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded))
			{
				list.Add(new TextObject("{=EhK4Q6S4}Can't be used with Two Handed weapons.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded))
			{
				list.Add(new TextObject("{=wJbkXP98}Can't be used with One Handed weapons.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForShield))
			{
				list.Add(new TextObject("{=bJMUTZ00}Can't be used with Shields.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForBow))
			{
				list.Add(new TextObject("{=B9Gp7pIf}Can't be used with Bows.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForCrossbow))
			{
				list.Add(new TextObject("{=kkzKtP78}Can't be used with Crossbows.", null));
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForEmpty))
			{
				list.Add(new TextObject("{=F59nAr9s}Can't be used without a weapon.", null));
			}
			if (list.Count > 0)
			{
				return TauntUsageManager.GetHintTextFromReasons(list).ToString();
			}
			if (disabledReasonFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.IsLeftStance))
			{
				return string.Empty;
			}
			return null;
		}

		// Token: 0x06000B1E RID: 2846 RVA: 0x0002426C File Offset: 0x0002246C
		public TauntUsageManager.TauntUsage.TauntUsageFlag GetIsActionNotSuitableReason(int index, bool isLeftStance, bool onFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
		{
			MBReadOnlyList<TauntUsageManager.TauntUsage> usages = this._tauntUsageSets[index].GetUsages();
			if (usages.Count == 0)
			{
				Debug.FailedAssert("Taunt usages are empty", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\TauntUsageManager.cs", "GetIsActionNotSuitableReason", 238);
				return TauntUsageManager.TauntUsage.TauntUsageFlag.None;
			}
			TauntUsageManager.TauntUsage.TauntUsageFlag[] array = new TauntUsageManager.TauntUsage.TauntUsageFlag[usages.Count];
			for (int i = 0; i < usages.Count; i++)
			{
				TauntUsageManager.TauntUsage.TauntUsageFlag isNotSuitableReason = usages[i].GetIsNotSuitableReason(isLeftStance, onFoot, mainHandWeapon, offhandWeapon);
				if (isNotSuitableReason == TauntUsageManager.TauntUsage.TauntUsageFlag.None)
				{
					return TauntUsageManager.TauntUsage.TauntUsageFlag.None;
				}
				array[i] = isNotSuitableReason;
			}
			Array.Sort<TauntUsageManager.TauntUsage.TauntUsageFlag>(array, new TauntUsageManager.TauntUsageFlagComparer());
			return array[0];
		}

		// Token: 0x06000B1F RID: 2847 RVA: 0x000242F5 File Offset: 0x000224F5
		public int GetTauntItemCount()
		{
			return this._tauntUsageSets.Count;
		}

		// Token: 0x06000B20 RID: 2848 RVA: 0x00024304 File Offset: 0x00022504
		public int GetIndexOfAction(string id)
		{
			int result;
			if (this._tauntUsageSetIndexMap.TryGetValue(id, out result))
			{
				return result;
			}
			return -1;
		}

		// Token: 0x06000B21 RID: 2849 RVA: 0x00024324 File Offset: 0x00022524
		public string GetDefaultAction(int index)
		{
			TauntUsageManager.TauntUsage tauntUsage = this._tauntUsageSets[index].GetUsages().Last<TauntUsageManager.TauntUsage>();
			if (tauntUsage == null)
			{
				return null;
			}
			return tauntUsage.GetAction();
		}

		// Token: 0x06000B22 RID: 2850 RVA: 0x00024348 File Offset: 0x00022548
		private static XmlDocument LoadXmlFile(string path)
		{
			string xml = new StreamReader(path).ReadToEnd();
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			return xmlDocument;
		}

		// Token: 0x04000639 RID: 1593
		private static TauntUsageManager _instance;

		// Token: 0x0400063A RID: 1594
		private List<TauntUsageManager.TauntUsageSet> _tauntUsageSets;

		// Token: 0x0400063B RID: 1595
		private Dictionary<string, int> _tauntUsageSetIndexMap;

		// Token: 0x0200013A RID: 314
		private class TauntUsageFlagComparer : IComparer<TauntUsageManager.TauntUsage.TauntUsageFlag>
		{
			// Token: 0x06000C34 RID: 3124 RVA: 0x00026AC4 File Offset: 0x00024CC4
			public int Compare(TauntUsageManager.TauntUsage.TauntUsageFlag x, TauntUsageManager.TauntUsage.TauntUsageFlag y)
			{
				int num = (int)x;
				return num.CompareTo((int)y);
			}
		}

		// Token: 0x0200013B RID: 315
		public class TauntUsageSet
		{
			// Token: 0x06000C36 RID: 3126 RVA: 0x00026AE3 File Offset: 0x00024CE3
			public TauntUsageSet()
			{
				this._tauntUsages = new MBList<TauntUsageManager.TauntUsage>();
			}

			// Token: 0x06000C37 RID: 3127 RVA: 0x00026AF6 File Offset: 0x00024CF6
			public void AddUsage(TauntUsageManager.TauntUsage usage)
			{
				this._tauntUsages.Add(usage);
			}

			// Token: 0x06000C38 RID: 3128 RVA: 0x00026B04 File Offset: 0x00024D04
			public MBReadOnlyList<TauntUsageManager.TauntUsage> GetUsages()
			{
				return this._tauntUsages;
			}

			// Token: 0x0400081E RID: 2078
			private MBList<TauntUsageManager.TauntUsage> _tauntUsages;
		}

		// Token: 0x0200013C RID: 316
		public class TauntUsage
		{
			// Token: 0x1700040A RID: 1034
			// (get) Token: 0x06000C39 RID: 3129 RVA: 0x00026B0C File Offset: 0x00024D0C
			public TauntUsageManager.TauntUsage.TauntUsageFlag UsageFlag { get; }

			// Token: 0x06000C3A RID: 3130 RVA: 0x00026B14 File Offset: 0x00024D14
			public TauntUsage(TauntUsageManager.TauntUsage.TauntUsageFlag usageFlag, string actionName)
			{
				this.UsageFlag = usageFlag;
				this._actionName = actionName;
			}

			// Token: 0x06000C3B RID: 3131 RVA: 0x00026B2A File Offset: 0x00024D2A
			public bool IsSuitable(bool isLeftStance, bool isOnFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
			{
				return this.GetIsNotSuitableReason(isLeftStance, isOnFoot, mainHandWeapon, offhandWeapon) == TauntUsageManager.TauntUsage.TauntUsageFlag.None;
			}

			// Token: 0x06000C3C RID: 3132 RVA: 0x00026B3C File Offset: 0x00024D3C
			public TauntUsageManager.TauntUsage.TauntUsageFlag GetIsNotSuitableReason(bool isLeftStance, bool isOnFoot, WeaponComponentData mainHandWeapon, WeaponComponentData offhandWeapon)
			{
				TauntUsageManager.TauntUsage.TauntUsageFlag tauntUsageFlag = TauntUsageManager.TauntUsage.TauntUsageFlag.None;
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresBow) && (mainHandWeapon == null || !mainHandWeapon.IsBow))
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresBow;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresShield) && (offhandWeapon == null || !offhandWeapon.IsShield))
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresShield;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresOnFoot) && !isOnFoot)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresOnFoot;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded) && mainHandWeapon != null && mainHandWeapon.IsTwoHanded)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded) && mainHandWeapon != null && mainHandWeapon.IsOneHanded)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForShield) && offhandWeapon != null && offhandWeapon.IsShield)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForShield;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForBow) && mainHandWeapon != null && mainHandWeapon.IsBow)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForBow;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForCrossbow) && mainHandWeapon != null && mainHandWeapon.IsCrossBow)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForCrossbow;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForEmpty) && mainHandWeapon == null && offhandWeapon == null)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForEmpty;
				}
				if (this.UsageFlag.HasAllFlags(TauntUsageManager.TauntUsage.TauntUsageFlag.IsLeftStance) != isLeftStance)
				{
					tauntUsageFlag |= TauntUsageManager.TauntUsage.TauntUsageFlag.IsLeftStance;
				}
				return tauntUsageFlag;
			}

			// Token: 0x06000C3D RID: 3133 RVA: 0x00026C7A File Offset: 0x00024E7A
			public string GetAction()
			{
				return this._actionName;
			}

			// Token: 0x04000820 RID: 2080
			private string _actionName;

			// Token: 0x02000142 RID: 322
			[Flags]
			public enum TauntUsageFlag
			{
				// Token: 0x0400083D RID: 2109
				None = 0,
				// Token: 0x0400083E RID: 2110
				RequiresBow = 1,
				// Token: 0x0400083F RID: 2111
				RequiresShield = 2,
				// Token: 0x04000840 RID: 2112
				IsLeftStance = 4,
				// Token: 0x04000841 RID: 2113
				RequiresOnFoot = 8,
				// Token: 0x04000842 RID: 2114
				UnsuitableForTwoHanded = 16,
				// Token: 0x04000843 RID: 2115
				UnsuitableForOneHanded = 32,
				// Token: 0x04000844 RID: 2116
				UnsuitableForShield = 64,
				// Token: 0x04000845 RID: 2117
				UnsuitableForBow = 128,
				// Token: 0x04000846 RID: 2118
				UnsuitableForCrossbow = 256,
				// Token: 0x04000847 RID: 2119
				UnsuitableForEmpty = 512
			}
		}
	}
}
