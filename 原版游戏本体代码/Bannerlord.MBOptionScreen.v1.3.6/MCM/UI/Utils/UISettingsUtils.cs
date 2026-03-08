using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Bannerlord.ModuleManager;
using ComparerExtensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Common;
using MCM.UI.Actions;
using MCM.UI.GUI.ViewModels;
using Microsoft.Extensions.Logging;
using TaleWorlds.Localization;

namespace MCM.UI.Utils
{
	// Token: 0x02000014 RID: 20
	[NullableContext(1)]
	[Nullable(0)]
	internal static class UISettingsUtils
	{
		// Token: 0x06000061 RID: 97 RVA: 0x00002F1C File Offset: 0x0000111C
		public static void OverrideValues(UndoRedoStack urs, BaseSettings current, BaseSettings @new)
		{
			Dictionary<string, SettingsPropertyGroupDefinition> currentDict = SettingPropertyDefinitionCache.GetSettingPropertyGroups(current).ToDictionary((SettingsPropertyGroupDefinition x) => x.GroupNameRaw, (SettingsPropertyGroupDefinition x) => x);
			foreach (SettingsPropertyGroupDefinition nspg in SettingPropertyDefinitionCache.GetAllSettingPropertyGroupDefinitions(@new))
			{
				SettingsPropertyGroupDefinition spg;
				if (currentDict.TryGetValue(nspg.GroupNameRaw, out spg))
				{
					UISettingsUtils.OverrideValues(urs, spg, nspg);
				}
				else
				{
					LoggerExtensions.LogWarning(MCMUISubModule.Logger, "{NewId}::{GroupName} was not found on, {CurrentId}", new object[] { @new.Id, nspg.GroupNameRaw, current.Id });
				}
			}
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00002FF8 File Offset: 0x000011F8
		public static void OverrideValues(UndoRedoStack urs, SettingsPropertyGroupDefinition current, SettingsPropertyGroupDefinition @new)
		{
			Dictionary<string, SettingsPropertyGroupDefinition> currentSubGroups = current.SubGroups.ToDictionary((SettingsPropertyGroupDefinition x) => x.GroupNameRaw, (SettingsPropertyGroupDefinition x) => x);
			Dictionary<string, ISettingsPropertyDefinition> currentSettingProperties = current.SettingProperties.ToDictionary((ISettingsPropertyDefinition x) => x.DisplayName, (ISettingsPropertyDefinition x) => x);
			foreach (SettingsPropertyGroupDefinition nspg in @new.SubGroups)
			{
				SettingsPropertyGroupDefinition spg;
				if (currentSubGroups.TryGetValue(nspg.GroupNameRaw, out spg))
				{
					UISettingsUtils.OverrideValues(urs, spg, nspg);
				}
				else
				{
					LoggerExtensions.LogWarning(MCMUISubModule.Logger, "{NewId}::{GroupName} was not found on, {CurrentId}", new object[] { @new.GroupNameRaw, nspg.GroupNameRaw, current.GroupNameRaw });
				}
			}
			foreach (ISettingsPropertyDefinition nsp in @new.SettingProperties)
			{
				ISettingsPropertyDefinition sp;
				if (currentSettingProperties.TryGetValue(nsp.DisplayName, out sp))
				{
					UISettingsUtils.OverrideValues(urs, sp, nsp);
				}
				else
				{
					LoggerExtensions.LogWarning(MCMUISubModule.Logger, "{NewId}::{GroupName} was not found on, {CurrentId}", new object[] { @new.GroupNameRaw, nsp.DisplayName, current.GroupNameRaw });
				}
			}
		}

		// Token: 0x06000063 RID: 99 RVA: 0x000031A8 File Offset: 0x000013A8
		public static void OverrideValues(UndoRedoStack urs, ISettingsPropertyDefinition current, ISettingsPropertyDefinition @new)
		{
			if (SettingsUtils.Equals(current, @new))
			{
				return;
			}
			switch (current.SettingType)
			{
			case 0:
			{
				object value = @new.PropertyReference.Value;
				if (value is bool)
				{
					bool val = (bool)value;
					urs.Do(new SetValueTypeAction<bool>(current.PropertyReference, val));
					return;
				}
				break;
			}
			case 1:
			{
				object value = @new.PropertyReference.Value;
				if (value is int)
				{
					int val2 = (int)value;
					urs.Do(new SetValueTypeAction<int>(current.PropertyReference, val2));
					return;
				}
				break;
			}
			case 2:
			{
				object value = @new.PropertyReference.Value;
				if (value is float)
				{
					float val3 = (float)value;
					urs.Do(new SetValueTypeAction<float>(current.PropertyReference, val3));
					return;
				}
				break;
			}
			case 3:
			{
				string val4 = @new.PropertyReference.Value as string;
				if (val4 != null)
				{
					urs.Do(new SetStringAction(current.PropertyReference, val4));
					return;
				}
				break;
			}
			case 4:
			{
				object val5 = @new.PropertyReference.Value;
				if (val5 != null)
				{
					urs.Do(new SetSelectedIndexAction(current.PropertyReference, val5));
					return;
				}
				break;
			}
			case 5:
			{
				Action val6 = @new.PropertyReference.Value as Action;
				break;
			}
			default:
				return;
			}
		}

		// Token: 0x06000064 RID: 100 RVA: 0x000032E4 File Offset: 0x000014E4
		public static IEnumerable<object> GetDropdownValues(IRef @ref)
		{
			object value = @ref.Value;
			IEnumerable<object> enumerableObj = value as IEnumerable<object>;
			IEnumerable<object> result;
			if (enumerableObj == null)
			{
				IEnumerable enumerable = value as IEnumerable;
				if (enumerable == null)
				{
					result = Array.Empty<object>();
				}
				else
				{
					result = enumerable.Cast<object>();
				}
			}
			else
			{
				result = enumerableObj;
			}
			return result;
		}

		// Token: 0x04000019 RID: 25
		public static readonly IComparer<SettingsPropertyVM> SettingsPropertyVMComparer = (from x in KeyComparer<SettingsPropertyVM>
			orderby x.SettingPropertyDefinition.Order
			select x).ThenBy((SettingsPropertyVM x) => new TextObject(x.SettingPropertyDefinition.DisplayName, null).ToString(), new AlphanumComparatorFast());

		// Token: 0x0400001A RID: 26
		public static readonly IComparer<SettingsPropertyGroupVM> SettingsPropertyGroupVMComparer = (from x in KeyComparer<SettingsPropertyGroupVM>
			orderby x.SettingPropertyGroupDefinition.GroupNameRaw == SettingsPropertyGroupDefinition.DefaultGroupName descending, x.SettingPropertyGroupDefinition.Order
			select x).ThenBy((SettingsPropertyGroupVM x) => new TextObject(x.SettingPropertyGroupDefinition.GroupName, null).ToString(), new AlphanumComparatorFast());
	}
}
