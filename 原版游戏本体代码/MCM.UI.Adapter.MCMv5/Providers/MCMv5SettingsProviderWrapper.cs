using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Common;
using MCM.UI.Adapter.MCMv5.Base;
using MCM.UI.Adapter.MCMv5.Presets;

namespace MCM.UI.Adapter.MCMv5.Providers
{
	// Token: 0x0200000A RID: 10
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MCMv5SettingsProviderWrapper : SettingsProviderWrapper
	{
		// Token: 0x06000012 RID: 18 RVA: 0x000024BC File Offset: 0x000006BC
		public MCMv5SettingsProviderWrapper(object @object)
			: base(@object)
		{
			Type type = @object.GetType();
			this._methodGetPresetsDelegate = AccessTools2.GetDelegate<MCMv5SettingsProviderWrapper.GetPresetsDelegate>(@object, type, "GetPresets", null, null, true);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x000024F0 File Offset: 0x000006F0
		[return: Nullable(2)]
		protected override BaseSettings Create(object obj)
		{
			Type type = obj.GetType();
			bool flag = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.FluentGlobalSettings", true);
			BaseSettings result;
			if (flag)
			{
				result = new MCMv5FluentSettingsWrapper(obj);
			}
			else
			{
				bool flag2 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.GlobalSettings", true);
				if (flag2)
				{
					result = new MCMv5AttributeSettingsWrapper(obj);
				}
				else
				{
					bool flag3 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.FluentPerCampaignSettings", true);
					if (flag3)
					{
						result = new MCMv5FluentSettingsWrapper(obj);
					}
					else
					{
						bool flag4 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.PerCampaignSettings", true);
						if (flag4)
						{
							result = new MCMv5AttributeSettingsWrapper(obj);
						}
						else
						{
							bool flag5 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.FluentPerSaveSettings", true);
							if (flag5)
							{
								result = new MCMv5FluentSettingsWrapper(obj);
							}
							else
							{
								bool flag6 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.PerSaveSettings", true);
								if (flag6)
								{
									result = new MCMv5AttributeSettingsWrapper(obj);
								}
								else
								{
									result = null;
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000025AC File Offset: 0x000007AC
		protected override bool IsSettings(BaseSettings settings, [Nullable(2)] [NotNullWhen(true)] out object wrapped)
		{
			object obj;
			bool flag;
			if (settings is MCMv5AttributeSettingsWrapper || settings is MCMv5FluentSettingsWrapper)
			{
				IWrapper wrapper = (IWrapper)settings;
				obj = wrapper.Object;
				if (obj != null)
				{
					flag = true;
					goto IL_2C;
				}
			}
			flag = false;
			IL_2C:
			bool flag2 = flag;
			bool result;
			if (flag2)
			{
				wrapped = obj;
				result = true;
			}
			else
			{
				wrapped = null;
				result = false;
			}
			return result;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002600 File Offset: 0x00000800
		public override IEnumerable<ISettingsPreset> GetPresets(string settingsId)
		{
			BaseSettings settings = this.GetSettings(settingsId);
			object obj;
			bool flag;
			if (settings is MCMv5AttributeSettingsWrapper)
			{
				IWrapper wrapper = settings as IWrapper;
				if (wrapper != null)
				{
					obj = wrapper.Object;
					if (obj != null)
					{
						flag = this._methodGetPresetsDelegate == null;
						goto IL_34;
					}
				}
			}
			flag = true;
			IL_34:
			bool flag2 = flag;
			IEnumerable<ISettingsPreset> result;
			if (flag2)
			{
				result = Array.Empty<ISettingsPreset>();
			}
			else
			{
				Type type = obj.GetType();
				IEnumerable<object> presets = this._methodGetPresetsDelegate(settingsId).OfType<object>();
				bool flag3 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.FluentGlobalSettings", true);
				if (flag3)
				{
					result = from x in presets
						select new MCMv5FluentSettingsPresetWrapper(x);
				}
				else
				{
					bool flag4 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.GlobalSettings", true);
					if (flag4)
					{
						result = from x in presets
							select new MCMv5AttributeSettingsPresetWrapper(x);
					}
					else
					{
						bool flag5 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.FluentPerCampaignSettings", true);
						if (flag5)
						{
							result = from x in presets
								select new MCMv5FluentSettingsPresetWrapper(x);
						}
						else
						{
							bool flag6 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.PerCampaignSettings", true);
							if (flag6)
							{
								result = from x in presets
									select new MCMv5AttributeSettingsPresetWrapper(x);
							}
							else
							{
								bool flag7 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.FluentPerSaveSettings", true);
								if (flag7)
								{
									result = from x in presets
										select new MCMv5FluentSettingsPresetWrapper(x);
								}
								else
								{
									bool flag8 = ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.PerSaveSettings", true);
									if (flag8)
									{
										result = from x in presets
											select new MCMv5AttributeSettingsPresetWrapper(x);
									}
									else
									{
										result = Array.Empty<ISettingsPreset>();
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x000027E2 File Offset: 0x000009E2
		public override IEnumerable<UnavailableSetting> GetUnavailableSettings()
		{
			return Array.Empty<UnavailableSetting>();
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000027E9 File Offset: 0x000009E9
		public override IEnumerable<SettingSnapshot> SaveAvailableSnapshots()
		{
			return Array.Empty<SettingSnapshot>();
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000027F0 File Offset: 0x000009F0
		public override IEnumerable<BaseSettings> LoadAvailableSnapshots(IEnumerable<SettingSnapshot> snapshots)
		{
			return Array.Empty<BaseSettings>();
		}

		// Token: 0x04000006 RID: 6
		[Nullable(2)]
		private readonly MCMv5SettingsProviderWrapper.GetPresetsDelegate _methodGetPresetsDelegate;

		// Token: 0x02000026 RID: 38
		// (Invoke) Token: 0x0600011E RID: 286
		[NullableContext(0)]
		private delegate IEnumerable GetPresetsDelegate(string settingsId);
	}
}
