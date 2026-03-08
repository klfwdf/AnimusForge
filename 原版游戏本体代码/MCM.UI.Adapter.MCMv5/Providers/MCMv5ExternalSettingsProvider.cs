using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection.Logger;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;

namespace MCM.UI.Adapter.MCMv5.Providers
{
	// Token: 0x02000009 RID: 9
	[NullableContext(1)]
	[Nullable(0)]
	public sealed class MCMv5ExternalSettingsProvider : IExternalSettingsProvider, IExternalSettingsProviderHasInitialize
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600000A RID: 10 RVA: 0x000021DC File Offset: 0x000003DC
		public IEnumerable<SettingsDefinition> SettingsDefinitions
		{
			get
			{
				return from x in this._settingsProviderWrappers.SelectMany((MCMv5SettingsProviderWrapper x) => x.SettingsDefinitions)
					where x.SettingsId != "MCM_v5"
					select x;
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002237 File Offset: 0x00000437
		public MCMv5ExternalSettingsProvider(IBUTRLogger<MCMv5ExternalSettingsProvider> logger)
		{
			this._logger = logger;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002254 File Offset: 0x00000454
		public void Initialize()
		{
			IEnumerable<object> foreignBaseSettingsProviders = (from x in AccessTools2.AllTypes()
				where x.Assembly != typeof(BaseSettings).Assembly
				where !x.IsSubclassOf(typeof(BaseSettingsProvider)) && ReflectionUtils.ImplementsOrImplementsEquivalent(x, "MCM.Abstractions.BaseSettingsProvider", true)
				select x).Select(delegate(Type x)
			{
				PropertyInfo prop = AccessTools2.DeclaredProperty(x, "Instance", true);
				if (prop != null)
				{
					MethodInfo getMethod = prop.GetMethod;
					if (getMethod != null && getMethod.IsStatic)
					{
						return prop.GetValue(null);
					}
				}
				return null;
			}).OfType<object>();
			this._settingsProviderWrappers.AddRange(from x in foreignBaseSettingsProviders
				select new MCMv5SettingsProviderWrapper(x));
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000230C File Offset: 0x0000050C
		[return: Nullable(2)]
		public BaseSettings GetSettings(string id)
		{
			foreach (MCMv5SettingsProviderWrapper settingsProvider in this._settingsProviderWrappers)
			{
				BaseSettings settings = settingsProvider.GetSettings(id);
				bool flag = settings != null;
				if (flag)
				{
					return settings;
				}
			}
			LoggerExtensions.LogWarning(this._logger, "GetSettings " + id + " returned null", Array.Empty<object>());
			return null;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000239C File Offset: 0x0000059C
		public void SaveSettings(BaseSettings settings)
		{
			foreach (MCMv5SettingsProviderWrapper settingsProvider in this._settingsProviderWrappers)
			{
				settingsProvider.SaveSettings(settings);
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000023F4 File Offset: 0x000005F4
		public void ResetSettings(BaseSettings settings)
		{
			foreach (MCMv5SettingsProviderWrapper settingsProvider in this._settingsProviderWrappers)
			{
				settingsProvider.ResetSettings(settings);
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000244C File Offset: 0x0000064C
		public void OverrideSettings(BaseSettings settings)
		{
			foreach (MCMv5SettingsProviderWrapper settingsProvider in this._settingsProviderWrappers)
			{
				settingsProvider.OverrideSettings(settings);
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000024A4 File Offset: 0x000006A4
		public IEnumerable<ISettingsPreset> GetPresets(string id)
		{
			MCMv5ExternalSettingsProvider.<GetPresets>d__10 <GetPresets>d__ = new MCMv5ExternalSettingsProvider.<GetPresets>d__10(-2);
			<GetPresets>d__.<>4__this = this;
			<GetPresets>d__.<>3__id = id;
			return <GetPresets>d__;
		}

		// Token: 0x04000004 RID: 4
		private readonly IBUTRLogger _logger;

		// Token: 0x04000005 RID: 5
		private readonly List<MCMv5SettingsProviderWrapper> _settingsProviderWrappers = new List<MCMv5SettingsProviderWrapper>();
	}
}
