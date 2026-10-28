using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Bannerlord.ButterLib.SubSystems;
using Bannerlord.ButterLib.SubSystems.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.Options;

internal class SettingsProvider
{
	private static readonly PlatformDirectoryPath BasePath = EngineFilePaths.ConfigsPath + "/ModSettings/ButterLib";

	private static readonly PlatformFilePath optionsFilePath = new PlatformFilePath(BasePath, "Options.json");

	private static PropertyInfo? GetPropertyInfo<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		if (expression == null || !(expression.Body is MemberExpression { Member: PropertyInfo member }))
		{
			return null;
		}
		return member;
	}

	private static bool Read<TSubSystem>(TSubSystem subSystem, JsonReader reader) where TSubSystem : ISubSystem
	{
		JObject jo = JObject.Load(reader);
		bool result = true;
		string propertyName = typeof(TSubSystem).Name + " Enabled";
		if (jo.TryGetValue(propertyName, out JToken value))
		{
			result = value.ToObject<bool>();
		}
		if (!(subSystem is ISubSystemSettings<TSubSystem> subSystemSettings))
		{
			return result;
		}
		foreach (SubSystemSettingsDeclaration<TSubSystem> declaration in subSystemSettings.Declarations)
		{
			SubSystemSettingsDeclaration<TSubSystem> subSystemSettingsDeclaration = declaration;
			Action action;
			if (subSystemSettingsDeclaration is SubSystemSettingsPropertyBool<TSubSystem> subSystemSettingsPropertyBool)
			{
				PropertyInfo propertyInfo = GetPropertyInfo(subSystemSettingsPropertyBool.Property);
				if ((object)propertyInfo != null)
				{
					action = delegate
					{
						string propertyName2 = typeof(TSubSystem).Name + "_" + propertyInfo.Name;
						if (jo.TryGetValue(propertyName2, out JToken value2))
						{
							propertyInfo.SetValue(subSystem, value2.ToObject(propertyInfo.PropertyType));
						}
					};
					goto IL_01e0;
				}
			}
			SubSystemSettingsPropertyDropdown<TSubSystem> subSystemSettingsPropertyDropdown = subSystemSettingsDeclaration as SubSystemSettingsPropertyDropdown<TSubSystem>;
			if ((object)subSystemSettingsPropertyDropdown != null)
			{
				PropertyInfo propertyInfo2 = GetPropertyInfo(subSystemSettingsPropertyDropdown.Property);
				if ((object)propertyInfo2 != null)
				{
					action = delegate
					{
						string propertyName2 = typeof(TSubSystem).Name + "_" + propertyInfo2.Name;
						if (jo.TryGetValue(propertyName2, out JToken value2))
						{
							subSystemSettingsPropertyDropdown.SelectedIndex = value2.ToObject<int>();
						}
					};
					goto IL_01e0;
				}
			}
			if (subSystemSettingsDeclaration is SubSystemSettingsPropertyFloat<TSubSystem> subSystemSettingsPropertyFloat)
			{
				PropertyInfo propertyInfo3 = GetPropertyInfo(subSystemSettingsPropertyFloat.Property);
				if ((object)propertyInfo3 != null)
				{
					action = delegate
					{
						string propertyName2 = typeof(TSubSystem).Name + "_" + propertyInfo3.Name;
						if (jo.TryGetValue(propertyName2, out JToken value2))
						{
							propertyInfo3.SetValue(subSystem, value2.ToObject(propertyInfo3.PropertyType));
						}
					};
					goto IL_01e0;
				}
			}
			if (subSystemSettingsDeclaration is SubSystemSettingsPropertyInt<TSubSystem> subSystemSettingsPropertyInt)
			{
				PropertyInfo propertyInfo4 = GetPropertyInfo(subSystemSettingsPropertyInt.Property);
				if ((object)propertyInfo4 != null)
				{
					action = delegate
					{
						string propertyName2 = typeof(TSubSystem).Name + "_" + propertyInfo4.Name;
						if (jo.TryGetValue(propertyName2, out JToken value2))
						{
							propertyInfo4.SetValue(subSystem, value2.ToObject(propertyInfo4.PropertyType));
						}
					};
					goto IL_01e0;
				}
			}
			if (subSystemSettingsDeclaration is SubSystemSettingsPropertyText<TSubSystem> subSystemSettingsPropertyText)
			{
				PropertyInfo propertyInfo5 = GetPropertyInfo(subSystemSettingsPropertyText.Property);
				if ((object)propertyInfo5 != null)
				{
					action = delegate
					{
						string propertyName2 = typeof(TSubSystem).Name + "_" + propertyInfo5.Name;
						if (jo.TryGetValue(propertyName2, out JToken value2))
						{
							propertyInfo5.SetValue(subSystem, value2.ToObject(propertyInfo5.PropertyType));
						}
					};
					goto IL_01e0;
				}
			}
			action = null;
			goto IL_01e0;
			IL_01e0:
			action?.Invoke();
		}
		return result;
	}

	public static bool? PopulateSubSystemSettings<TSubSystem>(TSubSystem subSystem) where TSubSystem : ISubSystem
	{
		if (!FileHelper.FileExists(optionsFilePath))
		{
			return null;
		}
		JsonTextReader reader = new JsonTextReader(new System.IO.StringReader(FileHelper.GetFileContentString(optionsFilePath)));
		return Read(subSystem, reader);
	}

	public static ButterLibOptions GetSettings()
	{
		if (FileHelper.FileExists(optionsFilePath))
		{
			try
			{
				return JsonConvert.DeserializeObject<ButterLibOptions>(FileHelper.GetFileContentString(optionsFilePath)) ?? new ButterLibOptions();
			}
			catch (Exception ex) when (ex is JsonSerializationException)
			{
				FileHelper.SaveFileString(optionsFilePath, JsonConvert.SerializeObject(new object()));
			}
			catch
			{
			}
		}
		else
		{
			FileHelper.SaveFileString(optionsFilePath, JsonConvert.SerializeObject(new object()));
		}
		return new ButterLibOptions();
	}
}
