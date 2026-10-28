using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.SubSystems;
using Bannerlord.ButterLib.SubSystems.Settings;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Common;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace MCM.UI.Extensions;

internal static class SettingsBuilderExtensions
{
	private static PropertyInfo? GetPropertyInfo<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		if (expression == null || !(expression.Body is MemberExpression { Member: PropertyInfo member }))
		{
			return null;
		}
		return member;
	}

	private static void AddForSubSystem<TSubSystem>(TSubSystem subSystem, ISettingsBuilder settingsBuilder) where TSubSystem : ISubSystem
	{
		if (!(subSystem is ISubSystemSettings<TSubSystem> subSystemSettings))
		{
			return;
		}
		foreach (SubSystemSettingsDeclaration<TSubSystem> declaration in subSystemSettings.Declarations)
		{
			settingsBuilder.CreateGroup("SubSystem " + subSystem.Id, delegate(ISettingsPropertyGroupBuilder groupBuilder)
			{
				SubSystemSettingsDeclaration<TSubSystem> subSystemSettingsDeclaration = declaration;
				SubSystemSettingsPropertyBool<TSubSystem> subSystemSettingsPropertyBool = subSystemSettingsDeclaration as SubSystemSettingsPropertyBool<TSubSystem>;
				if ((object)subSystemSettingsPropertyBool != null)
				{
					PropertyInfo propertyInfo = GetPropertyInfo(subSystemSettingsPropertyBool.Property);
					if ((object)propertyInfo != null)
					{
						groupBuilder.AddBool(subSystem.GetType().Name + "_" + propertyInfo.Name, new TextObject(subSystemSettingsPropertyBool.Name).ToString(), new PropertyRef(propertyInfo, subSystem), delegate(ISettingsPropertyBoolBuilder builder)
						{
							builder.SetHintText(subSystemSettingsPropertyBool.Description);
						});
						return;
					}
				}
				SubSystemSettingsPropertyDropdown<TSubSystem> subSystemSettingsPropertyDropdown = subSystemSettingsDeclaration as SubSystemSettingsPropertyDropdown<TSubSystem>;
				if ((object)subSystemSettingsPropertyDropdown != null)
				{
					PropertyInfo propertyInfo2 = GetPropertyInfo(subSystemSettingsPropertyDropdown.Property);
					if ((object)propertyInfo2 != null)
					{
						groupBuilder.AddDropdown(subSystem.GetType().Name + "_" + propertyInfo2.Name, new TextObject(subSystemSettingsPropertyDropdown.Name).ToString(), subSystemSettingsPropertyDropdown.SelectedIndex, new PropertyRef(propertyInfo2, subSystem), delegate(ISettingsPropertyDropdownBuilder builder)
						{
							builder.SetHintText(subSystemSettingsPropertyDropdown.Description);
						});
						return;
					}
				}
				SubSystemSettingsPropertyFloat<TSubSystem> subSystemSettingsPropertyFloat = subSystemSettingsDeclaration as SubSystemSettingsPropertyFloat<TSubSystem>;
				if ((object)subSystemSettingsPropertyFloat != null)
				{
					PropertyInfo propertyInfo3 = GetPropertyInfo(subSystemSettingsPropertyFloat.Property);
					if ((object)propertyInfo3 != null)
					{
						groupBuilder.AddFloatingInteger(subSystem.GetType().Name + "_" + propertyInfo3.Name, new TextObject(subSystemSettingsPropertyFloat.Name).ToString(), subSystemSettingsPropertyFloat.MinValue, subSystemSettingsPropertyFloat.MaxValue, new PropertyRef(propertyInfo3, subSystem), delegate(ISettingsPropertyFloatingIntegerBuilder builder)
						{
							builder.SetHintText(subSystemSettingsPropertyFloat.Description);
						});
						return;
					}
				}
				SubSystemSettingsPropertyInt<TSubSystem> subSystemSettingsPropertyInt = subSystemSettingsDeclaration as SubSystemSettingsPropertyInt<TSubSystem>;
				if ((object)subSystemSettingsPropertyInt != null)
				{
					PropertyInfo propertyInfo4 = GetPropertyInfo(subSystemSettingsPropertyInt.Property);
					if ((object)propertyInfo4 != null)
					{
						groupBuilder.AddInteger(subSystem.GetType().Name + "_" + propertyInfo4.Name, new TextObject(subSystemSettingsPropertyInt.Name).ToString(), subSystemSettingsPropertyInt.MinValue, subSystemSettingsPropertyInt.MaxValue, new PropertyRef(propertyInfo4, subSystem), delegate(ISettingsPropertyIntegerBuilder builder)
						{
							builder.SetHintText(subSystemSettingsPropertyInt.Description);
						});
						return;
					}
				}
				SubSystemSettingsPropertyText<TSubSystem> subSystemSettingsPropertyText = subSystemSettingsDeclaration as SubSystemSettingsPropertyText<TSubSystem>;
				if ((object)subSystemSettingsPropertyText != null)
				{
					PropertyInfo propertyInfo5 = GetPropertyInfo(subSystemSettingsPropertyText.Property);
					if ((object)propertyInfo5 != null)
					{
						groupBuilder.AddText(subSystem.GetType().Name + "_" + propertyInfo5.Name, new TextObject(subSystemSettingsPropertyText.Name).ToString(), new PropertyRef(propertyInfo5, subSystem), delegate(ISettingsPropertyTextBuilder builder)
						{
							builder.SetHintText(subSystemSettingsPropertyText.Description);
						});
						return;
					}
				}
				global::_003CPrivateImplementationDetails_003E.ThrowInvalidOperationException();
			});
		}
	}

	public static ISettingsBuilder AddButterLibSubSystems(this ISettingsBuilder settings)
	{
		foreach (ISubSystem subSystem in ((Game)null).GetServiceProvider().GetRequiredService<IEnumerable<ISubSystem>>())
		{
			if (!subSystem.CanBeDisabled)
			{
				continue;
			}
			ProxyRef<bool> prop = new ProxyRef<bool>(() => subSystem.IsEnabled, delegate(bool state)
			{
				if (state)
				{
					subSystem.Enable();
				}
				else
				{
					subSystem.Disable();
				}
			});
			settings = settings.CreateGroup("SubSystem " + subSystem.Id, delegate(ISettingsPropertyGroupBuilder builder)
			{
				builder.AddBool(subSystem.Id + " Enabled", "Enabled", prop, delegate(ISettingsPropertyBoolBuilder bBuilder)
				{
					bBuilder.SetHintText(subSystem.Description).SetRequireRestart(!subSystem.CanBeSwitchedAtRuntime);
				});
			});
			Type type = subSystem.GetType();
			if (type.GetInterfaces().Any((Type x) => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISubSystemSettings<>)))
			{
				(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(typeof(SettingsBuilderExtensions), "AddForSubSystem")?.MakeGenericMethod(type))?.Invoke(null, new object[2] { subSystem, settings });
			}
		}
		return settings;
	}
}
