using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;

namespace MCM.UI.Utils
{
	// Token: 0x02000013 RID: 19
	[NullableContext(1)]
	[Nullable(0)]
	internal static class SettingPropertyDefinitionCache
	{
		// Token: 0x0600005B RID: 91 RVA: 0x00002D46 File Offset: 0x00000F46
		public static void Clear()
		{
			SettingPropertyDefinitionCache.ClearDelegate clearMethod = SettingPropertyDefinitionCache.ClearMethod;
			if (clearMethod == null)
			{
				return;
			}
			clearMethod(SettingPropertyDefinitionCache._cache);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002D5C File Offset: 0x00000F5C
		public static IEnumerable<SettingsPropertyGroupDefinition> GetSettingPropertyGroups(BaseSettings settings)
		{
			List<SettingsPropertyGroupDefinition> list;
			if (!SettingPropertyDefinitionCache._cache.TryGetValue(settings, out list))
			{
				list = BaseSettingsExtensions.GetSettingPropertyGroups(settings);
				SettingPropertyDefinitionCache._cache.Add(settings, list);
			}
			return list;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002D8C File Offset: 0x00000F8C
		public static IEnumerable<ISettingsPropertyDefinition> GetAllSettingPropertyDefinitions(BaseSettings settings)
		{
			IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups = SettingPropertyDefinitionCache.GetSettingPropertyGroups(settings);
			Func<SettingsPropertyGroupDefinition, IEnumerable<ISettingsPropertyDefinition>> selector;
			if ((selector = SettingPropertyDefinitionCache.<>O.<0>__GetAllSettingPropertyDefinitions) == null)
			{
				selector = (SettingPropertyDefinitionCache.<>O.<0>__GetAllSettingPropertyDefinitions = new Func<SettingsPropertyGroupDefinition, IEnumerable<ISettingsPropertyDefinition>>(SettingsUtils.GetAllSettingPropertyDefinitions));
			}
			return settingPropertyGroups.SelectMany(selector);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002DB4 File Offset: 0x00000FB4
		public static IEnumerable<SettingsPropertyGroupDefinition> GetAllSettingPropertyGroupDefinitions(BaseSettings settings)
		{
			IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups = SettingPropertyDefinitionCache.GetSettingPropertyGroups(settings);
			Func<SettingsPropertyGroupDefinition, IEnumerable<SettingsPropertyGroupDefinition>> selector;
			if ((selector = SettingPropertyDefinitionCache.<>O.<1>__GetAllSettingPropertyGroupDefinitions) == null)
			{
				selector = (SettingPropertyDefinitionCache.<>O.<1>__GetAllSettingPropertyGroupDefinitions = new Func<SettingsPropertyGroupDefinition, IEnumerable<SettingsPropertyGroupDefinition>>(SettingsUtils.GetAllSettingPropertyGroupDefinitions));
			}
			return settingPropertyGroups.SelectMany(selector);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00002DDC File Offset: 0x00000FDC
		public static bool Equals(BaseSettings settings1, BaseSettings settings2)
		{
			Dictionary<ValueTuple<string, string>, ISettingsPropertyDefinition> setDict = SettingPropertyDefinitionCache.GetAllSettingPropertyDefinitions(settings1).ToDictionary((ISettingsPropertyDefinition x) => new ValueTuple<string, string>(x.DisplayName, x.GroupName), (ISettingsPropertyDefinition x) => x);
			Dictionary<ValueTuple<string, string>, ISettingsPropertyDefinition> setDict2 = SettingPropertyDefinitionCache.GetAllSettingPropertyDefinitions(settings2).ToDictionary((ISettingsPropertyDefinition x) => new ValueTuple<string, string>(x.DisplayName, x.GroupName), (ISettingsPropertyDefinition x) => x);
			if (setDict.Count != setDict2.Count)
			{
				return false;
			}
			foreach (KeyValuePair<ValueTuple<string, string>, ISettingsPropertyDefinition> kv in setDict)
			{
				ISettingsPropertyDefinition spd2;
				if (!setDict2.TryGetValue(kv.Key, out spd2) || !SettingsUtils.Equals(kv.Value, spd2))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x04000017 RID: 23
		[Nullable(2)]
		private static readonly SettingPropertyDefinitionCache.ClearDelegate ClearMethod = AccessTools2.GetDelegate<SettingPropertyDefinitionCache.ClearDelegate>(typeof(ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>>), "Clear", null, null, true);

		// Token: 0x04000018 RID: 24
		private static readonly ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>> _cache = new ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>>();

		// Token: 0x0200007B RID: 123
		// (Invoke) Token: 0x060004AC RID: 1196
		[NullableContext(0)]
		private delegate void ClearDelegate(ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>> instance);

		// Token: 0x0200007C RID: 124
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x04000167 RID: 359
			[Nullable(0)]
			public static Func<SettingsPropertyGroupDefinition, IEnumerable<ISettingsPropertyDefinition>> <0>__GetAllSettingPropertyDefinitions;

			// Token: 0x04000168 RID: 360
			[Nullable(0)]
			public static Func<SettingsPropertyGroupDefinition, IEnumerable<SettingsPropertyGroupDefinition>> <1>__GetAllSettingPropertyGroupDefinitions;
		}
	}
}
