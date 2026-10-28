using System;
using System.Reflection;
using Bannerlord.UIExtenderEx.Extensions;
using HarmonyLib;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx;

internal abstract class BUTRViewModel : ViewModel
{
	protected BUTRViewModel()
	{
		PropertyInfo[] properties = GetType().GetProperties(AccessTools.all);
		PropertyInfo[] array = properties;
		foreach (PropertyInfo propertyInfo in array)
		{
			BUTRDataSourcePropertyAttribute customAttribute = propertyInfo.GetCustomAttribute<BUTRDataSourcePropertyAttribute>();
			if (customAttribute == null)
			{
				continue;
			}
			MethodInfo? getMethod = propertyInfo.GetMethod;
			if ((object)getMethod == null || !getMethod.IsPrivate)
			{
				MethodInfo? setMethod = propertyInfo.SetMethod;
				if ((object)setMethod == null || !setMethod.IsPrivate)
				{
					this.AddProperty(customAttribute.OverrideName ?? propertyInfo.Name, propertyInfo);
					continue;
				}
			}
			throw new Exception();
		}
		MethodInfo[] methods = GetType().GetMethods(AccessTools.all);
		MethodInfo[] array2 = methods;
		foreach (MethodInfo methodInfo in array2)
		{
			BUTRDataSourceMethodAttribute customAttribute2 = methodInfo.GetCustomAttribute<BUTRDataSourceMethodAttribute>();
			if (customAttribute2 != null)
			{
				if (methodInfo.IsPrivate)
				{
					throw new Exception();
				}
				this.AddMethod(customAttribute2.OverrideName ?? methodInfo.Name, methodInfo);
			}
		}
	}
}
